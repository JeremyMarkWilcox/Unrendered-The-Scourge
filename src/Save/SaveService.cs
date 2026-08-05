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
            ResolveMax = ResolveManager.Instance?.Max ?? GameConfig.ResolveMax,
            Vitality = VitalityManager.Instance?.Current ?? GameConfig.VitalityMax,
            Flags = WorldStateManager.Instance?.ExportFlags() ?? new(),
            EntityStates = EntityStateManager.Instance?.ExportStates() ?? new(),
            Inventory = InventoryCraftingManager.Instance?.ExportStacks() ?? new(),
            Stash = InventoryCraftingManager.Instance?.ExportStash() ?? new(),
            DiscoveredBlueprints = InventoryCraftingManager.Instance?.ExportDiscovered() ?? new(),
            ReturnCampRoomId = CampManager.Instance?.ReturnCampRoomId ?? "",
            Wounds = CombatManager.Instance?.ExportWounds() ?? new(),
            Equipped = InventoryCraftingManager.Instance?.ExportEquipped() ?? new(),
            Rigs = EncounterManager.Instance?.ExportRigs() ?? new(),
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

        // **A pocket does not survive a load.** It is a 3D scene, so "where you were standing" in one is
        // geometry this format deliberately does not carry — see `SaveGame.ResolveMax`'s remark. Clearing it
        // here means a save taken inside one restores you to the Waking room the file names, entrance intact
        // and nothing granted, so a reload is never a way to bank half a pocket.
        PocketManager.Instance?.ForceClear();

        // Restore state before navigating, so the room renders against the loaded world.
        WorldStateManager.Instance?.ImportState(data.Flags);
        EntityStateManager.Instance?.ImportStates(data.EntityStates);

        // After the states above, not before: the ledger's meaning depends on which bodies are
        // severed, so restoring wounds against a world that still thinks everything is anchored
        // would be a window where the two disagree.
        CombatManager.Instance?.ImportWounds(data.Wounds);

        InventoryCraftingManager.Instance?.ImportStacks(data.Inventory);
        InventoryCraftingManager.Instance?.ImportStash(data.Stash);
        InventoryCraftingManager.Instance?.ImportDiscovered(data.DiscoveredBlueprints);

        // After ImportStacks, and the order is load-bearing rather than tidy: equipping names a stack you
        // already hold, so an import that ran first would be validating worn items against an empty
        // satchel. The same argument as ImportWounds above — restore the thing before the reference to it.
        InventoryCraftingManager.Instance?.ImportEquipped(data.Equipped);
        EncounterManager.Instance?.ImportRigs(data.Rigs);

        // Before the navigation below, so that if the loaded room happens to be a camp, arriving
        // there rebinds a return point that already agrees with the file rather than clobbering it.
        CampManager.Instance?.ImportReturnCamp(data.ReturnCampRoomId);

        // The ceiling before the value, and the order is load-bearing: `SetTo` clamps to `Max`, so restoring
        // a bar of 108 against a ceiling still at 100 would silently trim eight points of permanent progress
        // on every load. Same shape as `ImportEquipped` needing `ImportStacks` first.
        ResolveManager.Instance?.ImportMax(data.ResolveMax);
        ResolveManager.Instance?.SetTo(data.Resolve);
        VitalityManager.Instance?.Import(data.Vitality);

        var roomId = data.CurrentRoomId;
        var realm = (GameRealm)data.CurrentRealm;

        // **A stranding guard stood here and is gone as of unit 6b — unnecessary rather than migrated, and
        // the distinction is worth keeping.** It existed because a Dream room was an ordinary `RoomData` with
        // no door back to the Waking world, so a save taken inside the maze restored a room the player could
        // not leave: the save/load soft lock the whole guard was written for. **A pocket is a 3D scene and
        // never appears in `NavigationManager`**, so `CurrentRoomId` cannot name one and there is nothing
        // left to detect. The class of bug was deleted by the model changing, not fixed here.
        //
        // Its *probe* survives, reworded, in the `pockets` gate section: a save taken inside a pocket loads
        // you outside it with the entrance intact and nothing granted. That is the invariant; this was only
        // one mechanism's way of honouring it. See `deleting-a-gated-mechanism` — keep the invariant, move
        // the probe.

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
