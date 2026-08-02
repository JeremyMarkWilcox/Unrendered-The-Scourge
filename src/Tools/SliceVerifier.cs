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

        // A gate that throws used to take the whole run down with it, and not loudly: Godot's C#
        // layer logs the exception and keeps the loop alive, so `--verify-slice` sat at a prompt
        // until something killed it. A hang and a red look nothing alike to a person and identical
        // to a script. Twice in one day (2026-08-02) a broken fixture produced a hang instead of the
        // failure it should have — an empty spine reaching `.First()`, and a mistyped room id
        // leaving every downstream lookup null. So: catch, record it as a failure like any other,
        // and fall through to the summary. **The count will be short** when this fires, because the
        // gates after the throw never ran; that is why the message says so.
        try
        {
            RunGates();
        }
        catch (Exception e)
        {
            Failures.Add($"[crash] a gate threw and the rest never ran — {e.GetType().Name}: {e.Message}");
        }

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

    private static void RunGates()
    {
        GateHarness();
        GateM1_TwoScreenNavigation();
        GateM2_StatePresentationTable();
        GateM3_MicroLoop();
        GateVitalityPool();
        GateHeistMechanics();
        GatePersistence();

        // Between the two on purpose. It runs its own save round trip, which would land the player
        // somewhere GateCamps does not expect if it ran later — and it must not run *earlier* than
        // persistence, whose round trip would then be carrying wounds it never meant to assert.
        GateWoundLedger();

        // After persistence, because reaching a camp refills both pools and would clobber the
        // part-drained bar that gate saves. It leaves the player standing at a synthetic bench,
        // which is what lets the crafting gate below craft at all.
        GateCamps();

        // Late on purpose: this one churns the satchel, and persistence deliberately inherits the
        // reagents the micro-loop harvested. Running crafting in the middle wiped them, and that
        // gate caught it.
        GateCraftingAndUse();
        GateInventoryPanel();
        GateMultiFlagGating();
        GateChapterSpine();
        GateAuthoredContent();

        // Last, because it walks the player onto two synthetic screens and leaves them there, and
        // it toggles world flags that only its own fixture rooms read. Nothing after it to disturb.
        GatePlaceholderCues();
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

        // The boot room, asserted here because this is the last moment before a gate moves us. It
        // looks like a tautology and is not quite one: it asserts that `Main.StartGame` *arrived*,
        // catching a navigation that lands somewhere other than where it aimed. The harder case —
        // a starting room that will not load at all — never reaches this line, because `StartGame`
        // gives up before calling the verifier; that one is covered by the `Quit(1)` beside the
        // error it pushes, added the same day for the same reason.
        Check($"a new game begins in {GameConfig.StartingRoomId}",
            NavigationManager.Instance.CurrentRoomId == GameConfig.StartingRoomId);

        // And it must be a camp. Crafting, the cache and the respawn destination are all gated on
        // standing at a bench, and `CampManager` falls back to the starting room when you collapse
        // before finding one — so a boot room with no `Workbench` hotspot means a player who dies
        // early wakes somewhere they cannot make anything, with no way to bind a return point.
        Check("and the room it begins in is a camp",
            CampManager.IsCamp(NavigationManager.Instance.CurrentRoom));
    }

    // ---- M1: two screens and a doorway -----------------------------------

    private static void GateM1_TwoScreenNavigation()
    {
        Section("M1  two-screen prototype");

        // The boot moved to `ch1_cottage` on 2026-08-02 and every prototype gate below rode the old
        // one — M1, M2 and M3 all acted on "wherever the game put me" and silently meant the yard.
        // Walking there explicitly is the first half of the re-anchoring the retirement needs anyway:
        // a gate that names its fixture can have the fixture swapped under it, and a gate that
        // inherits it cannot. **Nothing below this line should read `GameConfig.StartingRoomId`.**
        NavigationManager.Instance.GoToRoom("waystation_yard");

        Check("the prototype yard is still walkable",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("starts in the Waking world",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("both Waking rooms are registered",
            NavigationManager.Instance.GetRoom("waystation_yard") != null &&
            NavigationManager.Instance.GetRoom("waystation_shed") != null);

        // Five checks stood here until 2026-08-02: Tab reaching the Pixel Realm and back, and three
        // on realm-scoped hotspots — the phantom door that existed only in the Dream, the carved
        // marker that existed only in the Waking world. All three of those authored hotspots and the
        // field they were scoped by are gone with the great swap, so the checks went with them
        // rather than being reworded. `yard_dream_door` was deleted from waystation_yard.tres too:
        // it was a hotspot with no reason to exist except to demonstrate the mechanism.
    }

    // ---- M2: state presentation, and the signals that carry it -----------

    private static void GateM2_StatePresentationTable()
    {
        Section("M2  state presentation table");

        var entities = EntityStateManager.Instance;
        var rat = Placement(Rat)?.Entity;

        Check("the rat was registered on first sight", entities.IsKnown(Rat));
        Check("the rat starts Corrupted", entities.GetState(Rat) == EntityState.Corrupted);

        // Form 1: a nightmare squatting in the road.
        var corrupt = entities.Resolve(rat, Rat);
        Check("Form 1 blocks the path", corrupt.BlocksPath);
        Check("Form 1 is named as the cursed beast", corrupt.Label == "Cursed Giant Rat");

        // Five Form 2 checks sat here — the porcelain Baron, its "Sever" verb, and the unattackable
        // Dream echo. Deleted 2026-08-02: `ResolveDream` is gone, so there is no second face to
        // compare against. Form 2 is not cancelled, it moved to the 3D pocket, and **it will need
        // its own checks there** — this comment is the note that this gate got smaller because
        // something was taken out of scope, not because it stopped mattering.

        // A signal must actually carry the change, not just the field.
        var heard = new List<string>();
        void Listener(string id, int state) => heard.Add($"{id}:{(EntityState)state}");
        EventBus.Instance.EntityStateChanged += Listener;
        entities.SetState(Rat, EntityState.Severed);
        EventBus.Instance.EntityStateChanged -= Listener;

        Check("EntityStateChanged fires on transition",
            heard.Contains($"{Rat}:{EntityState.Severed}"));

        var severed = entities.Resolve(rat, Rat);
        Check("Severed clears the road", !severed.BlocksPath);
        // The name is deliberately the same — it is still the cursed thing, just hollowed out — so
        // the only thing that distinguishes Form 1 from Form 1-deformed on screen is the wash.
        Check("and a severed body reads as drained", severed.Modulate != corrupt.Modulate);

        entities.SetState(Rat, EntityState.Purified);
        var purified = entities.Resolve(rat, Rat);
        Check("Form 3 is a plain animal in reality", purified.Label == "Field Rat");
        Check("Form 3 does not block", !purified.BlocksPath);

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
        Check("the bitterroot is not there yet", !IsAvailable("yard_bitterroot"));
        Check("the relic is hidden until the cairn is searched",
            !IsAvailable("yard_relic"));

        // 2. The problem is visible and cannot be solved from out here. This used to be phrased as
        // "you cannot sever by looking through spectral vision", flipping the realm first; the
        // refusal survives the deletion of the peek, because it was never the peek that refused —
        // it is that the anchor is not in this world. Kept, reworded, still falsifiable.
        Check("you cannot sever the anchor from the Waking world",
            !InteractionService.ActivateEntity(Rat));
        Check("the rat is still Corrupted", entities.GetState(Rat) == EntityState.Corrupted);

        // 3. The puzzle stands in for the entry minigame.
        Check("searching the cairn succeeds", InteractionService.ActivateHotspot(Hotspot("yard_cairn")));
        Check("the relic is now exposed", IsAvailable("yard_relic"));

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
            !IsAvailable("shed_gravecap"));
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
        Check("the bitterroot has appeared", IsAvailable("yard_bitterroot"));
        Check("it harvests", InteractionService.ActivateHotspot(Hotspot("yard_bitterroot")));
        Check("two bitterroot in the satchel", inv.GetCount("herb_bitterroot") == 2);

        var form3 = entities.Resolve(Placement(Rat).Entity, Rat);
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

        // A wound that keeps hitting a downed player must not announce it again. The clamp is
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

        // Being in the Dream must not, by itself, reorder or dim them: the HUD keys on a live run,
        // not on the realm. This was written against spectral vision (peeking flipped the realm with
        // no run behind it) and it outlives it, because unit 6's **quiet** pockets are the same
        // shape — squarely in the Dream, no clock, Resolve not moving, body still the pool at risk.
        // Forcing the realm with no run is exactly that situation, so the check keeps its teeth.
        SetRealm(GameRealm.Dream);
        Check("standing in the Dream with no run does not reorder them",
            _hud == null || _hud.TopPoolLabel == "VITALITY");
        Check("nor dim the body when nothing is hunting you",
            _hud == null || Mathf.IsEqualApprox(_hud.VitalityOpacity, 1f));
        SetRealm(GameRealm.Waking);

        // The debug wound key needs its action to exist in project.godot; a hand-edited input map
        // fails silently, and the symptom is a key that simply does nothing in play. It lived in
        // GateHazards until that gate was deleted on 2026-08-01 — F2 is now the only way to wound
        // the body outside a fight, so if anything it matters more here than it did there.
        Check("the debug wound key is bound", InputMap.HasAction("debug_wound"));

        // No odd value is handed downstream any more. This gate used to leave the bar at 64 for
        // GatePersistence to save and restore, and camps made that contract unsafe: any gate in
        // between that walks into a room Jeremy has since put a workbench in would silently refill
        // the bar, and persistence would go red over an authoring change. It sets its own value now.
        vitality.ResetAll();
    }

    // GateHazards lived here and was deleted 2026-08-01 with the hazard machinery it covered
    // (`combat.md` §22 — the Waking world no longer damages you). Fourteen checks went with it and
    // none of them were load-bearing for anything that survives: the collapse, `SurvivesDeath` and
    // the respawn are driven by `vitality.Damage(...)` directly in this gate and in GateCamps, which
    // is the question §22 asked to have answered before the field was touched. Its one check that
    // was not about hazards — the debug wound key binding — moved up into GateVitalityPool.

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

    // ---- The wound ledger --------------------------------------------------

    /// <summary>
    /// <c>CombatManager</c>: what a hit is worth, what a body has taken, and which of those two
    /// survives the engagement. No turn machine here — that is the next gate to be written.
    ///
    /// <para>Built on <see cref="EntityData"/> constructed in this method rather than on authored
    /// creatures, and for a stronger reason than the camps gate's: not one authored creature has a
    /// combat field set yet, so an authored-content version of this gate would be asserting
    /// arithmetic against <c>VulnerableTo = None</c> and would pass no matter what the resolver did.
    /// Synthetic bodies are the only way to assert the multiplier at all before the content pass.</para>
    ///
    /// <para>Every check below is written against <see cref="GameConfig"/>'s constants as
    /// <i>relationships</i> — vulnerable beats neutral beats resistant — rather than against the
    /// numbers themselves. Retuning 2× to 1.75× must not turn this section red; deleting the
    /// distinction must.</para>
    /// </summary>
    private static void GateWoundLedger()
    {
        Section("wounds  what a hit is worth, and what survives the fight");

        var combat = CombatManager.Instance;
        var states = EntityStateManager.Instance;

        Check("the manager is registered", combat != null);
        if (combat == null) return;

        combat.ResetAll();

        const string Body = "verify_body_01";
        const string Anchored = "verify_anchored_01";
        const string Cut = "verify_cut_01";
        const string Husk = "verify_husk_01";
        const string Named = "verify_named_01";
        const string Malformed = "verify_malformed_01";
        const int Potency = 10;

        var body = new EntityData
        {
            Id = "verify_body",
            CorruptedName = "Verification Body",
            Durability = 40,
            VulnerableTo = DamageType.Fire,
            ResistantTo = DamageType.Blunt,
            Deals = DamageType.Edge,
        };

        // -- What a hit is worth --
        var weak = CombatManager.DamageAgainst(DamageType.Fire, body, Potency);
        var plain = CombatManager.DamageAgainst(DamageType.Edge, body, Potency);
        var resisted = CombatManager.DamageAgainst(DamageType.Blunt, body, Potency);

        Check("the type it fears hurts more than one it feels nothing about", weak > plain);
        Check("and one it feels nothing about hurts more than the type it resists", plain > resisted);
        Check("a type it neither fears nor resists lands exactly as thrown", plain == Potency);

        // §7, and the reason CombatManager floors the result at 1: an absolute reads as a lock and
        // sends players hunting a key instead of asking whether the fight is worth what it costs. A
        // resisted hit that rounds to nothing is an absolute discovered at the low end of the scale,
        // where it looks like a bug rather than a rule.
        Check("the resisted type still lands for something, so half never reads as immune",
            CombatManager.DamageAgainst(DamageType.Blunt, body, 1) >= 1);

        Check("an untyped hit is not a hit at all",
            CombatManager.DamageAgainst(DamageType.None, body, Potency) == 0);
        Check("and neither is a hit with nothing behind it",
            CombatManager.DamageAgainst(DamageType.Fire, body, 0) == 0);

        // -- The ledger --
        Check("an untouched body carries no wounds", combat.WoundsOn(Body) == 0);
        Check("and is whole", combat.RemainingDurability(Body, body) == body.Durability);
        Check("and is not spent", !combat.IsSpent(Body, body));
        Check("and reads as unmarked", combat.ConditionOf(Body, body) == Condition.Unmarked);

        var dealt = combat.Wound(Body, body, DamageType.Fire, Potency);
        Check("a hit reports what it took off rather than the running total", dealt == weak);
        Check("and the ledger holds it", combat.WoundsOn(Body) == weak);
        Check("and what is left fell by exactly that much",
            combat.RemainingDurability(Body, body) == body.Durability - weak);
        Check("and one hit is not enough to be spent", !combat.IsSpent(Body, body));

        // The check above cannot tell "what it took off" from "the running total" — on a fresh ledger
        // those are the same number. A second hit is what separates them, and it is the one a fight
        // narrates wrongly if the manager returns the wrong one.
        var second = combat.Wound(Body, body, DamageType.Edge, Potency);
        Check("a second hit reports itself, not the running total", second == plain);
        Check("and the ledger is the sum of the two", combat.WoundsOn(Body) == weak + plain);

        // Overkill costs materials, never buys a bigger number (§5). An unclamped ledger would also
        // make the condition ladder depend on how hard the *last* hit was.
        combat.Wound(Body, body, DamageType.Fire, body.Durability * 10);
        Check("overkill is spent, not banked", combat.WoundsOn(Body) == body.Durability);
        Check("and what is left bottoms out at nothing",
            combat.RemainingDurability(Body, body) == 0);
        Check("and the body is spent", combat.IsSpent(Body, body));

        // -- The ladder: monotonic, total, and never wordless --
        // Point by point across the whole bar, because the claim is about every wound total a fight
        // can produce and not about the four the thresholds happen to name.
        var backwards = new List<string>();
        var wordless = new List<string>();
        var rungs = new HashSet<Condition>();
        var worst = Condition.Unmarked;

        for (var taken = 0; taken <= body.Durability; taken++)
        {
            combat.ResetAll();
            if (taken > 0) combat.Wound(Body, body, DamageType.Edge, taken);   // neutral: exact

            var band = combat.ConditionOf(Body, body);
            if (band < worst) backwards.Add($"{taken}/{body.Durability}: {worst}->{band}");
            worst = band;
            rungs.Add(band);

            if (string.IsNullOrWhiteSpace(combat.DescribeCondition(Body, body)))
                wordless.Add($"{taken}/{body.Durability}");
        }

        Check($"the ladder only ever gets worse as damage lands{Listing(backwards)}",
            backwards.Count == 0);
        Check($"and every point of damage on the bar has a word for it{Listing(wordless)}",
            wordless.Count == 0);
        Check("and the far end of it is collapsing", worst == Condition.Collapsing);

        // A rung no wound total can reach is a word the player will never read, which means a
        // threshold has collided with its neighbour. Distinct from the tuning of the thresholds
        // themselves, which is deliberately ungated.
        Check($"and no rung of the ladder is unreachable ({rungs.Count} of "
            + $"{Enum.GetValues<Condition>().Length} reached)",
            rungs.Count == Enum.GetValues<Condition>().Length);

        // -- Words are overridable; bands are not --
        var named = new EntityData { Id = "verify_named", Durability = body.Durability };
        foreach (var w in new[] { "uncracked", "hairlined", "the crown is cracking", "coming apart" })
            named.ConditionWords.Add(w);

        // Two entries where four are the only count that can be right. It is a content-sweep failure
        // (GateCombatShapes), and the fallback here is what keeps the *game* readable meanwhile.
        var malformed = new EntityData { Id = "verify_malformed", Durability = body.Durability };
        malformed.ConditionWords.Add("cracked");
        malformed.ConditionWords.Add("crazed");

        combat.ResetAll();
        combat.Wound(Body, body, DamageType.Edge, 30);
        combat.Wound(Named, named, DamageType.Edge, 30);

        Check("a creature's own vocabulary replaces the shared one",
            combat.DescribeCondition(Named, named) == "the crown is cracking");
        Check("and is not the words anything else would have used",
            combat.DescribeCondition(Named, named) != combat.DescribeCondition(Body, body));
        Check("but it does not move the band those words describe",
            combat.ConditionOf(Named, named) == combat.ConditionOf(Body, body));

        // A short array is ignored *entirely*, not honoured as far as it reaches. Probed at both
        // ends for that reason: the out-of-range rung would fall back under any implementation that
        // merely avoids crashing, so it is the in-range rung that distinguishes "ignored" from
        // "partly believed" — and partly believed is the version that ships a fight describing a
        // dying creature with the word for a scratched one.
        var partly = new List<string>();
        foreach (var taken in new[] { 15, 38 })
        {
            combat.ResetAll();
            combat.Wound(Body, body, DamageType.Edge, taken);
            combat.Wound(Malformed, malformed, DamageType.Edge, taken);

            var got = combat.DescribeCondition(Malformed, malformed);
            if (got != combat.DescribeCondition(Body, body)) partly.Add($"{taken}: \"{got}\"");
        }

        Check($"a wrong-length vocabulary is ignored outright, at both ends of it{Listing(partly)}",
            partly.Count == 0);

        // -- What survives the engagement --
        // The asymmetry is the whole reward for severing. If damage reset in both cases, severing
        // would only remove regeneration from inside a fight and the design's two routes would
        // collapse into one.
        combat.ResetAll();
        states.SetState(Anchored, EntityState.Corrupted);
        states.SetState(Husk, EntityState.Severed);
        states.SetState(Cut, EntityState.Corrupted);

        combat.Wound(Anchored, body, DamageType.Edge, 20);
        combat.Wound(Husk, body, DamageType.Edge, 20);
        combat.Wound(Cut, body, DamageType.Edge, 20);

        Check("wounds stand while the fight is still on",
            combat.WoundsOn(Anchored) == 20 && combat.WoundsOn(Husk) == 20);

        // Severed *after* the damage was done. The state has to be read when the fight ends, not when
        // the hit lands, or cutting the anchor mid-fight would throw away the work that got you there.
        states.Sever(Cut);

        combat.SettleEngagement(Anchored);
        combat.SettleEngagement(Husk);
        combat.SettleEngagement(Cut);

        Check("an anchored body closes its wounds when the engagement ends",
            combat.WoundsOn(Anchored) == 0);
        Check("and a severed one keeps them, so cleanup can be spread across visits",
            combat.WoundsOn(Husk) == 20);
        Check("and severing mid-fight keeps what the fight already cost",
            combat.WoundsOn(Cut) == 20);

        // -- Persistence --
        // §13's save-format question. Only the severed half can ever reach disk, because the anchored
        // half is already gone by the time an engagement is over.
        const string path = "user://verify_wounds.json";

        Check("a save carrying wounds writes", SaveService.Save(path));

        combat.ResetAll();
        Check("the ledger was actually cleared", combat.WoundsOn(Husk) == 0);

        Check("and the save reads back", SaveService.Load(path));
        Check("a severed body's wounds survive the file", combat.WoundsOn(Husk) == 20);
        Check("and an anchored body's do not, because they never reached disk",
            combat.WoundsOn(Anchored) == 0);

        SaveService.DeleteSave(path);
        combat.ResetAll();
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

        // -- Invariant 2: the bench is off inside a run, not merely inside the Dream --
        // The distinction matters because unit 6's quiet pockets put you in the Dream with no run,
        // and a camp reached from one must still be a camp. So the realm alone must not close it;
        // only the live run below does.
        nav.GoToRoom("verify_camp");
        SetRealm(GameRealm.Dream);
        Check("the realm alone does not close the bench", camp.AtCamp);
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

        // The two categories added on 2026-07-30 fall on the destructive side, and that is the
        // ratified decision rather than the default arm happening to catch them: Jeremy's reason for
        // cutting durability was that "losing items on death will make it so people may have to
        // recraft, making his trade more impactful than one and done". Exempt equipment here and that
        // reason evaporates — the recraft pressure is the *only* pressure left on the smithing side.
        Check("a trap does not survive a collapse",
            !InventoryCraftingManager.SurvivesDeath(ItemCategory.Trap));
        Check("nor does the gear you were wearing",
            !InventoryCraftingManager.SurvivesDeath(ItemCategory.Equipment));

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

        nav.DebugRegisterRoom(CueRoom("verify_cues", painted: false));
        nav.DebugRegisterRoom(CueRoom("verify_cues_painted", painted: true));
    }

    /// <summary>
    /// A fixture for <see cref="GatePlaceholderCues"/>, holding one of each case the layer has to
    /// tell apart: an open hotspot, a door, a hotspot shut by a flag, and a harvest node already
    /// taken. Built in code rather than authored because the *point* of it is a shut gate and a
    /// spent node existing simultaneously in one room, which no authored room usefully does.
    ///
    /// <para>
    /// The painted variant carries a 4x4 texture. Nothing looks at the pixels — it exists so the
    /// "cues switch themselves off when art lands" promise is checked against
    /// <c>WakingBackground != null</c>, which is the actual condition the code branches on.
    /// </para>
    /// </summary>
    private static RoomData CueRoom(string id, bool painted)
    {
        var room = new RoomData
        {
            Id = id,
            DisplayName = painted ? "A Painted Screen" : "An Unpainted Screen",
            ChapterId = "verify",
        };

        if (painted)
            room.WakingBackground = ImageTexture.CreateFromImage(
                Image.CreateEmpty(4, 4, false, Image.Format.Rgba8));

        room.Hotspots.Add(new HotspotData
        {
            Id = $"{id}_door",
            Label = "A Way Out",
            Kind = HotspotKind.Navigate,
            TargetRoomId = "verify_wilds",
        });

        room.Hotspots.Add(new HotspotData
        {
            Id = $"{id}_open",
            Label = "Something Open",
            Kind = HotspotKind.Examine,
            ExamineText = "verify: scenery.",
        });

        room.Hotspots.Add(new HotspotData
        {
            Id = $"{id}_shut",
            Label = "Something Shut",
            Kind = HotspotKind.Harvest,
            ItemId = "herb_bitterroot",
            ItemAmount = 1,
            RequiredFlag = CueShutFlag,
        });

        room.Hotspots.Add(new HotspotData
        {
            Id = $"{id}_spent",
            Label = "Something Already Taken",
            Kind = HotspotKind.Harvest,
            ItemId = "herb_bitterroot",
            ItemAmount = 1,
            HarvestedFlag = CueSpentFlag,
        });

        return room;
    }

    private const string CueShutFlag = "verify.cues.opened";
    private const string CueSpentFlag = "verify.cues.taken";

    /// <summary>
    /// A room built in code for <see cref="GateCamps"/>. Kept clean enough to pass the authoring
    /// sweep on its own merits even though that gate skips it — a helper that could only pass by
    /// being excluded would be worth distrusting.
    /// </summary>
    private static RoomData VerifyRoom(string id, string displayName, bool withBench)
    {
        var room = new RoomData { Id = id, DisplayName = displayName, ChapterId = "verify" };

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

    /// <summary>
    /// Sweeps every registered room and checks the *authoring*, not the engine. Jeremy owns content
    /// (see handoff.md, "Who does what"), so this is the safety net under that: it catches fields
    /// that look set but do nothing, which the inspector will never warn about and playing rarely
    /// reveals.
    ///
    /// <para>Deliberately **value-agnostic**. It never asserts a particular cost, label or room,
    /// because those are his to retune and a gate that fights the author is a gate that gets
    /// deleted. It asserts shapes and relationships: that somewhere is craftable-at and walkable-to,
    /// and that no hotspot is quietly inert.</para>
    ///
    /// <para>Nothing here activates anything — an arbitrary authored hotspot could navigate, or
    /// start a heist.</para>
    ///
    /// <para>This comment was detached from the method and stranded above the inventory-panel gate
    /// until 2026-08-01; it also promised a hazard sweep that was deleted with the field
    /// (`combat.md` §22). <b>This is the gate that grows into the bill checker</b> — the placement
    /// audit's fifteen rules are almost all relationships between two authored values, which is
    /// exactly what belongs here. See `docs/build-plan.md` §6.</para>
    /// </summary>
    private static void GateAuthoredContent()
    {
        Section("content  the authoring, swept for silent mistakes");

        // GateCamps registers synthetic camps into the same registry. They are not authored content,
        // so sweeping them would point any failure at a room Jeremy cannot open in the editor.
        var rooms = NavigationManager.Instance.Rooms
            .Where(kv => !kv.Key.StartsWith("verify_"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Check("authored rooms are registered", rooms.Count > 0);

        // Two checks stood here until 2026-08-01 — that a hazard was authored somewhere, and that at
        // least one was reachable in the Waking world rather than stranded behind spectral vision.
        // Both went with the field (`combat.md` §22). **The pair is the model for the blight sweep**:
        // one check that the content type exists at all, and one that it is not authored into a place
        // where the engine will silently refuse it.

        // -- Somewhere to craft, and a way to get there --
        // Crafting, the shared cache and the respawn point are all gated on standing at a camp, and a
        // camp is a room holding a Workbench hotspot — nothing else. So a world with no bench in it
        // has no crafting *at all*, and no gate below would notice: every crafting rule would keep
        // passing on the synthetic verify_ camps while the real game had nowhere to make anything.
        var camps = rooms.Where(kv => CampManager.IsCamp(kv.Value)).Select(kv => kv.Key).ToList();
        Check($"at least one authored room is a camp{Listing(camps)}", camps.Count > 0);

        // And it has to be walkable to. Flag-gated doors count as open here — a bench behind a lock
        // you have to earn is design; a bench in a room with no door into it is a typo.
        var reached = new HashSet<string> { GameConfig.StartingRoomId };
        var queue = new Queue<string>(reached);
        while (queue.Count > 0)
        {
            if (!rooms.TryGetValue(queue.Dequeue(), out var room)) continue;
            foreach (var h in room.Hotspots)
            {
                if (h is not { Kind: HotspotKind.Navigate }) continue;
                if (!string.IsNullOrEmpty(h.TargetRoomId) && reached.Add(h.TargetRoomId))
                    queue.Enqueue(h.TargetRoomId);
            }
        }

        Check("and you can walk to one from where the game starts",
            camps.Any(reached.Contains));

        // -- Fields that look set but do nothing --
        var blank = new List<string>();
        var inertFlagGate = new List<string>();
        var harvestNoItem = new List<string>();
        var navNowhere = new List<string>();
        var blankRequired = new List<string>();
        var dupRequired = new List<string>();
        var doubleGated = new List<string>();
        var examineSilent = new List<string>();

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

                // A blank entry in RequiredFlags holds the gate shut forever (IsAvailable treats it
                // as unmet on purpose), so it fails closed rather than open — but it is still a
                // door nobody can ever walk through, which no ledger row ever wants.
                if (h.RequiredFlags != null)
                {
                    var seen = new HashSet<string>();
                    foreach (var flag in h.RequiredFlags)
                    {
                        if (string.IsNullOrEmpty(flag)) { blankRequired.Add(where); continue; }

                        // The same fact stored twice, in two fields able to disagree.
                        if (flag == h.RequiredFlag) doubleGated.Add($"{where}: {flag}");
                        if (!seen.Add(flag)) dupRequired.Add($"{where}: {flag}");
                    }
                }

                if (h.Kind == HotspotKind.Harvest && string.IsNullOrEmpty(h.ItemId))
                    harvestNoItem.Add(where);

                if (h.Kind == HotspotKind.Navigate && string.IsNullOrEmpty(h.TargetRoomId))
                    navNowhere.Add(where);

                // The Examine equivalent of the two above. `InteractionService` guards on empty text
                // and emits nothing, so a wordless Examine is a region that swallows a click and
                // gives no feedback whatsoever — indistinguishable in play from a dead hotspot.
                if (h.Kind == HotspotKind.Examine && string.IsNullOrEmpty(h.ExamineText))
                    examineSilent.Add(where);
            }
        }

        Check($"no hotspot is blank or missing an Id{Listing(blank)}", blank.Count == 0);
        Check($"no hotspot gates on RequiredFlagValue with no RequiredFlag{Listing(inertFlagGate)}",
            inertFlagGate.Count == 0);
        Check($"no blank entry in a hotspot's RequiredFlags{Listing(blankRequired)}",
            blankRequired.Count == 0);
        Check($"no flag listed twice in one hotspot's RequiredFlags{Listing(dupRequired)}",
            dupRequired.Count == 0);
        Check($"no flag in both RequiredFlag and RequiredFlags{Listing(doubleGated)}",
            doubleGated.Count == 0);
        Check($"every Harvest hotspot yields an item{Listing(harvestNoItem)}",
            harvestNoItem.Count == 0);
        Check($"every Navigate hotspot goes somewhere{Listing(navNowhere)}",
            navNowhere.Count == 0);
        Check($"every Examine hotspot says something{Listing(examineSilent)}",
            examineSilent.Count == 0);

        GateWorldGraph(rooms);
        GateCombatShapes(rooms);
        GateFights(rooms);
    }

    /// <summary>
    /// The world as a <i>graph</i>, rather than as a bag of rooms — added 2026-08-02 with the 59
    /// pass-1 skeleton screens (<c>build-plan.md</c> §5).
    ///
    /// <para>
    /// The sweep above asks whether each hotspot is internally coherent. None of it can see the
    /// failure that actually matters once there are dozens of screens: a door whose target is spelled
    /// slightly wrong, a branch nothing connects to, two harvest nodes that turn out to be the same
    /// node. Those are invisible in the inspector and invisible in a single room, and they are exactly
    /// what a generator gets wrong at scale — which is why the gate lands with the generated content
    /// and not after it. <b>Never hand-edit a <c>.tres</c> typed array without a gate asserting the
    /// contents loaded</b>; this is that gate for <c>RoomData.Hotspots</c>.
    /// </para>
    /// </summary>
    private static void GateWorldGraph(Dictionary<string, RoomData> rooms)
    {
        Section("graph  the world as doors, not as rooms");

        // -- Every file on disk actually became a room --
        // The blind spot this gate was born from, on the day the 59 skeletons landed. A `.tres` with a
        // syntax error does not crash and does not fail anything: `GD.Load` returns null,
        // `LoadRoomRegistry` pushes a warning into a console nobody reads headlessly, and the room is
        // simply *absent*. Every sweep in this file iterates the registry, so all of them keep passing
        // — the suite went green on a run where none of the 59 had loaded. Counting the directory is
        // the only check here that does not trust the registry it is checking.
        var onDisk = 0;
        using (var dir = DirAccess.Open(GameConfig.RoomDataDir))
        {
            if (dir != null)
            {
                dir.ListDirBegin();
                for (var f = dir.GetNext(); f != ""; f = dir.GetNext())
                    if (!dir.CurrentIsDir() && (f.EndsWith(".tres") || f.EndsWith(".tres.remap")))
                        onDisk++;
            }
        }

        Check($"every room file on disk registered a room ({rooms.Count} of {onDisk})",
            onDisk > 0 && rooms.Count == onDisk);

        // -- Doors that lead nowhere real --
        // `navNowhere` above only asks whether the string is blank. A *populated* target that names no
        // registered room is the far likelier mistake and the far worse symptom: the hotspot draws, it
        // takes the click, and nothing happens. Silent in the editor, silent in the log.
        var brokenDoor = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in Doors(room))
                if (!rooms.ContainsKey(h.TargetRoomId))
                    brokenDoor.Add($"{roomId}/{h.Id} -> {h.TargetRoomId}");

        Check($"every door names a room that exists{Listing(brokenDoor)}", brokenDoor.Count == 0);

        // -- Two nodes that are secretly one node --
        // `Flags.Harvested` keys off the hotspot Id alone, so two nodes sharing an Id share a flag:
        // pick one and the other vanishes a chapter away, having yielded nothing. Ids are scoped by
        // room in the editor and by nothing at all here, which is the trap.
        var seenHarvest = new Dictionary<string, string>();
        var collided = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
            {
                if (h is not { Kind: HotspotKind.Harvest } || string.IsNullOrEmpty(h.Id)) continue;
                if (!seenHarvest.TryAdd(h.Id, roomId))
                    collided.Add($"{h.Id}: {seenHarvest[h.Id]} + {roomId}");
            }

        Check($"no two harvest nodes share an Id, and so a flag{Listing(collided)}",
            collided.Count == 0);

        // -- The chapter spine --
        // Scoped to `ch*` chapters because the waystation and its Dream graph are the retiring
        // prototype and were never authored to these rules. Everything below is about the ledger.
        var spine = rooms
            .Where(kv => kv.Value?.ChapterId?.StartsWith("ch") == true)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Check("the chapter spine is authored", spine.Count > 0);

        // Every door can be walked back through. A one-way door in a point-and-click is a softlock
        // waiting for a player who forgot something, and nothing in any ledger asks for one — so if
        // this ever goes red it is either a typo or a decision that should be written down first.
        var oneWay = new List<string>();
        foreach (var (roomId, room) in spine)
            foreach (var h in Doors(room))
            {
                if (!spine.TryGetValue(h.TargetRoomId, out var target)) continue;
                if (!Doors(target).Any(back => back.TargetRoomId == roomId))
                    oneWay.Add($"{roomId} -> {h.TargetRoomId}");
            }

        Check($"every door in the spine can be walked back through{Listing(oneWay)}",
            oneWay.Count == 0);

        // One component, all three chapters. This is what proves the two chapter seams exist: drop
        // the bridge or the cart road and ch2/ch3 fall off in a single named list. Started from an
        // arbitrary room rather than a hard-coded entry id, because the property being asserted is
        // connectedness, and pinning a start would make the gate a second place to keep in sync.
        var start = spine.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        var reached = new HashSet<string> { start };
        var queue = new Queue<string>(reached);
        while (queue.Count > 0)
        {
            if (!spine.TryGetValue(queue.Dequeue(), out var room)) continue;
            foreach (var h in Doors(room))
                if (spine.ContainsKey(h.TargetRoomId) && reached.Add(h.TargetRoomId))
                    queue.Enqueue(h.TargetRoomId);
        }

        var stranded = spine.Keys.Where(k => !reached.Contains(k)).OrderBy(k => k).ToList();
        Check($"and the whole spine is one connected world{Listing(stranded)}", stranded.Count == 0);

        // -- The unwritten marker, and where it must never survive to --
        // Dormant by design, exactly like GateCombatShapes: no screen is painted yet, so this is zero
        // offenders today. The point of writing it now is that pass 3 hands art and prose to different
        // sittings, and the failure it catches — a finished screen still emitting `[unwritten]` when
        // clicked — is one nobody would find except by clicking every hotspot in the game.
        var unwrittenOnArt = new List<string>();
        foreach (var (roomId, room) in rooms)
        {
            if (room?.WakingBackground == null) continue;
            foreach (var h in room.Hotspots)
                if (h != null && h.ExamineText.Contains(UnwrittenMark))
                    unwrittenOnArt.Add($"{roomId}/{h.Id}");
        }

        Check($"no painted screen still reads {UnwrittenMark}{Listing(unwrittenOnArt)}",
            unwrittenOnArt.Count == 0);
    }

    /// <summary>The sentinel pass 1 leaves in every Examine stub, for pass 3 to replace.</summary>
    private const string UnwrittenMark = "[unwritten]";

    /// <summary>
    /// A room's usable exits. Navigate hotspots with a target, availability ignored — a flag-gated
    /// door is design and still an edge in the graph; a blank one is already reported by the sweep
    /// above and would otherwise be counted twice here.
    /// </summary>
    private static IEnumerable<HotspotData> Doors(RoomData room) =>
        (room?.Hotspots ?? new Godot.Collections.Array<HotspotData>())
        .Where(h => h is { Kind: HotspotKind.Navigate } && !string.IsNullOrEmpty(h.TargetRoomId));

    /// <summary>
    /// The same sweep, over the combat fields added on 2026-07-30 (docs/combat.md §18).
    ///
    /// <para>These are <b>dormant</b> today: no item or creature has a combat field authored yet, so
    /// every one of them passes at zero offenders. That is the point of writing them now — the fields
    /// exist precisely so authoring can begin, and a sweep added *after* the content pass would be
    /// read against work already done rather than catching it as it lands.</para>
    ///
    /// <para>Every check here is a <i>relationship</i> between two authored values, never a value. A
    /// gate on <c>Durability</c> or <c>Damage</c> would turn tuning red, which is how a suite teaches
    /// people to stop running it.</para>
    /// </summary>
    private static void GateCombatShapes(Dictionary<string, RoomData> rooms)
    {
        var equipNoSlot = new List<string>();
        var slotNotEquip = new List<string>();
        var armedWithNothing = new List<string>();
        var wardNotArmour = new List<string>();

        foreach (var (id, item) in InventoryCraftingManager.Instance.Catalogue)
        {
            if (item == null) continue;

            // Category says worn, slot says nowhere. Completely dead in play rather than merely odd:
            // §18 sends equipment down UseBlock.NotUsable in the satchel *because* it is worn instead,
            // so an unwearable piece of equipment has no remaining way to be interacted with at all.
            if (item.Category == ItemCategory.Equipment && item.Slot == EquipSlot.None)
                equipNoSlot.Add(id);

            // The mirror: a slot on something the equip screen will never list.
            if (item.Slot != EquipSlot.None && item.Category != ItemCategory.Equipment)
                slotNotEquip.Add(id);

            // A weapon or a trap whose whole purpose is a damage type, with no damage type. Traps are
            // consumed win or lose (§13), so authoring an inert one spends the player's materials on
            // nothing and looks like a resolver bug from the outside.
            var mustDeal = item.Category == ItemCategory.Trap
                || (item.Category == ItemCategory.Equipment && item.Slot == EquipSlot.Weapon);
            if (mustDeal && item.Deals == DamageType.None) armedWithNothing.Add(id);

            // Warding is the armour half of the pair. On a weapon it is a field someone filled in
            // hopefully, and nothing reads it.
            if (item.WardsAgainst != DamageType.None && item.Slot != EquipSlot.Armour)
                wardNotArmour.Add(id);
        }

        Check($"no equipment is authored without a slot to wear it in{Listing(equipNoSlot)}",
            equipNoSlot.Count == 0);
        Check($"no non-equipment claims an equip slot{Listing(slotNotEquip)}",
            slotNotEquip.Count == 0);
        Check($"every weapon and trap deals some kind of harm{Listing(armedWithNothing)}",
            armedWithNothing.Count == 0);
        Check($"nothing but armour wards against a damage type{Listing(wardNotArmour)}",
            wardNotArmour.Count == 0);

        // -- Creatures, reached through their placements: there is no EntityData registry --
        var bothWays = new List<string>();
        var noAnswer = new List<string>();
        var wrongWordCount = new List<string>();
        var blankWord = new List<string>();
        var seen = new HashSet<string>();

        var rungs = Enum.GetValues<Condition>().Length;

        foreach (var (_, room) in rooms)
        {
            foreach (var placement in room.Entities)
            {
                var e = placement?.Entity;
                if (e == null || !seen.Add(e.Id)) continue;

                // Double and half at once. The resolver would pick one arm and the fight would quietly
                // behave as though the other field were blank.
                if (e.VulnerableTo != DamageType.None && e.VulnerableTo == e.ResistantTo)
                    bothWays.Add(e.Id);

                // Something that fights back but has nothing it is weak to. §7 promises the confirm
                // step names what the creature is weak to and what it deals, so this is an unanswerable
                // fight dressed as an answerable one — and EntityData.VulnerableTo says why it must be
                // set on *every* creature rather than only the armoured ones.
                if (e.Deals != DamageType.None && e.VulnerableTo == DamageType.None)
                    noAnswer.Add(e.Id);

                // Empty means "use the shared vocabulary"; anything other than one word per rung is a
                // typo. Counted against the enum rather than a literal four, so adding a rung to
                // Condition makes this sweep demand the new word instead of quietly accepting the old
                // length. CombatManager falls back to prose so play stays readable — the complaint
                // belongs here, not in the fight.
                var words = e.ConditionWords;
                if (words == null || words.Count == 0) continue;

                if (words.Count != rungs) wrongWordCount.Add($"{e.Id}({words.Count} of {rungs})");
                else if (words.Any(string.IsNullOrWhiteSpace)) blankWord.Add(e.Id);
            }
        }

        Check($"no creature is both vulnerable and resistant to the same type{Listing(bothWays)}",
            bothWays.Count == 0);
        Check($"every creature that hits back has something it is weak to{Listing(noAnswer)}",
            noAnswer.Count == 0);
        Check($"an overridden condition ladder has one word per rung{Listing(wrongWordCount)}",
            wrongWordCount.Count == 0);
        Check($"and none of those words is blank{Listing(blankWord)}", blankWord.Count == 0);
    }

    /// <summary>
    /// The bodies on the map — added 2026-08-02 with pass 2, which placed the 30 regular fights, the
    /// person each of the 24 human ones frees, and the carcass each of the 6 Beasts leaves.
    ///
    /// <para>
    /// <see cref="GateCombatShapes"/> already sweeps each creature's own fields. Nothing there can see
    /// the failures that only exist once creatures are <i>placed</i>: two placements sharing an
    /// instance id and therefore a flag, a freed person whose flag is misspelled so they never appear,
    /// a Warden that blocks nothing, or two of the same creature on adjoining screens. Those are
    /// invisible in the inspector and invisible in any single room.
    /// </para>
    ///
    /// <para>
    /// Every check is a <b>relationship</b> between two authored values, and none of them counts. A
    /// gate asserting 30 placements would go red the day unit 6 adds the three bosses, which is how a
    /// suite teaches people to stop running it — so the totals are printed and not asserted.
    /// </para>
    /// </summary>
    private static void GateFights(Dictionary<string, RoomData> rooms)
    {
        Section("fights  the bodies on the map, and what each one leaves behind");

        // -- The placement equivalent of "no hotspot is blank or missing an Id" --
        // Replacing an array element with a fresh resource in the inspector does exactly this, and a
        // placement with no Entity draws nothing at all: the screen simply reads as empty, with no
        // warning anywhere, which is indistinguishable from a screen nobody has authored yet.
        var blank = new List<string>();
        var noEntity = new List<string>();
        var collided = new List<string>();
        var placedIn = new Dictionary<string, string>();

        foreach (var (roomId, room) in rooms)
        {
            var placements = room?.Entities;
            if (placements == null) continue;

            for (var i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (p == null || string.IsNullOrEmpty(p.InstanceId)) { blank.Add($"{roomId}[{i}]"); continue; }
                if (p.Entity == null) noEntity.Add($"{roomId}/{p.InstanceId}");

                // Entity state is keyed by instance id alone, so two placements sharing one are one
                // creature standing in two rooms: beat it once and the other screen clears itself.
                // The exact shape of the harvest-id collision the graph section guards.
                if (!placedIn.TryAdd(p.InstanceId, roomId))
                    collided.Add($"{p.InstanceId}: {placedIn[p.InstanceId]} + {roomId}");
            }
        }

        Check($"no placement is blank or missing an InstanceId{Listing(blank)}", blank.Count == 0);
        Check($"every placement names an entity{Listing(noEntity)}", noEntity.Count == 0);
        Check($"no two placements share an InstanceId, and so a state{Listing(collided)}",
            collided.Count == 0);

        // -- Twelve creatures because four damage types give twelve pairs (items.md §10) --
        // Two creatures answering to the same tool and resisting the same tool are one creature in two
        // costumes, so a repeated pair means either a copy-paste or a roster that has outgrown the
        // matrix. The Beast's three variants deliberately share a pair, and share it by *design* —
        // they are keyed by entity id, so they are three placements of three files with one pair, and
        // this check has to allow that. It compares pairs across ids that differ in more than a
        // variant suffix, which is why the Beast files are named `beast_*` and the humans `creature_*`.
        var pairs = new Dictionary<string, string>();
        var repeated = new List<string>();
        foreach (var e in PlacedEntities(rooms))
        {
            if (e.VulnerableTo == DamageType.None || e.ResistantTo == DamageType.None) continue;
            if (e.Id.StartsWith("beast_")) continue;

            var key = $"{e.VulnerableTo}/{e.ResistantTo}";
            if (!pairs.TryAdd(key, e.Id)) repeated.Add($"{key}: {pairs[key]} + {e.Id}");
        }

        Check($"no two creatures answer to the same pair of types{Listing(repeated)}",
            repeated.Count == 0);

        // -- placement.md rule 2: never the same creature on two adjacent screens --
        // Newly checkable, because this is the first pass where placements and the door graph both
        // exist. It is the one rule in §5 that is a property of the *map* rather than of a file, so it
        // could not be gated at all before today and had to be held in someone's head.
        var adjacent = new List<string>();
        var reported = new HashSet<string>();
        foreach (var (roomId, room) in rooms)
        {
            if (room?.ChapterId?.StartsWith("ch") != true) continue;

            foreach (var h in Doors(room))
            {
                if (!rooms.TryGetValue(h.TargetRoomId, out var target)) continue;
                if (target?.ChapterId?.StartsWith("ch") != true) continue;

                foreach (var here in Species(room))
                    if (Species(target).Contains(here))
                    {
                        // One complaint per pair of screens, whichever direction it is found from.
                        var edge = string.CompareOrdinal(roomId, h.TargetRoomId) < 0
                            ? $"{here}: {roomId} + {h.TargetRoomId}"
                            : $"{here}: {h.TargetRoomId} + {roomId}";
                        if (reported.Add(edge)) adjacent.Add(edge);
                    }
            }
        }

        Check($"no creature stands on two adjoining screens{Listing(adjacent)}", adjacent.Count == 0);

        // -- What a fight leaves behind --
        // A freed person and a Beast carcass are the same machinery: a hotspot the room already holds,
        // gated on a flag the fight sets. So the failure mode is a *flag* failure, and a misspelled
        // flag is silent in the worst way — the hotspot is drawn dimmed exactly as an unearned one is,
        // so the screen looks correct until someone wins the fight and nobody appears.
        var danglingFlag = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
            {
                if (h == null) continue;
                foreach (var instance in AftermathOf(h))
                    if (!placedIn.ContainsKey(instance))
                        danglingFlag.Add($"{roomId}/{h.Id} -> {instance}");
            }

        Check($"every freed / defeated gate names a creature that is placed{Listing(danglingFlag)}",
            danglingFlag.Count == 0);

        // The coverage half, and it is keyed on the data rather than on a list of names: a creature
        // with a `PurifiedName` is one there is a person left to recover, so it owes a freed hotspot.
        // A blank `PurifiedName` means there is nobody in there — the Beast, and the bosses, which are
        // destroyed and never purified — so those owe nothing and this stays quiet when unit 6 places
        // a boss in a room with no aftermath at all.
        //
        // Scoped to `ch*` for the same reason the spine checks in GateWorldGraph are: the waystation
        // is the retiring prototype and was authored to the *old* model, where a purified creature was
        // farmable wildlife reached through EntityState rather than a person reached through a hotspot.
        // `waystation.rat_01` therefore has a PurifiedName and owes nobody, and it is the one placement
        // in the game that legitimately fails this. It goes away with the retirement, not before.
        var noOneToFree = new List<string>();
        foreach (var (roomId, room) in rooms)
        {
            if (room?.ChapterId?.StartsWith("ch") != true) continue;

            var freed = room.Hotspots
                .Where(h => h != null)
                .SelectMany(AftermathOf)
                .ToHashSet();

            foreach (var p in room.Entities)
            {
                if (p?.Entity == null || string.IsNullOrEmpty(p.InstanceId)) continue;
                if (string.IsNullOrEmpty(p.Entity.PurifiedName)) continue;
                if (!freed.Contains(p.InstanceId)) noOneToFree.Add($"{roomId}/{p.InstanceId}");
            }
        }

        Check($"every creature with someone inside it leaves them behind{Listing(noOneToFree)}",
            noOneToFree.Count == 0);

        // -- Wardens, and the trap that EntityData.BlocksPath defaults to true --
        // Stated as a relationship in both directions rather than as "only the Stable Master blocks",
        // which would write one chapter's content into the suite. The direction that matters is the
        // first: forget `BlocksPath = false` on a generated creature and *every* screen it stands on
        // becomes a dead end, which is the single most expensive silent mistake available in this pass.
        var blocksNothing = new List<string>();
        var guardedByAPasser = new List<string>();

        var wardens = new HashSet<string>();
        foreach (var (_, room) in rooms)
            foreach (var p in room.Entities)
                if (p?.Entity is { BlocksPath: true } && !string.IsNullOrEmpty(p.InstanceId))
                    wardens.Add(p.InstanceId);

        var guards = new HashSet<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
            {
                if (h == null || string.IsNullOrEmpty(h.BlockedByEntityId)) continue;
                guards.Add(h.BlockedByEntityId);

                if (!wardens.Contains(h.BlockedByEntityId))
                    guardedByAPasser.Add($"{roomId}/{h.Id} -> {h.BlockedByEntityId}");
            }

        foreach (var warden in wardens.Where(w => !guards.Contains(w)).OrderBy(w => w))
            blocksNothing.Add(warden);

        Check($"every creature that bars the path bars a door{Listing(blocksNothing)}",
            blocksNothing.Count == 0);
        Check($"and every barred door names a creature that bars it{Listing(guardedByAPasser)}",
            guardedByAPasser.Count == 0);

        // Printed, never asserted — see the summary. Reading these against `placement.md` is how a
        // content pass gets checked; failing on them is how a suite gets ignored. Counted over the
        // chapter spine only, so the figures are the bill's figures and the prototype rat is not
        // quietly making every total one too many. Today it reads 30 / 11 / 3, and 24 freed to 6
        // defeated; unit 6 takes the fights to 33.
        var spine = rooms.Where(kv => kv.Value?.ChapterId?.StartsWith("ch") == true)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var aftermath = spine
            .SelectMany(kv => kv.Value.Hotspots.Where(h => h != null))
            .SelectMany(Gates)
            .ToList();

        // -- And now the same thing in play, on one real authored screen --
        // Everything above reads the .tres files. None of it proves a creature *appears*: the whole
        // pass could parse perfectly and still put nothing on screen, because the Approach hotspot is
        // generated from the overlay's bounds rather than authored (see RoomView.AddEntityHotspots),
        // and an overlay whose presentation resolves to invisible is silent about it. That is the
        // blind spot handoff.md §3 names as the one that hides the rest — a layer the verifier never
        // questions — so pass 2 drives one screen for real rather than trusting thirty files.
        //
        // ch1_low_track is chosen because it is the first fight in the game: the screen a player
        // reaches third, and the one where a broken placement would be most obvious and least
        // excusable. It is not a camp, so standing here refills nothing and disturbs no later gate.
        const string firstFight = "ch1.logger_01";
        NavigationManager.Instance.GoToRoom("ch1_low_track");
        var onScreen = Live();

        var creature = onScreen.FirstOrDefault(h => h.EntityInstanceId == firstFight);
        Check("the first creature in the game is on screen", Blind || creature != null);
        Check("and it is labelled with the name it was authored with",
            Blind || creature?.Label == "The Logger");

        // The person it frees is drawn from the start and shut, which is the whole point of unit 1.5:
        // the screen tells you there is something here to earn without letting you have it.
        var person = Find(onScreen, "ch1_low_track_freed");
        Check("the person inside it is drawn but shut", Blind || person?.Locked == true);
        Check("and it names the flag that will free them",
            Blind || person?.LockedBy == Flags.Freed(firstFight));
        Check("drawing them did not make them available",
            !InteractionService.IsAvailable(person?.Data));

        var world = WorldStateManager.Instance;
        world.SetFlag(Flags.Freed(firstFight));
        var afterwards = Find(Live(), "ch1_low_track_freed");
        Check("winning the fight brings them out", Blind || afterwards?.Locked == false);
        Check("and they can be spoken to", InteractionService.IsAvailable(afterwards?.Data));
        world.SetFlag(Flags.Freed(firstFight), false);

        GD.Print($"       spine: {spine.Sum(kv => kv.Value.Entities.Count)} fights, "
            + $"{PlacedEntities(spine).Count()} creature types, "
            + $"{wardens.Count(w => spine.ContainsKey(placedIn[w]))} warden(s); aftermath "
            + $"{aftermath.Count(f => f.StartsWith("freed."))} freed / "
            + $"{aftermath.Count(f => f.StartsWith("defeated."))} defeated");
    }

    /// <summary>Every distinct <see cref="EntityData"/> reachable through a placement. There is no registry.</summary>
    private static IEnumerable<EntityData> PlacedEntities(Dictionary<string, RoomData> rooms)
    {
        var seen = new HashSet<string>();
        foreach (var (_, room) in rooms)
            foreach (var p in room?.Entities ?? new Godot.Collections.Array<EntityPlacement>())
                if (p?.Entity is { } e && !string.IsNullOrEmpty(e.Id) && seen.Add(e.Id))
                    yield return e;
    }

    /// <summary>Which creature <i>types</i> stand on a screen — rule 2 is about the type, not the individual.</summary>
    private static HashSet<string> Species(RoomData room) =>
        (room?.Entities ?? new Godot.Collections.Array<EntityPlacement>())
        .Where(p => p?.Entity != null && !string.IsNullOrEmpty(p.Entity.Id))
        .Select(p => p.Entity.Id)
        .ToHashSet();

    /// <summary>
    /// The instance id a hotspot is waiting on the death of, read back out of its gate. Both domains,
    /// because a freed person and a Beast carcass are the same shape — see <see cref="Flags.Defeated"/>.
    /// </summary>
    private static IEnumerable<string> AftermathOf(HotspotData h)
    {
        foreach (var flag in Gates(h))
        {
            if (flag.StartsWith("freed.")) yield return flag["freed.".Length..];
            else if (flag.StartsWith("defeated.")) yield return flag["defeated.".Length..];
        }
    }

    /// <summary>Every flag holding a hotspot shut, across both gating fields.</summary>
    private static IEnumerable<string> Gates(HotspotData h)
    {
        if (!string.IsNullOrEmpty(h.RequiredFlag)) yield return h.RequiredFlag;
        foreach (var f in h.RequiredFlags ?? Array.Empty<string>())
            if (!string.IsNullOrEmpty(f)) yield return f;
    }

    /// <summary>Appends the offenders to a check's description, so a failure says where to look.</summary>
    private static string Listing(List<string> offenders) =>
        offenders.Count == 0 ? "" : $"  [{string.Join(", ", offenders)}]";

    // ---- The placeholder readability layer (unit 1.5) --------------------

    /// <summary>
    /// Unit 1.5's cues. Two different kinds of check live here and it is worth saying which is which.
    ///
    /// <para>
    /// <b>The totality check is one of the few guards that genuinely goes red on its own.</b> Add a
    /// <see cref="HotspotKind"/>, forget its colour, the suite fails — and <b>unit 4 adds
    /// <c>Station</c></b>, so it fires for real within two units. That is only true because the
    /// palette is a dictionary; a <c>switch</c> would need a default arm, and a default arm makes
    /// the same check unfalsifiable (handoff.md's "guards that cannot fail").
    /// </para>
    ///
    /// <para>
    /// <b>The rest guard a real regression risk.</b> The view now instantiates hotspots that
    /// <see cref="InteractionService.IsAvailable"/> refuses — a shut gate, drawn as a picture of
    /// itself. If one of those ever became clickable, every flag gate in the game would be
    /// bypassable by walking into the room, and it would look exactly like a working feature. So
    /// "drawn" and "inert" are asserted separately, and availability is re-asserted underneath.
    /// </para>
    ///
    /// <para>
    /// <b>What this cannot check.</b> Headless runs the dummy renderer, so no <c>_Draw</c> body
    /// executes: nothing here proves a label is legible, or that hover beats the idle cue, or that
    /// lime survives purified ground. Those are eyes-on questions and the screenshot tool is where
    /// they get answered. What is asserted is structure — which nodes exist, in what state.
    /// </para>
    /// </summary>
    private static void GatePlaceholderCues()
    {
        Section("cues  the placeholder readability layer");

        // -- Totality: every kind has a colour, and no two share one --
        var missing = new List<string>();
        var seen = new Dictionary<string, string>();
        var collisions = new List<string>();

        foreach (HotspotKind kind in Enum.GetValues<HotspotKind>())
        {
            if (!PlaceholderCues.KindColors.TryGetValue(kind, out var colour))
            {
                missing.Add(kind.ToString());
                continue;
            }

            var hex = colour.ToHtml(false);
            if (seen.TryGetValue(hex, out var other)) collisions.Add($"{kind}={other}");
            else seen[hex] = kind.ToString();
        }

        Check($"every hotspot kind has a placeholder colour{Listing(missing)}", missing.Count == 0);
        Check($"and no two kinds share one{Listing(collisions)}", collisions.Count == 0);
        Check("an unmapped kind would fall back to something obviously wrong",
            PlaceholderCues.ColorFor((HotspotKind)9999) == Colors.Magenta);

        // -- An unpainted screen cues; a painted one does not --
        var world = WorldStateManager.Instance;
        world.SetFlag(CueShutFlag, false);
        world.SetFlag(CueSpentFlag, true);

        NavigationManager.Instance.GoToRoom("verify_cues");
        Check("the unpainted screen is cueing", _view == null || _view.CuesVisible);

        var live = Live();
        Check("the open hotspot is on screen", Blind || Present(live, "verify_cues_open"));
        Check("and it is drawing its cue",
            Blind || Find(live, "verify_cues_open")?.ShowCue == true);
        Check("the door is on screen too", Blind || Present(live, "verify_cues_door"));

        // -- A shut gate is drawn, and is a picture of itself --
        var shut = Find(live, "verify_cues_shut");
        Check("a hotspot shut by a flag is still drawn", Blind || shut != null);
        Check("it is marked locked", Blind || shut?.Locked == true);
        Check("it names the flag holding it", Blind || shut?.LockedBy == CueShutFlag);
        Check("it does not take the mouse",
            Blind || shut?.MouseFilter == Control.MouseFilterEnum.Ignore);

        // The one real regression risk in this whole unit: the view now instantiates hotspots that
        // `IsAvailable` refuses. Drawn and clickable are asserted apart, and availability is
        // re-asserted underneath, so a locked hotspot going live would show up here as a flag gate
        // bypass rather than as a cosmetic slip.
        Check("and drawing it did not make it available",
            !InteractionService.IsAvailable(shut?.Data));

        // -- A spent node is gone, not "locked" --
        // Different reason, different treatment: a shut gate is waiting to open, a picked herb is
        // finished. Drawing the second forever would fill every cleansed screen with ghosts.
        Check("a harvest node already taken is not drawn at all",
            Blind || !Present(live, "verify_cues_spent"));

        // -- Open the gate and the picture becomes the thing --
        world.SetFlag(CueShutFlag, true);
        live = Live();
        var opened = Find(live, "verify_cues_shut");
        Check("opening the flag leaves it on screen", Blind || opened != null);
        Check("no longer locked", Blind || opened?.Locked == false);
        Check("and it takes the mouse now",
            Blind || opened?.MouseFilter == Control.MouseFilterEnum.Stop);

        // -- Paint the screen and every cue switches itself off --
        // The promise is that there is nothing to remember and no way to ship the cues by accident.
        // It only holds if the condition is the art itself, which is what this asserts.
        world.SetFlag(CueShutFlag, false); // shut it again — the block above left the gate open
        NavigationManager.Instance.GoToRoom("verify_cues_painted");
        Check("a painted screen is not cueing", _view == null || !_view.CuesVisible);

        var painted = Live();
        Check("its hotspots draw no cue",
            Blind || Find(painted, "verify_cues_painted_open")?.ShowCue == false);
        Check("and a shut hotspot is absent again, as it ships",
            Blind || !Present(painted, "verify_cues_painted_shut"));

        // -- The debug toggle is the manual override for art --
        // A painting can hide a clickable region as well as a grey box can.
        if (_view != null) _view.ShowHotspotOutlines = true;
        var forced = Live();
        Check("forcing outlines cues a painted screen", Blind || _view.CuesVisible);
        Check("and brings its shut hotspots back to look at",
            Blind || Present(forced, "verify_cues_painted_shut"));
        if (_view != null) _view.ShowHotspotOutlines = false;

        world.SetFlag(CueShutFlag, false);
        world.SetFlag(CueSpentFlag, false);
    }

    private static IReadOnlyList<Hotspot> Live() =>
        _view?.LiveHotspots ?? (IReadOnlyList<Hotspot>)Array.Empty<Hotspot>();

    private static Hotspot Find(IReadOnlyList<Hotspot> live, string hotspotId)
    {
        foreach (var h in live)
            if (h.Data?.Id == hotspotId) return h;
        return null;
    }

    private static bool Present(IReadOnlyList<Hotspot> live, string hotspotId) =>
        Find(live, hotspotId) != null;

    /// <summary>
    /// No view handed in, so every view check above short-circuits to pass.
    ///
    /// <para>
    /// Written as its own name rather than as an inline <c>_view == null ||</c> because half the
    /// checks in this gate assert an *absence*, and the naive form inverts to <c>!(...)</c> — which
    /// turns the politeness into a guaranteed failure the moment there is no view. Same intent as
    /// the rest of the file, spelled so it survives negation. <see cref="GateHarness"/> is what
    /// keeps the politeness from hiding a wiring fault.
    /// </para>
    /// </summary>
    private static bool Blind => _view == null;

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

    /// <summary>
    /// The Snarl shape (ledger-ch3 row 12): one door that opens only when all three knot pockets
    /// are cut. Driven against synthetic hotspots rather than authored ones, because no .tres uses
    /// <c>RequiredFlags</c> yet — the three authoring sweeps in <see cref="GateAuthoredContent"/>
    /// are vacuous until pass 1 generates the mine, and a vacuous gate proves nothing.
    /// </summary>
    private static void GateMultiFlagGating()
    {
        Section("Multi-flag gating — the Snarl");

        var world = WorldStateManager.Instance;
        var knots = new[] { "verify.knot.a.cut", "verify.knot.b.cut", "verify.knot.c.cut" };
        foreach (var k in knots) world.SetFlag(k, false);

        var snarl = new HotspotData
        {
            Id = "verify_snarl",
            Kind = HotspotKind.Navigate,
            TargetRoomId = "verify_tear",
            RequiredFlags = knots,
        };

        Check("the Snarl is shut with no knots cut",
            !InteractionService.IsAvailable(snarl));

        world.SetFlag(knots[0]);
        Check("still shut with one cut", !InteractionService.IsAvailable(snarl));

        world.SetFlag(knots[1]);
        Check("still shut with two cut", !InteractionService.IsAvailable(snarl));

        world.SetFlag(knots[2]);
        Check("open with all three cut", InteractionService.IsAvailable(snarl));

        // Order must not matter — a partial gate that happened to read only the last entry would
        // pass every check above.
        world.SetFlag(knots[1], false);
        Check("shut again when a middle knot is undone",
            !InteractionService.IsAvailable(snarl));
        world.SetFlag(knots[1]);

        world.SetFlag(knots[0], false);
        Check("shut again when the first knot is undone",
            !InteractionService.IsAvailable(snarl));
        world.SetFlag(knots[0]);

        // The scalar gate still composes: the Snarl is also behind the chapter's tool.
        snarl.RequiredFlag = "verify.mine_pick.known";
        Check("all three cut is not enough while the scalar gate is unmet",
            !InteractionService.IsAvailable(snarl));
        world.SetFlag("verify.mine_pick.known");
        Check("open once the scalar gate is met too",
            InteractionService.IsAvailable(snarl));

        // A blank entry fails closed. It is an authoring slip either way, but the direction it
        // fails in is the difference between a stuck door and a door that opens the boss early.
        var halfAuthored = new HotspotData
        {
            Id = "verify_half",
            Kind = HotspotKind.Navigate,
            TargetRoomId = "verify_tear",
            RequiredFlags = new[] { knots[0], "" },
        };
        Check("a blank RequiredFlags entry holds the gate shut, not open",
            !InteractionService.IsAvailable(halfAuthored));

        // An empty array must not gate at all, or every hotspot in the game vanishes.
        var ungated = new HotspotData { Id = "verify_open", Kind = HotspotKind.Examine };
        Check("an empty RequiredFlags gates nothing",
            InteractionService.IsAvailable(ungated));

        foreach (var k in knots) world.SetFlag(k, false);
        world.SetFlag("verify.mine_pick.known", false);
    }

    /// <summary>
    /// The material refill (items.md §7) and the ordinal-keyed knot beat (combat.md §23). Both are
    /// mechanisms without callers — the refill fires on a boss death and the beat on a cut knot, and
    /// neither of those exists until units 3 and 6 — so both are driven here against synthetic rooms.
    /// </summary>
    private static void GateChapterSpine()
    {
        Section("Chapter spine — refill and knot beats");

        var world = WorldStateManager.Instance;

        RoomData NodeRoom(string id, string chapter, string nodeId, bool refills)
        {
            var room = new RoomData { Id = id, DisplayName = id, ChapterId = chapter };
            room.Hotspots.Add(new HotspotData
            {
                Id = nodeId,
                Kind = HotspotKind.Harvest,
                ItemId = "herb_bitterroot",
                RefillsAtMilestone = refills,
            });
            return room;
        }

        var common1 = NodeRoom("verify_ch1_room", "ch1", "verify_ch1_tallow", refills: true);
        var scarce1 = NodeRoom("verify_ch1_stone", "ch1", "verify_ch1_cairnstone", refills: false);
        var common2 = NodeRoom("verify_ch2_room", "ch2", "verify_ch2_pitch", refills: true);
        var common3 = NodeRoom("verify_ch3_room", "ch3", "verify_ch3_pitch", refills: true);
        var rooms = new[] { common1, scarce1, common2, common3 };

        string Node(RoomData r) => r.Hotspots[0].ResolvedHarvestedFlag;

        Check("a harvested flag is the published name",
            Node(common1) == Flags.Harvested("verify_ch1_tallow"));

        Check("a refill milestone is named per chapter",
            Flags.MaterialRefill("ch2") == "milestone.ch2.material_refill");

        foreach (var r in rooms) world.SetFlag(Node(r));

        Check("nothing refills before the milestone",
            world.RefillMaterialNodes(rooms) == 0);

        // Each chapter refills at its own boss, so a chapter 1 boss must leave chapter 2 alone.
        // This is the check the old pooled call could not make.
        Check("the chapter 1 boss refills one node", world.RefillMaterialNodes(rooms, "ch1") == 1);
        Check("the ch1 common node is back", !world.GetFlag(Node(common1)));
        Check("the ch2 common node is untouched by the ch1 boss", world.GetFlag(Node(common2)));

        Check("the chapter 2 boss refills one node", world.RefillMaterialNodes(rooms, "ch2") == 1);
        Check("the ch2 common node is back", !world.GetFlag(Node(common2)));

        // The two that carry the whole point of the field's default.
        Check("the scarce node stays taken", world.GetFlag(Node(scarce1)));
        Check("chapter 3 is terminal and does not refill", world.GetFlag(Node(common3)));

        Check("refilling twice restores nothing the second time",
            world.RefillMaterialNodes(rooms, "ch1", "ch2") == 0);

        // A non-Harvest hotspot marked to refill must not have its flag cleared — the flag means
        // something else entirely on any other kind.
        var oddity = new RoomData { Id = "verify_odd", DisplayName = "odd", ChapterId = "ch1" };
        oddity.Hotspots.Add(new HotspotData
        {
            Id = "verify_odd_door", Kind = HotspotKind.Navigate,
            TargetRoomId = "verify_ch1_room", RefillsAtMilestone = true,
        });
        world.SetFlag(oddity.Hotspots[0].ResolvedHarvestedFlag);
        Check("only Harvest nodes refill",
            world.RefillMaterialNodes(new[] { oddity }, "ch1") == 0);
        world.SetFlag(oddity.Hotspots[0].ResolvedHarvestedFlag, false);

        // ---- The knot beat ----------------------------------------------

        var knots = new[] { "verify_lit", "verify_high", "verify_far" };
        foreach (var k in knots) world.SetFlag(Flags.KnotCut(k), false);

        Check("no knots cut yet", world.CutKnotCount(knots) == 0);
        Check("the first cut plays beat 1", world.NextKnotBeatOrdinal(knots) == 1);

        // Cut them out of order. The ordinal must follow the count, not the pocket — cutting the
        // *last* listed knot first still has to play beat 1, which is the whole decision.
        world.SetFlag(Flags.KnotCut("verify_far"));
        Check("cutting the last-listed knot first still leaves beat 2 next",
            world.NextKnotBeatOrdinal(knots) == 2);

        world.SetFlag(Flags.KnotCut("verify_lit"));
        Check("the third cut plays beat 3", world.NextKnotBeatOrdinal(knots) == 3);

        world.SetFlag(Flags.KnotCut("verify_high"));
        Check("all three are cut", world.CutKnotCount(knots) == 3);
        Check("there is no fourth beat", world.NextKnotBeatOrdinal(knots) == 0);

        Check("an empty knot list has no beat", world.NextKnotBeatOrdinal(System.Array.Empty<string>()) == 0);

        foreach (var k in knots) world.SetFlag(Flags.KnotCut(k), false);
        foreach (var r in rooms) world.SetFlag(Node(r), false);
    }

    private static HotspotData Hotspot(string hotspotId)
    {
        var room = NavigationManager.Instance.CurrentRoom;
        var found = room?.Hotspots.FirstOrDefault(h => h != null && h.Id == hotspotId);
        if (found == null)
            Failures.Add($"[setup] no hotspot '{hotspotId}' in room '{room?.Id}'");
        return found;
    }

    private static bool IsAvailable(string hotspotId)
    {
        var room = NavigationManager.Instance.CurrentRoom;
        var data = room?.Hotspots.FirstOrDefault(h => h != null && h.Id == hotspotId);
        return data != null && InteractionService.IsAvailable(data);
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
