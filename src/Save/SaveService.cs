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
            Resolve = ResolveManager.Instance?.Current ?? GameConfig.ResolveMax,
            Vitality = VitalityManager.Instance?.Current ?? GameConfig.VitalityMax,
            Flags = WorldStateManager.Instance?.ExportFlags() ?? new(),
            ZonePhases = WorldStateManager.Instance?.ExportZonePhases() ?? new(),
            EntityStates = EntityStateManager.Instance?.ExportStates() ?? new(),
            Inventory = InventoryCraftingManager.Instance?.ExportStacks() ?? new(),
            Stash = InventoryCraftingManager.Instance?.ExportStash() ?? new(),
            DiscoveredBlueprints = InventoryCraftingManager.Instance?.ExportDiscovered() ?? new(),
            ReturnCampRoomId = CampManager.Instance?.ReturnCampRoomId ?? "",
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

        // A heist does not survive a load: whatever run was live belonged to the session we
        // are leaving behind, and its pursuer and clock have no meaning in this one.
        DreamRunManager.Instance?.ForceClear();

        // Restore state before navigating, so the room renders against the loaded world.
        WorldStateManager.Instance?.ImportState(data.Flags, data.ZonePhases);
        EntityStateManager.Instance?.ImportStates(data.EntityStates);
        InventoryCraftingManager.Instance?.ImportStacks(data.Inventory);
        InventoryCraftingManager.Instance?.ImportStash(data.Stash);
        InventoryCraftingManager.Instance?.ImportDiscovered(data.DiscoveredBlueprints);

        // Before the navigation below, so that if the loaded room happens to be a camp, arriving
        // there rebinds a return point that already agrees with the file rather than clobbering it.
        CampManager.Instance?.ImportReturnCamp(data.ReturnCampRoomId);
        ResolveManager.Instance?.SetTo(data.Resolve);
        VitalityManager.Instance?.Import(data.Vitality);

        var roomId = data.CurrentRoomId;
        var realm = (GameRealm)data.CurrentRealm;

        // A save taken mid-heist has no run left to carry it out, and no Dream room has a door
        // to the Waking world — the ejection is the only exit. Land outside instead of inside a
        // maze that cannot be left.
        var stranded = DreamRunManager.Instance?.HeistOwningRoom(roomId);
        if (stranded != null)
        {
            roomId = string.IsNullOrEmpty(stranded.ReturnRoomId)
                ? GameConfig.StartingRoomId
                : stranded.ReturnRoomId;
            realm = GameRealm.Waking;

            EventBus.Instance?.EmitNotify(
                "The way behind you closed while you slept. You are in the cold again, with nothing to show for it.");
        }

        RealmTransitionManager.Instance?.SetRealm(realm, force: true);

        if (!string.IsNullOrEmpty(roomId))
            NavigationManager.Instance?.GoToRoom(roomId);

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
