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
    private Button _equipButton;
    private Button _closeButton;
    private Container _tabStrip;

    /// <summary>Slots built on the last refresh, in the order they are drawn.</summary>
    private readonly List<Control> _slots = new();

    /// <summary>
    /// Tabs built on the last refresh, in the order they are drawn — <c>null</c> for All, then one
    /// <see cref="ItemCategory"/> per kind actually carried.
    /// </summary>
    private readonly List<(ItemCategory? Category, Button Button)> _tabs = new();

    /// <summary>
    /// Which kind the grid is filtered to, <c>null</c> meaning All.
    /// </summary>
    /// <remarks>
    /// <b>What protects the seven older <see cref="SlotCount"/> gates is not this initialiser.</b> The plan
    /// claimed it was — those gates count held stacks across mixed categories, so a filtered grid would
    /// turn them all red — and the break-test said otherwise: initialising this to
    /// <see cref="ItemCategory.Salve"/> left the suite fully green, because
    /// <see cref="RefreshTabs"/> finds no salve in an empty satchel and normalises the filter back to All
    /// before any gate reads a slot. Those gates are safe for a duller reason too: none of them touches a
    /// tab, and nothing else does either.
    /// <para>
    /// So the filter is only reachable through <see cref="SelectTab"/>, and the one mutation that does turn
    /// a gate red is a strand-fallback that picks a kind instead of All — which is the failure worth having
    /// a gate for, and now has one.
    /// </para>
    /// </remarks>
    private ItemCategory? _activeCategory;

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

    /// <summary>
    /// Whether the Equip button is clickable as drawn — the same read-off as <see cref="UseEnabled"/>,
    /// and it answers the same question about a different verb. Added by unit 3a.
    /// </summary>
    public bool EquipEnabled => _equipButton != null && !_equipButton.Disabled;

    /// <summary>How many tabs are drawn, counting All. One on an empty satchel, and it grows with it.</summary>
    public int TabCount => _tabs.Count;

    /// <summary>
    /// The label on the tab currently filtering the grid — the <i>player's</i> word for the kind, read
    /// off the button rather than mapped again from <see cref="_activeCategory"/>, so the gate proves
    /// what is on screen and not that the table agrees with itself.
    /// </summary>
    public string ActiveTabLabel
    {
        get
        {
            foreach (var (category, button) in _tabs)
                if (category.Equals(_activeCategory)) return button.Text;

            return "";
        }
    }

    /// <summary>The kinds the strip offers, All excluded. For the gate that asserts Lore has no tab yet.</summary>
    public IEnumerable<ItemCategory> TabbedCategories =>
        _tabs.Where(t => t.Category.HasValue).Select(t => t.Category.Value);

    /// <summary>
    /// The caption drawn on each slot, in draw order — read off the label rather than recomputed from the
    /// stack, so the gate proves the tile says the name and not that <c>DisplayNameOf</c> is a function.
    /// Empty string for a slot with no caption at all, which is the failure worth catching.
    /// </summary>
    public IEnumerable<string> SlotNames =>
        _slots.Select(slot => slot.FindChild("SlotName", true, false) is Label label ? label.Text : "");

    /// <summary>
    /// Whether every drawn stack count has been given a colour of its own. False when a slot is left
    /// wearing <c>PanelTheme.tres</c>'s pale cream, which is the bug the first playtest found on
    /// Graveflower.
    /// </summary>
    public bool SlotCountsAreLegible =>
        _slots.All(slot => slot.FindChild("SlotCount", true, false) is not Label label ||
                           (label.HasThemeColorOverride("font_color") &&
                            label.GetThemeColor("font_color").Luminance < 0.3f &&
                            label.HasThemeColorOverride("font_outline_color")));

    /// <summary>
    /// Whether <see cref="MakeCountLegible"/> keeps its hands off a label that already has a colour.
    /// <b>Driven against a probe rather than a drawn slot, because the guard is only observable when there
    /// is something to guard</b> — and no authored slot has an override yet, so on screen the guarded and
    /// unguarded versions are indistinguishable. That is exactly the class of stopgap that rots silently.
    /// </summary>
    public static bool CountColourDefersToTheScene()
    {
        var authored = new Color(0.85f, 0.15f, 0.1f);

        var probe = new Label();
        probe.AddThemeColorOverride("font_color", authored);

        MakeCountLegible(probe);

        var kept = probe.GetThemeColor("font_color").IsEqualApprox(authored);
        var leftAlone = !probe.HasThemeColorOverride("font_outline_color");
        probe.Free();

        return kept && leftAlone;
    }

    /// <summary>
    /// Where the caption band and the stack count land on a tile, once
    /// <see cref="EnsureSlotNameOnTemplate"/> has run over <c>SlotTemplate</c>'s geometry: the caption's
    /// box, the count's box and the swatch's box, in tile-local pixels.
    /// </summary>
    /// <remarks>
    /// <b>Driven against a synthetic tile, not the live one</b>, and that is the point. Reading the drawn
    /// slots would put the gate red the day Jeremy authors a real <c>SlotName</c> — the builder stops, the
    /// count keeps his offsets, and a good change would look like a regression. This asserts the
    /// <i>builder's</i> arithmetic against the geometry the template has today, so it only ever fails on
    /// code, which is what it is for.
    /// <para>
    /// The boxes are computed from anchors and offsets rather than read from <c>Size</c>, because nothing
    /// here is in a tree and so nothing has been laid out. This is the same sum Godot does.
    /// </para>
    /// </remarks>
    public static (Rect2 Caption, Rect2 Count, Rect2 Swatch) CaptionLayoutOnATile()
    {
        const float side = SlotTemplateSide;

        var tile = new Control { Name = "Probe", Size = new Vector2(side, side) };

        var swatch = new ColorRect { Name = "SlotSwatch" };
        tile.AddChild(swatch);
        swatch.SetAnchorsPreset(LayoutPreset.FullRect);
        swatch.OffsetLeft = SwatchInset;
        swatch.OffsetTop = SwatchInset;
        swatch.OffsetRight = -SwatchInset;
        swatch.OffsetBottom = -SwatchInset;

        var count = new Label { Name = "SlotCount" };
        tile.AddChild(count);
        count.SetAnchorsPreset(LayoutPreset.BottomRight);
        count.OffsetLeft = -44f;
        count.OffsetTop = -34f;
        count.OffsetRight = -8f;
        count.OffsetBottom = -6f;

        EnsureSlotNameOnTemplate(tile);

        var boxes = (
            Box(tile.FindChild("SlotName", true, false) as Control, side),
            Box(count, side),
            Box(swatch, side));

        tile.Free();
        return boxes;

        static Rect2 Box(Control control, float side)
        {
            if (control == null) return new Rect2();
            var left = control.AnchorLeft * side + control.OffsetLeft;
            var top = control.AnchorTop * side + control.OffsetTop;
            return new Rect2(left, top,
                             control.AnchorRight * side + control.OffsetRight - left,
                             control.AnchorBottom * side + control.OffsetBottom - top);
        }
    }

    /// <summary>The tile's authored side, from <c>SlotTemplate</c> in <c>InventoryPanel.tscn</c>.</summary>
    private const float SlotTemplateSide = 112f;

    /// <summary><c>SlotSwatch</c>'s authored inset inside the tile, all four sides.</summary>
    private const float SwatchInset = 11f;

    /// <summary>
    /// Height of the caption strip at the bottom of a tile, measured up from the tile's bottom edge. The
    /// swatch is shortened to exactly this, so the caption sits <i>below</i> the artwork rather than across
    /// it — which is what leaves the swatch a bottom-right corner for the stack count to sit in. See
    /// <see cref="EnsureSlotNameOnTemplate"/>'s remarks for why the square swatch and the corner cannot
    /// both be had on a 112px tile.
    /// </summary>
    private const float CaptionBand = 40f;

    /// <summary>
    /// How far inside the swatch's corner the stack count sits — Jeremy's <i>"a few pixels"</i>. Public
    /// because the gate over this asserts the <i>gap</i> rather than a coordinate: a count at the right
    /// absolute pixels on a taller swatch reads as floating mid-tile, which is the bug that got past four
    /// green gates and one screenshot.
    /// </summary>
    public const float CountInset = 5f;

    public override void _Ready()
    {
        _slotContainer = GetNode<Container>("%SlotContainer");
        _slotTemplate = GetNode<Control>("%SlotTemplate");
        _itemName = GetNode<Label>("%ItemName");
        _itemDescription = GetNode<Label>("%ItemDescription");
        _useButton = GetNode<Button>("%UseButton");
        _closeButton = GetNode<Button>("%CloseButton");

        _slotTemplate.Visible = false;

        // Optional in the contract, unlike every node above it: `InventoryPanel.tscn` was authored before
        // anything could be worn, so until Jeremy adds an `%EquipButton` beside `%UseButton` one is built
        // here as its sibling. `GetNodeOrNull` rather than `GetNode` is what makes the authored version a
        // drop-in — the day it exists this branch stops running and nothing else changes.
        _equipButton = GetNodeOrNull<Button>("%EquipButton") ?? BuildEquipButtonBeside(_useButton);

        // Both optional on the same terms as the Equip button. The strip has no authored home yet either;
        // the caption band has no room in `SlotTemplate` without moving layout that is Jeremy's.
        _tabStrip = GetNodeOrNull<Container>("%CategoryTabs") ?? BuildTabStripAboveTheGrid();
        EnsureSlotNameOnTemplate(_slotTemplate);

        _useButton.Pressed += OnUsePressed;
        if (_equipButton != null) _equipButton.Pressed += OnEquipPressed;
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
        if (_equipButton != null) _equipButton.Pressed -= OnEquipPressed;
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
            RefreshTabs(null);
            RefreshDetail();
            return;
        }

        // Before the grid, because it can move the filter: a tab whose last item was just spent goes
        // away, and if that was the active one the grid has to fall back to All rather than draw nothing.
        RefreshTabs(inv);

        foreach (var (itemId, count) in Ordered(inv, _activeCategory))
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
            {
                countLabel.Text = count > 1 ? count.ToString() : "";
                MakeCountLegible(countLabel);
            }

            // The name on the tile, not only in the tooltip. The first playtest went a whole session
            // carrying the Splitting Axe without knowing it — *"I didn't realize I had the axe"* — because
            // a swatch is a colour and a tooltip needs you to already suspect something is there.
            if (slot.FindChild("SlotName", true, false) is Label nameLabel)
                nameLabel.Text = inv.DisplayNameOf(itemId);

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
    /// slot. **What you carry first, the tally after** — then salves ahead of the rest within each
    /// group, because they are what you reach for under pressure, and by name within a kind.
    /// </summary>
    /// <remarks>
    /// <b>The leading sort is <see cref="InventoryCraftingManager.CountsAgainstSlots"/>, and it reverses
    /// what this method used to do.</b> The old order was the raw category int, which put
    /// <see cref="ItemCategory.Component"/> first for no better reason than that it is zero — so the
    /// satchel opened on a wall of raw material with the salves pushed past it. §19 says materials are a
    /// tally rather than an inventory, and a tally belongs at the bottom.
    /// <para>
    /// <b>This is not the strip §19 asks for, and it is deliberately short of it.</b> The strip is a row
    /// of counters that lives beside the crafting panel — and there is no crafting panel yet (the
    /// <c>Workbench</c> case in <c>InteractionService</c> still says so). Building it here would mean
    /// authoring a node into this scene and moving it the week the bench panel lands. So the predicate
    /// goes to work on the ordering now, where it needs no new node and is still assertable, and the
    /// strip is built once, in the place it belongs.
    /// </para>
    /// </remarks>
    /// <param name="only">
    /// The one kind to draw, or <c>null</c> for all of them. <b>The filter is applied before the sort and
    /// changes none of it</b> — a tab is a narrower window onto the same order, so an item does not move
    /// when you tab to it and back.
    /// </param>
    private static IEnumerable<(string ItemId, int Count)> Ordered(
        InventoryCraftingManager inv, ItemCategory? only = null) =>
        inv.Stacks
           .Where(pair => pair.Value > 0)
           .Where(pair => only == null || CategoryOf(inv, pair.Key) == only.Value)
           .OrderByDescending(pair => InventoryCraftingManager.CountsAgainstSlots(
               inv.GetItem(pair.Key)?.Category ?? ItemCategory.Component))
           .ThenBy(pair => (int)(inv.GetItem(pair.Key)?.Category ?? ItemCategory.Component))
           .ThenBy(pair => inv.DisplayNameOf(pair.Key))
           .Select(pair => (pair.Key, pair.Value));

    /// <summary>
    /// One place that answers "what kind is this", so the filter, the sort and the tab strip cannot
    /// disagree about an item whose data failed to load. Falls back the same way
    /// <see cref="Ordered"/> already did.
    /// </summary>
    private static ItemCategory CategoryOf(InventoryCraftingManager inv, string itemId) =>
        inv.GetItem(itemId)?.Category ?? ItemCategory.Component;

    // ---- The tab strip ---------------------------------------------------

    /// <summary>
    /// The player's word for each <see cref="ItemCategory"/>. <b>A translation table rather than an enum
    /// rename</b>: <see cref="ItemCategory.Relic"/> is what all three access tools are authored as, and
    /// renaming it to Tool would spend the member on tools and leave nowhere for actual story objects.
    /// Flagged in the plan as a smell worth revisiting when the Dream pockets land.
    /// </summary>
    /// <remarks>
    /// Plural, because a tab labels a shelf and not an item. <see cref="ItemCategory.Lore"/> is in the
    /// table with nothing authored to it yet — the strip only draws a tab for a kind you actually carry,
    /// so it costs nothing to have the word ready for the day unit 6 lands a Dream page.
    /// </remarks>
    private static string LabelFor(ItemCategory category) => category switch
    {
        ItemCategory.Component => "Materials",
        ItemCategory.Salve => "Salves",
        ItemCategory.Catalyst => "Catalysts",
        ItemCategory.Relic => "Tools",
        ItemCategory.Lore => "Lore",
        ItemCategory.Trap => "Traps",
        ItemCategory.Equipment => "Equipment",
        _ => category.ToString(),
    };

    /// <summary>The label on the tab that filters nothing. Named so the gates can ask for it by name.</summary>
    public const string AllTabLabel = "All";

    /// <summary>
    /// Rebuild the strip from what is carried: All, then one tab per kind present, in enum order.
    /// </summary>
    /// <remarks>
    /// <b>Only the kinds you hold get a tab.</b> Seven fixed tabs would mean six dead ones at the start of
    /// the game — you open the satchel holding an axe — and the Lore tab would stay dead for several units.
    /// Because the order is the enum's, a tab is only ever <i>inserted</i> as the game opens up; positions
    /// never reshuffle under the player's hand.
    /// <para>
    /// A tab going away can strand the filter, so the active kind is checked against the rebuilt strip and
    /// falls back to All. Without that, spending your last salve leaves you looking at an empty grid with
    /// no obvious way back.
    /// </para>
    /// </remarks>
    private void RefreshTabs(InventoryCraftingManager inv)
    {
        if (_tabStrip == null) return;

        foreach (var (_, button) in _tabs)
        {
            _tabStrip.RemoveChild(button);
            button.QueueFree();
        }
        _tabs.Clear();

        var carried = inv == null
            ? new List<ItemCategory>()
            : inv.Stacks
                 .Where(pair => pair.Value > 0)
                 .Select(pair => CategoryOf(inv, pair.Key))
                 .Distinct()
                 .OrderBy(category => (int)category)
                 .ToList();

        // The filter cannot outlive its tab.
        if (_activeCategory.HasValue && !carried.Contains(_activeCategory.Value)) _activeCategory = null;

        AddTab(null, AllTabLabel);
        foreach (var category in carried) AddTab(category, LabelFor(category));
    }

    private void AddTab(ItemCategory? category, string label)
    {
        var button = new Button
        {
            Name = $"Tab_{category?.ToString() ?? AllTabLabel}",
            Text = label,
            // Pressed-looking rather than disabled: the active tab still says what it is, and a greyed
            // button reads as "not available" instead of "you are here".
            ToggleMode = true,
            ButtonPressed = category.Equals(_activeCategory),
            FocusMode = FocusModeEnum.None,
        };

        button.Pressed += () => SelectTab(category);

        _tabStrip.AddChild(button);
        _tabs.Add((category, button));
    }

    /// <summary>
    /// Filter the grid to one kind. <b>The selection survives</b>, even when the selected item is filtered
    /// out — the detail column is about what you last looked at, and losing "(worn)" because you changed
    /// tabs would be the panel forgetting something it was told.
    /// </summary>
    private void SelectTab(ItemCategory? category)
    {
        _activeCategory = category;
        Refresh();
    }

    /// <summary>Tab selection, exposed so the gates can drive the strip without a mouse.</summary>
    public void DebugSelectTab(ItemCategory? category) => SelectTab(category);

    /// <summary>
    /// Put a tab strip in above the grid, for as long as the scene has no <c>%CategoryTabs</c> of its own.
    /// <b>Delete this when it does</b>, on <see cref="BuildEquipButtonBeside"/>'s precedent.
    /// </summary>
    /// <remarks>
    /// It walks up from <c>%SlotContainer</c> to the nearest <see cref="VBoxContainer"/> ancestor and
    /// inserts itself before the child on that path — which lands it between <c>Title</c> and
    /// <c>Columns</c> in the scene as authored, <b>without naming either</b>. Hard-coding <c>Body</c>
    /// would make this break the day Jeremy renames a node the contract never promised.
    /// </remarks>
    private Container BuildTabStripAboveTheGrid()
    {
        if (_slotContainer == null) return null;

        Node child = _slotContainer;
        while (child.GetParent() is Control and not VBoxContainer) child = child.GetParent();

        if (child.GetParent() is not VBoxContainer column) return null;

        var strip = new HBoxContainer { Name = "CategoryTabs" };
        column.AddChild(strip);
        column.MoveChild(strip, child.GetIndex());

        return strip;
    }

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

        // The same arrangement one verb over, and it is the same sentence: EquipBlock lives in the manager,
        // so a greyed button and a refused click are the same answer to one question.
        var worn = item != null && inv != null && inv.EquippedIn(item.Slot) == item.Id;

        if (_equipButton != null)
        {
            _equipButton.Disabled = item == null || inv?.CanEquip(_selectedItemId) != true;

            // Unequipping is always legal (§18: bare hands are a weak verb, not no verb), which is why the
            // one button flips its own label instead of a second button appearing beside it.
            _equipButton.Text = worn ? "Put away" : "Equip";
            if (worn) _equipButton.Disabled = false;
        }

        // Said in the detail column rather than on the slot, because worn is not a property of the stack —
        // an equipped item is still sitting in the satchel and still counts once.
        if (_itemName != null && worn) _itemName.Text += "  (worn)";

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
    /// Equip the selection, or put it away if it is already worn. Fire and forget, like
    /// <see cref="OnUsePressed"/>: the manager decides, narrates and emits the InventoryChanged that
    /// redraws this panel.
    /// </summary>
    private void OnEquipPressed()
    {
        var inv = InventoryCraftingManager.Instance;
        if (inv == null || string.IsNullOrEmpty(_selectedItemId)) return;

        var item = inv.GetItem(_selectedItemId);
        if (item == null) return;

        if (inv.EquippedIn(item.Slot) == item.Id) inv.Unequip(item.Slot);
        else inv.Equip(_selectedItemId);

        // Unequip emits InventoryChanged but the panel is open, so the rebuild is immediate — and the
        // label above has to flip whether or not the stack list moved. Cheapest correct thing.
        RefreshDetail();
    }

    /// <summary>
    /// Give the stack count a colour it can be read in, for as long as the scene sets none.
    /// <b>Guarded on <see cref="Control.HasThemeColorOverride"/>, and that is the whole point</b>: the day
    /// Jeremy sets a colour on <c>SlotCount</c> in the editor, this stops running rather than overwriting
    /// him every repaint.
    /// </summary>
    /// <remarks>
    /// <c>PanelTheme.tres</c> paints every <c>Label</c> a pale cream, and a slot's swatch colour is
    /// <i>data</i> — <see cref="ItemData.PlaceholderColor"/>, 28 different values. Graveflower's is a pale
    /// green, which is where the first playtest found this: *"the number on graveflower should be black or
    /// a dark color, you cannot see it well."*
    /// <para>
    /// Dark with a light outline rather than the plain black he asked for, because plain black is the same
    /// bug at the other end of the range — Pitch's swatch is nearly black. The outline is what makes one
    /// colour work against all 28, and it is the only reason this is code and not taste.
    /// </para>
    /// <para>
    /// <b>And the outline is why it stayed bright.</b> The first version drew it 5px wide, which on a digit
    /// two or three pixels thick means the pale halo meets itself over the stroke and swallows the dark fill —
    /// so a change to the font colour showed up on screen as no change at all, and the second playtest said so:
    /// <i>"Yeah it is still bright."</i> At 2px it separates the number from the swatch without painting over
    /// it. The lesson is general: <b>an outline wider than the stroke it surrounds is a fill</b>.
    /// </para>
    /// </remarks>
    private static void MakeCountLegible(Label count)
    {
        if (count.HasThemeColorOverride("font_color")) return;

        count.AddThemeColorOverride("font_color", new Color(0.07f, 0.07f, 0.08f));
        count.AddThemeColorOverride("font_outline_color", new Color(0.93f, 0.92f, 0.88f, 0.85f));
        count.AddThemeConstantOverride("outline_size", 2);
    }

    /// <summary>
    /// Put a name caption across the bottom of every slot, for as long as <c>SlotTemplate</c> has no
    /// <c>SlotName</c> of its own. <b>Delete this when it does</b>, on
    /// <see cref="BuildEquipButtonBeside"/>'s precedent — the line in <see cref="Refresh"/> that sets the
    /// text keeps working either way, because it finds the node by name.
    /// </summary>
    /// <remarks>
    /// Added to the template once rather than to each slot, because every slot is a
    /// <see cref="Node.Duplicate"/> of it and so inherits whatever is there.
    /// <para>
    /// <b>Corrected on 2026-08-03 by three successive looks at the same tile, and the last one is the
    /// interesting correction.</b> Version 1 stopped 48px short of the right edge so <c>SlotCount</c>'s
    /// authored box was untouched, which left 56px — about eight characters at font 13 — and cut every longer
    /// name: <i>"Cairnsto"</i>, <i>"Corrosiv"</i>, <i>"Rendere"</i>. Version 2 took the whole width and moved
    /// the count out of the way instead.
    /// </para>
    /// <para>
    /// Version 3 shortened <c>SlotSwatch</c> so the band could sit *under* the artwork rather than over it,
    /// which every earlier version of this comment called "the nicer arrangement". <b>Version 4 undid that on a
    /// fair question with a wrong answer</b> — Jeremy: <i>"I don't understand why it can't be square and have
    /// the number where it is?"</i> I said it could: the count's box had been derived from the swatch's bottom
    /// edge, so I made the swatch square and held the count's pixels still. <b>Version 5 put version 3
    /// back, and the reason is the useful part of this whole comment: the square and the corner are mutually
    /// exclusive on a 112px tile, and holding the pixels still is not the same as holding the position.</b>
    /// </para>
    /// <para>
    /// The arithmetic, so nobody re-litigates it. The caption needs the bottom <see cref="CaptionBand"/> = 40px
    /// for two lines at font 12 — that is what un-truncates the names. The count belongs
    /// <see cref="CountInset"/> in from the swatch's bottom-right *corner*. A 90×90 square swatch spans y
    /// 11..101, so its corner is at y 96 — <b>inside the caption band</b>, on the words. The number can only be
    /// in the corner if the swatch ends where the band begins. Growing the tile instead is not free either: the
    /// grid is 5 columns in a 600px frame and 28 items already make six rows, so taller tiles worsen an
    /// overflow that exists today.
    /// </para>
    /// <para>
    /// <b>What version 4 got wrong is worth more than the geometry.</b> Asked <i>"why can't it be square and
    /// have the number where it is?"</i> I made the count's box land on identical pixels and called that
    /// "where it is." It was not: relative to a swatch that now reached 40px further down, the same pixels read
    /// as floating mid-tile, and he said so — <i>"everything is right besides it being in the middle."</i>
    /// <b>A position on a tile is a relationship to the artwork, not a coordinate.</b> The check that now
    /// guards this measures the *gap* between the count and the swatch's two near edges, because that is the
    /// thing being claimed; an absolute box passed every gate while looking wrong.
    /// </para>
    /// <para>
    /// So the tile is three bands, none overlapping: swatch on top, the count inside its lower-right corner,
    /// caption in the strip below with the full width to wrap into. <b>Version 6 is version 5 with the swatch
    /// made square the only way it can be</b> — <i>"the placement is correct but it isn't square"</i> — which
    /// is by *narrowing* it to its height rather than by restoring its height. 61×61 centred in the tile, not
    /// 90×61 and not 90×90. See <see cref="SquareTheSwatchAboveTheCaption"/>; the height is the constrained
    /// dimension, so the width is the one that has to move.
    /// </para>
    /// </remarks>
    private static void EnsureSlotNameOnTemplate(Control template)
    {
        if (template == null || template.FindChild("SlotName", true, false) != null) return;

        var name = new Label
        {
            Name = "SlotName",
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            // Two lines and then an ellipsis, rather than clipping mid-word. `ClipText` is deliberately
            // left off: it forces a single line, which is what truncated the names in the first place.
            MaxLinesVisible = 2,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };

        template.AddChild(name);

        // Make room first: the caption goes *under* the swatch rather than across it, so the swatch gives up
        // its bottom band — and then gives up the matching width, because a swatch that only loses height is
        // a rectangle and *"it isn't square"* was the note. Authored offsets, moved only from in here: the
        // builder does not run once the scene has a caption of its own, so authoring one restores all four.
        if (template.FindChild("SlotSwatch", true, false) is Control art)
            SquareTheSwatchAboveTheCaption(art, TileSide(template));

        // Bottom strip, anchored so it follows the tile if the scene resizes it. Full width: two lines of
        // 100px at font 12 hold every authored display name. It sits in the band the swatch just vacated,
        // so nothing overlaps the artwork or the number.
        name.SetAnchorsPreset(LayoutPreset.BottomWide);
        name.OffsetLeft = 6f;
        name.OffsetTop = -CaptionBand;
        name.OffsetRight = -6f;
        name.OffsetBottom = -4f;

        name.AddThemeFontSizeOverride("font_size", 12);
        name.AddThemeColorOverride("font_color", new Color(0.97f, 0.96f, 0.93f));
        name.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        name.AddThemeConstantOverride("outline_size", 6);

        NudgeCountOntoTheSwatch(template);
    }

    /// <summary>
    /// The tile's side in pixels. <c>SlotTemplate</c> is sized by <c>custom_minimum_size</c> rather than by
    /// offsets, so <see cref="Control.Size"/> reads zero until the container has laid it out — which is
    /// exactly when the builder runs. Falls back to the authored constant in that case.
    /// </summary>
    private static float TileSide(Control template) =>
        template.Size.X > 0f ? template.Size.X : SlotTemplateSide;

    /// <summary>
    /// Shrink <c>SlotSwatch</c> to the largest square that fits above the caption band, centred across the
    /// tile. <b>Called only from <see cref="EnsureSlotNameOnTemplate"/></b>, so authoring a real caption node
    /// restores every authored offset in the same stroke.
    /// </summary>
    /// <remarks>
    /// <i>"The placement is correct but it isn't square."</i> The caption fixes the swatch's bottom at
    /// <see cref="CaptionBand"/> up from the tile's edge, and the swatch's top is the inset the scene
    /// authored — so <b>the height is not free, and squaring the swatch means the width follows the
    /// height</b>, not the reverse. On the authored 112px tile that is 61×61 rather than 90×61: the same
    /// height now on screen, with the extra width given back as margin. It cannot be 90×90 —
    /// see <see cref="EnsureSlotNameOnTemplate"/>'s remarks for why the caption leaves no room for that.
    /// <para>
    /// The top offset is read rather than assumed, so shrinking the inset in the editor grows the icon: at
    /// top 6 the square is 66, at top 0 it is 72. The horizontal margin is then split evenly and rounded, so
    /// the swatch lands on whole pixels and stays crisp.
    /// </para>
    /// </remarks>
    private static void SquareTheSwatchAboveTheCaption(Control art, float side)
    {
        var top = art.AnchorTop * side + art.OffsetTop;
        var square = side - CaptionBand - top;
        if (square <= 0f) return;

        var left = Mathf.Round((side - square) * 0.5f);

        art.OffsetLeft = left - art.AnchorLeft * side;
        art.OffsetRight = left + square - art.AnchorRight * side;
        art.OffsetBottom = -CaptionBand;
    }

    /// <summary>
    /// Move <c>SlotCount</c> inside <c>SlotSwatch</c>'s bottom-right corner, a few pixels in from both
    /// edges. <b>Called only from <see cref="EnsureSlotNameOnTemplate"/></b>, so it stops the day the scene
    /// authors its own caption and the authored offsets come back.
    /// </summary>
    /// <remarks>
    /// From the second playtest: <i>"The number of items needs to move left a tiny bit to be on the icon
    /// where it should be."</i> It was three pixels past the swatch's right edge and five past its bottom,
    /// because the authored box is anchored to the <i>tile</i> and the swatch is inset 11px inside it.
    /// <para>
    /// <b>Three corners in three rounds, and the third is the one he asked for twice.</b> It went to the
    /// swatch's top-right (mine), then its top-left (<i>"top left is probably better for the number"</i>),
    /// and then back down: <i>"I think bottom right. And it should be moved into the slot a few pixels."</i>
    /// Bottom-right is where the count started, and the only reason it ever left is that the caption band
    /// crosses the swatch's own bottom-right corner. <b>"Into the slot" is the
    /// <see cref="CountInset"/></b>: the number sits inside the artwork rather than on its edge.
    /// </para>
    /// <para>
    /// <b>Both edges come from the swatch</b>, and the round that derived the bottom one from
    /// <see cref="CaptionBand"/> instead is the reason that is stated so firmly. The two are the same pixels
    /// while the swatch ends where the band begins, so the substitution looked free and passed every gate —
    /// and then the swatch went back to full height and the number, unmoved, read as
    /// <i>"in the middle."</i> <b>The count's position is a relationship to the artwork's corner; derive it
    /// from anything else and it is only accidentally right.</b> See
    /// <see cref="EnsureSlotNameOnTemplate"/>'s remarks.
    /// </para>
    /// <para>
    /// The right edge reads the swatch's own geometry rather than hardcoding 11px, so shrinking the inset in
    /// the editor carries the number with it. It needs the tile's width to resolve, because the swatch is
    /// full-rect anchored and that offset is measured from the far side.
    /// </para>
    /// </remarks>
    private static void NudgeCountOntoTheSwatch(Control template)
    {
        if (template.FindChild("SlotCount", true, false) is not Label count) return;
        if (template.FindChild("SlotSwatch", true, false) is not Control swatch) return;

        var side = TileSide(template);
        var right = swatch.AnchorRight * side + swatch.OffsetRight;

        // Anchored to the tile's bottom-right in the scene, which is 11px outside the artwork; re-anchor to
        // the top-left so the offsets below are absolute tile pixels and can be aimed at the swatch instead.
        count.SetAnchorsPreset(LayoutPreset.TopLeft);
        count.OffsetRight = right - CountInset;
        count.OffsetLeft = count.OffsetRight - 36f;
        // Measured from the swatch's own bottom, not from the caption band's top. Those are the same pixels
        // here because the swatch was just shortened to the band — but the swatch is what the eye reads the
        // number's position against, so the swatch is what it must be derived from. See the remarks.
        count.OffsetBottom = swatch.AnchorBottom * side + swatch.OffsetBottom - CountInset;
        count.OffsetTop = count.OffsetBottom - 26f;

        // Right-aligned and bottom-aligned, so the digits sit in the corner of the box rather than floating
        // at the far side of one wide enough for three of them.
        count.HorizontalAlignment = HorizontalAlignment.Right;
        count.VerticalAlignment = VerticalAlignment.Bottom;
    }

    /// <summary>
    /// Put an Equip button in beside Use, for as long as <c>InventoryPanel.tscn</c> does not have one.
    /// <b>Delete this when it does.</b>
    /// </summary>
    private static Button BuildEquipButtonBeside(Button useButton)
    {
        var parent = useButton?.GetParent();
        if (parent == null) return null;

        var button = new Button { Name = "EquipButton", Text = "Equip", Disabled = true };
        parent.AddChild(button);
        parent.MoveChild(button, useButton.GetIndex() + 1);

        return button;
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
