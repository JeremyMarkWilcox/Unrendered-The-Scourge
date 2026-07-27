using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// Owns which realm the player is perceiving, and the input that flips it.
///
/// The logical flip is instantaneous — gameplay queries never wait on an animation. Views
/// receive <c>RealmChanged</c> immediately and run their own crossfade to catch up, so a
/// click landing mid-transition still resolves against the realm you actually chose.
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

    /// <summary>
    /// Handled in _Input rather than _UnhandledInput on purpose: Tab is also bound to
    /// Godot's built-in ui_focus_next, and a focused Control would eat it first.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("toggle_realm")) return;
        if (@event.IsEcho()) return;

        Toggle();
        GetViewport().SetInputAsHandled();
    }

    public void Toggle() =>
        SetRealm(CurrentRealm == GameRealm.Waking ? GameRealm.Dream : GameRealm.Waking);

    public void SetRealm(GameRealm realm, bool force = false)
    {
        if (TransitionsLocked && !force) return;
        if (realm == CurrentRealm) return;

        CurrentRealm = realm;
        EventBus.Instance?.EmitRealmChanged(realm);
    }

    public bool IsDream => CurrentRealm == GameRealm.Dream;
}
