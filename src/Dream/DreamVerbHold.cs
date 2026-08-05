using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// Turns the three Dream verb keys into <see cref="DreamVerbs"/> holds. Drop one anywhere in a pocket scene
/// and Suppress, Float and Flare work; there is nothing to wire.
///
/// <para><b>This is the act, and <see cref="DreamVerbs"/> is the cost.</b> That manager's doc is explicit that
/// it holds the price and not the button, and leaves *"whatever the Dream layer binds"* undefined. This is the
/// smallest honest answer: press → <see cref="DreamVerbs.BeginHold"/>, release → <see cref="DreamVerbs.EndHold"/>,
/// and no opinion about movement, light or camera, all of which belong to the 3D pass.</para>
/// </summary>
/// <remarks>
/// <b>The fourth node in the clone-and-go family</b>, beside <see cref="PocketAnchor"/>, <see cref="PocketExit"/>
/// and <see cref="PocketGate"/>, and for the same reason: fourteen pocket scenes each holding their own copy of
/// this loop would be fourteen copies that drift, and cloning is exactly how they would drift — silently and
/// identically, which the 59 generated rooms already paid for once.
/// <para>
/// <b>It polls rather than handling input events, and that is not a style choice.</b>
/// <c>DreamProbeWalker</c> reads WASD with <see cref="Input.IsPhysicalKeyPressed"/> and says why: whether
/// <c>_UnhandledInput</c> reaches a node inside a <c>SubViewport</c> depends on the container's mouse and input
/// forwarding, and a pocket is always inside one. Polling is unconditional. It reads *actions* rather than
/// physical keys so the bindings are yours to change in Project Settings without touching code — a missing
/// action is skipped rather than spamming the log, and the totality gate is what makes that safe.
/// </para>
/// <para>
/// <b><see cref="DreamVerbs"/> is the only record of what is held.</b> This node keeps no set of its own and
/// asks <see cref="DreamVerbs.IsHolding"/> every frame instead, so there is no second copy to disagree with the
/// first — the mistake <c>RoomData.IsSanctuary</c> was deleted for. It matters here specifically because
/// <c>PocketManager.Return</c> calls <see cref="DreamVerbs.ReleaseAll"/> on the way out: a private set would
/// still believe it was holding Suppress, and the key would be dead until the player let go of a button they
/// were already holding.
/// </para>
/// </remarks>
[GlobalClass]
public partial class DreamVerbHold : Node
{
    /// <summary>
    /// The action behind each verb. <b>Public because a gate asserts it is total</b> — add a fourth
    /// <see cref="DreamVerb"/> and forget its action and the suite fails, which is the one shape of guard that
    /// reliably goes red on its own (the <c>HotspotKind</c> palette gate, again).
    /// </summary>
    public static readonly IReadOnlyDictionary<DreamVerb, string> Actions = new Dictionary<DreamVerb, string>
    {
        [DreamVerb.Suppress] = "dream_suppress",
        [DreamVerb.Float] = "dream_float",
        [DreamVerb.Flare] = "dream_flare",
    };

    private bool _warned;

    public override void _Process(double delta) => Poll();

    /// <summary>
    /// Read the three actions and open or close the matching holds. One frame's worth.
    /// </summary>
    /// <remarks>
    /// <b>Public so the gates can drive it without a frame.</b> The verifier is fully synchronous and
    /// <c>_Process</c> never ticks meaningfully in a headless run, so a check that pressed an action and waited
    /// would measure nothing — the same seam <c>DebugRepaintNow</c> exists for.
    /// <para>
    /// A refused <see cref="DreamVerbs.BeginHold"/> is retried on the next frame rather than latched, because
    /// the two reasons it refuses expire differently: an unlearned verb stays unlearned for hours, but an empty
    /// bar refills the moment you are carried to a camp, and a latch would leave the key dead afterwards.
    /// </para>
    /// </remarks>
    public void Poll()
    {
        foreach (var pair in Actions)
        {
            if (!InputMap.HasAction(pair.Value)) continue;

            var down = Input.IsActionPressed(pair.Value);
            var holding = DreamVerbs.IsHolding(pair.Key);

            if (down && !holding)
            {
                if (!DreamVerbs.BeginHold(pair.Key)) WarnIfNowhere();
            }
            else if (!down && holding)
            {
                DreamVerbs.EndHold(pair.Key);
            }
        }
    }

    /// <summary>Release anything still held if this node goes away before the pocket does.</summary>
    public override void _ExitTree()
    {
        foreach (var verb in Actions.Keys) DreamVerbs.EndHold(verb);
    }

    /// <summary>
    /// Say so, once, when a verb is pressed while nothing is inside a pocket.
    /// </summary>
    /// <remarks>
    /// <b>For a specific twenty minutes of confusion.</b> Playing a pocket scene on its own — the obvious way to
    /// iterate on 3D — does not put <c>PocketManager</c> inside a pocket, so every verb refuses and the feature
    /// reads as broken rather than as untested. Once rather than per-frame, and only for this cause: an
    /// unlearned Float refusing is <i>correct</i>, and warning about it would train the warning to be ignored.
    /// </remarks>
    private void WarnIfNowhere()
    {
        if (_warned || PocketManager.InsideTheDream) return;

        _warned = true;
        GD.PushWarning(
            "[DreamVerbHold] A Dream verb was pressed while not inside a pocket, so it does nothing. "
            + "Running a pocket scene on its own does not put you inside one — launch the game and cross in.");
    }
}
