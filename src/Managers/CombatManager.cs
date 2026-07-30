using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// The wound ledger and the damage resolver — the arithmetic half of a fight, with no turn machine
/// and no UI. Built to docs/combat.md §18, which is the authority.
///
/// <para>Two things live here and nothing else does, deliberately. **How much a hit is worth**
/// (§18's 2× / 0.5× against the target's own vulnerability, which replaced Defence as a number), and
/// **how much a body has taken so far** — because that second one is not a number a fight can own.
/// Wounds outlive the engagement when the body is <see cref="EntityState.Severed"/> and close when it
/// is still <see cref="EntityState.Corrupted"/>, so the ledger has to sit above whatever the
/// encounter turns out to be.</para>
///
/// <para>It is registered last among the autoloads, after <see cref="CampManager"/>, for the same
/// reason that one is: it asks other managers questions rather than answering them. It reads entity
/// state and never writes it — severing and purifying stay <see cref="EntityStateManager"/>'s
/// business, so there is exactly one place that decides what a body *is*.</para>
///
/// <para>No <c>_Process</c>, and there must never be one. Same argument as
/// <see cref="VitalityManager"/>: a clock inside a fight would make turns stop being the only
/// currency (§18), and it is the easiest violation to add by accident.</para>
/// </summary>
public partial class CombatManager : Node
{
    public static CombatManager Instance { get; private set; }

    /// <summary>
    /// Accumulated damage per entity instance id. Absent means unwounded — the dictionary is kept
    /// sparse rather than pre-filled so that "has this ever been fought?" stays answerable, and so a
    /// save from a world where nothing has been touched is empty rather than a list of zeroes.
    /// </summary>
    private readonly Dictionary<string, int> _wounds = new();

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ---- What a hit is worth ---------------------------------------------

    /// <summary>
    /// §18's whole difficulty surface: the right type does double, the wrong one half, anything else
    /// stands. Static because it is a pure function of the two arguments — a gate can ask it without
    /// a scene tree, and nothing about it depends on who is fighting or when.
    /// </summary>
    /// <remarks>
    /// The order of the two tests matters in exactly one case, and it is an authoring mistake rather
    /// than a design decision: a creature authored as both vulnerable and resistant to the same type
    /// would take double here. That combination is a content-sweep gate failure
    /// (<c>--verify-slice</c>), which is the right place to catch it — silently averaging the two, or
    /// picking the kinder arm, would make a contradictory `.tres` look like it worked.
    /// </remarks>
    public static int DamageAgainst(DamageType incoming, EntityData target, int potency)
    {
        if (potency <= 0 || incoming == DamageType.None || target == null) return 0;

        var scale = 1f;
        if (incoming == target.VulnerableTo) scale = GameConfig.VulnerableMultiplier;
        else if (incoming == target.ResistantTo) scale = GameConfig.ResistantMultiplier;

        // Floor of 1: a halved weak hit must still be a hit. Rounding 0.5 × 1 down to nothing would
        // make the wrong damage type read as *immunity*, which is the exact thing §7 forbids — and it
        // would do it only at the low end, so it would look like a bug rather than a rule.
        return Mathf.Max(1, Mathf.RoundToInt(potency * scale));
    }

    // ---- The ledger ------------------------------------------------------

    /// <summary>Damage standing against a body. Zero for anything never fought.</summary>
    public int WoundsOn(string instanceId) =>
        string.IsNullOrEmpty(instanceId) ? 0 : _wounds.GetValueOrDefault(instanceId, 0);

    /// <summary>
    /// Apply a typed hit and return what it actually took off, after the multiplier. The return value
    /// is the *dealt* amount rather than the new total, because that is what a fight narrates.
    /// </summary>
    public int Wound(string instanceId, EntityData data, DamageType incoming, int potency)
    {
        if (string.IsNullOrEmpty(instanceId) || data == null) return 0;

        var dealt = DamageAgainst(incoming, data, potency);
        if (dealt <= 0) return 0;

        // Clamped at durability rather than allowed to run past it. Overkill is expensive in
        // *materials* (§5), never a bigger number — and an unclamped ledger would make the condition
        // ladder and any future "how nearly dead is it" readout depend on how hard the last hit was.
        _wounds[instanceId] = Mathf.Min(WoundsOn(instanceId) + dealt, Mathf.Max(0, data.Durability));
        return dealt;
    }

    /// <summary>How much body is left. Never negative.</summary>
    public int RemainingDurability(string instanceId, EntityData data) =>
        data == null ? 0 : Mathf.Max(0, data.Durability - WoundsOn(instanceId));

    /// <summary>Whether the body has taken everything it can take.</summary>
    public bool IsSpent(string instanceId, EntityData data) =>
        data != null && data.Durability > 0 && WoundsOn(instanceId) >= data.Durability;

    /// <summary>
    /// Wipe the ledger for one body. This is the *anchored* half of the asymmetry — see
    /// <see cref="SettleEngagement"/>, which is what callers should use, because it knows when
    /// closing is correct and this does not.
    /// </summary>
    public void CloseWounds(string instanceId)
    {
        if (!string.IsNullOrEmpty(instanceId)) _wounds.Remove(instanceId);
    }

    /// <summary>
    /// End-of-engagement bookkeeping for one body, and the single place the persistence rule lives:
    ///
    /// <list type="bullet">
    /// <item><b>Corrupted</b> — wounds close. Something is still animating it, so it regenerates, and
    /// overkill has to be a single-sitting burst.</item>
    /// <item><b>Severed</b> — wounds stay open. A husk has nothing closing them, so cleanup can be
    /// spread across visits.</item>
    /// </list>
    ///
    /// <para>That asymmetry *is* the reward for severing, and it is the reason it is worth a method of
    /// its own: if damage reset in both cases, severing would only remove regeneration from inside a
    /// fight, which pushes the design's two routes together. Leave a body at 40 anchored and it is
    /// back to full; leave it at 40 severed and it is still 40. §3's teaching moment is that
    /// contrast.</para>
    ///
    /// <para>Called per body when an engagement ends <i>however</i> it ended — won, fled or collapsed.
    /// Fleeing must not be a way to bank progress against an anchored creature, so there is
    /// deliberately no outcome parameter here: the body's state decides, not the player's exit.</para>
    /// </summary>
    public void SettleEngagement(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;
        if (EntityStateManager.Instance?.GetState(instanceId) == EntityState.Severed) return;

        CloseWounds(instanceId);
    }

    // ---- The condition ladder --------------------------------------------

    /// <summary>
    /// §5's ladder, derived from what is left rather than authored. Total by construction — every
    /// fraction from 0 to 1 lands in exactly one band, so there is no creature and no wound total that
    /// has no word for it.
    /// </summary>
    public Condition ConditionOf(string instanceId, EntityData data)
    {
        if (data == null || data.Durability <= 0) return Condition.Unmarked;

        // Asked before any hit lands, this must be Unmarked rather than a computed band, and the
        // guard is not redundant: a full bar divides to exactly 1.0 and would fall through the
        // comparisons correctly, but only by arithmetic luck. Saying it outright means the answer
        // survives someone re-tuning BleedingBelow to 1.0.
        var wounds = WoundsOn(instanceId);
        if (wounds <= 0) return Condition.Unmarked;

        var left = (float)RemainingDurability(instanceId, data) / data.Durability;

        if (left <= GameConfig.CollapsingBelow) return Condition.Collapsing;
        if (left <= GameConfig.FalteringBelow) return Condition.Faltering;
        if (left <= GameConfig.BleedingBelow) return Condition.Bleeding;
        return Condition.Unmarked;
    }

    /// <summary>The shared vocabulary, used by any creature that does not override it.</summary>
    private static readonly string[] DefaultWords =
    {
        "unmarked", "bleeding", "faltering", "collapsing",
    };

    /// <summary>
    /// The word for a body's condition — the only thing a fight ever reports about how hurt something
    /// is. Falls back to the shared vocabulary when <see cref="EntityData.ConditionWords"/> is empty,
    /// and also when it is the wrong length, so that a mis-authored array degrades to readable prose
    /// in play. It is still a gate failure; the fallback is there so the *game* stays legible while
    /// the suite is what complains.
    /// </summary>
    public string DescribeCondition(string instanceId, EntityData data)
    {
        var band = (int)ConditionOf(instanceId, data);
        var authored = data?.ConditionWords;

        if (authored != null && authored.Count == DefaultWords.Length
            && !string.IsNullOrWhiteSpace(authored[band]))
            return authored[band];

        return DefaultWords[band];
    }

    // ---- Persistence -----------------------------------------------------

    /// <summary>
    /// A plain dictionary, matching every other manager's export. §13's save-format question needed no
    /// version bump for exactly this reason: a save written before wounds existed deserialises to an
    /// empty ledger, and an empty ledger is the correct reading of a world where nothing had been
    /// fought yet.
    /// </summary>
    public Dictionary<string, int> ExportWounds() => new(_wounds);

    public void ImportWounds(Dictionary<string, int> wounds)
    {
        _wounds.Clear();
        if (wounds == null) return;

        foreach (var kv in wounds)
        {
            // Zero and negative entries are dropped rather than stored. Absent is how this ledger
            // spells "unwounded", and admitting a second spelling would mean every reader had to
            // handle both.
            if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0) _wounds[kv.Key] = kv.Value;
        }
    }

    /// <summary>For the gates and for starting a new game.</summary>
    public void ResetAll() => _wounds.Clear();
}
