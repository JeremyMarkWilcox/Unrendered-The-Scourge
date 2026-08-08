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
    /// stack you already have. That is deliberate: nothing is lost on death either way (unit 8), so moving
    /// it out of the satchel would create a second place a weapon can be and a
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
    /// the deleted <c>DreamRunManager.DebugSetPursuerRoom</c>. Not for gameplay.
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

    // ---- `SurvivesDeath` stood here, and unit 8 deleted it along with `LoseUnprotectedItems` ----
    //
    // **It was a live game-breaker, not a tidy-up.** The predicate keyed on `ItemCategory`, and
    // `herb_graveflower` and `mat_cairnstone` do not serialise a `Category` at all — so both fell to
    // `Component = 0`, which burned. There are **12 Graveflower and 6 Cairnstone in the whole game**, they
    // never re-arm at any milestone (`placement.md` rule 13), and the exorcism engine costs 8 Graveflower.
    // Dying while carrying them permanently deleted the good ending, silently.
    //
    // **The tell was this file's own gate comment**: *"components are gone, because you can go and get
    // more."* True of Bitterroot and Tallow, false of exactly those two. Category-keying was chosen so a new
    // item could not forget to opt in, and its safe-direction default was wrong for the two items where the
    // stakes were highest — rule 13's softlock in a shape rule 13 never anticipated, because you do not
    // trade the scarce thing away, you just die.
    //
    // Jeremy, 2026-08-04, ruling it out entirely rather than patching the two categories: *"the common
    // resources are still finite so I don't like item lost ever other than consumed of course."* That is the
    // argument the whole mechanism rested on, withdrawn — *"you can go and get more"* was never
    // unconditionally true, because the world's total of every material is bounded.
    //
    // **So: items are never lost, only consumed.** A collapse costs the walk back and nothing else, which is
    // also what makes Vitality and Resolve able to share one path (§6 as revised). What a lost fight still
    // costs is **the salves you chose to pour into it** — spent by using them, not by dying — so invariant 7
    // is untouched, and `combat.md` §17's *"a lost engagement costs you your consumables"* keeps the only
    // half of itself that was ever load-bearing.
    //
    // The invariant this deletion *inverts* rather than removes is gated in `SliceVerifier`: the old claim
    // was *a new category inherits the destructive default*, and the new one is **a collapse takes nothing at
    // all**, asserted against a full satchel with both scarce nouns in it by name.

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
    /// It had a partner — <c>SurvivesDeath</c>, deleted with item loss in unit 8 — and this remark used to
    /// argue that the two were not each other's negation. **The argument is gone with the other predicate, and
    /// this one is unchanged**, which is the point worth keeping: it was never *about* death. It asks *is this
    /// bookkeeping*, and the gate over it now names the two ends the rule separates (a `Component` is a tally
    /// entry, an `Equipment` is a carried thing) rather than comparing it to something that no longer exists.
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

    // ---- Which half of the journal (unit 7a, decision 9) ------------------

    // The two prefixes that say whose hand a page is in. Named once because three things read them now —
    // <see cref="TrackOf"/>, <see cref="KnowsAnyPageOn"/>, and a gate that holds every page to declaring
    // one — and a second spelling of either string is a second thing to keep in agreement for no gain.
    private const string CraftPagePrefix = "lore_craft_";
    private const string PhysickPagePrefix = "lore_physick_";

    /// <summary>
    /// Which hand taught this recipe — the divider the journal is drawn along.
    /// </summary>
    /// <remarks>
    /// <b>Derived, never authored, and that is decision 9 taken at its word.</b> The panel is one journal
    /// with two sides, so *which side* is not new information: it is already written down twice over. The
    /// blueprint id says it out loud (<c>lore_craft_</c> / <c>lore_physick_</c>, and a gate already asserts
    /// every family declares its hand), and the output item carries <see cref="Data.ItemData.Track"/>. A
    /// third authored field would be a third thing to keep in agreement, which is the mistake
    /// <c>RoomData.IsSanctuary</c> was deleted for.
    /// <para>
    /// <b>The blueprint wins where both speak, and a gate holds them to agreeing anyway.</b> The order
    /// matters for exactly one live case and it is the one the item has to answer: Clearmind is the only
    /// recipe in the game with no blueprint — you have always known it — and <c>salve_clearmind</c>
    /// authors <c>Track = 1</c>, so hers is the side it lands on. Every other recipe resolves the same way
    /// from either source, which is what makes the crossing check meaningful rather than decorative.
    /// </para>
    /// <para>
    /// <b>A repair recipe has no output item</b> (<see cref="RecipeData.RepairsWorld"/>), so for the three
    /// major repairs the blueprint prefix is the only answer available — another reason it is consulted
    /// first rather than as a fallback.
    /// </para>
    /// </remarks>
    public KnowledgeTrack TrackOf(RecipeData recipe)
    {
        if (recipe == null) return KnowledgeTrack.Craft;

        if (recipe.RequiredBlueprintId.StartsWith(PhysickPagePrefix)) return KnowledgeTrack.Physick;
        if (recipe.RequiredBlueprintId.StartsWith(CraftPagePrefix)) return KnowledgeTrack.Craft;

        return GetItem(recipe.OutputItemId)?.Track ?? KnowledgeTrack.Craft;
    }

    /// <summary>
    /// Whether this recipe is written on the given side of the journal. <b>The engine is on both</b>, which
    /// is the whole reason this is a predicate rather than a comparison against
    /// <see cref="TrackOf"/>.
    /// </summary>
    /// <remarks>
    /// <b>This is <see cref="RecipeData.RequiresBothTracks"/>'s display reader; <see cref="Inspect"/> is the
    /// one that bites.</b> Worth being exact about, because a field going from no readers to one is where
    /// the eleventh blind-spot class hides: for one unit this method was the only reader, so the engine
    /// appeared on both sides of the book and could be made by a player who knew one of them. ~~That
    /// refusal is unit 7b and is still outstanding.~~ **Landed 2026-08-05 as
    /// <see cref="CraftBlock.HalfKnown"/>.** Appearing on both sides is where the field *reads* correctly;
    /// refusing until both are known is where it *bites*, and the two landing in different units is exactly
    /// the situation that makes a green suite feel like more than it is.
    /// <para>
    /// The two now compose into the state the engine is for: its page is drawn on her side, greyed, while
    /// her side is otherwise blank — which is the only place the game says out loud that you need something
    /// of hers. Nothing leaks before that, because a recipe whose own page is unfound is <c>Unknown</c> and
    /// an unknown page is not in the book at all.
    /// </para>
    /// </remarks>
    public bool BelongsToSide(RecipeData recipe, KnowledgeTrack side) =>
        recipe != null && (recipe.RequiresBothTracks || TrackOf(recipe) == side);

    /// <summary>
    /// Whether anything at all has been learned on the given side of the journal. The question
    /// <see cref="RecipeData.RequiresBothTracks"/> asks of the hand the recipe is not written in.
    /// </summary>
    /// <remarks>
    /// <b>Pages held, not recipes known, and that distinction is the whole rule rather than an
    /// implementation detail.</b> The near-miss is Clearmind: it is hers, it is the one recipe in the game
    /// with no blueprint, and you know it from the first minute. Asking *do you know a recipe on her side*
    /// would therefore be true at game start, the engine would be half-satisfied before the story began,
    /// and the flag would refuse nothing it was added to refuse — a check that cannot fail, wearing the
    /// clothes of one that can. Asking *do you hold a page of hers* is the question `items.md` §6 actually
    /// poses: *"his Craft from a pocket, her Physick from a scattered note."*
    /// <para>
    /// <b>Prefix only, with no fallback to the item's <see cref="Data.ItemData.Track"/>.</b> A fallback would
    /// be unreachable: every page declares its hand in its id and a gate holds them to it, so the second arm
    /// could never run, and an arm that cannot run is a shape with no reader. It also keeps the answer
    /// honest for a set that contains something odd — an id on neither prefix counts for neither side,
    /// rather than defaulting into one the way <see cref="TrackOf"/> has to.
    /// </para>
    /// </remarks>
    public bool KnowsAnyPageOn(KnowledgeTrack side)
    {
        var prefix = side == KnowledgeTrack.Physick ? PhysickPagePrefix : CraftPagePrefix;

        foreach (var id in _discovered)
            if (id.StartsWith(prefix)) return true;

        return false;
    }

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
        if (PocketManager.InsideTheDream) return false;

        if (CampManager.Instance?.AtCamp == true) return true;
        if (string.IsNullOrEmpty(recipe.RequiresStation)) return false;

        return RoomHasStation(NavigationManager.Instance?.CurrentRoom, recipe.RequiresStation);
    }

    /// <summary>
    /// Whether this recipe has already produced its one permanent result. True for a trap or a piece of
    /// equipment you own and for a world repair already done; **always false for anything stackable**,
    /// which is every salve, component and catalyst.
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
    /// <b>Equipment joined the held arm 2026-08-07, on Jeremy's ruling after playing chapter 2:</b>
    /// *"you can create equipment items more than once, which didn't trap me with limited resources but
    /// still isn't helpful — removing it from the list once made is cleaner."* This paragraph used to say
    /// the opposite, and its reason was already dead when it was written: it argued *"a second coat is a
    /// legitimate thing to want — the cache exists partly so a spare can be banked (§18)"*, but §19
    /// **retired that purpose** in the same breath as it made equipment survive death (`combat.md`, *"the
    /// shared cache is retired, not deleted… equipment surviving death removes that purpose"*). With
    /// nothing able to take a coat off you, a second one is a row that can never do anything but spend
    /// materials. Equipment is now permanent on exactly the terms a trap is.
    /// </para>
    /// <para>
    /// <b>The cache is counted, not just the satchel</b>, and that half is a fix rather than a new rule.
    /// <see cref="Has"/> reads <c>_stacks</c> alone, so before today <see cref="Deposit"/> was a way to
    /// launder the trap arm: bank the snare, and the bench offered to forge another. The cache is retired
    /// but it is not deleted and <c>Deposit</c> still works, so the permanence question has to be *do you
    /// own one anywhere* — the only reading that cannot be undone by moving an item between two boxes you
    /// control. Deliberately <i>not</i> folded into <c>Has</c>, which every ingredient check calls and
    /// which must keep meaning *to hand*: materials in the cache are not on the bench.
    /// </para>
    /// </remarks>
    public bool AlreadyMade(RecipeData recipe)
    {
        if (recipe == null) return false;

        if (!string.IsNullOrEmpty(recipe.SetsFlag) &&
            WorldStateManager.Instance?.GetFlag(recipe.SetsFlag) == true)
            return true;

        var made = GetItem(recipe.OutputItemId)?.Category;
        return made is ItemCategory.Trap or ItemCategory.Equipment &&
               GetCount(recipe.OutputItemId) + GetStashCount(recipe.OutputItemId) > 0;
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

        // The other hand, immediately after the one this recipe is written in, because it is the same
        // question and a knowledge block outranks everything a knowledge block outranks. One recipe sets
        // this flag — the exorcism engine — and until 2026-08-05 nothing here read it, so knowing his half
        // was enough to build the thing that needs both.
        //
        // **Both sides are tested rather than "the side this recipe is not on".** The narrower test would
        // need `TrackOf` and would be right today by accident: the engine's own page is his, so the side to
        // interrogate would be hers. Asking for both is symmetric, cannot be wrong if that page ever changes
        // hands, and is what the field says — `RecipeData.RequiresBothTracks` is a bool precisely because
        // *"the interesting state is both, never either"*.
        if (recipe.RequiresBothTracks &&
            (!KnowsAnyPageOn(KnowledgeTrack.Craft) || !KnowsAnyPageOn(KnowledgeTrack.Physick)))
            return CraftBlock.HalfKnown;

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
    /// Plain words for a refusal, or "" for the one that is not a refusal.
    /// </summary>
    /// <remarks>
    /// <b>Public and static on <c>CampManager.Words</c>'s precedent</b>, which took it from
    /// <c>EncounterManager.Words</c>: the panel has to say why a row is dead, and a private copy of these
    /// sentences over there would close the loop today and drift the first time one is reworded. One
    /// vocabulary, two readers.
    /// <para>
    /// <b><see cref="CraftBlock.AlreadyMade"/> is worded as news rather than as a no</b>, because its own
    /// doc-comment instructs the panel to: *"the only CraftBlock that is good news… a row greyed by this
    /// one must not read like a failure."* The enum asked for this sentence before there was anything to
    /// read it.
    /// </para>
    /// <para>
    /// <b><see cref="CraftBlock.NotAtStation"/> names the cure and not just the cause</b>, for the reason
    /// §19 gives: a venue block can never dead-end anyone, since the camp bench satisfies every station and
    /// a camp is reachable from anywhere. A line reading only *"not here"* would be true and would still
    /// leave the player hunting.
    /// </para>
    /// <para>
    /// <b><see cref="CraftBlock.MissingMaterials"/> does not name what is short</b>, and that is a division
    /// of labour rather than an omission: this method sees a <see cref="CraftBlock"/> and nothing else,
    /// while the row already draws every input with a *(have 1)* against the ones you are short of, which
    /// is decision 9's *"Deadfall — needs Scrap Iron"* reading. Naming it here as well would need the
    /// recipe passed in, and would then say it twice.
    /// </para>
    /// <para>
    /// <b><see cref="CraftBlock.HalfKnown"/> does not name which side is blank</b>, for the same reason and
    /// with a better consolation: this method sees a block and not a knowledge set, so it could not say. It
    /// does not need to. The row is drawn on both sides of the book (<see cref="BelongsToSide"/>), so the
    /// player reads that refusal while looking at the empty half — the panel points at the answer by where
    /// it puts the page, which is stronger than a sentence naming it.
    /// </para>
    /// </remarks>
    public static string Words(CraftBlock block) => block switch
    {
        CraftBlock.NoSuchRecipe => "The page is spoiled. Nothing on it can be made.",
        CraftBlock.Unknown => "He does not know how.",
        CraftBlock.HalfKnown => "One side of the book is still blank. This needs both hands.",
        CraftBlock.MissingMaterials => "Not enough to hand.",
        CraftBlock.NotAtStation => "Not here. The bench at camp, or the right station.",
        CraftBlock.AlreadyMade => "Already made.",
        _ => "",
    };

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
    /// <remarks>
    /// <paramref name="asFightTurn"/> is how <see cref="EncounterManager.Treat"/> says *"the fight is
    /// asking"*. Left false, this refuses with <see cref="UseBlock.InFight"/> while a fight is running,
    /// which is the guard that stops the satchel being free unlimited healing — see that member for the
    /// hole it closes.
    /// <para>
    /// <b>The default is the safe direction, and that is the whole reason it is a defaulted parameter
    /// rather than a required one.</b> A new caller that forgets it gets the *refusal* — visible
    /// immediately, one argument to fix — instead of quietly reopening a bypass of the salve cap. Same
    /// argument as <c>HotspotData.RefillsAtMilestone</c> defaulting to false: when a field can be
    /// forgotten, pick the direction whose failure is loud.
    /// </para>
    /// </remarks>
    public UseBlock InspectUse(string itemId, bool asFightTurn = false)
    {
        var item = GetItem(itemId);
        if (item == null) return UseBlock.NoSuchItem;
        if (!Has(itemId)) return UseBlock.NotHeld;

        if (item.Category != ItemCategory.Salve || item.VitalityRestore <= 0f)
            return UseBlock.NotUsable;

        var vitality = VitalityManager.Instance;
        if (vitality == null) return UseBlock.NotUsable;

        // Before the fight guard, so a player at full Vitality mid-fight hears the more useful of two true
        // answers — and one that does not imply the salve would otherwise have worked.
        if (vitality.Current >= vitality.Max) return UseBlock.NothingToTreat;

        if (!asFightTurn && EncounterManager.Instance?.InFight == true) return UseBlock.InFight;

        return UseBlock.None;
    }

    /// <summary>Whether using this item right now would do something. See <see cref="InspectUse"/>.</summary>
    public bool CanUse(string itemId, bool asFightTurn = false) =>
        InspectUse(itemId, asFightTurn) == UseBlock.None;

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
    /// <remarks>
    /// See <see cref="InspectUse"/> for <paramref name="asFightTurn"/>, and for why its default is the
    /// refusing one. Only <see cref="EncounterManager.Treat"/> passes true.
    /// </remarks>
    public bool UseItem(string itemId, bool asFightTurn = false)
    {
        // Each refusal gets its own sentence. "Nothing to treat" is a different thing to learn
        // from "that isn't something you use", and telling the player neither is worse than both.
        switch (InspectUse(itemId, asFightTurn))
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
            // The satchel is not where a fight is fought. `EncounterManager.Treat` is the verb, and it
            // charges a turn and counts against the per-fight slot cap; this path did neither.
            case UseBlock.InFight:
                EventBus.Instance?.EmitNotify("Not from the satchel — not with this on you.");
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
            // build wrote (nothing is ever lost, so a worn item is always still held), and that
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
