using Godot;
using Unrendered.Core;
using Unrendered.Managers;
using Unrendered.World;

namespace Unrendered.UI;

/// <summary>
/// Screen furniture: where you are, which realm you are in, how much Resolve is left, what
/// the cursor is over, and the last thing that happened.
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
    private DebugPanel _debug;

    private float _notifyTimer;
    private const float NotifyHoldSeconds = 5f;

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

        OnRealmChanged((int)(RealmTransitionManager.Instance?.CurrentRealm ?? GameRealm.Waking));
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

        // -- Top right: Resolve --
        var topRight = new VBoxContainer { Name = "TopRight", MouseFilter = Control.MouseFilterEnum.Ignore };
        topRight.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        topRight.Position = new Vector2(-460, 32);
        topRight.Alignment = BoxContainer.AlignmentMode.End;
        root.AddChild(topRight);

        var resolveCaption = MakeLabel("RESOLVE", 18, new Color("#9aa6b2"));
        resolveCaption.HorizontalAlignment = HorizontalAlignment.Right;
        topRight.AddChild(resolveCaption);

        _resolve = new ResolveMeter { Name = "ResolveMeter" };
        topRight.AddChild(_resolve);

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
        var hint = MakeLabel("[Tab] spectral vision   [F3] debug   [F5] save   [F9] load",
                             16, new Color(0.62f, 0.62f, 0.66f, 0.65f));
        hint.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        hint.Position = new Vector2(40, -44);
        root.AddChild(hint);

        _dreadLabel = MakeLabel("", 28, new Color("#8a93a0"));
        _dreadLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _dreadLabel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        // Top centre: clear of the room name on the left, the meter on the right, and the
        // vanishing point in the middle of every Dream backdrop.
        _dreadLabel.Position = new Vector2(-400, 30);
        _dreadLabel.CustomMinimumSize = new Vector2(800, 0);
        root.AddChild(_dreadLabel);

        _debug = new DebugPanel { Name = "DebugPanel", Visible = false };
        root.AddChild(_debug);
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
