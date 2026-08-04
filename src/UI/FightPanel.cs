using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// The fight, on screen (<c>combat.md</c> §18, unit 3a). Opened by <c>Hud</c> off
/// <c>EventBus.FightStarted</c> and closed off <c>FightEnded</c>, and <b>it decides nothing</b>: every
/// button is one call into <see cref="EncounterManager"/>, which owns the turn, the rig and the
/// arithmetic. Same division as <see cref="MendPanel"/>, for the same reason — the seam has to be
/// narrow enough that the gates reach all of it with no scene tree.
///
/// <para>
/// Layout lives in <c>scenes/ui/FightPanel.tscn</c> once Jeremy authors it, on the
/// <see cref="InventoryPanel"/> and <see cref="MendPanel"/> precedent: this file knows the node
/// <i>names</i> in the published contract and nothing about their arrangement. Until then
/// <see cref="BuildFallback"/> puts up a crude version of exactly that contract in code, so the loop is
/// walkable the day the manager lands. <b>What gets deleted when the scene arrives is the fallback
/// builder, not this class</b> — which is the point of doing it this way round rather than as a second
/// button row in <c>Hud</c>. There is one fight screen, and the <c>.tscn</c> only replaces its looks.
/// </para>
///
/// <para>
/// <b>The verb strip is rebuilt every repaint from what you actually have</b>, rather than being a fixed
/// row with things greyed out. §18 makes the fight a question of what you brought, so a strip with two
/// entries is a truthful answer to *"I brought nothing"* — and the baseline Strike is always one of them,
/// which is the guarantee that keeps that from being a fight with no verbs.
/// </para>
///
/// <para>
/// <b>No numbers anywhere.</b> §5's condition ladder is the only readout of how hurt something is, and
/// §7 gives the same treatment to what it hits you with: <c>%Reading</c> names types, never values. The
/// player learns *"it comes at you with edges"* and picks a coat, instead of reading 8 and doing
/// arithmetic.
/// <para>
/// <b>An opponent health bar is deferred, not refused</b> — the first playtest asked for one and the
/// ruling was to wait (2026-08-03, and see <c>combat.md</c> §18). If it ever lands it belongs on
/// <c>%Condition</c> as a fill driven off the same ladder, so there is still exactly one opinion about
/// how hurt something is. <b>Do not add a second readout that counts durability.</b>
/// </para>
/// </para>
/// </summary>
public partial class FightPanel : Control
{
    private Label _title;
    private Label _condition;
    private Label _reading;
    private Label _rigStrip;
    private Container _verbContainer;
    private Button _verbTemplate;
    private Label _log;
    private Button _closeButton;

    /// <summary>
    /// The black tint over the room behind the fight. Optional in the contract and built by
    /// <see cref="BuildDim"/> when the tree has none — see that method for why it is a node here rather
    /// than a modulate on the room.
    /// </summary>
    private ColorRect _dim;

    private string _instanceId = "";

    /// <summary>The buttons currently drawn, template excluded, paired with what pressing one does.</summary>
    private readonly List<Button> _verbs = new();

    private readonly List<string> _verbLabels = new();

    /// <summary>The last few notify lines, newest last. A fight is mostly its own transcript.</summary>
    private readonly List<string> _lines = new();

    private const int LogLines = 6;

    // ---- For the gates ---------------------------------------------------
    // Read off what is on screen rather than recomputing, for the reason MendPanel's properties give:
    // a property that recomputes only proves it agrees with itself.

    /// <summary>Whether the fight screen is up. Reads <c>Visible</c> rather than a bool of its own.</summary>
    public bool IsOpen => Visible;

    /// <summary>Which placement is being fought, as the panel has it, or "".</summary>
    public string OpponentId => _instanceId;

    /// <summary>The name across the top, as drawn.</summary>
    public string TitleText => _title?.Text ?? "";

    /// <summary>§5's word for how hurt it is, as drawn.</summary>
    public string ConditionText => _condition?.Text ?? "";

    /// <summary>
    /// The legibility line, as drawn. §18 requires it to name <b>both</b> the vulnerability and what the
    /// creature deals, which is what makes the coat a decision — so this is the string gate 13 reads.
    /// </summary>
    public string ReadingText => _reading?.Text ?? "";

    /// <summary>What is on the creature's sites, as drawn.</summary>
    public string RigText => _rigStrip?.Text ?? "";

    /// <summary>How many verb buttons are drawn — template excluded.</summary>
    public int VerbCount => _verbs.Count;

    /// <summary>The transcript, as drawn.</summary>
    public string LogText => _log?.Text ?? "";

    /// <summary>The label on a drawn verb, or "" out of range.</summary>
    public string VerbLabelAt(int index) =>
        index >= 0 && index < _verbs.Count ? _verbs[index].Text : "";

    /// <summary>
    /// Whether the room behind the fight is tinted down — a full-screen node dark enough to read as a
    /// tint rather than a haze. Asked of the anchors and offsets rather than of the size, so it answers the
    /// same before the first layout pass and under <c>--verify-slice</c>, where nothing is ever laid out.
    /// </summary>
    /// <remarks>
    /// <b>The offsets are here because their absence hid the bug this property exists to catch.</b> The
    /// first version asked only about the anchors, which sounds like the same question and is not: anchors
    /// of 0..1 with offsets of (0, 0, -1920, -1080) describe a node that is nominally full-rect and
    /// actually 0x0, and that is precisely what shipped. Every check passed, and <c>--shots</c> proved the
    /// tint was absent by producing a byte-identical frame with the node's alpha forced to 1.
    /// <para>
    /// So: a *complete* arrangement rather than a partial one. Anchors 0..1 with zero offsets is the whole
    /// of "fills its parent" and needs no layout pass to be true, which is what keeps this answerable
    /// headlessly. Asking for <c>Size</c> would be the real invariant and cannot be asked here — the
    /// lesson is that when you settle for the arrangement, it has to be all of it.
    /// </para>
    /// </remarks>
    public bool DimsTheRoom =>
        _dim is { Visible: true } &&
        _dim.Color.A >= 0.4f &&
        Mathf.IsEqualApprox(_dim.AnchorRight, 1f) &&
        Mathf.IsEqualApprox(_dim.AnchorBottom, 1f) &&
        Mathf.IsZeroApprox(_dim.OffsetLeft) && Mathf.IsZeroApprox(_dim.OffsetTop) &&
        Mathf.IsZeroApprox(_dim.OffsetRight) && Mathf.IsZeroApprox(_dim.OffsetBottom);

    /// <summary>
    /// Whether the tint draws <i>behind</i> the fight rather than over it. This is the failure that
    /// looks like the feature: a dim in front of the verb strip still darkens the room, still passes
    /// <see cref="DimsTheRoom"/>, and greys out every button you need to press.
    /// </summary>
    /// <remarks>
    /// Compared at the panel's own child level, because the two nodes need not be siblings — the tint is
    /// always a direct child, while <c>%VerbContainer</c> sits several deep inside the frame in both the
    /// fallback and any authored scene. So the walk finds whichever ancestor of the strip <i>is</i> a
    /// direct child, and compares that.
    /// </remarks>
    public bool DimDrawsBehindTheVerbs
    {
        get
        {
            if (_dim == null || _verbContainer == null) return false;

            Node branch = _verbContainer;
            while (branch.GetParent() != null && branch.GetParent() != this) branch = branch.GetParent();

            return branch.GetParent() == this && _dim.GetIndex() < branch.GetIndex();
        }
    }

    /// <summary>
    /// Whether the panel's theme gives a <c>PanelContainer</c> something you cannot see through.
    /// </summary>
    /// <remarks>
    /// The behaviour rather than the resource, deliberately. What was actually wrong until 2026-08-03 is
    /// that this panel is the only one built in code, so it received no theme at all and its frame fell
    /// back to Godot's default — the room showed straight through a live fight, and the first playtest
    /// reported it. Asserting <c>Theme == PanelTheme.tres</c> instead would go red the day Jeremy gives
    /// the authored scene a fight-specific theme, which is a change and not a regression.
    /// </remarks>
    public bool DrawsOnSomethingOpaque =>
        Theme != null &&
        Theme.HasStylebox("panel", "PanelContainer") &&
        Theme.GetStylebox("panel", "PanelContainer") is StyleBoxFlat box &&
        box.BgColor.A >= 0.85f;

    public override void _Ready()
    {
        _title = Bind<Label>("Title");
        _condition = Bind<Label>("Condition");
        _reading = Bind<Label>("Reading");
        _rigStrip = Bind<Label>("RigStrip");
        _verbContainer = Bind<Container>("VerbContainer");
        _verbTemplate = Bind<Button>("VerbTemplate");
        _log = Bind<Label>("Log");
        _closeButton = Bind<Button>("CloseButton");

        // Optional in the contract, like the corner ✕ — and built rather than merely tolerated when it is
        // absent, because unlike the ✕ there is no other way to get the effect. `InventoryPanel.tscn`
        // authored its own `Dim` from the start; this panel predates the convention by being the one built
        // in code, which is exactly how it ended up transparent.
        _dim = Bind<ColorRect>("Dim", optional: true) ?? BuildDim();

        if (_verbTemplate != null) _verbTemplate.Visible = false;
        if (_closeButton != null) _closeButton.Pressed += OnClosePressed;

        Visible = false;

        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.Notify += OnNotify;

            // The strip is a function of the satchel — the strike names the equipped weapon and its damage
            // type, the salves are one verb each — and `Unequip` emits only `InventoryChanged`, so before
            // 2026-08-03 taking a weapon off mid-fight left a button still offering it. Harmless until the
            // label started naming the damage type it would deliver; then it became a button that lies.
            bus.InventoryChanged += OnInventoryChanged;
        }
    }

    public override void _ExitTree()
    {
        if (_closeButton != null) _closeButton.Pressed -= OnClosePressed;

        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.Notify -= OnNotify;
            bus.InventoryChanged -= OnInventoryChanged;
        }
    }

    /// <summary>
    /// Find a contract node whether the tree came from a <c>.tscn</c> or from
    /// <see cref="BuildFallback"/>. The authored scene marks its nodes unique-in-owner and <c>%Name</c>
    /// resolves; a tree built in code has no owner to be unique in, so it falls back to a name search.
    /// One helper rather than two code paths, so the panel cannot behave differently in the two cases.
    ///
    /// <para>
    /// <paramref name="optional"/> is for the nodes the contract says <i>may</i> be absent, and it exists
    /// because the ones that are absent by design were shouting. Every clean <c>--shots</c> and
    /// <c>--verify-slice</c> run printed <c>contract node 'Dim' is missing</c> with a stack trace while
    /// the panel went on to build the thing in code and work correctly — a red line in a green run, which
    /// is the shape this project keeps arguing against, and it cost a detour to establish that beat 02 was
    /// fine. A missing <i>required</i> node still shouts.
    /// </para>
    /// </summary>
    private T Bind<T>(string name, bool optional = false) where T : Node
    {
        var byUnique = GetNodeOrNull<T>($"%{name}");
        if (byUnique != null) return byUnique;

        var found = FindChild(name, true, false);
        if (found is T typed) return typed;

        if (!optional) GD.PushError($"[FightPanel] contract node '{name}' is missing.");
        return null;
    }

    // ---- Opening and closing ---------------------------------------------

    /// <summary>
    /// Put the fight up. Refuses when the manager is not actually in a fight against that placement,
    /// because a screen offering verbs that all return false is worse than no screen. Returns whether it
    /// opened.
    /// </summary>
    public bool Open(string instanceId)
    {
        var encounters = EncounterManager.Instance;
        if (encounters == null || !encounters.InFight || encounters.OpponentId != instanceId) return false;

        _instanceId = instanceId;
        _lines.Clear();

        Refresh();
        Visible = true;
        return true;
    }

    /// <summary>
    /// Take the screen down. <b>Does not end the fight</b> — <see cref="EncounterManager"/> is told to
    /// stop fighting by <c>Flee</c> and by nothing else, so closing the window can never be a way to
    /// leave one. See <see cref="OnClosePressed"/> for what the close button actually does.
    /// </summary>
    public void Close()
    {
        Visible = false;
        _instanceId = "";
        _lines.Clear();
    }

    /// <summary>
    /// The close button is <b>Flee</b>, spelled the way a window is spelled.
    /// </summary>
    /// <remarks>
    /// There is deliberately no other way out. §3 makes flight guaranteed and prices it at one parting
    /// strike, so a close button that merely hid the screen would be a free escape sitting next to the
    /// paid one — and the paid one is the softlock protection the whole overkill route leans on. Wiring
    /// the obvious gesture to the real verb costs the player nothing they were not going to spend.
    ///
    /// <para>It must not be <i>labelled</i> as the verb, though: the fallback spelled it "Break off" until
    /// 2026-08-03 and drew two buttons for one act. It is the corner ✕ and the strip has the words. See
    /// <see cref="BuildFallback"/>.</para>
    /// </remarks>
    private void OnClosePressed() => EncounterManager.Instance?.Flee();

    // ---- Painting --------------------------------------------------------

    private void Refresh()
    {
        var encounters = EncounterManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        if (encounters == null || !encounters.InFight) return;

        var placement = NavigationManager.Instance?.FindPlacement(_instanceId);
        var data = placement?.Entity;
        var state = EntityStateManager.Instance?.GetState(_instanceId) ?? EntityState.Corrupted;

        if (_title != null)
            _title.Text = (data?.NameFor(state) ?? _instanceId).ToUpperInvariant();

        if (_condition != null && data != null)
            _condition.Text = CombatManager.Instance?.DescribeCondition(_instanceId, data) ?? "";

        if (_reading != null)
            _reading.Text = encounters.DescribeOpponent(_instanceId);

        if (_rigStrip != null)
        {
            var rig = encounters.RiggedAgainst(_instanceId);
            _rigStrip.Text = rig.Count == 0
                ? "Nothing set."
                : "Set: " + string.Join(", ", rig.Select(id => inv?.DisplayNameOf(id) ?? id));
        }

        RebuildVerbs();
        RefreshLog();
    }

    /// <summary>
    /// Rebuild the strip from what you have this instant: the baseline strike, every rigged trap still
    /// waiting on a turn, every salve in the satchel while the fight's allowance holds, and flight.
    /// </summary>
    private void RebuildVerbs()
    {
        foreach (var button in _verbs)
        {
            _verbContainer?.RemoveChild(button);
            button.QueueFree();
        }
        _verbs.Clear();
        _verbLabels.Clear();

        var encounters = EncounterManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        if (encounters == null || _verbContainer == null || _verbTemplate == null) return;

        // Named with its damage type, because the reading above already says what the creature barely
        // notices and the verb was the half that never said what you were about to hit it with. Both
        // halves derived the same way `Strike` derives them — the weapon's own `Deals` or the bare-handed
        // constant — so a label promising `edges` cannot outlive the strike that delivers weight.
        var weapon = inv?.EquippedWeapon;
        var strikeType = weapon?.Deals ?? GameConfig.BareHandedType;
        var strikeWith = weapon != null ? inv.DisplayNameOf(weapon.Id) : "bare hands";
        AddVerb($"Strike — {strikeWith} ({EncounterManager.Words(strikeType)})", () => encounters.Strike());

        // Only the ones that wait for a turn. The self-triggering half fired on engage and refuses a
        // second firing, so offering it would be offering a button that says no.
        foreach (var trapId in encounters.RiggedAgainst(_instanceId))
        {
            var trap = inv?.GetItem(trapId);
            if (trap == null || trap.TriggersItself) continue;

            var id = trapId;
            AddVerb($"Set off — {inv.DisplayNameOf(id)}", () => encounters.Trigger(id));
        }

        if (inv != null && encounters.SalvesUsed < GameConfig.SalveSlotsPerFight)
        {
            foreach (var pair in inv.Stacks.OrderBy(p => inv.DisplayNameOf(p.Key)))
            {
                if (pair.Value <= 0) continue;
                if (inv.GetItem(pair.Key)?.Category != ItemCategory.Salve) continue;

                var id = pair.Key;
                AddVerb($"Treat — {inv.DisplayNameOf(id)}", () => encounters.Treat(id));
            }
        }

        AddVerb("Break off", () => encounters.Flee());
    }

    private void AddVerb(string label, System.Action action)
    {
        var button = (Button)_verbTemplate.Duplicate();
        button.Name = $"Verb_{_verbs.Count}";
        button.Visible = true;
        button.Text = label;

        // Repaint after acting, because one press changes the condition, the rig, the bar and the
        // transcript at once — which is why there is no per-turn signal to subscribe to instead.
        button.Pressed += () =>
        {
            action();
            if (Visible) Refresh();
        };

        _verbContainer.AddChild(button);
        _verbs.Add(button);
        _verbLabels.Add(label);
    }

    private void RefreshLog()
    {
        if (_log == null) return;
        _log.Text = string.Join("\n", _lines);
    }

    /// <summary>
    /// The fight's own transcript, taken off the same <c>Notify</c> line the HUD shows. A subscription
    /// rather than a return value from each verb: the interesting lines come from three managers deep
    /// (a trap firing, a wound closing, a bar draining), and threading them back out would mean every
    /// method in that chain returning prose.
    /// </summary>
    private void OnNotify(string text)
    {
        if (!Visible || string.IsNullOrEmpty(text)) return;

        _lines.Add(text);
        while (_lines.Count > LogLines) _lines.RemoveAt(0);

        RefreshLog();

        // Deferred, and the deferral is the whole point rather than a style choice. Lines arrive from
        // inside a verb's own handler, and a full repaint frees the button that handler belongs to — so
        // repainting here directly would be tearing down the node whose signal is still on the stack.
        // Deferring runs it once the stack has unwound.
        //
        // What this exists for is the opening volley: `Engage` emits FightStarted *before* firing the
        // self-triggering traps, so the panel is up to log them but was painted against a creature they
        // have not hit yet. Without this the condition line reads one volley stale — and a trap can kill
        // outright on engage, which would leave the screen showing a fight that is already over.
        // Wrapped in a Callable rather than named as a string, so the deferral does not depend on the
        // generator having registered a private method for engine-side lookup.
        Callable.From(RepaintIfLive).CallDeferred();
    }

    /// <summary>Repaint, or do nothing if the fight ended in the meantime. See <see cref="OnNotify"/>.</summary>
    /// <summary>
    /// The satchel changed under the strip, so the strip is stale. Deferred for exactly the reason
    /// <see cref="OnNotify"/> defers — <c>Treat</c> spends a salve from inside its own button's handler,
    /// so repainting inline would free the node whose signal is still on the stack.
    /// </summary>
    private void OnInventoryChanged()
    {
        SatchelSignalsSeen++;
        Callable.From(RepaintIfLive).CallDeferred();
    }

    private void RepaintIfLive()
    {
        if (!Visible || string.IsNullOrEmpty(_instanceId)) return;
        if (EncounterManager.Instance?.InFight != true) return;

        Refresh();
    }

    // ---- Driving it without a mouse --------------------------------------

    /// <summary>
    /// Press a verb by index, through the same handler a click goes through. For
    /// <c>--verify-slice</c>, on the <see cref="MendPanel.DebugClickPart"/> precedent — the panel layer
    /// is live headlessly and untested by anything else, so it has to be drivable.
    /// </summary>
    public bool DebugPressVerb(int index)
    {
        if (index < 0 || index >= _verbs.Count) return false;

        _verbs[index].EmitSignal(BaseButton.SignalName.Pressed);
        return true;
    }

    /// <summary>
    /// How many times the satchel has told this panel it changed.
    /// </summary>
    /// <remarks>
    /// <b>One field, so the subscription is falsifiable.</b> Two tidier spellings were tried and both went
    /// red against correct code: a C# <c>signal += handler</c> marshals into a custom callable, so walking
    /// <c>GetSignalConnectionList</c> finds an entry whose <c>Target</c> is null and whose <c>Delegate</c>
    /// does not survive the round trip, and <c>IsConnected</c> with a freshly-made callable to the same
    /// handler does not match it either. What is reliably observable is that the handler *ran* — the
    /// deferral is only on the repaint, so the count moves inside the same call that changed the satchel.
    /// A gate reads it either side of an <c>Unequip</c>; deleting the <c>+=</c> leaves it still.
    /// </remarks>
    public int SatchelSignalsSeen { get; private set; }

    /// <summary>
    /// Repaint now instead of on the next idle frame.
    /// </summary>
    /// <remarks>
    /// <b>This exists because the strip's real repaints are deferred and <c>--verify-slice</c> never
    /// yields a frame.</b> <see cref="OnInventoryChanged"/> has to defer — <c>Treat</c> spends a salve from
    /// inside its own button's handler — so a gate that changes the satchel and then reads a label would
    /// read the label from before the change. This is the synchronous door for that, and it means the
    /// checks over the labels cover <see cref="RebuildVerbs"/> and <b>not</b> the subscription that calls
    /// it. The subscription has a check of its own next to them, over the connection rather than its
    /// effect, because its effect needs a frame nobody in that harness has.
    /// </remarks>
    public void DebugRepaintNow() => RepaintIfLive();

    /// <summary>
    /// Press the first verb whose label contains <paramref name="fragment"/>, case-insensitively.
    /// Indices shift as the strip rebuilds — a spent salve stops being offered — so a gate that wants
    /// *"the Break off button"* should ask for it by name rather than counting.
    /// </summary>
    public bool DebugPressVerbNamed(string fragment)
    {
        if (string.IsNullOrEmpty(fragment)) return false;

        for (var i = 0; i < _verbs.Count; i++)
            if (_verbs[i].Text.Contains(fragment, System.StringComparison.OrdinalIgnoreCase))
                return DebugPressVerb(i);

        return false;
    }

    /// <summary>
    /// Strike until the fight ends, and report how many turns it took — <b>the number §5 asks for</b>,
    /// measured through the panel rather than beside it. Returns 0 if it did not end, which for a fight
    /// bounded by <paramref name="cap"/> means the weapon cannot finish it.
    /// </summary>
    /// <remarks>
    /// The cap is a hang guard, not a rule. <c>verify-slice-blind-spots</c>'s worst class is a run that
    /// hangs instead of failing, and a strike loop against a creature whose durability the current
    /// weapon cannot dent is exactly that shape.
    /// </remarks>
    public int DebugStrikeUntilDone(int cap = 40)
    {
        var encounters = EncounterManager.Instance;
        if (encounters == null || !encounters.InFight) return 0;

        for (var i = 0; i < cap; i++)
        {
            var turns = encounters.TurnNumber;
            if (!DebugPressVerbNamed("Strike")) return 0;
            if (!encounters.InFight) return turns + 1;
        }

        return 0;
    }

    // ---- The stopgap layout ----------------------------------------------

    /// <summary>
    /// Build the contract in code, crudely, so the fight is walkable before
    /// <c>scenes/ui/FightPanel.tscn</c> exists. <b>Delete this method when the scene lands</b> — it is
    /// the only part of this file the authored version replaces.
    /// </summary>
    /// <remarks>
    /// Node names here are exactly the published contract — <c>Title</c>, <c>Condition</c>,
    /// <c>Reading</c>, <c>RigStrip</c>, <c>VerbContainer</c>, <c>VerbTemplate</c>, <c>Log</c>,
    /// <c>CloseButton</c> — so the authored scene is a drop-in and this class never learns which one it
    /// got. Marking them unique-in-owner is what the <c>.tscn</c> adds; <see cref="Bind{T}"/> covers the
    /// difference.
    /// </remarks>
    public static FightPanel BuildFallback()
    {
        var panel = new FightPanel
        {
            Name = "FightPanel",
            MouseFilter = MouseFilterEnum.Stop,
        };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);

        // The shared theme the two authored panels already carry, rather than a StyleBoxFlat built here
        // the way `DebugPanel` builds its own. Loading it is what stops this panel being the odd one out:
        // `PanelTheme.tres` gives `PanelContainer` a 96%-opaque frame, and its absence is the whole reason
        // a live fight was legible *through* the room until 2026-08-03. A second hand-rolled opinion about
        // what a panel looks like would also have to be kept in step with the theme by hand, in the one
        // method whose doc comment says it gets deleted.
        panel.Theme = ResourceLoader.Load<Theme>(PanelThemePath);

        var backing = new PanelContainer { Name = "Backing" };

        // Centred, not hung off the right edge. §18 makes a fight the one screen you are not free to walk
        // away from, and the first playtest read a right-hand panel over a live room as furniture rather
        // than as the thing to look at. Symmetric offsets rather than `LayoutPreset.Center` alone, because
        // a `PanelContainer` sizes to its content and the preset would centre whatever size it happened to
        // have on the frame the preset was applied.
        backing.SetAnchorsPreset(LayoutPreset.Center);
        backing.OffsetLeft = -300f;
        backing.OffsetTop = -260f;
        backing.OffsetRight = 300f;
        backing.OffsetBottom = 260f;

        panel.AddChild(backing);

        var column = new VBoxContainer { Name = "Column" };
        backing.AddChild(column);

        column.AddChild(new Label { Name = "Title", Text = "" });
        column.AddChild(new Label { Name = "Condition", Text = "" });
        column.AddChild(new Label
        {
            Name = "Reading",
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(440f, 0f),
        });
        column.AddChild(new Label { Name = "RigStrip", Text = "" });

        var verbs = new VBoxContainer { Name = "VerbContainer" };
        column.AddChild(verbs);

        // Inside the container it fills, so a duplicate lands beside its siblings with no re-parenting.
        verbs.AddChild(new Button { Name = "VerbTemplate", Text = "" });

        column.AddChild(new Label
        {
            Name = "Log",
            Text = "",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(440f, 0f),
        });
        // A window gesture, not a second copy of the verb. `--shots` caught this drawing **two** buttons
        // both reading "Break off" — one from `RebuildVerbs`, one here — which is one control too many for
        // one act. The strip keeps the words, because that is where every other turn-costing verb lives;
        // this keeps the corner and the wiring, because a dismiss gesture that did *not* cost a blow would
        // be the free escape `OnClosePressed` exists to prevent.
        //
        // For the `.tscn` author: if the authored scene has no corner ✕, leave the node out — `Bind`
        // returns null, nothing subscribes, and the strip is still the way out.
        column.AddChild(new Button { Name = "CloseButton", Text = "✕" });

        return panel;
    }

    /// <summary>The theme the two authored panels carry, so the third one looks like them.</summary>
    private const string PanelThemePath = "res://scenes/ui/PanelTheme.tres";

    /// <summary>
    /// Put a black tint over the room, for as long as the tree has no <c>Dim</c> of its own.
    /// <b>Delete this when <c>FightPanel.tscn</c> authors one</b> — <c>InventoryPanel.tscn</c> already has
    /// the node, at the same 55% black, and copying its value rather than picking a new one is the point.
    /// </summary>
    /// <remarks>
    /// A node inside the panel rather than a <c>Modulate</c> on the room, for two reasons. It goes up and
    /// down with the panel's own <c>Visible</c> and so cannot survive a fight ending by any route — and
    /// there are four routes, one of which is dying. And it keeps the fight screen the only thing that
    /// knows a fight is on screen: darkening `RoomView` from here would give the world a second opinion
    /// about UI state, which is what the whole manager/panel split exists to avoid.
    /// <para>
    /// <c>MouseFilter.Ignore</c> because it must not eat the clicks: the panel root is already
    /// <c>Stop</c> and full-rect, which is what makes the room unclickable mid-fight, and a <c>Stop</c>
    /// here would sit between that root and its own buttons.
    /// </para>
    /// </remarks>
    private ColorRect BuildDim()
    {
        var dim = new ColorRect
        {
            Name = "Dim",
            Color = new Color(0f, 0f, 0f, 0.55f),
            MouseFilter = MouseFilterEnum.Ignore,
        };

        AddChild(dim);

        // **Anchors *and* offsets, and the difference is the whole bug.** `SetAnchorsPreset` alone reads as
        // "make this fill its parent" and does not do that: with a parent whose size is already known it
        // rewrites the offsets to *preserve the node's current rect*, which for a ColorRect one line old is
        // 0x0. The dim sat at 0x0 for a whole unit, and `BuildFallback`'s identical-looking
        // `panel.SetAnchorsPreset(FullRect)` worked only by accident of order — it runs before the panel
        // enters the tree, so there is no parent range to preserve a rect against and the offsets stay 0.
        // Two calls that look the same, one of which depends on not being in the tree yet.
        dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // First, so everything already in the tree draws over it. See `DimDrawsBehindTheVerbs`: a tint in
        // front of the verb strip passes every other check and greys out the buttons.
        MoveChild(dim, 0);

        return dim;
    }
}
