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
        MouseFilter = MouseFilterEnum.Stop;
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
        Verb = verb;
        Position = region.Position;
        Size = region.Size;
        _localPolygon = Array.Empty<Vector2>();
    }

    private static string VerbFor(HotspotKind kind) => kind switch
    {
        HotspotKind.Navigate => "Go",
        HotspotKind.Harvest => "Harvest",
        HotspotKind.Entity => "Approach",
        HotspotKind.Interact => "Use",
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

    public override void _Draw()
    {
        if (!_hovered && !DebugOutline) return;

        var colour = _hovered
            ? new Color(1f, 0.95f, 0.82f, 0.75f)
            : new Color(0.35f, 0.85f, 1f, 0.30f);

        if (_localPolygon.Length >= 3)
        {
            var closed = new Vector2[_localPolygon.Length + 1];
            _localPolygon.CopyTo(closed, 0);
            closed[^1] = _localPolygon[0];
            DrawPolyline(closed, colour, 3f);
        }
        else
        {
            DrawRect(new Rect2(Vector2.Zero, Size), colour, filled: false, width: 3f);
        }

        if (_hovered)
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(1f, 0.95f, 0.82f, 0.06f));
    }
}
