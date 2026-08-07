using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// The journal — unit 7a, built to <c>build-plan.md</c> §7 decision 9 (2026-08-02). Jeremy: *"The journal
/// should just be the crafting station and the herblore station where you create them."*
///
/// <para><b>It is not a lore reader with a crafting tab bolted on.</b> It is one book with two sides — his
/// Craft and her Physick — and each side is where you make that side's things. That is why there is no
/// separate crafting screen and never was one: decision 9 deleted a screen rather than adding one, and
/// <see cref="KnowledgeTrack"/> is what draws the divider instead of being metadata nobody looks at.</para>
///
/// <para><b>A row is the verb</b>, on <see cref="CampPanel"/>'s reasoning and for the same reason: there is
/// exactly one thing you might do with a recipe, so a *Make* button beside a selected row would be a
/// confirmation step with one possible answer. Pressing the page makes the thing. Rows you cannot press
/// carry the reason on them, which is <see cref="CraftBlock"/>'s whole purpose — the panel says *which kind
/// of no* it is.</para>
///
/// <para><b>What you have not learned is not here at all.</b> Not greyed, not listed as unknown — absent.
/// <see cref="CraftBlock.Unknown"/>'s own doc says *"do not list the inputs — you do not know them"*, and
/// the fiction agrees: Daniel writes these pages himself once the Dream gives the craft back (decision 11),
/// so a page he has not written is not in the book. It also means the journal cannot be read as a checklist
/// of content you have yet to find, which a greyed row would make it.</para>
///
/// <para><b>It decides nothing.</b> Whether a recipe is known, whether this is the place for it and whether
/// you can afford it are all <see cref="InventoryCraftingManager.Inspect"/>'s answers, and the refusal text
/// comes from <see cref="InventoryCraftingManager.Words(CraftBlock)"/> so there is one vocabulary rather
/// than a copy over here. <b>Including the venue</b>: this panel is handed a station id and deliberately
/// never checks it, because <c>Inspect</c> reads the room itself and the camp-bench-satisfies-every-station
/// ruling must not get a second copy somewhere it can disagree. The id is a caption.</para>
///
/// <para><b>No authored scene yet, so it builds its own</b> — <see cref="CampPanel"/>'s arrangement
/// exactly. <see cref="BuildFallback"/> puts up the published contract in code, so the bench does something
/// today; authoring <c>scenes/ui/CraftingPanel.tscn</c> against the same node names is a drop-in and deletes
/// the builder, not a branch in this class. The contract is <c>RecipeList</c>, <c>RecipeTemplate</c>,
/// <c>CraftTab</c>, <c>PhysickTab</c>, <c>CloseButton</c>, and optionally <c>Heading</c> and <c>Dim</c>.</para>
/// </summary>
public partial class CraftingPanel : Control
{
    private Container _recipeList;
    private Button _recipeTemplate;
    private Button _craftTab;
    private Button _physickTab;
    private Button _closeButton;
    private Label _heading;
    private ColorRect _dim;

    private readonly List<(string RecipeId, Button Button)> _rows = new();

    /// <summary>Which side is open. Never null-valued; there are two sides and there will never be three.</summary>
    private KnowledgeTrack _side = KnowledgeTrack.Craft;

    /// <summary>The station that was clicked, for the caption only. See the class remarks.</summary>
    private string _stationId = "";

    /// <summary>The theme the other panels carry, so this one looks like them.</summary>
    private const string PanelThemePath = "res://scenes/ui/PanelTheme.tres";

    // ---- Readouts, for the gates -----------------------------------------

    public bool IsOpen => Visible;

    /// <summary>Which half of the book is open. For the gates.</summary>
    public KnowledgeTrack ActiveSide => _side;

    /// <summary>
    /// The label on the tab currently drawn as pressed — read off the button rather than mapped again
    /// from <see cref="_side"/>, on <see cref="InventoryPanel.ActiveTabLabel"/>'s reasoning: the gate
    /// should prove what is on screen and not that the table agrees with itself.
    /// </summary>
    public string ActiveSideLabel =>
        _craftTab is { ButtonPressed: true } ? _craftTab.Text
        : _physickTab is { ButtonPressed: true } ? _physickTab.Text
        : "";

    /// <summary>Which station the caption is naming, or "". For the gates.</summary>
    public string VenueStationId => _stationId;

    /// <summary>How many pages this side is showing. Rows drawn, not recipes registered.</summary>
    public int RowCount => _rows.Count;

    /// <summary>The text of a row as drawn, or "" out of range.</summary>
    public string RowLabelAt(int index) =>
        index >= 0 && index < _rows.Count ? _rows[index].Button.Text : "";

    /// <summary>
    /// Whether that row can be pressed. A page you cannot make right now draws disabled rather than
    /// absent — unlike an unlearned one, which is not drawn at all. The distinction is the panel's whole
    /// job: *you know how and cannot* is a different sentence from *you do not know how*.
    /// </summary>
    public bool RowEnabledAt(int index) =>
        index >= 0 && index < _rows.Count && !_rows[index].Button.Disabled;

    /// <summary>The recipe ids on show, in draw order. For the gates.</summary>
    public IEnumerable<string> RowRecipeIds => _rows.Select(r => r.RecipeId);

    /// <summary>The heading line, or "". Optional in the contract, so this may be "" by design.</summary>
    public string HeadingText => _heading?.Text ?? "";

    /// <summary>
    /// The height the book <i>demands</i>, in pixels — the combined minimum size of the outermost sized
    /// container. <b>The one layout fact a headless run can honestly answer</b>, and the readout that holds the
    /// 2026-08-05 softlock shut.
    /// </summary>
    /// <remarks>
    /// <c>Size</c> is 0 without a layout pass, which is why every other readout on this class asks about anchors
    /// and offsets instead — and why the overflow that made ✕ unreachable was invisible to all of them.
    /// <c>GetCombinedMinimumSize</c> is different: it is *computed* from children rather than assigned by a
    /// layout pass, so it is meaningful with no viewport at all.
    /// <para>
    /// The gate does not compare it to a number. It reads it with an empty book and again with a full one and
    /// requires the two to be equal — **a demand that grows with content is the defect**, whatever the pixel
    /// value happens to be. That survives Jeremy restyling the panel, which a bound like *"under 480"* would not.
    /// Returns 0 when nothing is bound, so a broken panel cannot pass by being absent.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b>Measured on the container that holds ✕, which is the symptom rather than a proxy for it.</b> The first
    /// version of this walked up to the outermost <c>Control</c> — the full-rect root — whose combined minimum
    /// size ignores its children entirely, so it would have answered ~0 forever and the check would have been
    /// vacuous in the *"asserts that a creature which cannot hit anything did not hit anything"* sense. The
    /// defect was **the close button being pushed out of the box it shares with the list**, so the box they
    /// share is the thing to measure, and it works for any authored scene that arranges them that way.
    /// </remarks>
    public float DemandedHeight =>
        _closeButton?.GetParent() is Control box ? box.GetCombinedMinimumSize().Y : 0f;

    /// <summary>
    /// The width the book demands, the twin of <see cref="DemandedHeight"/> and added the same day for the
    /// same defect on the other axis.
    /// </summary>
    /// <remarks>
    /// <b>The height fix shipped with no width check and the identical bug was sitting beside it.</b> A row's
    /// text is one line — *name · inputs · why-not* — and a <c>Button</c>'s minimum width includes its text, so
    /// the exorcism engine's row (three inputs plus *"One side of the book is still blank. This needs both
    /// hands"*) pushed the column's minimum past the 800 the offsets ask for. Minimums beat offsets, the panel
    /// grew rightwards off the screen, and because it grows from an anchor-derived top-left it also **stopped
    /// looking centred** — which is how Jeremy reported it: *"seems like the crafting menu should be more
    /// centered?"* It was centred; it was too wide.
    /// <para>
    /// The lesson is narrower than "check both axes": <b>a fix that constrains one dimension should be gated on
    /// the dimension it did not constrain</b>, because the reason the first one broke — content-driven minimum
    /// size — is axis-agnostic and was still live.
    /// </para>
    /// </remarks>
    public float DemandedWidth =>
        _closeButton?.GetParent() is Control box ? box.GetCombinedMinimumSize().X : 0f;

    /// <summary>
    /// Whether a tint is actually drawn over the room, at a rect that covers it.
    /// <see cref="CampPanel.DimsTheRoom"/> field for field, including the part that looks like timidity:
    /// it asks about <b>anchors and offsets</b> rather than <c>Size</c>, because a headless run never lays
    /// anything out and <c>Size</c> would answer 0 for a correct panel.
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
    /// interacted with while the book is open. <see cref="CampPanel.BlocksTheRoom"/>, which was added
    /// while chasing a bug found in play.
    /// </summary>
    public bool BlocksTheRoom =>
        MouseFilter == MouseFilterEnum.Stop &&
        Mathf.IsEqualApprox(AnchorRight, 1f) && Mathf.IsEqualApprox(AnchorBottom, 1f) &&
        Mathf.IsZeroApprox(OffsetLeft) && Mathf.IsZeroApprox(OffsetTop) &&
        Mathf.IsZeroApprox(OffsetRight) && Mathf.IsZeroApprox(OffsetBottom);

    /// <summary>
    /// Whether the tint draws <i>behind</i> the rows rather than over them — the same failure that looks
    /// like the feature, and the reason <see cref="CampPanel.DimDrawsBehindTheRows"/> exists: a dim in
    /// front of the list still darkens the room, still passes <see cref="DimsTheRoom"/>, and greys out
    /// every page you need to press.
    /// </summary>
    public bool DimDrawsBehindTheRows
    {
        get
        {
            if (_dim == null || _recipeList == null) return false;

            Node branch = _recipeList;
            while (branch.GetParent() != null && branch.GetParent() != this) branch = branch.GetParent();

            return branch.GetParent() == this && _dim.GetIndex() < branch.GetIndex();
        }
    }

    public override void _Ready()
    {
        _recipeList = Bind<Container>("RecipeList");
        _recipeTemplate = Bind<Button>("RecipeTemplate");
        _craftTab = Bind<Button>("CraftTab");
        _physickTab = Bind<Button>("PhysickTab");
        _closeButton = Bind<Button>("CloseButton");
        _heading = Bind<Label>("Heading", optional: true);
        _dim = Bind<ColorRect>("Dim", optional: true) ?? BuildDim();

        if (_recipeTemplate != null) _recipeTemplate.Visible = false;
        if (_closeButton != null) _closeButton.Pressed += Close;

        // Pressed-looking rather than disabled, on `InventoryPanel.AddTab`'s note: the open side still says
        // what it is, and a greyed button reads as "not available" instead of "you are here".
        if (_craftTab != null)
        {
            _craftTab.ToggleMode = true;
            _craftTab.FocusMode = FocusModeEnum.None;
            _craftTab.Pressed += OnCraftTabPressed;
        }

        if (_physickTab != null)
        {
            _physickTab.ToggleMode = true;
            _physickTab.FocusMode = FocusModeEnum.None;
            _physickTab.Pressed += OnPhysickTabPressed;
        }

        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_closeButton != null) _closeButton.Pressed -= Close;
        if (_craftTab != null) _craftTab.Pressed -= OnCraftTabPressed;
        if (_physickTab != null) _physickTab.Pressed -= OnPhysickTabPressed;
    }

    /// <summary>
    /// Find a contract node whether the tree came from a <c>.tscn</c> or from
    /// <see cref="BuildFallback"/>. Copied from <see cref="CampPanel"/> rather than shared, for the reason
    /// it copied <see cref="FightPanel"/>'s: every builder here is scheduled for deletion, so a base class
    /// would outlive its only reason to exist.
    /// </summary>
    private T Bind<T>(string name, bool optional = false) where T : Node
    {
        var byUnique = GetNodeOrNull<T>($"%{name}");
        if (byUnique != null) return byUnique;

        var found = FindChild(name, true, false);
        if (found is T typed) return typed;

        if (!optional) GD.PushError($"[CraftingPanel] contract node '{name}' is missing.");
        return null;
    }

    // ---- Opening and closing ---------------------------------------------

    /// <summary>
    /// Open the book at a station. Returns whether it opened.
    /// </summary>
    /// <remarks>
    /// <b>It never refuses, and that is a decision rather than an omission.</b> <see cref="CampPanel.Open"/>
    /// refuses when every row would be dead for one reason that belongs in a sentence, and the temptation
    /// here is to copy it — but there is no such reason. A venue block is per-recipe and per-station
    /// (<see cref="InventoryCraftingManager.AtVenueFor"/>), so *some* rows can be dead while others are
    /// live, and the panel is where you find out which. Adding a refusal would mean re-deriving the venue
    /// rule up here to decide whether to open, which is the second copy the class remarks refuse.
    /// <para>
    /// Reading the journal where nothing can be made is also a thing you should be allowed to do: it is
    /// how you learn what to go and gather. The satchel's Lore tab answers that away from a bench
    /// (decision 11); this answers it in front of one.
    /// </para>
    /// </remarks>
    public bool Open(string stationId)
    {
        _stationId = stationId ?? "";
        _side = SideToOpenOn();

        Rebuild();
        Visible = true;
        return true;
    }

    public void Close() => Visible = false;

    /// <summary>
    /// Which half to open at. <b>His first, hers when his has nothing to offer here.</b>
    /// </summary>
    /// <remarks>
    /// This is decision 9's *"a field station is this panel with one side open"* reduced to the smallest
    /// rule that does the job. A mortar has only her recipes, an anvil only his, and asking *is anything
    /// on this side makeable where I am standing* answers both without the panel learning what a mortar is
    /// — which it must not, since that would be the venue rule copied.
    /// <para>
    /// <b>It also answers correctly on the first bench of the game</b>, which is the case that made the
    /// rule worth having: Clearmind is the one recipe known from the start and it is hers, so his side is
    /// empty and opening on Craft would show a blank book to a player who does have something to brew.
    /// </para>
    /// <para>
    /// Recomputed on every open rather than remembered. A remembered side would be right about the last
    /// bench and wrong about this one, and "which page was I on" is not state a journal you carry needs to
    /// keep — you are not mid-sentence.
    /// </para>
    /// </remarks>
    private KnowledgeTrack SideToOpenOn() =>
        AnythingMakeableOn(KnowledgeTrack.Craft) ? KnowledgeTrack.Craft
        : AnythingMakeableOn(KnowledgeTrack.Physick) ? KnowledgeTrack.Physick
        : KnowledgeTrack.Craft;

    private static bool AnythingMakeableOn(KnowledgeTrack side)
    {
        var inv = InventoryCraftingManager.Instance;
        if (inv == null) return false;

        return inv.Recipes.Values.Any(recipe =>
            recipe != null &&
            inv.BelongsToSide(recipe, side) &&
            inv.Inspect(recipe.Id) == CraftBlock.None);
    }

    // ---- Painting --------------------------------------------------------

    private void Rebuild()
    {
        // Removed *then* freed, on `CampPanel.Rebuild`'s precedent: `QueueFree` alone defers to the end of
        // the frame, and a headless run has no end of frame — so the old rows would still be children of
        // the list on the next open, and only `_rows` would know they were gone.
        foreach (var (_, button) in _rows)
        {
            _recipeList?.RemoveChild(button);
            button.QueueFree();
        }
        _rows.Clear();

        if (_craftTab != null) _craftTab.ButtonPressed = _side == KnowledgeTrack.Craft;
        if (_physickTab != null) _physickTab.ButtonPressed = _side == KnowledgeTrack.Physick;

        var inv = InventoryCraftingManager.Instance;
        if (inv == null || _recipeList == null || _recipeTemplate == null) return;

        // Ordered by id so the book reads the same way twice. `Recipes` is a dictionary and its order is
        // an implementation detail — a page that moved between two openings because a flag was written
        // elsewhere would be the panel appearing to shuffle itself.
        foreach (var recipe in inv.Recipes.Values.Where(r => r != null).OrderBy(r => r.Id))
        {
            if (!inv.BelongsToSide(recipe, _side)) continue;

            var verdict = inv.Inspect(recipe.Id);

            // Absent, not greyed. See the class remarks — an unlearned page is not in the book.
            if (verdict == CraftBlock.Unknown) continue;

            var button = (Button)_recipeTemplate.Duplicate();
            button.Name = $"Page_{recipe.Id}";
            button.Text = RowText(inv, recipe, verdict);
            button.Visible = true;
            button.Disabled = verdict != CraftBlock.None;

            // Captured by value; rows are rebuilt on every open and every craft, so nothing here
            // outlives its recipe id.
            var made = recipe.Id;
            button.Pressed += () => OnRowPressed(made);

            _recipeList.AddChild(button);
            _rows.Add((recipe.Id, button));
        }

        if (_heading != null) _heading.Text = HeadingFor(_rows.Count);
    }

    /// <summary>
    /// One page as a line: what it makes, what it costs, and why not if not.
    /// </summary>
    /// <remarks>
    /// <b>The inputs are drawn whether or not you have them</b>, which is decision 9's *"the 'Deadfall —
    /// needs Scrap Iron' reading that `items.md` §3 leans on happens for free, in the place you would look
    /// anyway."* The <i>(have 1)</i> only appears on the ones you are short of, so the line names the
    /// shortage without becoming a table of numbers you have to read.
    /// <para>
    /// A repair recipe (<see cref="RecipeData.RepairsWorld"/>) has no output item to be named after, so
    /// the display name carries it — which is why those three must author one. There is a gate.
    /// </para>
    /// </remarks>
    private static string RowText(InventoryCraftingManager inv, RecipeData recipe, CraftBlock verdict)
    {
        var name = string.IsNullOrEmpty(recipe.DisplayName)
            ? inv.DisplayNameOf(recipe.OutputItemId)
            : recipe.DisplayName;

        if (string.IsNullOrEmpty(name)) name = recipe.Id;

        var cost = string.Join(", ", recipe.Inputs
            .Where(input => input != null && !string.IsNullOrEmpty(input.ItemId))
            .Select(input =>
            {
                var held = inv.GetCount(input.ItemId);
                var line = $"{inv.DisplayNameOf(input.ItemId)} ×{input.Amount}";
                return held >= input.Amount ? line : $"{line} (have {held})";
            }));

        // A repair recipe can legitimately cost nothing but the walk, and an empty middle segment would
        // leave a stray separator behind. `items.md` §6 gives all three a cost, but the row must not
        // depend on that being true.
        var parts = new List<string> { name };
        if (!string.IsNullOrEmpty(cost)) parts.Add(cost);

        var why = InventoryCraftingManager.Words(verdict);
        if (!string.IsNullOrEmpty(why)) parts.Add(why);

        return string.Join("  ·  ", parts);
    }

    /// <summary>
    /// The line above the list. It names the side rather than the station, because the side is the thing
    /// the player is choosing between and the station is already the object they clicked.
    /// </summary>
    private string HeadingFor(int rows)
    {
        var side = _side == KnowledgeTrack.Physick ? "Her physick" : "His craft";

        return rows switch
        {
            0 => $"{side} — nothing written here yet.",
            1 => $"{side} — one page.",
            _ => $"{side} — {rows} pages.",
        };
    }

    /// <summary>
    /// Make it, and stay open. <b>Unlike the camp menu, a successful press does not close the panel</b> —
    /// crafting is repeatable and the next thing you want is usually the second salve, so shutting the
    /// book would mean walking back into it. Rebuilt either way: a craft spends materials and may set a
    /// flag, so every other row's verdict can legitimately have changed.
    /// </summary>
    private void OnRowPressed(string recipeId)
    {
        InventoryCraftingManager.Instance?.Craft(recipeId);
        Rebuild();
    }

    // ---- The two sides ---------------------------------------------------

    private void OnCraftTabPressed() => SelectSide(KnowledgeTrack.Craft);

    private void OnPhysickTabPressed() => SelectSide(KnowledgeTrack.Physick);

    private void SelectSide(KnowledgeTrack side)
    {
        _side = side;
        Rebuild();
    }

    // ---- Debug and gates -------------------------------------------------

    /// <summary>Turn to a side without a mouse. For the gates, on <c>InventoryPanel.DebugSelectTab</c>.</summary>
    public void DebugSelectSide(KnowledgeTrack side) => SelectSide(side);

    /// <summary>
    /// Press a row by index. For the gates: a <c>Pressed</c> signal connected from C# is not
    /// introspectable from C#, so the only honest way to assert the wiring is to fire it — the lesson
    /// unit 3a's re-read wrote down about the satchel's repaint.
    /// </summary>
    public bool DebugPressRow(int index)
    {
        if (index < 0 || index >= _rows.Count || _rows[index].Button.Disabled) return false;

        _rows[index].Button.EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    /// <summary>Press the row for a named recipe, whichever side it is on. Fails if it is not drawn.</summary>
    public bool DebugPressRecipe(string recipeId)
    {
        for (var i = 0; i < _rows.Count; i++)
            if (_rows[i].RecipeId == recipeId) return DebugPressRow(i);

        return false;
    }

    /// <summary>Press whichever tab the given side belongs to, as a click would. For the gates.</summary>
    public bool DebugPressSideTab(KnowledgeTrack side)
    {
        var tab = side == KnowledgeTrack.Physick ? _physickTab : _craftTab;
        if (tab == null) return false;

        tab.EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    // ---- The fallback tree -----------------------------------------------

    /// <summary>
    /// The published contract, built in code because no <c>.tscn</c> exists yet. <b>Delete this the day
    /// <c>scenes/ui/CraftingPanel.tscn</c> lands</b>; the node names here are exactly what that scene must
    /// carry, marked unique-in-owner.
    /// </summary>
    public static CraftingPanel BuildFallback()
    {
        var panel = new CraftingPanel
        {
            Name = "CraftingPanel",
            MouseFilter = MouseFilterEnum.Stop,
        };
        // Anchors **and** offsets, like `BuildDim` below and for the same reason. `SetAnchorsPreset` alone
        // happens to leave zeroes here — the node is 0×0 in a parent that is 0×0 at this moment — so
        // `BlocksTheRoom` passes for a reason that is true by accident and would stop being true the day
        // this node is built anywhere else. The eighth blind-spot class, caught by reading rather than by a
        // red check, which is why the check stays and this line changed.
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panel.Theme = ResourceLoader.Load<Theme>(PanelThemePath);

        var backing = new PanelContainer { Name = "Backing" };
        backing.SetAnchorsPreset(LayoutPreset.Center);
        backing.OffsetLeft = -400f;
        backing.OffsetTop = -240f;
        backing.OffsetRight = 400f;
        backing.OffsetBottom = 240f;
        panel.AddChild(backing);

        var column = new VBoxContainer { Name = "Column" };
        backing.AddChild(column);

        // The divider, first and above everything: it is the one thing decision 9 says this screen *is*.
        var sides = new HBoxContainer { Name = "Sides" };
        column.AddChild(sides);
        sides.AddChild(new Button { Name = "CraftTab", Text = "Craft" });
        sides.AddChild(new Button { Name = "PhysickTab", Text = "Physick" });

        column.AddChild(new Label { Name = "Heading", Text = "" });

        // **The list scrolls, and this was a softlock until 2026-08-05.** Found in play by Jeremy: *"when it is
        // full of the craft recipes it is so long you cannot see the bottom or scroll on the menu so you cannot
        // exit the screen."* Exactly right, and the mechanism is minimum-size propagation — a `PanelContainer`'s
        // minimum size is its child's, so once the `VBoxContainer` of rows was taller than the 480 these offsets
        // ask for, **the minimum won and the backing grew past its own rect**, carrying `CloseButton` — the last
        // child of the column — off the bottom of the screen. `MouseFilter = Stop` means a click cannot reach the
        // room behind, and nothing in the project binds Escape, so ✕ was the only exit and it was unreachable.
        //
        // A `ScrollContainer` fixes it at the cause rather than by moving the button: a scroll container's
        // minimum size deliberately does **not** include its content's, so the backing keeps the size these
        // offsets ask for however long the book gets, the tabs stay at the top and ✕ stays at the bottom.
        //
        // **Why 1092 green checks could not see it.** `DimsTheRoom` and its neighbours already work around the
        // fact that a headless run lays nothing out — they assert *anchors and offsets* precisely because `Size`
        // answers 0 — so the panel's arrangement was checked and its *layout under content* was not. That is
        // `handoff.md`'s standing warning made concrete: the gates prove structure, and this was the first thing
        // play found that structure could not express. The check that now holds it is a **relationship between
        // two measurements** rather than a pixel bound: the backing's combined minimum height must not grow when
        // the book fills up.
        var scroll = new ScrollContainer
        {
            Name = "RecipeScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(740f, 300f),
        };
        column.AddChild(scroll);

        var list = new VBoxContainer
        {
            Name = "RecipeList",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scroll.AddChild(list);

        // Inside the container it fills, so a duplicate lands beside its siblings with no re-parenting —
        // `CampPanel`'s CampTemplate, same reason. `Bind` searches recursively, so nesting the list one level
        // deeper inside the scroll costs the contract nothing.
        // **`AutowrapMode` is what stops a long row widening the whole book**, added 2026-08-05 with the width
        // half of the overflow fix. A `Button`'s minimum width includes its text, and a row is one line of
        // *name · inputs · why-not* — the exorcism engine's runs past 140 characters — so without this the
        // column's minimum beat the offsets and the panel grew off the right of the screen. Wrapping bounds the
        // minimum to the longest **word** and spends the overflow on height instead, which the `ScrollContainer`
        // above already absorbs. Clipping with an ellipsis was the other option and is worse here: the tail of
        // that row is the *reason you cannot make it*, which is the panel's whole job (§2).
        list.AddChild(new Button
        {
            Name = "RecipeTemplate",
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(720f, 0f),
        });

        column.AddChild(new Button { Name = "CloseButton", Text = "✕" });

        return panel;
    }

    /// <summary>
    /// Tint the room while the book is open. <b>Delete when the <c>.tscn</c> authors a <c>Dim</c></b> —
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
        // ColorRect one line old — which is the eighth blind-spot class in handoff §3.
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // First, so the rows draw over it rather than through it.
        MoveChild(dim, 0);

        return dim;
    }
}
