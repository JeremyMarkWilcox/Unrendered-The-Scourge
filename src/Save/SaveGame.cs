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
    public float Sanity { get; set; } = 100f;

    public Dictionary<string, bool> Flags { get; set; } = new();
    public Dictionary<string, int> ZonePhases { get; set; } = new();
    public Dictionary<string, int> EntityStates { get; set; } = new();
    public Dictionary<string, int> Inventory { get; set; } = new();
}
