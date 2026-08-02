using Godot;

namespace Unrendered.Data;

/// <summary>
/// One screen. A room is pure data — RoomView renders any of these without knowing which.
/// Adding a location to the game is a new .tres file, not a new scene and not new code.
/// </summary>
[GlobalClass]
public partial class RoomData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    /// <summary>
    /// Which chapter this screen belongs to — <c>ch1</c>, <c>ch2</c>, <c>ch3</c>. A bare string on
    /// purpose: there is no chapter resource, because nothing yet needs one (see
    /// <see cref="Core.GameConfig"/>). This is what the material refill groups by.
    /// </summary>
    [Export] public string ChapterId { get; set; } = "";

    [ExportGroup("Backgrounds")]
    [Export] public Texture2D WakingBackground { get; set; }

    /// <summary>Shown instead once the zone is purified — weather cleared, colour returned.</summary>
    [Export] public Texture2D WakingBackgroundPurified { get; set; }

    // `DreamBackground` — a second 480x270 painting per screen, shown through the spectral-vision
    // overlay. Deleted 2026-08-02 with the great swap. It was the most expensive field in the game:
    // every authored screen implied a second authored screen. A room is one picture again.

    /// <summary>Resolve drain scalar for this screen, before zone modifiers. 0..1.</summary>
    [ExportGroup("Atmosphere")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float BaseCorruption { get; set; } = 0.3f;

    // `IsSanctuary` used to sit here, unused, reserved for camps. Deleted 2026-07-30 rather than
    // renamed to IsCamp: a room is a camp precisely when it contains a
    // Core.HotspotKind.Workbench hotspot, so a flag beside that would be the same fact stored
    // twice and able to disagree with itself. Ask CampManager.IsCamp. Its doc comment was also
    // stale — it promised Resolve restoration "instead of draining", and the ambient drain it
    // referred to no longer exists.

    /// <summary>Which procedural scene to draw while <see cref="WakingBackground"/> is unset.</summary>
    [Export] public Core.BackdropStyle PlaceholderStyle { get; set; } = Core.BackdropStyle.Wilderness;

    [ExportGroup("Contents")]
    [Export] public Godot.Collections.Array<HotspotData> Hotspots { get; set; } = new();
    [Export] public Godot.Collections.Array<EntityPlacement> Entities { get; set; } = new();

    /// <summary>
    /// Optional scene of hand-placed <c>Hotspot</c> nodes. When set it replaces the data-driven
    /// list above — the escape hatch for drawing tight polygons over real painted art.
    /// </summary>
    [ExportGroup("Advanced")]
    [Export] public PackedScene HotspotSceneOverride { get; set; }
}
