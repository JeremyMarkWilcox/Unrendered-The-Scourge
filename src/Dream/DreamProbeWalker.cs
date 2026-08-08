using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// First-person walk and look for the Dream probe. Slow on purpose.
/// </summary>
/// <remarks>
/// <para>
/// The walk speed is a design decision rather than a feel knob. <c>combat.md</c> §14 records the
/// real risk of going 3D and it is mechanical, not artistic: real-time movement adds a reflex
/// requirement, and a player who cannot dodge would be walled out of the sever route and pushed
/// into overkill — the difficulty slider invariant 1 exists to prevent. So: deliberate rather than
/// twitchy. Slower is also more frightening, which is why this is probably not a trade at all.
/// </para>
/// <para>
/// <b>It polls keys and is handed mouse deltas; it never reads the mouse itself.</b> That is not a
/// style choice. Whether <c>_UnhandledInput</c> reaches a node inside a <c>SubViewport</c> depends
/// on how the container and viewport are configured, and the probe is the wrong place to discover
/// that. <see cref="Input.IsPhysicalKeyPressed"/> is global, and <see cref="Look"/> is called by
/// <see cref="DreamLookTuner"/>, which sits on the scene root in the main tree — both work
/// regardless of viewport routing. Only the probe needs this; a real controller can use the input
/// map, and polling here also keeps the probe from having to add actions to
/// <c>project.godot</c>, which is shared state the gates stand on.
/// </para>
/// <para>
/// <b>No jump, and that is a design position rather than an omission.</b> The Dream is meant to test
/// what you understood about a room, not your execution — reflex is <i>one</i> route past a creature
/// and never the only one, and that fairness argument is what lets a player who cannot dodge still
/// finish. Platforming is the least accessible possible skill test: fail it and there is no alternative
/// route, you simply cannot pass. It also fights every other decision here — you cannot see your feet
/// in first person, and at 480x270 with two greys and a suppressed spirit light you certainly cannot
/// judge a ledge edge. And it would break level design that has just become load-bearing: any wall you
/// can jump is a wall that cannot reliably break a boss's line of sight for ten seconds.
/// <b>Verticality is authored instead</b> — steps, ramps and climb points — so height costs the player
/// attention rather than timing. If the reflexive space-bar press ever needs answering, a hop that does
/// nothing mechanically is cheaper than a jump that does.
/// </para>
/// <para>
/// No rigid bodies yet either. A <c>CharacterBody3D</c> sliding along static collision is the floor and
/// was never the deferred part; pushing and dropping arrive with the puzzle that asks for a specific
/// one, so the complexity budget stays attached to a consumer.
/// </para>
/// </remarks>
public partial class DreamProbeWalker : CharacterBody3D
{
    [Export] public float WalkSpeed { get; set; } = 2.2f;

    /// <summary>
    /// Hold Shift. Roughly double, which is the point — it has to be worth the cost.
    /// </summary>
    /// <remarks>
    /// <b>Sprint is not a convenience here, it is the reflex route past a creature</b> — the design
    /// ratified two ways past anything, reflex or the environment, and this is the mechanical half of
    /// the first one. Which also means it must cost something, and it already does without a new
    /// system: bosses acquire by proximity, <i>noise</i> or light, so <b>running is loud</b>. That is
    /// the cost. **No stamina bar** — a third pool would price the same decision twice and the game
    /// already has two pools it is careful about.
    /// </remarks>
    [Export] public float SprintSpeed { get; set; } = 4.2f;

    [Export] public float Gravity { get; set; } = 14.0f;
    [Export] public float LookSensitivity { get; set; } = 0.0022f;

    /// <summary>
    /// Metres per second of rise while <see cref="DreamVerb.Float"/> is held.
    /// </summary>
    /// <remarks>
    /// <b>The act for the second verb, and it is here because the first one taught the lesson.</b> Suppress
    /// was priced in unit 8 and had nothing happen for three days — the meter drained, the light stayed on,
    /// and it read as broken rather than unbuilt. Float was in the same state until now: the hold was live,
    /// the drain was real, and Space did nothing at all.
    /// <para>
    /// <b>Slow on purpose.</b> `Enums.cs` calls it *"slow rise and slight lateral drift — vertical traversal
    /// without ladders"*, which is a lift and not a jump. At 4 Resolve a second against a 100 bar, a full
    /// meter buys about 25 seconds of rise, so this number and the drain together are what decide how high a
    /// pocket can gate something. Change one and re-walk the ledge.
    /// </para>
    /// </remarks>
    [Export] public float FloatRiseSpeed { get; set; } = 1.6f;

    /// <summary>
    /// How much of your walking speed you keep in the air while floating, 0 to 1.
    /// </summary>
    /// <remarks>
    /// The *drift* half of the verb. Full control would make floating a better walk than walking; none at all
    /// would make it a lift you cannot steer, and every gated ledge would have to sit directly above a
    /// launch point. A fraction is the reading that keeps it traversal rather than flight.
    /// </remarks>
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float FloatDrift { get; set; } = 0.5f;

    /// <summary>Whether the rise is happening this frame. For the gates and a readout.</summary>
    public bool Floating { get; private set; }

    private Camera3D _camera;
    private float _pitch;

    public override void _Ready() => _camera = GetNode<Camera3D>("%Camera");

    /// <summary>
    /// Applies a mouse delta. Yaw turns the body so movement follows the view; pitch belongs to the
    /// camera alone, or looking down would steer you into the floor.
    /// </summary>
    public void Look(Vector2 relative)
    {
        RotateY(-relative.X * LookSensitivity);
        _pitch = Mathf.Clamp(_pitch - (relative.Y * LookSensitivity), -1.55f, 1.55f);
        _camera.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        var wish = Transform.Basis * new Vector3(Axis(Key.D, Key.A), 0.0f, Axis(Key.S, Key.W));
        wish.Y = 0.0f;
        if (wish.LengthSquared() > 0.0f)
        {
            wish = wish.Normalized();
        }

        // **`IsHolding`, not `Holds`.** The first asks whether the key is down this instant, the second
        // whether the verb was ever taught — and a ledge that lifts you the moment you learn Float, forever,
        // is a different game. `DreamVerbs` refuses the hold outside a pocket and on an empty bar, so this
        // needs no guard of its own: the meter running out drops you, which is the intended failure.
        Floating = DreamVerbs.IsHolding(DreamVerb.Float);

        var speed = Input.IsPhysicalKeyPressed(Key.Shift) ? SprintSpeed : WalkSpeed;
        if (Floating) speed *= Mathf.Clamp(FloatDrift, 0.0f, 1.0f);

        var velocity = Velocity;
        velocity.X = wish.X * speed;
        velocity.Z = wish.Z * speed;

        // Rise beats gravity outright rather than fighting it. A float that fell back at (rise − gravity)
        // would need `FloatRiseSpeed` tuned against `Gravity` to go up at all, which is two numbers holding
        // one decision — and the first time someone lowered the rise it would silently become a slow fall.
        if (Floating) velocity.Y = FloatRiseSpeed;
        else velocity.Y = IsOnFloor() ? 0.0f : velocity.Y - (Gravity * (float)delta);

        Velocity = velocity;
        MoveAndSlide();
    }

    private static float Axis(Key positive, Key negative) =>
        (Input.IsPhysicalKeyPressed(positive) ? 1.0f : 0.0f)
        - (Input.IsPhysicalKeyPressed(negative) ? 1.0f : 0.0f);
}
