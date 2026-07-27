using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.World;

/// <summary>
/// The one scene that renders every screen in the game. Give it a <see cref="RoomData"/>
/// and it assembles backdrops, entity overlays and hotspots for both realms; change any
/// world state and it reassembles itself from signals.
///
/// The Waking layer sits underneath at full opacity and the Dream layer fades in over it,
/// rather than crossfading both — that avoids the mid-transition moment where two
/// half-transparent layers let the clear colour show through.
/// </summary>
public partial class RoomView : Control
{
    // Static structure, authored in RoomView.tscn.
    private PlaceholderBackdrop _wakingBackdrop;
    private TextureRect _wakingPhoto;
    private Node2D _wakingEntities;

    private Control _dreamLayer;
    private PlaceholderBackdrop _dreamBackdrop;
    private TextureRect _dreamPhoto;
    private Node2D _dreamEntities;

    private Control _hotspotLayer;
    private ColorRect _fade;

    // Dynamic content, rebuilt per room / per state change.
    private readonly Dictionary<string, EntityOverlay> _wakingOverlays = new();
    private readonly Dictionary<string, EntityOverlay> _dreamOverlays = new();
    private readonly List<Hotspot> _hotspots = new();

    private RoomData _room;
    private Tween _realmTween;
    private Tween _fadeTween;

    /// <summary>Draws every hotspot boundary. Toggled from the debug panel.</summary>
    public bool ShowHotspotOutlines
    {
        get => _showOutlines;
        set
        {
            _showOutlines = value;
            foreach (var h in _hotspots) h.DebugOutline = value;
        }
    }
    private bool _showOutlines;

    /// <summary>Dream art is authored at 480x270 and displayed at 1920x1080.</summary>
    private static float DreamToScreen =>
        GameConfig.WakingResolution.X / (float)GameConfig.DreamResolution.X;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _wakingBackdrop = GetNode<PlaceholderBackdrop>("WakingLayer/Backdrop");
        _wakingPhoto = GetNode<TextureRect>("WakingLayer/Photo");
        _wakingEntities = GetNode<Node2D>("WakingLayer/Entities");

        _dreamLayer = GetNode<Control>("DreamLayer");
        _dreamBackdrop = GetNode<PlaceholderBackdrop>("DreamLayer/DreamContainer/DreamViewport/Backdrop");
        _dreamPhoto = GetNode<TextureRect>("DreamLayer/DreamContainer/DreamViewport/Photo");
        _dreamEntities = GetNode<Node2D>("DreamLayer/DreamContainer/DreamViewport/Entities");

        _hotspotLayer = GetNode<Control>("Hotspots");
        _fade = GetNode<ColorRect>("Fade");

        var startInDream = RealmTransitionManager.Instance?.IsDream ?? false;
        _dreamLayer.Modulate = new Color(1, 1, 1, startInDream ? 1f : 0f);
        _dreamLayer.Visible = startInDream;

        var bus = EventBus.Instance;
        if (bus == null)
        {
            GD.PushError("[RoomView] EventBus autoload missing.");
            return;
        }

        bus.RoomEntered += OnRoomEntered;
        bus.RealmChanged += OnRealmChanged;
        bus.EntityStateChanged += OnEntityStateChanged;
        bus.WorldFlagChanged += OnWorldFlagChanged;
        bus.ZonePhaseChanged += OnZonePhaseChanged;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.RoomEntered -= OnRoomEntered;
        bus.RealmChanged -= OnRealmChanged;
        bus.EntityStateChanged -= OnEntityStateChanged;
        bus.WorldFlagChanged -= OnWorldFlagChanged;
        bus.ZonePhaseChanged -= OnZonePhaseChanged;
    }

    // ---- Signal handlers -------------------------------------------------

    private void OnRoomEntered(string roomId)
    {
        _room = NavigationManager.Instance?.GetRoom(roomId);
        if (_room == null) return;

        BuildEntities();
        RefreshBackdrops();
        RebuildHotspots();
        PlayRoomFade();
    }

    private void OnRealmChanged(int realm)
    {
        CrossfadeTo((GameRealm)realm);

        // Hotspot visibility and entity hit-boxes are both realm-dependent.
        RebuildHotspots();
    }

    private void OnEntityStateChanged(string entityId, int newState)
    {
        // Overlays refresh themselves off the same signal; the view only has to redo the
        // hit-boxes, which move and resize as a creature changes form.
        RefreshBackdrops();
        RebuildHotspots();
    }

    private void OnWorldFlagChanged(string flag, bool value) => RebuildHotspots();

    private void OnZonePhaseChanged(string zoneId, int phase)
    {
        RefreshBackdrops();
        RebuildHotspots();
    }

    // ---- Backdrops -------------------------------------------------------

    private void RefreshBackdrops()
    {
        if (_room == null) return;

        var cleansed = WorldStateManager.Instance?.IsZoneCleansed(_room.ZoneId) ?? false;

        // -- Waking --
        var photo = cleansed && _room.WakingBackgroundPurified != null
            ? _room.WakingBackgroundPurified
            : _room.WakingBackground;

        _wakingPhoto.Texture = photo;
        _wakingPhoto.Visible = photo != null;
        _wakingBackdrop.Visible = photo == null;

        if (photo == null)
        {
            var style = _room.PlaceholderStyle;

            if (cleansed)
            {
                _wakingBackdrop.Configure(style,
                    GameConfig.PurifiedSky, GameConfig.PurifiedMid, GameConfig.PurifiedGround,
                    new Color("#fff3d6"), fog: 0.05f, seedSource: _room.Id);
            }
            else
            {
                _wakingBackdrop.Configure(style,
                    GameConfig.WakingSky, GameConfig.WakingMid, GameConfig.WakingGround,
                    GameConfig.WakingFog, fog: 0.35f + _room.BaseCorruption * 0.65f,
                    seedSource: _room.Id);
            }
        }

        // -- Dream -- untouched by purification; the Pixel Realm is always the Pixel Realm.
        _dreamPhoto.Texture = _room.DreamBackground;
        _dreamPhoto.Visible = _room.DreamBackground != null;
        _dreamBackdrop.Visible = _room.DreamBackground == null;

        if (_room.DreamBackground == null)
        {
            _dreamBackdrop.Configure(BackdropStyle.Dream,
                GameConfig.DreamSky, GameConfig.DreamMid, GameConfig.DreamGround,
                GameConfig.DreamAccent, fog: 0f, seedSource: _room.Id);
        }
    }

    // ---- Entities --------------------------------------------------------

    private void BuildEntities()
    {
        foreach (var o in _wakingOverlays.Values) o.QueueFree();
        foreach (var o in _dreamOverlays.Values) o.QueueFree();
        _wakingOverlays.Clear();
        _dreamOverlays.Clear();

        if (_room == null) return;

        foreach (var placement in _room.Entities)
        {
            if (placement?.Entity == null || string.IsNullOrEmpty(placement.InstanceId))
            {
                GD.PushWarning($"[RoomView] Room '{_room.Id}' has an incomplete entity placement.");
                continue;
            }

            var waking = new EntityOverlay { Name = $"W_{placement.InstanceId}" };
            _wakingEntities.AddChild(waking);
            waking.Setup(placement, GameRealm.Waking);
            _wakingOverlays[placement.InstanceId] = waking;

            var dream = new EntityOverlay { Name = $"D_{placement.InstanceId}" };
            _dreamEntities.AddChild(dream);
            dream.Setup(placement, GameRealm.Dream);
            _dreamOverlays[placement.InstanceId] = dream;
        }
    }

    // ---- Hotspots --------------------------------------------------------

    private void RebuildHotspots()
    {
        foreach (var h in _hotspots)
        {
            h.Activated -= OnHotspotActivated;
            h.QueueFree();
        }
        _hotspots.Clear();

        if (_room == null) return;

        var realm = RealmTransitionManager.Instance?.CurrentRealm ?? GameRealm.Waking;

        AddAuthoredHotspots(realm);
        AddEntityHotspots(realm);
    }

    private void AddAuthoredHotspots(GameRealm realm)
    {
        foreach (var data in _room.Hotspots)
        {
            if (!InteractionService.IsAvailable(data, realm)) continue;

            var hotspot = new Hotspot { Name = $"HS_{data.Id}" };
            _hotspotLayer.AddChild(hotspot);
            hotspot.Setup(data);
            hotspot.DebugOutline = _showOutlines;
            hotspot.Activated += OnHotspotActivated;
            _hotspots.Add(hotspot);
        }
    }

    /// <summary>
    /// Entity hotspots are generated from the overlay's own bounds rather than authored.
    /// The alternative — hand-placing a rect over each creature, per realm — means keeping
    /// two coordinate spaces in sync by hand forever, and getting it silently wrong.
    /// </summary>
    private void AddEntityHotspots(GameRealm realm)
    {
        var overlays = realm == GameRealm.Dream ? _dreamOverlays : _wakingOverlays;

        foreach (var (instanceId, overlay) in overlays)
        {
            var presentation = overlay.Presentation;
            if (presentation is not { Visible: true, Interactable: true }) continue;

            var bounds = overlay.Bounds();
            if (realm == GameRealm.Dream)
                bounds = new Rect2(bounds.Position * DreamToScreen, bounds.Size * DreamToScreen);

            // Small figures are hard to click; give everything a usable minimum.
            const float minSide = 90f;
            if (bounds.Size.X < minSide || bounds.Size.Y < minSide)
            {
                var centre = bounds.GetCenter();
                var size = new Vector2(Mathf.Max(bounds.Size.X, minSide), Mathf.Max(bounds.Size.Y, minSide));
                bounds = new Rect2(centre - size * 0.5f, size);
            }

            var hotspot = new Hotspot { Name = $"HS_ENT_{instanceId}", EntityInstanceId = instanceId };
            _hotspotLayer.AddChild(hotspot);
            hotspot.SetupRect(bounds, presentation.Label, presentation.Verb);
            hotspot.DebugOutline = _showOutlines;
            hotspot.Activated += OnHotspotActivated;
            _hotspots.Add(hotspot);
        }
    }

    // ---- Interaction -----------------------------------------------------

    /// <summary>
    /// Clicks are forwarded straight to <see cref="InteractionService"/>. The view holds no
    /// game rules of its own; it will hear about any consequences through the EventBus like
    /// everything else does.
    /// </summary>
    private void OnHotspotActivated(Hotspot hotspot)
    {
        if (!string.IsNullOrEmpty(hotspot.EntityInstanceId))
            InteractionService.ActivateEntity(hotspot.EntityInstanceId);
        else
            InteractionService.ActivateHotspot(hotspot.Data);
    }

    // ---- Transitions -----------------------------------------------------

    private void CrossfadeTo(GameRealm realm)
    {
        var toDream = realm == GameRealm.Dream;

        _realmTween?.Kill();
        if (toDream) _dreamLayer.Visible = true;

        _realmTween = CreateTween();
        _realmTween.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
        _realmTween.TweenProperty(_dreamLayer, "modulate:a", toDream ? 1f : 0f,
                                  GameConfig.RealmCrossfadeSeconds);

        // Stop rendering the SubViewport contents once they are fully hidden.
        if (!toDream)
            _realmTween.TweenCallback(Callable.From(() => _dreamLayer.Visible = false));
    }

    private void PlayRoomFade()
    {
        _fadeTween?.Kill();
        _fade.Color = new Color(0, 0, 0, 1);

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(_fade, "color:a", 0f, GameConfig.RoomTransitionSeconds);
    }
}
