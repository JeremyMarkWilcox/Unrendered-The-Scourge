using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// The craftsman's satchel and the apothecary's bench. For M0-M3 this is stacks and a
/// registry; recipes arrive with the crafting panel in M4.
/// </summary>
public partial class InventoryCraftingManager : Node
{
    public static InventoryCraftingManager Instance { get; private set; }

    private readonly Dictionary<string, ItemData> _catalogue = new();
    private readonly Dictionary<string, int> _stacks = new();

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready() => LoadCatalogue();

    // ---- Catalogue -------------------------------------------------------

    private void LoadCatalogue()
    {
        _catalogue.Clear();

        using var dir = DirAccess.Open(GameConfig.ItemDataDir);
        if (dir == null)
        {
            GD.PushWarning($"[Inventory] No item directory at {GameConfig.ItemDataDir}");
            return;
        }

        dir.ListDirBegin();
        for (var file = dir.GetNext(); file != ""; file = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;

            var name = file.EndsWith(".remap") ? file[..^6] : file;
            if (!name.EndsWith(".tres")) continue;

            var item = GD.Load<ItemData>($"{GameConfig.ItemDataDir}/{name}");
            if (item == null || string.IsNullOrEmpty(item.Id)) continue;

            _catalogue[item.Id] = item;
        }
        dir.ListDirEnd();

        GD.Print($"[Inventory] Registered {_catalogue.Count} item(s).");
    }

    public ItemData GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        return _catalogue.TryGetValue(itemId, out var i) ? i : null;
    }

    public string DisplayNameOf(string itemId)
    {
        var item = GetItem(itemId);
        return item != null && !string.IsNullOrEmpty(item.DisplayName) ? item.DisplayName : itemId;
    }

    // ---- Stacks ----------------------------------------------------------

    public int GetCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return _stacks.TryGetValue(itemId, out var n) ? n : 0;
    }

    public bool Has(string itemId, int amount = 1) => GetCount(itemId) >= amount;

    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;

        _stacks[itemId] = GetCount(itemId) + amount;

        EventBus.Instance?.EmitItemAcquired(itemId, amount);
        EventBus.Instance?.EmitInventoryChanged();
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (!Has(itemId, amount)) return false;

        var left = GetCount(itemId) - amount;
        if (left > 0) _stacks[itemId] = left;
        else _stacks.Remove(itemId);

        EventBus.Instance?.EmitInventoryChanged();
        return true;
    }

    /// <summary>Every held stack, for the grid UI and the debug panel.</summary>
    public IReadOnlyDictionary<string, int> Stacks => _stacks;

    // ---- Persistence -----------------------------------------------------

    public Dictionary<string, int> ExportStacks() => new(_stacks);

    public void ImportStacks(Dictionary<string, int> stacks)
    {
        _stacks.Clear();
        if (stacks != null)
            foreach (var kv in stacks) _stacks[kv.Key] = kv.Value;

        EventBus.Instance?.EmitInventoryChanged();
    }

    public void ResetAll()
    {
        _stacks.Clear();
        EventBus.Instance?.EmitInventoryChanged();
    }
}
