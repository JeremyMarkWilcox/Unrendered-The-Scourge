using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// What the three Dream verbs cost while you hold them. Unit 8, built to `combat.md` §19 — Suppress, Float
/// and Flare all **drain while held**, and all three draw on one Resolve meter.
///
/// <para><b>This is the cost, not the act.</b> Floating, putting the light out and pushing it back are
/// movement and rendering, which belong to the 3D pass. The contract is two methods the Dream layer calls —
/// <see cref="BeginHold"/> when the button goes down, <see cref="EndHold"/> when it comes up — and everything
/// about what that spends lives here. It exists before the movement does on purpose: otherwise the movement
/// gets written with no cost attached and the meter is bolted on afterwards.</para>
///
/// <para><b>Not an autoload.</b> It holds a set of held verbs and nothing else, and it is asked for a rate by
/// <see cref="ResolveManager"/> rather than doing anything on its own — so a static class is honest where an
/// eleventh registered node would be ceremony. Same call <c>MendManager</c> made.</para>
/// </summary>
public static class DreamVerbs
{
    private static readonly HashSet<DreamVerb> Held = new();

    /// <summary>Which verbs are being held right now. For the gates and for a readout.</summary>
    public static IReadOnlyCollection<DreamVerb> HeldVerbs => Held;

    /// <summary>Whether that verb is being held this instant. Distinct from *knowing* it.</summary>
    public static bool IsHolding(DreamVerb verb) => Held.Contains(verb);

    /// <summary>
    /// Start spending on a verb. Returns whether the hold began.
    /// </summary>
    /// <remarks>
    /// <b>Three refusals, and each one is a rule rather than a nicety.</b>
    /// <list type="bullet">
    /// <item><b>Not outside a pocket.</b> The verbs are Dream verbs; §6 is permanent that there is no ambient
    /// drain, so a verb held in the Waking world would be exactly that.</item>
    /// <item><b>Not a verb you have not been taught</b> — <see cref="PocketManager.Holds"/> is the one place
    /// that knows, including that Suppress needs no flag.</item>
    /// <item><b>Not on an empty bar.</b> Otherwise a held verb re-triggers the ejection every frame while the
    /// player is already being carried to a camp.</item>
    /// </list>
    /// </remarks>
    public static bool BeginHold(DreamVerb verb)
    {
        if (verb == DreamVerb.None) return false;
        if (!PocketManager.InsideTheDream) return false;
        if (!PocketManager.Holds(verb)) return false;
        if (ResolveManager.Instance is null or { IsEmpty: true }) return false;

        return Held.Add(verb);
    }

    /// <summary>Stop spending on a verb. Returns whether it had been held.</summary>
    public static bool EndHold(DreamVerb verb) => Held.Remove(verb);

    /// <summary>
    /// Resolve per second for everything currently held. <b>Additive</b>, so holding Float and Suppress at
    /// once costs both.
    /// </summary>
    /// <remarks>
    /// Additive rather than "the most expensive one", because §19's whole design for the verbs is that they
    /// compete for one meter — *"floating spends the same meter suppression does, so the climb is paid for out
    /// of the hiding"*. Taking the max would delete that sentence: you could hide for free while climbing.
    /// </remarks>
    public static float RatePerSecond() =>
        Held.Sum(verb => verb switch
        {
            DreamVerb.Suppress => GameConfig.SuppressDrainPerSecond,
            DreamVerb.Float => GameConfig.FloatDrainPerSecond,
            DreamVerb.Flare => GameConfig.FlareDrainPerSecond,
            _ => 0f,
        });

    /// <summary>
    /// Drop every hold. Called when a pocket ends by any route, and by the gates.
    /// </summary>
    /// <remarks>
    /// <b>Necessary rather than tidy.</b> A verb held at the moment you are ejected would still be in this set
    /// afterwards, and <see cref="RatePerSecond"/> does not ask where you are — so the Waking world would
    /// quietly drain, which is the one thing §6 rules out permanently. <c>PocketManager.Return</c> calls this
    /// on every exit, so there is no path that leaves a hold behind.
    /// </remarks>
    public static void ReleaseAll() => Held.Clear();
}
