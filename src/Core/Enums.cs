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
    /// You know how and you have the materials, but there is no bench here. Crafting happens at
    /// camps and nowhere else (`combat.md` §17) — that is what makes "what do I carry from camp to
    /// the site" a decision instead of a formality.
    ///
    /// Ordered after <see cref="Unknown"/> on purpose: an unlearned recipe must not admit it exists
    /// by complaining about the venue.
    /// </summary>
    NotAtCamp = 4,
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
    /// Held, but not a thing you click. Reagents, lore and catalysts all land here — a catalyst is
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

/// <summary>Progress of a region through the cleansing arc.</summary>
public enum ZonePhase
{
    /// <summary>Distortion field intact. Full corruption, full Resolve drain.</summary>
    Corrupted = 0,

    /// <summary>Player is clearing it. Zero respawns while the front is active.</summary>
    ActiveFront = 1,

    /// <summary>Lieutenant slain. Weather clears, drain stops, Form 3 creatures are farmable.</summary>
    Purified = 2,
}
