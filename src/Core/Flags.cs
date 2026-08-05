namespace Unrendered.Core;

/// <summary>
/// Every world-flag name the game uses, built in one place.
/// </summary>
/// <remarks>
/// Published by unit 1 (build-plan §4) because pass 1 generates ~58 rooms at once: a flag name
/// invented per-room is a typo the gates cannot see, since <c>WorldStateManager.GetFlag</c> returns
/// <c>false</c> for a name nobody ever set and a mistyped gate simply stays shut forever.
/// <para>
/// The shape is <c>domain.subject.state</c>. Domains are deliberately distinct so a bill checker can
/// count a class of thing by prefix — how many knots are cut, how many screens are cleansed —
/// without a registry of which flag means what.
/// </para>
/// <para>
/// Several domains below belong to systems that are not built yet (mending is unit 2, recipes are
/// unit 4, pockets are unit 6). They are named now anyway: the <c>.tres</c> files that reference them
/// are generated before those units land, and the alternative is renaming flags across 58 files
/// later. Naming a flag early costs nothing; the system it names is free to change shape around it.
/// </para>
/// </remarks>
public static class Flags
{
    // ---- Built and in use today -------------------------------------------

    /// <summary>A material node already taken, so it cannot be farmed twice.</summary>
    /// <remarks>Must match <c>HotspotData.ResolvedHarvestedFlag</c>, which is the fallback form.</remarks>
    public static string Harvested(string hotspotId) => $"harvested.{hotspotId}";

    /// <summary>A story milestone. The only legal trigger for a supply change (items.md §7).</summary>
    public static string Milestone(string id) => $"milestone.{id}";

    /// <summary>
    /// A camp stood in at least once, and therefore a destination the return-to-camp verb will offer
    /// (unit 5, combat.md §17). Written by <c>CampManager</c> on arrival, beside the silent rebind of
    /// the collapse return point.
    /// </summary>
    /// <remarks>
    /// <b>A flag rather than a field on <c>SaveGame</c>, and that is the whole reason the save format did
    /// not move for this unit.</b> Flags already export, import and rebroadcast, so "which camps can I
    /// travel to" persists for free — the same trade unit 2 made when <c>ZonePhases</c> became
    /// <see cref="Cleansed"/>, and for the same reason: one vocabulary means one export.
    /// <para>
    /// <b>This is per-camp state, which §17 said it did not want, and the sentence it appears to
    /// contradict is about something else.</b> That list ruled out a picker for the *collapse return
    /// point* — an answer that is the newest camp ~95% of the time, so asking is a confirmation step.
    /// A travel destination is declined two thirds of the time, so it is a choice; and §20's revised
    /// good ending is built on backtracking being nearly free, which a single destination cannot
    /// deliver. <c>CampManager.ReturnCampRoomId</c> is untouched and still has no picker.
    /// </para>
    /// </remarks>
    public static string CampFound(string roomId) => $"camp.{roomId}.found";

    /// <summary>
    /// The milestone that re-arms a chapter's material nodes: <b>each chapter refills when that
    /// chapter's own boss is defeated</b> (Jeremy, 2026-08-02). Chapter 3 is terminal — its boss is
    /// the finale, so there is nothing left to resupply for.
    /// </summary>
    /// <remarks>
    /// Per-chapter rather than one pooled milestone, which is what <c>items.md</c> §7 used to say.
    /// Nothing in the scarce economy depends on the difference: scarce nodes never re-arm at all, so
    /// the only thing this timing governs is when common material — salve stock — comes back.
    /// </remarks>
    public static string MaterialRefill(string chapterId) => $"milestone.{chapterId}.material_refill";

    // ---- Named ahead of the systems that will set them --------------------

    /// <summary>A Dream pocket entered and finished, whatever it granted.</summary>
    public static string PocketCleared(string pocketId) => $"pocket.{pocketId}.cleared";

    /// <summary>One of the three knots behind the Snarl, cut (ledger-ch3 rows 6, 7, 10).</summary>
    public static string KnotCut(string pocketId) => $"knot.{pocketId}.cut";

    /// <summary>A blight mended, which pays its one Graveflower (unit 2, combat.md §22).</summary>
    public static string Mended(string blightId) => $"blight.{blightId}.mended";

    /// <summary>
    /// A screen with nothing Held, Blocked or Fouled left on it — the flip to
    /// <c>RoomData.WakingBackgroundPurified</c> (unit 2).
    /// </summary>
    public static string Cleansed(string roomId) => $"screen.{roomId}.cleansed";

    /// <summary>
    /// A person freed by severing their tether — <b>up to 24 of the 30 regular fights</b>, one per
    /// human sighting.
    /// </summary>
    /// <remarks>
    /// This used to say "all 30", which is <c>placement.md</c> §6 correction 1 in miniature: the 30
    /// assumed nine creatures × three sightings, and the Beast is seen six times and frees nobody.
    /// The six that do not free anyone set <see cref="Defeated"/> instead, so the human half of the
    /// roster counts exactly 24 — which is the property the class comment promises, that a bill checker
    /// can count a class of thing by prefix.
    /// <para>
    /// <b>"Exactly 24" became "up to 24" in unit 3a</b>, and the missing ones went to
    /// <see cref="Killed"/> rather than nowhere. The count that survives as an equality is the pair:
    /// <c>freed.* + killed.* == 24</c>, with <c>defeated.* == 6</c>. See <see cref="Killed"/> for why
    /// a third domain was cheaper than the alternatives.
    /// </para>
    /// </remarks>
    public static string Freed(string instanceId) => $"freed.{instanceId}";

    /// <summary>
    /// A person who died still tethered — killed by overkill rather than freed by severing. The other
    /// half of <see cref="Freed"/>'s 24, and <b>the only flag in the game that records a failure you
    /// chose</b>.
    /// </summary>
    /// <remarks>
    /// <b>Added by unit 3a, because the fight had two endings and one name for them.</b>
    /// <c>tethered-dies-untethered-survives</c> is the rule: cut the tether and the person walks away,
    /// kill the body while the tether holds and the person dies with it. Both routes end the same fight
    /// and only one of them frees anybody, so with two domains there was nowhere to write the
    /// difference.
    /// <para>
    /// <b>Not folded into <see cref="Defeated"/>, though it was one line cheaper.</b> That domain's own
    /// remark says it was split off <see cref="Freed"/> precisely so both counts stay readable; sharing
    /// it here would have made <c>defeated.*</c> mean "the Beast, plus however many villagers you
    /// murdered" — unreadable in exactly the way the split was meant to prevent, and it is the ending's
    /// bill that reads these.
    /// </para>
    /// <para>
    /// <b>Which flag a win writes is derived, never authored.</b> A creature with a blank
    /// <c>EntityData.PurifiedName</c> was never a person, so it can only be <see cref="Defeated"/>; one
    /// with a name set gets <see cref="Freed"/> or this, depending on whether the body was
    /// <c>Severed</c> when it fell. So there is no per-creature field anyone can author wrongly, and
    /// the moral weight of the overkill route lives in a count rather than in a special case.
    /// </para>
    /// </remarks>
    public static string Killed(string instanceId) => $"killed.{instanceId}";

    /// <summary>
    /// A creature put down that frees nobody — the six Beast sightings, and nothing else
    /// (<c>items.md</c> §10: <i>"it is slain, not freed"</i>).
    /// </summary>
    /// <remarks>
    /// A separate domain rather than reusing <see cref="Freed"/>, so that the human half of the roster
    /// stays at 24 and <c>defeated.*</c> at 6. Sharing one name would have been cheaper by a line and
    /// would have made both counts unreadable — and counting by prefix is the only reason these are
    /// functions instead of string literals in the .tres files. <b>Unit 3a made that argument a third
    /// time</b> and reached the same answer: see <see cref="Killed"/>.
    /// <para>
    /// What it gates today is the carcass: the Beast is the one creature you may harvest, because it
    /// is the one that was never a person. See <c>EntityData.HarvestItemId</c>, which stays unused —
    /// the drop is a <see cref="HotspotKind.Harvest"/> the room places behind this flag, exactly
    /// mirroring the NPC hotspot a freed person appears as.
    /// </para>
    /// </remarks>
    public static string Defeated(string instanceId) => $"defeated.{instanceId}";

    /// <summary>A recipe known. This is also how a tool opens a door (unit 4 — no new field needed).</summary>
    public static string Learned(string recipeId) => $"learned.{recipeId}";

    /// <summary>One of her four scattered notes, read.</summary>
    public static string NoteRead(string noteId) => $"note.{noteId}.read";

    /// <summary>
    /// A Dream verb learned, by pulling the anchor in a boss pocket (unit 6, combat.md §19).
    /// </summary>
    /// <remarks>
    /// <b>Only ever two of these exist</b>: <c>verb.Float.learned</c> and <c>verb.Flare.learned</c>.
    /// <see cref="DreamVerb.Suppress"/> is held from the first frame, so it is deliberately *not* flagged —
    /// a flag nothing can fail to have is a flag every reader has to remember to ignore. Geometry gated on
    /// Suppress would be geometry gated on nothing.
    /// <para>
    /// Its own domain rather than reusing <see cref="Learned"/>, which is recipes. Counting a class of thing
    /// by prefix is the whole reason these are functions instead of string literals, and *"how many verbs
    /// does the player have"* and *"how many recipes"* are different questions the ending's bill asks
    /// separately.
    /// </para>
    /// </remarks>
    public static string VerbLearned(DreamVerb verb) => $"verb.{verb}.learned";
}
