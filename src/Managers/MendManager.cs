using System.Linq;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// Mending, and the rule that decides when a screen is finished (<c>combat.md</c> §22, unit 2).
///
/// <para>
/// <b>A static class rather than an autoload, unlike every other manager here.</b> It holds no state
/// at all — a mend is a flag and a cleansed screen is a flag, so there is nothing for a node to own.
/// The deciding argument is the failure mode: a manager that must be registered in
/// <c>project.godot</c> can be missing, and a missing <c>MendManager.Instance</c> would make every
/// mend in the game silently do nothing while all 12 structural gates stayed green. That is precisely
/// the blind spot <c>handoff.md</c> keeps a list of. <see cref="World.InteractionService"/> is the
/// precedent: pure rules, no tree, drivable headlessly.
/// </para>
///
/// <para>
/// <b>If a second thing ever purifies a creature, promote this to an autoload</b> subscribing to
/// <c>EntityStateChanged</c> — do not sprinkle <see cref="Reevaluate"/> calls at each new site. Today
/// there is exactly one purify path (<c>InteractionService.ActivateEntity</c>) and one mend path
/// (<see cref="Mend"/>), so two explicit calls are honest; three would be a signal wearing a disguise.
/// </para>
/// </summary>
public static class MendManager
{
    /// <summary>
    /// The refusal, in one place because two callers need it: <see cref="Mend"/> guards the seam and
    /// <c>InteractionService</c> guards the click, since a mended blight must not open an empty board.
    /// </summary>
    public const string AlreadyMended = "Already put right. Your own work, and it is holding.";

    // ---- Mending ---------------------------------------------------------

    /// <summary>Has this blight already been put right?</summary>
    public static bool IsMended(HotspotData blight) =>
        blight != null && WorldStateManager.Instance?.GetFlag(blight.ResolvedMendedFlag) == true;

    /// <summary>
    /// Whether clicking this would open a board. Asked by the view and by the panel, so that a mended
    /// blight is refused in one place rather than in each caller.
    /// </summary>
    /// <remarks>
    /// Deliberately says nothing about whether the puzzle is <i>solvable</i>
    /// (<see cref="MendPuzzleData.IsSolvable"/>). An out-of-range solution index is a content bug that
    /// <c>--verify-slice</c> fails on; folding it in here would turn it into a shrug in play instead,
    /// which is the harder version to notice.
    /// </remarks>
    public static bool CanMend(HotspotData blight) =>
        blight != null && !IsMended(blight) && CarriesABoard(blight);

    /// <summary>
    /// Whether this hotspot's click is a board rather than its usual verb. True for every
    /// <see cref="HotspotKind.Mend"/>, and for the three major repairs of <c>items.md</c> §6 — a
    /// <see cref="HotspotKind.Station"/> with a <see cref="HotspotData.MendPuzzle"/> on it — once the
    /// repair has been assembled.
    /// </summary>
    /// <remarks>
    /// <b>Added 2026-08-05, and the widening is deliberately this narrow.</b> §22 rules that the three
    /// majors get a jigsaw "on the same terms", and `Enums.cs` already authored them as stations whose
    /// recipe outputs a flag. What was missing was only the carrier: the board had nowhere to hang,
    /// because both this method and <see cref="UI.MendPanel.Open"/> tested the kind directly.
    /// <para>
    /// <b>A repair is not a blight, and only this method widened.</b> <see cref="IsFouled"/>,
    /// <see cref="IsCleansed"/> and the room sweep still count <see cref="HotspotKind.Mend"/> alone, so
    /// a broken bridge does not foul its screen and cannot be the last thing standing between a screen
    /// and cleansed. It also pays no Graveflower — a repair station carries no <c>ItemId</c>, and
    /// <see cref="Mend"/> already says "it holds" rather than naming a yield when there is none. Both
    /// matter to the good ending: <c>GateMending</c> bills exactly twelve blights and the exorcism
    /// engine is priced at eight of their flowers, so a thirteenth would be a flower nobody costed.
    /// </para>
    /// <para>
    /// <b>The board is the last step, not the first.</b> The recipe is what costs commons and what the
    /// reclaimed memory gates, so the station opens the crafting panel until that recipe has been made
    /// and the board once it has — which is §22's <i>"the puzzle is not a gate, it is the animation"</i>
    /// in the only order that leaves the price somewhere.
    /// </para>
    /// </remarks>
    public static bool CarriesABoard(HotspotData blight) => blight?.Kind switch
    {
        HotspotKind.Mend => true,
        HotspotKind.Station => blight.MendPuzzle != null && IsAssembled(blight),
        _ => false,
    };

    /// <summary>
    /// Has the repair this station is for been crafted yet? Read off the recipe registry rather than a
    /// field on the hotspot: the repair recipe already names its venue
    /// (<see cref="Data.RecipeData.RequiresStation"/>) and already names the flag it writes
    /// (<see cref="Data.RecipeData.SetsFlag"/>), so a station and its repair cannot be authored into
    /// disagreeing about which belongs to which.
    /// </summary>
    /// <remarks>
    /// A station with a board but no repair recipe authored for it yet returns false rather than
    /// opening: an unbuilt bridge whose recipe is missing should refuse, and <c>--verify-slice</c>
    /// fails on the pairing separately so this is never how the mistake is found.
    /// </remarks>
    public static bool IsAssembled(HotspotData station)
    {
        var flag = RepairAt(station?.StationId)?.SetsFlag;
        return !string.IsNullOrEmpty(flag) && WorldStateManager.Instance?.GetFlag(flag) == true;
    }

    /// <summary>The world-repairing recipe made at this station, if there is one.</summary>
    public static RecipeData RepairAt(string stationId) =>
        string.IsNullOrEmpty(stationId)
            ? null
            : InventoryCraftingManager.Instance?.Recipes.Values.FirstOrDefault(
                r => r != null && r.RepairsWorld && r.RequiresStation == stationId);

    /// <summary>
    /// The blight is put right: set the flag, pay the authored yield, re-check the screen. Returns
    /// false when refused rather than performed.
    /// </summary>
    /// <remarks>
    /// <b>This is the seam, and it is one method wide on purpose (§22).</b> The panel calls it when the
    /// board is solved and <c>--verify-slice</c> calls it directly, so no gate ever has to simulate a
    /// puzzle — which is what lets the click puzzle be replaced by real draggable pieces later without
    /// touching a single <c>.tres</c> or a single check.
    /// <para>
    /// It costs nothing. There is no material price, no Vitality, no Resolve: <c>items.md</c> §6 prices
    /// a blight at <i>"a jigsaw, and nothing else"</i>, so the only thing spent here is attention.
    /// </para>
    /// </remarks>
    public static bool Mend(HotspotData blight)
    {
        if (!CarriesABoard(blight)) return false;

        var world = WorldStateManager.Instance;

        if (IsMended(blight))
        {
            // The Harvest shape: refuse out loud rather than paying twice. Unlike a spent harvest node
            // this hotspot is still drawn — the maker's mark (§22) belongs where the work was done — so
            // the refusal is the only thing telling the player they already did this.
            EventBus.Instance?.EmitNotify(AlreadyMended);
            return false;
        }

        world?.SetFlag(blight.ResolvedMendedFlag);
        if (!string.IsNullOrEmpty(blight.SetsFlag)) world?.SetFlag(blight.SetsFlag);

        var paid = 0;
        if (!string.IsNullOrEmpty(blight.ItemId) && blight.ItemAmount > 0)
        {
            InventoryCraftingManager.Instance?.AddItem(blight.ItemId, blight.ItemAmount);
            paid = blight.ItemAmount;
        }

        if (paid > 0)
        {
            var name = InventoryCraftingManager.Instance?.DisplayNameOf(blight.ItemId) ?? blight.ItemId;
            EventBus.Instance?.EmitNotify(
                $"The last piece seats. Something green pushes up through the seam — {name} x{paid}.");
        }
        else
        {
            EventBus.Instance?.EmitNotify("The last piece seats. It holds.");
        }

        Reevaluate(RoomHolding(blight), announce: true);
        return true;
    }

    /// <summary>
    /// Which screen a blight sits on. Searched for rather than stored on the hotspot: a hotspot
    /// already lives inside exactly one room's list, so a <c>RoomId</c> field beside it would be the
    /// same fact twice, in the version that can disagree with where the file actually puts it.
    /// </summary>
    /// <remarks>
    /// Public because <c>--verify-slice</c> mends blights it is not standing in, and the registry scan
    /// below is the only reason that works. In play the first branch always hits.
    /// </remarks>
    public static RoomData RoomHolding(HotspotData blight)
    {
        var nav = NavigationManager.Instance;
        if (nav == null || blight == null) return null;

        // The common case by far, and the only one that happens in play: you are standing on it.
        if (nav.CurrentRoom?.Hotspots?.Contains(blight) == true) return nav.CurrentRoom;

        foreach (var room in nav.Rooms.Values)
            if (room?.Hotspots?.Contains(blight) == true) return room;

        return null;
    }

    // ---- The cleansed rule -----------------------------------------------

    /// <summary>
    /// §22's <b>Held</b>: something is still standing on this screen that should not be.
    /// </summary>
    /// <remarks>
    /// Anything not <see cref="EntityState.Purified"/> counts, so a <see cref="EntityState.Severed"/>
    /// body holds the screen too — the anchor is cut but the thing is still there, which is exactly the
    /// state §5's second beat exists to describe. A missing <c>EntityStateManager</c> reads as
    /// Corrupted, so the failure direction is "nothing cleanses" rather than "everything does".
    /// </remarks>
    public static bool IsHeld(RoomData room)
    {
        if (room?.Entities == null) return false;

        return room.Entities.Any(e =>
            e != null &&
            !string.IsNullOrEmpty(e.InstanceId) &&
            (EntityStateManager.Instance?.GetState(e.InstanceId) ?? EntityState.Corrupted)
                != EntityState.Purified);
    }

    /// <summary>§22's <b>Fouled</b>: a blight on this screen has not been mended.</summary>
    /// <remarks>
    /// Counts every authored <see cref="HotspotKind.Mend"/> whether or not it is currently
    /// <i>visible</i>. A blight behind a shut gate leaves the screen uncleansed, which is the right
    /// answer — the broken thing is on the screen either way, and the alternative would let a screen
    /// read finished because the work on it is out of sight.
    /// </remarks>
    public static bool IsFouled(RoomData room)
    {
        if (room?.Hotspots == null) return false;

        return room.Hotspots.Any(h => h != null && h.Kind == HotspotKind.Mend && !IsMended(h));
    }

    /// <summary>
    /// The verdict: is there anything left to do on this screen? Computed, never read from the flag —
    /// <see cref="Reevaluate"/> is what makes the two agree.
    /// </summary>
    /// <remarks>
    /// <b>§22 names three reasons a screen is unfinished and this checks two.</b> The missing one is
    /// <b>Blocked</b>, and leaving it out is deliberate: a tool gate is not screen state. Learning the
    /// Pry Bar resolves every Blocked door in the world at once, so a screen whose only unfinished
    /// thing is a door would flip to cleansed with no act performed <i>on that screen</i> — and the
    /// colour coming back has to be caused by something you did here.
    /// <para>
    /// Checked against all three ledgers, this costs one screen: every Blocked screen also carries a
    /// creature or a blight, except the three major repairs — whose boards are
    /// <see cref="HotspotKind.Station"/> and so are invisible to <see cref="IsFouled"/> by design
    /// (see <see cref="CarriesABoard"/>), leaving a broken bridge on a screen that reads cleansed — and
    /// <b>the Snarl, which reads cleansed from the start</b> and is a known wart. Unit 6 owns that
    /// screen's door. The note belongs here rather than as a special case in the predicate — a
    /// hard-coded room id would outlive the reason for it.
    /// </para>
    /// </remarks>
    public static bool IsCleansed(RoomData room) =>
        room != null && !IsHeld(room) && !IsFouled(room);

    /// <summary>
    /// Bring <c>screen.&lt;id&gt;.cleansed</c> into line with the verdict. Returns the verdict.
    /// </summary>
    /// <remarks>
    /// <b>The flag is a cache of <see cref="IsCleansed"/>, not a second source of truth.</b> It exists
    /// because content gates on flag names — a <c>RequiredFlag</c> cannot call a predicate — and
    /// because a flag round-trips through the save with no field of its own. So this writes in
    /// <i>both</i> directions even though nothing in the game can currently un-purify a creature or
    /// un-mend a blight: the false branch is what makes "cache" true rather than aspirational, and it is
    /// what lets the debug panel print the verdict and the flag side by side and expect them to agree.
    /// <para>
    /// <b>Silent by default, which is the point of the parameter.</b> Two callers re-derive rather than
    /// cause: <see cref="NavigationManager.GoToRoom"/> syncs the screen you walk onto (which is what
    /// makes an old save with no cleansed flags correct itself, and what stops the ~47 skeleton screens
    /// that have nothing authored on them yet from being permanently out of step with their own flag),
    /// and a load restores. Neither is an achievement, and announcing one would be the same mistake as
    /// narrating a rest that did not happen — see <c>CampManager.ImportReturnCamp</c>. Only mending and
    /// purifying pass <c>announce: true</c>.
    /// </para>
    /// </remarks>
    public static bool Reevaluate(RoomData room, bool announce = false)
    {
        if (room == null || string.IsNullOrEmpty(room.Id)) return false;

        var world = WorldStateManager.Instance;
        var flag = Flags.Cleansed(room.Id);
        var verdict = IsCleansed(room);

        var flipped = verdict && world?.GetFlag(flag) != true;
        world?.SetFlag(flag, verdict);

        if (flipped && announce)
            EventBus.Instance?.EmitNotify("Nothing left here that shouldn't be. Air moves. Somewhere, birds.");

        return verdict;
    }
}
