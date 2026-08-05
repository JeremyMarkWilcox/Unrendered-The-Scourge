using Godot;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// The way back out without finishing. Calls <see cref="PocketManager.Leave"/>, which **writes nothing** —
/// no flag, no grant, and the entrance in the Waking world is still there.
///
/// <para><b>That is the softlock rule, and this node is where a player meets it.</b> Nothing in the Dream can
/// kill you (invariant 2), so leaving early is a pocket's only failure — and if it broke the entrance, a bad
/// night would permanently delete a craft family and a point of max Resolve. A dead save reached by playing
/// badly. So the two exits are two nodes rather than one with a flag, because the difference between them is
/// the difference between progress and a ruined run.</para>
/// </summary>
/// <remarks>
/// <b>Every pocket should have one</b>, and it is worth saying why rather than trusting it: a pocket with no
/// exit is only leavable by finishing it, which turns *"I'll come back when I can float"* into *"I am stuck in
/// here"*. The Dream has no death to fall back on. It cannot be gated — whether a scene contains this node is
/// 3D authoring, the same category as `placement.md` rule 7 — so it is written down here and in the pocket
/// checklist instead.
/// </remarks>
[GlobalClass]
public partial class PocketExit : Area3D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        if (GetChildCount() == 0)
            GD.PushWarning($"[PocketExit] '{Name}' has no collision shape; nobody can reach the way out.");
    }

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node3D body) => Leave();

    /// <summary>Walk out, taking nothing. Public for the same reason <c>PocketAnchor.Pull</c> is.</summary>
    public bool Leave() => PocketManager.Instance?.Leave() ?? false;
}
