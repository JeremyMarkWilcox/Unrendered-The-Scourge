using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// The carried lamp, put out while <see cref="DreamVerb.Suppress"/> is held. Attach this script to the
/// player's <c>OmniLight3D</c> — it replaces the node's type and needs no wiring.
///
/// <para><b>This is the act; <c>DreamVerbs</c> is the cost.</b> That manager has priced the hold since unit 8
/// and says the doing belongs to the 3D pass — so this is the smallest honest version of the doing, and it
/// deliberately has no opinion about the camera, the creatures or the AI.</para>
/// </summary>
/// <remarks>
/// <b>It drives the radius and not the energy, and that is forced rather than chosen.</b>
/// <c>DreamLightFlicker</c> writes <c>LightEnergy</c> every frame from a base it captured at <c>_Ready</c>, so
/// anything else writing energy is overwritten before it is ever seen — a feature that would look simply
/// broken with nothing in the log. The radius is also the channel the design already names: `combat.md`
/// calls Suppress *"the near field goes dark"*, and `dream-probe.md` calls the same number *"the Resolve
/// degrade in its entirety"*. One channel, two things that dim it, no third writer.
/// <para>
/// <b>While you are lit it writes nothing at all</b>, and re-reads the radius instead. That is what keeps the
/// tuner's <c>,</c> / <c>.</c> keys working: a node that drove the radius unconditionally would stomp every
/// nudge on the next frame, and tuning the lamp is the thing those keys exist for. It takes the channel over
/// only while it has something to say.
/// </para>
/// </remarks>
[GlobalClass]
public partial class DreamSuppressibleLight : OmniLight3D
{
    /// <summary>
    /// How much lamp is left while hiding. <b>0 puts it out entirely; 0.1 leaves a stub you can still read the
    /// floor by.</b>
    /// </summary>
    /// <remarks>
    /// <b>A number rather than a ruling, because the question it settles is a feel question.</b> Whether
    /// suppression should be total — invisible and blind — or partial, so distance still matters, is decided
    /// by playing it, and both answers are one field apart. What must not move is
    /// <c>DreamCreatureData.PursuesOnlyWhenLit</c>, which is a *boolean about detection*: this dims the lamp,
    /// it does not make being seen a matter of degree. If partial suppression should also mean partially
    /// visible, that is a change to the creature model and a decision, not a slider.
    /// </remarks>
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float SuppressedFraction { get; set; }

    /// <summary>
    /// Seconds for the lamp to go out or come back. Not instant, because an instant cut reads as a bug in the
    /// renderer rather than as an act — and it is short, because a verb you pay for by the second should not
    /// spend a quarter of its first second arriving.
    /// </summary>
    [Export(PropertyHint.Range, "0.01,2,0.01")]
    public float FadeSeconds { get; set; } = 0.2f;

    /// <summary>
    /// How far the lamp is thrown while <see cref="DreamVerb.Flare"/> is held, as a multiple of its radius.
    /// </summary>
    /// <remarks>
    /// <b>Flare's act costs one field because Suppress already claimed the right channel.</b> §19 calls Flare
    /// *"the exact mirror of Suppress"* — one puts the near field out, the other throws it wide — so once the
    /// radius was the thing Suppress drives, the mirror was a multiplier on the same number rather than a
    /// second mechanism.
    /// <para>
    /// <b>What is *not* here is the rest of the verb.</b> Flare also reveals hidden geometry and lets
    /// everything in the pocket see you. The first wants a <c>PocketGate</c> that reads the hold rather than
    /// the grant; the second is creature AI, which is unwritten. This is the light, and only the light —
    /// worth knowing before concluding Flare is finished.
    /// </para>
    /// </remarks>
    [Export(PropertyHint.Range, "1,8,0.25")]
    public float FlaredMultiple { get; set; } = 3.0f;

    private float _baseRange;
    private float _factor = 1.0f;

    /// <summary>How far through the fade it is: 1 fully lit, 0 fully out. For the gates.</summary>
    public float Factor => _factor;

    /// <summary>The radius it returns to. Whatever the lamp was last set to while lit. For the gates.</summary>
    public float BaseRange => _baseRange;

    public override void _Ready() => _baseRange = OmniRange;

    public override void _Process(double delta) => Tick(delta);

    /// <summary>
    /// One frame of fade. Public so the gates can drive it — <c>_Process</c> does not tick meaningfully in a
    /// headless run, the same seam <see cref="DreamVerbHold.Poll"/> exists for.
    /// </summary>
    public void Tick(double delta)
    {
        // Flare first, then Suppress, so **Suppress wins when both are held**. Holding the two together is a
        // contradiction — hide and shout — and the player pays for both, additively, because `DreamVerbs`
        // sums the rates. Going dark is the safe way to resolve it: the alternative hands someone who
        // fat-fingered Q a lamp three times its size in a room full of things that hunt light.
        var target = DreamVerbs.IsHolding(DreamVerb.Flare) ? Mathf.Max(FlaredMultiple, 1.0f) : 1.0f;
        if (DreamVerbs.IsHolding(DreamVerb.Suppress)) target = Mathf.Clamp(SuppressedFraction, 0.0f, 1.0f);

        // Lit and staying lit: hands off the radius entirely, and take whatever it says now as the value to
        // come back to. This is the branch that lets the tuner keep the lamp.
        if (Mathf.IsEqualApprox(_factor, 1.0f) && Mathf.IsEqualApprox(target, 1.0f))
        {
            _baseRange = OmniRange;
            return;
        }

        var step = FadeSeconds <= 0.0f ? 1.0f : (float)delta / FadeSeconds;
        _factor = Mathf.MoveToward(_factor, target, step);
        OmniRange = _baseRange * _factor;
    }
}
