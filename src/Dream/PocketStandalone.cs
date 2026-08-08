using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// Makes <b>F6</b> on a pocket scene a real visit. Put one anywhere in the scene and running it directly
/// behaves like crossing in: the verbs work, creatures cost, the exit fires.
///
/// <para><b>Because F6 is how a level actually gets built.</b> Everything interesting in a pocket asks
/// <c>PocketManager.InsideTheDream</c> first — <c>DreamVerbs.BeginHold</c>, <c>DreamCreature.Touch</c>,
/// <c>PocketExit</c> — and running a scene on its own never sets it, so every one of them refuses and the
/// pocket reads as broken. The refusals are all correct; what was missing was anything to tell the manager
/// that a pocket is on screen. <c>DreamVerbHold</c> already carries a warning about this exact twenty minutes
/// of confusion, which is the sign it was worth removing rather than documenting again.</para>
/// </summary>
/// <remarks>
/// <b>It does nothing at all in the real game</b>, because by the time <c>PocketHost</c> has instantiated the
/// scene you are already inside — so the first thing it asks is whether someone got here first, and the F5
/// path answers yes. One node, both workflows, no switch to remember.
///
/// <para><b>Which pocket it enters is derived, not authored.</b> It matches this scene's own
/// <c>SceneFilePath</c> against every <c>PocketData.ScenePath</c>, so the fourteenth pocket needs no more
/// setup than the first and a copied scene cannot come with the wrong id baked into it. If nothing names the
/// scene it says so — which is itself worth hearing, because a pocket scene no <c>PocketData</c> points at is
/// unreachable in the built game.</para>
///
/// <para><b>The one thing it cannot be is automatic.</b> <c>PocketHost</c> attaches the verb poller and the
/// author light so no scene has to carry them, and that is the better pattern — but the host is precisely
/// what does not exist under F6, so this has to be in the scene. Forgetting it costs the F6 convenience and
/// nothing else, which is the safe direction for a thing that cannot be wired centrally.</para>
///
/// <para><b>Debug scaffolding.</b> It enters a pocket and tops up a meter; it never completes anything, so it
/// cannot write <c>pocket.&lt;id&gt;.cleared</c> and cannot break an entrance — the same line
/// <c>F10</c> is held to.</para>
/// </remarks>
[GlobalClass]
public partial class PocketStandalone : Node
{
    /// <summary>
    /// Also hand over the verbs a boss pocket would have taught, so Float and Flare can be tried here.
    /// <c>F11</c> in the running game, without the running game.
    /// </summary>
    [Export] public bool GrantVerbs { get; set; } = true;

    /// <summary>Fill the bar on entry, so a session does not start part-drained from the last one.</summary>
    [Export] public bool FillResolve { get; set; } = true;

    /// <summary>Whether it decided this was a standalone run and entered. For the gates and the log.</summary>
    public bool Engaged { get; private set; }

    public override void _Ready() => CallDeferred(MethodName.Engage);

    /// <summary>
    /// Enter the pocket this scene belongs to, if nothing has already put us in one.
    /// </summary>
    /// <remarks>
    /// Deferred by a frame rather than run in <c>_Ready</c>, and public so the gates can call it directly. The
    /// frame is for the rest of the scene: nothing else is reliably built yet in <c>_Ready</c>.
    /// <para>
    /// <b>This remark used to claim the delay avoided a race with <c>PocketGate</c>, and that was exactly
    /// wrong.</b> Deferring does not avoid the race — it settles it against every gate, deterministically, on
    /// every run. Each gate read <c>Holds</c> a frame before the grant, hid its subtree, and had nothing to
    /// make it look again; the sandbox's Float-gated ledge was reported as simply *invisible*. **A guaranteed
    /// loss is not the fix for an unpredictable one.** The grant now re-reads the gates itself, which is the
    /// only ordering that does not depend on when anything runs.
    /// </para>
    /// </remarks>
    public void Engage()
    {
        // **A detached node has no run to help, and this is not hypothetical.** `_Ready` queues this a frame
        // ahead; a fixture that instantiates a pocket scene, drives it and frees it inside that same frame
        // leaves the deferred call to land on a node that is no longer in a tree — which then entered a pocket
        // nobody was in, from geometry that no longer exists, and asked a freed tree for its root. The gate
        // suite printed exactly that: one `ERROR: Parameter "data.tree" is null` after the last check, with a
        // cheerful *"running as a pocket"* under it.
        if (!IsInsideTree()) return;

        var pockets = PocketManager.Instance;
        if (pockets == null) return;

        // The F5 path: `PocketHost` put us inside before it built this scene. Nothing to do, and saying so
        // would be noise on every real crossing.
        if (PocketManager.InsideTheDream) return;

        var scene = OwningScenePath();
        if (string.IsNullOrEmpty(scene))
        {
            GD.PushWarning("[standalone] Cannot tell which scene this is, so no pocket was entered.");
            return;
        }

        var owner = pockets.Pockets.Values.FirstOrDefault(p => p.ScenePath == scene);
        if (owner == null)
        {
            // Worth hearing even though it only bites under F6: a pocket scene that no `PocketData` names is
            // one the built game has no way to reach.
            GD.PushWarning($"[standalone] No PocketData names '{scene}', so there is no pocket to enter. "
                + "Point one at it, or this scene is unreachable in the game.");
            return;
        }

        if (!pockets.Enter(owner.Id))
        {
            GD.PushWarning($"[standalone] '{owner.Id}' refused entry: {pockets.InspectEnter(owner.Id)}.");
            return;
        }

        Engaged = true;

        if (FillResolve && ResolveManager.Instance is { } resolve) resolve.SetTo(resolve.Max);

        // `Main.LearnGrantableDreamVerbs` on F11, written out rather than called: that method is private to
        // a node that does not exist under F6, and the whole point here is not needing it.
        if (GrantVerbs && WorldStateManager.Instance is { } world)
        {
            foreach (DreamVerb verb in System.Enum.GetValues<DreamVerb>())
            {
                if (verb == DreamVerb.None || PocketManager.Holds(verb)) continue;

                world.SetFlag(Flags.VerbLearned(verb), true);
            }
        }

        AttachVerbInput();

        // **Granting a verb after the gates have read is what made a Float-only ledge invisible**, and the
        // remark on this method had the reasoning backwards: deferring does not avoid the race, it decides it
        // against every gate. `PocketGate._Ready` has already run by now, on flags this method was about to
        // change, and nothing re-reads on its own. So re-read them here, after the grant.
        var reopened = PocketGate.ReapplyAll(GetTree()?.Root);

        GD.Print($"[standalone] Running '{owner.Id}' as a pocket. Verbs, creatures and the exit are live "
            + $"({reopened} gate(s) re-read after the grant). "
            + "This does nothing when you cross in from the game.");
    }

    /// <summary>Whether it built a verb poller, because nothing else was going to. For the gates.</summary>
    public bool VerbInputLive => GetNodeOrNull<DreamVerbHold>("VerbInput") != null;

    /// <summary>
    /// Give the standalone run a <see cref="DreamVerbHold"/>, since <c>PocketHost</c> is not here to.
    /// </summary>
    /// <remarks>
    /// <b>This was missed on the first pass and the symptom was the exact one the node exists to remove.</b>
    /// Entering the pocket makes <c>DreamVerbs.BeginHold</c> stop refusing — and no key reaches it, because
    /// the poller is attached by the host and under F6 there is no host. So the verbs went from *refused for
    /// a reason in the log* to *silently doing nothing*, which is strictly worse.
    /// <para>
    /// Only on the standalone path, so there is never a second poller beside the host's. Two would in fact be
    /// harmless — <c>DreamVerbs</c> holds a set and both would ask it the same question — but "harmless
    /// duplicate" is a thing someone has to work out from scratch every time they see it.
    /// </para>
    /// </remarks>
    private void AttachVerbInput()
    {
        if (VerbInputLive) return;

        AddChild(new DreamVerbHold { Name = "VerbInput" });
    }

    /// <summary>
    /// The path of the scene this node was saved into.
    /// </summary>
    /// <remarks>
    /// <c>Owner</c> first, because that is the instantiated scene's root and is set for every node saved in
    /// it. The walk up is for the case where this node was added in code and has no owner — cheap, and it
    /// keeps the class usable from a gate that builds a fixture by hand.
    /// </remarks>
    private string OwningScenePath()
    {
        if (!string.IsNullOrEmpty(Owner?.SceneFilePath)) return Owner.SceneFilePath;
        if (!string.IsNullOrEmpty(SceneFilePath)) return SceneFilePath;

        var parent = GetParent();
        while (parent != null)
        {
            if (!string.IsNullOrEmpty(parent.SceneFilePath)) return parent.SceneFilePath;
            parent = parent.GetParent();
        }

        return "";
    }
}
