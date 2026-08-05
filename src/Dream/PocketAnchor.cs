using Godot;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// The thing you came into a pocket for. Reaching it calls <see cref="PocketManager.Complete"/> — the grant
/// lands, the flag is written, the entrance in the Waking world breaks, and you are put back on the screen
/// you left.
///
/// <para><b>Drop this on an <c>Area3D</c> and the pocket is finished.</b> That is the whole point of it
/// existing: Jeremy's plan is to build one pocket and clone it fourteen times, and a scene that carried its
/// own copy of this call would carry fourteen copies that can drift. Cloning is exactly how they would drift
/// — silently, and identically, which is the lesson the 59 generated rooms already paid for. So the seam
/// logic lives here once and a clone is pure geometry.</para>
/// </summary>
/// <remarks>
/// <b>It fires once and cannot fire twice</b>, which matters because a body can re-enter an
/// <c>Area3D</c> in the same frame it left. <see cref="PocketManager.Complete"/> already refuses when no
/// pocket is live, so this is belt-and-braces rather than the enforcement — but the second call would emit a
/// second line to the player, and a doubled *"something of his own comes back to him"* reads as a bug even
/// when nothing is wrong.
/// <para>
/// <b>Nothing here knows which pocket it is in</b>, and it must not: the manager owns that. A scene that
/// named its own pocket id would be the second copy of a fact — the <c>IsSanctuary</c> mistake, in a place
/// where cloning would replicate it fourteen times over.
/// </para>
/// </remarks>
[GlobalClass]
public partial class PocketAnchor : Area3D
{
    /// <summary>
    /// Whether it has already been reached this visit. Reset by leaving, because the node is freed with the
    /// scene — there is no state to carry between visits.
    /// </summary>
    private bool _pulled;

    /// <summary>For the gates and for a scene that would rather drive this from its own script.</summary>
    public bool Pulled => _pulled;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        // A placeholder anchor with no shape is a pocket that cannot be finished, and the symptom is a player
        // walking through the middle of it forever. Loud, because it is an authoring mistake and the alternative
        // is a bug report that reads "the pocket is broken".
        if (GetChildCount() == 0)
            GD.PushWarning($"[PocketAnchor] '{Name}' has no collision shape; nothing can reach it.");
    }

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node3D body) => Pull();

    /// <summary>
    /// Finish the pocket. Public so a scene can trigger it from a button, a cutscene or a puzzle rather than
    /// a collision — the anchor is *what* ends a pocket, not *how* you touch it.
    /// </summary>
    public bool Pull()
    {
        if (_pulled) return false;

        var pockets = PocketManager.Instance;
        if (pockets == null || !pockets.InPocket) return false;

        _pulled = true;
        return pockets.Complete();
    }
}
