using Godot;

namespace Unrendered.Core;

/// <summary>
/// Global signal hub. Managers publish here; views subscribe. Nothing in the game holds a
/// hard reference to anything else, which is what lets a single state change ripple out to
/// every backdrop, overlay and HUD element at once — the "signals from the World Manager"
/// the design doc asks for.
///
/// Godot signals cannot carry C# enums, so enum payloads travel as ints. Use the typed
/// Emit* helpers below rather than calling EmitSignal directly, and cast on receipt.
/// </summary>
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ---- Realm -----------------------------------------------------------

    /// <summary>Spectral vision toggled. Payload is a <see cref="GameRealm"/>.</summary>
    [Signal] public delegate void RealmChangedEventHandler(int realm);

    // ---- World state -----------------------------------------------------

    [Signal] public delegate void WorldFlagChangedEventHandler(string flag, bool value);

    /// <summary>Payload is a <see cref="ZonePhase"/>.</summary>
    [Signal] public delegate void ZonePhaseChangedEventHandler(string zoneId, int phase);

    // ---- Entities --------------------------------------------------------

    /// <summary>Payload is an <see cref="EntityState"/>.</summary>
    [Signal] public delegate void EntityStateChangedEventHandler(string entityId, int newState);

    // ---- Navigation ------------------------------------------------------

    [Signal] public delegate void RoomExitingEventHandler(string roomId);
    [Signal] public delegate void RoomEnteredEventHandler(string roomId);

    // ---- Resolve ---------------------------------------------------------

    [Signal] public delegate void ResolveChangedEventHandler(float current, float max);

    // ---- Dream heist -----------------------------------------------------

    [Signal] public delegate void HeistStartedEventHandler(string heistId);

    /// <summary>Payload is a <see cref="HeistOutcome"/>.</summary>
    [Signal] public delegate void HeistEndedEventHandler(string heistId, int outcome);

    /// <summary>
    /// How many rooms away the pursuer is, by shortest path. 0 means it is on top of you,
    /// -1 means unreachable/unknown. Drives the dread cues rather than any UI number.
    /// </summary>
    [Signal] public delegate void PursuerProximityChangedEventHandler(int roomsAway);

    // ---- Inventory -------------------------------------------------------

    [Signal] public delegate void ItemAcquiredEventHandler(string itemId, int amount);
    [Signal] public delegate void InventoryChangedEventHandler();

    // ---- Feedback --------------------------------------------------------

    /// <summary>A line of text for the player: examine flavour, refusals, pickups.</summary>
    [Signal] public delegate void NotifyEventHandler(string text);

    /// <summary>Cursor moved onto or off a hotspot. Blank label means "nothing under the cursor".</summary>
    [Signal] public delegate void HoverTargetChangedEventHandler(string label, string verb);

    // ---- Typed emitters --------------------------------------------------

    public void EmitRealmChanged(GameRealm realm)
        => EmitSignal(SignalName.RealmChanged, (int)realm);

    public void EmitWorldFlagChanged(string flag, bool value)
        => EmitSignal(SignalName.WorldFlagChanged, flag, value);

    public void EmitZonePhaseChanged(string zoneId, ZonePhase phase)
        => EmitSignal(SignalName.ZonePhaseChanged, zoneId, (int)phase);

    public void EmitEntityStateChanged(string entityId, EntityState state)
        => EmitSignal(SignalName.EntityStateChanged, entityId, (int)state);

    public void EmitRoomExiting(string roomId)
        => EmitSignal(SignalName.RoomExiting, roomId);

    public void EmitRoomEntered(string roomId)
        => EmitSignal(SignalName.RoomEntered, roomId);

    public void EmitResolveChanged(float current, float max)
        => EmitSignal(SignalName.ResolveChanged, current, max);

    public void EmitHeistStarted(string heistId)
        => EmitSignal(SignalName.HeistStarted, heistId);

    public void EmitHeistEnded(string heistId, HeistOutcome outcome)
        => EmitSignal(SignalName.HeistEnded, heistId, (int)outcome);

    public void EmitPursuerProximityChanged(int roomsAway)
        => EmitSignal(SignalName.PursuerProximityChanged, roomsAway);

    public void EmitItemAcquired(string itemId, int amount)
        => EmitSignal(SignalName.ItemAcquired, itemId, amount);

    public void EmitInventoryChanged()
        => EmitSignal(SignalName.InventoryChanged);

    public void EmitNotify(string text)
        => EmitSignal(SignalName.Notify, text);

    public void EmitHoverTargetChanged(string label, string verb)
        => EmitSignal(SignalName.HoverTargetChanged, label, verb);
}
