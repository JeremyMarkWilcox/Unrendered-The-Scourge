using System.Text.Json;
using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.Save;

/// <summary>
/// Gathers state from the managers into a <see cref="SaveGame"/> and back. Uses Godot's
/// FileAccess rather than System.IO so that <c>user://</c> resolves correctly on every
/// platform and inside exported builds.
/// </summary>
public static class SaveService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static bool Save(string path = GameConfig.SaveFilePath)
    {
        var data = new SaveGame
        {
            CurrentRoomId = NavigationManager.Instance?.CurrentRoomId ?? "",
            CurrentRealm = (int)(RealmTransitionManager.Instance?.CurrentRealm ?? GameRealm.Waking),
            Sanity = SanityManager.Instance?.Current ?? GameConfig.SanityMax,
            Flags = WorldStateManager.Instance?.ExportFlags() ?? new(),
            ZonePhases = WorldStateManager.Instance?.ExportZonePhases() ?? new(),
            EntityStates = EntityStateManager.Instance?.ExportStates() ?? new(),
            Inventory = InventoryCraftingManager.Instance?.ExportStacks() ?? new(),
        };

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError($"[Save] Could not open {path} for writing: {FileAccess.GetOpenError()}");
            return false;
        }

        file.StoreString(JsonSerializer.Serialize(data, Options));
        GD.Print($"[Save] Wrote {path}");
        return true;
    }

    public static bool Load(string path = GameConfig.SaveFilePath)
    {
        if (!FileAccess.FileExists(path))
        {
            EventBus.Instance?.EmitNotify("No save file.");
            return false;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[Save] Could not open {path}: {FileAccess.GetOpenError()}");
            return false;
        }

        SaveGame data;
        try
        {
            data = JsonSerializer.Deserialize<SaveGame>(file.GetAsText());
        }
        catch (JsonException e)
        {
            GD.PushError($"[Save] Corrupt save file: {e.Message}");
            EventBus.Instance?.EmitNotify("Save file is unreadable.");
            return false;
        }

        if (data == null) return false;

        // Restore state before navigating, so the room renders against the loaded world.
        WorldStateManager.Instance?.ImportState(data.Flags, data.ZonePhases);
        EntityStateManager.Instance?.ImportStates(data.EntityStates);
        InventoryCraftingManager.Instance?.ImportStacks(data.Inventory);
        SanityManager.Instance?.SetTo(data.Sanity);
        RealmTransitionManager.Instance?.SetRealm((GameRealm)data.CurrentRealm, force: true);

        if (!string.IsNullOrEmpty(data.CurrentRoomId))
            NavigationManager.Instance?.GoToRoom(data.CurrentRoomId);

        GD.Print($"[Save] Loaded {path}");
        return true;
    }

    public static void DeleteSave(string path = GameConfig.SaveFilePath)
    {
        if (!FileAccess.FileExists(path)) return;

        using var dir = DirAccess.Open("user://");
        dir?.Remove(path);
    }
}
