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
    public static bool IsAvailable(HotspotData data, GameRealm realm)
    {
        if (data == null) return false;

        if (!MatchesRealm(data.Visibility, realm)) return false;

        var world = WorldStateManager.Instance;

        if (!string.IsNullOrEmpty(data.RequiredFlag) &&
            world?.GetFlag(data.RequiredFlag) != data.RequiredFlagValue)
            return false;

        if (data.Kind == HotspotKind.Harvest && world?.GetFlag(data.ResolvedHarvestedFlag) == true)
            return false;

        return true;
    }

    public static bool MatchesRealm(RealmVisibility visibility, GameRealm realm) => visibility switch
    {
        RealmVisibility.WakingOnly => realm == GameRealm.Waking,
        RealmVisibility.DreamOnly => realm == GameRealm.Dream,
        _ => true,
    };

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

        // Sampled before the action, because the action itself can move you: a CrossToDream
        // relic puts you in the Pixel Realm mid-call, and the wound belongs to the Waking-world
        // choice you just made rather than to where that choice landed you.
        var realmAtClick = RealmTransitionManager.Instance?.CurrentRealm ?? GameRealm.Waking;
        var runAtClick = DreamRunManager.Instance?.IsRunning == true;

        var performed = Perform(data);

        // Only a *successful* action wounds you. Otherwise a stripped-bare harvest node or a
        // refused door would let you grind yourself down on something that did nothing.
        if (performed) ApplyHazard(data, realmAtClick, runAtClick);

        return performed;
    }

    /// <summary>
    /// Takes the Vitality a hotspot costs. Silently does nothing for the overwhelming majority
    /// of hotspots, which cost nothing.
    ///
    /// **Invariant 2 is enforced here, in code, not left to authoring.** Vitality must never
    /// appear in the Dream — every ending in there is an ejection priced in Resolve, and if the
    /// body could be hurt inside a heist the two-pool split collapses along with the reason the
    /// two routes cost different currencies. So a cost is refused if you were looking through
    /// spectral vision or standing inside a live run when you clicked. Authoring a Dream-side
    /// hazard is therefore harmless rather than dangerous: it simply will not bite.
    /// </summary>
    private static void ApplyHazard(HotspotData data, GameRealm realmAtClick, bool runAtClick)
    {
        if (data is not { IsHazard: true }) return;
        if (realmAtClick != GameRealm.Waking || runAtClick) return;

        var vitality = VitalityManager.Instance;
        if (vitality == null) return;

        var before = vitality.Current;
        vitality.Damage(data.VitalityCost);

        // Nothing to say if the bar was already on the floor — VitalityManager announces the
        // collapse itself, and repeating it every click would read as a new injury each time.
        if (Mathf.IsEqualApprox(vitality.Current, before)) return;

        EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(data.HazardText)
            ? "That cost you. You can feel it settling in."
            : data.HazardText);
    }

    /// <summary>The action itself, with no hazard accounting. Split out so the caller can tell
    /// success from refusal before deciding whether to charge for it.</summary>
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

        var realm = RealmTransitionManager.Instance?.CurrentRealm ?? GameRealm.Waking;
        var state = entities.GetState(instanceId);

        // Spectral vision is a *look*, not a place. You can see the anchor from here; you
        // cannot reach it. Reaching it means finding the relic and crossing over properly.
        if (realm == GameRealm.Dream)
        {
            EventBus.Instance?.EmitNotify(state switch
            {
                EntityState.Corrupted =>
                    $"{placement.Entity.SpiritName} does not look up. Whatever holds it here is "
                    + "somewhere behind all this, and you are only looking through glass.",
                EntityState.Severed => "The crown is broken. It has stopped pretending to be calm.",
                _ => "Only warmth, where a crown used to be.",
            });
            return false;
        }

        // Waking world.
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

        var presentation = entities.Resolve(placement.Entity, instanceId, realm);
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
        if (room == null || string.IsNullOrEmpty(room.ZoneId)) return;

        var world = WorldStateManager.Instance;
        if (world == null || world.IsZoneCleansed(room.ZoneId)) return;

        world.SetZonePhase(room.ZoneId, ZonePhase.Purified);

        // Content gates on a plain flag rather than having to query zone phase.
        world.SetFlag($"zone.{room.ZoneId}.purified");

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
