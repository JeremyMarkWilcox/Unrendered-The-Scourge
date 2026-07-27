using Godot;

namespace Unrendered.Data;

/// <summary>
/// A region of the map that cleanses as a unit. Killing the lieutenant breaks the
/// distortion field: weather clears, drain stops, survivors become Form 3 and farmable.
/// </summary>
[GlobalClass]
public partial class ZoneData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    /// <summary>Instance id of the entity whose death purifies the whole zone.</summary>
    [Export] public string LieutenantEntityId { get; set; } = "";

    /// <summary>Multiplies every room's own corruption while the field stands. 0..1.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float CorruptionScalar { get; set; } = 1.0f;

    /// <summary>Line shown when the field breaks.</summary>
    [Export(PropertyHint.MultilineText)] public string PurificationText { get; set; } = "";
}
