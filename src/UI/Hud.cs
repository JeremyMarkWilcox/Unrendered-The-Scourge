using Godot;
using Unrendered.Core;
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
    private Label _dreadLabel;
    private ResolveMeter _resolve;
    private VitalityMeter _vitality;
    private VBoxContainer _poolColumn;
    private VBoxContainer _resolveGroup;
    private VBoxContainer _vitalityGroup;
    private DebugPanel _debug;
    private InventoryPanel _inventory;

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
        bus.PursuerProximityChanged += OnPursuerProximityChanged;
        bus.HeistStarted += OnHeistStarted;
        bus.HeistEnded += OnHeistEnded;

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
        bus.PursuerProximityChanged -= OnPursuerProximityChanged;
        bus.HeistStarted -= OnHeistStarted;
        bus.HeistEnded -= OnHeistEnded;
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
        var hint = MakeLabel("[I] satchel   [F3] debug   [F5] save   [F9] load",
                             16, new Color(0.62f, 0.62f, 0.66f, 0.65f));
        hint.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        hint.Position = new Vector2(40, -44);
        root.AddChild(hint);

        _dreadLabel = MakeLabel("", 28, new Color("#8a93a0"));
        _dreadLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dreadLabel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        // Top centre: clear of the room name on the left and the meter on the right.
        _dreadLabel.Position = new Vector2(-400, 30);
        _dreadLabel.CustomMinimumSize = new Vector2(800, 0);
        root.AddChild(_dreadLabel);

        _debug = new DebugPanel { Name = "DebugPanel", Visible = false };
        root.AddChild(_debug);

        // Added last so it draws over the screen furniture, including the debug panel. Authored as
        // a scene rather than built here, per handoff §2 — grids and slots are layout you need to
        // see while tuning, and this is the first of three panels.
        var scene = ResourceLoader.Load<PackedScene>("res://scenes/ui/InventoryPanel.tscn");
        if (scene == null)
        {
            GD.PushError("Hud: could not load InventoryPanel.tscn — the satchel will not open.");
            return;
        }

        _inventory = scene.Instantiate<InventoryPanel>();
        root.AddChild(_inventory);
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

        var inDreamRun = DreamRunManager.Instance?.IsRunning == true;

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

    private void OnPursuerProximityChanged(int roomsAway)
    {
        _hoverLabel.Text = "";

        _dreadLabel.Text = roomsAway switch
        {
            0 => "IT IS HERE",
            1 => "something is moving in the next room",
            2 => "footsteps, not far",
            3 => "you can hear it somewhere",
            < 0 => "",
            _ => "the colonnade is quiet",
        };

        _dreadLabel.AddThemeColorOverride("font_color", roomsAway switch
        {
            0 => new Color("#d94f3c"),
            1 => new Color("#d9873c"),
            2 => new Color("#c9a94e"),
            _ => new Color("#8a93a0"),
        });
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
