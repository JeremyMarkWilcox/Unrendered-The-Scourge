using System.Collections.Generic;

namespace Unrendered.Save;

/// <summary>
/// The whole game, on disk. Deliberately just dictionaries of primitives — the world is
/// defined by flags, entity states and stacks, so there is nothing else to persist.
/// </summary>
public sealed class SaveGame
{
    public int Version { get; set; } = 1;

    public string CurrentRoomId { get; set; } = "";
    public int CurrentRealm { get; set; }
    public float Resolve { get; set; } = 100f;

    /// <summary>
    /// Absent from pre-Vitality saves, which deserialise to this default — a full bar, which is
    /// the right answer for a save taken before the body could be wounded at all. No version
    /// bump needed for that reason.
    /// </summary>
    public float Vitality { get; set; } = 100f;

    /// <summary>
    /// The camp you would wake at. Absent from pre-camp saves, which deserialise to blank — and
    /// blank is the correct answer for a save taken before camps existed, since the player had never
    /// reached one. No version bump, for the same reason <see cref="Vitality"/> needed none.
    /// </summary>
    public string ReturnCampRoomId { get; set; } = "";

    public Dictionary<string, bool> Flags { get; set; } = new();
    public Dictionary<string, int> ZonePhases { get; set; } = new();
    public Dictionary<string, int> EntityStates { get; set; } = new();
    public Dictionary<string, int> Inventory { get; set; } = new();

    /// <summary>
    /// The shared cache. One store for the whole game, not one per camp — see
    /// <c>InventoryCraftingManager.Stash</c> for why.
    /// </summary>
    public Dictionary<string, int> Stash { get; set; } = new();

    /// <summary>
    /// Blueprints learned. Knowledge rather than an item, so it is stored apart from the satchel —
    /// spending the page you read does not unlearn the recipe.
    /// </summary>
    public List<string> DiscoveredBlueprints { get; set; } = new();

    /// <summary>
    /// Damage standing against severed bodies, per entity instance id. This is the whole of what
    /// docs/combat.md §13 asked the save format to carry: anchored wounds close when the engagement
    /// ends, so nothing about them ever reaches disk, and only the severed half needs storing.
    ///
    /// Absent from pre-combat saves, which deserialise to empty — the right answer for a world where
    /// nothing had been fought yet. No version bump, for the same reason <see cref="Vitality"/> and
    /// <see cref="ReturnCampRoomId"/> needed none.
    /// </summary>
    public Dictionary<string, int> Wounds { get; set; } = new();
}
