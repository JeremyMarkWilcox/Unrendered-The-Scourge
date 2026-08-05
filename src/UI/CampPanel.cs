using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// The camp menu — unit 5, built to docs/combat.md §17 as amended 2026-08-01. One row per camp you have
/// stood in; pressing a row carries you there.
///
/// <para><b>A row is the verb, not a selection.</b> There is no *Travel* button beside the list, and that
/// is §17's own rule applied rather than a shortcut: <i>if the player would never decline, do not ask.</i>
/// Pick a destination and then confirm it and the second click is a confirmation step with one possible
/// answer — the same thing the section deleted the rest-button and the return-point picker for. So this
/// panel is <see cref="FightPanel"/>-shaped (each control is an act) rather than
/// <see cref="InventoryPanel"/>-shaped (select, then act), because a satchel slot has four things you
/// might do with it and a camp has one.</para>
///
/// <para><b>It decides nothing.</b> Which camps exist, which are known and whether you may leave are all
/// <see cref="CampManager"/>'s answers, and the refusal text comes from <see cref="CampManager.Words"/> so
/// there is one vocabulary rather than a copy over here — handoff, "Who does what", and the reason
/// <c>--verify-slice</c> can test the whole verb without this class in the tree.</para>
///
/// <para><b>No authored scene yet, so it builds its own</b> — <see cref="FightPanel"/>'s arrangement
/// exactly. <see cref="BuildFallback"/> puts up the published contract in code, so the verb is playable
/// today; authoring <c>scenes/ui/CampPanel.tscn</c> against the same node names is a drop-in and deletes
/// the builder, not a branch in this class. The contract is <c>CampList</c>, <c>CampTemplate</c>,
/// <c>CloseButton</c>, and optionally <c>Heading</c> and <c>Dim</c>.</para>
/// </summary>
public partial class CampPanel : Control
{
    private Container _campList;
    private Button _campTemplate;
    private Button _closeButton;
    private Label _heading;
    private ColorRect _dim;

    private readonly List<Button> _rows = new();

    /// <summary>The theme the other panels carry, so this one looks like them.</summary>
    private const string PanelThemePath = "res://scenes/ui/PanelTheme.tres";

    // ---- Readouts, for the gates -----------------------------------------

    public bool IsOpen => Visible;

    /// <summary>How many camps the menu is offering. Rows drawn, not camps known.</summary>
    public int RowCount => _rows.Count;

    /// <summary>The text of a row as drawn, or "" out of range.</summary>
    public string RowLabelAt(int index) =>
        index >= 0 && index < _rows.Count ? _rows[index].Text : "";

    /// <summary>
    /// Whether that row can be pressed. The camp you are standing in draws disabled rather than absent —
    /// <see cref="TravelBlock.AlreadyHere"/> is not something withheld, and a list that hid your own camp
    /// would make the count change as you walked.
    /// </summary>
    public bool RowEnabledAt(int index) =>
        index >= 0 && index < _rows.Count && !_rows[index].Disabled;

    /// <summary>The heading line, or "". Optional in the contract, so this may be "" by design.</summary>
    public string HeadingText => _heading?.Text ?? "";

    /// <summary>
    /// Whether a tint is actually drawn over the room, and at a rect that covers it.
    /// <see cref="FightPanel.DimsTheRoom"/> copied field for field, including the part that looks like
    /// timidity: it asks about <b>anchors and offsets</b> rather than <c>Size</c>, because a headless run
    /// never lays anything out and <c>Size</c> would answer 0 for a correct panel. The arrangement is what
    /// can be checked here, and the lesson from the eighth blind-spot class is that when you settle for the
    /// arrangement you check <i>all</i> of it — one anchor asserted is how a 0×0 dim passed for a whole unit.
    /// </summary>
    public bool DimsTheRoom =>
        _dim is { Visible: true } &&
        _dim.Color.A >= 0.4f &&
        Mathf.IsEqualApprox(_dim.AnchorRight, 1f) &&
        Mathf.IsEqualApprox(_dim.AnchorBottom, 1f) &&
        Mathf.IsZeroApprox(_dim.OffsetLeft) && Mathf.IsZeroApprox(_dim.OffsetTop) &&
        Mathf.IsZeroApprox(_dim.OffsetRight) && Mathf.IsZeroApprox(_dim.OffsetBottom);

    /// <summary>
    /// Whether the panel's own root covers the screen and eats clicks, so the world underneath cannot be
    /// interacted with while the menu is up.
    /// </summary>
    /// <remarks>
    /// <b>Added while chasing a bug found in play, and it is the eighth blind-spot class's exact shape.</b>
    /// If this were false the menu would only block its own centred frame, a creature hotspot beside it would
    /// still be clickable, and a fight could start with the menu open — which is what the report looked
    /// like. Asserted the same way <see cref="DimsTheRoom"/> is, on anchors and offsets rather than
    /// <c>Size</c>, because a headless run lays nothing out.
    /// </remarks>
    public bool BlocksTheRoom =>
        MouseFilter == MouseFilterEnum.Stop &&
        Mathf.IsEqualApprox(AnchorRight, 1f) && Mathf.IsEqualApprox(AnchorBottom, 1f) &&
        Mathf.IsZeroApprox(OffsetLeft) && Mathf.IsZeroApprox(OffsetTop) &&
        Mathf.IsZeroApprox(OffsetRight) && Mathf.IsZeroApprox(OffsetBottom);

    /// <summary>
    /// Whether the tint draws <i>behind</i> the rows rather than over them — <see cref="FightPanel"/>'s
    /// <c>DimDrawsBehindTheVerbs</c>, and the same failure that looks like the feature: a dim in front of
    /// the list still darkens the room, still passes <see cref="DimsTheRoom"/>, and greys out every row you
    /// need to press.
    /// </summary>
    public bool DimDrawsBehindTheRows
    {
        get
        {
            if (_dim == null || _campList == null) return false;

            Node branch = _campList;
            while (branch.GetParent() != null && branch.GetParent() != this) branch = branch.GetParent();

            return branch.GetParent() == this && _dim.GetIndex() < branch.GetIndex();
        }
    }

    public override void _Ready()
    {
        _campList = Bind<Container>("CampList");
        _campTemplate = Bind<Button>("CampTemplate");
        _closeButton = Bind<Button>("CloseButton");
        _heading = Bind<Label>("Heading", optional: true);
        _dim = Bind<ColorRect>("Dim", optional: true) ?? BuildDim();

        if (_campTemplate != null) _campTemplate.Visible = false;
        if (_closeButton != null) _closeButton.Pressed += Close;

        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_closeButton != null) _closeButton.Pressed -= Close;
    }

    /// <summary>
    /// Find a contract node whether the tree came from a <c>.tscn</c> or from
    /// <see cref="BuildFallback"/>. Copied deliberately from <see cref="FightPanel"/> rather than shared:
    /// the two panels are the only ones with a code fallback, and this one's builder is scheduled for
    /// deletion, so a base class would outlive its only reason to exist.
    /// </summary>
    private T Bind<T>(string name, bool optional = false) where T : Node
    {
        var byUnique = GetNodeOrNull<T>($"%{name}");
        if (byUnique != null) return byUnique;

        var found = FindChild(name, true, false);
        if (found is T typed) return typed;

        if (!optional) GD.PushError($"[CampPanel] contract node '{name}' is missing.");
        return null;
    }

    // ---- Opening and closing ---------------------------------------------

    // `Toggle()` stood here until 2026-08-04 and was deleted the day it lost its caller. `Main` now goes
    // through `Hud.ToggleCampMenu`, which also shuts the satchel — so a `Toggle` on the panel would be a
    // second way in that skips that, and the only thing worse than dead API is dead API with a subtly
    // different contract. Same reasoning that deleted `ZoneData` for having no reader.

    /// <summary>
    /// Put the menu up, and refuse with a line when there is no leaving from here. Returns whether it
    /// opened.
    /// </summary>
    /// <remarks>
    /// <b>The refusal happens here rather than on the row</b>, which is <see cref="FightPanel.Open"/>'s
    /// argument: a screen offering nothing but dead controls is worse than no screen, and mid-fight every
    /// row would be dead for one reason that belongs in a sentence. <see cref="CampManager.TravelToCamp"/>
    /// refuses independently, so this is a courtesy and not the enforcement — press F5, load, or reach a row
    /// some other way and the verb still says no.
    /// </remarks>
    public bool Open()
    {
        var camp = CampManager.Instance;
        if (camp == null) return false;

        var departure = camp.InspectDeparture();
        if (departure != TravelBlock.None)
        {
            EventBus.Instance?.EmitNotify(CampManager.Words(departure));
            return false;
        }

        Rebuild();
        Visible = true;
        return true;
    }

    public void Close() => Visible = false;

    // ---- Painting --------------------------------------------------------

    private void Rebuild()
    {
        // Removed *then* freed, on `FightPanel.RebuildVerbs`'s precedent: `QueueFree` alone defers to the
        // end of the frame, and a headless run has no end of frame — so the old rows would still be
        // children of the list on the next open, and only `_rows` would know they were gone.
        foreach (var row in _rows)
        {
            _campList?.RemoveChild(row);
            row.QueueFree();
        }
        _rows.Clear();

        var camp = CampManager.Instance;
        var nav = NavigationManager.Instance;
        if (camp == null || _campList == null || _campTemplate == null) return;

        var known = CampManager.KnownCamps;

        foreach (var roomId in known)
        {
            var room = nav?.GetRoom(roomId);
            var verdict = camp.InspectTravel(roomId);

            var name = string.IsNullOrEmpty(room?.DisplayName) ? roomId : room.DisplayName;
            var chapter = string.IsNullOrEmpty(room?.ChapterId) ? "" : $"  ·  {room.ChapterId}";

            // The reason rides on the row rather than being implied by the greying, on `CraftBlock`'s
            // rule: the panel says *which kind of no* it is. Here there is only ever one — you are
            // standing in it — and it reads as a statement rather than a refusal.
            var why = verdict == TravelBlock.None ? "" : $"  ·  {CampManager.Words(verdict)}";

            var button = (Button)_campTemplate.Duplicate();
            button.Name = $"Camp_{roomId}";
            button.Text = $"{name}{chapter}{why}";
            button.Visible = true;
            button.Disabled = verdict != TravelBlock.None;

            // Captured by value; the row is rebuilt on every open, so nothing here outlives its room id.
            var destination = roomId;
            button.Pressed += () => OnRowPressed(destination);

            _campList.AddChild(button);
            _rows.Add(button);
        }

        if (_heading != null)
            _heading.Text = known.Count == 1
                ? "One camp. You have not been anywhere else."
                : $"{known.Count} camps.";
    }

    /// <summary>
    /// Travel, and take the menu down if it happened. Left up on a refusal so the reason is readable
    /// against the thing that refused.
    /// </summary>
    private void OnRowPressed(string roomId)
    {
        if (CampManager.Instance?.TravelToCamp(roomId) == true) Close();
        else Rebuild();
    }

    // ---- Debug and gates -------------------------------------------------

    /// <summary>
    /// Press a row by index. For the gates: a `Pressed` signal connected from C# is not introspectable
    /// from C#, so the only honest way to assert the wiring is to fire it — the lesson unit 3a's re-read
    /// wrote down about the satchel's repaint.
    /// </summary>
    public bool DebugPressRow(int index)
    {
        if (index < 0 || index >= _rows.Count || _rows[index].Disabled) return false;

        _rows[index].EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    /// <summary>Press the first row whose text contains <paramref name="fragment"/>.</summary>
    public bool DebugPressRowNamed(string fragment)
    {
        for (var i = 0; i < _rows.Count; i++)
            if (_rows[i].Text.Contains(fragment)) return DebugPressRow(i);

        return false;
    }

    // ---- The fallback tree -----------------------------------------------

    /// <summary>
    /// The published contract, built in code because no <c>.tscn</c> exists yet. <b>Delete this the day
    /// <c>scenes/ui/CampPanel.tscn</c> lands</b>; the node names here are exactly what that scene must
    /// carry, marked unique-in-owner.
    /// </summary>
    public static CampPanel BuildFallback()
    {
        var panel = new CampPanel
        {
            Name = "CampPanel",
            MouseFilter = MouseFilterEnum.Stop,
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.Theme = ResourceLoader.Load<Theme>(PanelThemePath);

        var backing = new PanelContainer { Name = "Backing" };
        backing.SetAnchorsPreset(LayoutPreset.Center);
        backing.OffsetLeft = -320f;
        backing.OffsetTop = -180f;
        backing.OffsetRight = 320f;
        backing.OffsetBottom = 180f;
        panel.AddChild(backing);

        var column = new VBoxContainer { Name = "Column" };
        backing.AddChild(column);

        column.AddChild(new Label { Name = "Heading", Text = "" });

        var list = new VBoxContainer { Name = "CampList" };
        column.AddChild(list);

        // Inside the container it fills, so a duplicate lands beside its siblings with no re-parenting —
        // `FightPanel`'s VerbTemplate, same reason.
        list.AddChild(new Button
        {
            Name = "CampTemplate",
            Text = "",
            CustomMinimumSize = new Vector2(560f, 0f),
        });

        column.AddChild(new Button { Name = "CloseButton", Text = "✕" });

        return panel;
    }

    /// <summary>
    /// Tint the room while the menu is up. <b>Delete when the <c>.tscn</c> authors a <c>Dim</c></b> —
    /// <c>InventoryPanel.tscn</c> has one at this exact 55% black, and copying its value rather than
    /// choosing a new one is the point.
    /// </summary>
    private ColorRect BuildDim()
    {
        var dim = new ColorRect
        {
            Name = "Dim",
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = MouseFilterEnum.Ignore,
        };

        AddChild(dim);

        // Anchors **and** offsets. `SetAnchorsPreset` alone preserves the node's current rect — 0×0 for a
        // ColorRect one line old — which is the eighth blind-spot class in handoff §3, found in
        // `FightPanel` after the dim had sat invisible for a whole unit behind four green checks.
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // First, so the rows draw over it rather than through it.
        MoveChild(dim, 0);

        return dim;
    }
}
