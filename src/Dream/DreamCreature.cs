using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.Dream;

/// <summary>
/// A thing in a pocket that costs you. Fourth of the reusable nodes, and the same bargain as the other three:
/// drop it on an <c>Area3D</c>, point it at a <see cref="DreamCreatureData"/>, and the scene needs no script.
///
/// <para><b>Contact costs Resolve, never health</b> — invariant 2, and `dream-puzzles-vs-pursuer-tension`:
/// mindless creatures are a Resolve cost rather than a fight, and bosses eject. There is no attack, no
/// durability and no turn order in here.</para>
/// </summary>
/// <remarks>
/// <b>The cooldown is the whole reason this is a node rather than two lines in a scene.</b> <c>BodyEntered</c>
/// fires once per entry but a body resting inside an area can re-enter repeatedly, and a player pinned against
/// a creature would otherwise lose the meter in a handful of frames — an ejection that reads as arbitrary
/// because nothing on screen explains it. Timed here, once, so fourteen cloned pockets cannot each get it
/// slightly wrong.
/// </remarks>
[GlobalClass]
public partial class DreamCreature : Area3D
{
    /// <summary>What this one does. Without it the node is inert rather than free — see <c>_Ready</c>.</summary>
    [Export] public DreamCreatureData Data { get; set; }

    private float _cooldownLeft;

    /// <summary>Seconds until it can cost you again. For the gates.</summary>
    public float CooldownRemaining => _cooldownLeft;

    /// <summary>How many times it has landed a touch. For the gates.</summary>
    public int Touches { get; private set; }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        // Inert rather than harmless: a creature with no data would drift around costing nothing, which looks
        // exactly like a creature the player is successfully avoiding. Loud, because it is an authoring
        // mistake and the symptom is indistinguishable from working.
        if (Data == null)
            GD.PushWarning($"[DreamCreature] '{Name}' has no DreamCreatureData; it will cost nothing.");
    }

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    public override void _Process(double delta)
    {
        if (_cooldownLeft > 0f) _cooldownLeft -= (float)delta;
    }

    private void OnBodyEntered(Node3D body) => Touch();

    /// <summary>
    /// Land a touch, if it is allowed to. Returns whether it cost anything.
    /// </summary>
    /// <remarks>
    /// Public so a scene can drive it from its own pursuit code — reaching the player is the AI's business and
    /// the AI is Jeremy's, so this must be callable rather than only reactive.
    /// </remarks>
    public bool Touch()
    {
        if (Data == null) return false;
        if (_cooldownLeft > 0f) return false;

        var pockets = PocketManager.Instance;
        if (pockets == null || !pockets.InPocket) return false;

        _cooldownLeft = Data.TouchCooldownSeconds > 0f
            ? Data.TouchCooldownSeconds
            : GameConfig.CreatureTouchCooldownSeconds;
        Touches++;

        if (Data.Banishes)
        {
            // A boss. Same call as running out of nerve — writes nothing, so the entrance survives and the
            // cost is the trip back. Ruled 2026-08-04, and one path is the point.
            EventBus.Instance?.EmitNotify(
                $"{Called(Data)} puts something through you that is not a hand. You are in the cold again.");
            pockets.Eject();
            CampManager.Instance?.RespawnAtCamp();
            return true;
        }

        // A minor one. Takes a chunk and leaves you in the pocket — and if that chunk is the last of the bar,
        // `ResolveManager` raises `ResolveDepleted` and `CampManager` carries you out. So an empty bar is
        // handled in exactly one place regardless of what emptied it.
        EventBus.Instance?.EmitNotify($"{Called(Data)} is on you, and something goes out of you.");
        ResolveManager.Instance?.Modify(-Data.ResolveCost);
        return true;
    }

    // Called `Called` rather than `Name`, which shadows `Node.Name` — a compiler warning, and a real trap in
    // this class specifically: `Name` is the node's own name and it is used in the warning above, so two
    // different things would have read as one identifier inside the same file.
    private static string Called(DreamCreatureData data) =>
        string.IsNullOrEmpty(data.DisplayName) ? "Something" : data.DisplayName;
}
