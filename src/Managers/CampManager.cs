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
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.RoomEntered -= OnRoomEntered;
            bus.VitalityDepleted -= OnVitalityDepleted;
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

            if (DreamRunManager.Instance is { IsRunning: true }) return false;

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
        if (DreamRunManager.Instance is { IsRunning: true }) return;

        var firstVisit = ReturnCampRoomId != roomId;
        ReturnCampRoomId = roomId;

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

    // ---- Collapsing ------------------------------------------------------

    private void OnVitalityDepleted() => RespawnAtCamp();

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
        // Nothing in the Dream may cost the body (invariant 2), so nothing in the Dream may trigger
        // this. If it ever does, the bug is upstream and swallowing it here would hide it — hence
        // the pushed error rather than a silent return.
        if (DreamRunManager.Instance is { IsRunning: true })
        {
            GD.PushError("[CampManager] Vitality emptied during a Dream run. Invariant 2 is broken upstream.");
            return;
        }

        InventoryCraftingManager.Instance?.LoseUnprotectedItems();

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
    public void ImportReturnCamp(string roomId) => ReturnCampRoomId = roomId ?? "";

    public void ResetAll() => ReturnCampRoomId = "";
}
