using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;
using Unrendered.Save;
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

    public static int Run()
    {
        Failures.Clear();
        _checks = 0;

        // Deterministic: no ambient drain racing the assertions.
        if (ResolveManager.Instance != null) ResolveManager.Instance.Active = false;

        GateM1_TwoScreenNavigation();
        GateM2_StateRealmTable();
        GateM3_MicroLoop();
        GateHeistMechanics();
        GatePersistence();

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

    // ---- Heist mechanics -------------------------------------------------

    private static void GateHeistMechanics()
    {
        Section("heist  clock, pursuit and both failure endings");

        var run = DreamRunManager.Instance;
        var resolve = ResolveManager.Instance;

        Check("the heist is registered", run.GetHeist("waystation_heist") != null);

        // -- Being touched --
        resolve.SetTo(resolve.Max);
        Check("a run begins", run.Begin("waystation_heist"));
        Check("the pursuer starts on the prize", run.PursuerRoomId == "dream_way_solar");
        Check("and is three rooms off", run.PursuerDistance == 3);

        Walk("dream_way_gallery");
        Check("closing the distance is reported", run.PursuerDistance == 2);

        // Burn some of the clock, then walk straight into it.
        resolve.SetTo(resolve.Max * 0.5f);
        Walk("dream_way_stair");
        Walk("dream_way_solar");
        Check("walking into the pursuer ends the run", !run.IsRunning);
        Check("returning refills the bar", Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        Check("and throws you back to the Waking world",
            NavigationManager.Instance.CurrentRoomId == "waystation_yard" &&
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);

        // -- Running out of nerve --
        resolve.SetTo(resolve.Max);
        Check("a second run begins", run.Begin("waystation_heist"));
        Check("drain is at least the base rate",
            run.CurrentDrainPerSecond() >= run.ActiveHeist.DrainPerSecond);

        resolve.SetTo(0f);
        Check("running out of Resolve ends the run", !run.IsRunning);
        Check("and ejects you", NavigationManager.Instance.CurrentRoomId == "waystation_yard");
        Check("and you are ready to try again", Mathf.IsEqualApprox(resolve.Current, resolve.Max));

        // -- The Waking world never touches the bar --
        var held = resolve.Current;
        Check("no drain outside a heist", Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));
        Check("the bar holds still in the Waking world",
            Mathf.IsEqualApprox(resolve.Current, held));

        run.ForceClear();
    }

    // ---- Persistence -----------------------------------------------------

    private static void GatePersistence()
    {
        Section("save / load round trip");

        const string path = "user://verify_slice.json";

        Check("save writes", SaveService.Save(path));

        // Wipe everything the save is supposed to carry.
        WorldStateManager.Instance.ResetAll();
        EntityStateManager.Instance.ResetAll();
        InventoryCraftingManager.Instance.ResetAll();
        SetRealm(GameRealm.Dream);

        Check("world was actually cleared",
            !WorldStateManager.Instance.GetFlag("zone.waystation.purified") &&
            InventoryCraftingManager.Instance.GetCount("rat_tallow") == 0);

        Check("load reads", SaveService.Load(path));

        Check("entity states restored",
            EntityStateManager.Instance.GetState(Rat) == EntityState.Purified);
        Check("zone phase restored", WorldStateManager.Instance.IsZoneCleansed("waystation"));
        Check("flags restored", WorldStateManager.Instance.GetFlag("waystation.bench_found"));
        Check("inventory restored",
            InventoryCraftingManager.Instance.GetCount("herb_bitterroot") == 2 &&
            InventoryCraftingManager.Instance.GetCount("rat_tallow") == 1);
        Check("realm restored", RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("room restored", NavigationManager.Instance.CurrentRoomId == "waystation_yard");

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

        SaveService.DeleteSave(dreamPath);
    }

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
