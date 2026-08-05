using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// The room registry and the screen graph. Every <c>RoomData</c> under
/// <see cref="GameConfig.RoomDataDir"/> is discovered at boot and keyed by its Id, so
/// hotspots can point at rooms by string without any scene coupling.
/// </summary>
public partial class NavigationManager : Node
{
    public static NavigationManager Instance { get; private set; }

    private readonly Dictionary<string, RoomData> _rooms = new();

    public string CurrentRoomId { get; private set; } = "";
    public RoomData CurrentRoom { get; private set; }

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready() => LoadRoomRegistry();

    // ---- Registry --------------------------------------------------------

    private void LoadRoomRegistry()
    {
        _rooms.Clear();

        using var dir = DirAccess.Open(GameConfig.RoomDataDir);
        if (dir == null)
        {
            GD.PushError($"[NavigationManager] Cannot open {GameConfig.RoomDataDir}");
            return;
        }

        dir.ListDirBegin();
        for (var file = dir.GetNext(); file != ""; file = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;

            // Exported builds may present resources as "foo.tres.remap".
            var name = file.EndsWith(".remap") ? file[..^6] : file;
            if (!name.EndsWith(".tres")) continue;

            var room = GD.Load<RoomData>($"{GameConfig.RoomDataDir}/{name}");
            if (room == null)
            {
                GD.PushWarning($"[NavigationManager] Failed to load room resource: {name}");
                continue;
            }

            if (string.IsNullOrEmpty(room.Id))
            {
                GD.PushWarning($"[NavigationManager] Room resource {name} has a blank Id; skipping.");
                continue;
            }

            if (_rooms.ContainsKey(room.Id))
            {
                GD.PushWarning($"[NavigationManager] Duplicate room Id '{room.Id}' in {name}.");
                continue;
            }

            _rooms[room.Id] = room;
        }
        dir.ListDirEnd();

        GD.Print($"[NavigationManager] Registered {_rooms.Count} room(s): {string.Join(", ", _rooms.Keys)}");
    }

    public RoomData GetRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return null;
        return _rooms.TryGetValue(roomId, out var r) ? r : null;
    }

    public IReadOnlyDictionary<string, RoomData> Rooms => _rooms;

    /// <summary>
    /// Register a room built in code. For the gates, and specifically so camps can be asserted
    /// without an authored camp existing — which room is a camp is a judged call Jeremy makes in the
    /// editor, and the mechanic must be provable before he makes it. Same role as
    /// <see cref="InventoryCraftingManager.DebugRegisterRecipe"/>. Not for gameplay.
    /// </summary>
    public void DebugRegisterRoom(RoomData room)
    {
        if (room == null || string.IsNullOrEmpty(room.Id)) return;
        _rooms[room.Id] = room;
    }

    // ---- Navigation ------------------------------------------------------

    public bool GoToRoom(string roomId)
    {
        var room = GetRoom(roomId);
        if (room == null)
        {
            GD.PushError($"[NavigationManager] No room registered with Id '{roomId}'.");
            EventBus.Instance?.EmitNotify($"[missing room: {roomId}]");
            return false;
        }

        if (!string.IsNullOrEmpty(CurrentRoomId))
            EventBus.Instance?.EmitRoomExiting(CurrentRoomId);

        CurrentRoomId = room.Id;
        CurrentRoom = room;

        // Entities are seeded on first sight only — re-entering never resets them.
        foreach (var placement in room.Entities)
            EntityStateManager.Instance?.Register(placement);

        // The zone system's "first footfall opens the front" flip stood here until 2026-08-03. Its slot
        // is now the per-screen verdict being re-derived, which is a closer fit than it looks: both are
        // "arriving somewhere updates what that place is".
        //
        // It must run *after* the registration above (the verdict reads entity states) and *before* the
        // signal below (RoomView paints the backdrop off this flag). Silent — arriving is not an
        // achievement; see MendManager.Reevaluate.
        MendManager.Reevaluate(room);

        EventBus.Instance?.EmitRoomEntered(room.Id);
        return true;
    }

    /// <summary>Look up a placement in the current room by instance id.</summary>
    public EntityPlacement FindPlacement(string instanceId)
    {
        if (CurrentRoom == null || string.IsNullOrEmpty(instanceId)) return null;

        foreach (var p in CurrentRoom.Entities)
            if (p != null && p.InstanceId == instanceId) return p;

        return null;
    }

    /// <summary>
    /// The creature on this screen, or <c>null</c> where there is none. What the satchel's <i>Set</i> button
    /// rigs against.
    /// </summary>
    /// <remarks>
    /// <b>The name is a claim about content, and it is the reason this is a named readout rather than a
    /// <c>FirstOrDefault</c> at each call site.</b> Every one of the thirty authored rooms that holds a
    /// creature holds exactly one, so <i>which</i> body a room-level verb means has never been a question —
    /// which is what lets rigging be a button instead of a target-picking mode. That invariant was assumed
    /// by the retired F8 key for two units and gated by nothing; it is asserted now
    /// (<c>GateFights</c>, *"no room holds two creatures for a room-level verb to choose between"*), so
    /// the day it stops being true a check says so and names the room, rather than the button quietly
    /// rigging against whichever placement happens to be first in the file.
    /// <para>
    /// <b>Placements with no <c>Entity</c> are skipped, and that is not the same as being empty.</b> A
    /// placement whose <c>Entity</c> failed to resolve is a content error the authoring sweeps already fail
    /// (a body that cannot be drawn or fought), so a verb offering itself against one would be offering
    /// something that cannot happen. It is also what keeps this honest in the suite, where
    /// <c>verify_held</c>'s body is deliberately id-only.
    /// </para>
    /// </remarks>
    public EntityPlacement SoleCreatureHere()
    {
        if (CurrentRoom?.Entities == null) return null;

        foreach (var p in CurrentRoom.Entities)
            if (p?.Entity != null) return p;

        return null;
    }
}
