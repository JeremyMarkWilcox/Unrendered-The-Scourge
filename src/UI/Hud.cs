using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;
using Unrendered.World;

namespace Unrendered.UI;

/// <summary>
/// Screen furniture: where you are, which realm you are in, how much Resolve and Vitality are
/// left, what the cursor is over, and the last thing that happened.
///
/// Built in code rather than authored as a scene tree. M4 replaces this with a designed
/// HUD; until then, code keeps the whole thing in one readable place and out of the way.
/// </summary>
public partial class Hud : CanvasLayer
{
    private Label _roomLabel;
    private Label _realmLabel;
    private Label _hoverLabel;
    private Label _notifyLabel;
    private ResolveMeter _resolve;
    private VitalityMeter _vitality;
    private VBoxContainer _poolColumn;
    private VBoxContainer _resolveGroup;
    private VBoxContainer _vitalityGroup;
    private DebugPanel _debug;
    private InventoryPanel _inventory;
    private MendPanel _mend;
    private FightPanel _fight;
    private CampPanel _camp;

    private float _notifyTimer;
    private const float NotifyHoldSeconds = 5f;

    /// <summary>
    /// Opacity of the pool that cannot currently move. Recessive but still readable — see
    /// <see cref="RefreshPools"/> for why it is dimmed rather than hidden.
    /// </summary>
    private const float InertPoolAlpha = 0.45f;

    /// <summary>The room name on screen. For the gates; see <see cref="RoomView.DisplayedRoomId"/>.</summary>
    public string DisplayedRoomName => _roomLabel?.Text ?? "";

    /// <summary>
    /// The satchel panel, or null if its scene failed to load. Exposed so <c>Main</c> can toggle it
    /// and the gates can drive it — a null here is itself worth failing a gate over, since a
    /// mistyped scene path would otherwise cost nothing until someone pressed <c>I</c>.
    /// </summary>
    public InventoryPanel Inventory => _inventory;

    /// <summary>
    /// The mending board, or null if its scene failed to load. Nothing toggles this one — it opens
    /// off <c>MendRequested</c> and closes itself — but it is exposed for the same reason
    /// <see cref="Inventory"/> is: a null here is a mend that silently does nothing, which is exactly
    /// the class of failure the gates exist to make loud.
    /// </summary>
    public MendPanel Mend => _mend;

    /// <summary>
    /// The fight screen. <b>Never null</b>, which is the one way this differs from the two above: it
    /// falls back to a code-built layout rather than to nothing, so there is no version of this game in
    /// which a fight starts and no screen appears. The gates drive it through here.
    /// </summary>
    public FightPanel Fight => _fight;

    /// <summary>
    /// The camp menu — unit 5's return-to-camp verb. <b>Never null</b>, for
    /// <see cref="Fight"/>'s reason: it has no authored scene yet and falls back to a code-built tree, so
    /// there is no version of this game in which the verb has nowhere to appear.
    /// </summary>
    public CampPanel Camp => _camp;

    /// <summary>Opacity of the Vitality caption and bar as drawn. For the gates.</summary>
    public float VitalityOpacity => _vitalityGroup?.Modulate.A ?? 1f;

    /// <summary>Opacity of the Resolve caption and bar as drawn. Should always be 1. For the gates.</summary>
    public float ResolveOpacity => _resolveGroup?.Modulate.A ?? 1f;

    /// <summary>
    /// Which pool is drawn uppermost — "RESOLVE" or "VITALITY". Exposed for the gates, so the
    /// ordering rule in <see cref="RefreshPools"/> is asserted rather than assumed. Reads the
    /// caption actually on screen rather than recomputing the condition, which would only prove
    /// the condition agrees with itself.
    /// </summary>
    public string TopPoolLabel =>
        _poolColumn?.GetChildCount() > 0 && _poolColumn.GetChild(0) is VBoxContainer top &&
        top.GetChildCount() > 0 && top.GetChild(0) is Label caption
            ? caption.Text
            : "";

    public override void _Ready()
    {
        Layer = 10;
        BuildUi();

        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.RoomEntered += OnRoomEntered;
        bus.RealmChanged += OnRealmChanged;
        bus.HoverTargetChanged += OnHoverTargetChanged;
        bus.Notify += OnNotify;
        bus.ItemAcquired += OnItemAcquired;
        bus.PocketEntered += OnPocketEntered;
        bus.PocketLeft += OnPocketLeft;
        bus.MendRequested += OnMendRequested;
        bus.FightStarted += OnFightStarted;
        bus.FightEnded += OnFightEnded;

        OnRealmChanged((int)(RealmTransitionManager.Instance?.CurrentRealm ?? GameRealm.Waking));
        RefreshPools();
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.RoomEntered -= OnRoomEntered;
        bus.RealmChanged -= OnRealmChanged;
        bus.HoverTargetChanged -= OnHoverTargetChanged;
        bus.Notify -= OnNotify;
        bus.ItemAcquired -= OnItemAcquired;
        bus.PocketEntered -= OnPocketEntered;
        bus.PocketLeft -= OnPocketLeft;
        bus.MendRequested -= OnMendRequested;
        bus.FightStarted -= OnFightStarted;
        bus.FightEnded -= OnFightEnded;
    }

    // ---- Construction ----------------------------------------------------

    private void BuildUi()
    {
        var root = new Control
        {
            Name = "HudRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        // -- Top left: location and realm --
        var topLeft = new VBoxContainer { Name = "TopLeft", MouseFilter = Control.MouseFilterEnum.Ignore };
        topLeft.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        topLeft.Position = new Vector2(40, 32);
        root.AddChild(topLeft);

        _roomLabel = MakeLabel("", 30, new Color("#e6dfd2"));
        topLeft.AddChild(_roomLabel);

        _realmLabel = MakeLabel("", 20, new Color("#9aa6b2"));
        topLeft.AddChild(_realmLabel);

        // -- Top right: the two pools, live one on top and undimmed. See RefreshPools. --
        _poolColumn = new VBoxContainer { Name = "Pools", MouseFilter = Control.MouseFilterEnum.Ignore };
        _poolColumn.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _poolColumn.Position = new Vector2(-460, 32);
        _poolColumn.Alignment = BoxContainer.AlignmentMode.End;
        root.AddChild(_poolColumn);

        _resolve = new ResolveMeter { Name = "ResolveMeter" };
        _resolveGroup = MakePoolGroup("ResolveGroup", "RESOLVE", new Color("#9aa6b2"), _resolve);
        _poolColumn.AddChild(_resolveGroup);

        _vitality = new VitalityMeter { Name = "VitalityMeter" };
        _vitalityGroup = MakePoolGroup("VitalityGroup", "VITALITY", new Color("#a89a8c"), _vitality);
        _poolColumn.AddChild(_vitalityGroup);

        RefreshPools();

        // -- Bottom centre: hover target and notifications --
        var bottom = new VBoxContainer { Name = "Bottom", MouseFilter = Control.MouseFilterEnum.Ignore };
        bottom.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        bottom.Position = new Vector2(0, -150);
        bottom.Alignment = BoxContainer.AlignmentMode.Center;
        root.AddChild(bottom);

        _notifyLabel = MakeLabel("", 26, new Color("#e6dfd2"));
        _notifyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _notifyLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _notifyLabel.CustomMinimumSize = new Vector2(0, 70);
        bottom.AddChild(_notifyLabel);

        _hoverLabel = MakeLabel("", 24, new Color("#f2e4c4"));
        _hoverLabel.HorizontalAlignment = HorizontalAlignment.Center;
        bottom.AddChild(_hoverLabel);

        // -- Controls reminder --
        // F6 and F7 are unit 4a's crafting scaffolding and are listed because a debug key nobody knows
        // about is not an unblock. Both come off this line when the journal lands (`build-plan.md` §4).
        // **F8 joined them on 2026-08-03 for the same reason and comes off separately** — it reaches
        // `EncounterManager.Rig`, which shipped in 3a with no verb anywhere, and it retires when the
        // satchel grows a *Set* button beside Use and Equip rather than when the journal lands.
        //
        // `[C] camp` is **not** on the demolition list — it is §17's return-to-camp verb, a shipping
        // control, and it sits first with the satchel rather than with the F-keys for that reason.
        var hint = MakeLabel(
            "[I] satchel   [C] camp   [F3] debug   [F5] save   [F9] load   "
            + "[F6] learn   [F7] craft   [F8] rig   [F10] pocket",
                             16, new Color(0.62f, 0.62f, 0.66f, 0.65f));
        hint.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        hint.Position = new Vector2(40, -44);
        root.AddChild(hint);

        // A `_dreadLabel` sat top-centre here and went with unit 6b. It was the pursuer's only readout —
        // *"footsteps, not far"* keyed to how many **rooms** away it was — and a 3D pursuer is a chase in
        // space, so a room-hop count is the wrong shape to inherit. The 3D pass brings its own, and the one
        // thing worth carrying over is the rule it was built on: **distance as language, never a number and
        // never a minimap**, because being unsure where it is doing the work.

        _debug = new DebugPanel { Name = "DebugPanel", Visible = false };
        root.AddChild(_debug);

        // Added last so they draw over the screen furniture, including the debug panel. Authored as
        // scenes rather than built here, per handoff §2 — grids and slots are layout you need to
        // see while tuning. Two of the three panels now.
        //
        // Each load is checked on its own and neither returns early: a mistyped inventory path used
        // to skip everything below it, so one bad string would have taken the mending board down with
        // the satchel and the second failure would never have been reported.
        var inventoryScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/InventoryPanel.tscn");
        if (inventoryScene == null)
        {
            GD.PushError("Hud: could not load InventoryPanel.tscn — the satchel will not open.");
        }
        else
        {
            _inventory = inventoryScene.Instantiate<InventoryPanel>();
            root.AddChild(_inventory);
        }

        // Over the satchel, because a mend opens on a click in the world and the satchel is closed by
        // then. If they ever can overlap, this is the one that must be readable.
        var mendScene = ResourceLoader.Load<PackedScene>("res://scenes/ui/MendPanel.tscn");
        if (mendScene == null)
        {
            GD.PushError("Hud: could not load MendPanel.tscn — no blight in the game can be mended.");
        }
        else
        {
            _mend = mendScene.Instantiate<MendPanel>();
            root.AddChild(_mend);
        }

        // Over both, because a fight is the one screen you are not free to walk away from — flight is a
        // verb inside it (§3), not a matter of finding the window underneath.
        //
        // The third panel, and **the only one that loads with a fallback instead of an error.** The other
        // two are authored and a missing scene means a mistyped path; this one has no scene yet, so
        // `FightPanel.BuildFallback` puts up the same node contract in code and the loop is walkable
        // today. It is not a second implementation — the panel class is the same either way, and what
        // gets deleted when Jeremy authors `FightPanel.tscn` is the builder, not a branch here.
        // Existence is checked before loading, unlike the two above: a `Load` of a path that is not there
        // logs a Godot-level error, and this is the one case where absence is the expected state rather
        // than a typo. The other two want that error.
        const string fightScenePath = "res://scenes/ui/FightPanel.tscn";
        var fightScene = ResourceLoader.Exists(fightScenePath)
            ? ResourceLoader.Load<PackedScene>(fightScenePath)
            : null;

        // Before the fight screen, so the fight draws over it. The order is the rule rather than a
        // preference: the camp menu refuses to open inside a fight (§17), so the two can never both be up —
        // and on the day that guard is wrong, the fight is the one that must stay readable, because it is
        // the screen you are not free to walk away from. Same fallback arrangement as the panel below and
        // for the same reason: no `.tscn` yet, so `BuildFallback` puts up the contract in code.
        const string campScenePath = "res://scenes/ui/CampPanel.tscn";
        var campScene = ResourceLoader.Exists(campScenePath)
            ? ResourceLoader.Load<PackedScene>(campScenePath)
            : null;

        _camp = campScene != null ? campScene.Instantiate<CampPanel>() : CampPanel.BuildFallback();
        root.AddChild(_camp);

        _fight = fightScene != null ? fightScene.Instantiate<FightPanel>() : FightPanel.BuildFallback();
        root.AddChild(_fight);
    }

    /// <summary>
    /// A caption above its bar, so the pair can be moved and faded as one thing — the group's
    /// <c>Modulate</c> carries down to both children.
    /// </summary>
    private static VBoxContainer MakePoolGroup(string name, string caption, Color colour, Control meter)
    {
        var group = new VBoxContainer { Name = name, MouseFilter = Control.MouseFilterEnum.Ignore };

        var label = MakeLabel(caption, 18, colour);
        label.HorizontalAlignment = HorizontalAlignment.Right;
        group.AddChild(label);
        group.AddChild(meter);

        return group;
    }

    /// <summary>
    /// Puts whichever pool is currently at risk on top, and fades the one that cannot move.
    ///
    /// **Keyed on a live Dream run, deliberately not on the current realm.** This began as a guard
    /// against spectral vision, which flipped <c>CurrentRealm</c> on every <c>Tab</c> press while
    /// Resolve did not move at all — keying on realm would have made both bars jump and dim every
    /// time the player peeked. That mechanism was deleted on 2026-08-02, and the distinction
    /// survives it for a better reason: unit 6's pockets come in a **quiet** version with no clock,
    /// where you are squarely in the Dream and Resolve still is not draining. Realm answers *where
    /// are you*; the run answers *what can move*, which is the question a bar should be sized by.
    ///
    /// **Resolve is never faded, by decision** — inside a heist it is the clock you are playing
    /// against, and out here it still tells you what you have to cross with. Only the body dims,
    /// and only to say "not this, not in here": invariant 2 promises nothing in the Dream can
    /// kill you, and a visible-but-recessive bar states that promise instead of leaving the
    /// player to infer it from an absence.
    ///
    /// The opacity is snapped rather than tweened on purpose. <c>_Process</c> does not tick
    /// meaningfully in a headless run, so a tween would leave the gates asserting an intended
    /// value rather than the one on screen. Inside a heist transition the snap is invisible.
    ///
    /// When Waking boss fights land, Resolve becomes live out here too while the tether is
    /// unbroken (`Corrupted` drains, `Severed` does not), and this condition widens to include
    /// them. It is asking "which pool can move right now?", which is the question that survives.
    /// </summary>
    private void RefreshPools()
    {
        if (_poolColumn == null) return;

        var inDreamRun = PocketManager.InsideTheDream;

        _poolColumn.MoveChild(inDreamRun ? _resolveGroup : _vitalityGroup, 0);

        _vitalityGroup.Modulate = new Color(1f, 1f, 1f, inDreamRun ? InertPoolAlpha : 1f);
        _resolveGroup.Modulate = Colors.White;
    }

    private static Label MakeLabel(string text, int size, Color colour)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", colour);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.85f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeConstantOverride("shadow_outline_size", 3);
        return label;
    }

    // ---- Signals ---------------------------------------------------------

    private void OnRoomEntered(string roomId)
    {
        // Same stale-payload guard as RoomView.OnRoomEntered, for the same reason: an earlier
        // subscriber can navigate mid-emit, and the label must name where you ended up.
        if (NavigationManager.Instance?.CurrentRoomId != roomId) return;

        var room = NavigationManager.Instance?.GetRoom(roomId);
        _roomLabel.Text = room != null && !string.IsNullOrEmpty(room.DisplayName)
            ? room.DisplayName
            : roomId;
    }

    private void OnRealmChanged(int realm)
    {
        var dream = (GameRealm)realm == GameRealm.Dream;
        _realmLabel.Text = dream ? "PIXEL REALM" : "WAKING WORLD";
        _realmLabel.AddThemeColorOverride("font_color",
            dream ? new Color("#c9a2e0") : new Color("#9aa6b2"));
    }

    private void OnHoverTargetChanged(string label, string verb)
    {
        if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(verb))
        {
            _hoverLabel.Text = "";
            return;
        }

        _hoverLabel.Text = string.IsNullOrEmpty(verb) ? label : $"{verb} — {label}";
    }

    private void OnNotify(string text)
    {
        _notifyLabel.Text = text;
        _notifyLabel.Modulate = Colors.White;
        _notifyTimer = NotifyHoldSeconds;
    }

    private void OnItemAcquired(string itemId, int amount) { /* M4: inventory flash */ }

    /// <summary>
    /// The only readout the player gets on the thing hunting them: distance as language,
    /// never a number and never a minimap. Being unsure where it is doing the work.
    /// </summary>
    // Both just reorder the pools. Safe to call either way round: HeistStarted is emitted after
    // ActiveHeist is set and HeistEnded after it is cleared, so IsRunning already reads correctly
    // by the time we are called — no need to trust the payload (invariant 13).
    private void OnHeistStarted(string heistId) => RefreshPools();

    private void OnHeistEnded(string heistId, int outcome) => RefreshPools();

    /// <summary>
    /// Crossed into a pocket (unit 6). Reorders the pools like the heist pair, and <b>names the pocket where
    /// the room name goes</b> — a pocket is not a <c>RoomData</c>, so the label has nothing else to show and
    /// would otherwise sit there naming the screen you are no longer looking at.
    /// </summary>
    /// <remarks>
    /// The hover caption is cleared for the same reason: hovering is a Waking-world affordance over authored
    /// hotspots, and nothing in a 3D pocket produces one. A stale *"Take — Bitterroot"* under a Dream
    /// landscape is the kind of leftover that reads as a bug.
    /// </remarks>
    private void OnPocketEntered(string pocketId)
    {
        var pocket = PocketManager.Instance?.GetPocket(pocketId);

        _roomLabel.Text = string.IsNullOrEmpty(pocket?.DisplayName) ? pocketId : pocket.DisplayName;
        _hoverLabel.Text = "";

        RefreshPools();
    }

    /// <summary>
    /// Came back out. The room label is left to <c>RoomEntered</c>, which
    /// <c>PocketManager.Return</c> triggers by navigating — so this does not set it, and must not: writing
    /// it here would race the arrival and could name the pocket you just left.
    /// </summary>
    private void OnPocketLeft(string pocketId, bool completed) => RefreshPools();

    /// <summary>
    /// Puts the mending board up. This is the whole of the HUD's involvement in mending: the panel
    /// calls <c>MendManager.Mend</c> itself when the board is solved, so nothing about the world
    /// travels back through here.
    /// </summary>
    /// <remarks>
    /// The satchel is closed first. Both panels dim the screen, and two dims stack into something much
    /// darker than either was drawn against — and a mend is not something you should be able to start
    /// from inside the satchel anyway.
    /// </remarks>
    private void OnMendRequested(HotspotData blight)
    {
        if (_inventory != null) _inventory.Visible = false;
        _mend?.Open(blight);
    }

    /// <summary>
    /// A fight was committed to. The satchel closes for the same reason a mend closes it — a fight opens
    /// on a click in the world, so the satchel is shut by then anyway, and this is the one that has to be
    /// readable if they ever do overlap.
    /// </summary>
    /// <remarks>
    /// <b>The panel opening is not what starts the fight</b>, which is the whole shape of unit 3a: the
    /// manager has already committed by the time this runs, and if this screen failed to appear the fight
    /// would still be there to be driven. That is why the gates can check the loop and the panel
    /// separately.
    /// <para>
    /// <b>The camp menu is closed here too, added 2026-08-04 after Jeremy found it up behind the fight
    /// screen in play.</b> Unit 5 considered this line and declined it, reasoning that the menu blocks the
    /// whole screen so no click could start a fight from underneath it — and the menu really does cover the
    /// screen (<c>CampPanel.BlocksTheRoom</c> is asserted). The reasoning was still wrong, because *"no path
    /// can produce this state"* is only ever as good as the paths that were thought of, and a panel left
    /// visible behind another one costs nothing to prevent. **The verb's own refusal is untouched and is
    /// still the rule** — you cannot open the menu inside a fight; this is the other direction, which had no
    /// guard at all.
    /// </para>
    /// </remarks>
    private void OnFightStarted(string instanceId)
    {
        if (_inventory != null) _inventory.Visible = false;
        _camp?.Close();
        _fight?.Open(instanceId);
    }

    /// <summary>
    /// Won, fled or collapsed — the signal carries no outcome and this handler needs none. The last few
    /// lines of the fight have already gone through <c>Notify</c> to the caption, so taking the screen
    /// down loses nothing the player has not read.
    /// </summary>
    private void OnFightEnded(string instanceId) => _fight?.Close();

    // ---- The two panels a key opens -------------------------------------
    //
    // Both live here rather than in `Main` because the HUD is already the thing that decides which panels
    // may be up at once — `OnMendRequested` and `OnFightStarted` close the satchel for the same reason
    // these do. Leaving it in the input handler would put that policy in two places and make the second
    // one easy to forget, which is exactly how the satchel and the camp menu came to be openable together
    // in the first place.

    /// <summary>
    /// Open or shut the satchel. Takes the camp menu down first, and <b>refuses inside a fight</b>. Returns
    /// whether it is now open.
    /// </summary>
    /// <remarks>
    /// <b>Both panels draw a 55% black dim, and two dims stack.</b> That is the stated reason
    /// <see cref="OnMendRequested"/> shuts the satchel before the mending board, and it applies here in both
    /// directions: pressing <c>I</c> with the camp menu up, or <c>C</c> with the satchel up, produced a
    /// screen much darker than either panel was drawn against. The mend and fight cases could rely on the
    /// world being unclickable by then; these two are opened by keys, so neither can.
    ///
    /// <para><b>The fight refusal is the bug Jeremy reported on 2026-08-04</b> — <c>I</c> opened the satchel
    /// behind the fight screen, where <c>C</c> had correctly refused with a line. Being *behind* the fight
    /// screen was the smaller half: the satchel's Use button reached <c>UseItem</c> directly, healing for
    /// free with no turn spent and nothing counted against <c>GameConfig.SalveSlotsPerFight</c>. The
    /// manager-side guard (<see cref="UseBlock.InFight"/>) is what actually closes that; this is the half
    /// that stops the screen appearing at all, which is what he asked for and is also the honest one — a
    /// panel whose only verb is refused should not open. <see cref="FightPanel"/>'s own argument, and the
    /// same shape as <c>CampPanel.Open</c>.</para>
    /// </remarks>
    public bool ToggleSatchel()
    {
        if (_inventory == null) return false;

        if (_inventory.IsOpen)
        {
            _inventory.Close();
            return false;
        }

        if (EncounterManager.Instance?.InFight == true)
        {
            EventBus.Instance?.EmitNotify("No time to be rummaging. Deal with this first.");
            return false;
        }

        _camp?.Close();
        _inventory.Open();
        return true;
    }

    /// <summary>
    /// Open or shut the camp menu, and take the satchel down first. Returns whether it is now open — it
    /// refuses with a line inside a fight or a pocket, so *pressed* and *opened* are different answers.
    /// </summary>
    public bool ToggleCampMenu()
    {
        if (_camp == null) return false;

        if (_camp.IsOpen)
        {
            _camp.Close();
            return false;
        }

        if (_inventory != null) _inventory.Visible = false;
        return _camp.Open();
    }

    public override void _Process(double delta)
    {
        if (_notifyTimer <= 0f) return;

        _notifyTimer -= (float)delta;
        if (_notifyTimer < 1f)
            _notifyLabel.Modulate = new Color(1, 1, 1, Mathf.Max(_notifyTimer, 0f));
    }

    // ---- Debug -----------------------------------------------------------

    public void ToggleDebugPanel(RoomView roomView)
    {
        _debug.Visible = !_debug.Visible;
        if (roomView != null) roomView.ShowHotspotOutlines = _debug.Visible;
    }
}
