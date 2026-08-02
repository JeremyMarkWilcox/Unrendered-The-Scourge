using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// Owns which realm the player is <i>in</i>.
///
/// <para>
/// It owned which realm the player was <i>perceiving</i> until 2026-08-02, and a Tab binding that
/// flipped it at will. The great swap deleted spectral vision, and with it the only caller that
/// changed realm as a look rather than as a journey. What is left is a place: the realm changes
/// when <see cref="DreamRunManager"/> takes you into a pocket and when it throws you out, and
/// nothing else moves it.
/// </para>
///
/// <para>
/// The state is kept rather than folded into <c>DreamRunManager.IsRunning</c> because the two will
/// come apart: unit 6's pockets have a quiet version with no run and no clock, and you are still
/// in the Dream while you walk one.
/// </para>
/// </summary>
public partial class RealmTransitionManager : Node
{
    public static RealmTransitionManager Instance { get; private set; }

    public GameRealm CurrentRealm { get; private set; } = GameRealm.Waking;

    /// <summary>Set while a duel, menu or cutscene owns the view.</summary>
    public bool TransitionsLocked { get; set; }

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // `_Input` and `Toggle` — the Tab handler, written into _Input rather than _UnhandledInput
    // because Tab is also Godot's built-in ui_focus_next and a focused Control would eat it first.
    // Both deleted 2026-08-02, along with the `toggle_realm` action in project.godot. Nothing
    // toggles a realm any more; you travel.

    public void SetRealm(GameRealm realm, bool force = false)
    {
        if (TransitionsLocked && !force) return;
        if (realm == CurrentRealm) return;

        CurrentRealm = realm;
        EventBus.Instance?.EmitRealmChanged(realm);
    }

    public bool IsDream => CurrentRealm == GameRealm.Dream;
}
