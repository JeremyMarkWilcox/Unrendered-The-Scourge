using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// The turn machine <see cref="CombatManager"/> deliberately does not have. Built to docs/combat.md
/// §18, which is the authority on every rule below.
///
/// <para><b>It computes no damage.</b> Every number comes from
/// <see cref="CombatManager.DamageAgainst"/> and <see cref="CombatManager.Wound"/>, so §18's 2× / 0.5×
/// keeps exactly one home and a fight cannot quietly disagree with a gate that tests the resolver
/// directly. What lives here instead is *sequence*: who acts, what an action costs, and what the end of
/// a fight writes down.</para>
///
/// <para><b>No <c>_Process</c>, and there must never be one</b> — the same prohibition
/// <see cref="CombatManager"/> and <see cref="VitalityManager"/> carry, and this is the class where
/// breaking it would be easiest to justify and worst to live with. §18 makes the turn the only currency
/// inside a fight; a clock would be a second one.</para>
///
/// <para>Registered last among the autoloads, after <see cref="CombatManager"/>, because it asks every
/// other manager questions and answers none of theirs.</para>
///
/// <para><b>Battles are 1v1</b> — Jeremy, <i>"as of now and probably always"</i> — so
/// <see cref="OpponentId"/> is one string rather than a list. That is a deliberate narrowing of §18's
/// wording and not an oversight: a list of length one invites gates that cannot fail, and the
/// alternative shape is a rewrite of this class rather than a field on it.</para>
/// </summary>
public partial class EncounterManager : Node
{
    public static EncounterManager Instance { get; private set; }

    /// <summary>
    /// Traps sitting on a creature's rig sites, keyed by placement instance id, in the order they were
    /// rigged — which is the order they fire in.
    /// </summary>
    /// <remarks>
    /// Keyed by placement rather than by room, which is §18's most load-bearing staging decision:
    /// <i>"rig sites hang off the creature, not off the room"</i>, so no <c>RoomData</c> or
    /// <c>HotspotData</c> field corresponds to any of this and all 59 rooms were authorable before this
    /// file existed. A <see cref="List{T}"/> rather than a set because order is meaningful and the cap
    /// is two.
    /// </remarks>
    private readonly Dictionary<string, List<string>> _rigs = new();

    /// <summary>The live fight, or "" when there is none.</summary>
    private string _opponentId = "";

    private bool _playersTurn = true;
    private int _turn;
    private int _salvesUsed;

    public override void _EnterTree() => Instance = this;

    public override void _Ready()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.VitalityDepleted += OnCollapsed;
        bus.RoomEntered += OnRoomEntered;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.VitalityDepleted -= OnCollapsed;
            bus.RoomEntered -= OnRoomEntered;
        }

        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// A fight does not survive the screen it happens on.
    ///
    /// <para><b>Found by <c>--shots</c>, not by a gate</b> (2026-08-03). Engaging and then clicking a
    /// door left the fight live for the rest of the run: the panel stayed open across two chapters,
    /// still titled after a creature nobody was standing next to, and — because
    /// <see cref="NavigationManager.FindPlacement"/> only ever searches the current room — it degraded to
    /// printing the raw instance id, because its own opponent no longer resolved. Every gate in the
    /// <c>encounters</c> section stands in one room, so none of them could see it.</para>
    ///
    /// <para><b>This charges nothing, and that is the load-bearing part.</b> A collapse navigates too:
    /// <see cref="CampManager.RespawnAtCamp"/> runs inside <c>VitalityDepleted</c> and carries you to a
    /// camp, which fires this handler <i>before</i> <see cref="OnCollapsed"/> gets control (see the note
    /// there on autoload order). If leaving cost a parting blow here, a death would be billed for two.
    /// So the cost lives on the door instead — <c>InteractionService</c>'s <c>Navigate</c> case flees
    /// deliberately, which is a player's act — and this stays a backstop that no navigation path can get
    /// around, including a load and the camp menu.</para>
    /// </summary>
    private void OnRoomEntered(string roomId)
    {
        if (!InFight) return;
        EndFight(_opponentId, "");
    }

    /// <summary>
    /// The body gave out mid-fight, so there is no fight any more.
    ///
    /// <para><b>A signal rather than a test of the bar, and the difference is not stylistic.</b>
    /// <see cref="CampManager.RespawnAtCamp"/> runs from inside <c>VitalityDepleted</c> and refills both
    /// pools before the handler returns — so by the time <see cref="EndPlayerTurn"/> gets control back,
    /// <c>IsEmpty</c> is <c>false</c> again and a collapse looks exactly like a blow you survived. That
    /// version left the player standing in a camp, still committed to a creature two rooms away, whose
    /// placement no longer resolves.</para>
    /// </summary>
    /// <remarks>
    /// Registered after <c>CampManager</c>'s handler, because this manager is last among the autoloads —
    /// so the respawn has already happened when this runs. That is harmless and worth stating: nothing
    /// here reads the room or the bar, only the id, and <see cref="EndFight"/> settles by instance id.
    /// No line, because the collapse announced itself.
    /// </remarks>
    private void OnCollapsed()
    {
        if (!InFight) return;
        EndFight(_opponentId, "");
    }

    // ---- Readouts, for the panel and the gates ---------------------------

    /// <summary>Whether a fight is running. Reads the id rather than a bool of its own.</summary>
    public bool InFight => !string.IsNullOrEmpty(_opponentId);

    /// <summary>The placement being fought, or "".</summary>
    public string OpponentId => _opponentId;

    /// <summary>Whose move it is. Meaningless outside a fight, and true there so nothing reads as stuck.</summary>
    public bool PlayersTurn => _playersTurn;

    /// <summary>
    /// Player turns taken this fight, counted from 1 on the first action. §5 wants 3–6, and this is the
    /// number that says whether it got them — so it is a readout rather than an internal.
    /// </summary>
    public int TurnNumber => _turn;

    /// <summary>Salves spent this fight, against <see cref="GameConfig.SalveSlotsPerFight"/>.</summary>
    public int SalvesUsed => _salvesUsed;

    /// <summary>What is on a creature's sites. Empty for a creature never rigged against.</summary>
    public IReadOnlyList<string> RiggedAgainst(string instanceId) =>
        !string.IsNullOrEmpty(instanceId) && _rigs.TryGetValue(instanceId, out var rig)
            ? rig
            : System.Array.Empty<string>();

    // ---- Rigging ---------------------------------------------------------

    /// <summary>
    /// Why <see cref="Rig"/> would refuse, without spending a turn or a trap. Same inspect/act split as
    /// <c>InventoryCraftingManager.InspectUse</c>, for the same reason: the button and the act must ask
    /// one question.
    /// </summary>
    public RigBlock InspectRig(string instanceId, string itemId)
    {
        var inv = InventoryCraftingManager.Instance;
        if (inv == null || string.IsNullOrEmpty(instanceId)) return RigBlock.NoSuchItem;

        var item = inv.GetItem(itemId);
        if (item == null) return RigBlock.NoSuchItem;
        if (!inv.Has(itemId)) return RigBlock.NotHeld;
        if (item.Category != ItemCategory.Trap) return RigBlock.NotATrap;

        var rig = RiggedAgainst(instanceId);
        if (rig.Contains(itemId)) return RigBlock.AlreadyRigged;
        if (rig.Count >= GameConfig.RigSlotsPerFight) return RigBlock.SlotsFull;

        return RigBlock.None;
    }

    /// <summary>Whether rigging this here would work. See <see cref="InspectRig"/>.</summary>
    public bool CanRig(string instanceId, string itemId) =>
        InspectRig(instanceId, itemId) == RigBlock.None;

    /// <summary>
    /// Put a trap on one of a creature's sites.
    ///
    /// <para><b>Free before the fight, a turn inside it</b> — §18's one sentence, and the only place in
    /// this class where the same call costs different things. Nothing is consumed either way: traps went
    /// permanent on 2026-08-01 (§19), so rigging *names* a trap you own rather than spending one, and
    /// <c>SurvivesDeath</c> keeps it through a collapse.</para>
    /// </summary>
    /// <remarks>
    /// Rigging mid-fight needed no rule of its own, which is §18's point about the turn currency: it is
    /// an action, so it costs an action. At 3–6 turns that is usually a bad trade and occasionally the
    /// right desperate move, which is a decision rather than something to memorise.
    /// </remarks>
    public bool Rig(string instanceId, string itemId)
    {
        var fightingThis = InFight && _opponentId == instanceId;

        // Mid-fight, it is only legal on your own turn — like every other action.
        if (fightingThis && !_playersTurn) return false;

        switch (InspectRig(instanceId, itemId))
        {
            case RigBlock.NoSuchItem:
            case RigBlock.NotHeld:
                return false;
            case RigBlock.NotATrap:
                EventBus.Instance?.EmitNotify("Not something you can set as a trap.");
                return false;
            case RigBlock.AlreadyRigged:
                EventBus.Instance?.EmitNotify("That one is already set.");
                return false;
            case RigBlock.SlotsFull:
                EventBus.Instance?.EmitNotify(
                    $"Nowhere left to set it — {GameConfig.RigSlotsPerFight} at a time.");
                return false;
        }

        if (!_rigs.TryGetValue(instanceId, out var rig))
        {
            rig = new List<string>();
            _rigs[instanceId] = rig;
        }

        rig.Add(itemId);
        EventBus.Instance?.EmitNotify($"Set: {InventoryCraftingManager.Instance?.DisplayNameOf(itemId)}");

        if (fightingThis) EndPlayerTurn();
        return true;
    }

    /// <summary>
    /// Take a trap back off a site. Free outside a fight — the rigging phase has no timer, so changing
    /// your mind is not a cost — and a turn inside one, like everything else.
    /// </summary>
    public bool Unrig(string instanceId, string itemId)
    {
        var fightingThis = InFight && _opponentId == instanceId;
        if (fightingThis && !_playersTurn) return false;

        if (!_rigs.TryGetValue(instanceId, out var rig) || !rig.Remove(itemId)) return false;
        if (rig.Count == 0) _rigs.Remove(instanceId);

        if (fightingThis) EndPlayerTurn();
        return true;
    }

    // ---- Committing ------------------------------------------------------

    /// <summary>
    /// Why <see cref="Engage"/> would refuse. What the confirm step consults before it offers the
    /// choice, since §18 makes fights opt-in.
    /// </summary>
    public EngageBlock InspectEngage(string instanceId)
    {
        if (InFight) return EngageBlock.AlreadyFighting;

        var placement = NavigationManager.Instance?.FindPlacement(instanceId);
        if (placement?.Entity == null) return EngageBlock.NoSuchCreature;

        var state = EntityStateManager.Instance?.GetState(instanceId) ?? EntityState.Corrupted;
        if (state == EntityState.Purified) return EngageBlock.NotHostile;

        var combat = CombatManager.Instance;
        if (combat != null && combat.IsSpent(instanceId, placement.Entity)) return EngageBlock.Spent;

        return EngageBlock.None;
    }

    /// <summary>Whether a fight could start here. See <see cref="InspectEngage"/>.</summary>
    public bool CanEngage(string instanceId) => InspectEngage(instanceId) == EngageBlock.None;

    /// <summary>
    /// The confirm line, and <b>the one place §7's legibility promise gets paid</b>. §18 requires it to
    /// name <i>both</i> halves — what the thing is weak to, and what it deals — because the second half
    /// is what makes the armour choice a decision instead of a coin flip.
    /// </summary>
    /// <remarks>
    /// Never a number. §5's condition bands are the only readout of how hurt something is, and the same
    /// rule applies to how hard it hits: the player learns "it comes at you with edges" and picks a coat,
    /// rather than reading 8 and doing arithmetic. It is safe to state a *type* outright because a type
    /// is an access question with an answer you can go and craft, which is the Zelda-tools shape
    /// invariant 5 asks for.
    /// </remarks>
    public string DescribeOpponent(string instanceId)
    {
        var placement = NavigationManager.Instance?.FindPlacement(instanceId);
        var data = placement?.Entity;
        if (data == null) return "";

        var state = EntityStateManager.Instance?.GetState(instanceId) ?? EntityState.Corrupted;
        var name = data.NameFor(state);
        var condition = CombatManager.Instance?.DescribeCondition(instanceId, data) ?? "unmarked";

        var parts = new List<string> { $"{name} — {condition}." };

        if (data.VulnerableTo != DamageType.None)
            parts.Add($"It flinches from {Words(data.VulnerableTo)}.");
        if (data.ResistantTo != DamageType.None)
            parts.Add($"It barely notices {Words(data.ResistantTo)}.");
        if (data.Deals != DamageType.None)
            parts.Add($"It comes at you with {Words(data.Deals)}.");

        // The tether, stated last because it is the thing that changes what winning means. §2: the
        // anchor is what closes wounds between visits, so a severed body is a different proposition
        // from an anchored one and the player must be able to see which they are looking at.
        parts.Add(state == EntityState.Severed
            ? "Whatever was holding on has let go — what you do to it stays done."
            : "Something still has hold of it. Finish this in one sitting or not at all.");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Plain words for a damage type. Prose rather than the enum member, because the enum is a
    /// programmer's vocabulary and this string is read by a player.
    /// </summary>
    /// <remarks>
    /// <b>Public since 2026-08-03, so the fight screen's verbs speak this vocabulary and not a second
    /// one.</b> <see cref="DescribeOpponent"/> says *"It barely notices weight"* and the strike button
    /// said *"Strike — bare hands"*: everything needed to work out that bare hands are the wrong tool
    /// was on screen, in two vocabularies that never met. A private copy in <c>FightPanel</c> would have
    /// closed the loop and then drifted the first time one of these words was reworded.
    /// </remarks>
    public static string Words(DamageType type) => type switch
    {
        DamageType.Edge => "edges",
        DamageType.Blunt => "weight",
        DamageType.Fire => "fire",
        DamageType.Caustic => "her caustics",
        _ => "nothing in particular",
    };

    /// <summary>
    /// Start the fight. <b>Every trap that sets itself off fires before turn 1</b>, in rig order — which
    /// is what §18's <c>TriggersItself</c> column means by <i>"itself, when the thing moves"</i>, and the
    /// reason placement is free. Oil and pitch stay on their sites until <see cref="Trigger"/> spends a
    /// turn on them.
    /// </summary>
    public bool Engage(string instanceId)
    {
        switch (InspectEngage(instanceId))
        {
            case EngageBlock.NoSuchCreature:
                return false;
            case EngageBlock.AlreadyFighting:
                EventBus.Instance?.EmitNotify("One thing at a time.");
                return false;
            case EngageBlock.NotHostile:
                return false;
            case EngageBlock.Spent:
                // Not a refusal the player should have to work around: the body is finished, so finish
                // it. This is the husk-cleanup path §3 describes, reached without a fight around it.
                return Finish(instanceId);
        }

        _opponentId = instanceId;
        _playersTurn = true;
        _turn = 0;
        _salvesUsed = 0;

        var placement = NavigationManager.Instance?.FindPlacement(instanceId);
        EventBus.Instance?.EmitNotify($"You commit. {DescribeOpponent(instanceId)}");
        EventBus.Instance?.EmitFightStarted(instanceId);

        // Fire the self-triggering rig. Copied first: SpringTrap can end the fight, which clears the
        // dictionary entry this loop is walking.
        foreach (var trapId in RiggedAgainst(instanceId).ToList())
        {
            var trap = InventoryCraftingManager.Instance?.GetItem(trapId);
            if (trap == null || !trap.TriggersItself) continue;

            SpringTrap(instanceId, placement?.Entity, trap);
            if (!InFight) return true; // it died on the opening. A legitimate way to win.
        }

        return true;
    }

    // ---- The four verbs. Each one ends your turn. -------------------------

    /// <summary>
    /// Hit it with what you are holding — <b>or with nothing, which is the point</b>. §18 keeps a weak
    /// baseline attack so that a player out of items has a fight with verbs rather than a fight with
    /// none, and names this as the home of the one-click <i>"finish the Severed body"</i> that used to
    /// live in <c>InteractionService</c>.
    /// </summary>
    /// <remarks>
    /// Bare-handed is <see cref="GameConfig.BareHandedType"/> at <see cref="GameConfig.BareHandedPotency"/>.
    /// A real type rather than <c>None</c> because <c>DamageAgainst</c> returns 0 for <c>None</c> — a typeless attack
    /// would be no attack at all, which is the one outcome §18 rules out. It means bare hands can be
    /// resisted and can even be a creature's vulnerability, and both readings are fine: fists are weight.
    /// </remarks>
    public bool Strike()
    {
        if (!InFight || !_playersTurn) return false;

        var placement = NavigationManager.Instance?.FindPlacement(_opponentId);
        var data = placement?.Entity;
        var combat = CombatManager.Instance;
        if (data == null || combat == null) return false;

        var weapon = InventoryCraftingManager.Instance?.EquippedWeapon;
        var type = weapon?.Deals ?? GameConfig.BareHandedType;
        var potency = weapon != null && weapon.Potency > 0 ? weapon.Potency : GameConfig.BareHandedPotency;

        var dealt = combat.Wound(_opponentId, data, type, potency);
        var with = weapon != null ? InventoryCraftingManager.Instance.DisplayNameOf(weapon.Id) : "Bare hands";

        EventBus.Instance?.EmitNotify(dealt > 0
            ? $"{with}. It is {combat.DescribeCondition(_opponentId, data)}."
            : $"{with}, and it does not touch it.");

        if (ResolveIfSpent(placement.InstanceId, data)) return true;

        EndPlayerTurn();
        return true;
    }

    /// <summary>
    /// Set off a trap that will not set itself off — oil, pitch, powder. Costs your turn, which is the
    /// whole distinction §18 draws, and is legible from what the object is rather than from a flag the
    /// player has to recall.
    /// </summary>
    public bool Trigger(string itemId)
    {
        if (!InFight || !_playersTurn) return false;
        if (!RiggedAgainst(_opponentId).Contains(itemId)) return false;

        var trap = InventoryCraftingManager.Instance?.GetItem(itemId);
        if (trap == null) return false;

        if (trap.TriggersItself)
        {
            // Already fired, on engage. A refusal rather than a second firing, because a trap that can
            // be spent twice is a damage multiplier and §11 has no damage multipliers.
            EventBus.Instance?.EmitNotify("That one has already done its work.");
            return false;
        }

        var placement = NavigationManager.Instance?.FindPlacement(_opponentId);
        SpringTrap(_opponentId, placement?.Entity, trap);

        if (!InFight) return true;

        EndPlayerTurn();
        return true;
    }

    /// <summary>
    /// Treat a wound mid-fight, against <see cref="GameConfig.SalveSlotsPerFight"/>.
    ///
    /// <para><b>The cap is on uses, not on a picked loadout.</b> Same number either way, and this is the
    /// version that does not need a screen: §19 wanted a slot limit rather than a carry limit so that a
    /// pickup is never wasted, and counting uses inside the fight delivers exactly that. The two caps
    /// deliberately do not arbitrate against each other — two salves and two traps, and nothing
    /// trades.</para>
    /// </summary>
    public bool Treat(string itemId)
    {
        if (!InFight || !_playersTurn) return false;

        var inv = InventoryCraftingManager.Instance;
        if (inv == null) return false;

        if (_salvesUsed >= GameConfig.SalveSlotsPerFight)
        {
            EventBus.Instance?.EmitNotify(
                $"No hands for another — {GameConfig.SalveSlotsPerFight} to a fight.");
            return false;
        }

        // Routed through UseItem rather than reimplemented, so that "nothing to treat" refuses here
        // exactly as it refuses in the satchel and her finite salves cannot be burned for nothing in
        // one place and protected in the other.
        if (!inv.UseItem(itemId)) return false;

        _salvesUsed++;
        EndPlayerTurn();
        return true;
    }

    /// <summary>
    /// Leave. <b>Guaranteed</b> — no roll, no "you cannot escape" — with §3's parting strike as the
    /// whole cost. This is the softlock protection for the entire overkill route, which is what lets
    /// damage types be sharp without being dangerous.
    /// </summary>
    /// <remarks>
    /// The strike lands before the fight ends, so fleeing at low Vitality can kill you. That is the
    /// price being real rather than nominal; death itself is cheap (§19) and routes through the camp.
    /// <see cref="CombatManager.SettleEngagement"/> then decides whether the damage you did survives —
    /// and it takes no outcome parameter on purpose, so fleeing is never a way to bank progress against
    /// an anchored body.
    /// </remarks>
    public bool Flee()
    {
        if (!InFight) return false;

        var instanceId = _opponentId;
        var placement = NavigationManager.Instance?.FindPlacement(instanceId);

        EventBus.Instance?.EmitNotify("You break off.");
        CreatureStrike(placement?.Entity, parting: true);

        // The parting blow can be the one that puts you down, and then OnCollapsed has already ended
        // this. Guarded rather than settled twice: you did get away, and saying so on top of the
        // collapse line would be the wrong last word.
        if (!InFight) return true;

        EndFight(instanceId, "You are out of its reach.");
        return true;
    }

    // ---- Turn machinery --------------------------------------------------

    /// <summary>
    /// Hand the turn over, let the creature act, and hand it back. One method because there is exactly
    /// one opponent and it always acts — so the "creature turn" is a step rather than a state anything
    /// can be stuck in.
    /// </summary>
    private void EndPlayerTurn()
    {
        if (!InFight) return;

        _turn++;
        _playersTurn = false;

        var placement = NavigationManager.Instance?.FindPlacement(_opponentId);
        CreatureStrike(placement?.Entity, parting: false);

        // Its blow may have collapsed you, in which case the fight is already over: the bar's own
        // signal reaches OnCollapsed from inside Damage, above. So the test is whether a fight is still
        // running rather than whether the bar is empty — CampManager refills it on the way down, so by
        // this line it reads Max again either way.
        if (!InFight) return;

        _playersTurn = true;
    }

    /// <summary>
    /// The creature's whole turn: it hits you for what it deals, halved if the coat you are wearing turns
    /// that type aside.
    /// </summary>
    /// <remarks>
    /// <b>Halved, never negated</b> (§18) — an immunity reads as a lock and sends the player hunting a
    /// key instead of weighing a trade, which is the same argument that keeps creature resistance at 0.5×
    /// rather than at zero. The floor of 1 is the mirror of
    /// <see cref="CombatManager.DamageAgainst"/>'s: a warded hit is a smaller hit, not a miss.
    ///
    /// <para><b>There is deliberately no regeneration step here.</b> §2 describes the anchored body as
    /// regenerating every turn, and the built model already answers that more coarsely and better:
    /// <see cref="CombatManager.SettleEngagement"/> closes wounds when the fight ends anchored. Healing
    /// per turn as well would turn §5's 3–6 turn fight into a race against a heal, which is exactly the
    /// length failure §18 deleted the Defence scalar to avoid.</para>
    /// </remarks>
    private void CreatureStrike(EntityData data, bool parting)
    {
        if (data == null || data.Deals == DamageType.None || data.Damage <= 0) return;

        var armour = InventoryCraftingManager.Instance?.EquippedArmour;
        var warded = armour != null && armour.WardsAgainst == data.Deals;

        var amount = warded
            ? Mathf.Max(1, Mathf.RoundToInt(data.Damage * GameConfig.WardMultiplier))
            : data.Damage;

        VitalityManager.Instance?.Damage(amount);

        var lead = parting ? "As you turn, it" : "It";
        EventBus.Instance?.EmitNotify(warded
            ? $"{lead} strikes, and the {InventoryCraftingManager.Instance?.DisplayNameOf(armour.Id)} takes most of it."
            : $"{lead} strikes home.");
    }

    /// <summary>
    /// One trap going off. Shared by the on-engage volley and by <see cref="Trigger"/> so the two cannot
    /// drift — a trap does the same thing whoever set it off, which is the readability §18 is buying with
    /// <c>TriggersItself</c>.
    /// </summary>
    /// <remarks>
    /// The trap stays on its site afterwards and is not removed from the rig. Traps are permanent (§19),
    /// and a fired one is prevented from firing twice by <see cref="Trigger"/>'s own refusal rather than
    /// by deleting it — which means the rig strip still shows you what you set, and a second engagement
    /// gets the volley again. That is the abundance rule the permanence decision already accepted.
    /// </remarks>
    private void SpringTrap(string instanceId, EntityData data, ItemData trap)
    {
        var combat = CombatManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        if (data == null || combat == null || trap == null) return;

        var dealt = combat.Wound(instanceId, data, trap.Deals, trap.Potency);
        var name = inv?.DisplayNameOf(trap.Id) ?? trap.Id;

        EventBus.Instance?.EmitNotify(dealt > 0
            ? $"{name} — it is {combat.DescribeCondition(instanceId, data)}."
            : $"{name}, and it walks through it.");

        ResolveIfSpent(instanceId, data);
    }

    /// <summary>
    /// If the body has taken everything it can take, end the fight and write down what happened.
    /// Returns whether it did.
    /// </summary>
    private bool ResolveIfSpent(string instanceId, EntityData data)
    {
        if (CombatManager.Instance?.IsSpent(instanceId, data) != true) return false;

        Finish(instanceId);
        return true;
    }

    /// <summary>
    /// The body goes down. <b>Which flag that writes is derived from the creature, never authored</b> —
    /// see the table on <see cref="Flags.Killed"/>:
    ///
    /// <list type="bullet">
    /// <item>blank <c>PurifiedName</c> — never a person, so <see cref="Flags.Defeated"/> either way</item>
    /// <item><c>Severed</c> when it fell — the tether was already cut, so <see cref="Flags.Freed"/></item>
    /// <item><c>Corrupted</c> when it fell — <see cref="Flags.Killed"/>, and the person died with it</item>
    /// </list>
    ///
    /// <para>All three set <see cref="EntityState.Purified"/>, because the enum has three members and that
    /// is the terminal one. So the screen cleanses whichever route you took: the corruption really is
    /// gone either way, and the weight of the overkill route lives in the ending's count of
    /// <c>freed.*</c> rather than in a screen that stays dirty.</para>
    /// </summary>
    private bool Finish(string instanceId)
    {
        var entities = EntityStateManager.Instance;
        var placement = NavigationManager.Instance?.FindPlacement(instanceId);
        var data = placement?.Entity;
        if (entities == null || data == null) return false;

        var wasSevered = entities.GetState(instanceId) == EntityState.Severed;
        var wasPerson = !string.IsNullOrEmpty(data.PurifiedName);

        string flag;
        string line;

        if (!wasPerson)
        {
            flag = Flags.Defeated(instanceId);
            line = "It stops. Nothing leaves it — there was never anyone in there to leave.";
        }
        else if (wasSevered)
        {
            flag = Flags.Freed(instanceId);
            line = "Without the thing behind it holding on, the body comes apart almost politely.";
        }
        else
        {
            flag = Flags.Killed(instanceId);
            line = "It falls, and whatever was riding it goes out through the ceiling. "
                   + "What is left on the floor was a person a moment ago.";
        }

        WorldStateManager.Instance?.SetFlag(flag);
        entities.Purify(instanceId);

        EndFight(instanceId, line);

        // The screen may have just become finished. An explicit call rather than a signal, on
        // InteractionService's precedent — this and that one are the only two purify paths in the game.
        MendManager.Reevaluate(NavigationManager.Instance?.CurrentRoom, announce: true);
        return true;
    }

    /// <summary>
    /// Let go of the fight, whatever ended it — won, fled or collapsed. Settling is
    /// <see cref="CombatManager.SettleEngagement"/>'s call and takes no outcome, so this method has no
    /// opinion about who won either.
    /// </summary>
    private void EndFight(string instanceId, string line)
    {
        _opponentId = "";
        _playersTurn = true;
        _turn = 0;
        _salvesUsed = 0;

        CombatManager.Instance?.SettleEngagement(instanceId);

        if (!string.IsNullOrEmpty(line)) EventBus.Instance?.EmitNotify(line);
        EventBus.Instance?.EmitFightEnded(instanceId);
    }

    // ---- Persistence -----------------------------------------------------

    /// <summary>
    /// The rig, flattened to <c>"instanceId|itemId"</c> strings in rig order — the same shape as
    /// <c>SaveGame.DiscoveredBlueprints</c> rather than a nested dictionary, because order matters and a
    /// flat list is the one structure every serialiser in this project already round-trips.
    /// </summary>
    /// <remarks>
    /// <b>A live fight is deliberately not saved.</b> Loading mid-fight puts you back in the room with
    /// your rig intact and nothing committed, which is the rigging phase §18 says has no timer — so a
    /// reload is a way to reconsider rather than a way to bank half a fight. It also means the save
    /// format has no notion of "whose turn it was", which is a state worth not persisting.
    /// </remarks>
    public List<string> ExportRigs()
    {
        var flat = new List<string>();
        foreach (var kv in _rigs)
            foreach (var itemId in kv.Value)
                flat.Add($"{kv.Key}|{itemId}");

        return flat;
    }

    /// <summary>Restore the rig. Silent, like every other import: a load is not a placement.</summary>
    public void ImportRigs(List<string> flat)
    {
        _rigs.Clear();
        if (flat == null) return;

        foreach (var entry in flat)
        {
            if (string.IsNullOrEmpty(entry)) continue;

            var split = entry.IndexOf('|');
            if (split <= 0 || split >= entry.Length - 1) continue;

            var instanceId = entry.Substring(0, split);
            var itemId = entry.Substring(split + 1);

            if (!_rigs.TryGetValue(instanceId, out var rig))
            {
                rig = new List<string>();
                _rigs[instanceId] = rig;
            }

            // The cap is re-applied on the way in rather than trusted, for the same reason
            // ImportEquipped re-validates slots: a save written while the dial was higher must not
            // load into a state the live rules cannot produce.
            if (rig.Count < GameConfig.RigSlotsPerFight && !rig.Contains(itemId)) rig.Add(itemId);
        }
    }

    /// <summary>For the gates and for starting a new game.</summary>
    public void ResetAll()
    {
        _rigs.Clear();
        _opponentId = "";
        _playersTurn = true;
        _turn = 0;
        _salvesUsed = 0;
    }
}
