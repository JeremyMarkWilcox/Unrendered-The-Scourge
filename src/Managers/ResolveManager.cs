using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// Resolve. It is a clock, and an empty bar always means the same thing: you are removed
/// from the confrontation.
///
/// **As built, it only runs in the Pixel Realm** — <see cref="DreamRunManager"/> owns the
/// rate while a run is live, and returning refills it. Out here the danger is Vitality and
/// the supplies you are spending.
///
/// Per docs/combat.md §6 that is not the finished rule: Resolve moves in *two* venues, the
/// Dream and a boss fight, where empty means forced flight rather than ejection. Boss fights
/// do not exist yet, which is the only reason this manager is Dream-only today — do not read
/// the current behaviour as the design.
///
/// When they land, the drain is **conditional on the tether**: a boss still
/// <see cref="Unrendered.Core.EntityState.Corrupted"/> drains, one already
/// <see cref="Unrendered.Core.EntityState.Severed"/> does not. So the rate will need to ask
/// <see cref="EntityStateManager"/> about the entity being fought, exactly as
/// <see cref="CurrentRatePerSecond"/> currently asks <see cref="DreamRunManager"/> about the
/// run. What §6 keeps permanently is that there is **no ambient drain**: no corrupted-ground
/// pressure, no bleed for standing near something anchored, nothing outside a confrontation.
/// </summary>
public partial class ResolveManager : Node
{
    public static ResolveManager Instance { get; private set; }

    public float Current { get; private set; } = GameConfig.ResolveMax;
    public float Max => GameConfig.ResolveMax;

    /// <summary>Off until the first room is entered, and during menus.</summary>
    public bool Active { get; set; }

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Process(double delta)
    {
        if (!Active) return;

        var rate = CurrentRatePerSecond();
        if (Mathf.IsZeroApprox(rate)) return;

        SetTo(Current - rate * (float)delta);
    }

    /// <summary>
    /// Resolve spent per second. Zero everywhere except inside a heist, where the run owns
    /// the rate. Exposed so the meter can show the trend.
    /// </summary>
    public float CurrentRatePerSecond()
    {
        var run = DreamRunManager.Instance;
        return run is { IsRunning: true } ? run.CurrentDrainPerSecond() : 0f;
    }

    public void Modify(float amount) => SetTo(Current + amount);

    public void Restore(float amount) => Modify(Mathf.Abs(amount));

    /// <summary>Top the bar back up. Called on return from the Dream.</summary>
    public void Replenish(float fraction = 1f)
        => SetTo(Mathf.Max(Current, Max * Mathf.Clamp(fraction, 0f, 1f)));

    public void SetTo(float value)
    {
        var next = Mathf.Clamp(value, 0f, Max);
        if (Mathf.IsEqualApprox(next, Current)) return;

        Current = next;
        EventBus.Instance?.EmitResolveChanged(Current, Max);

        // Emptying is only possible inside a heist, and the run owns what happens next.
        if (Current <= 0f) DreamRunManager.Instance?.NotifyResolveExhausted();
    }

    public bool IsEmpty => Current <= 0f;

    public void ResetAll() => SetTo(Max);
}
