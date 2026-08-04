using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.World;

/// <summary>
/// The one scene that renders every screen in the game. Give it a <see cref="RoomData"/>
/// and it assembles a backdrop, entity overlays and hotspots; change any world state and it
/// reassembles itself from signals.
///
/// <para>
/// It rendered two stacked layers until 2026-08-02 — a Waking one at full opacity and a Dream one
/// crossfading over it out of a 480x270 SubViewport, with a second set of overlays and a second
/// coordinate space. The great swap deleted the upper half. What is left is a single painted
/// screen, which is what a Waking screen was always meant to be.
/// </para>
/// </summary>
public partial class RoomView : Control
{
    // Static structure, authored in RoomView.tscn.
    private PlaceholderBackdrop _backdrop;
    private TextureRect _photo;
    private Node2D _entityLayer;

    private Control _hotspotLayer;
    private RoomCard _card;
    private ColorRect _fade;

    /// <summary>
    /// True when this screen has no painted art, which is the only condition the unit-1.5 cues key
    /// on. Deliberately derived from what is actually on screen rather than from a debug flag:
    /// paint the room and every cue in it switches itself off, with nothing to remember and no way
    /// to ship them by accident.
    /// </summary>
    private bool _unpainted = true;

    // Dynamic content, rebuilt per room / per state change.
    private readonly Dictionary<string, EntityOverlay> _overlays = new();
    private readonly List<Hotspot> _hotspots = new();

    private RoomData _room;
    private Tween _fadeTween;

    /// <summary>
    /// The room currently on screen. Exposed so the gates can assert what the player can
    /// actually see, rather than only what <see cref="NavigationManager"/> believes — the two
    /// came apart once, and nothing headless noticed.
    /// </summary>
    public string DisplayedRoomId => _room?.Id ?? "";

    /// <summary>
    /// The hotspot nodes actually on screen. Exposed for the same reason as
    /// <see cref="DisplayedRoomId"/>: the gates need to assert what the player can see and click,
    /// not what the data says should be there. It matters more since unit 1.5, because the view now
    /// instantiates hotspots that <see cref="InteractionService.IsAvailable"/> refuses — a shut
    /// gate drawn as a picture — and "drawn but inert" is only true if something checks it.
    /// </summary>
    public IReadOnlyList<Hotspot> LiveHotspots => _hotspots;

    /// <summary>Whether this screen is currently drawing the unit-1.5 placeholder cues.</summary>
    public bool CuesVisible => _unpainted || _showOutlines;

    /// <summary>
    /// Forces the placeholder cues on. Toggled from the debug panel, and the manual override for
    /// screens that <i>do</i> have art — a painting can hide a clickable region as well as a grey
    /// box can.
    ///
    /// <para>
    /// This rebuilds rather than just redrawing, because turning it on changes which hotspots
    /// exist: shut ones are only instantiated while cueing.
    /// </para>
    /// </summary>
    public bool ShowHotspotOutlines
    {
        get => _showOutlines;
        set
        {
            if (_showOutlines == value) return;
            _showOutlines = value;
            RebuildHotspots();
        }
    }
    private bool _showOutlines;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        _backdrop = GetNode<PlaceholderBackdrop>("WorldLayer/Backdrop");
        _photo = GetNode<TextureRect>("WorldLayer/Photo");
        _entityLayer = GetNode<Node2D>("WorldLayer/Entities");

        _hotspotLayer = GetNode<Control>("Hotspots");
        _fade = GetNode<ColorRect>("Fade");

        // Created in code rather than authored into RoomView.tscn: it is debug furniture with no
        // layout to tune by eye, so the `%UniqueName` scene contract buys nothing here. Slotted
        // directly under the fade so a room transition covers it like everything else.
        _card = new RoomCard { Name = "RoomCard" };
        AddChild(_card);
        MoveChild(_card, _fade.GetIndex());

        var bus = EventBus.Instance;
        if (bus == null)
        {
            GD.PushError("[RoomView] EventBus autoload missing.");
            return;
        }

        bus.RoomEntered += OnRoomEntered;
        bus.EntityStateChanged += OnEntityStateChanged;
        bus.WorldFlagChanged += OnWorldFlagChanged;

        // `RealmChanged` was subscribed here too. Nothing this view draws depends on the realm any
        // more — not the backdrop, not the overlays, not which hotspots exist — so the handler had
        // no work left to do. The signal itself survives for the HUD, which still names where you are.
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.RoomEntered -= OnRoomEntered;
        bus.EntityStateChanged -= OnEntityStateChanged;
        bus.WorldFlagChanged -= OnWorldFlagChanged;
    }

    // ---- Signal handlers -------------------------------------------------

    private void OnRoomEntered(string roomId)
    {
        // Never paint a room the player has already left. A subscriber ahead of us in this
        // signal's list can navigate again before we are reached — being caught ejects you
        // from inside this very emit — and the newer RoomEntered then arrives first, leaving
        // the stale one to land last. Trusting the payload there would draw the Dream room
        // you were just yanked out of, with the realm and the run already gone: no drain, no
        // exits, and no way to tell what happened. See handoff.md, "Closed post-mortem: the
        // ejection failure," and invariant 13.
        if (NavigationManager.Instance?.CurrentRoomId != roomId) return;

        _room = NavigationManager.Instance?.GetRoom(roomId);
        if (_room == null) return;

        BuildEntities();
        RefreshBackdrops();
        RebuildHotspots();
        PlayRoomFade();
    }

    private void OnEntityStateChanged(string entityId, int newState)
    {
        // Overlays refresh themselves off the same signal; the view only has to redo the
        // hit-boxes, which move and resize as a creature changes form.
        RefreshBackdrops();
        RebuildHotspots();
    }

    /// <summary>
    /// A flag moved, so both the hit-boxes and the backdrop may be stale.
    /// </summary>
    /// <remarks>
    /// This refreshed only the hotspots until 2026-08-03, because the backdrop was driven by
    /// <c>ZonePhaseChanged</c> — a separate signal, now deleted. Cleansing is a world flag
    /// (<c>screen.&lt;id&gt;.cleansed</c>), so leaving this line as it was would have set the flag and
    /// not repainted until you left the room and came back, which is the exact failure the deleted
    /// signal was hiding. Unconditional rather than filtered on the flag name: a repaint is cheap, and a
    /// filter here would be a second place that has to know how cleansed flags are spelled.
    /// </remarks>
    private void OnWorldFlagChanged(string flag, bool value)
    {
        RefreshBackdrops();
        RebuildHotspots();
    }

    // ---- Backdrop --------------------------------------------------------

    private void RefreshBackdrops()
    {
        if (_room == null) return;

        // Per screen since 2026-08-03, not per chapter. Read from the flag rather than by asking
        // `MendManager.IsCleansed(_room)` directly: the flag is what content gates on, so painting from
        // it means a screen whose colour has come back and a door that opens "once this screen is
        // cleansed" can never disagree about whether it has.
        var cleansed = WorldStateManager.Instance?.GetFlag(Flags.Cleansed(_room.Id)) ?? false;

        var photo = cleansed && _room.WakingBackgroundPurified != null
            ? _room.WakingBackgroundPurified
            : _room.WakingBackground;

        _photo.Texture = photo;
        _photo.Visible = photo != null;
        _backdrop.Visible = photo == null;
        _unpainted = photo == null;

        if (photo == null)
        {
            var style = _room.PlaceholderStyle;

            if (cleansed)
            {
                _backdrop.Configure(style,
                    GameConfig.PurifiedSky, GameConfig.PurifiedMid, GameConfig.PurifiedGround,
                    new Color("#fff3d6"), fog: 0.05f, seedSource: _room.Id);
            }
            else
            {
                _backdrop.Configure(style,
                    GameConfig.WakingSky, GameConfig.WakingMid, GameConfig.WakingGround,
                    GameConfig.WakingFog, fog: 0.35f + _room.BaseCorruption * 0.65f,
                    seedSource: _room.Id);
            }
        }
    }

    // ---- Entities --------------------------------------------------------

    private void BuildEntities()
    {
        foreach (var o in _overlays.Values) o.QueueFree();
        _overlays.Clear();

        if (_room == null) return;

        foreach (var placement in _room.Entities)
        {
            if (placement?.Entity == null || string.IsNullOrEmpty(placement.InstanceId))
            {
                GD.PushWarning($"[RoomView] Room '{_room.Id}' has an incomplete entity placement.");
                continue;
            }

            var overlay = new EntityOverlay { Name = $"E_{placement.InstanceId}" };
            _entityLayer.AddChild(overlay);
            overlay.Setup(placement);
            _overlays[placement.InstanceId] = overlay;
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

        AddAuthoredHotspots();
        AddEntityHotspots();

        // Rebuilt alongside the hotspots because its exit list is drawn from them, and because a
        // flag change can open a door without the room changing.
        _card?.Show(_room, _unpainted || _showOutlines);
    }

    /// <summary>
    /// <para>
    /// <b>Shut hotspots are drawn while cueing, and only then.</b> In the shipped game a gated
    /// hotspot is simply absent — an invisible ungrabbable herb reads better than a grey one. But
    /// on an unpainted screen that makes a flag gate untestable without the inspector, so the
    /// placeholder layer shows it dimmed with the name of the flag holding it. See
    /// <see cref="Hotspot.Locked"/>.
    /// </para>
    /// <para>
    /// <b>Spent harvest nodes are not "locked", they are gone.</b> `IsAvailable` refuses a hotspot
    /// for two different reasons and only one of them is interesting here: a shut gate is a thing
    /// waiting to open, while a harvested node is finished. Drawing the second forever would fill
    /// every cleansed screen with the ghosts of things you already picked.
    /// </para>
    /// </summary>
    private void AddAuthoredHotspots()
    {
        var cueing = _unpainted || _showOutlines;

        foreach (var data in _room.Hotspots)
        {
            if (data == null) continue;

            var available = InteractionService.IsAvailable(data);
            var locked = false;

            if (!available)
            {
                if (!cueing || !IsFlagShut(data)) continue;
                locked = true;
            }

            // Set before AddChild: `_Ready` reads `Locked` to decide whether this thing takes the
            // mouse at all, and AddChild runs it synchronously.
            var hotspot = new Hotspot
            {
                Name = $"HS_{data.Id}",
                Locked = locked,
                LockedBy = locked ? ShutBy(data) : "",
                ShowCue = _unpainted,
            };
            _hotspotLayer.AddChild(hotspot);
            hotspot.Setup(data);
            hotspot.DebugOutline = _showOutlines;
            if (!locked) hotspot.Activated += OnHotspotActivated;
            _hotspots.Add(hotspot);
        }
    }

    /// <summary>Whether a hotspot is unavailable because a flag gate is shut, rather than spent.</summary>
    private static bool IsFlagShut(HotspotData data)
    {
        var world = WorldStateManager.Instance;

        if (!string.IsNullOrEmpty(data.RequiredFlag) &&
            world?.GetFlag(data.RequiredFlag) != data.RequiredFlagValue)
            return true;

        if (data.RequiredFlags != null)
            foreach (var flag in data.RequiredFlags)
                if (string.IsNullOrEmpty(flag) || world?.GetFlag(flag) != true)
                    return true;

        return false;
    }

    /// <summary>The first unmet flag, named. One is enough to go and look at.</summary>
    private static string ShutBy(HotspotData data)
    {
        var world = WorldStateManager.Instance;

        if (!string.IsNullOrEmpty(data.RequiredFlag) &&
            world?.GetFlag(data.RequiredFlag) != data.RequiredFlagValue)
            return data.RequiredFlagValue ? data.RequiredFlag : $"not {data.RequiredFlag}";

        if (data.RequiredFlags != null)
            foreach (var flag in data.RequiredFlags)
            {
                if (string.IsNullOrEmpty(flag)) return "(blank flag)";
                if (world?.GetFlag(flag) != true) return flag;
            }

        return "";
    }

    /// <summary>
    /// Entity hotspots are generated from the overlay's own bounds rather than authored.
    /// The alternative — hand-placing a rect over each creature — means keeping the rect and the
    /// figure in sync by hand forever, and getting it silently wrong as the figure changes form.
    ///
    /// <para>
    /// This is also where unit 1.5's "name over the silhouette" comes from, rather than from a
    /// second draw in <see cref="EntityOverlay"/>: the hotspot's rect *is* the figure's bounds and
    /// its label is already <c>EntityData.NameFor(state)</c>, so drawing it here puts the name on
    /// the figure with nothing to keep in sync. It follows that an entity that is visible but not
    /// interactable would go unlabelled — no state resolves that way today, and if one ever does,
    /// the name draw belongs in the overlay at that point.
    /// </para>
    /// </summary>
    private void AddEntityHotspots()
    {
        foreach (var (instanceId, overlay) in _overlays)
        {
            var presentation = overlay.Presentation;
            if (presentation is not { Visible: true, Interactable: true }) continue;

            var bounds = overlay.Bounds();

            // Small figures are hard to click; give everything a usable minimum.
            const float minSide = 90f;
            if (bounds.Size.X < minSide || bounds.Size.Y < minSide)
            {
                var centre = bounds.GetCenter();
                var size = new Vector2(Mathf.Max(bounds.Size.X, minSide), Mathf.Max(bounds.Size.Y, minSide));
                bounds = new Rect2(centre - size * 0.5f, size);
            }

            var hotspot = new Hotspot
            {
                Name = $"HS_ENT_{instanceId}",
                EntityInstanceId = instanceId,
                ShowCue = _unpainted,
            };
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

    // `CrossfadeTo` — the 0.35s sine fade that brought the Dream layer up over the Waking one, and
    // switched the SubViewport off once it was fully hidden. Deleted 2026-08-02. The only transition
    // left is the room fade below.

    private void PlayRoomFade()
    {
        _fadeTween?.Kill();
        _fade.Color = new Color(0, 0, 0, 1);

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(_fade, "color:a", 0f, GameConfig.RoomTransitionSeconds);
    }
}
