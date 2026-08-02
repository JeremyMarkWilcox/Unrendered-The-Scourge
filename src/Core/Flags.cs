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
    /// A person freed by severing their tether — <b>24 of the 30 regular fights</b>, one per human
    /// sighting.
    /// </summary>
    /// <remarks>
    /// This used to say "all 30", which is <c>placement.md</c> §6 correction 1 in miniature: the 30
    /// assumed nine creatures × three sightings, and the Beast is seen six times and frees nobody.
    /// The six that do not free anyone set <see cref="Defeated"/> instead, so this domain counts
    /// exactly 24 — which is the property the class comment promises, that a bill checker can count
    /// a class of thing by prefix.
    /// </remarks>
    public static string Freed(string instanceId) => $"freed.{instanceId}";

    /// <summary>
    /// A creature put down that frees nobody — the six Beast sightings, and nothing else
    /// (<c>items.md</c> §10: <i>"it is slain, not freed"</i>).
    /// </summary>
    /// <remarks>
    /// A separate domain rather than reusing <see cref="Freed"/>, so that <c>freed.*</c> stays at 24
    /// and <c>defeated.*</c> at 6. Sharing one name would have been cheaper by a line and would have
    /// made both counts unreadable — and counting by prefix is the only reason these are functions
    /// instead of string literals in the .tres files.
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
}
