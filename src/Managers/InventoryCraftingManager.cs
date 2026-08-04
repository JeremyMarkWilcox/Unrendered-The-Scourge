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

    /// <summary>
    /// What is worn, by slot — <b>the state that made <c>EquipSlot</c> mean something</b>, added by unit
    /// 3a. Two slots, one weapon and one armour (`combat.md` §18), held as a dictionary rather than two
    /// named fields so that deferring a shield is genuinely additive: the enum grows a member and
    /// nothing here is re-authored, which is the property §18 leaned on when it deferred the shield as a
    /// verb.
    /// </summary>
    /// <remarks>
    /// <b>An item can be worn and still sit in the satchel.</b> Equipping moves nothing — it names a
    /// stack you already have. That is deliberate: <see cref="SurvivesDeath"/> keeps Equipment either
    /// way (§19), so moving it out of the satchel would create a second place a weapon can be and a
    /// second way for the two to disagree, in exchange for nothing. The one thing it costs is that
    /// dropping is not a mechanic — and there is no drop verb in the game.
    /// </remarks>
    private readonly Dictionary<EquipSlot, string> _equipped = new();

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
    /// Every registered item. Sibling of <see cref="Recipes"/>, and it exists so the gates can sweep
    /// the authored catalogue for combat fields that are set but inert — the same job
    /// <c>GateAuthoredContent</c> already does for hotspots.
    /// </summary>
    public IReadOnlyDictionary<string, ItemData> Catalogue => _catalogue;

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
    /// Components and catalysts are farmable and craftable, so losing those is pure cost with no
    /// permanence — which is exactly the sting the penalty is for.
    ///
    /// <para>
    /// ~~<b>Traps and equipment are destroyed too</b> (docs/combat.md §18, 2026-07-30). Both are
    /// craftable, so the rule above answers them without an exception; recrafting is what keeps the
    /// trade relevant instead of one-and-done.~~ **Both halves reversed by §19 on 2026-07-31 and built
    /// 2026-08-03 (unit 4a)** — *"Equipment survives death — reversing §18"* and *"Traps are permanent —
    /// reversing §13, and reversing this section's own answer."*
    /// </para>
    ///
    /// <para>
    /// <b>The rule at the top did not change; what changed is the answer to "can you remake it".</b> A
    /// trap is now crafted once and owned forever, so there is nothing to remake and destroying it would
    /// delete a permanent thing — the same argument that always protected <c>Lore</c>. Equipment is the
    /// same shape: §19 made the kit a startup cost paid in dormant commons (`items.md` §1), and burning
    /// the coat on death would turn that one-time cost into a repeating tax on failure. What still
    /// carries the sting is the two live commons and her salves, which is where §19 wanted the pressure.
    /// </para>
    ///
    /// <para>
    /// <b>The gate over this moved rather than being deleted.</b> A check asserting equipment burns was
    /// the old rule's probe, but the invariant it was really protecting is the one in the remark below —
    /// *a new category inherits the destructive default*. So it now stands on <c>Component</c> and
    /// <c>Catalyst</c>, which still burn. Deleting it outright would have left that claim ungated
    /// (`deleting-a-gated-mechanism`: keep the invariant, move the probe).
    /// </para>
    ///
    /// <para>
    /// Keyed on category rather than a per-item flag on purpose: a new item cannot forget to declare
    /// itself, and a new *category* inherits the destructive default, which is the safe direction to
    /// fail in. Adding a category that must survive means adding an arm here — and the gates in
    /// <c>SliceVerifier</c> assert both sides so the choice is never silent.
    /// </para>
    /// </summary>
    public static bool SurvivesDeath(ItemCategory category) => category switch
    {
        ItemCategory.Salve => true,
        ItemCategory.Lore => true,
        ItemCategory.Relic => true,
        ItemCategory.Equipment => true, // §19, reversing §18
        ItemCategory.Trap => true,      // §19, reversing §13
        _ => false,
    };

    /// <summary>
    /// Whether an item takes up room you have to manage. **False for
    /// <see cref="ItemCategory.Component"/> and for the key items** — everything else, true.
    /// </summary>
    /// <remarks>
    /// <b>Materials are a tally, not an inventory</b> (`combat.md` §19). Eight named components exist in
    /// the whole game and they read as a strip of counters, never a bag that is sorted or arbitrated:
    /// gathering is meant to be pure gain, so a harvest can never be refused and a pickup can never be
    /// wasted (`items.md` §1: *"There is no cap, so you are never maxed"*).
    /// <para>
    /// <b>The key items are exempt for the opposite reason.</b> A blueprint page and a crossing relic are
    /// progress rather than supplies — the only copy of something — so charging a slot for one would make
    /// carrying your own progress a cost, and in the worst case would let a full satchel refuse a pickup
    /// that cannot be re-found. Both directions of the exemption end at the same rule: **nothing
    /// unmanageable and nothing irreplaceable occupies a slot.**
    /// </para>
    /// <para>
    /// <b>Nothing enforces this yet, and that is the intended order.</b> There is no slot cap — see
    /// <see cref="GameConfig.SalveSlotsPerFight"/> for the per-fight caps, which are a different
    /// mechanism entirely. This lands with the shapes because it is what unit 4b's twenty items are
    /// authored against; a cap is a number to turn later, over a predicate that already exists.
    /// </para>
    /// <para>
    /// Deliberately <i>not</i> the negation of <see cref="SurvivesDeath"/>, though four of seven
    /// categories happen to agree. The two ask unrelated questions — *does losing this hurt* versus *is
    /// this bookkeeping* — and Salve is where they part: it survives death and does occupy a slot. A gate
    /// asserts they disagree somewhere, so collapsing one into the other cannot pass quietly.
    /// </para>
    /// </remarks>
    public static bool CountsAgainstSlots(ItemCategory category) => category switch
    {
        ItemCategory.Component => false,
        ItemCategory.Lore => false,
        ItemCategory.Relic => false,
        _ => true,
    };

    /// <summary>
    /// Put <see cref="GameConfig.StartingItems"/> in the satchel — the things he owned before any of
    /// this started. <b>Idempotent</b>: an item already held is left alone, so calling this twice
    /// cannot hand out a second axe.
    /// </summary>
    /// <remarks>
    /// Idempotent rather than guarded by a "has begun" flag, because the flag would be a second piece of
    /// state able to disagree with the satchel — and it would disagree on exactly the path that matters.
    /// <see cref="ImportStacks"/> clears before it restores, so a load replaces the satchel wholesale;
    /// a boot-then-load sequence therefore ends with the file's contents and no phantom axe, whichever
    /// order they happen in. The <see cref="Has(string,int)"/> test asks the satchel itself, which is the
    /// only authority that cannot drift.
    /// <para>
    /// Deliberately not called from <c>_Ready</c>. The gates reset the satchel constantly, so a grant that
    /// happened once at boot would be invisible to them and untested; calling it from <c>Main</c> where the
    /// game begins means the check can call it too and watch it work.
    /// </para>
    /// </remarks>
    public void GrantStartingKit()
    {
        var granted = false;

        foreach (var id in GameConfig.StartingItems)
        {
            if (string.IsNullOrEmpty(id) || Has(id)) continue;
            if (GetItem(id) == null)
            {
                GD.PushError($"[Inventory] Starting kit names '{id}', which is not in the catalogue.");
                continue;
            }

            _stacks[id] = _stacks.GetValueOrDefault(id) + 1;
            granted = true;
        }

        if (granted) EventBus.Instance?.EmitInventoryChanged();
    }

    /// <summary>
    /// Empty the satchel of everything death takes. One <c>InventoryChanged</c> at the end rather
    /// than one per stack, so the grid rebuilds once instead of once per loss.
    /// </summary>
    public void LoseUnprotectedItems()
    {
        var doomed = _stacks.Keys
            .Where(id => !SurvivesDeath(GetItem(id)?.Category ?? ItemCategory.Component))
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
    /// Whether a room holds a given station — the exact shape of <see cref="CampManager.IsCamp"/>, and
    /// static for the same reason: the gates and the authoring guard must be able to ask about a room
    /// the player is not standing in.
    /// </summary>
    /// <remarks>
    /// <b>A <see cref="HotspotKind.Workbench"/> counts when it carries the id</b>, which is how a camp
    /// bench declares the stations it also is. That is separate from the bench satisfying *every*
    /// station in <see cref="AtVenueFor"/> — this predicate answers "is the mortar here", and a bench
    /// authored as the mortar answers yes.
    /// <para>
    /// <b>Gating is not consulted, matching <c>IsCamp</c> deliberately.</b> A station behind an unmet
    /// <see cref="Data.HotspotData.RequiredFlag"/> would still count, exactly as a gated workbench
    /// already makes a room a camp. Re-deriving hotspot visibility here would put a second copy of that
    /// rule where it can disagree with the one <c>RoomView</c> draws from, and the disagreement would be
    /// invisible: the station you cannot see is the station you cannot click. If a gated station is ever
    /// authored, fix it by asking the one visibility authority, not by copying it.
    /// </para>
    /// </remarks>
    public static bool RoomHasStation(RoomData room, string stationId) =>
        !string.IsNullOrEmpty(stationId) &&
        room?.Hotspots is not null &&
        room.Hotspots.Any(h => h != null && h.StationId == stationId &&
                               h.Kind is HotspotKind.Station or HotspotKind.Workbench);

    /// <summary>
    /// Whether you are somewhere this recipe may be made. **The camp bench satisfies every station; a
    /// field station satisfies only its own** (`combat.md` §19).
    /// </summary>
    /// <remarks>
    /// <b>A blank <see cref="RecipeData.RequiresStation"/> therefore means the bench, not the open
    /// road.</b> Worth being exact, because "blank = anywhere" is the reading that first suggests itself
    /// and it would silently repeal §17 — every recipe authored before stations existed has a blank
    /// field, so that reading would make all of them craftable in a ditch and quietly delete the
    /// carry-from-camp decision. Blank means *unspecialised*, and the unspecialised venue is the bench.
    /// <para>
    /// The Dream guard is load-bearing on the field-station arm only: <see cref="CampManager.AtCamp"/>
    /// already refuses inside a run, so without this a mortar standing in a Waking room would keep
    /// working while the player was in the Dream on top of it (invariant 2).
    /// </para>
    /// </remarks>
    public bool AtVenueFor(RecipeData recipe)
    {
        if (recipe == null) return false;
        if (DreamRunManager.Instance is { IsRunning: true }) return false;

        if (CampManager.Instance?.AtCamp == true) return true;
        if (string.IsNullOrEmpty(recipe.RequiresStation)) return false;

        return RoomHasStation(NavigationManager.Instance?.CurrentRoom, recipe.RequiresStation);
    }

    /// <summary>
    /// Whether this recipe has already produced its one permanent result. True for a trap you own and
    /// for a world repair already done; **always false for anything stackable**, which is every salve,
    /// component and catalyst.
    /// </summary>
    /// <remarks>
    /// Two questions rather than one because the two permanences are stored differently and neither can
    /// stand in for the other: a trap is an item you hold (§19 made traps permanent, so the count is 0
    /// or 1 and never more), a repair is a flag on the world. Keeping them in one predicate is what lets
    /// <see cref="Inspect"/> carry a single arm, so the bench can never spam either one.
    /// <para>
    /// <b>The flag arm is any recipe that sets one, not just the flag-only ones</b>, and the wider test
    /// is the correct one rather than the convenient one. A recipe with a
    /// <see cref="RecipeData.SetsFlag"/> changes the world, and the world only changes the first time —
    /// so the three tools land here too, which is what stops a second Bill Hook from being forged for
    /// materials that buy nothing. Restricting this to <see cref="RecipeData.RepairsWorld"/> would have
    /// covered the three major repairs and quietly left the tools spammable.
    /// </para>
    /// <para>
    /// Equipment is deliberately <i>not</i> here. A second coat is a legitimate thing to want — the
    /// cache exists partly so a spare can be banked (`combat.md` §18) — and §19's reversal made it
    /// survive death rather than made it unique.
    /// </para>
    /// </remarks>
    public bool AlreadyMade(RecipeData recipe)
    {
        if (recipe == null) return false;

        if (!string.IsNullOrEmpty(recipe.SetsFlag) &&
            WorldStateManager.Instance?.GetFlag(recipe.SetsFlag) == true)
            return true;

        return GetItem(recipe.OutputItemId)?.Category == ItemCategory.Trap &&
               Has(recipe.OutputItemId);
    }

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

        // A recipe must make *something*. Until 2026-08-03 that meant an item; §19 added the recipe
        // that outputs only a world flag (`RecipeData.RepairsWorld`), so the test is now "neither"
        // rather than "no item". Still `NoSuchRecipe`, because a recipe with neither is a content
        // error and not a state the player can be in.
        if (string.IsNullOrEmpty(recipe.OutputItemId) && !recipe.RepairsWorld)
            return CraftBlock.NoSuchRecipe;

        // Already done, before the venue and the materials. Sending someone to a bench for a thing
        // they already own — or a bridge they already repaired — would be a worse refusal than
        // saying so, and it would let them spend the materials twice.
        if (AlreadyMade(recipe)) return CraftBlock.AlreadyMade;

        // The venue, after knowledge and before materials. Deliberately *after* the blueprint check
        // so an unlearned recipe still refuses on ignorance and does not reveal itself by
        // complaining about where you are standing instead.
        if (!AtVenueFor(recipe)) return CraftBlock.NotAtStation;

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
        if (!string.IsNullOrEmpty(recipe.OutputItemId))
        {
            AddItem(recipe.OutputItemId, amount);
            EventBus.Instance?.EmitItemCrafted(recipe.OutputItemId, amount);
        }

        // The flag last, and after the item rather than instead of it. Most recipes that set one also
        // hand you the thing — crafting the Bill Hook gives you a Bill Hook *and* opens the bramble —
        // and only the three major repairs (`items.md` §6) set a flag alone. One line covers both
        // because `SetsFlag` is orthogonal to the output, which is the whole reason it lives on the
        // recipe and not on the item.
        if (!string.IsNullOrEmpty(recipe.SetsFlag))
            WorldStateManager.Instance?.SetFlag(recipe.SetsFlag);

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

    // ---- Wearing things --------------------------------------------------

    /// <summary>What is in a slot, or "" when the slot is empty.</summary>
    public string EquippedIn(EquipSlot slot) =>
        slot != EquipSlot.None && _equipped.TryGetValue(slot, out var id) ? id : "";

    /// <summary>The worn weapon, or <c>null</c> when bare-handed — which is a legal way to fight.</summary>
    /// <remarks>
    /// Returns the <see cref="ItemData"/> rather than the id because both callers want the fields on it
    /// (<see cref="ItemData.Deals"/> and <see cref="ItemData.Potency"/>), and because it folds the one
    /// failure worth folding: an id naming something no longer in the catalogue reads as unarmed rather
    /// than as a crash.
    /// </remarks>
    public ItemData EquippedWeapon => GetItem(EquippedIn(EquipSlot.Weapon));

    /// <summary>The worn armour, or <c>null</c> when unprotected. See <see cref="EquippedWeapon"/>.</summary>
    public ItemData EquippedArmour => GetItem(EquippedIn(EquipSlot.Armour));

    /// <summary>
    /// Why <see cref="Equip"/> would refuse, without changing anything. Same split as
    /// <see cref="Inspect"/> and <see cref="InspectUse"/>, and for the same reason.
    /// </summary>
    public EquipBlock InspectEquip(string itemId)
    {
        var item = GetItem(itemId);
        if (item == null) return EquipBlock.NoSuchItem;
        if (!Has(itemId)) return EquipBlock.NotHeld;

        // Both halves of the test, because either one alone lets something through. Category alone would
        // accept an Equipment item whose Slot was never set; Slot alone would accept a trap someone gave
        // a slot to by accident. The roster gates assert the two agree on every authored file, so this
        // pair can only fire on content that is already failing a check.
        if (item.Category != ItemCategory.Equipment || item.Slot == EquipSlot.None)
            return EquipBlock.NotEquipment;

        return EquippedIn(item.Slot) == itemId ? EquipBlock.AlreadyWorn : EquipBlock.None;
    }

    /// <summary>Whether equipping this right now would change anything. See <see cref="InspectEquip"/>.</summary>
    public bool CanEquip(string itemId) => InspectEquip(itemId) == EquipBlock.None;

    /// <summary>
    /// Wear something. <b>Exclusive per slot</b> — a second coat replaces the first rather than stacking,
    /// which is §18's *"one worn item, not a set"* stated as code instead of as a rule the player has to
    /// be told.
    ///
    /// Nothing is consumed and nothing leaves the satchel; see <see cref="_equipped"/>.
    /// </summary>
    public bool Equip(string itemId)
    {
        switch (InspectEquip(itemId))
        {
            case EquipBlock.NoSuchItem:
            case EquipBlock.NotHeld:
                return false;
            case EquipBlock.NotEquipment:
                EventBus.Instance?.EmitNotify("Not something you can wear or carry into a fight.");
                return false;
            case EquipBlock.AlreadyWorn:
                return false;
        }

        var item = GetItem(itemId);
        if (item == null) return false;

        var displaced = EquippedIn(item.Slot);
        _equipped[item.Slot] = itemId;

        // The displaced thing gets named, because it is still yours and the satchel will not say so.
        EventBus.Instance?.EmitNotify(string.IsNullOrEmpty(displaced)
            ? $"Equipped: {DisplayNameOf(itemId)}"
            : $"Equipped: {DisplayNameOf(itemId)} — {DisplayNameOf(displaced)} back in the satchel.");

        EventBus.Instance?.EmitInventoryChanged();
        return true;
    }

    /// <summary>
    /// Take a slot back to empty. Always legal: bare hands are a fight with a weak verb rather than no
    /// verb (§18 keeps a baseline attack for exactly that), and no armour is a fight you take in full.
    /// </summary>
    public bool Unequip(EquipSlot slot)
    {
        if (slot == EquipSlot.None || !_equipped.Remove(slot)) return false;

        EventBus.Instance?.EmitInventoryChanged();
        return true;
    }

    // ---- Persistence -----------------------------------------------------

    public Dictionary<string, int> ExportStacks() => new(_stacks);

    /// <summary>
    /// The two worn slots, keyed by the enum's integer so the save file does not depend on a member's
    /// name — the same reason <c>EntityStateManager.ExportStates</c> writes ints.
    /// </summary>
    public Dictionary<string, int> ExportEquipped()
    {
        var worn = new Dictionary<string, int>();
        foreach (var kv in _equipped)
            if (!string.IsNullOrEmpty(kv.Value)) worn[kv.Value] = (int)kv.Key;

        return worn;
    }

    /// <summary>
    /// Restore what was worn. Silent, like <see cref="ImportDiscovered"/>: a load restores a loadout, it
    /// does not re-equip it, so no notify line announces a coat you put on hours ago.
    /// </summary>
    /// <remarks>
    /// <b>Re-validated on the way in rather than trusted.</b> An id absent from the catalogue, or one
    /// whose slot no longer matches what the save recorded, is dropped — so a save written before an
    /// item was renamed loads as unarmed instead of as a slot holding a ghost. Order matters: this must
    /// run after <see cref="ImportStacks"/>, since a worn item is one you hold.
    /// </remarks>
    public void ImportEquipped(Dictionary<string, int> worn)
    {
        _equipped.Clear();
        if (worn == null) return;

        foreach (var kv in worn)
        {
            var item = GetItem(kv.Key);
            if (item == null || item.Slot == EquipSlot.None) continue;
            if ((int)item.Slot != kv.Value) continue;

            // The same three questions InspectEquip asks, in the same order, plus this one — which is
            // what makes the ordering note above a rule rather than a hope. It cannot fail on a save this
            // build wrote (Equipment is kept by SurvivesDeath, so a worn item is always held), and that
            // is the point: the check exists for saves written by builds that reasoned differently.
            if (!Has(kv.Key)) continue;

            _equipped[item.Slot] = kv.Key;
        }
    }

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
        _equipped.Clear();
        EventBus.Instance?.EmitInventoryChanged();
    }
}
