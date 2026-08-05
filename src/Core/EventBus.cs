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

    /// <summary>
    /// You have crossed into or out of the Dream. Payload is a <see cref="GameRealm"/>.
    /// <para>
    /// This was "spectral vision toggled" until 2026-08-02, fired on every <c>Tab</c> press. It now
    /// only fires when you actually travel, which is rare and load-bearing — so a subscriber can
    /// afford to do real work in it, where before it had to be cheap.
    /// </para>
    /// </summary>
    [Signal] public delegate void RealmChangedEventHandler(int realm);

    // ---- World state -----------------------------------------------------

    [Signal] public delegate void WorldFlagChangedEventHandler(string flag, bool value);

    // `ZonePhaseChanged(zoneId, phase)` went with `ZonePhase` on 2026-08-03. Nothing replaced it,
    // because nothing needed to: cleansing is a world flag now, so the signal above already carries it
    // and every subscriber was already listening to that one as well.

    // ---- Entities --------------------------------------------------------

    /// <summary>Payload is an <see cref="EntityState"/>.</summary>
    [Signal] public delegate void EntityStateChangedEventHandler(string entityId, int newState);

    // ---- Navigation ------------------------------------------------------

    [Signal] public delegate void RoomExitingEventHandler(string roomId);
    [Signal] public delegate void RoomEnteredEventHandler(string roomId);

    // ---- Resolve ---------------------------------------------------------

    [Signal] public delegate void ResolveChangedEventHandler(float current, float max);

    // ---- Vitality --------------------------------------------------------

    [Signal] public delegate void VitalityChangedEventHandler(float current, float max);

    /// <summary>
    /// The body gave out. Fires once per emptying, not once per hit at zero. <c>CampManager</c>
    /// listens and carries out §6's consequence: you wake at the last camp having lost what you can
    /// remake and kept what you cannot.
    /// </summary>
    [Signal] public delegate void VitalityDepletedEventHandler();

    /// <summary>
    /// The nerve gave out. The exact mirror of <see cref="VitalityDepletedEventHandler"/>, and
    /// <c>CampManager</c> listens to both and does the same thing — ruled 2026-08-04: *"we should use the
    /// same function if VIT or Resolve hit 0 that the player goes to the most recent camp."*
    /// </summary>
    /// <remarks>
    /// <b>Two signals rather than one shared "you are down", and the reason is what each carries.</b> They
    /// have the same consequence today, but they are raised by different managers about different pools, and
    /// a subscriber that cares which one fired — the fight screen, a future death counter, anything that
    /// words a line — would otherwise have to ask a manager which bar it was. Same argument that keeps
    /// `RoomExiting` and `RoomEntered` apart.
    /// <para>
    /// <b>Unit 6b left this hole and unit 8 fills it.</b> Retiring the heist deleted
    /// <c>NotifyResolveExhausted</c>, which had been the only thing that reacted to an empty nerve — so for
    /// one unit the bar could reach zero and nothing whatsoever happened.
    /// </para>
    /// </remarks>
    [Signal] public delegate void ResolveDepletedEventHandler();

    // ---- Dream pockets (unit 6) ------------------------------------------
    //
    // `HeistStarted`, `HeistEnded` and `PursuerProximityChanged` stood here and were deleted 2026-08-04
    // with unit 6b. The first two are replaced one-for-one by the pair below; the third has **no successor
    // and should not get one** — it reported how many *rooms* away the pursuer was, and a 3D pursuer is a
    // chase in space, so a room-hop count is the wrong shape to inherit. When it returns it brings its own
    // readout.

    /// <summary>The player crossed into a pocket. Emitted after the state is set, so a subscriber may
    /// trust <c>PocketManager.InPocket</c> rather than the payload (invariant 13).</summary>
    [Signal] public delegate void PocketEnteredEventHandler(string pocketId);

    /// <summary>
    /// The player came back out. <paramref name="completed"/> distinguishes the two exits, and it is the
    /// only thing that distinguishes them: a completed pocket has already written its flag and made its
    /// grant by the time this fires, and an abandoned one has written nothing at all.
    /// </summary>
    [Signal] public delegate void PocketLeftEventHandler(string pocketId, bool completed);

    // ---- Inventory -------------------------------------------------------

    [Signal] public delegate void ItemAcquiredEventHandler(string itemId, int amount);
    [Signal] public delegate void InventoryChangedEventHandler();

    /// <summary>Something was made at the bench. Payload is the output item, not the recipe.</summary>
    [Signal] public delegate void ItemCraftedEventHandler(string itemId, int amount);

    [Signal] public delegate void ItemUsedEventHandler(string itemId);

    /// <summary>
    /// A blueprint became known, which is what unlocks the recipes gated on it. The journal will
    /// subscribe to this rather than owning the knowledge itself.
    /// </summary>
    [Signal] public delegate void BlueprintDiscoveredEventHandler(string blueprintId);

    // ---- Mending ---------------------------------------------------------

    /// <summary>
    /// A blight was clicked and wants its board (unit 2, <c>combat.md</c> §22). <c>Hud</c> listens and
    /// opens <c>MendPanel</c>; nothing else does.
    /// </summary>
    /// <remarks>
    /// <b>Carries the hotspot rather than its id</b>, which is a departure from every other signal
    /// here. The panel needs the <c>MendPuzzle</c> hanging off it, and an id would mean re-finding the
    /// hotspot in the current room — a lookup that can miss, added purely to keep the payload a string.
    /// This is also what keeps <see cref="Unrendered.World.InteractionService"/> free of UI: it emits
    /// and returns, so the whole mend path stays drivable with no scene tree.
    /// </remarks>
    [Signal] public delegate void MendRequestedEventHandler(Data.HotspotData blight);

    /// <summary>
    /// A station was clicked and wants the crafting panel, narrowed to the recipes made there. Blank
    /// means the camp bench with nothing to narrow by — everything you know.
    /// </summary>
    /// <remarks>
    /// <b>Carries the id rather than the hotspot</b>, which is the opposite choice from
    /// <see cref="MendRequestedEventHandler"/> above and for the reason stated there: that one departs
    /// from the string payload because the panel needs the <c>MendPuzzle</c> resource hanging off the
    /// hotspot. A station has nothing hanging off it — the id *is* the filter, and every recipe the panel
    /// would show is in the registry rather than on the screen. So the default holds here.
    /// <para>
    /// <b>Nothing subscribes yet</b>, and that is the honest state: there is no crafting panel (the
    /// <c>Workbench</c> case in <c>InteractionService</c> says so out loud). The signal lands now anyway
    /// because it is the seam the gates drive — a station click is assertable headlessly the moment it
    /// emits, and the panel arriving later subscribes to something already proven to fire.
    /// </para>
    /// </remarks>
    [Signal] public delegate void CraftingRequestedEventHandler(string stationId);

    /// <summary>
    /// A fight was committed to, on the placement named. The fight panel's cue to open, and
    /// <b>the first combat signal on this bus</b> — added by unit 3a, which is the first unit in which
    /// a fight is a thing that starts rather than a thing the arithmetic describes.
    /// </summary>
    /// <remarks>
    /// Carries the instance id rather than the <c>EntityData</c>, which is the default here and correct
    /// for the reason <see cref="CraftingRequestedEventHandler"/> gives: everything hanging off a
    /// creature — its data, its wounds, its rig — is reachable from the id through a manager, and the id
    /// is the key all three of those are stored under anyway.
    /// <para>
    /// <b>There is no per-turn signal, and there should not be one.</b> A turn changes several things at
    /// once — a condition, a rig, a log line, a Vitality bar — and the pieces that already have signals
    /// (<see cref="VitalityChangedEventHandler"/>, <see cref="NotifyEventHandler"/>) fire on their own.
    /// The panel reads the rest off <c>EncounterManager</c> when it repaints, which is
    /// <c>MendPanel</c>'s precedent exactly: <i>a property that recomputes only proves it agrees with
    /// itself</i>, so the state has one owner and the screen asks it.
    /// </para>
    /// </remarks>
    [Signal] public delegate void FightStartedEventHandler(string instanceId);

    /// <summary>
    /// The fight let go — won, fled or collapsed. <b>Deliberately carries no outcome</b>, on
    /// <c>CombatManager.SettleEngagement</c>'s precedent: what happened is written in the body's state
    /// and in a flag, so a payload saying it a second time is a second place the two can disagree.
    /// </summary>
    [Signal] public delegate void FightEndedEventHandler(string instanceId);

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

    public void EmitEntityStateChanged(string entityId, EntityState state)
        => EmitSignal(SignalName.EntityStateChanged, entityId, (int)state);

    public void EmitRoomExiting(string roomId)
        => EmitSignal(SignalName.RoomExiting, roomId);

    public void EmitRoomEntered(string roomId)
        => EmitSignal(SignalName.RoomEntered, roomId);

    public void EmitResolveChanged(float current, float max)
        => EmitSignal(SignalName.ResolveChanged, current, max);

    public void EmitVitalityChanged(float current, float max)
        => EmitSignal(SignalName.VitalityChanged, current, max);

    public void EmitResolveDepleted()
        => EmitSignal(SignalName.ResolveDepleted);

    public void EmitVitalityDepleted()
        => EmitSignal(SignalName.VitalityDepleted);

    public void EmitPocketEntered(string pocketId)
        => EmitSignal(SignalName.PocketEntered, pocketId);

    public void EmitPocketLeft(string pocketId, bool completed)
        => EmitSignal(SignalName.PocketLeft, pocketId, completed);

    public void EmitItemAcquired(string itemId, int amount)
        => EmitSignal(SignalName.ItemAcquired, itemId, amount);

    public void EmitInventoryChanged()
        => EmitSignal(SignalName.InventoryChanged);

    public void EmitItemCrafted(string itemId, int amount)
        => EmitSignal(SignalName.ItemCrafted, itemId, amount);

    public void EmitItemUsed(string itemId)
        => EmitSignal(SignalName.ItemUsed, itemId);

    public void EmitBlueprintDiscovered(string blueprintId)
        => EmitSignal(SignalName.BlueprintDiscovered, blueprintId);

    public void EmitMendRequested(Data.HotspotData blight)
        => EmitSignal(SignalName.MendRequested, blight);

    public void EmitCraftingRequested(string stationId)
        => EmitSignal(SignalName.CraftingRequested, stationId);

    public void EmitFightStarted(string instanceId)
        => EmitSignal(SignalName.FightStarted, instanceId);

    public void EmitFightEnded(string instanceId)
        => EmitSignal(SignalName.FightEnded, instanceId);

    public void EmitNotify(string text)
        => EmitSignal(SignalName.Notify, text);

    public void EmitHoverTargetChanged(string label, string verb)
        => EmitSignal(SignalName.HoverTargetChanged, label, verb);
}
