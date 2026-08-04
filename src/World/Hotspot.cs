using System;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.World;

/// <summary>
/// A clickable region on the screen.
///
/// Built on Control rather than Area2D deliberately: this is a point-and-click game in
/// screen space, so Control's GUI input path is the natural fit and it sidesteps physics
/// picking configuration entirely. Arbitrary hit shapes still work — <see cref="_HasPoint"/>
/// does a point-in-polygon test when a polygon is supplied, which is the hook that lets
/// painted art get tight, non-rectangular hotspots later.
/// </summary>
public partial class Hotspot : Control
{
    public HotspotData Data { get; private set; }

    /// <summary>Overridden label/verb for auto-generated entity hotspots.</summary>
    public string Label { get; set; } = "";
    public string Verb { get; set; } = "";

    /// <summary>
    /// What this is, for the placeholder cue's colour. Kept as its own field rather than read off
    /// <see cref="Data"/> because entity hotspots are generated and have no <c>HotspotData</c>.
    /// </summary>
    public HotspotKind Kind { get; set; } = HotspotKind.Examine;

    /// <summary>
    /// Draw the unit-1.5 readability cue: kind-tinted fill, outline and <see cref="Label"/> in
    /// place. Set by <see cref="RoomView"/> when the screen has no art. Paint the screen and this
    /// goes false for it — there is nothing to remember and no way to ship the cues by accident.
    /// </summary>
    public bool ShowCue { get; set; }

    /// <summary>
    /// This hotspot exists but its flag gate is shut.
    ///
    /// <para>
    /// Locked hotspots are drawn dimmed instead of being omitted, and only while cueing — in the
    /// shipped game a gated hotspot is still absent, because an invisible ungrabbable herb reads
    /// better than a grey one (<see cref="InteractionService.IsAvailable"/>). This is a playtest
    /// affordance: it is how a flag gate gets tested by walking into the room rather than by
    /// opening the inspector.
    /// </para>
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>Which flag is holding it shut, drawn under a locked hotspot's label.</summary>
    public string LockedBy { get; set; } = "";

    /// <summary>Cue drawing is also what the debug outline toggle turns on for painted screens.</summary>
    private bool Cueing => ShowCue || DebugOutline;

    /// <summary>Set on hotspots generated from an <see cref="EntityPlacement"/>.</summary>
    public string EntityInstanceId { get; set; } = "";

    /// <summary>Raised on left click. RoomView subscribes; nothing else should.</summary>
    public event Action<Hotspot> Activated;

    private Vector2[] _localPolygon = Array.Empty<Vector2>();
    private bool _hovered;
    private bool _debugOutline;

    public bool DebugOutline
    {
        get => _debugOutline;
        set { _debugOutline = value; QueueRedraw(); }
    }

    public override void _Ready()
    {
        // A locked hotspot is a picture of a hotspot. It must not hover, must not click, and must
        // not eat the cursor for whatever is behind it — the cue is allowed to be visible without
        // being in the way.
        MouseFilter = Locked ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.PointingHand;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    /// <summary>
    /// Position and size from a region in 1920x1080 design space. When a polygon is given
    /// the region is derived from its bounding box, so authors supply one or the other.
    /// </summary>
    public void Setup(HotspotData data)
    {
        Data = data;
        Label = data.Label;
        Kind = data.Kind;
        Verb = VerbFor(data.Kind);

        var region = data.Polygon is { Length: >= 3 }
            ? BoundingBox(data.Polygon)
            : data.Region;

        Position = region.Position;
        Size = region.Size;

        // Polygon arrives in design space; hit testing happens in local space.
        if (data.Polygon is { Length: >= 3 })
        {
            _localPolygon = new Vector2[data.Polygon.Length];
            for (var i = 0; i < data.Polygon.Length; i++)
                _localPolygon[i] = data.Polygon[i] - region.Position;
        }
        else
        {
            _localPolygon = Array.Empty<Vector2>();
        }
    }

    /// <summary>Place a generated hotspot directly, used for entity overlays.</summary>
    public void SetupRect(Rect2 region, string label, string verb)
    {
        Data = null;
        Label = label;
        Kind = HotspotKind.Entity;
        Verb = verb;
        Position = region.Position;
        Size = region.Size;
        _localPolygon = Array.Empty<Vector2>();
    }

    /// <summary>
    /// The word under the cursor. <c>Look</c> is the fallback rather than an omission — a kind with no
    /// arm reads as something to examine, which is the harmless wrong answer.
    /// </summary>
    /// <remarks>
    /// Both crafting venues say <b>Craft</b>: the mortar and the camp bench do the same thing when
    /// clicked (they emit the same signal), so telling them apart in the hover text would promise the
    /// player a distinction the mechanism does not have. <c>Mend</c> is left on the <c>Look</c> fallback
    /// on purpose — a blight is not obviously repairable until you have opened one, and *"Look"* is what
    /// invites the first click without naming the verb before the game has taught it.
    /// </remarks>
    private static string VerbFor(HotspotKind kind) => kind switch
    {
        HotspotKind.Navigate => "Go",
        HotspotKind.Harvest => "Harvest",
        HotspotKind.Entity => "Approach",
        HotspotKind.Interact => "Use",
        HotspotKind.Station => "Craft",
        HotspotKind.Workbench => "Craft",
        _ => "Look",
    };

    private static Rect2 BoundingBox(Vector2[] pts)
    {
        var min = pts[0];
        var max = pts[0];
        foreach (var p in pts)
        {
            min = new Vector2(MathF.Min(min.X, p.X), MathF.Min(min.Y, p.Y));
            max = new Vector2(MathF.Max(max.X, p.X), MathF.Max(max.Y, p.Y));
        }
        return new Rect2(min, max - min);
    }

    // ---- Hit testing -----------------------------------------------------

    public override bool _HasPoint(Vector2 point)
    {
        if (_localPolygon.Length < 3)
            return new Rect2(Vector2.Zero, Size).HasPoint(point);

        return Geometry2D.IsPointInPolygon(point, _localPolygon);
    }

    // ---- Input -----------------------------------------------------------

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;

        Activated?.Invoke(this);
        AcceptEvent();
    }

    private void OnMouseEntered()
    {
        _hovered = true;
        QueueRedraw();
        EventBus.Instance?.EmitHoverTargetChanged(Label, Verb);
    }

    private void OnMouseExited()
    {
        _hovered = false;
        QueueRedraw();
        EventBus.Instance?.EmitHoverTargetChanged("", "");
    }

    public override void _ExitTree()
    {
        // A hotspot destroyed under the cursor would otherwise leave a stale hover label.
        if (_hovered) EventBus.Instance?.EmitHoverTargetChanged("", "");
    }

    // ---- Drawing ---------------------------------------------------------

    /// <summary>
    /// Three states, and only one of them outlives the placeholder.
    ///
    /// <para>
    /// <b>Hidden</b> — a painted screen, cursor elsewhere: the art is the affordance.
    /// <b>Cue</b> — an unpainted screen: outline, faint fill and the label, in the kind's colour.
    /// <b>Hovered</b> — the shipped behaviour, and the one that has to read as strongest of the
    /// three or the existing feedback is lost inside the cue.
    /// </para>
    ///
    /// <para>
    /// <b>Hover brightens the kind's own colour</b> rather than washing it with a fixed cream
    /// (Jeremy's pick of two). It reads more literally as "hover tints it", and it dissolves a
    /// collision instead of dodging it: <c>Workbench</c> gold sits very close to the old hover
    /// cream, and with no fixed hover hue there is nothing left to collide with. On a painted
    /// screen there is no kind colour, so hover falls back to that warm highlight — unchanged.
    /// </para>
    ///
    /// <para>
    /// This also fixes a bug found in play — <i>"you would see the outline of the square once you
    /// hovered but it didn't show until you did hover."</i> On an unpainted screen the hotspots
    /// were invisible until the cursor found one by luck, because the idle draw returned early.
    /// </para>
    /// </summary>
    public override void _Draw()
    {
        var cueing = Cueing;
        if (!_hovered && !cueing) return;

        var kind = Locked ? PlaceholderCues.Locked(Kind) : PlaceholderCues.ColorFor(Kind);

        Color line, fill;
        if (_hovered && cueing)
        {
            line = new Color(kind.Lightened(0.45f), 0.95f);
            fill = new Color(kind, 0.22f);
        }
        else if (_hovered)
        {
            line = new Color(1f, 0.95f, 0.82f, 0.75f);
            fill = new Color(1f, 0.95f, 0.82f, 0.06f);
        }
        else
        {
            line = new Color(kind, Locked ? 0.34f : 0.62f);
            fill = new Color(kind, Locked ? 0.04f : 0.09f);
        }

        DrawShape(fill, filled: true);
        DrawShape(line, filled: false);

        if (cueing) DrawCueText(kind);
    }

    private void DrawShape(Color colour, bool filled)
    {
        if (_localPolygon.Length >= 3)
        {
            if (filled)
            {
                DrawColoredPolygon(_localPolygon, colour);
                return;
            }

            var closed = new Vector2[_localPolygon.Length + 1];
            _localPolygon.CopyTo(closed, 0);
            closed[^1] = _localPolygon[0];
            DrawPolyline(closed, colour, 3f);
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, Size), colour, filled, filled ? -1f : 3f);
    }

    /// <summary>
    /// The label in place, and — on a door — where it goes. Drawn inside the top-left of the
    /// region rather than above it, so a hotspot near the top of the screen does not push its own
    /// name off the edge.
    /// </summary>
    private void DrawCueText(Color kind)
    {
        var alpha = Locked ? 0.72f : 1f;
        var baseline = new Vector2(8f, 26f);

        PlaceholderCues.DrawLabel(this, baseline, Label, kind.Lightened(0.25f), alpha: alpha);

        // A door's whole meaning is the other end of it, and on a grey screen that is unguessable.
        if (Kind == HotspotKind.Navigate && !string.IsNullOrEmpty(Data?.TargetRoomId))
        {
            baseline += new Vector2(0f, 24f);
            PlaceholderCues.DrawLabel(this, baseline, $"→ {Data.TargetRoomId}",
                kind, fontSize: 17, alpha: alpha * 0.9f);
        }

        // Which flag is holding it shut — the reason to draw a locked hotspot at all is so the
        // gate can be tested from inside the game, and that needs the flag's name, not just "shut".
        if (Locked && !string.IsNullOrEmpty(LockedBy))
        {
            baseline += new Vector2(0f, 24f);
            PlaceholderCues.DrawLabel(this, baseline, $"⊘ {LockedBy}",
                new Color("#ff9c9c"), fontSize: 17, alpha: 0.85f);
        }
    }
}
