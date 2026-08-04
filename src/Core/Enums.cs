namespace Unrendered.Core;

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

    /// <summary>The anchor itself, at the far end of the maze. Taking it ends the heist.</summary>
    TakeAnchor = 6,

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

/// <summary>How a Dream heist finished. Every ending is an ejection; only the price differs.</summary>
public enum HeistOutcome
{
    /// <summary>The anchor was taken. The target is Severed.</summary>
    Success = 0,

    /// <summary>It reached you. Thrown out, and it cost.</summary>
    Captured = 1,

    /// <summary>Resolve ran out. The Dream spat you back on its own terms.</summary>
    Exhausted = 2,
}

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
    /// *which kind of no* it is, and "you have it" is not a shortage. Ordered last: a state you reach by
    /// succeeding cannot be the reason a recipe is refused before you have learned it.
    /// </para>
    /// </remarks>
    AlreadyMade = 5,
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
