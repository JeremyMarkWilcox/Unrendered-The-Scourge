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
    /// The Resolve <b>ceiling</b>, which rises once per Dream knowledge pocket — eight in the game. Added by
    /// unit 6, the first unit in which it could move at all.
    /// </summary>
    /// <remarks>
    /// Absent from pre-unit-6 saves, which deserialise to this default — correct for a world where the
    /// ceiling was a <c>const</c> and no pocket existed to raise it. No version bump, for the same reason
    /// <see cref="Vitality"/> and <see cref="ReturnCampRoomId"/> needed none.
    /// <para>
    /// <b>There is deliberately no Vitality twin.</b> Max Vitality never rises
    /// (<c>spirit-light-and-resolve</c>): the body is a fixed thing you spend and mend, and only the spirit
    /// grows. A field here would be the cheapest possible way to break that.
    /// </para>
    /// <para>
    /// <b>A pocket in progress is deliberately not saved</b>, on the same ruling that kept a live heist out
    /// of this format: a save taken inside one restores you to the Waking room named above, entrance intact
    /// and nothing granted. It keeps 3D state out of the save file entirely, and there is no way to bank
    /// half a pocket.
    /// </para>
    /// </remarks>
    public float ResolveMax { get; set; } = 100f;

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

    // `ZonePhases` stood here until 2026-08-03 (unit 2), and this is the one field the format has ever
    // *lost*. Cleansing is a per-screen world flag now (`Flags.Cleansed`), so it rides along in `Flags`
    // above with no field of its own.
    //
    // No version bump, and the reason is the mirror image of the three defaults below: an old save
    // carrying a `ZonePhases` object simply has a member System.Text.Json does not recognise, and
    // ignoring an unknown property is the deserialiser's default.
    //
    // It loads *correctly* rather than merely without crashing, and not by luck: an old save has the
    // rat's state in `EntityStates` but no `screen.*.cleansed` flag, and `NavigationManager.GoToRoom`
    // re-derives that flag on arrival from what is actually standing on the screen. So the yard comes
    // back cleansed if the rat was dealt with and uncleansed if it was not, which is what the deleted
    // field was carrying anyway. The verdict was always derivable; only the cache was ever stored.

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

    /// <summary>
    /// What is worn, keyed by <b>item id</b> → <c>(int)EquipSlot</c>. Added by unit 3a, the first unit in
    /// which anything is worn at all.
    /// </summary>
    /// <remarks>
    /// Keyed by item rather than by slot — the inverse of how <c>InventoryCraftingManager</c> holds it —
    /// so the save does not depend on an enum member's <i>name</i>, only on the item ids the rest of this
    /// format is already built out of. Deferring a shield slot then adds nothing here.
    /// <para>
    /// Re-validated on the way in rather than trusted (<c>ImportEquipped</c>): a save can name an item
    /// whose <c>Slot</c> has since been re-authored, and the live rules win. Equipping moves nothing, so
    /// this is a second reference to a stack in <see cref="Inventory"/> rather than a second place a
    /// weapon can be — and <c>ImportEquipped</c> must therefore run <b>after</b> <c>ImportStacks</c>.
    /// </para>
    /// <para>
    /// Absent from pre-equipment saves, which deserialise to empty — correct for a world where nothing
    /// could be worn. No version bump, for the same reason <see cref="Vitality"/> needed none.
    /// </para>
    /// </remarks>
    public Dictionary<string, int> Equipped { get; set; } = new();

    /// <summary>
    /// Traps sitting on creatures' rig sites, flattened to <c>"instanceId|itemId"</c> in rig order —
    /// which is the order they fire in.
    /// </summary>
    /// <remarks>
    /// A flat <see cref="List{T}"/> of strings rather than a dictionary-of-lists, on
    /// <see cref="DiscoveredBlueprints"/>'s precedent: order is meaningful here, and a flat list of
    /// strings is the one shape every serialiser in this project already round-trips. The cap is
    /// re-applied on load rather than trusted, for the same reason <see cref="Equipped"/> is
    /// re-validated.
    /// <para>
    /// <b>A live fight is deliberately not here.</b> Loading mid-fight puts you back in the room with
    /// your rig intact and nothing committed, which is the rigging phase §18 says has no timer — so a
    /// reload is a way to reconsider rather than a way to bank half a fight. It also means this format
    /// has no notion of whose turn it was, which is a state worth not storing.
    /// </para>
    /// </remarks>
    public List<string> Rigs { get; set; } = new();
}
