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

/// <summary>Which realm(s) a hotspot or overlay is perceivable in.</summary>
public enum RealmVisibility
{
    Both = 0,
    WakingOnly = 1,
    DreamOnly = 2,
}

/// <summary>Which procedural scene a room draws while it has no painted background.</summary>
public enum BackdropStyle
{
    /// <summary>Treeline, mist, a track receding into it.</summary>
    Wilderness = 0,

    /// <summary>Walls, a doorway, a floor. Ruins and interiors.</summary>
    Interior = 1,

    /// <summary>Pillars and a void sun. Used for every room's Dream layer.</summary>
    Dream = 2,
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
