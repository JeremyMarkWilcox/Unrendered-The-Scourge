using Godot;
using Unrendered.Core;

namespace Unrendered.UI;

/// <summary>
/// Vitality, drawn as a bar of meat rather than a guttering flame.
///
/// Deliberately a separate class from <see cref="ResolveMeter"/> and not a shared base with
/// virtuals: the two bars agree on about a dozen lines of rectangle drawing and disagree on
/// everything that matters — palette, whether there is a trend arrow at all (Vitality has no
/// rate to trend), and what low means. Two short classes each readable top to bottom beat one
/// abstract one, which is the same reasoning that keeps <c>Hud</c> in code.
///
/// The palette is warm — bone through rust — so the body reads at a glance as a different
/// quantity from Resolve's cold teal. They converge on the same red when nearly empty, because
/// red already means "about to lose" everywhere else on screen and that vocabulary is worth
/// more than the distinction.
/// </summary>
public partial class VitalityMeter : Control
{
    private float _current = GameConfig.VitalityMax;
    private float _max = GameConfig.VitalityMax;
    private float _displayed = GameConfig.VitalityMax;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(420, 26);

        var bus = EventBus.Instance;
        if (bus != null) bus.VitalityChanged += OnVitalityChanged;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null) bus.VitalityChanged -= OnVitalityChanged;
    }

    private void OnVitalityChanged(float current, float max)
    {
        _current = current;
        _max = max;
    }

    public override void _Process(double delta)
    {
        // Slide rather than jump, so a wound reads as one event you can watch land.
        _displayed = Mathf.MoveToward(_displayed, _current, (float)delta * 45f);
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
            > 0.6f => new Color("#c4a186"),
            > 0.3f => new Color("#c07a4e"),
            _ => new Color("#b1483c"),
        };

        const float pad = 3f;
        DrawRect(new Rect2(pad, pad, (w - pad * 2f) * fraction, h - pad * 2f), fill);

        DrawRect(new Rect2(0, 0, w, h), new Color(0.75f, 0.72f, 0.66f, 0.45f), filled: false, width: 2f);
    }
}
