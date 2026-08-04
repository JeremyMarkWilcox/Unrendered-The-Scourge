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
    /// <summary>Unique across the whole game, e.g. "ch1.hunter_01".</summary>
    [Export] public string InstanceId { get; set; } = "";

    [Export] public EntityData Entity { get; set; }

    /// <summary>Anchor point in 1920x1080 Waking space.</summary>
    [Export] public Vector2 WakingPosition { get; set; } = new(960, 720);

    [Export] public float WakingScale { get; set; } = 1.0f;

    // `DreamPosition` / `DreamScale` — where the spirit stood in 480x270 Dream space, which need not
    // be where the host stood. Deleted 2026-08-02 with spectral vision: a placement is a point on one
    // painted screen again. The Dream's own creatures get placed in their 3D pocket, by that pocket,
    // and a Waking body no longer has a second silhouette to keep in sync.

    // `IsZoneLieutenant` — purifying this one broke the whole zone's distortion field, dropping
    // everything else alive in the room to Form 3. Deleted 2026-08-03 (unit 2), and it is the field the
    // build plan flagged as "build it a registry or delete it": nothing enforced that a zone had exactly
    // one lieutenant, so two of them, or none, were both silently authorable.
    //
    // Delete won, and pass 2 is why the window was open: none of the 30 fight placements authored on
    // 2026-08-02 set it, so exactly one .tres in the game ever did. What replaces the mechanism is not
    // a registry — it is the removal of the idea that one death settles a region. Every creature is a
    // person with their own tether (`tethered-dies-untethered-survives`), so nobody is freed as a side
    // effect of somebody else's fight, and a screen is finished when the things *on it* are dealt with.
    // See `MendManager.IsCleansed`.
}
