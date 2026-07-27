using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// Resolve. It is a clock, and it only runs in the Pixel Realm.
///
/// The Waking world does not touch it at all — no ambient drain, no corrupted-ground
/// pressure, no slow bleed for standing next to something anchored. Out there the danger is
/// Vitality and the supplies you are spending, and Resolve is simply how long you can stay
/// on the other side. Returning from a heist refills it.
///
/// One resource, one meaning, one place. An empty bar can only ever mean one thing: the
/// heist is over. See <see cref="DreamRunManager"/>, which owns the drain rate while a run
/// is live.
/// </summary>
public partial class SanityManager : Node
{
    public static SanityManager Instance { get; private set; }

    public float Current { get; private set; } = GameConfig.SanityMax;
    public float Max => GameConfig.SanityMax;

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
        EventBus.Instance?.EmitSanityChanged(Current, Max);

        // Emptying is only possible inside a heist, and the run owns what happens next.
        if (Current <= 0f) DreamRunManager.Instance?.NotifyResolveExhausted();
    }

    public bool IsEmpty => Current <= 0f;

    public void ResetAll() => SetTo(Max);
}
