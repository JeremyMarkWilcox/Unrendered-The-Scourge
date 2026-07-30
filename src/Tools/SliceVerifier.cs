using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;
using Unrendered.Save;
using Unrendered.UI;
using Unrendered.World;

namespace Unrendered.Tools;

/// <summary>
/// Drives the vertical slice end to end with no mouse and no eyes, and asserts the three
/// milestone gates from the build plan. Runs against the live scene tree, so RoomView is
/// present and reacting to every signal — a view that throws on a state change fails the
/// run rather than being quietly missed.
///
/// Run with: <c>godot --headless --path . -- --verify-slice</c>
/// Exit code 0 means every gate passed.
/// </summary>
public static class SliceVerifier
{
    private const string Rat = "waystation.rat_01";

    private static readonly List<string> Failures = new();
    private static int _checks;

    // The live view and HUD, so a gate can assert what is on screen and not merely what the
    // managers hold. Null-tolerant: every check that uses them says so.
    private static RoomView _view;
    private static Hud _hud;

    public static int Run(RoomView view = null, Hud hud = null)
    {
        Failures.Clear();
        _checks = 0;
        _view = view;
        _hud = hud;

        // Deterministic: no ambient drain racing the assertions.
        if (ResolveManager.Instance != null) ResolveManager.Instance.Active = false;

        RegisterVerifyRooms();

        GateHarness();
        GateM1_TwoScreenNavigation();
        GateM2_StateRealmTable();
        GateM3_MicroLoop();
        GateVitalityPool();
        GateHeistMechanics();
        GatePersistence();

        // After persistence, because reaching a camp refills both pools and would clobber the
        // part-drained bar that gate saves. It leaves the player standing at a synthetic bench,
        // which is what lets the crafting gate below craft at all.
        GateCamps();

        // Late on purpose: this one churns the satchel, and persistence deliberately inherits the
        // reagents the micro-loop harvested. Running crafting in the middle wiped them, and that
        // gate caught it.
        GateCraftingAndUse();
        GateInventoryPanel();
        GateAuthoredContent();

        GD.Print("");
        GD.Print(new string('-', 62));
        if (Failures.Count == 0)
        {
            GD.Print($"  ALL GATES PASSED  ({_checks} checks)");
            GD.Print(new string('-', 62));
            return 0;
        }

        GD.Print($"  {Failures.Count} FAILURE(S) of {_checks} checks");
        foreach (var f in Failures) GD.Print($"    x {f}");
        GD.Print(new string('-', 62));
        return 1;
    }

    // ---- The harness itself ----------------------------------------------

    /// <summary>
    /// Every view check below is written to pass when its view is null, so that the gates stay
    /// runnable from anywhere. That politeness would otherwise turn a wiring mistake in
    /// <c>Main</c> into five silently skipped checks and a green run — the same shape of
    /// blindness that hid 6.1 in the first place. So assert the wiring once, here, loudly.
    /// </summary>
    private static void GateHarness()
    {
        Section("harness  the gates can see what the player sees");

        Check("the live RoomView was handed to the verifier", _view != null);
        Check("the live Hud was handed to the verifier", _hud != null);
    }

    // ---- M1: two screens, a toggle, and a doorway ------------------------

    private static void GateM1_TwoScreenNavigation()
    {
        Section("M1  two-screen prototype");

        Check("starts in the Waystation yard",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("starts in the Waking world",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("both Waking rooms are registered",
            NavigationManager.Instance.GetRoom("waystation_yard") != null &&
            NavigationManager.Instance.GetRoom("waystation_shed") != null);

        SetRealm(GameRealm.Dream);
        Check("Tab reaches the Pixel Realm",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Dream);

        // Realm-scoped hotspots: the phantom door exists only in the Dream.
        Check("Dream-only hotspot appears in the Dream",
            IsAvailable("yard_dream_door", GameRealm.Dream));
        Check("Dream-only hotspot is absent from the Waking world",
            !IsAvailable("yard_dream_door", GameRealm.Waking));
        Check("Waking-only hotspot is absent from the Dream",
            !IsAvailable("yard_sign", GameRealm.Dream));

        SetRealm(GameRealm.Waking);
        Check("Tab returns to the Waking world",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
    }

    // ---- M2: state x realm, and the signals that carry it ----------------

    private static void GateM2_StateRealmTable()
    {
        Section("M2  state x realm presentation table");

        var entities = EntityStateManager.Instance;
        var rat = Placement(Rat)?.Entity;

        Check("the rat was registered on first sight", entities.IsKnown(Rat));
        Check("the rat starts Corrupted", entities.GetState(Rat) == EntityState.Corrupted);

        // Corrupted x Waking = Form 1: a nightmare squatting in the road.
        var wakingCorrupt = entities.Resolve(rat, Rat, GameRealm.Waking);
        Check("Form 1 blocks the path", wakingCorrupt.BlocksPath);
        Check("Form 1 is named as the cursed beast", wakingCorrupt.Label == "Cursed Giant Rat");

        // Corrupted x Dream = Form 2: the same creature, as porcelain royalty.
        var dreamCorrupt = entities.Resolve(rat, Rat, GameRealm.Dream);
        Check("Form 2 is the spirit anchor", dreamCorrupt.Label == "The Gnawing Baron");
        Check("Form 2 offers Sever", dreamCorrupt.Verb == "Sever");
        Check("Form 2 does not block the path", !dreamCorrupt.BlocksPath);
        Check("one state, two different faces", wakingCorrupt.Label != dreamCorrupt.Label);

        // A signal must actually carry the change, not just the field.
        var heard = new List<string>();
        void Listener(string id, int state) => heard.Add($"{id}:{(EntityState)state}");
        EventBus.Instance.EntityStateChanged += Listener;
        entities.SetState(Rat, EntityState.Severed);
        EventBus.Instance.EntityStateChanged -= Listener;

        Check("EntityStateChanged fires on transition",
            heard.Contains($"{Rat}:{EntityState.Severed}"));

        var wakingSevered = entities.Resolve(rat, Rat, GameRealm.Waking);
        Check("Severed clears the road", !wakingSevered.BlocksPath);

        entities.SetState(Rat, EntityState.Purified);
        var purifiedWaking = entities.Resolve(rat, Rat, GameRealm.Waking);
        var purifiedDream = entities.Resolve(rat, Rat, GameRealm.Dream);
        Check("Form 3 is a plain animal in reality", purifiedWaking.Label == "Field Rat");
        Check("Form 3 does not block", !purifiedWaking.BlocksPath);
        Check("the Dream echo cannot be attacked", !purifiedDream.Interactable);

        // Put it back — M3 walks the loop properly from the top.
        entities.SetState(Rat, EntityState.Corrupted);
        Check("state is restorable for the next gate",
            entities.GetState(Rat) == EntityState.Corrupted);
    }

    // ---- M3: the whole loop, in order ------------------------------------

    private static void GateM3_MicroLoop()
    {
        Section("M3  Waystation loop  (puzzle -> heist -> finish)");

        var entities = EntityStateManager.Instance;
        var world = WorldStateManager.Instance;
        var inv = InventoryCraftingManager.Instance;

        // 1. The road is barred and the way in is not obvious.
        SetRealm(GameRealm.Waking);
        Check("the shed is unreachable while the rat is Corrupted",
            !InteractionService.ActivateHotspot(Hotspot("yard_to_shed")));
        Check("and the player did not move",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("the bitterroot is not there yet", !IsAvailable("yard_bitterroot", GameRealm.Waking));
        Check("the relic is hidden until the cairn is searched",
            !IsAvailable("yard_relic", GameRealm.Waking));

        // 2. Spectral vision shows you the problem but cannot solve it.
        SetRealm(GameRealm.Dream);
        Check("you cannot sever by looking through spectral vision",
            !InteractionService.ActivateEntity(Rat));
        Check("the rat is still Corrupted", entities.GetState(Rat) == EntityState.Corrupted);
        SetRealm(GameRealm.Waking);

        // 3. The puzzle stands in for the entry minigame.
        Check("searching the cairn succeeds", InteractionService.ActivateHotspot(Hotspot("yard_cairn")));
        Check("the relic is now exposed", IsAvailable("yard_relic", GameRealm.Waking));

        // 4. Cross over and rob the place.
        Check("touching the relic begins the heist",
            InteractionService.ActivateHotspot(Hotspot("yard_relic")));
        Check("you are in the Pixel Realm",
            NavigationManager.Instance.CurrentRoomId == "dream_way_threshold");
        Check("realm is locked for the duration", RealmTransitionManager.Instance.TransitionsLocked);

        // The pursuer starts camped on the anchor, and nothing moves it in a headless run.
        // Drawing it off is the player's job; here we just park it so this gate tests the
        // route rather than the hunt. GateHeistMechanics covers being caught.
        DreamRunManager.Instance.DebugSetPursuerRoom("dream_way_cistern");

        Walk("dream_way_gallery");
        Walk("dream_way_stair");
        Walk("dream_way_solar");
        Check("reached the Baron's solar",
            NavigationManager.Instance.CurrentRoomId == "dream_way_solar");

        Check("taking the anchor succeeds",
            InteractionService.ActivateHotspot(Hotspot("dream_way_anchor")));
        Check("the rat is Severed by the theft", entities.GetState(Rat) == EntityState.Severed);
        Check("you were ejected to the yard",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("back in the Waking world",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("realm is unlocked again", !RealmTransitionManager.Instance.TransitionsLocked);
        Check("the heist is over", !DreamRunManager.Instance.IsRunning);
        Check("and the screen came back with you",
            _view == null || _view.DisplayedRoomId == "waystation_yard");

        // 5. Reality gave way.
        Check("the road is now passable", InteractionService.ActivateHotspot(Hotspot("yard_to_shed")));
        Check("and the player is in the shed",
            NavigationManager.Instance.CurrentRoomId == "waystation_shed");

        Check("gravecap harvests", InteractionService.ActivateHotspot(Hotspot("shed_gravecap")));
        Check("gravecap is in the satchel", inv.GetCount("herb_gravecap") == 1);
        Check("a stripped node refuses a second take",
            !InteractionService.ActivateHotspot(Hotspot("shed_gravecap")));
        Check("the consumed node is gone from the screen",
            !IsAvailable("shed_gravecap", GameRealm.Waking));
        Check("the bench sets its flag", InteractionService.ActivateHotspot(Hotspot("shed_bench")));
        Check("waystation.bench_found is recorded", world.GetFlag("waystation.bench_found"));

        // 6. Zero respawn: the world remembers.
        InteractionService.ActivateHotspot(Hotspot("shed_to_yard"));
        Check("back in the yard", NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("the rat is still Severed, not reset", entities.GetState(Rat) == EntityState.Severed);

        // 7. Finish the anchorless body. Stands in for the turn-based fight.
        Check("finishing the severed body succeeds", InteractionService.ActivateEntity(Rat));
        Check("the rat is Purified", entities.GetState(Rat) == EntityState.Purified);
        Check("the zone is cleansed", world.IsZoneCleansed("waystation"));
        Check("the zone reads as inert",
            Mathf.IsZeroApprox(world.ZoneCorruptionScalar("waystation")));

        // 8. And the payoff is reachable.
        Check("the bitterroot has appeared", IsAvailable("yard_bitterroot", GameRealm.Waking));
        Check("it harvests", InteractionService.ActivateHotspot(Hotspot("yard_bitterroot")));
        Check("two bitterroot in the satchel", inv.GetCount("herb_bitterroot") == 2);

        var form3 = entities.Resolve(Placement(Rat).Entity, Rat, GameRealm.Waking);
        Check("Form 3 is a plain animal", form3.Label == "Field Rat");
        Check("the animal is farmable", InteractionService.ActivateEntity(Rat));
        Check("tallow taken", inv.GetCount("rat_tallow") == 1);
    }

    // ---- Vitality --------------------------------------------------------

    /// <summary>
    /// The body's pool. Nothing here needs the clock switched off the way Resolve's gates do,
    /// because Vitality has no clock — which is itself worth asserting, since the cheapest way
    /// to break §6 later is to give it one.
    ///
    /// Deliberately leaves the bar at a known part-drained value on the way out, so that
    /// <see cref="GateHeistMechanics"/> can prove a whole heist does not move it and
    /// <see cref="GatePersistence"/> round-trips something other than the default.
    /// </summary>
    private static void GateVitalityPool()
    {
        Section("vitality  the body, and the two-pool split");

        var vitality = VitalityManager.Instance;

        Check("the manager is registered", vitality != null);
        if (vitality == null) return;

        vitality.ResetAll();
        Check("starts on a full bar", Mathf.IsEqualApprox(vitality.Current, vitality.Max));
        Check("and is not empty", !vitality.IsEmpty);

        vitality.Damage(30f);
        Check("a wound takes it down", Mathf.IsEqualApprox(vitality.Current, vitality.Max - 30f));

        vitality.Restore(10f);
        Check("treating a wound puts some back",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - 20f));

        // Sign-insensitive on purpose: a caller passing a negative magnitude to Damage must not
        // accidentally heal, which is the classic version of this bug.
        vitality.Damage(-5f);
        Check("Damage ignores the sign it was handed",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - 25f));

        vitality.Restore(999f);
        Check("healing clamps at the ceiling", Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        // -- Emptying, and how often that is announced --
        // Since 2026-07-30 the collapse has a consequence: CampManager listens, empties the satchel
        // of everything death takes, refills both bars and moves you. Two things follow for this
        // gate. The floor is observed from *inside* the handler, because by the time Damage()
        // returns the bar is full again — the zero is real, it is just not the state you are left
        // standing in. And the reagents the micro-loop harvested are borrowed back afterwards,
        // because persistence still needs them; the loss itself is asserted properly in GateCamps,
        // where the mechanic lives.
        var borrowed = InventoryCraftingManager.Instance.Stacks
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Watched through VitalityChanged rather than from inside the VitalityDepleted handler.
        // CampManager subscribed at boot and this gate subscribes now, so its handler runs first and
        // has already refilled the bar by the time ours is reached — reading Current in there would
        // report Max and prove nothing. VitalityChanged carries the zero out before the collapse is
        // even announced, so the lowest value it reports is the floor as it really happened.
        var lowestSeen = float.MaxValue;
        void TrackLow(float current, float _)
        {
            if (current < lowestSeen) lowestSeen = current;
        }
        EventBus.Instance.VitalityChanged += TrackLow;

        var depletions = 0;
        void CountDepletion() => depletions++;
        EventBus.Instance.VitalityDepleted += CountDepletion;

        vitality.Damage(9999f);
        Check("a big enough wound empties it", Mathf.IsZeroApprox(lowestSeen));
        Check("and the collapse is announced", depletions == 1);
        Check("and something picked you up off the floor", !vitality.IsEmpty);

        // A hazard that keeps hitting a downed player must not announce it again. The clamp is
        // what makes this true rather than any latch: a wound that cannot lower the bar is not
        // a change, so no event is raised at all. Reached through Import, which writes zero without
        // announcing — in play the respawn means you never *stay* down, so this is the only honest
        // way to hold the bar on the floor and hit it again.
        vitality.Import(0f);
        Check("the bar can be held at zero", vitality.IsEmpty);
        Check("and holding it there announces nothing", depletions == 1);
        vitality.Damage(20f);
        Check("wounding a downed player announces nothing further", depletions == 1);

        vitality.Restore(15f);
        vitality.Damage(9999f);
        Check("recovering and collapsing again announces again", depletions == 2);

        // -- A load restores state; it must not replay the event that produced it --
        // From a full bar on purpose, so that Import genuinely crosses to zero. Run from an
        // already-empty bar this would pass on the clamp instead and prove nothing.
        vitality.ResetAll();
        Check("resetting refills the bar", Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        vitality.Import(0f);
        Check("importing an empty bar applies the value", vitality.IsEmpty);
        Check("but announces no collapse", depletions == 2);

        EventBus.Instance.VitalityDepleted -= CountDepletion;
        EventBus.Instance.VitalityChanged -= TrackLow;

        InventoryCraftingManager.Instance.ImportStacks(borrowed);
        Check("the satchel the collapses emptied was handed back",
            InventoryCraftingManager.Instance.Stacks.Count == borrowed.Count);

        // -- No clock, which is the rule most likely to be broken by accident later --
        // There is no honest runtime test for this: _Process does not tick meaningfully in a
        // headless run (see handoff.md, "Placeholders and traps"), so a "the bar held still"
        // assertion would be tautological. Assert the structure instead. This fails the moment
        // someone gives the body a per-second drain, which is the change combat.md §6 forbids.
        Check("the body has no clock of its own",
            typeof(VitalityManager).GetMethod("_Process")?.DeclaringType != typeof(VitalityManager));

        // -- The HUD puts the pool that can actually move on top --
        // Out here that is the body. Asserted through the caption on screen rather than by
        // re-deriving the condition, which would only prove the condition agrees with itself.
        Check("the body is the top bar in the Waking world",
            _hud == null || _hud.TopPoolLabel == "VITALITY");
        Check("and is drawn at full strength out here",
            _hud == null || Mathf.IsEqualApprox(_hud.VitalityOpacity, 1f));

        // Spectral vision must not reorder or dim them. Peeking flips the realm without Resolve
        // moving, and you are still standing in the Waking world with a body that can be hurt
        // (invariant 4 — spectral vision is a look, not a place).
        SetRealm(GameRealm.Dream);
        Check("and peeking with spectral vision does not reorder them",
            _hud == null || _hud.TopPoolLabel == "VITALITY");
        Check("nor dim the body while you are only looking",
            _hud == null || Mathf.IsEqualApprox(_hud.VitalityOpacity, 1f));
        SetRealm(GameRealm.Waking);

        GateHazards(vitality);

        // No odd value is handed downstream any more. This gate used to leave the bar at 64 for
        // GatePersistence to save and restore, and camps made that contract unsafe: any gate in
        // between that walks into a room Jeremy has since put a workbench in would silently refill
        // the bar, and persistence would go red over an authoring change. It sets its own value now.
        vitality.ResetAll();
    }

    /// <summary>
    /// Hazards: a Vitality cost on an ordinary hotspot. Built here in code rather than read from
    /// a room, because the mechanism should be provable without depending on authored content
    /// that Jeremy owns and may re-tune in the inspector at any time.
    /// </summary>
    private static void GateHazards(VitalityManager vitality)
    {
        Section("hazards  the world costing you the body");

        var hazard = new HotspotData
        {
            Id = "verify_hazard",
            Label = "A Bad Idea",
            Kind = HotspotKind.Interact,
            VitalityCost = 12f,
            HazardText = "verify: it bit.",
        };

        vitality.ResetAll();
        Check("a plain hotspot with no cost is not a hazard",
            !new HotspotData { Kind = HotspotKind.Interact }.IsHazard);
        Check("one with a cost is", hazard.IsHazard);

        SetRealm(GameRealm.Waking);
        Check("using it succeeds", InteractionService.ActivateHotspot(hazard));
        Check("and it takes exactly what it says",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - 12f));

        // Repeatable on purpose: RequiredFlag / SetsFlag already let an author make any single
        // hazard a one-time event, so no new mechanism is needed for that.
        InteractionService.ActivateHotspot(hazard);
        Check("and can bite twice unless authoring says otherwise",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - 24f));

        // -- Invariant 2, enforced in code rather than trusted to authoring --
        vitality.ResetAll();
        SetRealm(GameRealm.Dream);
        Check("using a hazard through spectral vision still succeeds",
            InteractionService.ActivateHotspot(hazard));
        Check("but costs the body nothing, because Vitality is never spent in the Dream",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));
        SetRealm(GameRealm.Waking);

        // -- A refused action must not wound you --
        // An already-stripped harvest node returns false; charging for it would let the player
        // grind themselves down on something that did nothing at all.
        var strippedNode = new HotspotData
        {
            Id = "verify_hazard_harvest",
            Kind = HotspotKind.Harvest,
            ItemId = "herb_bitterroot",
            HarvestedFlag = "verify.hazard.stripped",
            VitalityCost = 30f,
        };
        WorldStateManager.Instance.SetFlag("verify.hazard.stripped");

        vitality.ResetAll();
        Check("a refused action reports failure",
            !InteractionService.ActivateHotspot(strippedNode));
        Check("and does not wound you",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        // -- Wounding a downed player is silent, not a fresh injury every click --
        // Import rather than SetTo, so the bar is held on the floor without announcing a collapse
        // that CampManager would answer by refilling it and moving you. What is under test here is
        // the clamp inside ApplyHazard, not the respawn.
        vitality.Import(0f);
        InteractionService.ActivateHotspot(hazard);
        Check("a hazard cannot push the bar below the floor",
            Mathf.IsZeroApprox(vitality.Current));

        vitality.ResetAll();

        // The debug wound key needs its action to exist in project.godot; a hand-edited input
        // map fails silently, and the symptom is a key that simply does nothing in play.
        Check("the debug wound key is bound", InputMap.HasAction("debug_wound"));
    }

    // ---- Crafting, knowledge and item use --------------------------------

    /// <summary>
    /// M4's resolution layer. All of it lives in <see cref="InventoryCraftingManager"/> rather than
    /// in a panel, which is the only reason any of this is assertable without a mouse.
    ///
    /// Runs at a bench: <see cref="GateCamps"/> leaves the player standing at one, because since
    /// 2026-07-30 crafting anywhere else is refused outright. The venue rule itself is asserted
    /// there rather than repeated here; everything below is about the resolution.
    /// </summary>
    private static void GateCraftingAndUse()
    {
        Section("crafting  recipes, knowledge and spending things");

        var inv = InventoryCraftingManager.Instance;
        var vitality = VitalityManager.Instance;

        Check("these run at a bench, which crafting now requires",
            CampManager.Instance?.AtCamp == true);

        // -- The authored recipe .tres loaded, and loaded *completely* --
        // Hand-written typed arrays of nested resources are the documented silent-failure mode in
        // this project: a wrong ext_resource path or a malformed Array[...] line yields a recipe
        // with zero inputs and no error. So assert the contents, not merely that it parsed.
        var recipe = inv.GetRecipe("recipe_salve_clearmind");
        Check("the authored recipe is registered", recipe != null);
        if (recipe == null) return;

        Check("and its inputs survived the load", recipe.Inputs.Count == 2);
        // Ids and positivity, not the quantities. A malformed typed array yields an empty
        // collection or blank sub-resources, and that is what this catches; the exact counts are
        // a balance decision and pinning them would only manufacture false failures.
        Check("with their ingredients intact",
            recipe.Inputs[0].ItemId == "herb_bitterroot" && recipe.Inputs[0].Amount > 0 &&
            recipe.Inputs[1].ItemId == "rat_tallow" && recipe.Inputs[1].Amount > 0);
        Check("and it points at a real item", inv.GetItem(recipe.OutputItemId) != null);

        // The rename settled on 2026-07-29. A stale property name in the .tres would deserialise
        // to 0 in silence, so the field is asserted to have arrived — but NOT to equal any
        // particular number. How much a salve heals is Jeremy's to tune; a gate that pins it
        // would go red on a balance pass and teach him that red means "ignore me".
        Check("the salve's VitalityRestore came through the rename",
            inv.GetItem("salve_clearmind").VitalityRestore > 0f);

        // -- Refusing to craft what you cannot afford --
        inv.ResetAll();
        Check("an empty satchel cannot craft", !inv.CanCraft("recipe_salve_clearmind"));
        Check("and says why", inv.Inspect("recipe_salve_clearmind") == CraftBlock.MissingMaterials);
        Check("a nonexistent recipe is its own kind of no",
            inv.Inspect("recipe_does_not_exist") == CraftBlock.NoSuchRecipe);

        // Partial materials must not craft, and must not consume what is there. Note this actually
        // *attempts* the craft — an earlier version only asked CanCraft, which meant the check
        // named "a refused craft consumes nothing" was really asserting that doing nothing
        // consumed nothing. Charging the first input before discovering the second is missing is
        // the exact bug this is here to catch, so the attempt has to happen.
        inv.AddItem("herb_bitterroot", 2);
        Check("two of three ingredients is still no", !inv.CanCraft("recipe_salve_clearmind"));
        Check("attempting it anyway fails", !inv.Craft("recipe_salve_clearmind"));
        Check("a refused craft consumes nothing", inv.GetCount("herb_bitterroot") == 2);
        Check("and produces nothing", inv.GetCount("salve_clearmind") == 0);

        // -- A craft that works --
        inv.AddItem("rat_tallow", 1);
        Check("with everything in hand it can be made", inv.CanCraft("recipe_salve_clearmind"));
        Check("and crafting reports success", inv.Craft("recipe_salve_clearmind"));
        Check("the output arrives", inv.GetCount("salve_clearmind") == 1);
        Check("the inputs are gone",
            inv.GetCount("herb_bitterroot") == 0 && inv.GetCount("rat_tallow") == 0);
        Check("and it cannot be made twice off one set of materials",
            !inv.CanCraft("recipe_salve_clearmind"));

        // -- Knowledge gating, built in from the first recipe --
        // Registered rather than merely constructed, so these run through the real Inspect path.
        // An earlier version only poked the discovery set and left the gate in Inspect completely
        // untested — an unregistered recipe answers NoSuchRecipe, which looks like a refusal and
        // is nothing of the kind.
        var gated = new RecipeData
        {
            Id = "verify_gated",
            OutputItemId = "salve_clearmind",
            RequiredBlueprintId = "lore_verify_page",
        };
        gated.Inputs.Add(new RecipeIngredient { ItemId = "herb_gravecap", Amount = 1 });
        inv.DebugRegisterRecipe(gated);

        Check("a recipe needing a blueprint knows it does", gated.NeedsBlueprint);
        Check("an unlearned blueprint is not discovered", !inv.IsDiscovered("lore_verify_page"));

        // Materials in hand, so the only thing standing in the way is the knowledge.
        inv.AddItem("herb_gravecap", 1);
        Check("and without it the recipe refuses", !inv.CanCraft("verify_gated"));
        Check("naming knowledge, not materials, as the reason",
            inv.Inspect("verify_gated") == CraftBlock.Unknown);
        Check("attempting an unknown recipe fails", !inv.Craft("verify_gated"));
        Check("and consumes nothing", inv.GetCount("herb_gravecap") == 1);

        Check("learning it reports newly-learned", inv.Discover("lore_verify_page"));
        Check("and it is now known", inv.IsDiscovered("lore_verify_page"));
        Check("learning it again is not news", !inv.Discover("lore_verify_page"));
        Check("and now the same recipe is craftable", inv.CanCraft("verify_gated"));
        Check("so knowledge was the only thing missing",
            inv.Inspect("verify_gated") == CraftBlock.None);

        // Picking up a Lore item teaches it, which is what makes the journal purely additive.
        Check("a blueprint not yet seen is unknown", !inv.IsDiscovered("catalyst_ashincense"));
        inv.AddItem("herb_gravecap", 1);
        Check("picking up a non-Lore item teaches nothing",
            !inv.IsDiscovered("herb_gravecap"));

        // -- Using a salve --
        vitality.SetTo(vitality.Max - 20f);
        Check("a wounded body can be treated", inv.UseItem("salve_clearmind"));
        Check("the salve is spent", inv.GetCount("salve_clearmind") == 0);
        Check("and Vitality came back, capped at full",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        // Refuse rather than waste: her salves are finite and uncraftable (invariant 7).
        inv.AddItem("salve_clearmind", 1);
        vitality.ResetAll();
        Check("a salve is refused at full Vitality", !inv.UseItem("salve_clearmind"));
        Check("and is not consumed by the refusal", inv.GetCount("salve_clearmind") == 1);

        // A salve must never touch Resolve — combat.md §6, and invariant 11 downstream of it.
        var resolve = ResolveManager.Instance;
        resolve.SetTo(resolve.Max * 0.5f);
        vitality.SetTo(vitality.Max - 40f);
        var resolveBefore = resolve.Current;
        Check("using a salve while short of nerve still works", inv.UseItem("salve_clearmind"));
        Check("but Resolve is untouched by it",
            Mathf.IsEqualApprox(resolve.Current, resolveBefore));
        resolve.SetTo(resolve.Max);

        // -- Things that are not salves do not get used --
        inv.AddItem("herb_bitterroot", 1);
        vitality.SetTo(vitality.Max - 10f);
        var vitalityBefore = vitality.Current;
        Check("a raw reagent cannot be used", !inv.UseItem("herb_bitterroot"));
        Check("and using it changes nothing",
            inv.GetCount("herb_bitterroot") == 1 &&
            Mathf.IsEqualApprox(vitality.Current, vitalityBefore));
        Check("an item you do not hold cannot be used", !inv.UseItem("salve_clearmind"));

        inv.ResetAll();
        vitality.ResetAll();
    }

    // ---- Heist mechanics -------------------------------------------------

    private static void GateHeistMechanics()
    {
        Section("heist  clock, pursuit and both failure endings");

        var run = DreamRunManager.Instance;
        var resolve = ResolveManager.Instance;

        // Invariant 2: Vitality must never appear in the Dream. Every ending in there is an
        // ejection priced in Resolve, and if the body followed you in, the two-pool split — and
        // with it the reason the two routes cost different currencies — collapses. Captured here
        // rather than in the Vitality gate because this is the gate that actually goes inside,
        // through both failure endings and a success.
        var vitality = VitalityManager.Instance;
        var vitalityOnEntry = vitality?.Current ?? 0f;

        Check("the heist is registered", run.GetHeist("waystation_heist") != null);

        // -- Being touched --
        resolve.SetTo(resolve.Max);
        Check("a run begins", run.Begin("waystation_heist"));
        Check("the pursuer starts on the prize", run.PursuerRoomId == "dream_way_solar");
        Check("and is three rooms off", run.PursuerDistance == 3);

        // Inside a run the nerve is the pool at risk, so it takes the top slot and the body dims.
        Check("the nerve becomes the top bar inside a heist",
            _hud == null || _hud.TopPoolLabel == "RESOLVE");
        Check("and the body dims to say it is not in play",
            _hud == null || _hud.VitalityOpacity < 1f);
        // Dimmed, never hidden: invariant 2 promises nothing in the Dream can kill you, and a
        // readable bar states that promise where an absent one would leave it to be guessed.
        Check("but stays readable rather than vanishing",
            _hud == null || _hud.VitalityOpacity > 0.2f);
        Check("and the nerve is never dimmed",
            _hud == null || Mathf.IsEqualApprox(_hud.ResolveOpacity, 1f));

        Walk("dream_way_gallery");
        Check("closing the distance is reported", run.PursuerDistance == 2);

        // An ordinary door, to prove the view tracks the player at all before we ask it to
        // survive the hard case. Without this, "the view never renders the wrong room" could
        // be passed by a view that renders nothing.
        Check("the view follows an ordinary door",
            _view == null || _view.DisplayedRoomId == "dream_way_gallery");

        // Burn some of the clock, then walk straight into it.
        resolve.SetTo(resolve.Max * 0.5f);
        Walk("dream_way_stair");
        Walk("dream_way_solar");
        Check("walking into the pursuer ends the run", !run.IsRunning);
        Check("returning refills the bar", Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        Check("and throws you back to the Waking world",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard" &&
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);

        // Being caught navigates *from inside* the RoomEntered handler that reported the step
        // into the pursuer's room. The ejection's RoomEntered therefore reaches every later
        // subscriber first, and the stale one lands last — so a view that trusts the payload
        // finishes by drawing the Dream room the player was just yanked out of. Managers stay
        // correct throughout, which is why the gates above pass and the game still looked
        // broken: no drain, no exits, the Dream still on screen. Written up in handoff.md,
        // "Closed post-mortem: the ejection failure."
        Check("the view shows where you were ejected to, not where you were caught",
            _view == null || _view.DisplayedRoomId == "waystation_yard");
        Check("and the HUD names that room too",
            _hud == null || _hud.DisplayedRoomName == "The Forgotten Waystation Yard");

        // -- Running out of nerve --
        resolve.SetTo(resolve.Max);
        Check("a second run begins", run.Begin("waystation_heist"));
        Check("drain is at least the base rate",
            run.CurrentDrainPerSecond() >= run.ActiveHeist.DrainPerSecond);

        resolve.SetTo(0f);
        Check("running out of Resolve ends the run", !run.IsRunning);
        Check("and ejects you", NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("and you are ready to try again", Mathf.IsEqualApprox(resolve.Current, resolve.Max));

        // -- Nothing outside a heist touches the bar, as built --
        // True today only because boss fights do not exist. combat.md §6 has Resolve draining in
        // a Waking boss fight too — though only while that boss's tether is unbroken — so when
        // auras land these two widen to "no drain outside a heist, or a boss fight against a
        // Corrupted entity" rather than being deleted. The rule that survives unchanged is that
        // there is no *ambient* drain, which is what these two actually pin down.
        var held = resolve.Current;
        Check("no drain outside a heist", Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));
        Check("the bar holds still in the Waking world",
            Mathf.IsEqualApprox(resolve.Current, held));

        // -- Invariant 2, measured across everything above --
        Check("Vitality never moved in the Dream",
            vitality == null || Mathf.IsEqualApprox(vitality.Current, vitalityOnEntry));

        // And the emphasis came back with us, through both failure endings above.
        Check("the body is the top bar again once the run is over",
            _hud == null || _hud.TopPoolLabel == "VITALITY");
        Check("and is undimmed again, because it can be hurt out here",
            _hud == null || Mathf.IsEqualApprox(_hud.VitalityOpacity, 1f));

        run.ForceClear();
    }

    // ---- Persistence -----------------------------------------------------

    private static void GatePersistence()
    {
        Section("save / load round trip");

        const string path = "user://verify_slice.json";

        // Taken from the verifier's own patch of open ground, and a part-drained bar set here rather
        // than inherited from the Vitality gate. Both for the same reason: reaching a camp refills
        // both pools, so a wounded bar saved in — or loaded into — an authored room would go red the
        // day Jeremy puts a workbench in that room. Nothing about this round trip should depend on
        // where he decides the bench goes.
        NavigationManager.Instance.GoToRoom("verify_wilds");

        const float wounded = 64f;
        VitalityManager.Instance.SetTo(wounded);

        Check("save writes", SaveService.Save(path));

        // Wipe everything the save is supposed to carry.
        WorldStateManager.Instance.ResetAll();
        EntityStateManager.Instance.ResetAll();
        InventoryCraftingManager.Instance.ResetAll();
        VitalityManager.Instance.ResetAll();
        SetRealm(GameRealm.Dream);

        Check("world was actually cleared",
            !WorldStateManager.Instance.GetFlag("zone.waystation.purified") &&
            InventoryCraftingManager.Instance.GetCount("rat_tallow") == 0);

        Check("and Vitality was actually cleared",
            Mathf.IsEqualApprox(VitalityManager.Instance.Current, VitalityManager.Instance.Max));

        Check("load reads", SaveService.Load(path));

        Check("entity states restored",
            EntityStateManager.Instance.GetState(Rat) == EntityState.Purified);
        Check("zone phase restored", WorldStateManager.Instance.IsZoneCleansed("waystation"));
        Check("flags restored", WorldStateManager.Instance.GetFlag("waystation.bench_found"));
        Check("inventory restored",
            InventoryCraftingManager.Instance.GetCount("herb_bitterroot") == 2 &&
            InventoryCraftingManager.Instance.GetCount("rat_tallow") == 1);
        Check("realm restored", RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("room restored", NavigationManager.Instance.CurrentRoomId == "verify_wilds");
        Check("Vitality restored",
            Mathf.IsEqualApprox(VitalityManager.Instance.Current, wounded));

        SaveService.DeleteSave(path);

        // -- A save taken inside a heist must not strand you there --
        // No Dream room has a door back to the Waking world, so a load that restores a Dream
        // room without a run is a soft lock with no drain, no exit and a dead anchor.
        const string dreamPath = "user://verify_slice_dream.json";
        var run = DreamRunManager.Instance;

        Check("a run begins for the stranding check", run.Begin("waystation_heist"));
        run.DebugSetPursuerRoom("dream_way_cistern");
        Walk("dream_way_gallery");

        Check("a save can be taken from inside the maze",
            NavigationManager.Instance.CurrentRoomId == "dream_way_gallery" &&
            SaveService.Save(dreamPath));

        // Simulate a fresh session: the run is gone, only the file remains.
        run.ForceClear();

        Check("loading a mid-heist save reads", SaveService.Load(dreamPath));
        Check("and does not strand you inside the Dream maze",
            !run.IsDreamRoom(NavigationManager.Instance.CurrentRoomId));
        Check("and returns you to the heist's exit",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("and puts you back in the Waking world",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("and leaves the realm unlocked",
            !RealmTransitionManager.Instance.TransitionsLocked);
        Check("and leaves no run running", !run.IsRunning);
        Check("and shows you the outside", _view == null || _view.DisplayedRoomId == "waystation_yard");

        SaveService.DeleteSave(dreamPath);
    }

    // ---- Camps -----------------------------------------------------------

    /// <summary>
    /// Camps: the bench that makes a room safe, the one shared cache, and what a collapse costs.
    ///
    /// Built on rooms constructed here and registered into <c>NavigationManager</c> rather than on
    /// authored content, because *which* room holds a workbench is a judged call Jeremy makes in the
    /// editor and the mechanic has to be provable before he makes it. **Two** camps rather than one,
    /// so "one shared cache reachable from every bench" is asserted instead of assumed — with a single
    /// camp, per-camp storage and a shared box are indistinguishable.
    ///
    /// Runs after <see cref="GatePersistence"/> because arriving at a camp refills both pools, which
    /// would clobber the part-drained bar that gate saves. It leaves the player standing at a bench,
    /// which is what lets <see cref="GateCraftingAndUse"/> craft at all.
    /// </summary>
    private static void GateCamps()
    {
        Section("camps  the bench, the cache, and what a collapse costs");

        var camp = CampManager.Instance;
        var nav = NavigationManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        var vitality = VitalityManager.Instance;
        var resolve = ResolveManager.Instance;

        Check("the manager is registered", camp != null);
        if (camp == null) return;

        camp.ResetAll();
        inv.ResetAll();
        SetRealm(GameRealm.Waking);

        // -- What makes a camp a camp --
        // One fact, one place: there is no IsCamp flag on RoomData, so a bench outside a camp and a
        // camp with no bench are both unrepresentable rather than merely unlikely.
        Check("a room holding a workbench is a camp", CampManager.IsCamp("verify_camp"));
        Check("a room without one is not", !CampManager.IsCamp("verify_wilds"));
        Check("an unregistered room is not", !CampManager.IsCamp("no_such_room_anywhere"));
        Check("and neither is nothing at all", !CampManager.IsCamp((RoomData)null));

        // -- Out in the open --
        nav.GoToRoom("verify_wilds");
        Check("standing on open ground is not being at camp", !camp.AtCamp);
        Check("and nothing has bound a return point yet", camp.ReturnCampRoomId == "");

        // -- Crafting is a thing you do at a bench, or not at all --
        inv.AddItem("herb_bitterroot", 5);
        inv.AddItem("rat_tallow", 5);
        Check("a full satchel still will not craft in the open",
            !inv.CanCraft("recipe_salve_clearmind"));
        Check("and the refusal names the venue, not the materials",
            inv.Inspect("recipe_salve_clearmind") == CraftBlock.NotAtCamp);

        // Order inside Inspect, which is the part that is easy to get wrong: an unlearned recipe must
        // stay Unknown even in the open. Answering NotAtCamp first would confirm the recipe exists to
        // a player who has not found the page yet, and the journal is supposed to be the only thing
        // that tells them that.
        var gated = new RecipeData
        {
            Id = "verify_camp_gated",
            OutputItemId = "salve_clearmind",
            RequiredBlueprintId = "lore_verify_camp_page",
        };
        gated.Inputs.Add(new RecipeIngredient { ItemId = "herb_bitterroot", Amount = 1 });
        inv.DebugRegisterRecipe(gated);
        Check("an unlearned recipe stays unknown rather than complaining about the venue",
            inv.Inspect("verify_camp_gated") == CraftBlock.Unknown);

        // -- The cache is shut out here too --
        Check("the cache will not take a deposit in the open",
            inv.InspectDeposit("herb_bitterroot") == StashBlock.NotAtCamp);
        Check("attempting one anyway does nothing",
            !inv.Deposit("herb_bitterroot") && inv.GetCount("herb_bitterroot") == 5);
        Check("nor will it hand anything back",
            inv.InspectWithdraw("herb_bitterroot") == StashBlock.NotAtCamp);
        Check("and an item that does not exist is its own kind of no",
            inv.InspectDeposit("no_such_item_at_all") == StashBlock.NoSuchItem);

        // -- Arriving. There is nothing to click, which is the whole design --
        vitality.SetTo(vitality.Max - 40f);
        resolve.SetTo(resolve.Max * 0.4f);

        nav.GoToRoom("verify_camp");
        Check("reaching a bench is being at camp", camp.AtCamp);
        Check("and arriving refills the body", Mathf.IsEqualApprox(vitality.Current, vitality.Max));
        Check("and the nerve", Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        Check("and binds where you wake, with no claim to click",
            camp.ReturnCampRoomId == "verify_camp");

        // -- Invariant 13: the payload can be stale, the manager cannot --
        // An earlier subscriber may navigate again before this one is reached, which is exactly the
        // ejection post-mortem in the handoff. Reproduced by hand: announce arrival at a camp while
        // the player is demonstrably standing somewhere else.
        nav.GoToRoom("verify_wilds");
        vitality.SetTo(vitality.Max - 30f);
        EventBus.Instance.EmitRoomEntered("verify_camp");
        Check("a stale arrival at a camp is ignored",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - 30f));

        // -- Invariant 2: the bench is off inside the Dream, on while merely looking --
        nav.GoToRoom("verify_camp");
        SetRealm(GameRealm.Dream);
        Check("peeking with spectral vision does not close the bench", camp.AtCamp);
        SetRealm(GameRealm.Waking);

        var run = DreamRunManager.Instance;
        resolve.SetTo(resolve.Max);
        Check("a run begins for the invariant-2 check", run.Begin("waystation_heist"));
        vitality.SetTo(vitality.Max - 25f);
        nav.GoToRoom("verify_camp");
        Check("a workbench inside a live run is not a camp", !camp.AtCamp);
        Check("so arriving at one cannot refill the body in there",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - 25f));
        run.ForceClear();
        SetRealm(GameRealm.Waking);

        // -- One box, reachable from every bench --
        nav.GoToRoom("verify_camp");
        Check("the cache takes a deposit at a bench", inv.Deposit("herb_bitterroot", 2));
        Check("the satchel is lighter by exactly that", inv.GetCount("herb_bitterroot") == 3);
        Check("and the cache heavier by it", inv.GetStashCount("herb_bitterroot") == 2);
        Check("depositing more than you carry is refused",
            inv.InspectDeposit("herb_bitterroot", 99) == StashBlock.NotEnough);
        Check("and attempting it takes nothing",
            !inv.Deposit("herb_bitterroot", 99) && inv.GetCount("herb_bitterroot") == 3);
        Check("withdrawing more than you cached is refused",
            inv.InspectWithdraw("herb_bitterroot", 99) == StashBlock.NotEnough);

        nav.GoToRoom("verify_camp_two");
        Check("a second bench is a camp too", camp.AtCamp);
        Check("and the return point follows you to the newest one",
            camp.ReturnCampRoomId == "verify_camp_two");
        Check("the same cache is open at the other bench",
            inv.GetStashCount("herb_bitterroot") == 2);
        Check("and gives it back there", inv.Withdraw("herb_bitterroot", 2));
        Check("the satchel has it again", inv.GetCount("herb_bitterroot") == 5);
        Check("and the cache is empty", inv.GetStashCount("herb_bitterroot") == 0);

        // -- And with a bench in the room, the recipe that was refused above goes through --
        Check("a bench is the only thing the recipe was missing",
            inv.Inspect("recipe_salve_clearmind") == CraftBlock.None);
        Check("and it can actually be made here", inv.Craft("recipe_salve_clearmind"));

        // -- What a collapse costs: what you can remake, and nothing else --
        inv.ResetAll();
        inv.AddItem("herb_bitterroot", 3);     // Reagent  — farmable
        inv.AddItem("catalyst_ashincense", 1); // Catalyst — craftable, so never a softlock
        inv.AddItem("salve_clearmind", 2);     // Salve    — hers, finite, uncraftable
        inv.AddItem("herb_gravecap", 4);
        Check("something is put in the cache before dying", inv.Deposit("herb_gravecap", 4));

        nav.GoToRoom("verify_wilds");
        vitality.SetTo(vitality.Max - 10f);
        resolve.SetTo(resolve.Max * 0.3f);
        vitality.Damage(9999f);

        Check("collapsing wakes you at the last bench you stood at",
            nav.CurrentRoomId == "verify_camp_two");
        Check("with the body back", Mathf.IsEqualApprox(vitality.Current, vitality.Max));
        Check("and the nerve back", Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        Check("reagents are gone, because you can go and get more",
            inv.GetCount("herb_bitterroot") == 0);
        Check("catalysts too, because you can make more",
            inv.GetCount("catalyst_ashincense") == 0);
        Check("but her salves come back with you", inv.GetCount("salve_clearmind") == 2);
        Check("and dying does not reach into the cache", inv.GetStashCount("herb_gravecap") == 4);

        // The rule behind those, stated where it cannot drift: **death destroys what you can remake
        // and keeps what you cannot.** Lore and Relic have no authored item yet, so they are asserted
        // against the rule directly — a blueprint page or a crossing relic is the only copy, and
        // destroying one would delete progress permanently and in silence.
        Check("a blueprint page would survive a collapse",
            InventoryCraftingManager.SurvivesDeath(ItemCategory.Lore));
        Check("so would a relic", InventoryCraftingManager.SurvivesDeath(ItemCategory.Relic));
        Check("a reagent would not", !InventoryCraftingManager.SurvivesDeath(ItemCategory.Reagent));
        Check("nor a catalyst", !InventoryCraftingManager.SurvivesDeath(ItemCategory.Catalyst));

        // -- Collapsing before you have ever found a camp --
        // Driven through the public method rather than the signal, so both entry points are covered.
        camp.ResetAll();
        nav.GoToRoom("verify_wilds");
        vitality.SetTo(vitality.Max - 50f);
        camp.RespawnAtCamp();
        Check("with no camp found yet you wake where the game started",
            nav.CurrentRoomId == GameConfig.StartingRoomId);
        Check("and still get up off the ground",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        // -- Both new save fields, round-tripped --
        // Saved from *outside* a camp on purpose. Save from inside one and the load's navigation
        // would rebind the return point on arrival, so the check would pass with Import broken.
        const string path = "user://verify_camps.json";

        nav.GoToRoom("verify_camp");
        inv.ResetAll();
        inv.AddItem("rat_tallow", 3);
        Check("something is cached for the round trip", inv.Deposit("rat_tallow", 3));
        nav.GoToRoom("verify_wilds");
        Check("the camp save writes", SaveService.Save(path));

        camp.ResetAll();
        inv.ResetAll();
        Check("the return point was actually cleared", camp.ReturnCampRoomId == "");
        Check("and the cache with it", inv.GetStashCount("rat_tallow") == 0);

        Check("the camp save loads", SaveService.Load(path));
        Check("and it puts you back on open ground, where nothing can rebind",
            nav.CurrentRoomId == "verify_wilds");
        Check("so the return point came out of the file", camp.ReturnCampRoomId == "verify_camp");
        Check("and so did the cache", inv.GetStashCount("rat_tallow") == 3);

        SaveService.DeleteSave(path);

        // Left standing at a bench with an empty satchel: the crafting gate below crafts repeatedly,
        // and crafting only happens at a camp now.
        inv.ResetAll();
        nav.GoToRoom("verify_camp");
        vitality.ResetAll();
        resolve.SetTo(resolve.Max);
        Check("the gates below are left standing at a bench", camp.AtCamp);
    }

    /// <summary>
    /// The verifier's own rooms, registered once before any gate runs. Two camps and one patch of
    /// open ground, all under a <c>verify_</c> prefix that the authoring sweep skips.
    ///
    /// Registered up front rather than inside <see cref="GateCamps"/> because
    /// <see cref="GatePersistence"/> needs somewhere to stand that Jeremy cannot turn into a camp.
    /// It saves a wounded bar and loads it back, and arriving at a camp refills both pools — so
    /// taking that round trip in an authored room would go red the day a workbench lands in it.
    /// </summary>
    private static void RegisterVerifyRooms()
    {
        var nav = NavigationManager.Instance;
        if (nav == null) return;

        nav.DebugRegisterRoom(VerifyRoom("verify_camp", "A Bench Under Canvas", withBench: true));
        nav.DebugRegisterRoom(VerifyRoom("verify_camp_two", "A Second Bench", withBench: true));
        nav.DebugRegisterRoom(VerifyRoom("verify_wilds", "Open Ground", withBench: false));
    }

    /// <summary>
    /// A room built in code for <see cref="GateCamps"/>. Kept clean enough to pass the authoring
    /// sweep on its own merits even though that gate skips it — a helper that could only pass by
    /// being excluded would be worth distrusting.
    /// </summary>
    private static RoomData VerifyRoom(string id, string displayName, bool withBench)
    {
        var room = new RoomData { Id = id, DisplayName = displayName, ZoneId = "verify" };

        room.Hotspots.Add(new HotspotData
        {
            Id = $"{id}_look",
            Label = "Nothing In Particular",
            Kind = HotspotKind.Interact,
            ExamineText = "verify: scenery.",
        });

        if (withBench)
        {
            room.Hotspots.Add(new HotspotData
            {
                Id = $"{id}_bench",
                Label = "Your Workbench",
                Kind = HotspotKind.Workbench,
            });
        }

        return room;
    }

    // ---- Authored content ------------------------------------------------

    /// <summary>
    /// Sweeps every registered room and checks the *authoring*, not the engine. Jeremy owns content
    /// (see handoff.md, "Who does what"), so this is the safety net under that: it catches fields
    /// that look set but do nothing, which the inspector will never warn about and playing rarely
    /// reveals.
    ///
    /// Deliberately **value-agnostic**. It never asserts a particular cost, label or room, because
    /// those are his to retune and a gate that fights the author is a gate that gets deleted. It
    /// asserts shapes: that a hazard exists somewhere, and that no hotspot is quietly inert.
    ///
    /// Nothing here activates anything — an arbitrary authored hotspot could navigate, or start a
    /// heist. The hazard *mechanism* is proven with synthetic data in <see cref="GateHazards"/>.
    /// </summary>
    /// <summary>
    /// The satchel panel, asserted through its own drawn state rather than its intentions.
    ///
    /// The reason this gate exists at all: the panel is loaded from a path string, so a mistyped
    /// or renamed <c>.tscn</c> costs nothing until someone presses <c>I</c> in a playtest and
    /// nothing happens. That is a bug the compiler cannot see and the other 188 checks would not
    /// notice. Every check below reads a property that reflects what is on screen — slot children
    /// that exist, a button that is actually disabled — so none of them can pass by agreeing with
    /// the condition that produced them.
    ///
    /// Placed after <see cref="GateCraftingAndUse"/> for the same reason that one is late: it
    /// churns the satchel and the Vitality bar, and the gates above inherit state from each other.
    /// It sets up its own satchel rather than borrowing whatever crafting left behind.
    /// </summary>
    private static void GateInventoryPanel()
    {
        Section("inventory panel  the satchel on screen");

        var panel = _hud?.Inventory;
        Check("the panel scene loaded and reached the Hud", panel != null);
        if (panel == null) return;

        var inv = InventoryCraftingManager.Instance;
        var vitality = VitalityManager.Instance;
        Check("its managers are present", inv != null && vitality != null);
        if (inv == null || vitality == null) return;

        // -- Opening and closing --
        Check("it starts closed", !panel.IsOpen);

        panel.Open();
        Check("Open puts it up", panel.IsOpen);

        panel.Close();
        Check("Close takes it down", !panel.IsOpen);

        panel.Toggle();
        Check("Toggle opens from closed", panel.IsOpen);
        panel.Toggle();
        Check("and closes from open", !panel.IsOpen);

        // -- One slot per stack, and only for what is held --
        inv.ResetAll();
        panel.Open();
        Check("an empty satchel draws no slots", panel.SlotCount == 0);

        inv.AddItem("herb_bitterroot", 3);
        Check("a stack appears as one slot", panel.SlotCount == 1);

        inv.AddItem("salve_clearmind", 1);
        Check("a second kind is a second slot", panel.SlotCount == 2);

        // Not a third slot: quantity lives on the slot, not beside it.
        inv.AddItem("herb_bitterroot", 2);
        Check("more of the same is still one slot", panel.SlotCount == 2);

        inv.RemoveItem("herb_bitterroot", 5);
        Check("a stack spent to nothing loses its slot", panel.SlotCount == 1);

        // Two separate paths to a correct grid, and the panel takes a different one for each: while
        // it is up, the subscription rebuilds immediately (proven above — those slots appeared with
        // nothing calling into the panel); while it is down, the change is deferred to Open.
        panel.Close();
        inv.AddItem("rat_tallow", 1);
        panel.Open();
        Check("a pickup made while it was down is there on opening", panel.SlotCount == 2);
        inv.RemoveItem("rat_tallow", 1);
        Check("and the grid is back to one when it goes", panel.SlotCount == 1);

        // The load case, which is where a deferred rebuild actually bites: ImportStacks replaces
        // the whole satchel at once, and it happens while no panel is on screen.
        panel.Close();
        inv.ImportStacks(new Dictionary<string, int> { ["salve_clearmind"] = 1, ["rat_tallow"] = 2 });
        panel.Open();
        Check("a loaded satchel draws the loaded stacks", panel.SlotCount == 2);
        Check("and nothing of the old one survives", !inv.Has("herb_bitterroot"));

        // -- The detail column and the one question the panel does not answer --
        panel.DebugSelect("");
        Check("nothing is selected to begin with", panel.SelectedItemId == "");
        Check("and the Use button is dead", !panel.UseEnabled);

        vitality.ResetAll();
        vitality.Damage(30f);

        panel.DebugSelect("salve_clearmind");
        Check("selecting a salve names it", panel.DetailName == inv.GetItem("salve_clearmind").DisplayName);
        Check("and a wounded body can use it", panel.UseEnabled);

        // The panel must react to the *body*, not only to the satchel. Nothing here adds or
        // removes an item, so only a VitalityChanged subscription can make this pass.
        vitality.Restore(9999f);
        Check("healing to full greys the button with the satchel untouched", !panel.UseEnabled);

        vitality.Damage(30f);
        Check("and wounding again brings it back", panel.UseEnabled);

        inv.AddItem("herb_bitterroot", 1);
        panel.DebugSelect("herb_bitterroot");
        Check("a raw reagent is named but not usable",
            panel.DetailName == inv.GetItem("herb_bitterroot").DisplayName && !panel.UseEnabled);

        // -- Spending the selection --
        panel.DebugSelect("salve_clearmind");
        var before = vitality.Current;
        var slotsBefore = panel.SlotCount;
        Check("using it from the panel spends the item", inv.UseItem("salve_clearmind"));
        Check("and treats the wound", vitality.Current > before);
        Check("the spent stack takes the selection with it", panel.SelectedItemId == "");
        // One fewer than there were, rather than a literal count — the setup above is free to
        // change without this line becoming a lie about what it is checking.
        Check("and its slot is gone", panel.SlotCount == slotsBefore - 1);

        panel.Close();
        Check("it is left closed for whatever runs next", !panel.IsOpen);
    }

    private static void GateAuthoredContent()
    {
        Section("content  the authoring, swept for silent mistakes");

        // GateCamps registers synthetic camps into the same registry. They are not authored content,
        // so sweeping them would both dilute "at least one hazard is authored" and point any failure
        // at a room Jeremy cannot open in the editor.
        var rooms = NavigationManager.Instance.Rooms
            .Where(kv => !kv.Key.StartsWith("verify_"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Check("authored rooms are registered", rooms.Count > 0);

        // -- At least one hazard exists, and can actually bite --
        var hazards = 0;
        var biteableHazards = 0;
        foreach (var (_, room) in rooms)
        {
            foreach (var h in room.Hotspots)
            {
                if (h is not { IsHazard: true }) continue;
                hazards++;

                // A hazard only reachable from the Dream can never charge you: ApplyHazard refuses
                // in the Dream (invariant 2), so a DreamOnly hazard is authored cost that is
                // unreachable by construction.
                if (h.Visibility != RealmVisibility.DreamOnly) biteableHazards++;
            }
        }

        Check("at least one hazard is authored", hazards > 0);
        Check("and at least one of them is reachable in the Waking world", biteableHazards > 0);

        // -- Fields that look set but do nothing --
        var blank = new List<string>();
        var inertFlagGate = new List<string>();
        var harvestNoItem = new List<string>();
        var navNowhere = new List<string>();
        var costNoText = new List<string>();

        foreach (var (roomId, room) in rooms)
        {
            for (var i = 0; i < room.Hotspots.Count; i++)
            {
                var h = room.Hotspots[i];
                if (h == null) { blank.Add($"{roomId}[{i}] null"); continue; }

                // An Id-less hotspot is an array element someone replaced with a fresh resource —
                // the inspector's "New HotspotData" on an existing slot does exactly this, and it
                // silently deletes whatever was there. It still draws a clickable region at the
                // default rect, so the symptom in play is a dead zone and a missing exit.
                if (string.IsNullOrEmpty(h.Id)) { blank.Add($"{roomId}[{i}]"); continue; }

                var where = $"{roomId}/{h.Id}";

                // RequiredFlagValue defaults to true, so a *false* with no flag name is someone
                // reaching for "hide this once something happens" and getting a no-op: IsAvailable
                // skips the entire check when RequiredFlag is blank.
                if (!h.RequiredFlagValue && string.IsNullOrEmpty(h.RequiredFlag))
                    inertFlagGate.Add(where);

                if (h.Kind == HotspotKind.Harvest && string.IsNullOrEmpty(h.ItemId))
                    harvestNoItem.Add(where);

                if (h.Kind == HotspotKind.Navigate && string.IsNullOrEmpty(h.TargetRoomId))
                    navNowhere.Add(where);

                // Harmless, but it means someone wrote the flavour and forgot the cost.
                if (!h.IsHazard && !string.IsNullOrEmpty(h.HazardText))
                    costNoText.Add(where);
            }
        }

        Check($"no hotspot is blank or missing an Id{Listing(blank)}", blank.Count == 0);
        Check($"no hotspot gates on RequiredFlagValue with no RequiredFlag{Listing(inertFlagGate)}",
            inertFlagGate.Count == 0);
        Check($"every Harvest hotspot yields an item{Listing(harvestNoItem)}",
            harvestNoItem.Count == 0);
        Check($"every Navigate hotspot goes somewhere{Listing(navNowhere)}",
            navNowhere.Count == 0);
        Check($"no HazardText is stranded on a costless hotspot{Listing(costNoText)}",
            costNoText.Count == 0);
    }

    /// <summary>Appends the offenders to a check's description, so a failure says where to look.</summary>
    private static string Listing(List<string> offenders) =>
        offenders.Count == 0 ? "" : $"  [{string.Join(", ", offenders)}]";

    // ---- Plumbing --------------------------------------------------------

    private static void SetRealm(GameRealm realm) =>
        RealmTransitionManager.Instance.SetRealm(realm, force: true);

    /// <summary>Take the door from the current room to the named one.</summary>
    private static void Walk(string targetRoomId)
    {
        var room = NavigationManager.Instance.CurrentRoom;
        var door = room?.Hotspots.FirstOrDefault(
            h => h != null && h.Kind == HotspotKind.Navigate && h.TargetRoomId == targetRoomId);

        if (door == null)
        {
            Failures.Add($"[setup] no door from '{room?.Id}' to '{targetRoomId}'");
            return;
        }

        InteractionService.ActivateHotspot(door);
    }

    private static EntityPlacement Placement(string instanceId) =>
        NavigationManager.Instance.FindPlacement(instanceId);

    private static HotspotData Hotspot(string hotspotId)
    {
        var room = NavigationManager.Instance.CurrentRoom;
        var found = room?.Hotspots.FirstOrDefault(h => h != null && h.Id == hotspotId);
        if (found == null)
            Failures.Add($"[setup] no hotspot '{hotspotId}' in room '{room?.Id}'");
        return found;
    }

    private static bool IsAvailable(string hotspotId, GameRealm realm)
    {
        var room = NavigationManager.Instance.CurrentRoom;
        var data = room?.Hotspots.FirstOrDefault(h => h != null && h.Id == hotspotId);
        return data != null && InteractionService.IsAvailable(data, realm);
    }

    private static void Section(string title)
    {
        GD.Print("");
        GD.Print($"== {title} {new string('=', Math.Max(0, 58 - title.Length))}");
    }

    private static void Check(string description, bool condition)
    {
        _checks++;
        GD.Print($"  {(condition ? "ok  " : "FAIL")} {description}");
        if (!condition) Failures.Add(description);
    }
}
