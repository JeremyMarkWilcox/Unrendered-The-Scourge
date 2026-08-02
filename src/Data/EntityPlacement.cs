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

    [Export] public float WakingScale { get; set; } = 1.0f;

    // `DreamPosition` / `DreamScale` — where the spirit stood in 480x270 Dream space, which need not
    // be where the host stood. Deleted 2026-08-02 with spectral vision: a placement is a point on one
    // painted screen again. The Dream's own creatures get placed in their 3D pocket, by that pocket,
    // and a Waking body no longer has a second silhouette to keep in sync.

    /// <summary>
    /// Purifying this one breaks the whole zone's distortion field: weather clears, drain
    /// stops, everything left alive drops to Form 3.
    /// </summary>
    [Export] public bool IsZoneLieutenant { get; set; }
}
