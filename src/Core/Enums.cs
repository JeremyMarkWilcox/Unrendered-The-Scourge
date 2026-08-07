namespace Unrendered.Core;

/// <summary>
/// The three things you can do in the Dream, all drawing on one Resolve meter and all draining
/// <b>while held</b> (docs/combat.md §19). Jeremy is open to more later; three is the set that exists.
/// </summary>
/// <remarks>
/// <b>The eight max-Resolve increments exist because of these</b> (`items.md` §7): the meter is a fuel tank
/// for verbs, never a clock, which is why the per-second heist drain was the wrong model and retired with it.
/// <para>
/// <b>Only two are ever granted.</b> <see cref="Suppress"/> is yours from the first frame, so nothing hands
/// it over and no geometry gates on holding it. <see cref="Float"/> and <see cref="Flare"/> come from
/// **pulling the anchor** in boss pockets 1 and 2 — ratified 2026-08-04, and note it is the *anchor* rather
/// than the Waking-world fight that follows it. Boss pocket 3 grants nothing: it is the finale, and there is
/// no game left to spend a verb in.
/// </para>
/// </remarks>
public enum DreamVerb
{
    /// <summary>Not a verb. The default, so a pocket that grants nothing says so.</summary>
    None = 0,

    /// <summary>
    /// Hide — the near field goes dark. Yours from the start, and <b>the only verb boss pocket 1 can be
    /// solved with</b>, since Float is what is inside it.
    /// </summary>
    Suppress = 1,

    /// <summary>Slow rise and slight lateral drift. Vertical traversal without ladders.</summary>
    Float = 2,

    /// <summary>
    /// Push the light out: hidden geometry, the anchor and the tether lines become visible — and everything
    /// in the pocket can see you. The exact mirror of <see cref="Suppress"/>, which is why it is the right
    /// third one.
    /// </summary>
    Flare = 3,
}

/// <summary>Which of the two worlds the player is currently perceiving.</summary>
public enum GameRealm
{
    /// <summary>Cold, desaturated, high-res reality.</summary>
    Waking = 0,

    /// <summary>The pixelated nightmare realm where spirit anchors live.</summary>
    Dream = 1,
}

/// <summary>
/// The persistent condition of an entity. Note this is NOT the same axis as the GDD's
/// "Form 1 / 2 / 3" — a Form is what a state <i>looks like in a given realm</i>.
/// See <see cref="Unrendered.Managers.EntityStateManager"/> for the resolution table.
/// </summary>
public enum EntityState
{
    /// <summary>Possessed. Nightmare in the Waking world, pristine noble in the Dream.</summary>
    Corrupted = 0,

    /// <summary>Anchor cut. Deformed and weakened in reality, spirit reeling in the Dream.</summary>
    Severed = 1,

    /// <summary>Exorcised. Mundane wildlife, farmable, harmless.</summary>
    Purified = 2,
}

/// <summary>What a hotspot does when the player clicks it.</summary>
public enum HotspotKind
{
    /// <summary>A doorway or path. Moves the view to another room.</summary>
    Navigate = 0,

    /// <summary>Flavour text / lore. Prints to the notification line.</summary>
    Examine = 1,

    /// <summary>Targets a placed entity — sever it, or pet it once purified.</summary>
    Entity = 2,

    /// <summary>A resource node. Yields an item, once.</summary>
    Harvest = 3,

    /// <summary>Generic world interaction that sets a flag.</summary>
    Interact = 4,

    /// <summary>A relic in the Waking world. Touching it begins a Dream heist.</summary>
    CrossToDream = 5,

    // `TakeAnchor = 6` stood here and was deleted 2026-08-04 with unit 6b. It was the anchor at the far end
    // of the 2D maze, and the objective lives **inside the 3D pocket** now — a pocket scene calls
    // `PocketManager.Complete()` when the player reaches it, so there is no Waking-world hotspot for taking
    // anything. Its sibling `CrossToDream` survives with a new owner, which is why one went and one stayed.
    //
    // **The number is deliberately not reused.** `Workbench` stays at 7 and the gap is left open: every
    // authored `.tres` stores `Kind` as an integer, so renumbering would silently repoint 59 rooms' worth of
    // hotspots at a different verb. A hole in an enum is free; a shifted one is a content migration.

    /// <summary>
    /// The bench. Placing one of these is what *makes* a room a camp — there is no separate camp
    /// flag, deliberately (`combat.md` §17). Jeremy: "the room you put the workbench in."
    ///
    /// Two facts that must always agree are better stored as one, and a `RoomData.IsCamp` bool
    /// beside this would be the second one: a bench outside a camp, or a camp with no bench, are
    /// both authoring mistakes that could not otherwise be prevented. So <see cref="Unrendered.Managers.CampManager"/>
    /// derives camp-ness by looking for this kind, and the old `IsSanctuary` flag is gone.
    /// </summary>
    Workbench = 7,

    /// <summary>
    /// A broken made-thing. Clicking it presents a jigsaw; solving it pays the authored yield and
    /// can cleanse the screen (`combat.md` §22, unit 2).
    ///
    /// <para>
    /// <b>Named for the verb, not the noun, and deliberately.</b> §22 is emphatic that the thing is a
    /// <i>blight</i> and the act is <i>mending</i> — and the noun does survive, in the flag
    /// (<c>blight.&lt;id&gt;.mended</c>, see <see cref="Flags.Mended"/>). But a kind is what you *do*
    /// to a region: every member above is a verb except <see cref="Entity"/> and
    /// <see cref="Workbench"/>. The deciding case is the one that has not been authored yet — the
    /// three major repairs share this interaction (§22: *"the three major repairs get one too"*), and a
    /// broken bridge clicked as `Blight` would be a lie about what is on the screen.
    /// </para>
    ///
    /// <para>
    /// <b>This is not the deleted hazard wearing a new name.</b> A hazard was a `VitalityCost` on any
    /// existing kind; a mend charges nothing, gates the cleanse and pays out. It shares no field with
    /// it, which is why that one was deleted rather than renamed.
    /// </para>
    /// </summary>
    Mend = 8,

    /// <summary>
    /// A place work gets done — the mortar, the anvil, the trapper's stump (`combat.md` §19). Clicking
    /// one opens the crafting panel narrowed to the recipes made there, named by
    /// <see cref="Data.HotspotData.StationId"/>.
    ///
    /// <para>
    /// <b>A station is not a camp, and that separation is the whole point.</b>
    /// <see cref="Workbench"/> remains the only thing <see cref="Managers.CampManager"/> derives
    /// camp-ness from; if a station refilled both pools, every herb bank would be a rest stop and the
    /// body economy would be gone in one move. What a camp keeps exclusively is fusion.
    /// </para>
    ///
    /// <para>
    /// <b>The world itself can be one.</b> A collapsed bridge is a station whose recipe outputs a flag
    /// instead of an item (<see cref="Data.RecipeData.SetsFlag"/>) — same kind, same venue check, and
    /// the three major repairs of `items.md` §6 are authored against it.
    /// </para>
    /// </summary>
    Station = 9,
}

/// <summary>
/// The four kinds of harm, ratified 2026-07-30 (docs/combat.md §18). Every creature has one type
/// it is vulnerable to and one it resists — the right type does double, the wrong one half, and
/// neither is ever zero. There is no Defence number; this pair replaced it.
///
/// Three of the four are the craftsman's own work and the fourth is his wife's, which is why
/// <see cref="Caustic"/> is taught only by the Physick side of the journal
/// (<see cref="KnowledgeTrack"/>). The split is 3-to-1 on purpose: §8 says Physick starts near zero
/// and grows badly, so there is less of her in the vocabulary and it arrives late.
/// </summary>
public enum DamageType
{
    /// <summary>
    /// Not a weapon. First so that a reagent, a relic or a piece of lore does not silently
    /// default to dealing edge damage — the same reason <see cref="CraftBlock.None"/> is 0.
    /// </summary>
    None = 0,

    /// <summary>Blades, spikes, snares.</summary>
    Edge = 1,

    /// <summary>Weights, falls, hammers.</summary>
    Blunt = 2,

    /// <summary>Oil, pitch, brands.</summary>
    Fire = 3,

    /// <summary>Her chemistry. Physick knowledge only.</summary>
    Caustic = 4,
}

/// <summary>
/// Where a piece of equipment is worn. Two slots on purpose (docs/combat.md §18): one weapon
/// carrying a <see cref="DamageType"/>, one armour halving an incoming one.
///
/// A shield is deliberately absent rather than forgotten. Deferring it is safe precisely because
/// this is an enum and an item declares its own slot, so adding a third value re-authors nothing —
/// and the version worth building is a *verb* (brace, costing a turn) rather than a second passive
/// stat, which cannot be judged until the encounter loop has been played.
/// </summary>
public enum EquipSlot
{
    /// <summary>Not equipment. Everything that is not worn.</summary>
    None = 0,

    /// <summary>Carries the damage type you deal. There are no weapon tiers (§11).</summary>
    Weapon = 1,

    /// <summary>Halves one incoming damage type. One at a time, never a set.</summary>
    Armour = 2,
}

/// <summary>
/// Which half of the journal a piece of knowledge belongs to. Both halves are *found* — recipe
/// discovery is by pickup only, and trial-and-error brewing was rejected on 2026-07-30 — so this
/// is a label on knowledge rather than a score.
///
/// Note that neither track is a number: per docs/combat.md §8 both are simply the *set* of recipes
/// you know, and only Attunement is numeric. That is invariant 5 paying off — two thirds of
/// progression cannot even be expressed as a multiplier.
/// </summary>
public enum KnowledgeTrack
{
    /// <summary>His own craft, stolen by the demon and reclaimed in the Dream.</summary>
    Craft = 0,

    /// <summary>His wife's notes, found in the Waking world. Teaches Caustic damage.</summary>
    Physick = 1,
}

/// <summary>
/// How wounded a body reads. §5's ladder, and the <b>only</b> readout a fight ever gives — the
/// durability number exists for the resolver and is never shown.
///
/// Ordered from healthy to nearly dead so the ordinal is meaningful: a condition getting *worse* is
/// an increasing value, which is what lets a UI compare two without a lookup table. Derived from
/// remaining durability as fractions in <see cref="GameConfig"/>, never authored per creature —
/// §18's reason is that an authored ladder lets a creature exist with no description for some band,
/// and a content pass finds that hole at the worst possible moment.
/// </summary>
public enum Condition
{
    /// <summary>Untouched. Also what an unwounded body reports before any fight has happened.</summary>
    Unmarked = 0,

    Bleeding = 1,
    Faltering = 2,

    /// <summary>About to come apart. Not the same as dead — the resolver owns that.</summary>
    Collapsing = 3,
}

// `HeistOutcome` — Success / Captured / Exhausted — stood here and went with unit 6b. It was the payload of
// `HeistEnded`, and a pocket has **two** exits rather than three: `Complete` and `Leave`, carried as a bool
// on `PocketLeft`. That is not a simplification for its own sake, it is the design changing underneath: two
// of the three endings were the pursuer and the drain clock, and a quiet pocket has neither.
//
// A third outcome comes back if a boss pocket ever ejects you on its own terms, and it should be named then
// rather than kept warm now — an enum member with no producer reads as a state the game can reach.

// `RealmVisibility` stood here — Both / WakingOnly / DreamOnly, the per-hotspot filter that made
// spectral vision work. Deleted 2026-08-02 with the rest of it (the great swap). A Waking screen
// now shows everything it has; the Dream is a place you travel to in 3D, not an overlay you peek
// through, so "which realm can you see this from" stopped being a question a 2D hotspot can answer.
// `GameRealm` itself survives, because *where you are* is still a real distinction.

/// <summary>Which procedural scene a room draws while it has no painted background.</summary>
public enum BackdropStyle
{
    /// <summary>Treeline, mist, a track receding into it.</summary>
    Wilderness = 0,

    /// <summary>Walls, a doorway, a floor. Ruins and interiors.</summary>
    Interior = 1,

    // `Dream = 2` — pillars and a void sun, drawn into the 480x270 SubViewport under every Waking
    // screen. Deleted 2026-08-02: the Dream is 3D now and draws nothing through this class.
}

/// <summary>
/// Why a recipe cannot be made. Exists so the crafting panel can grey a row and say which kind of
/// "no" it is — not knowing how is a different sentence from being short of bitterroot, and the
/// two should never be shown with the same words.
/// </summary>
public enum CraftBlock
{
    /// <summary>Nothing in the way; it can be made.</summary>
    None = 0,

    /// <summary>No such recipe, or it has no output. A content error rather than a player state.</summary>
    NoSuchRecipe = 1,

    /// <summary>The blueprint has not been found. Do not list the inputs — you do not know them.</summary>
    Unknown = 2,

    /// <summary>Known, but you are short of something.</summary>
    MissingMaterials = 3,

    /// <summary>
    /// You know how and you have the materials, but this is not the place. ~~Crafting happens at camps
    /// and nowhere else (`combat.md` §17).~~ **Renamed from `NotAtCamp` 2026-08-03 (§19), same value** —
    /// camps are no longer the only venue, because a mortar in a chapel and an anvil in a forge are
    /// stations too (<see cref="Data.RecipeData.RequiresStation"/>).
    ///
    /// <para>
    /// <b>The rename is not a widening of what refuses; it is a narrowing of what the refusal means.</b>
    /// The old name promised a destination — *go to a camp* — and §19 made that promise false in two
    /// directions at once: a field station is somewhere else you may craft, and the camp bench still
    /// satisfies every station, so "not at camp" was about to describe neither the cause nor the cure.
    /// </para>
    ///
    /// <para>
    /// <b>It cannot dead-end anyone</b>, and that is worth stating because a venue block is exactly the
    /// shape that usually can. A camp is a menu reachable from anywhere (§17, 2026-08-01) and satisfies
    /// every station, so the answer to this block is always available: camp, or walk to the station and
    /// save the trip. Invariant 12 holds by construction rather than by vigilance.
    /// </para>
    ///
    /// Ordered after <see cref="Unknown"/> on purpose: an unlearned recipe must not admit it exists
    /// by complaining about the venue.
    /// </summary>
    NotAtStation = 4,

    /// <summary>
    /// You already have the one that exists. A trap you own, or a repair already made — the two things
    /// §19 made permanent (2026-08-03).
    /// </summary>
    /// <remarks>
    /// <b>This is the only <c>CraftBlock</c> that is good news</b>, and the panel should word it that
    /// way: *"the snare is already built"*, not *"cannot craft"*. Every other member names something
    /// missing, so a row greyed by this one must not read like a failure.
    /// <para>
    /// It exists as its own member rather than reusing <see cref="MissingMaterials"/> — which would have
    /// been the smaller change and a lie — because the whole purpose of this enum is that the panel says
    /// *which kind of no* it is, and "you have it" is not a shortage. Evaluated after the knowledge
    /// blocks: a state you reach by succeeding cannot be the reason a recipe is refused before you have
    /// learned it.
    /// </para>
    /// </remarks>
    AlreadyMade = 5,

    /// <summary>
    /// One side of the journal is blank. The recipe needs both hands
    /// (<see cref="Data.RecipeData.RequiresBothTracks"/>) and only one has anything written on it.
    /// </summary>
    /// <remarks>
    /// <b>This is the second half of <see cref="Unknown"/>, and it exists because the first half cannot
    /// carry it.</b> <c>Unknown</c> asks whether one named page has been found — the recipe supplies the
    /// id and the answer is a set lookup. The engine's second requirement has no id to supply: the field
    /// asserting it is a bool, deliberately (*"the interesting state is both, never either"*), so there is
    /// nothing for <c>Unknown</c> to test. A different question needs a different answer, which is this
    /// member.
    /// <para>
    /// <b>Rule A, ruled 2026-08-05: the other hand means any page on it, not a named one.</b> Knowledge is
    /// a flat set of blueprint ids and the prefix says whose each is, so *do you know anything of hers* is
    /// already answerable — see <c>InventoryCraftingManager.KnowsAnyPageOn</c>. The alternative was a
    /// second id field on the recipe, which buys the ability to demand one specific note and costs a field
    /// that eighteen recipes leave blank.
    /// </para>
    /// <para>
    /// <b>Numbered after <see cref="AlreadyMade"/> and evaluated second</b> — appended so no existing value
    /// shifted, but read immediately after <c>Unknown</c>, because it is the same question about the other
    /// hand and must outrank everything a knowledge block outranks. A recipe you half-know must not start
    /// complaining about the venue or the materials, for the reason <c>NotAtStation</c> gives: a refusal
    /// that names the inputs has admitted the page exists.
    /// </para>
    /// <para>
    /// <b>It is a refusal on a side path, and no invariant leans on it.</b> The one recipe that sets the
    /// flag is the exorcism engine, which buys the good ending and is needed for nothing else —
    /// `combat.md` §20: *"a player who reaches the finale with nothing built can still finish the game.
    /// They just finish it badly."* So invariant 12 is not in play here and nothing may be gated into
    /// existence to satisfy it (ruled 2026-08-05, correcting a claim that it was).
    /// </para>
    /// </remarks>
    HalfKnown = 6,
}

/// <summary>
/// Why the shared cache refused a transfer, or <see cref="None"/>. Same shape as
/// <see cref="CraftBlock"/> and <see cref="UseBlock"/>, and it exists for the same reason: the
/// panel that greys a button must ask the same question the action asks.
/// </summary>
public enum StashBlock
{
    /// <summary>Nothing in the way; the transfer would happen.</summary>
    None = 0,

    /// <summary>No such item in the catalogue. A content error rather than a player state.</summary>
    NoSuchItem = 1,

    /// <summary>The cache only opens at a camp. There is one cache, not one per camp.</summary>
    NotAtCamp = 2,

    /// <summary>Fewer than that many, on the side you are moving them from.</summary>
    NotEnough = 3,
}

/// <summary>
/// Why the return-to-camp verb refused, or <see cref="None"/>. Unit 5, built to combat.md §17 as amended
/// 2026-08-01: <i>the menu is a shortcut in, not a bubble you stand inside.</i>
/// </summary>
/// <remarks>
/// <b>The order is the decision, and it is <see cref="CraftBlock"/>'s argument applied to a different
/// axis.</b> There, <see cref="CraftBlock.Unknown"/> beats <see cref="CraftBlock.NotAtStation"/> so a
/// player who has not found the page is not told the recipe exists. Here the two *where you are* refusals
/// come first, because a player mid-fight should be told they are mid-fight rather than that they have
/// never visited the cottage — the second sentence is true and useless, and it sends them looking for a
/// map instead of finishing the thing in front of them.
/// </remarks>
public enum TravelBlock
{
    /// <summary>Nothing in the way; the verb would carry you there.</summary>
    None = 0,

    /// <summary>
    /// A fight is running. <b>Refused rather than priced as a flight, which is the load-bearing choice in
    /// this enum</b> — see <c>CampManager.InspectTravel</c> for the full argument. Short version: a door
    /// clicked mid-fight flees and charges a parting blow, and arriving at a camp refills both pools, so
    /// one button doing both would be escape *plus* a free heal.
    /// </summary>
    InFight = 1,

    /// <summary>
    /// Inside a Dream pocket. Keyed on a live run rather than on <c>GameRealm.Dream</c>, for the reason
    /// <c>Hud.RefreshPools</c> gives at length: unit 6's quiet pockets put you in the Dream with nothing
    /// draining, and the question this asks is *can you leave from here*, not *where are you*.
    /// </summary>
    InPocket = 2,

    /// <summary>
    /// No room by that id, or one that holds no <see cref="HotspotKind.Workbench"/>. A content error
    /// rather than a player state, like <see cref="CraftBlock.NoSuchRecipe"/> — nothing the player does
    /// reaches this, because the panel only ever lists camps.
    /// </summary>
    NotACamp = 3,

    /// <summary>
    /// A real camp you have not stood in yet, so <c>Flags.CampFound</c> is unset. <b>This is the member
    /// that keeps the verb from being a map</b>: without it, a fresh game could travel to the chapter-3
    /// store from the cottage doorstep.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// You are standing in it. Its own member rather than folded into a silent success, on
    /// <see cref="CraftBlock.AlreadyMade"/>'s precedent: it is not a failure, and the panel should word it
    /// as *"you are here"* rather than greying the row like something withheld.
    /// </summary>
    AlreadyHere = 5,
}

/// <summary>
/// Why <c>PocketManager.Enter</c> would refuse. Unit 6, and the sixth member of the inspect/act family
/// (<see cref="CraftBlock"/>, <see cref="UseBlock"/>, <see cref="StashBlock"/>, <see cref="EquipBlock"/>,
/// <see cref="TravelBlock"/>) — the hotspot's availability and the act must ask one question.
/// </summary>
public enum PocketBlock
{
    /// <summary>Nothing in the way; the way in is open.</summary>
    None = 0,

    /// <summary>No pocket registered under that id. A content error, like <see cref="CraftBlock.NoSuchRecipe"/>.</summary>
    NoSuchPocket = 1,

    /// <summary>You are in one already. There is exactly one at a time.</summary>
    AlreadyInside = 2,

    /// <summary>
    /// A fight is running. <b>A correctness guard rather than a rule of taste</b> — see
    /// <c>PocketManager.InspectEnter</c>: a pocket is not a room, so entering one does not navigate, so the
    /// backstop that stops a fight outliving its screen never fires. Without this you could tour a 3D scene
    /// and come back still committed to a creature.
    /// </summary>
    InFight = 3,

    /// <summary>
    /// Already finished, so the artifact is broken. Unreachable from an authored entrance — the hotspot
    /// hides itself on <c>Flags.PocketCleared</c> — which makes this the arm that catches a second door
    /// authored onto one pocket, or a debug key.
    /// </summary>
    AlreadyTaken = 4,

    /// <summary>
    /// <b>This pocket has a 3D scene, so it must be walked rather than taken from out here.</b> The one
    /// refusal <c>PocketManager.TakeFromWaking</c> adds, and unit 10's whole safety property: a finished
    /// pocket can never hand its reward over on a click.
    /// </summary>
    /// <remarks>
    /// <b>It guards one direction, and the asymmetry is deliberate — I built it both ways first and the
    /// symmetric version was wrong.</b> The tempting shape was for <c>Enter</c> to refuse a *scene-less*
    /// pocket too, so the two paths refused each other and a routing mistake was impossible. But
    /// <c>PocketHost.OnPocketEntered</c> already documents a blank <c>ScenePath</c> as a supported state —
    /// *"a pocket with no scene is still a pocket as far as every rule is concerned… this is the state a
    /// headless gate run lives in permanently"* — so refusing it in the manager would have contradicted the
    /// host's own contract and reddened every fixture in <c>GatePockets</c>, all of which are scene-less by
    /// design.
    /// <para>
    /// <b>Only one direction was ever dangerous, which is why one guard is enough.</b> Granting a
    /// scene-built pocket's reward on a click would silently keep the stand-in alive past the thing that was
    /// supposed to retire it — unnoticeable, and it would hand out max Resolve for a pocket you were meant to
    /// walk. Entering a scene-less one is merely empty, and no authored path can do it: the routing in
    /// <c>InteractionService</c> sends those to <c>TakeFromWaking</c>, and <c>Main</c>'s debug key skips them
    /// so it cannot strand you. Get the routing condition backwards and this refusal still catches the half
    /// that matters.
    /// </para>
    /// <para>
    /// <b>The switch retires per pocket with nothing to remember.</b> It is derived from
    /// <see cref="Data.PocketData.HasScene"/>, on unit 1.5's precedent — a placeholder cue draws when there is
    /// no art, not when a flag is set — so pointing a <c>PocketData</c> at a finished scene deletes the
    /// stand-in for that pocket alone. No mode to turn off, and no way to ship it by accident.
    /// </para>
    /// </remarks>
    MustBeWalked = 5,
}

/// <summary>
/// Why an item cannot be used from the satchel. The sibling of <see cref="CraftBlock"/>, and it
/// exists for the same reason plus one: the inventory panel has to grey its Use button without
/// deciding anything itself, and duplicating the rules into the panel would put game logic
/// somewhere <c>--verify-slice</c> cannot reach.
/// </summary>
public enum UseBlock
{
    /// <summary>Nothing in the way; using it will do something.</summary>
    None = 0,

    /// <summary>No such item id. A content error rather than a player state.</summary>
    NoSuchItem = 1,

    /// <summary>Not in the satchel.</summary>
    NotHeld = 2,

    /// <summary>
    /// Held, but not a thing you click. Components, lore and catalysts all land here — a catalyst is
    /// a Dream loadout spent by crossing, not something you use in a satchel.
    ///
    /// <b>Traps and equipment land here too, and for the same reason: they are spent somewhere else.</b>
    /// A trap is rigged into a site before a fight and a weapon is worn, so neither is "used" from the
    /// satchel — which keeps this refusal about the *satchel* rather than about usefulness.
    /// </summary>
    NotUsable = 3,

    /// <summary>
    /// Usable, but there is no wound to treat. A refusal rather than a waste: her salves are finite
    /// and uncraftable at the good end of the scale (invariant 7).
    /// </summary>
    NothingToTreat = 4,

    /// <summary>
    /// A fight is running, so the satchel is not where an item gets spent — <c>EncounterManager.Treat</c>
    /// is, and it charges a turn and counts against <c>GameConfig.SalveSlotsPerFight</c>.
    /// </summary>
    /// <remarks>
    /// <b>Added 2026-08-04 to close a hole, not to tidy an interface.</b> Jeremy found the satchel opening
    /// during a fight, and the draw order was the least of it: <c>UseItem</c> had no fight guard, so the
    /// satchel's Use button healed for **free, unlimited, and without ending the turn** — bypassing both the
    /// salve cap and the turn currency §18 makes the only currency inside a fight.
    /// <para>
    /// <c>EncounterManager.Treat</c>'s own comment says it routes through <c>UseItem</c> *"so that 'nothing
    /// to treat' refuses here exactly as it refuses in the satchel"* — the two were deliberately joined in
    /// that direction and **the reverse leak was never closed**. The lesson is the general one about shared
    /// paths: joining two callers so a rule cannot drift says nothing about rules that should apply to only
    /// one of them.
    /// </para>
    /// <para>
    /// Ordered last, after <see cref="NothingToTreat"/>, so a player at full Vitality mid-fight still hears
    /// *"nothing to treat"* — the more useful of two true answers, and the one that does not imply the salve
    /// would have worked.
    /// </para>
    /// </remarks>
    InFight = 5,
}

/// <summary>
/// Why <c>InventoryCraftingManager.Equip</c> would refuse. The fourth member of the inspect/act family
/// (<see cref="CraftBlock"/>, <see cref="UseBlock"/>, <see cref="StashBlock"/>), and it exists for the
/// same reason they do: the button and the act must ask one question.
/// </summary>
public enum EquipBlock
{
    /// <summary>Nothing in the way; equipping it would change what you are wearing.</summary>
    None = 0,

    /// <summary>No such item id. A content error rather than a player state.</summary>
    NoSuchItem = 1,

    /// <summary>Not in the satchel.</summary>
    NotHeld = 2,

    /// <summary>
    /// Held, but it is not <see cref="ItemCategory.Equipment"/> — or it is and its
    /// <see cref="EquipSlot"/> was left at <c>None</c>, which is an authoring error the roster gates
    /// already catch. One refusal covers both because the player-facing answer is identical: this is
    /// not a thing you wear.
    /// </summary>
    NotEquipment = 3,

    /// <summary>
    /// Already worn in its own slot. A refusal rather than a no-op, so the panel can grey the button
    /// instead of offering an action that does nothing.
    /// </summary>
    AlreadyWorn = 4,
}

/// <summary>
/// Why <c>EncounterManager.Rig</c> would refuse. Rigging is free and has no timer (<c>combat.md</c>
/// §18), so every member here is a state you can fix by walking somewhere — none of them is a loss.
/// </summary>
public enum RigBlock
{
    /// <summary>Nothing in the way; the trap would go on the site.</summary>
    None = 0,

    /// <summary>No such item id, or no such creature to rig against. A content error.</summary>
    NoSuchItem = 1,

    /// <summary>Not in the satchel.</summary>
    NotHeld = 2,

    /// <summary>Held, but not <see cref="ItemCategory.Trap"/>.</summary>
    NotATrap = 3,

    /// <summary>
    /// Both sites are taken. <c>GameConfig.RigSlotsPerFight</c> is the number, and it is read rather
    /// than duplicated here — the cap is a dial to turn, not a rule to restate.
    /// </summary>
    SlotsFull = 4,

    /// <summary>
    /// That exact trap is already on one of this creature's sites. Traps are permanent and owned rather
    /// than stacked (§19), so a second copy of one is not a thing you can have.
    /// </summary>
    AlreadyRigged = 5,

    /// <summary>
    /// A fight is running, and rigging is a before-the-fight verb.
    ///
    /// <para><b>This replaces a mid-fight rigging path that had already been overruled.</b>
    /// <c>combat.md</c> §18 priced re-rigging inside a fight as an action costing a turn, and
    /// <c>EncounterManager.Rig</c> implemented that. §22's table then <b>struck rigging</b> from what a
    /// fight lets you reach, and the satchel refusing to open mid-fight is what enforces it — so the
    /// implementation was serving a superseded rule and the turn-cost branch was unreachable through the
    /// UI. Jeremy on the deletion, 2026-08-05: *"sounds like a good delete. Maybe we thought about bringing
    /// the item menu up awhile ago but the potion options come up instead which makes more sense."*</para>
    /// </summary>
    /// <remarks>
    /// Ordered <b>first</b> in <c>EncounterManager.InspectRig</c>, unlike <see cref="UseBlock.InFight"/>,
    /// which is ordered last. The difference is which of two true answers is worth hearing. A salve mid-fight
    /// is a thing you can nearly do, so the useful answer is about the salve; rigging mid-fight is not on the
    /// menu at all, so the rule is the answer and whether you happen to hold the trap is beside the point.
    /// </remarks>
    InFight = 6,
}

/// <summary>
/// Why <c>EncounterManager.Engage</c> would refuse. Fights are opt-in (<c>combat.md</c> §18), so this
/// is what the confirm step consults before it offers the choice.
/// </summary>
public enum EngageBlock
{
    /// <summary>Nothing in the way; clicking through would start the fight.</summary>
    None = 0,

    /// <summary>No placement by that instance id in the current room. A content error.</summary>
    NoSuchCreature = 1,

    /// <summary>A fight is already running. There is exactly one at a time — battles are 1v1.</summary>
    AlreadyFighting = 2,

    /// <summary>
    /// Already <see cref="EntityState.Purified"/> — the body is finished and there is nothing left to
    /// fight. Distinct from <see cref="Spent"/>: this one is about the *state*, which persists, and it
    /// is the cell where a freed person stands as an NPC.
    /// </summary>
    NotHostile = 3,

    /// <summary>
    /// Hostile, but its durability is already gone — a husk left standing by a fight you fled. Its own
    /// refusal rather than <see cref="NotHostile"/>'s, because the answer is *hit it once more*, not
    /// *leave it alone*.
    /// </summary>
    Spent = 4,
}

// `ZonePhase` — Corrupted / ActiveFront / Purified, the progress of a whole region through the
// cleansing arc. Deleted 2026-08-03 (unit 2), and it is the last of the superseded zone design to go:
// `RoomData.ZoneId` became `ChapterId` in unit 1, and this is the machinery that rename was making
// room for.
//
// It described a shape `combat.md` §22 replaced. A zone was cleansed by killing one lieutenant, which
// is a boss-per-region structure the game does not have; the arc's middle value existed to suppress
// respawns, and there are no respawns; and `ZoneCorruptionScalar` fed an ambient Resolve drain that
// became boss-only aura. **A chapter is no longer a thing that can be cleansed. 59 screens are** —
// see `Flags.Cleansed` and `MendManager.Reevaluate`, where the state now lives as one flag per screen
// and therefore round-trips through the save with no field of its own.
