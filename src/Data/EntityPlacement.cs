using Godot;

namespace Unrendered.Data;

/// <summary>
/// An instance of an <see cref="EntityData"/> standing in a specific room. Entities are
/// unique and persistent — there is no pooling and no respawn, which is the mechanical
/// half of the zero-respawn rule. The instance id is what state is keyed by.
/// </summary>
[GlobalClass]
public partial class EntityPlacement : Resource
{
    /// <summary>Unique across the whole game, e.g. "waystation.rat_01".</summary>
    [Export] public string InstanceId { get; set; } = "";

    [Export] public EntityData Entity { get; set; }

    /// <summary>Anchor point in 1920x1080 Waking space.</summary>
    [Export] public Vector2 WakingPosition { get; set; } = new(960, 720);

    /// <summary>Anchor point in 480x270 Dream space. The spirit need not stand where the host does.</summary>
    [Export] public Vector2 DreamPosition { get; set; } = new(240, 180);

    [Export] public float WakingScale { get; set; } = 1.0f;
    [Export] public float DreamScale { get; set; } = 1.0f;

    /// <summary>
    /// Purifying this one breaks the whole zone's distortion field: weather clears, drain
    /// stops, everything left alive drops to Form 3.
    /// </summary>
    [Export] public bool IsZoneLieutenant { get; set; }
}
