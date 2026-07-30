using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// Vitality — the body. Spent by Waking-world combat and by physical hazards, restored by her
/// finite named salves and by poor crude substitutes (docs/combat.md §6).
///
/// **It has no clock, and that is a design rule rather than an omission.** Resolve drains on a
/// wall clock in the Dream and per turn in a boss fight; Vitality moves only when something
/// wounds you or you treat a wound. So there is no <c>_Process</c> here and no rate to ask for.
/// If you are about to add ambient attrition — chip damage for standing somewhere bad — read
/// §6 first: that exact pattern was considered and rejected for Resolve on the grounds that a
/// drain the player cannot act on teaches them nothing, and the reasoning carries over. A
/// hazard that wounds you should be a *thing you can see and route around*, not a tax on
/// walking.
///
/// **Invariant 2: Vitality must never appear in the Dream.** Every Dream ending is an ejection
/// priced in Resolve, and if the body followed you in there the two-pool split collapses along
/// with the reason the two routes cost different currencies. Nothing in the Pixel Realm may
/// touch this pool; a gate asserts the bar holds still across a whole heist.
///
/// **The 0-Vitality consequence is implemented as of 2026-07-30.** This manager still does not
/// own it: <see cref="Deplete"/> announces the collapse and <see cref="CampManager"/> carries it
/// out — losing what can be remade, keeping salves, lore and relics, and waking you at the last
/// camp. Keeping the two apart is what lets Vitality stay ignorant of rooms and items.
/// </summary>
public partial class VitalityManager : Node
{
    public static VitalityManager Instance { get; private set; }

    public float Current { get; private set; } = GameConfig.VitalityMax;
    public float Max => GameConfig.VitalityMax;

    public bool IsEmpty => Current <= 0f;

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Take a wound. Sign-insensitive — pass the magnitude.</summary>
    public void Damage(float amount) => SetTo(Current - Mathf.Abs(amount));

    /// <summary>Treat a wound: one of her salves, or a crude substitute.</summary>
    public void Restore(float amount) => SetTo(Current + Mathf.Abs(amount));

    /// <summary>Signed change, for callers that already know which way it goes.</summary>
    public void Modify(float amount) => SetTo(Current + amount);

    public void SetTo(float value)
    {
        var next = Mathf.Clamp(value, 0f, Max);

        // No change, no event. This is also what stops a hazard that keeps hitting a downed
        // player from announcing the collapse over and over: once the bar is on the floor a
        // further wound cannot lower it, so it is not a change at all. No latch needed — and a
        // latch would be unreachable dead code sitting behind this line.
        if (Mathf.IsEqualApprox(next, Current)) return;

        Current = next;
        EventBus.Instance?.EmitVitalityChanged(Current, Max);

        if (Current <= 0f) Deplete();
    }

    /// <summary>
    /// The bar reached zero, and only ever on the transition down to it. What follows belongs to
    /// <see cref="CampManager"/>, which listens for the signal.
    ///
    /// The line goes out *before* the signal on purpose. The respawn is synchronous, so it speaks
    /// too — and "you come to somewhere else" has to land after "your legs go out", not before it.
    /// </summary>
    private void Deplete()
    {
        EventBus.Instance?.EmitNotify(
            "Your legs go out from under you, and the cold starts getting in.");
        EventBus.Instance?.EmitVitalityDepleted();
    }

    /// <summary>
    /// Restore a saved value. Deliberately not <see cref="SetTo"/>: a load restores state, it
    /// does not replay the events that produced that state, so loading a save taken at zero
    /// must not announce a collapse that happened before the file was written.
    /// </summary>
    public void Import(float value)
    {
        Current = Mathf.Clamp(value, 0f, Max);
        EventBus.Instance?.EmitVitalityChanged(Current, Max);
    }

    public void ResetAll() => SetTo(Max);
}
