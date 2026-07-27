using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// Resolve, drawn as a guttering bar. Colour tracks how much is left rather than how fast
/// it is going, because the player needs to read danger at a glance; the small arrow at
/// the end is what shows the trend.
/// </summary>
public partial class ResolveMeter : Control
{
    private float _current = GameConfig.ResolveMax;
    private float _max = GameConfig.ResolveMax;
    private float _displayed = GameConfig.ResolveMax;
    private float _pulse;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(420, 34);

        var bus = EventBus.Instance;
        if (bus != null) bus.ResolveChanged += OnResolveChanged;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null) bus.ResolveChanged -= OnResolveChanged;
    }

    private void OnResolveChanged(float current, float max)
    {
        _current = current;
        _max = max;
    }

    public override void _Process(double delta)
    {
        // The bar lags the value slightly so damage reads as a slide, not a jump.
        _displayed = Mathf.MoveToward(_displayed, _current, (float)delta * 45f);

        var fraction = _max > 0 ? _current / _max : 0f;
        if (fraction < 0.3f) _pulse += (float)delta * 4f;
        else _pulse = 0f;

        QueueRedraw();
    }

    public override void _Draw()
    {
        var w = Size.X;
        var h = Size.Y;
        var fraction = _max > 0 ? Mathf.Clamp(_displayed / _max, 0f, 1f) : 0f;

        DrawRect(new Rect2(0, 0, w, h), new Color(0.04f, 0.04f, 0.06f, 0.78f));

        var fill = fraction switch
        {
            > 0.6f => new Color("#8fb8a8"),
            > 0.3f => new Color("#c9a94e"),
            _ => new Color("#b1483c"),
        };

        if (_pulse > 0f)
        {
            var beat = 0.75f + 0.25f * Mathf.Sin(_pulse);
            fill = new Color(fill.R * beat, fill.G * beat, fill.B * beat, fill.A);
        }

        const float pad = 3f;
        DrawRect(new Rect2(pad, pad, (w - pad * 2f) * fraction, h - pad * 2f), fill);

        // Trend arrow: draining or recovering.
        var rate = ResolveManager.Instance?.CurrentRatePerSecond() ?? 0f;
        if (!Mathf.IsZeroApprox(rate))
        {
            var draining = rate > 0f;
            var x = w - 18f;
            var mid = h * 0.5f;
            var tip = draining ? mid + 7f : mid - 7f;
            var tail = draining ? mid - 7f : mid + 7f;

            DrawColoredPolygon(
                new[] { new Vector2(x - 6f, tail), new Vector2(x + 6f, tail), new Vector2(x, tip) },
                draining ? new Color("#d97b6c") : new Color("#8fd0a8"));
        }

        DrawRect(new Rect2(0, 0, w, h), new Color(0.75f, 0.72f, 0.66f, 0.45f), filled: false, width: 2f);
    }
}
