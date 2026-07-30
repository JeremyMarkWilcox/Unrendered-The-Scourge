using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// The craftsman's satchel and the apothecary's bench: what you are carrying, what you know how
/// to make, and what happens when you spend something.
///
/// **All of it lives here rather than in a panel**, which is what keeps it testable headlessly —
/// the crafting panel and inventory grid are thin views over these methods, and every rule below
/// is asserted by <c>--verify-slice</c> without a scene tree. Put resolution in a panel script and
/// that stops being true.
/// </summary>
public partial class InventoryCraftingManager : Node
{
    public static InventoryCraftingManager Instance { get; private set; }

    private readonly Dictionary<string, ItemData> _catalogue = new();
    private readonly Dictionary<string, RecipeData> _recipes = new();
    private readonly Dictionary<string, int> _stacks = new();

    /// <summary>
    /// The cache — a Souls box. **One shared store, not one per camp** (`combat.md` §17): the same
    /// contents are reachable from every bench. Per-camp storage was considered and rejected,
    /// because it makes camps non-interchangeable and turns §17's departure decision ("what do I
    /// carry from camp to the site") into a fetch trip, which is the hub backtracking that section
    /// exists to avoid. If chest-to-chest logistics are ever wanted, that is a deliberate change.
    /// </summary>
    private readonly Dictionary<string, int> _stash = new();

    /// <summary>
    /// Blueprints you have learned. Knowledge, not an item — so it survives spending the page you
    /// read it from, and so the journal can be a view of this rather than an owner of it.
    /// </summary>
    private readonly HashSet<string> _discovered = new();

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        LoadCatalogue();
        LoadRecipes();
    }

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

    private void LoadRecipes()
    {
        _recipes.Clear();

        using var dir = DirAccess.Open(GameConfig.RecipeDataDir);
        if (dir == null)
        {
            GD.Print("[Inventory] No recipe directory yet.");
            return;
        }

        dir.ListDirBegin();
        for (var file = dir.GetNext(); file != ""; file = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;

            var name = file.EndsWith(".remap") ? file[..^6] : file;
            if (!name.EndsWith(".tres")) continue;

            var recipe = GD.Load<RecipeData>($"{GameConfig.RecipeDataDir}/{name}");
            if (recipe == null || string.IsNullOrEmpty(recipe.Id)) continue;

            _recipes[recipe.Id] = recipe;
        }
        dir.ListDirEnd();

        GD.Print($"[Inventory] Registered {_recipes.Count} recipe(s).");
    }

    public ItemData GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        return _catalogue.TryGetValue(itemId, out var i) ? i : null;
    }

    public RecipeData GetRecipe(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return null;
        return _recipes.TryGetValue(recipeId, out var r) ? r : null;
    }

    /// <summary>Every registered recipe, for the crafting panel.</summary>
    public IReadOnlyDictionary<string, RecipeData> Recipes => _recipes;

    /// <summary>
    /// Register a recipe built in code. For the gates, which need to exercise rules — the blueprint
    /// gate especially — without inventing authored content to hang them on. Same role as
    /// <see cref="DreamRunManager.DebugSetPursuerRoom"/>. Not for gameplay.
    /// </summary>
    public void DebugRegisterRecipe(RecipeData recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.Id)) return;
        _recipes[recipe.Id] = recipe;
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

        // Picking up a blueprint teaches it. Knowledge is kept separately from the page, so
        // learning survives spending, losing or reading it — see Discover.
        if (GetItem(itemId)?.Category == ItemCategory.Lore) Discover(itemId);

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

    // ---- The cache, and what death takes ---------------------------------

    /// <summary>Everything in the shared cache, for the panel that will show it and for the save.</summary>
    public IReadOnlyDictionary<string, int> Stash => _stash;

    public int GetStashCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return _stash.TryGetValue(itemId, out var n) ? n : 0;
    }

    /// <summary>
    /// Whether a death leaves this kind of thing alone. **The rule is: death destroys what you can
    /// remake and keeps what you cannot** — which is a softlock guard rather than a kindness.
    ///
    /// - <see cref="ItemCategory.Salve"/> — Jeremy's call, and it protects invariant 7 rather than
    ///   undermining it. A salve's scarcity is "how many can I carry into this fight", never "how do
    ///   I get back to full afterwards"; destroying them on death would push players to burn her
    ///   finite, uncraftable work on topping up between fights, which is the boring use §10 is
    ///   trying to prevent.
    /// - <see cref="ItemCategory.Lore"/> — the carrier of a blueprint. Destroying the only copy
    ///   would delete a recipe permanently and silently.
    /// - <see cref="ItemCategory.Relic"/> — a story object, and what opens a Dream crossing.
    ///   Destroying one could strand progression outright.
    ///
    /// Reagents and catalysts are farmable and craftable, so losing those is pure cost with no
    /// permanence — which is exactly the sting the penalty is for.
    /// </summary>
    public static bool SurvivesDeath(ItemCategory category) => category switch
    {
        ItemCategory.Salve => true,
        ItemCategory.Lore => true,
        ItemCategory.Relic => true,
        _ => false,
    };

    /// <summary>
    /// Empty the satchel of everything death takes. One <c>InventoryChanged</c> at the end rather
    /// than one per stack, so the grid rebuilds once instead of once per loss.
    /// </summary>
    public void LoseUnprotectedItems()
    {
        var doomed = _stacks.Keys
            .Where(id => !SurvivesDeath(GetItem(id)?.Category ?? ItemCategory.Reagent))
            .ToList();

        if (doomed.Count == 0) return;

        foreach (var id in doomed) _stacks.Remove(id);

        EventBus.Instance?.EmitInventoryChanged();
    }

    /// <summary>
    /// Why moving <paramref name="amount"/> of something into the cache would be refused. Split from
    /// the action for the same reason as <see cref="Inspect"/> and <see cref="InspectUse"/>: the
    /// button and the act must ask one question.
    /// </summary>
    public StashBlock InspectDeposit(string itemId, int amount = 1)
    {
        if (GetItem(itemId) == null) return StashBlock.NoSuchItem;
        if (CampManager.Instance?.AtCamp != true) return StashBlock.NotAtCamp;
        return Has(itemId, amount) ? StashBlock.None : StashBlock.NotEnough;
    }

    public StashBlock InspectWithdraw(string itemId, int amount = 1)
    {
        if (GetItem(itemId) == null) return StashBlock.NoSuchItem;
        if (CampManager.Instance?.AtCamp != true) return StashBlock.NotAtCamp;
        return GetStashCount(itemId) >= amount ? StashBlock.None : StashBlock.NotEnough;
    }

    public bool Deposit(string itemId, int amount = 1)
    {
        if (amount <= 0 || InspectDeposit(itemId, amount) != StashBlock.None) return false;
        if (!RemoveItem(itemId, amount)) return false;

        _stash[itemId] = GetStashCount(itemId) + amount;
        EventBus.Instance?.EmitInventoryChanged();
        return true;
    }

    public bool Withdraw(string itemId, int amount = 1)
    {
        if (amount <= 0 || InspectWithdraw(itemId, amount) != StashBlock.None) return false;

        var left = GetStashCount(itemId) - amount;
        if (left > 0) _stash[itemId] = left;
        else _stash.Remove(itemId);

        // Via AddItem, so a deposited blueprint page is re-learned on the way out if it somehow was
        // not already known, and so the acquisition event fires exactly as a pickup would.
        AddItem(itemId, amount);
        return true;
    }

    // ---- Knowledge -------------------------------------------------------

    public bool IsDiscovered(string blueprintId) =>
        !string.IsNullOrEmpty(blueprintId) && _discovered.Contains(blueprintId);

    /// <summary>
    /// Learn a blueprint. Idempotent, and only announces the first time so the journal does not
    /// flash for something you already knew.
    /// </summary>
    public bool Discover(string blueprintId)
    {
        if (string.IsNullOrEmpty(blueprintId)) return false;
        if (!_discovered.Add(blueprintId)) return false;

        EventBus.Instance?.EmitBlueprintDiscovered(blueprintId);
        return true;
    }

    /// <summary>Everything known, for the journal and the save.</summary>
    public IReadOnlyCollection<string> DiscoveredBlueprints => _discovered;

    // ---- Crafting --------------------------------------------------------

    /// <summary>
    /// Why a recipe cannot be made right now, or <see cref="CraftBlock.None"/>. Separate from
    /// <see cref="Craft"/> so the panel can grey a row and say *why* without attempting it.
    /// </summary>
    public CraftBlock Inspect(string recipeId)
    {
        var recipe = GetRecipe(recipeId);
        if (recipe == null) return CraftBlock.NoSuchRecipe;

        // Knowledge first: not knowing how is a different sentence from lacking the materials,
        // and an unknown recipe should not advertise its inputs.
        if (recipe.NeedsBlueprint && !IsDiscovered(recipe.RequiredBlueprintId))
            return CraftBlock.Unknown;

        if (string.IsNullOrEmpty(recipe.OutputItemId)) return CraftBlock.NoSuchRecipe;

        // The bench, after knowledge and before materials. Crafting happens at camps and nowhere
        // else (`combat.md` §17) — it is what keeps "what do I carry from camp to the site" a real
        // decision. Deliberately *after* the blueprint check so an unlearned recipe still refuses on
        // ignorance and does not reveal itself by complaining about the venue instead.
        if (CampManager.Instance?.AtCamp != true) return CraftBlock.NotAtCamp;

        foreach (var input in recipe.Inputs)
        {
            if (input == null || string.IsNullOrEmpty(input.ItemId)) continue;
            if (!Has(input.ItemId, input.Amount)) return CraftBlock.MissingMaterials;
        }

        return CraftBlock.None;
    }

    public bool CanCraft(string recipeId) => Inspect(recipeId) == CraftBlock.None;

    /// <summary>
    /// Spend the inputs, gain the output. Returns false without touching anything when refused —
    /// a half-consumed craft would be worse than no craft at all, which is why every input is
    /// checked by <see cref="Inspect"/> before the first one is removed.
    /// </summary>
    public bool Craft(string recipeId)
    {
        if (!CanCraft(recipeId)) return false;

        var recipe = GetRecipe(recipeId);

        foreach (var input in recipe.Inputs)
        {
            if (input == null || string.IsNullOrEmpty(input.ItemId)) continue;
            RemoveItem(input.ItemId, input.Amount);
        }

        var amount = Mathf.Max(1, recipe.OutputAmount);
        AddItem(recipe.OutputItemId, amount);

        EventBus.Instance?.EmitItemCrafted(recipe.OutputItemId, amount);
        return true;
    }

    // ---- Using things ----------------------------------------------------

    /// <summary>
    /// Why <see cref="UseItem"/> would refuse, without spending anything. The single source of
    /// truth for that question: <c>UseItem</c> switches on this rather than re-testing the
    /// conditions, so the Use button in the panel and the act of using can never disagree.
    /// </summary>
    public UseBlock InspectUse(string itemId)
    {
        var item = GetItem(itemId);
        if (item == null) return UseBlock.NoSuchItem;
        if (!Has(itemId)) return UseBlock.NotHeld;

        if (item.Category != ItemCategory.Salve || item.VitalityRestore <= 0f)
            return UseBlock.NotUsable;

        var vitality = VitalityManager.Instance;
        if (vitality == null) return UseBlock.NotUsable;

        return vitality.Current < vitality.Max ? UseBlock.None : UseBlock.NothingToTreat;
    }

    /// <summary>Whether using this item right now would do something. See <see cref="InspectUse"/>.</summary>
    public bool CanUse(string itemId) => InspectUse(itemId) == UseBlock.None;

    /// <summary>
    /// Spend one of an item for its effect. Returns false when the item does nothing, is not held,
    /// or would be wasted.
    ///
    /// **Salves restore Vitality and never Resolve** (`combat.md` §6, and see
    /// <see cref="ItemData.VitalityRestore"/>). Resolve comes back by leaving the Dream or resting
    /// at a camp, and nothing else — a consumable that steadied your nerve would be one step from
    /// substituting for severing, which is invariant 11.
    ///
    /// Catalysts are deliberately not usable here. They are a Dream loadout spent by crossing, not
    /// something you click in a satchel.
    /// </summary>
    public bool UseItem(string itemId)
    {
        // Each refusal gets its own sentence. "Nothing to treat" is a different thing to learn
        // from "that isn't something you use", and telling the player neither is worse than both.
        switch (InspectUse(itemId))
        {
            case UseBlock.NoSuchItem:
            case UseBlock.NotHeld:
                return false;
            case UseBlock.NotUsable:
                EventBus.Instance?.EmitNotify("Nothing to be done with that.");
                return false;
            // Refuse rather than waste her work: finite and uncraftable at the good end of the
            // scale (invariant 7), so silently burning one for nothing is a real loss.
            case UseBlock.NothingToTreat:
                EventBus.Instance?.EmitNotify("Nothing to treat. Keep it.");
                return false;
        }

        var item = GetItem(itemId);
        var vitality = VitalityManager.Instance;
        if (item == null || vitality == null) return false;

        if (!RemoveItem(itemId)) return false;

        vitality.Restore(item.VitalityRestore);
        EventBus.Instance?.EmitItemUsed(itemId);
        EventBus.Instance?.EmitNotify($"Used: {DisplayNameOf(itemId)}");
        return true;
    }

    // ---- Persistence -----------------------------------------------------

    public Dictionary<string, int> ExportStacks() => new(_stacks);

    public Dictionary<string, int> ExportStash() => new(_stash);

    /// <summary>Silent, like <see cref="ImportStacks"/>'s sibling for knowledge: a load is not a pickup.</summary>
    public void ImportStash(Dictionary<string, int> stash)
    {
        _stash.Clear();
        if (stash == null) return;

        foreach (var kv in stash)
            if (!string.IsNullOrEmpty(kv.Key) && kv.Value > 0) _stash[kv.Key] = kv.Value;
    }

    public void ImportStacks(Dictionary<string, int> stacks)
    {
        _stacks.Clear();
        if (stacks != null)
            foreach (var kv in stacks) _stacks[kv.Key] = kv.Value;

        EventBus.Instance?.EmitInventoryChanged();
    }

    public List<string> ExportDiscovered() => new(_discovered);

    /// <summary>
    /// Restore known blueprints. Deliberately silent — a load restores knowledge, it does not
    /// re-teach it, so no <c>BlueprintDiscovered</c> fires and the journal will not announce
    /// twenty things you learned hours ago.
    /// </summary>
    public void ImportDiscovered(List<string> blueprints)
    {
        _discovered.Clear();
        if (blueprints == null) return;

        foreach (var id in blueprints)
            if (!string.IsNullOrEmpty(id)) _discovered.Add(id);
    }

    public void ResetAll()
    {
        _stacks.Clear();
        _stash.Clear();
        _discovered.Clear();
        EventBus.Instance?.EmitInventoryChanged();
    }
}
