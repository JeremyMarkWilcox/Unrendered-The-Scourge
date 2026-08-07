using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.World;

/// <summary>
/// What clicking something actually does. Kept out of <see cref="RoomView"/> so the view
/// stays responsible only for drawing: the rules here can then be driven without a scene
/// tree, which is what makes the milestone gates testable rather than merely claimed.
/// </summary>
public static class InteractionService
{
    /// <summary>
    /// Whether an authored hotspot should exist at all right now. Gating removes a hotspot
    /// rather than disabling it — an invisible ungrabbable herb reads better than a grey one.
    /// </summary>
    public static bool IsAvailable(HotspotData data)
    {
        if (data == null) return false;

        var world = WorldStateManager.Instance;

        if (!string.IsNullOrEmpty(data.RequiredFlag) &&
            world?.GetFlag(data.RequiredFlag) != data.RequiredFlagValue)
            return false;

        // Every entry must be true. A blank entry is treated as unmet rather than skipped, so a
        // half-authored gate stays shut instead of quietly opening the door it was meant to hold.
        if (data.RequiredFlags != null)
        {
            foreach (var flag in data.RequiredFlags)
                if (string.IsNullOrEmpty(flag) || world?.GetFlag(flag) != true)
                    return false;
        }

        if (data.Kind == HotspotKind.Harvest && world?.GetFlag(data.ResolvedHarvestedFlag) == true)
            return false;

        return true;
    }

    // `MatchesRealm` and the realm parameter above went on 2026-08-02 with spectral vision. A
    // hotspot's availability is now a question about the world's flags alone, which is why this
    // method no longer needs to be told where the player is looking from.

    // ---- Hotspots --------------------------------------------------------

    /// <summary>Returns false when the action was refused rather than performed.</summary>
    public static bool ActivateHotspot(HotspotData data)
    {
        if (data == null) return false;

        // Anything still Corrupted in the way refuses passage, whatever the hotspot is.
        if (!string.IsNullOrEmpty(data.BlockedByEntityId) &&
            EntityStateManager.Instance?.GetState(data.BlockedByEntityId) == EntityState.Corrupted)
        {
            EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(data.BlockedText)
                ? "Something is in the way."
                : data.BlockedText);
            return false;
        }

        // `ApplyHazard` was called here until 2026-08-01 and is deleted with the rest of the hazard
        // machinery (`combat.md` §22 — the Waking world no longer damages you). It also carried the
        // only in-code enforcement of invariant 2 on the Vitality side: a cost was refused if you
        // clicked through spectral vision or inside a live run, sampled *before* the action because
        // a CrossToDream relic moves you mid-call. **Nothing spends Vitality outside a fight now, so
        // there is nothing left to refuse** — but if anything ever charges the body from a hotspot
        // again, that sample-then-act shape is the part to bring back, not the field.
        return Perform(data);
    }

    /// <summary>The action itself.</summary>
    private static bool Perform(HotspotData data)
    {
        switch (data.Kind)
        {
            // A door, clicked mid-fight, IS breaking off — so it is priced like one. §18 makes flight
            // guaranteed and charges it a parting blow; walking out is the same act with a different
            // button, and letting it be free would make "flee" the strictly worse of two identical
            // choices. The backstop in `EncounterManager.OnRoomEntered` charges nothing and cannot,
            // because a collapse navigates too — see the note there.
            case HotspotKind.Navigate:
                var nav = NavigationManager.Instance;
                var fight = EncounterManager.Instance;
                if (fight?.InFight == true)
                {
                    var stoodIn = nav?.CurrentRoomId;
                    fight.Flee();

                    // The parting blow can be lethal, and a collapse carries you to a camp. If you were
                    // moved, the door you clicked is not next to you any more and walking through it
                    // would quietly undo the respawn — so the flight is the whole outcome, and it
                    // succeeded.
                    if (nav?.CurrentRoomId != stoodIn) return true;
                }
                SetFlagIfAny(data.SetsFlag);
                return nav?.GoToRoom(data.TargetRoomId) ?? false;

            case HotspotKind.Harvest:
                return Harvest(data.ItemId, data.ItemAmount, data.ResolvedHarvestedFlag);

            case HotspotKind.Interact:
                SetFlagIfAny(data.SetsFlag);
                if (!string.IsNullOrEmpty(data.ExamineText))
                    EventBus.Instance?.EmitNotify(data.ExamineText);
                return true;

            // The way into the Dream: a broken thing on a Waking screen that is scenery rather than an
            // object — never picked up, and it stays where it is until you use it.
            //
            // Had a second arm for the length of 6a, reaching the heist's `Begin` — deleted in 6b with the
            // class. The flag set here is `SetsFlag`, the author's own optional one, and **not** the pocket's
            // `cleared` flag: that is written by `PocketManager.Complete` and by nothing else, which is what
            // keeps an abandoned pocket's entrance intact.
            // Two destinations, and **which one is derived from the pocket, never from the hotspot** (unit 10,
            // 2026-08-05). A pocket whose `ScenePath` is blank has no 3D scene built yet, so clicking its
            // entrance grants what it holds where you stand; one that names a scene crosses you in, exactly as
            // before. Jeremy builds the fourteen Dream levels on his own clock, and pointing a `PocketData` at
            // a finished scene switches that one pocket over — nothing here changes, and no mode gets turned
            // off. Unit 1.5's rule again: derive the placeholder from the absence of the real thing.
            //
            // **Only one direction is guarded, and that is the correct one.** `TakeFromWaking` refuses a pocket
            // that has a scene (`PocketBlock.MustBeWalked`), so inverting this condition cannot make a finished
            // pocket hand its reward over on a click — it just refuses. The other way round is unguarded on
            // purpose: entering a scene-less pocket is a supported state that every gate fixture lives in, so
            // the manager permits it and this branch is what keeps play out of it.
            case HotspotKind.CrossToDream:
                SetFlagIfAny(data.SetsFlag);
                var pockets = PocketManager.Instance;
                if (pockets == null) return false;
                return pockets.GetPocket(data.PocketId)?.HasScene == true
                    ? pockets.Enter(data.PocketId)
                    : pockets.TakeFromWaking(data.PocketId);

            // `case HotspotKind.TakeAnchor` stood here and went with the kind in 6b. The objective is inside
            // the 3D scene now — a pocket calls `PocketManager.Complete()` when the player reaches it — so
            // there is no Waking-world hotspot for taking anything, and a kind with no case is a kind that
            // silently does nothing.

            // A field station: the mortar, the anvil, the trapper's stump (§19). Same rule as `Mend` —
            // it emits and returns, never touching UI, so a station click is drivable with no scene
            // tree. What it does *not* do is check the venue: `InventoryCraftingManager.Inspect` is the
            // one authority on whether a given recipe can be made here, and re-asking that question at
            // the door would put a second copy of the camp-satisfies-every-station ruling somewhere it
            // could disagree. Clicking a station always opens the panel; the panel greys the rows.
            //
            // A station with a board on it is one of §22's three major repairs, and it has two clicks
            // in this order: the panel, where the commons are paid and the reclaimed memory gates the
            // row, and then the board, which is the animation rather than the gate. `SetsFlag` is
            // withheld from a repair for exactly the reason the Mend branch below withholds it — on a
            // repair it is the passage, and a passage must not open because you looked at the bridge.
            case HotspotKind.Station:
                if (data.MendPuzzle != null)
                {
                    if (MendManager.CanMend(data))
                    {
                        EventBus.Instance?.EmitMendRequested(data);
                        return true;
                    }

                    if (MendManager.IsMended(data))
                    {
                        EventBus.Instance?.EmitNotify(MendManager.AlreadyMended);
                        return false;
                    }
                }
                else
                {
                    SetFlagIfAny(data.SetsFlag);
                }

                EventBus.Instance?.EmitCraftingRequested(data.StationId);
                AnnounceCraftingVenue(data);
                return true;

            // The bench. Its real job is passive — merely being in the room with it is what makes this
            // a camp, refills both pools and sets where you wake, all handled by CampManager on arrival
            // with nothing to click. Clicking it asks for the crafting panel, which is the same verb as
            // the case above and deliberately shares its signal rather than having one of its own: the
            // bench is the station that is every station, and a panel that had to tell the two apart
            // would be the place the ruling went to rot.
            case HotspotKind.Workbench:
                SetFlagIfAny(data.SetsFlag);
                EventBus.Instance?.EmitCraftingRequested(data.StationId);
                AnnounceCraftingVenue(data);
                return true;

            // A blight. The click asks for the board and does nothing else — no flag, no yield, no
            // cost. Solving the board is what pays out, through MendManager.Mend, which is the one
            // seam the panel and the gates share (§22). `SetsFlag` is deliberately not set here: it
            // fires on the mend, not on looking at it, so MendManager owns it.
            //
            // The refusal has to live here as well as in MendManager: a mended blight stays on screen
            // as its maker's mark, so clicking it again must say so rather than open an empty board.
            case HotspotKind.Mend:
                if (!MendManager.CanMend(data))
                {
                    EventBus.Instance?.EmitNotify(MendManager.AlreadyMended);
                    return false;
                }

                EventBus.Instance?.EmitMendRequested(data);
                return true;

            default:
                SetFlagIfAny(data.SetsFlag);
                EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(data.ExamineText)
                    ? "Nothing worth the time."
                    : data.ExamineText);
                return true;
        }
    }

    // ---- Entities --------------------------------------------------------

    /// <summary>
    /// The dual-world feedback loop, in one method. Nothing you do to a possessed body in
    /// the Waking world matters; the anchor is in the Dream, and cutting it there is what
    /// changes reality. Two beats, so the middle state earns its place: sever frees the
    /// road, banish frees the animal.
    /// </summary>
    public static bool ActivateEntity(string instanceId)
    {
        var placement = NavigationManager.Instance?.FindPlacement(instanceId);
        if (placement?.Entity == null) return false;

        var entities = EntityStateManager.Instance;
        if (entities == null) return false;

        var state = entities.GetState(instanceId);

        // A Dream branch stood here until 2026-08-02: three lines of refusal for clicking a body
        // while peering at it through spectral vision. The refusal it enforced — you cannot sever
        // from out here — is not lost, because the Waking `Corrupted` cell already says so and
        // returns false at the bottom of this method. What went was the second way of saying it.

        if (state == EntityState.Purified && !string.IsNullOrEmpty(placement.Entity.HarvestItemId))
            return Harvest(placement.Entity.HarvestItemId, 1, $"harvested.{instanceId}");

        // A `TODO(combat)` stood on the Severed branch until 2026-08-03 (unit 3a): finishing a severed
        // body was a single click, standing in for a fight that did not exist. `combat.md` §18 named its
        // replacement exactly — *"the baseline attack is what the existing one-click 'finish the Severed
        // body' becomes"* — so both hostile states now go the same way, into `Engage`.
        //
        // **The cheap ending survives as an outcome rather than as a shortcut.** A severed body does not
        // regenerate and its damage persists between visits (§3), so `Strike` with nothing equipped still
        // gets there — several clicks instead of one, which is the design's own description of finishing
        // a husk. What went is the special case, not the promise. `EngageBlock.Spent` routes a husk
        // straight to its ending, so a fight you fled does not need a fight to close.
        if (state is EntityState.Severed or EntityState.Corrupted)
        {
            var encounters = EncounterManager.Instance;
            if (encounters == null) return false;

            // `Spent` is handed to `Engage` rather than filtered out by `CanEngage`, which is the one
            // place those two disagree: a husk cannot be *fought*, so `CanEngage` is honestly false, but
            // clicking it should still finish it. `Engage` owns that routing so the husk ending has one
            // implementation instead of a copy here.
            var block = encounters.InspectEngage(instanceId);
            if (block is EngageBlock.None or EngageBlock.Spent) return encounters.Engage(instanceId);
            if (block == EngageBlock.AlreadyFighting) return false;  // Engage already said so.
        }

        var presentation = entities.Resolve(placement.Entity, instanceId);
        EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(presentation.InteractText)
            ? "Nothing happens."
            : presentation.InteractText);
        return false;
    }

    // `OnPurified` stood here until 2026-08-03 (unit 2). It was the zone system's payoff: purifying a
    // placement marked `IsZoneLieutenant` set the whole chapter to `ZonePhase.Purified`, set
    // `zone.<chapter>.purified`, and dropped every other creature in the room to Form 3 for free.
    //
    // Three things replaced it, and only the first is a rename:
    //  * the flip is now per screen — `MendManager.Reevaluate` sets `screen.<id>.cleansed` when nothing
    //    on that screen is Held or Fouled, which is what `combat.md` §22 asks for. A chapter is no
    //    longer a unit that can be cleansed; 59 screens are.
    //  * the Form-3 cascade is deleted outright rather than moved. It contradicts `tethered dies,
    //    untethered survives`: every creature is a person with their own tether, so nobody is freed as
    //    a side effect of somebody else's fight. Thirty fights are thirty fights.
    //  * `zone.*.purified` is gone as a gating vocabulary. The only content that used it was
    //    `waystation_yard`'s bitterroot, re-pointed at `screen.<id>.cleansed` and then deleted outright
    //    with the prototype in unit 3b — so no authored hotspot gates on cleansing at all today, and
    //    the shape survives only as the verifier's `verify_loop` harvest node.

    // ---- Shared ----------------------------------------------------------

    public static bool Harvest(string itemId, int amount, string harvestedFlag)
    {
        var world = WorldStateManager.Instance;

        if (world?.GetFlag(harvestedFlag) == true)
        {
            EventBus.Instance?.EmitNotify("Already stripped bare.");
            return false;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            EventBus.Instance?.EmitNotify("Nothing here to take.");
            return false;
        }

        InventoryCraftingManager.Instance?.AddItem(itemId, amount);
        world?.SetFlag(harvestedFlag);

        var name = InventoryCraftingManager.Instance?.DisplayNameOf(itemId) ?? itemId;
        EventBus.Instance?.EmitNotify($"Taken: {name} x{amount}");
        return true;
    }

    private static void SetFlagIfAny(string flag)
    {
        if (!string.IsNullOrEmpty(flag)) WorldStateManager.Instance?.SetFlag(flag);
    }

    /// <summary>
    /// What a bench or a station says when you click it. Both kinds share it, because the ruling they
    /// share is that a bench <i>is</i> a station and the player should not be able to tell the click
    /// apart (§19).
    ///
    /// <b>The scaffold half of this is why it exists.</b> The note used to live in the `Workbench`
    /// arm as the <i>else</i> of its flavour text and was missing from the `Station` arm entirely, so
    /// what you were told depended on which of four benches you clicked: three had no `ExamineText`
    /// and explained themselves, the since-deleted `waystation_shed` had flavour text and so swallowed
    /// the note, and every field station said either nothing at all or the literal string
    /// <c>[unwritten]</c>.
    /// Found 2026-08-03 by Jeremy gathering materials and finding no way to spend them. Flavour text
    /// and a scaffold notice are not alternatives — one is content and one is an apology for absent
    /// content, and an <c>else</c> between them makes authoring a line of prose silently delete a
    /// diagnostic. **Anything that stands in for an unbuilt screen has to be additive.**
    ///
    /// ~~Delete the second emit when the crafting panel lands; that is the whole demolition.~~
    /// <b>Done 2026-08-05, and it was three units late.</b> The scaffold line read *"Your tools are laid out
    /// here. [the crafting panel is not built yet — F7 crafts]"* and both of its claims stopped being true with
    /// unit 7a: the panel exists, a bench opens it, and `F7` was deleted the same day. So a player clicking a
    /// bench was told to press a key that does nothing, about a screen that was already open in front of them.
    /// Jeremy hit it in a camp on 2026-08-05.
    ///
    /// <para><b>It is the third scheduled deletion in two days to be missed the same way</b> — after `[F8] rig`
    /// in the HUD caption and the two self-retiring ledger gates. Each one wrote down the day it should go, in
    /// the file it lived in, and none of them could *fire*. The pattern worth naming: **a note that says
    /// "delete this later" is a reminder to whoever happens to reopen the file, not to whoever finishes the
    /// work.** The ledger gates are the counterexample and the model — they went red on schedule, because they
    /// were checks rather than comments.</para>
    ///
    /// <para>What stays is the additive shape, which was the real lesson here: flavour text and a diagnostic are
    /// not alternatives, so this never used an <c>else</c>. With the diagnostic gone the method is just the
    /// flavour line, and a station with nothing authored now says nothing rather than apologising.</para>
    /// </summary>
    private static void AnnounceCraftingVenue(HotspotData data)
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        if (!string.IsNullOrEmpty(data.ExamineText) && data.ExamineText != "[unwritten]")
            bus.EmitNotify(data.ExamineText);
    }
}
