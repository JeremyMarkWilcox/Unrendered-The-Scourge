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
            case HotspotKind.Navigate:
                SetFlagIfAny(data.SetsFlag);
                return NavigationManager.Instance?.GoToRoom(data.TargetRoomId) ?? false;

            case HotspotKind.Harvest:
                return Harvest(data.ItemId, data.ItemAmount, data.ResolvedHarvestedFlag);

            case HotspotKind.Interact:
                SetFlagIfAny(data.SetsFlag);
                if (!string.IsNullOrEmpty(data.ExamineText))
                    EventBus.Instance?.EmitNotify(data.ExamineText);
                return true;

            // The relic in the Waking world. Touching it puts you in the Pixel Realm with
            // a draining clock and something already looking for you.
            case HotspotKind.CrossToDream:
                SetFlagIfAny(data.SetsFlag);
                return DreamRunManager.Instance?.Begin(data.HeistId) ?? false;

            // The anchor, at the far end of the maze.
            case HotspotKind.TakeAnchor:
                if (DreamRunManager.Instance is not { IsRunning: true }) return false;
                SetFlagIfAny(data.SetsFlag);
                DreamRunManager.Instance.TakeAnchor();
                return true;

            // The bench. Its real job is passive — merely being in the room with it is what makes
            // this a camp, refills both pools and sets where you wake, all handled by CampManager on
            // arrival with nothing to click. Clicking it will open the crafting panel; until that
            // panel exists, say so rather than pretending the click did nothing.
            case HotspotKind.Workbench:
                SetFlagIfAny(data.SetsFlag);
                EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(data.ExamineText)
                    ? "Your tools are laid out here. [the crafting panel is not built yet]"
                    : data.ExamineText);
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

        // TODO(combat): a Severed body is where the turn-based item fight belongs. Until it
        // exists, finishing one is a single click — the anchor is already gone, so this is
        // the cheap ending the design promises, just without the fight around it.
        if (state == EntityState.Severed)
        {
            entities.Purify(instanceId);
            OnPurified(placement);
            EventBus.Instance?.EmitNotify(
                "Without the thing behind it holding on, the body comes apart almost politely.");
            return true;
        }

        var presentation = entities.Resolve(placement.Entity, instanceId);
        EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(presentation.InteractText)
            ? "Nothing happens."
            : presentation.InteractText);
        return false;
    }

    /// <summary>
    /// Cutting the head off a region breaks its distortion field. Everything still standing
    /// in the zone drops to Form 3 — the survivors were never the enemy.
    /// </summary>
    private static void OnPurified(EntityPlacement placement)
    {
        if (!placement.IsZoneLieutenant) return;

        var room = NavigationManager.Instance?.CurrentRoom;
        if (room == null || string.IsNullOrEmpty(room.ChapterId)) return;

        var world = WorldStateManager.Instance;
        if (world == null || world.IsZoneCleansed(room.ChapterId)) return;

        world.SetZonePhase(room.ChapterId, ZonePhase.Purified);

        // Content gates on a plain flag rather than having to query zone phase.
        world.SetFlag($"zone.{room.ChapterId}.purified");

        var entities = EntityStateManager.Instance;
        if (entities != null)
        {
            foreach (var other in room.Entities)
            {
                if (other == null || other.InstanceId == placement.InstanceId) continue;
                entities.Purify(other.InstanceId);
            }
        }

        EventBus.Instance?.EmitNotify("The distortion breaks. Air moves. Somewhere, birds.");
    }

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
}
