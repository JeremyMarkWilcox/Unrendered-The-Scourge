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

        // First footfall in a zone opens the front: respawns stop, drain eases slightly.
        if (!string.IsNullOrEmpty(room.ZoneId) &&
            WorldStateManager.Instance?.GetZonePhase(room.ZoneId) == ZonePhase.Corrupted)
        {
            WorldStateManager.Instance.SetZonePhase(room.ZoneId, ZonePhase.ActiveFront);
        }

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
}
