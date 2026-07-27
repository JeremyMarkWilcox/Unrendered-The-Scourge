using Godot;
using Unrendered.Managers;
using Unrendered.Save;
using Unrendered.Tools;
using Unrendered.UI;
using Unrendered.World;

namespace Unrendered.Core;

/// <summary>
/// Root of the running game. Owns the room view and the HUD, kicks off the first
/// navigation once the autoloads have registered themselves, and hosts the debug keys
/// that make the milestone gates checkable by hand.
/// </summary>
public partial class Main : Node
{
    private RoomView _roomView;
    private Hud _hud;

    public override void _Ready()
    {
        _roomView = GetNode<RoomView>("RoomView");
        _hud = GetNode<Hud>("Hud");

        // Deferred so every autoload's _Ready (notably the room registry scan) has run.
        CallDeferred(nameof(StartGame));
    }

    private void StartGame()
    {
        if (NavigationManager.Instance?.GoToRoom(GameConfig.StartingRoomId) != true)
        {
            GD.PushError($"[Main] Could not enter starting room '{GameConfig.StartingRoomId}'.");
            return;
        }

        if (SanityManager.Instance != null) SanityManager.Instance.Active = true;

        if (Boot.VerifySlice)
        {
            GetTree().Quit(SliceVerifier.Run());
            return;
        }

        if (Boot.CaptureShots)
        {
            _ = ScreenshotTool.CaptureSlice(this);
            return;
        }

        EventBus.Instance?.EmitNotify("Cold. The track keeps going. Press Tab to look the other way.");
    }

    // ---- Debug -----------------------------------------------------------

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsEcho() || !@event.IsPressed()) return;

        if (@event.IsActionPressed("debug_panel"))
        {
            _hud.ToggleDebugPanel(_roomView);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_cycle_entity"))
        {
            CycleEntityStates();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_save"))
        {
            if (SaveService.Save()) EventBus.Instance?.EmitNotify("Saved.");
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_load"))
        {
            if (SaveService.Load()) EventBus.Instance?.EmitNotify("Loaded.");
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Steps every entity in the current room one place along the state enum. This is the
    /// M2 gate: press it and watch both realms update without a room reload.
    /// </summary>
    private static void CycleEntityStates()
    {
        var room = NavigationManager.Instance?.CurrentRoom;
        var entities = EntityStateManager.Instance;
        if (room == null || entities == null) return;

        foreach (var placement in room.Entities)
        {
            if (placement?.Entity == null) continue;

            var next = entities.GetState(placement.InstanceId) switch
            {
                EntityState.Corrupted => EntityState.Severed,
                EntityState.Severed => EntityState.Purified,
                _ => EntityState.Corrupted,
            };
            entities.SetState(placement.InstanceId, next);
        }

        EventBus.Instance?.EmitNotify("[debug] cycled entity states");
    }
}
