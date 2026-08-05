using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// Geometry that is only there once you hold a Dream verb — a ledge that needs <see cref="DreamVerb.Float"/>,
/// a wall that only <see cref="DreamVerb.Flare"/> shows you. Put this on a node and its whole subtree obeys.
///
/// <para><b>The 2D side's <c>HotspotData.RequiredFlag</c> in three dimensions</b>, and deliberately the same
/// shape: one field, one flag, and the thing is either there or it is not. Everything about which verb gates
/// which piece of level is Jeremy's judgement; this is only the switch.</para>
/// </summary>
/// <remarks>
/// <b>It reads <see cref="PocketManager.Holds"/> rather than the flag directly</b>, which is the whole reason
/// it is worth having as a node instead of two lines in each scene. Fourteen cloned pockets asking the same
/// question must get the same answer, and <c>Holds</c> is where the one answer lives — including the part a
/// scene would get wrong, that <see cref="DreamVerb.Suppress"/> is held from the first frame and carries no
/// flag at all.
/// <para>
/// <b>Re-evaluated on entering the tree rather than every frame.</b> A verb is granted by pulling an anchor,
/// which ends the pocket — so nothing can be learned *while* a pocket is open, and a <c>_Process</c> here
/// would poll for a change that cannot happen. Pockets are instantiated on entry and freed on exit
/// (<c>PocketHost</c>), so entering the tree is exactly the moment the answer can differ from last time.
/// </para>
/// </remarks>
[GlobalClass]
public partial class PocketGate : Node
{
    /// <summary>
    /// The verb this gate wants. <see cref="DreamVerb.None"/> means always open, which is the harmless
    /// default — a gate someone forgot to set lets the player through rather than walling off a pocket they
    /// cannot finish. Same *fail toward the visible mistake* direction as
    /// <c>HotspotData.RefillsAtMilestone</c>.
    /// </summary>
    [Export] public DreamVerb Requires { get; set; } = DreamVerb.None;

    /// <summary>
    /// Invert it: present only *until* the verb is held. For the rare piece that Flare is meant to burn away
    /// — the mirror of <c>HotspotData.RequiredFlagValue</c>, and it exists so the 3D side does not have to
    /// invent a second node for the opposite case.
    /// </summary>
    [Export] public bool Inverted { get; set; }

    /// <summary>Whether the gate is currently letting its subtree exist. For the gates.</summary>
    public bool IsOpen { get; private set; } = true;

    public override void _Ready() => Apply();

    /// <summary>
    /// Show or hide the parent's subtree, and stop it colliding when hidden.
    /// </summary>
    /// <remarks>
    /// <b>Visibility alone is not enough and that is the whole trap here.</b> A hidden <c>MeshInstance3D</c>
    /// still has a <c>CollisionShape3D</c> beside it, so a Float-only ledge you cannot see is a ledge you can
    /// still stand on — the player walks on air and the gate reads as broken. Same shape as the eighth
    /// blind-spot class: a half-applied arrangement that looks applied.
    /// </remarks>
    public void Apply()
    {
        var held = PocketManager.Holds(Requires);
        IsOpen = Inverted ? !held : held;

        // The parent rather than this node, so the script can hang off a bare `Node` child and the thing being
        // gated stays whatever Jeremy built — a mesh, a body, a whole room's worth of props.
        var target = GetParent();
        if (target == null) return;

        if (target is Node3D spatial) spatial.Visible = IsOpen;

        SetCollision(target, IsOpen);
    }

    /// <summary>Turn collision on or off through a subtree, since hiding a mesh does not move its shape.</summary>
    private static void SetCollision(Node node, bool on)
    {
        if (node is CollisionObject3D body)
        {
            // Layer 0 is "collides with nothing" — cheaper and less surprising than disabling the node, which
            // would also stop its children processing.
            body.ProcessMode = on ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            body.SetDeferred(CollisionObject3D.PropertyName.CollisionLayer, on ? 1 : 0);
            body.SetDeferred(CollisionObject3D.PropertyName.CollisionMask, on ? 1 : 0);
        }

        foreach (var child in node.GetChildren()) SetCollision(child, on);
    }
}
