using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// The satchel, on screen. A thin view over <see cref="InventoryCraftingManager"/> and nothing
/// more: it asks what is held and whether the selection can be used, and it never decides either.
/// That is what keeps the whole game assertable with no mouse — see handoff, "Who does what".
///
/// Layout lives in <c>scenes/ui/InventoryPanel.tscn</c>; this file only knows the node *names* in
/// the published contract, so the scene can be restyled freely without touching code.
/// </summary>
public partial class InventoryPanel : Control
{
    private Container _slotContainer;
    private Control _slotTemplate;
    private Label _itemName;
    private Label _itemDescription;
    private Button _useButton;
    private Button _closeButton;

    /// <summary>Slots built on the last refresh, in the order they are drawn.</summary>
    private readonly List<Control> _slots = new();

    private string _selectedItemId = "";

    /// <summary>
    /// Set when the satchel changed while the panel was down. See <see cref="OnInventoryChanged"/>:
    /// rebuilding a grid nobody is looking at is pure waste, and pickups happen constantly.
    /// </summary>
    private bool _dirty = true;

    /// <summary>Selected slot brightness, against <see cref="UnselectedSlotAlpha"/> for the rest.</summary>
    private const float UnselectedSlotAlpha = 0.62f;

    // ---- For the gates ---------------------------------------------------
    // All of these read what is on screen rather than recomputing the condition, for the same
    // reason Hud.TopPoolLabel does: a property that recomputes only proves it agrees with itself.

    /// <summary>Whether the panel is up. Reads <c>Visible</c> rather than a bool of its own.</summary>
    public bool IsOpen => Visible;

    /// <summary>How many slots are drawn — one per held stack, template excluded.</summary>
    public int SlotCount => _slots.Count;

    /// <summary>Item id of the selected slot, or "" when nothing is selected.</summary>
    public string SelectedItemId => _selectedItemId;

    /// <summary>The name shown in the detail column.</summary>
    public string DetailName => _itemName?.Text ?? "";

    /// <summary>Whether the Use button is clickable as drawn.</summary>
    public bool UseEnabled => _useButton != null && !_useButton.Disabled;

    public override void _Ready()
    {
        _slotContainer = GetNode<Container>("%SlotContainer");
        _slotTemplate = GetNode<Control>("%SlotTemplate");
        _itemName = GetNode<Label>("%ItemName");
        _itemDescription = GetNode<Label>("%ItemDescription");
        _useButton = GetNode<Button>("%UseButton");
        _closeButton = GetNode<Button>("%CloseButton");

        _slotTemplate.Visible = false;

        _useButton.Pressed += OnUsePressed;
        _closeButton.Pressed += Close;

        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.InventoryChanged += OnInventoryChanged;
            // The Use button's enabled state depends on the *body*, not only on the satchel:
            // a salve is refused at full Vitality (invariant 7). Heal to full with the panel
            // open and the button has to go grey without the inventory having changed at all.
            bus.VitalityChanged += OnVitalityChanged;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        if (_useButton != null) _useButton.Pressed -= OnUsePressed;
        if (_closeButton != null) _closeButton.Pressed -= Close;

        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.InventoryChanged -= OnInventoryChanged;
        bus.VitalityChanged -= OnVitalityChanged;
    }

    // ---- Opening and closing ---------------------------------------------

    /// <summary>
    /// Catches up on the way up. The satchel changes constantly while the panel is down — every
    /// pickup, every craft, and a load restoring a whole different satchel — and
    /// <see cref="OnInventoryChanged"/> defers all of it to here.
    /// </summary>
    public void Open()
    {
        if (_dirty) Refresh();
        Visible = true;
    }

    public void Close() => Visible = false;

    public void Toggle()
    {
        if (Visible) Close();
        else Open();
    }

    // ---- Building the grid -----------------------------------------------

    /// <summary>
    /// Throws the slots away and rebuilds them from the manager. Rebuild-everything rather than
    /// diffing: a satchel is a few dozen stacks at most, and a diff would be the second place in
    /// the codebase that has an opinion about what is held.
    /// </summary>
    private void Refresh()
    {
        if (_slotContainer == null || _slotTemplate == null) return;

        _dirty = false;

        // Removed before freeing so the container re-flows this frame rather than next. The
        // per-slot GuiInput lambdas need no unsubscribing — they are connected to the slot itself,
        // so they die with it. Nothing here touches the template, which stays in the scene hidden.
        foreach (var slot in _slots)
        {
            _slotContainer.RemoveChild(slot);
            slot.QueueFree();
        }
        _slots.Clear();

        var inv = InventoryCraftingManager.Instance;
        if (inv == null)
        {
            RefreshDetail();
            return;
        }

        foreach (var (itemId, count) in Ordered(inv))
        {
            var slot = (Control)_slotTemplate.Duplicate();
            slot.Name = $"Slot_{itemId}";
            slot.Visible = true;
            // Duplicated nodes have no owner, so %UniqueName does not resolve inside one. The
            // template's children are found by name instead — which is why the contract names
            // SlotSwatch and SlotCount rather than describing their position.
            slot.MouseFilter = MouseFilterEnum.Stop;

            if (slot.FindChild("SlotSwatch", true, false) is ColorRect swatch)
                swatch.Color = inv.GetItem(itemId)?.PlaceholderColor ?? new Color("#6b7f5e");

            if (slot.FindChild("SlotCount", true, false) is Label countLabel)
                countLabel.Text = count > 1 ? count.ToString() : "";

            slot.TooltipText = inv.DisplayNameOf(itemId);

            var captured = itemId;
            slot.GuiInput += @event => OnSlotInput(@event, captured);

            _slotContainer.AddChild(slot);
            _slots.Add(slot);
        }

        // A stack spent down to nothing takes the selection with it.
        if (!string.IsNullOrEmpty(_selectedItemId) && !inv.Has(_selectedItemId))
            _selectedItemId = "";

        RefreshDetail();
    }

    /// <summary>
    /// A stable order, so the grid does not reshuffle on every pickup and so the gates can name a
    /// slot. Salves first — the things you reach for under pressure — then by name within a kind.
    /// </summary>
    private static IEnumerable<(string ItemId, int Count)> Ordered(InventoryCraftingManager inv) =>
        inv.Stacks
           .Where(pair => pair.Value > 0)
           .OrderBy(pair => (int)(inv.GetItem(pair.Key)?.Category ?? ItemCategory.Reagent))
           .ThenBy(pair => inv.DisplayNameOf(pair.Key))
           .Select(pair => (pair.Key, pair.Value));

    // ---- The detail column -----------------------------------------------

    private void RefreshDetail()
    {
        var inv = InventoryCraftingManager.Instance;
        var item = string.IsNullOrEmpty(_selectedItemId) ? null : inv?.GetItem(_selectedItemId);

        if (_itemName != null)
            _itemName.Text = item != null ? item.DisplayName : "Nothing selected";

        if (_itemDescription != null)
            _itemDescription.Text = item?.Description ?? "";

        // The one question the panel asks and does not answer. UseBlock lives in the manager so
        // this button and the act of using can never disagree.
        if (_useButton != null)
            _useButton.Disabled = item == null || inv?.CanUse(_selectedItemId) != true;

        for (var i = 0; i < _slots.Count; i++)
        {
            var isSelected = _slots[i].Name.ToString() == $"Slot_{_selectedItemId}";
            _slots[i].Modulate = new Color(1f, 1f, 1f, isSelected ? 1f : UnselectedSlotAlpha);
        }
    }

    private void Select(string itemId)
    {
        _selectedItemId = itemId;
        RefreshDetail();
    }

    /// <summary>Selection, exposed so the gates can drive the panel without a mouse.</summary>
    public void DebugSelect(string itemId) => Select(itemId);

    // ---- Signals ----------------------------------------------------------

    private void OnSlotInput(InputEvent @event, string itemId)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            Select(itemId);
    }

    private void OnUsePressed()
    {
        if (string.IsNullOrEmpty(_selectedItemId)) return;

        // Fire and forget. The manager decides, spends, heals and narrates the refusal; the
        // InventoryChanged that follows a success is what redraws this panel.
        InventoryCraftingManager.Instance?.UseItem(_selectedItemId);
    }

    /// <summary>
    /// Rebuild now if the player is looking, otherwise note it and let <see cref="Open"/> catch up.
    /// Both paths are asserted in <c>--verify-slice</c>; the deferred one is easy to get wrong and
    /// shows up as a stale grid after a load, which is the blind spot the handoff already names.
    /// </summary>
    private void OnInventoryChanged()
    {
        if (Visible) Refresh();
        else _dirty = true;
    }

    private void OnVitalityChanged(float current, float max) => RefreshDetail();
}
