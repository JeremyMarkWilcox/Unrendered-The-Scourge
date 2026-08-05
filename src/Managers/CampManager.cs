using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// Camps — the only safe ground in the Waking world, and the only place crafting happens
/// (docs/combat.md §17, ratified 2026-07-30).
///
/// **A room is a camp precisely when it holds a <see cref="HotspotKind.Workbench"/> hotspot.**
/// There is no camp flag on <see cref="RoomData"/>, and there deliberately never was one for long:
/// two facts that must always agree are better stored as one. Jeremy's phrasing settled it — the
/// camp is "the room you put the workbench in" — so authoring a camp means placing a single hotspot
/// and it is impossible to end up with a bench outside a camp or a camp with no bench.
///
/// **Everything here happens on arrival. There is nothing to click.** That is a design rule with a
/// stated reason: a rest button is annoying when the player forgets it, and declining to heal is
/// never advantageous, so making it optional would be bug-like behaviour dressed as a choice. The
/// general principle, which is worth carrying to the next system that wants a confirmation step:
/// **if the player would never decline, do not ask.** So reaching a camp refills both pools *and*
/// silently becomes your return point, with no claim interaction and no picker.
///
/// **Invariant 2 is enforced here, not merely respected.** Vitality must never move in the Dream,
/// and a camp refills it — so a camp is only ever recognised in the Waking realm with no run live.
/// A Dream room carrying a workbench would be an authoring mistake; this makes it a harmless one.
///
/// **Since unit 5 this class also owns getting *to* a camp** (§17 as amended 2026-08-01). A camp is still
/// a screen with ordinary exits — you are teleported in and you walk out, and the walk out is the price
/// the amendment exists to charge. See <see cref="InspectTravel"/> for the two refusals that make that
/// price real.
/// </summary>
public partial class CampManager : Node
{
    public static CampManager Instance { get; private set; }

    /// <summary>
    /// Where <see cref="RespawnAtCamp"/> sends you: the last camp you stood in. Blank until you
    /// have reached one, which is the honest state of a fresh game rather than a bug.
    /// </summary>
    public string ReturnCampRoomId { get; private set; } = "";

    public override void _EnterTree() => Instance = this;

    public override void _Ready()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.RoomEntered += OnRoomEntered;
        bus.VitalityDepleted += OnVitalityDepleted;
        bus.ResolveDepleted += OnResolveDepleted;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.RoomEntered -= OnRoomEntered;
            bus.VitalityDepleted -= OnVitalityDepleted;
            bus.ResolveDepleted -= OnResolveDepleted;
        }

        if (Instance == this) Instance = null;
    }

    // ---- What a camp is --------------------------------------------------

    /// <summary>
    /// Whether this room is a camp: does it hold a workbench? Pure and static, so the gates and the
    /// authoring guard can ask about a room the player is not standing in.
    /// </summary>
    public static bool IsCamp(RoomData room) =>
        room?.Hotspots is not null &&
        room.Hotspots.Any(h => h != null && h.Kind == HotspotKind.Workbench);

    public static bool IsCamp(string roomId) =>
        IsCamp(NavigationManager.Instance?.GetRoom(roomId));

    /// <summary>
    /// Whether the player may craft and open the cache right now. The realm and run guards are the
    /// enforcement of invariant 2 described in the class summary — peeking into the Dream from a
    /// camp with `Tab` must not turn the bench off, but *being* in the Dream must.
    /// </summary>
    public bool AtCamp
    {
        get
        {
            var nav = NavigationManager.Instance;
            if (nav?.CurrentRoom == null || !IsCamp(nav.CurrentRoom)) return false;

            if (PocketManager.InsideTheDream) return false;

            return true;
        }
    }

    // ---- Arriving --------------------------------------------------------

    private void OnRoomEntered(string roomId)
    {
        // Invariant 13: emission is synchronous and in subscription order, so an earlier subscriber
        // may have navigated again before this one is reached and the payload can be stale. The
        // ejection post-mortem in the handoff is this exact failure. Trust the manager, not the
        // argument.
        var nav = NavigationManager.Instance;
        if (nav == null || nav.CurrentRoomId != roomId) return;

        if (!IsCamp(nav.CurrentRoom)) return;
        if (PocketManager.InsideTheDream) return;

        var firstVisit = ReturnCampRoomId != roomId;
        ReturnCampRoomId = roomId;

        // And it becomes somewhere you can get back to. Deliberately the same event as the rebind above
        // and not a separate act: §17's "there is nothing to click" applies to the whole of arriving, so
        // a camp is discovered by standing in it rather than by claiming it.
        MarkFound(roomId);

        // Only speak when something actually changed. Walking in and out of camp while topped up
        // should not narrate a rest that did not happen — the same restraint VitalityManager.SetTo
        // applies to a wound that cannot lower the bar.
        var restored = RestorePools();

        if (restored)
            EventBus.Instance?.EmitNotify("You set your things down. The shaking stops.");
        else if (firstVisit)
            EventBus.Instance?.EmitNotify("A bench, and room to work. You will come back here.");
    }

    /// <summary>Fill both bars. Returns whether either of them actually moved.</summary>
    private static bool RestorePools()
    {
        var resolve = ResolveManager.Instance;
        var vitality = VitalityManager.Instance;

        var moved = false;

        if (resolve != null && resolve.Current < resolve.Max)
        {
            resolve.SetTo(resolve.Max);
            moved = true;
        }

        if (vitality != null && vitality.Current < vitality.Max)
        {
            vitality.SetTo(vitality.Max);
            moved = true;
        }

        return moved;
    }

    // ---- Getting there (unit 5, §17 as amended) --------------------------

    /// <summary>
    /// Remember a camp as a travel destination. Idempotent, because <c>SetFlag</c> is.
    /// </summary>
    private static void MarkFound(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;
        WorldStateManager.Instance?.SetFlag(Flags.CampFound(roomId));
    }

    /// <summary>Whether that camp has been stood in, and so may be travelled to.</summary>
    public static bool IsFound(string roomId) =>
        !string.IsNullOrEmpty(roomId) &&
        WorldStateManager.Instance?.GetFlag(Flags.CampFound(roomId)) == true;

    /// <summary>
    /// Every camp the player may travel to, in chapter order. One list, read by both the panel and the
    /// gates, so a destination the menu offers and a destination the verb accepts cannot diverge.
    /// </summary>
    /// <remarks>
    /// Derived on each call rather than cached. It is a sweep of ten-odd rooms behind a keypress, and the
    /// alternative is a second copy of "which camps are known" that has to be kept in step with the flag
    /// — the <c>IsSanctuary</c> mistake in miniature. <see cref="IsCamp"/> is derived for the same reason.
    /// </remarks>
    public static IReadOnlyList<string> KnownCamps
    {
        get
        {
            var rooms = NavigationManager.Instance?.Rooms;
            if (rooms == null) return System.Array.Empty<string>();

            return rooms
                .Where(kv => IsCamp(kv.Value) && IsFound(kv.Key))
                .OrderBy(kv => kv.Value?.ChapterId ?? "")
                .ThenBy(kv => kv.Key)
                .Select(kv => kv.Key)
                .ToList();
        }
    }

    /// <summary>
    /// Whether you may leave *at all* from where you are standing, before any destination is named —
    /// <see cref="TravelBlock.InFight"/>, <see cref="TravelBlock.InPocket"/>, or
    /// <see cref="TravelBlock.None"/>.
    /// </summary>
    /// <remarks>
    /// <b>Split out of <see cref="InspectTravel"/> so the two guards have exactly one home.</b> The panel
    /// needs this question on its own: a menu whose every row is dead is worse than no menu, so pressing the
    /// key mid-fight should say *why* rather than draw three greyed camps. Answering it by inspecting an
    /// arbitrary destination would work and would put the fight guard on two code paths, which is how one
    /// of them ends up break-testable and the other does not.
    /// </remarks>
    public TravelBlock InspectDeparture()
    {
        if (EncounterManager.Instance?.InFight == true) return TravelBlock.InFight;
        if (PocketManager.InsideTheDream) return TravelBlock.InPocket;

        return TravelBlock.None;
    }

    /// <summary>
    /// Why <see cref="TravelToCamp"/> would refuse, without moving anybody. Same inspect/act split as
    /// <c>InventoryCraftingManager.Inspect</c> and <c>EncounterManager.InspectRig</c>, for the same
    /// reason: the row the panel greys and the verb it fires must ask one question.
    /// </summary>
    /// <remarks>
    /// <b>Two refusals, and §17's surviving-pressures table depends on both.</b>
    /// <para>
    /// <b>Inside a fight — refused, not priced as a flight.</b> This is the decision most worth writing
    /// down, because the codebase already contains the other answer: <c>InteractionService</c>'s
    /// <c>Navigate</c> case treats a door clicked mid-fight as breaking off, and charges §3's parting blow
    /// for it. Copying that here would have been consistent and wrong. Fleeing leaves you standing where
    /// you fought; arriving at a camp refills <i>both</i> pools — so a camp menu that also fled would be
    /// escape and a full heal on one keypress, and the retravel cost the amendment exists to charge would
    /// be gone. Nothing is taken away from the player: flee, pay the blow, then travel. And
    /// <c>EncounterManager.OnRoomEntered</c> already names <i>"including a load and the camp menu"</i> as a
    /// path its backstop covers, so a fight cannot survive this even if the refusal is ever removed —
    /// which is precisely why the refusal has to be asserted <i>together with</i> the fight still being
    /// live, or the backstop would make a broken guard look fine.
    /// </para>
    /// <para>
    /// <b>Inside a pocket — refused, keyed on the run.</b> Invariant 4: you cannot reach the anchor from
    /// out here, and the mirror of that is that you cannot step out of the Dream sideways into a bench.
    /// Keyed on <c>DreamRunManager.IsRunning</c> rather than on <c>GameRealm.Dream</c> for
    /// <c>Hud.RefreshPools</c>'s reason — unit 6's quiet pockets are the Dream with nothing draining, and
    /// the same guard on <see cref="AtCamp"/> is already run-keyed, so realm-keying this one would make
    /// the two disagree about what "in the Dream" means.
    /// </para>
    /// </remarks>
    public TravelBlock InspectTravel(string roomId)
    {
        // Where you are beats where you want to go — see the note on TravelBlock's ordering.
        var departure = InspectDeparture();
        if (departure != TravelBlock.None) return departure;

        if (!IsCamp(roomId)) return TravelBlock.NotACamp;
        if (!IsFound(roomId)) return TravelBlock.NotFound;
        if (NavigationManager.Instance?.CurrentRoomId == roomId) return TravelBlock.AlreadyHere;

        return TravelBlock.None;
    }

    /// <summary>Whether the verb would carry you there. See <see cref="InspectTravel"/>.</summary>
    public bool CanTravelTo(string roomId) => InspectTravel(roomId) == TravelBlock.None;

    /// <summary>
    /// The verb: go to that camp from wherever you are. Returns whether you moved.
    /// </summary>
    /// <remarks>
    /// <b>It navigates and stops.</b> The refill, the rebind and the line are <see cref="OnRoomEntered"/>'s
    /// job and are not repeated here — there is exactly one arrival path whether you walked in or travelled
    /// in, which is what keeps "arriving at a camp refills both pools" a single fact rather than two that
    /// must agree. The same argument <see cref="RespawnAtCamp"/> could not use, and says so: its
    /// destination may not be a camp at all.
    /// </remarks>
    public bool TravelToCamp(string roomId)
    {
        var verdict = InspectTravel(roomId);
        if (verdict != TravelBlock.None)
        {
            // `NotACamp` and `NotFound` say nothing, because nothing the player did reaches them: the
            // first is a content error and the second is a room the menu never listed. Every refusal a
            // player can actually provoke has words.
            var line = Words(verdict);
            if (!string.IsNullOrEmpty(line)) EventBus.Instance?.EmitNotify(line);
            return false;
        }

        return NavigationManager.Instance?.GoToRoom(roomId) == true;
    }

    /// <summary>
    /// Plain words for a refusal, or "" for the two nobody can provoke.
    /// </summary>
    /// <remarks>
    /// <b>Public and static on <c>EncounterManager.Words</c>'s precedent, and for the same reason it was
    /// made public on 2026-08-03:</b> the panel has to say why a row is dead, and a private copy of these
    /// sentences over there would close the loop today and drift the first time one of them is reworded.
    /// One vocabulary, two readers.
    /// </remarks>
    public static string Words(TravelBlock block) => block switch
    {
        TravelBlock.InFight => "Not with this in front of you. Break off first.",
        TravelBlock.InPocket => "There is no road out of here. Only the way you came.",
        TravelBlock.AlreadyHere => "You are already here.",
        _ => "",
    };

    // ---- Collapsing ------------------------------------------------------

    private void OnVitalityDepleted() => RespawnAtCamp();

    /// <summary>
    /// The nerve gave out, and it ends the same way the body does — ruled 2026-08-04: *"we should use the
    /// same function if VIT or Resolve hit 0 that the player goes to the most recent camp. I think it keeps
    /// it consistent and easy to remember / fair."*
    /// </summary>
    /// <remarks>
    /// <b>This revises `combat.md` §6</b>, which had an empty bar in a boss fight mean *forced flight* — you
    /// broke off and stayed where you were. One destination for both pools is easier to hold in your head,
    /// and with item loss gone (unit 8) the two paths are genuinely identical rather than merely similar:
    /// each costs the walk back and nothing else.
    /// <para>
    /// <b>Ejecting from a pocket happens first, and it has to.</b> `RespawnAtCamp` navigates, and arriving
    /// somewhere while the manager still believes you are in a pocket would make `OnRoomEntered` refuse to
    /// recognise the camp — you would land at a bench that did not refill you. So the pocket is torn down
    /// before the respawn rather than after, which is invariant 13's lesson generalised: set the state, then
    /// announce it.
    /// </para>
    /// </remarks>
    private void OnResolveDepleted()
    {
        PocketManager.Instance?.Eject();
        RespawnAtCamp();
    }

    /// <summary>
    /// The body gave out: lose what you can remake, keep what you cannot, wake at the last camp.
    ///
    /// Called straight from the <c>VitalityDepleted</c> handler rather than deferred, and the
    /// re-entrancy is safe: <see cref="VitalityManager.SetTo"/> has already written 0 before
    /// announcing the collapse, so refilling from inside its own signal simply overwrites that with
    /// Max and the depletion branch cannot fire a second time on the way back up. No guard flag
    /// here on purpose — one would be unreachable, which is the dead-latch mistake this codebase has
    /// already made once and removed.
    ///
    /// Public because the gates drive it directly as well as through the signal; both paths are
    /// asserted.
    /// </summary>
    public void RespawnAtCamp()
    {
        // **Invariant 2, and unit 8 changed what this guard means without changing what it protects.** It
        // used to read *"nobody may respawn from inside the Dream"*, which was the same thing while the only
        // caller was a Vitality collapse. It is not the same thing any more: an empty *nerve* respawns you and
        // is inside the Dream by definition. So the guard now asks the question it always meant — **has the
        // body been emptied in there** — and an empty Resolve arrives having already ejected, so it is out by
        // the time it gets here.
        //
        // Still a pushed error rather than a silent return: nothing in the Dream may cost the body, so if this
        // fires the bug is upstream and swallowing it would hide it.
        if (PocketManager.InsideTheDream)
        {
            GD.PushError(
                "[CampManager] Vitality emptied inside the Dream. Invariant 2 is broken upstream.");
            return;
        }

        // `LoseUnprotectedItems()` was called here and went with unit 8. **A collapse now takes nothing** —
        // the predicate behind it burned `Component`, and both scarce nouns default to `Component`, so dying
        // while carrying Graveflower deleted the good ending. Jeremy: *"I don't like item lost ever other
        // than consumed."* What a collapse costs is the walk back, which is also what lets Resolve share
        // this path.

        // Explicitly, rather than relying on the arrival handler below: the fallback destination is
        // not a camp, and waking at 0 Vitality in the starting room would leave the player exactly
        // as stuck as they were before camps existed.
        RestorePools();

        var destination = string.IsNullOrEmpty(ReturnCampRoomId)
            ? GameConfig.StartingRoomId
            : ReturnCampRoomId;

        EventBus.Instance?.EmitNotify(
            "You come to somewhere else, hours later, with less than you set out with.");

        NavigationManager.Instance?.GoToRoom(destination);
    }

    // ---- Persistence -----------------------------------------------------

    /// <summary>
    /// Restore the return point. Silent by design: a load restores where you would wake, it does not
    /// re-enact arriving there, so nothing is announced and no pools are touched.
    /// </summary>
    /// <remarks>
    /// <b>It marks that camp found as well, and the one line is there for saves written before unit 5.</b>
    /// Travel destinations live in <c>Flags.CampFound</c>, so a file from yesterday has a return point and
    /// no known camps — a player who had reached the cottage would load in with the verb offering nothing
    /// and no way to tell that from a bug. Re-deriving the one camp the old format *does* record costs a
    /// line and cannot be wrong: you were sent there on collapse, so you had stood in it.
    /// <para>
    /// Order matters against <c>SaveService</c>: this runs after <c>ImportState</c>, so the flag it writes
    /// is not clobbered by the file's own flag dictionary. Asserted rather than assumed.
    /// </para>
    /// </remarks>
    public void ImportReturnCamp(string roomId)
    {
        ReturnCampRoomId = roomId ?? "";
        MarkFound(ReturnCampRoomId);
    }

    public void ResetAll() => ReturnCampRoomId = "";
}
