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
    /// ~~<b>"Exactly 24" became "up to 24" in unit 3a</b>, and the missing ones went to <c>Killed</c>.~~
    /// <b>It is back to exactly 24, and to a single domain, as of 2026-08-06.</b> The overkill route never
    /// existed for a person: severing an ordinary creature has no route in the game, so the <c>killed.*</c>
    /// branch took every regular fight and every hotspot on this side stayed shut. Jeremy's ruling —
    /// <i>"all non-bosses live… you more or less bonk them on the head and wake them up"</i> — deleted the
    /// other domain rather than the plumbing. The comment block where <c>Killed</c> stood carries the whole
    /// account.
    /// </para>
    /// <para>
    /// <b>The 24 did not move when the bosses landed on 2026-08-05, and that is the point of the split.</b>
    /// <c>defeated.*</c> went 6 → 8 and reached 9 with Iris, while this side stayed exactly where it was — a
    /// boss frees nobody, so it cannot appear here even though two of the three were people. The invariant is
    /// now the flat <c>freed.* == 24</c>; only the other domain ever grew.
    /// </para>
    /// </remarks>
    public static string Freed(string instanceId) => $"freed.{instanceId}";

    // **`Killed(instanceId)` stood here and was deleted 2026-08-06.** It meant *a person who died still
    // tethered* — the overkill route — and it was written in unit 3a alongside the rule
    // `tethered-dies-untethered-survives`. **Nothing could ever write it.** Severing an ordinary creature
    // has no route in the game: `EntityStateManager.Sever` is called from exactly one place, a boss pocket
    // cutting its own `TargetEntityId`, and no pocket targets a villager. So every one of the 30 regular
    // fights took the killed branch and all 24 `freed.*` hotspots stayed shut — found in play on
    // 2026-08-05 by beating the Apothecary and watching her notes stay locked.
    //
    // Jeremy ruled the *premise* wrong rather than the plumbing: **"All non-bosses live — they have minor
    // demons possessing them. They have no dream worlds to conquer. You more or less bonk them on the head
    // and wake them up. This is why they don't have auras as well like bosses."** So there is no overkill
    // route for a person to fail, and severing is what weakens one of the three bosses or reclaims
    // knowledge. `EncounterManager.Finish` now derives two flags from one question — is there a person in
    // there — and this domain has nothing left to mean.
    //
    // **Deleted rather than kept and documented, and that is the whole lesson of the bug.** A flag with no
    // writer is a flag any future hotspot can gate on and never open, which is precisely the defect being
    // fixed; leaving the helper in place would have preserved the trap. `GateContent` now asserts that no
    // hotspot gates on a flag nothing writes, so re-adding it with content behind it goes red.
    //
    // The ending's promise moves with it: **`freed.* + killed.* == 24` becomes `freed.* == 24`**
    // (`combat.md` §20, `items.md` §10).

    /// <summary>
    /// A creature put down that <b>frees nobody</b> — the six Beast sightings and the bosses
    /// (<c>items.md</c> §10: <i>"it is slain, not freed"</i>).
    /// </summary>
    /// <remarks>
    /// A separate domain rather than reusing <see cref="Freed"/>, so that the human half of the roster
    /// stays at 24 and <c>defeated.*</c> at 8. Sharing one name would have been cheaper by a line and
    /// would have made both counts unreadable — and counting by prefix is the only reason these are
    /// functions instead of string literals in the .tres files. <b>Unit 3a made that argument a third
    /// time</b> and reached the same answer: see <see cref="Killed"/>.
    /// <para>
    /// <b>This used to read "the six Beast sightings, and nothing else", and it used to mean "was never a
    /// person". Jeremy widened it on 2026-08-05 when the Blacksmith was placed.</b> His spirit is broken
    /// in the Dream, his demonic form is weakened, and the body dies with the demon that was holding it up
    /// — so he is a person, and nobody walks away from his fight. The domain is therefore about the
    /// *outcome*, not the *nature*: it means the ending's bill cannot count this one as saved. <c>combat.md</c>
    /// §20's <i>"begs, is freed, thanks you, and dies"</i> is still the scene; the freeing is what the scene
    /// is *about*, and the flag records where it left him. Nothing needed implementing for that — see
    /// <see cref="Killed"/>, where the choice of flag is derived from a blank <c>EntityData.PurifiedName</c>
    /// and never authored.
    /// </para>
    /// <para>
    /// <b>The carcass did not widen with it, and that asymmetry is a ruling rather than an oversight.</b>
    /// The Beast is still the only thing you may harvest — Jeremy, the same day: <i>"no loot, since it would
    /// be dishonourable to loot a human corpse."</i> So the harvest argument is now scoped to the Beast
    /// **by what it is**, where it used to ride along on what this domain meant, and the two can no longer
    /// be conflated. The drop is a hotspot the room places behind this flag, exactly mirroring the NPC
    /// hotspot a freed person appears as; <c>EntityData.HarvestItemId</c> stays unused, and both bosses
    /// leave it blank.
    /// <para>
    /// <b>Unenforced, and deliberately so — Jeremy declined a gate on it, 2026-08-05: <i>"don't think it has
    /// or needs this logic"</i>.</b> The offer was one check reading "a body that projects an aura carries no
    /// harvest", since <c>EntityData.CorruptionAura</c> marks the three bosses and nothing else and all three
    /// are people. It buys nothing today: <b>no entity file in <c>data/</c> carries a harvest at all</b>, so
    /// there is no content for it to hold in place, and the ruling above is the record. Do not read this
    /// paragraph as a queued chore — if a harvest is ever authored on a boss the suite will not object, and
    /// that is the accepted cost rather than a missing gate.
    /// </para>
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
