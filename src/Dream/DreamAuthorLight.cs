using System.Collections.Generic;
using Godot;

namespace Unrendered.Dream;

/// <summary>
/// A debug flood light for authoring a pocket: <b>B</b> lights the whole thing at once and drops the
/// monochrome look, B again puts both back. Nothing about it ships.
///
/// <para><b>It is two changes rather than one, and the second is the one that makes it useful.</b> Raising the
/// ambient alone would light the room and then hand the result to <c>DreamLook</c>, which posterises it into a
/// handful of greys under a clamp — so a flooded pocket comes out as one flat mid-grey and you can see *less*
/// than you could by lamplight. The look material comes off the container at the same time, which is the same
/// view the editor's 3D tab gives you and the reason <c>dream-probe.md</c> says that tab looks "wrong".</para>
///
/// <para><b>For the editor there is nothing to build.</b> The 3D viewport's *Display Unshaded* mode
/// (the <i>Perspective</i> dropdown, top left) draws flat albedo with no lighting at all, per-viewport and
/// non-destructive. Prefer it to raising <c>AmbientLightEnergy</c> by hand in the inspector: that value lives
/// on the <c>Environment</c> embedded in the pocket, so a <c>Ctrl+S</c> while it is up **ships the pocket with
/// the horror drained out of it**, and nothing would ever go red about it.</para>
/// </summary>
/// <remarks>
/// <b>The fifth node in the clone-and-go family, and the second that needs no placing</b> — <c>PocketHost</c>
/// attaches it beside <see cref="DreamVerbHold"/>, so fourteen pocket scenes carry nothing and there is no
/// wiring to forget fourteen times. It is a child of the *host* rather than of the loaded scene for the same
/// reason the verb poller is: the no-geometry path is the state every headless gate run lives in.
/// <para>
/// <b>It polls rather than handling input events</b>, for the reason <see cref="DreamVerbHold"/> gives:
/// whether <c>_UnhandledInput</c> reaches a node inside a <c>SubViewport</c> depends on the container's
/// forwarding, and a pocket is always inside one. Polling is unconditional. What it polls is an
/// <c>InputMap</c> action rather than a raw key — see <see cref="ToggleAction"/>, where that choice was made
/// twice and the second time was the right one.
/// </para>
/// <para>
/// <b>Everything it touches is restored on the way out as well as on the way back off.</b> A flood left on
/// when the pocket is freed would be harmless today — <c>PocketHost</c> frees the geometry and the next
/// crossing loads it fresh — but the state lives on nodes this class did not create, which is the eighth
/// blind-spot class exactly: a half-applied arrangement that looks applied. Restoring in
/// <see cref="_ExitTree"/> makes the claim true by construction rather than by the host's current habits.
/// </para>
/// </remarks>
[GlobalClass]
public partial class DreamAuthorLight : Node
{
    /// <summary>
    /// Ambient energy while flooded. High enough that faces pointing away from every placed light still read,
    /// which is the whole job — a directional light would leave the far side of each pillar black and a big
    /// omni would fall off before the far wall of a pocket this size.
    /// </summary>
    public const float FloodAmbientEnergy = 1.6f;

    /// <summary>
    /// The action behind the toggle — <b>B</b>, for bright, and it is an action rather than a raw key for two
    /// reasons that only showed up under a gate.
    /// </summary>
    /// <remarks>
    /// <b>An action, because a physical key cannot be pressed from code.</b> <c>Input.ParseInputEvent</c> does
    /// not move what <c>Input.IsPhysicalKeyPressed</c> reports, so the raw-key version this shipped as first
    /// had a check that could only ever pass — the key never down, both lights dark, and the ownership guard
    /// underneath it measured by nothing. <c>debug_pocket</c> was already an action for the same reason.
    /// <para>
    /// <b>The key was wrong twice, both times silently, and the second one is the lesson.</b> F9 looked free
    /// and is <c>debug_load</c> — F1–F11 are all bound bar F4. Then <b>L</b> looked free and is
    /// <c>DreamLookTuner</c>'s dist-comp key, which collides **only inside the probe scene** — that is, only
    /// in the one workflow this feature exists to serve, and only by making a tuning key do two things at
    /// once. Neither collision produces an error. **Before binding anything here, grep
    /// <c>src/Dream/</c> for <c>Key.</c> as well as the <c>InputMap</c>**, because the probe reads raw keys
    /// that no action list knows about.
    /// </para>
    /// <para>
    /// Taken, between the tuner, the walker, the three verbs and the Waking panels: A C D E I J K L M N O P Q
    /// S U W, Space and Shift. <b>B</b> is free and stays free of a Waking binding for the same structural
    /// reason it is safe at all — this node only ever exists inside a pocket.
    /// </para>
    /// </remarks>
    public const string ToggleAction = "debug_author_light";

    /// <summary>
    /// The one instance that answers the key. <b>Not a singleton — a claim</b>, and the difference is the
    /// point: a second light is legal, inert and harmless rather than an error.
    /// </summary>
    /// <remarks>
    /// <b>Because there are two ways to look at a pocket and both have to work.</b> Crossing in from the game
    /// gets a light from <c>PocketHost</c>; pressing <b>F6</b> on the scene — which is how a pocket is actually
    /// authored — has no host at all, so that workflow needs one *in* the scene. With both present and both
    /// polling, the key would toggle twice in a frame and the feature would read as dead. First into the tree wins,
    /// which is the host's when there is one, and neither case needs the author to remember anything.
    /// </remarks>
    private static DreamAuthorLight _owner;

    private readonly Dictionary<WorldEnvironment, float> _ambient = new();
    private readonly Dictionary<SubViewportContainer, Material> _looks = new();

    private bool _keyDown;

    /// <summary>Whether the flood is up right now. For the gates and for a readout.</summary>
    public bool IsFlooded { get; private set; }

    /// <summary>Whether this is the instance the key talks to. For the gates.</summary>
    public bool OwnsTheKey => _owner == this;

    public override void _Process(double delta) => Poll();

    /// <summary>Claim the key if nothing living holds it.</summary>
    /// <remarks>
    /// The validity test is not defensive padding: an owner freed without leaving the tree — an orphaned
    /// fixture, a pocket torn down oddly — would otherwise hold the claim forever and leave every later light
    /// inert, which is a dead key with no error anywhere.
    /// </remarks>
    public override void _EnterTree()
    {
        if (_owner == null || !GodotObject.IsInstanceValid(_owner)) _owner = this;
    }

    /// <summary>Put everything back if this node goes away before the pocket does, and drop the claim.</summary>
    public override void _ExitTree()
    {
        Restore();
        if (_owner == this) _owner = null;
    }

    /// <summary>
    /// Read the key and toggle on the press edge. One frame's worth.
    /// </summary>
    /// <remarks>
    /// <b>Public and edge-detected so the gates can drive it without a frame.</b> The verifier is synchronous
    /// and <c>_Process</c> never ticks meaningfully in a headless run, so a check that pressed a key and waited
    /// would measure nothing — the seam <see cref="DreamVerbHold.Poll"/> and <c>DebugRepaintNow</c> both exist
    /// for. The edge is kept here rather than by asking the engine, because holding the key should not strobe.
    /// </remarks>
    public void Poll()
    {
        // Claim it here as well as in `_EnterTree`, so the key heals itself the frame after an owner goes
        // away. `PocketHost` frees its light before it frees the geometry, which leaves a scene-side light
        // ownerless for exactly that gap — and without this, the key would be dead for the rest of the visit with
        // a perfectly good light sitting in the tree.
        if (_owner == null || !GodotObject.IsInstanceValid(_owner)) _owner = this;
        if (!OwnsTheKey || !InputMap.HasAction(ToggleAction)) return;

        var down = Input.IsActionPressed(ToggleAction);
        if (down && !_keyDown) Toggle();
        _keyDown = down;
    }

    /// <summary>Flood if dark, restore if flooded.</summary>
    public void Toggle()
    {
        if (IsFlooded) Restore();
        else Flood();
    }

    /// <summary>
    /// Light every <c>WorldEnvironment</c> under this node's parent and take the look material off every
    /// container, remembering what each one was.
    /// </summary>
    /// <remarks>
    /// <b>It walks from the parent, not from itself.</b> The host is the parent, so the loaded pocket is a
    /// sibling — searching this node's own children would find nothing and the key would read as broken.
    /// <para>
    /// <b>It never touches the shader's parameters, and that is the one rule here that protects a file.</b>
    /// <c>DreamLook.tres</c> is a shared resource on disk; writing <c>Clamp</c> or <c>Levels</c> through it
    /// would change the look of every pocket in the game and persist if anything saved. Detaching the whole
    /// material is a property on one node in one instanced scene, and it dies with that instance.
    /// </para>
    /// </remarks>
    public void Flood()
    {
        if (IsFlooded) return;

        var root = GetParent();
        if (root == null) return;

        foreach (var node in Descendants(root))
        {
            switch (node)
            {
                case WorldEnvironment { Environment: not null } we when !_ambient.ContainsKey(we):
                    _ambient[we] = we.Environment.AmbientLightEnergy;
                    we.Environment.AmbientLightEnergy = FloodAmbientEnergy;
                    break;

                case SubViewportContainer svc when !_looks.ContainsKey(svc):
                    _looks[svc] = svc.Material;
                    svc.Material = null;
                    break;
            }
        }

        IsFlooded = true;
    }

    /// <summary>Put back exactly what was there, and forget it.</summary>
    /// <remarks>
    /// Idempotent, and safe against a node freed underneath it — a pocket can be left while the flood is up,
    /// which frees the geometry and leaves this holding references to nothing.
    /// </remarks>
    public void Restore()
    {
        foreach (var pair in _ambient)
            if (GodotObject.IsInstanceValid(pair.Key) && pair.Key.Environment != null)
                pair.Key.Environment.AmbientLightEnergy = pair.Value;

        foreach (var pair in _looks)
            if (GodotObject.IsInstanceValid(pair.Key))
                pair.Key.Material = pair.Value;

        _ambient.Clear();
        _looks.Clear();
        IsFlooded = false;
    }

    /// <summary>Every node under <paramref name="root"/>, at any depth, including itself.</summary>
    /// <remarks>
    /// Walked rather than assumed to be the probe's arrangement, for the reason <c>PocketHost.FindViewports</c>
    /// walks: a pocket may nest more than one viewport, and a missed one is a region of the screen that stays
    /// posterised while the rest floods — which reads as the feature being broken rather than as a miss.
    /// </remarks>
    private static IEnumerable<Node> Descendants(Node root)
    {
        yield return root;

        foreach (var child in root.GetChildren())
            foreach (var descendant in Descendants(child))
                yield return descendant;
    }
}
