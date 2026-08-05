using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// Resolve. It is a clock, and an empty bar always means the same thing: you are removed
/// from the confrontation.
///
/// **As built, nothing drains it at all** — the per-second heist clock retired with
/// <c>DreamRunManager</c> in unit 6b, and it was the wrong model anyway. §19 makes the meter a **fuel tank
/// for three verbs**, spent while Suppress, Float or Flare is *held*, so the rate comes back from the Dream
/// side rather than from a timer.
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
/// <see cref="CurrentRatePerSecond"/> is the seam both will report through — the held verbs and the boss
/// aura. What §6 keeps permanently is that there is **no ambient drain**: no corrupted-ground
/// pressure, no bleed for standing near something anchored, nothing outside a confrontation.
/// </summary>
public partial class ResolveManager : Node
{
    public static ResolveManager Instance { get; private set; }

    public float Current { get; private set; } = GameConfig.ResolveMax;

    /// <summary>
    /// The ceiling, which <b>rises</b> — one point per Dream knowledge pocket, eight in the whole game
    /// (`build-plan.md` §6 rule 6). See <see cref="RaiseMax"/>.
    /// </summary>
    /// <remarks>
    /// <b>A field since unit 6, and it was <c>=> GameConfig.ResolveMax</c> before — a const, so nothing
    /// could raise it and the eight grants had no mechanism at all.</b> Worth naming as a class of
    /// omission rather than a bug: the *content* was budgeted, counted and gated as a rule long before
    /// anything could pay it out, and a rule about a number nothing can change reads as satisfied. The
    /// const survives as the starting ceiling, which is all it was ever right about.
    /// <para>
    /// <b>Vitality has no twin of this and must never grow one</b> — <c>spirit-light-and-resolve</c>: the
    /// body is a fixed thing you spend and mend, only the spirit grows. `GameConfig.VitalityMax`'s own
    /// comment already says as much.
    /// </para>
    /// </remarks>
    public float Max { get; private set; } = GameConfig.ResolveMax;

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
    /// Resolve spent per second. <b>Zero everywhere, as of unit 6b</b> — the heist owned the only rate in
    /// the game and it retired with the class. Exposed so the meter can show a trend when there is one.
    /// </summary>
    /// <remarks>
    /// <b>Kept as a stub rather than deleted, and the reason is that two readers already draw it</b>
    /// (<c>ResolveMeter</c>'s trend, <c>DebugPanel</c>'s readout) and two designed mechanisms will supply
    /// it: the boss aura, which drains only while a boss's tether is unbroken (`combat.md` §6), and the
    /// Dream's own abilities, which spend rather than tick.
    /// <para>
    /// <b>The two checks that used to sit over this were deleted rather than re-pointed, deliberately.</b>
    /// They read *"no drain outside a run"* and *"the bar holds still in the Waking world"*, and with no
    /// rate source anywhere in the game they are now true of a method that returns a constant — a universal
    /// over an empty set, which is a wish rather than a check. The rule they encoded survives in prose,
    /// where it is honest: **there is no ambient drain, and never was one.** They come back with the first
    /// mechanism that can violate them, which is the boss aura.
    /// </para>
    /// </remarks>
    public float CurrentRatePerSecond() => DreamVerbs.RatePerSecond();

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

        if (Current <= 0f) Deplete();
    }

    /// <summary>
    /// The nerve reached zero, and only ever on the transition down to it — the guard is the
    /// <c>IsEqualApprox</c> early return above, exactly as in <see cref="VitalityManager"/>, so a further
    /// drain on an empty bar is not a change and cannot announce a second collapse. **No latch, because a
    /// latch behind that line would be unreachable dead code.**
    /// </summary>
    /// <remarks>
    /// The line goes out *before* the signal, for <see cref="VitalityManager"/>'s reason: the respawn is
    /// synchronous and speaks too, and *"you come to somewhere else"* has to land after *"your nerve goes"*
    /// rather than before it.
    /// </remarks>
    private void Deplete()
    {
        EventBus.Instance?.EmitNotify("Your nerve goes all at once, and the dark takes the rest.");
        EventBus.Instance?.EmitResolveDepleted();
    }

    public bool IsEmpty => Current <= 0f;

    // ---- The ceiling (unit 6) --------------------------------------------

    /// <summary>
    /// Raise the ceiling permanently, and fill the new room immediately. Returns whether it moved.
    /// </summary>
    /// <remarks>
    /// <b>Only a Dream knowledge pocket may call this</b>, one point each, eight in the game — the rule
    /// `build-plan.md` §6 counts as its own bill line. The caller is <c>PocketManager.Grant</c>, and it is
    /// guarded there by <c>Flags.PocketCleared</c>, so a re-entry cannot stack a second point.
    /// <para>
    /// <b>The new headroom arrives full rather than empty</b>, and that is a decision. A grant that raised
    /// the ceiling and left the bar where it was would read as *nothing happened* at the moment it landed —
    /// the payoff for a whole pocket, invisible until the next time you spent Resolve. Filling it makes the
    /// reward legible in the frame it is given, and costs nothing the design cares about: Resolve is
    /// refilled free at every camp anyway (§17), so the alternative is not scarcity, only confusion.
    /// </para>
    /// </remarks>
    public bool RaiseMax(float amount)
    {
        if (amount <= 0f) return false;

        Max += amount;
        SetTo(Current + amount);

        // `SetTo` stays silent when the bar was already full, so the ceiling moving would go unannounced —
        // and the bar's *shape* changed even when its value did not. Emitted unconditionally for that
        // reason: every meter reads `(Current, Max)` as a pair.
        EventBus.Instance?.EmitResolveChanged(Current, Max);
        return true;
    }

    /// <summary>Restore a saved ceiling. Silent, like every other import: a load is not an achievement.</summary>
    public void ImportMax(float max) =>
        Max = max > 0f ? Mathf.Max(max, GameConfig.ResolveMax) : GameConfig.ResolveMax;

    /// <summary>
    /// For a new game and the gates. <b>Resets the ceiling too</b> — a raised Max surviving into a fresh
    /// game would be the one piece of progress that outlived the save it belonged to.
    /// </summary>
    public void ResetAll()
    {
        Max = GameConfig.ResolveMax;
        SetTo(Max);
    }
}
