using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Dream;
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
    /// <summary>
    /// The body the state table and the micro-loop act on. It was <c>waystation.rat_01</c> until unit 3b
    /// retired the prototype; it is a placement in <see cref="LoopRoom"/> now, which is why the const is
    /// named for its job rather than for an animal.
    /// </summary>
    private const string LoopBeast = "verify.loop_beast";

    private static readonly List<string> Failures = new();
    private static int _checks;

    // The live view and HUD, so a gate can assert what is on screen and not merely what the
    // managers hold. Null-tolerant: every check that uses them says so.
    private static RoomView _view;
    private static Hud _hud;

    /// <summary>
    /// The live <see cref="PocketHost"/>. Added 2026-08-04, after a defect that could only live here.
    /// </summary>
    /// <remarks>
    /// <b>This was the last live node the gates could not see, and it cost a play session.</b> The verb poller
    /// shipped as a node to add to each pocket scene, nothing added one, and every check stayed green because
    /// the suite drives <c>PocketManager</c> — which owns *being in a pocket* — and had no handle on the thing
    /// that owns *what is in the pocket with you*. That is §3's second class exactly: a layer the gates never
    /// question, sitting one step downstream of the manager state they assert.
    /// </remarks>
    private static PocketHost _host;

    public static int Run(RoomView view = null, Hud hud = null, PocketHost host = null)
    {
        Failures.Clear();
        _checks = 0;
        _view = view;
        _hud = hud;
        _host = host;

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

        // `GateM1_TwoScreenNavigation` was called here until unit 3b. It asserted that two named rooms
        // register and a door joins them, which `GateWorldGraph` sweeps across all 59 authored rooms —
        // so it was deleted rather than re-pointed, and its one homeless claim moved into `GateHarness`.
        GateM2_StatePresentationTable();
        GateM3_MicroLoop();
        GateVitalityPool();
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

        // Straight after the satchel, and for the same two reasons plus the bench: it churns the satchel
        // (it resets it twice) and it needs `GateCamps`'s synthetic bench to stand at. It navigates
        // explicitly rather than inheriting a room — *which* room is the subject of half its checks — and
        // it leaves the player at a camp with an empty satchel, which is what `GateEncounters` builds on.
        GateCraftingPanel();

        // Later still, and for the same two reasons plus one of its own: it churns the satchel harder
        // than anything above it, and it is the only gate that *equips* anything — so running it before
        // the wound ledger would put a coat behind every number that gate reads from the resolver. It
        // leaves the player at a camp, having collapsed on purpose, which is why nothing below reads the
        // room.
        GateEncounters();

        // Straight after the fight loop, because it needs a live fight to be refused during and the arena
        // is the only fixture with bodies on it. Running earlier would leave wounds on creatures whose
        // damage numbers that gate reads. Everything below builds its own fixtures, so this one is free to
        // leave the player somewhere new.
        GateTravel();

        // After travel, because it borrows that section's fixtures — a camp to come back to and a creature
        // to be refused in front of — and because it leaves the realm and the Resolve ceiling changed, which
        // nothing below it reads.
        GatePockets();

        GateMultiFlagGating();
        GateChapterSpine();
        GateAuthoredContent();

        // Touches no game state at all — it reads two kinds of file off disk — so its position is free.
        // Here, beside the other sweeps, because what it sweeps is the same thing: authored artifacts
        // referencing each other by name, where the crossing is the only place the mistake shows.
        GateDocCitations();

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
        Check("the live PocketHost was handed to the verifier", _host != null);

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

        // Inherited from the deleted M1 section, which is the only one of its three claims that had
        // nowhere else to live. The other two — that two named rooms register, and that a door joins
        // them — are a two-room copy of what `GateWorldGraph` already sweeps across all 59 authored
        // rooms, so they went with the prototype rather than being re-pointed at a fixture. A weaker
        // synthetic version of a gate that already exists at real scale is the tautology
        // `deleting-a-gated-mechanism` warns about.
        Check("and a new game starts in the Waking world",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
    }

    // ---- M2: state presentation, and the signals that carry it -----------

    private static void GateM2_StatePresentationTable()
    {
        Section("M2  state presentation table");

        var entities = EntityStateManager.Instance;

        // Named rather than inherited. The boot moved to `ch1_cottage` on 2026-08-02 and this section
        // and M3 both rode the old one — they acted on "wherever the game put me" and silently meant
        // the waystation yard, which is what made the retirement look like unrelated breakage.
        // **Nothing below this line should read `GameConfig.StartingRoomId`.**
        NavigationManager.Instance.GoToRoom(LoopRoomId);

        var beast = Placement(LoopBeast)?.Entity;

        Check("the body was registered on first sight", entities.IsKnown(LoopBeast));
        Check("the body starts Corrupted", entities.GetState(LoopBeast) == EntityState.Corrupted);

        // Form 1: a nightmare squatting in the road.
        var corrupt = entities.Resolve(beast, LoopBeast);
        Check("Form 1 blocks the path", corrupt.BlocksPath);
        Check("Form 1 is named as the cursed thing", corrupt.Label == LoopCorruptedName);

        // Five Form 2 checks sat here — the porcelain Baron, its "Sever" verb, and the unattackable
        // Dream echo. Deleted 2026-08-02: `ResolveDream` is gone, so there is no second face to
        // compare against. Form 2 is not cancelled, it moved to the 3D pocket, and **it will need
        // its own checks there** — this comment is the note that this gate got smaller because
        // something was taken out of scope, not because it stopped mattering.

        // A signal must actually carry the change, not just the field.
        var heard = new List<string>();
        void Listener(string id, int state) => heard.Add($"{id}:{(EntityState)state}");
        EventBus.Instance.EntityStateChanged += Listener;
        entities.SetState(LoopBeast, EntityState.Severed);
        EventBus.Instance.EntityStateChanged -= Listener;

        Check("EntityStateChanged fires on transition",
            heard.Contains($"{LoopBeast}:{EntityState.Severed}"));

        var severed = entities.Resolve(beast, LoopBeast);
        Check("Severed clears the road", !severed.BlocksPath);
        // The name is deliberately the same — it is still the cursed thing, just hollowed out — so
        // the only thing that distinguishes Form 1 from Form 1-deformed on screen is the wash.
        Check("and a severed body reads as drained", severed.Modulate != corrupt.Modulate);

        entities.SetState(LoopBeast, EntityState.Purified);
        var purified = entities.Resolve(beast, LoopBeast);
        Check("Form 3 is a plain animal in reality", purified.Label == LoopPurifiedName);
        Check("Form 3 does not block", !purified.BlocksPath);

        // Put it back — M3 walks the loop properly from the top.
        entities.SetState(LoopBeast, EntityState.Corrupted);
        Check("state is restorable for the next gate",
            entities.GetState(LoopBeast) == EntityState.Corrupted);
    }

    // ---- M3: the whole loop, in order ------------------------------------

    private static void GateM3_MicroLoop()
    {
        Section("M3  the micro-loop  (barred road -> severing -> finish -> payoff)");

        var entities = EntityStateManager.Instance;
        var world = WorldStateManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        var encounters = EncounterManager.Instance;

        // 1. The road is barred and the way on is not obvious.
        SetRealm(GameRealm.Waking);
        Check("the way further in is refused while the body is Corrupted",
            !InteractionService.ActivateHotspot(Hotspot(LoopDoor)));
        Check("and the player did not move",
            NavigationManager.Instance.CurrentRoomId == LoopRoomId);
        Check("the payoff is not there yet", !IsAvailable(LoopHarvest));
        Check("what the cairn hides is hidden until it is searched",
            !IsAvailable(LoopRelic));

        // 2. The problem is visible and cannot be solved from out here. This used to be phrased as
        // "you cannot sever by looking through spectral vision", flipping the realm first; the
        // refusal survives the deletion of the peek, because it was never the peek that refused —
        // it is that the anchor is not in this world. Kept, reworded, still falsifiable.
        //
        // Reworded a second time by unit 3a, for the same reason and following the same rule
        // (`deleting-a-gated-mechanism`: keep the invariant, move the probe). The click now *starts a
        // fight* instead of refusing, so the old assertion — a false return — is gone. The claim it was
        // making is not: **a fight in the Waking world cannot sever an anchor that is not in this
        // world.** That was always a statement about the body's state rather than about a return value,
        // so the probe moves to where the claim actually lived.
        Check("clicking the body starts a fight rather than a severing",
            InteractionService.ActivateEntity(LoopBeast) && encounters.InFight);
        Check("and breaking off is always allowed", encounters.Flee());
        Check("the body is still Corrupted", entities.GetState(LoopBeast) == EntityState.Corrupted);

        // 3. Two hotspots where one thing reveals another, which is the shape most of the authored
        // game is made of. The second check is new with unit 3b and covers a field nothing else does:
        // the cairn is gated on its own flag being *false*, so it closes behind itself.
        Check("searching the cairn succeeds", InteractionService.ActivateHotspot(Hotspot(LoopCairn)));
        Check("what it hid is now exposed", IsAvailable(LoopRelic));
        Check("and the cairn cannot be searched twice", !IsAvailable(LoopCairn));

        // 4. The tether comes off.
        //
        // **Thirteen checks stood here and they were the heist.** Crossing over, parking the pursuer,
        // walking four Dream rooms, taking the anchor, and every claim about the ejection that followed.
        // All of it covered `DreamRunManager` and the six `dream_way_*` screens, which unit 3b deleted
        // and unit 6 replaces with a 3D pocket that shares none of its mechanics — so per §7 decision 7
        // they are deleted rather than re-pointed at a fixture, because migrating a gate onto a corpse
        // is work you do twice and throw away once. The invariant that outlives all of it is the one
        // line below: **severing is what clears the road, and the road does not care how it happened.**
        // Unit 6 owes this file a probe for *how*.
        entities.SetState(LoopBeast, EntityState.Severed);
        Check("the tether can be cut", entities.GetState(LoopBeast) == EntityState.Severed);

        // 5. Reality gave way.
        Check("the road is now passable", InteractionService.ActivateHotspot(Hotspot(LoopDoor)));
        Check("and the player is through it",
            NavigationManager.Instance.CurrentRoomId == LoopInnerRoomId);

        Check("the node behind it harvests",
            InteractionService.ActivateHotspot(Hotspot(LoopInnerHarvest)));
        Check("and what it gave is in the satchel", inv.GetCount("herb_bitterroot") == 1);
        Check("a stripped node refuses a second take",
            !InteractionService.ActivateHotspot(Hotspot(LoopInnerHarvest)));
        Check("the consumed node is gone from the screen",
            !IsAvailable(LoopInnerHarvest));
        Check("the bench sets its flag", InteractionService.ActivateHotspot(Hotspot(LoopInnerBench)));
        Check("and that flag is recorded", world.GetFlag(LoopBenchFlag));

        // 6. Zero respawn: the world remembers.
        InteractionService.ActivateHotspot(Hotspot(LoopInnerDoor));
        Check("back out in the road", NavigationManager.Instance.CurrentRoomId == LoopRoomId);
        Check("the body is still Severed, not reset",
            entities.GetState(LoopBeast) == EntityState.Severed);

        // 7. Finish the anchorless body. **This stopped standing in for the turn-based fight on
        // 2026-08-03 and became one** — which is unit 3a's whole point, and the one place in this suite
        // where a comment saying "stands in for" got to be deleted rather than reworded.
        //
        // Driven with nothing equipped and nothing rigged on purpose: this is what §18 means by *the cheap
        // ending survives as an outcome rather than as a shortcut*. The husk still comes apart with bare
        // hands; it just takes the turns that costs. The cap is a hang guard, not a rule — a strike loop
        // that cannot dent its target is `verify-slice-blind-spots`'s worst failure shape.
        Check("engaging the anchorless body starts a fight",
            InteractionService.ActivateEntity(LoopBeast) && encounters.InFight);

        var huskTurns = 0;
        while (encounters.InFight && huskTurns < 40)
        {
            encounters.Strike();
            huskTurns++;
        }

        Check("finishing the severed body succeeds", !encounters.InFight && huskTurns < 40);
        Check("the body is Purified", entities.GetState(LoopBeast) == EntityState.Purified);
        // The zone system's two probes lived here. Following `deleting-a-gated-mechanism`, the one with
        // a surviving invariant moved and the one without went:
        //  * "the zone is cleansed" is the same assertion at the new scope — one screen, not a chapter.
        //  * "the zone reads as inert" measured `ZoneCorruptionScalar`, which nothing read once Resolve
        //    drain became boss-only aura. It is deleted rather than reworded, because a check that
        //    still passes while measuring nothing is the tautology a rename leaves behind.
        Check("the screen is cleansed", world.GetFlag(Flags.Cleansed(LoopRoomId)));

        // 8. And the payoff is reachable. The two nodes give one each where the prototype's gave one
        // and two, so this total is the same total three later sections inherit — see `LoopRoom`.
        Check("the flag-gated growth has appeared", IsAvailable(LoopHarvest));
        Check("it harvests", InteractionService.ActivateHotspot(Hotspot(LoopHarvest)));
        Check("two bitterroot in the satchel", inv.GetCount("herb_bitterroot") == 2);

        var form3 = entities.Resolve(Placement(LoopBeast).Entity, LoopBeast);
        Check("Form 3 is a plain animal", form3.Label == LoopPurifiedName);
        Check("the animal is farmable", InteractionService.ActivateEntity(LoopBeast));
        Check("tallow taken", inv.GetCount("mat_tallow") == 1);
    }

    // ---- Vitality --------------------------------------------------------

    /// <summary>
    /// The body's pool. Nothing here needs the clock switched off the way Resolve's gates do,
    /// because Vitality has no clock — which is itself worth asserting, since the cheapest way
    /// to break §6 later is to give it one.
    ///
    /// Deliberately leaves the bar at a known part-drained value on the way out, so that
    /// <see cref="GatePersistence"/> round-trips something other than the default. It also fed
    /// <c>GateHeistMechanics</c>'s *"a whole heist does not move the body"* until unit 6b deleted that
    /// section; <see cref="GatePockets"/> makes the same claim about a pocket now.
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
            recipe.Inputs[1].ItemId == "mat_tallow" && recipe.Inputs[1].Amount > 0);
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
        inv.AddItem("mat_tallow", 1);
        Check("with everything in hand it can be made", inv.CanCraft("recipe_salve_clearmind"));
        Check("and crafting reports success", inv.Craft("recipe_salve_clearmind"));
        Check("the output arrives", inv.GetCount("salve_clearmind") == 1);
        Check("the inputs are gone",
            inv.GetCount("herb_bitterroot") == 0 && inv.GetCount("mat_tallow") == 0);
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
        gated.Inputs.Add(new RecipeIngredient { ItemId = "herb_graveflower", Amount = 1 });
        inv.DebugRegisterRecipe(gated);

        Check("a recipe needing a blueprint knows it does", gated.NeedsBlueprint);
        Check("an unlearned blueprint is not discovered", !inv.IsDiscovered("lore_verify_page"));

        // Materials in hand, so the only thing standing in the way is the knowledge.
        inv.AddItem("herb_graveflower", 1);
        Check("and without it the recipe refuses", !inv.CanCraft("verify_gated"));
        Check("naming knowledge, not materials, as the reason",
            inv.Inspect("verify_gated") == CraftBlock.Unknown);
        Check("attempting an unknown recipe fails", !inv.Craft("verify_gated"));
        Check("and consumes nothing", inv.GetCount("herb_graveflower") == 1);

        Check("learning it reports newly-learned", inv.Discover("lore_verify_page"));
        Check("and it is now known", inv.IsDiscovered("lore_verify_page"));
        Check("learning it again is not news", !inv.Discover("lore_verify_page"));
        Check("and now the same recipe is craftable", inv.CanCraft("verify_gated"));
        Check("so knowledge was the only thing missing",
            inv.Inspect("verify_gated") == CraftBlock.None);

        // Picking up a Lore item teaches it, which is what makes the journal purely additive.
        Check("a blueprint not yet seen is unknown", !inv.IsDiscovered("catalyst_ashincense"));
        inv.AddItem("herb_graveflower", 1);
        Check("picking up a non-Lore item teaches nothing",
            !inv.IsDiscovered("herb_graveflower"));

        // **The positive half, and it had never run — 2026-08-05.** The comment above has claimed this since
        // the method was written, and only the *negative* was checked, because until decision 11's pages
        // landed there was no authored `Lore` item to pick up and no way to register a synthetic one. A
        // one-sided pair like this is the third blind-spot class in its quietest form: the sibling check keeps
        // the claim looking covered, so nothing prompts anyone to notice the other arm is missing.
        Check("a page he has not found is unknown", !inv.IsDiscovered("lore_physick_deepmend"));
        inv.AddItem("lore_physick_deepmend", 1);
        Check("but picking one up teaches what is written on it",
            inv.IsDiscovered("lore_physick_deepmend"));
        Check("which opens the recipe it teaches",
            inv.Inspect("recipe_salve_deepmend") != CraftBlock.Unknown);

        // And the reason knowledge is kept apart from the page — `AddItem`'s own comment: *"learning survives
        // spending, losing or reading it"*. Nothing tested that either, and it is the claim decision 11 rests
        // on, since a page is a *view* of the knowledge rather than the knowledge itself.
        Check("losing the page does not unlearn it",
            inv.RemoveItem("lore_physick_deepmend", 1) && inv.IsDiscovered("lore_physick_deepmend"));

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

    // ---- Inside a run (retired) -------------------------------------------
    //
    // **`GateHeistMechanics` stood here, and unit 6b deleted it after moving every check it carried.** Its
    // own doc comment named the reason it had survived unit 3b: it was the third category of gate — one that
    // *touches a dying mechanism in order to probe a living one*. A live heist was simply the only place four
    // surviving files could be stood in.
    //
    // Nineteen checks, and the split on the way out is worth recording because getting it wrong would have
    // been silent:
    //
    //   • **Thirteen moved to the `pockets` section**, where they now stand inside a real pocket: the realm
    //     lock going on and off, the HUD's dim trio, the pool-order swap and its restore, Vitality never
    //     moving in the Dream, the crossing from a `CrossToDream` hotspot, the return to the screen you left,
    //     and both views being asked what they are drawing.
    //   • **Four died with the mechanism they were about**: the heist registry, the maze's start room, the
    //     pursuer ending a run, and the anchor.
    //   • **Two were deleted rather than moved, deliberately** — *"no drain outside a run"* and *"the bar
    //     holds still in the Waking world"*. With the heist gone nothing in the game can drain Resolve at
    //     all, so both became universals over an empty set. The rule survives in prose on
    //     `ResolveManager.CurrentRatePerSecond`, and the checks come back with the boss aura, which is the
    //     first mechanism that could violate them.
    //
    // The one that mattered most was **`RoomView.DisplayedRoomId`** — the whole regression cover for the
    // ejection post-mortem, and every assertion of it lived in here. Its replacement is *better*: the old
    // version reproduced one situation that happened to trigger the bug (walking into the pursuer nested a
    // navigation inside a `RoomEntered` handler), while the new one raises a stale payload by hand and
    // asserts the guard directly, so it cannot stop working because the Dream changed shape again.

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
            !WorldStateManager.Instance.GetFlag(Flags.Cleansed(LoopRoomId)) &&
            InventoryCraftingManager.Instance.GetCount("mat_tallow") == 0);

        Check("and Vitality was actually cleared",
            Mathf.IsEqualApprox(VitalityManager.Instance.Current, VitalityManager.Instance.Max));

        Check("load reads", SaveService.Load(path));

        Check("entity states restored",
            EntityStateManager.Instance.GetState(LoopBeast) == EntityState.Purified);
        // A stronger version of the "zone phase restored" check it replaces, and by accident rather than
        // design: the save was taken in `verify_wilds`, so the load re-derives *that* screen's verdict
        // on arrival and never touches the loop room's. This flag therefore comes out of the file or not
        // at all — which is the round trip the deleted `SaveGame.ZonePhases` field used to be needed for.
        Check("the cleansed screen restored",
            WorldStateManager.Instance.GetFlag(Flags.Cleansed(LoopRoomId)));
        Check("flags restored", WorldStateManager.Instance.GetFlag(LoopBenchFlag));
        Check("inventory restored",
            InventoryCraftingManager.Instance.GetCount("herb_bitterroot") == 2 &&
            InventoryCraftingManager.Instance.GetCount("mat_tallow") == 1);
        Check("realm restored", RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("room restored", NavigationManager.Instance.CurrentRoomId == "verify_wilds");
        Check("Vitality restored",
            Mathf.IsEqualApprox(VitalityManager.Instance.Current, wounded));

        SaveService.DeleteSave(path);

        // **A mid-run stranding block stood here and went with unit 6b.** It saved one door into the Dream
        // maze, cleared the run, loaded, and asserted the player was not left in a room with no exit — the
        // save/load soft lock `SaveService` was written for. **A pocket cannot reproduce it**: it is a 3D
        // scene and never enters `NavigationManager`, so `CurrentRoomId` can never name one and the guard was
        // deleted as unnecessary rather than migrated. The invariant it served — *a save taken inside the
        // Dream loads you outside it, with nothing granted* — is asserted in the `pockets` section against a
        // real pocket, which is where it belongs now.

        // -- A save from before the format lost a field --
        // `SaveGame.ZonePhases` was deleted today (unit 2), and the claim in the comment standing where
        // it used to be — that an old save still loads, and loads *correctly* rather than merely without
        // crashing — is the one thing the round trip above cannot check, because every file it reads was
        // written by today's code. So this file is hand-authored in the old shape: a `ZonePhases` object
        // System.Text.Json has nowhere to put, and no `screen.*.cleansed` flag at all. Nothing in the
        // game can produce it any more, which is exactly why it needs a gate rather than a comment.
        const string oldPath = "user://verify_slice_legacy.json";
        const string restorePath = "user://verify_slice_restore.json";
        SaveService.Save(restorePath);

        Check("a save from before the format lost a field still loads",
            WriteLegacySave(oldPath, EntityState.Purified) && SaveService.Load(oldPath));
        Check("and its cleansed screen comes back",
            WorldStateManager.Instance.GetFlag(Flags.Cleansed(LoopRoomId)));

        // The other direction, and it is what makes the line above a derivation rather than a default:
        // the same file with the creature still standing on that screen has to come back uncleansed.
        Check("the same save with the creature still standing loads too",
            WriteLegacySave(oldPath, EntityState.Corrupted) && SaveService.Load(oldPath));
        Check("and that screen comes back uncleansed, from what is standing on it",
            !WorldStateManager.Instance.GetFlag(Flags.Cleansed(LoopRoomId)));

        // Put back exactly what this section used to leave behind, rather than whatever the legacy file
        // happened to carry: the gates below inherit this world.
        SaveService.DeleteSave(oldPath);
        Check("the world this section borrowed is handed back", SaveService.Load(restorePath));
        SaveService.DeleteSave(restorePath);
    }

    /// <summary>
    /// A save file in the pre-unit-2 shape, for the two checks at the end of
    /// <see cref="GatePersistence"/>: the deleted <c>ZonePhases</c> member present, and no
    /// <c>screen.&lt;id&gt;.cleansed</c> flag anywhere.
    /// </summary>
    /// <remarks>
    /// Written as text rather than through <see cref="SaveService"/> because no code path can produce
    /// this file any more — the field it turns on is gone from <see cref="SaveGame"/>. A fixture that
    /// could be generated by the code under test would not be testing anything.
    ///
    /// <para>
    /// The room and the flag were re-pointed at the verifier's own fixture by unit 3b, but
    /// <c>ZonePhases</c> keeps its <c>"waystation"</c> key deliberately: this file is a *period piece*,
    /// and the zone it names is exactly as deleted as the field that holds it. Renaming the key to
    /// something current would make the fixture describe a save that never existed.
    /// </para>
    /// </remarks>
    private static bool WriteLegacySave(string path, EntityState beast)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null) return false;

        file.StoreString($@"{{
  ""Version"": 1,
  ""CurrentRoomId"": ""{LoopRoomId}"",
  ""CurrentRealm"": 0,
  ""Resolve"": 100,
  ""Vitality"": 100,
  ""ReturnCampRoomId"": """",
  ""Flags"": {{ ""{LoopBenchFlag}"": true }},
  ""ZonePhases"": {{ ""waystation"": 2 }},
  ""EntityStates"": {{ ""{LoopBeast}"": {(int)beast} }},
  ""Inventory"": {{}},
  ""Stash"": {{}},
  ""DiscoveredBlueprints"": [],
  ""Wounds"": {{}}
}}");
        return true;
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

        // -- Closing part of a wound back up --
        // `CombatManager.Mend`, the mirror of `Wound` and the arithmetic under the boss aura's per-turn
        // regeneration (`combat.md` §2, built 2026-08-05). The clamping belongs here rather than in the arena
        // section because these are claims about the *ledger*; whose turn the heal lands on and whether the
        // tether permits it at all are claims about a fight, and `GateEncounters` makes those over a live one.
        const string Mended = "verify_mended_01";

        combat.Wound(Mended, body, DamageType.Edge, 20);
        Check("closing part of a wound leaves the rest standing",
            combat.Mend(Mended, 5) == 5 && combat.WoundsOn(Mended) == 15);
        Check("and asking for more than stands closes only what stood, rather than running past nothing",
            combat.Mend(Mended, 500) == 15 && combat.WoundsOn(Mended) == 0);

        // Mended to nothing has to be *indistinguishable* from never fought: `ImportWounds` drops
        // non-positive entries on the way in, so a 0 left sitting in the dictionary would come back absent
        // and the ledger would disagree with itself across a save.
        Check("and a body closed all the way is absent from the ledger rather than a zero in it",
            !combat.ExportWounds().ContainsKey(Mended));

        Check("a body carrying nothing has nothing to close, and says so",
            combat.Mend(Mended, 10) == 0 && combat.WoundsOn(Mended) == 0);
        Check("and neither has one nobody ever fought", combat.Mend("verify_no_such_body_at_all", 10) == 0);

        combat.Wound(Mended, body, DamageType.Edge, 20);
        Check("closing nothing moves nothing, so a creature with no aura cannot narrate a heal",
            combat.Mend(Mended, 0) == 0 && combat.Mend(Mended, -5) == 0
            && combat.WoundsOn(Mended) == 20);

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
        inv.AddItem("mat_tallow", 5);
        Check("a full satchel still will not craft in the open",
            !inv.CanCraft("recipe_salve_clearmind"));
        Check("and the refusal names the venue, not the materials",
            inv.Inspect("recipe_salve_clearmind") == CraftBlock.NotAtStation);

        // Order inside Inspect, which is the part that is easy to get wrong: an unlearned recipe must
        // stay Unknown even in the open. Answering NotAtStation first would confirm the recipe exists to
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

        // The other half of that pair — *the bench and the station are shut inside a **pocket*** — used a live
        // heist until 6b and now lives in the `pockets` section, where it is driven against a real pocket and
        // each arm has a control on the same ground. Only the realm-versus-run distinction stays here,
        // because it is about `AtCamp` rather than about the Dream.
        nav.GoToRoom(StationRoomId);
        Check("a room can be a venue without being a camp", !camp.AtCamp);
        Check("and its recipe is makeable there", inv.Inspect(StationRecipeId) == CraftBlock.None);
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
        inv.AddItem("herb_bitterroot", 3);     // a farmable common
        inv.AddItem("catalyst_ashincense", 1); // a craftable catalyst
        inv.AddItem("salve_clearmind", 2);     // hers, finite, uncraftable
        // **The two the deleted mechanism destroyed.** Named here rather than left to the common above,
        // because they are the reason it was deleted: both default to `Component`, the world holds 12 and 6,
        // and neither ever comes back. If item loss is ever reintroduced, these two lines are what should
        // stop it.
        inv.AddItem("herb_graveflower", 2);
        inv.AddItem("mat_cairnstone", 1);
        // Something the collapse must *not* touch, and it has to be a fourth id rather than more of the
        // three above, so that "the cache was not reached into" cannot be satisfied by a satchel stack.
        // Was `herb_gravecap` until unit 3b deleted that item; safe to reuse graveflower because
        // `ResetAll` above emptied both the satchel and the cache.
        inv.AddItem("herb_graveflower", 4);
        Check("something is put in the cache before dying", inv.Deposit("herb_graveflower", 4));

        nav.GoToRoom("verify_wilds");
        vitality.SetTo(vitality.Max - 10f);
        resolve.SetTo(resolve.Max * 0.3f);
        vitality.Damage(9999f);

        Check("collapsing wakes you at the last bench you stood at",
            nav.CurrentRoomId == "verify_camp_two");
        Check("with the body back", Mathf.IsEqualApprox(vitality.Current, vitality.Max));
        Check("and the nerve back", Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        // -- What a collapse costs, and as of unit 8 the answer is *the walk back and nothing else* --
        //
        // **Eight checks stood here asserting what burned, and they went with the mechanism.** They read
        // *"components are gone, because you can go and get more"* and its siblings over `SurvivesDeath` —
        // and that comment was the tell: it is true of Bitterroot and **false of `herb_graveflower` and
        // `mat_cairnstone`**, neither of which serialises a `Category`, so both fell to `Component` and both
        // burned. Twelve Graveflower exist, they never re-arm, and the engine costs eight. Dying deleted the
        // good ending.
        //
        // **The invariant did not move, it inverted** — which is the interesting case for
        // `deleting-a-gated-mechanism`'s rule. The old claim was *a new category inherits the destructive
        // default*; there is no destructive default now, so the claim has nothing to protect. What replaces
        // it is stronger and needs one check instead of eight: **a collapse takes nothing at all.**
        //
        // Driven live rather than against a predicate, because a predicate is what lied last time: the
        // category was read off the *registry*, so an id that resolved to nothing defaulted to Component
        // whatever it was authored as. Every category that exists is in the satchel here, and the two scarce
        // nouns are named individually so the softlock cannot come back under a different default.
        Check("nothing in the satchel was taken by the collapse",
            inv.GetCount("herb_bitterroot") == 3 &&
            inv.GetCount("catalyst_ashincense") == 1 &&
            inv.GetCount("salve_clearmind") == 2);
        Check("least of all the two the world holds a fixed number of",
            inv.GetCount("herb_graveflower") == 2 && inv.GetCount("mat_cairnstone") == 1);
        Check("and dying does not reach into the cache either",
            inv.GetStashCount("herb_graveflower") == 4);

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
        inv.AddItem("mat_tallow", 3);
        Check("something is cached for the round trip", inv.Deposit("mat_tallow", 3));
        nav.GoToRoom("verify_wilds");
        Check("the camp save writes", SaveService.Save(path));

        camp.ResetAll();
        inv.ResetAll();
        Check("the return point was actually cleared", camp.ReturnCampRoomId == "");
        Check("and the cache with it", inv.GetStashCount("mat_tallow") == 0);

        Check("the camp save loads", SaveService.Load(path));
        Check("and it puts you back on open ground, where nothing can rebind",
            nav.CurrentRoomId == "verify_wilds");
        Check("so the return point came out of the file", camp.ReturnCampRoomId == "verify_camp");
        Check("and so did the cache", inv.GetStashCount("mat_tallow") == 3);

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
    /// The verifier's own rooms and its one heist, registered once before any gate runs, all under a
    /// <c>verify_</c> prefix and a <c>ChapterId</c> of <c>"verify"</c> that the authoring sweeps skip.
    ///
    /// Registered up front rather than inside the gates that use them because
    /// <see cref="GatePersistence"/> needs somewhere to stand that Jeremy cannot turn into a camp.
    /// It saves a wounded bar and loads it back, and arriving at a camp refills both pools — so
    /// taking that round trip in an authored room would go red the day a workbench lands in it.
    /// </summary>
    /// <remarks>
    /// Ten rooms and one heist since unit 3b, which is worth stating plainly: <b>every fixture the
    /// gates below stand on is built here, in code, and none of it is content.</b> Before the
    /// retirement the first four gate sections stood on eight authored <c>.tres</c> files that existed
    /// for no other reason, and the cost of that was paid twice — once when the boot room moved and
    /// every one of them silently meant "wherever the game put me", and again when the prototype came
    /// to be deleted. A gate that names its fixture can have the fixture swapped under it.
    /// </remarks>
    private static void RegisterVerifyRooms()
    {
        var nav = NavigationManager.Instance;
        if (nav == null) return;

        nav.DebugRegisterRoom(VerifyRoom("verify_camp", "A Bench Under Canvas", withBench: true));
        nav.DebugRegisterRoom(VerifyRoom("verify_camp_two", "A Second Bench", withBench: true));
        nav.DebugRegisterRoom(VerifyRoom("verify_wilds", "Open Ground", withBench: false));
        nav.DebugRegisterRoom(VerifyRoom(StationRoomId, "A Mortar In The Open",
            withBench: false, station: StationId));

        nav.DebugRegisterRoom(CueRoom("verify_cues", painted: false));
        nav.DebugRegisterRoom(CueRoom("verify_cues_painted", painted: true));

        nav.DebugRegisterRoom(BlightRoom("verify_blighted", withCreature: false));
        nav.DebugRegisterRoom(BlightRoom("verify_held", withCreature: true));

        nav.DebugRegisterRoom(ArenaRoom());
        nav.DebugRegisterRoom(PocketDoorRoom());

        // The retired prototype's replacement: two Waking screens and the closed three-room Dream
        // chain the surviving heist-shaped probes run inside.
        nav.DebugRegisterRoom(LoopRoom());
        nav.DebugRegisterRoom(LoopInnerRoom());

        // One pocket registered up front, for `GateTravel`'s *"the camp verb refuses inside a pocket"* pair.
        // `GatePockets` registers its own three when it runs; this one is here because the travel section
        // runs first and must not depend on a later section's fixtures.
        PocketManager.Instance?.DebugRegisterPocket(new PocketData
        {
            Id = TravelPocketId,
            DisplayName = "Somewhere Else",
            Kind = PocketKind.Knowledge,
        });
    }

    /// <summary>
    /// A fixture for <see cref="GateEncounters"/>: seven bodies on one screen, each shaped for exactly one
    /// question the turn loop has to answer. Synthetic for <see cref="GateWoundLedger"/>'s reason and one
    /// more of its own — a gate that fights an authored creature <b>wins</b>, and a win writes a
    /// <c>freed.*</c> or <c>defeated.*</c> flag that the ending's bill reads as something the player did.
    ///
    /// <para>
    /// Every field here exists to keep a check honest rather than to describe a creature. The frail pair
    /// die to one bare-handed strike, so the three win routes cost one turn each and the gate reads a flag
    /// instead of a loop counter. The durable ones cannot be finished by accident, so a fight can be
    /// started and abandoned repeatedly without the fixture running out. And <see cref="ArenaTough"/> and
    /// <see cref="ArenaCut"/> share one <see cref="EntityData"/> on purpose: the wound-persistence pair
    /// must differ in their <see cref="EntityState"/> and in <i>nothing else</i>, which is hard to promise
    /// with two files and free with one.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This is the only <c>verify_</c> room that gets navigated to with creatures on it, so unlike
    /// <c>verify_held</c> every placement here is complete — <c>RoomView.BuildEntities</c> warns on a
    /// placement with no <c>Entity</c>, and a pushed warning per gate run is noise that trains people to
    /// ignore the log.
    /// </remarks>
    private static RoomData ArenaRoom()
    {
        var room = new RoomData
        {
            Id = ArenaRoomId,
            DisplayName = "Somewhere To Fight",
            ChapterId = "verify",
        };

        // Frail to exactly the weakest attack in the game, so "one strike ends it" holds whatever the gate
        // above happened to leave equipped. The two differ only in `PurifiedName`, which is the authored
        // discriminator between "there was a person in there" and "there was never anyone".
        var husk = Arena("verify_arena_husk", GameConfig.BareHandedPotency, purifiedName: "");
        var person = Arena("verify_arena_person", GameConfig.BareHandedPotency, purifiedName: "Somebody");

        // Typed so the resolver has work to do — 2x to Edge, 0.5x to Blunt — and durable enough that
        // nothing below finishes it by accident.
        var tough = Arena("verify_arena_tough", ArenaDurable, purifiedName: "Somebody");
        tough.VulnerableTo = DamageType.Edge;
        tough.ResistantTo = DamageType.Blunt;

        // The two that hit back, and they differ in `Deals` and nothing else, because that is the single
        // field the armour test turns on. Anything else differing would leave the ward check able to pass
        // for the wrong reason.
        var edged = Arena("verify_arena_edged", ArenaDurable, purifiedName: "Somebody");
        edged.VulnerableTo = DamageType.Blunt;
        edged.ResistantTo = DamageType.Caustic;
        edged.Deals = DamageType.Edge;
        edged.Damage = ArenaBlow;

        var burning = Arena("verify_arena_burning", ArenaDurable, purifiedName: "Somebody");
        burning.VulnerableTo = DamageType.Blunt;
        burning.ResistantTo = DamageType.Caustic;
        burning.Deals = DamageType.Fire;
        burning.Damage = ArenaBlow;

        // The one the panel gate fights, and the only body here that has to be *finishable in a few
        // turns* — the strike loop reports the turn count §5 asks for, so a durable body would report the
        // hang guard instead. Its two types are set and deliberately distinct, because the reading the
        // panel prints has to be checkable for both halves at once; `Damage` is 1 so it can hit back
        // through three turns without the bar mattering.
        var shown = Arena("verify_arena_shown", 30, purifiedName: "Somebody");
        shown.VulnerableTo = DamageType.Fire;
        shown.Deals = DamageType.Caustic;
        shown.Damage = 1;

        // The only bodies here with an aura, and there are two placements of **one** `EntityData` on purpose:
        // the tether rule is *"`Corrupted` drains, `Severed` does not"*, so the pair that tests it has to
        // differ in state and in nothing else. `ArenaTough`/`ArenaCut` share a body for the same reason.
        var anchored = Arena("verify_arena_anchored", ArenaAnchoredDurability, purifiedName: "Somebody");
        anchored.VulnerableTo = DamageType.Edge;
        anchored.CorruptionAura = ArenaAura;

        var faint = Arena("verify_arena_faint", ArenaSlight, purifiedName: "Somebody");
        faint.CorruptionAura = ArenaAura;

        room.Entities.Add(new EntityPlacement { InstanceId = ArenaBeast, Entity = husk });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaFreed, Entity = person });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaKilled, Entity = person });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaTough, Entity = tough });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaCut, Entity = tough });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaHitter, Entity = edged });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaBurner, Entity = burning });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaShown, Entity = shown });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaAnchored, Entity = anchored });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaLoosed, Entity = anchored });
        room.Entities.Add(new EntityPlacement { InstanceId = ArenaFaint, Entity = faint });

        // A way out, because a fight that outlives the screen it started on was a real defect and only
        // `--shots` could see it — every other check in this section stands in one room and never moves.
        // It leads to `verify_wilds` rather than a camp: arriving at a bench refills both pools, which
        // would wash away the parting blow the door is supposed to charge.
        room.Hotspots.Add(new HotspotData
        {
            Id = ArenaDoor,
            Label = "A Way Out",
            Kind = HotspotKind.Navigate,
            TargetRoomId = "verify_wilds",
        });

        return room;
    }

    /// <summary>
    /// A body with nothing set that the gate does not need. <see cref="EntityData.Damage"/> is zeroed
    /// rather than left at its default of 8, because a creature that hits back is the exception here:
    /// every fight the gate runs for a reason other than the ward test has to be able to go long without
    /// draining the bar into a collapse and a respawn that navigates out of the room.
    /// </summary>
    private static EntityData Arena(string id, int durability, string purifiedName) => new()
    {
        Id = id,
        CorruptedName = "Something In The Way",
        PurifiedName = purifiedName,
        Durability = durability,
        Deals = DamageType.None,
        Damage = 0,
    };

    private const string ArenaRoomId = "verify_arena";

    private const string ArenaBeast = "verify.arena_beast";
    private const string ArenaFreed = "verify.arena_freed";
    private const string ArenaKilled = "verify.arena_killed";
    private const string ArenaTough = "verify.arena_tough";
    private const string ArenaCut = "verify.arena_cut";
    private const string ArenaHitter = "verify.arena_hitter";
    private const string ArenaBurner = "verify.arena_burner";
    private const string ArenaShown = "verify.arena_shown";
    private const string ArenaAnchored = "verify.arena_anchored";
    private const string ArenaLoosed = "verify.arena_loosed";
    private const string ArenaFaint = "verify.arena_faint";

    private const string ArenaDoor = "verify_arena_door";

    /// <summary>Enough that no sequence of strikes below can finish one of the durable bodies.</summary>
    private const int ArenaDurable = 400;

    /// <summary>What the two hitters hit for, chosen so halving it lands on a whole number.</summary>
    private const int ArenaBlow = 10;

    /// <summary>
    /// The aura the fixture bosses project — a quarter of the bar per player turn, so a full bar is gone in
    /// exactly four.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately not the number the design ships.</b> §6 sizes a real boss's aura to empty a bar in
    /// about six turns, and a fixture set to that would make every check here a check on the tuning: move
    /// the shipping number and the gate goes red having found nothing. Four turns divides the 100-point bar
    /// into whole quarters, which is the only property the checks below actually read.
    /// </remarks>
    private const float ArenaAura = 0.25f;

    /// <summary>
    /// A body slight enough that <c>GameConfig.AnchoredRegenFraction</c> of it rounds to nothing —
    /// 8 × 0.05 = 0.4. The floor of 1 in <c>EncounterManager.Regenerate</c> has no other witness, and a
    /// regenerating boss that measurably does not regenerate is the bug it exists to stop.
    /// </summary>
    private const int ArenaSlight = 8;

    /// <summary>
    /// Durability for the fixture boss, and it is <see cref="ArenaDurable"/> halved for a reason rather than
    /// for variety: at 400 the regen fraction comes to 20 a turn, which is exactly what the axe does to a body
    /// vulnerable to Edge — and a heal that ties with the only weapon in hand is the lock
    /// <c>GameConfig.AnchoredRegenFraction</c> is written to stay clear of. At 200 it closes 10 against the
    /// axe's 20, so the constraint is *checked* below rather than assumed. Still far more than the few turns
    /// here can spend.
    /// </summary>
    private const int ArenaAnchoredDurability = ArenaDurable / 2;

    /// <summary>
    /// What the waystation yard was, in code: a barred road, a creature barring it, a two-step puzzle
    /// that exposes the way across, and a harvest node that only appears once the screen is clean.
    /// Unit 3b deleted the prototype and this is where its surviving coverage stands now — the shapes
    /// below are copied field for field from <c>waystation_yard.tres</c>, because the gates that read
    /// them are gates about <see cref="InteractionService"/> and <see cref="EntityStateManager"/> and
    /// were never gates about a rat.
    ///
    /// <para>
    /// <b>Not <see cref="ArenaRoom"/>, and the reason is ordering.</b> The migrated micro-loop purifies
    /// its creature and cleanses its screen; <see cref="GateEncounters"/> runs later and needs the arena
    /// bodies still Corrupted. One fixture serving both would make <c>RunGates</c>' order load-bearing in
    /// a new way, and that order is already the most fragile thing in this file.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The two harvest nodes yield <b>one bitterroot each</b> where the prototype's yielded two and a
    /// gravecap, and that is deliberate rather than tidy: <c>SliceVerifier.cs:100</c>, <c>:406</c> and
    /// <c>:1910</c> all record that later sections inherit the exact stacks the micro-loop harvested, so
    /// the totals have to come out at two bitterroot and one tallow. Seeding them with
    /// <c>AddItem</c> instead would have deleted the coverage that <i>harvesting</i> is what put them
    /// there. Gravecap is gone entirely — it had one source in the game, that source was the shed, and
    /// no recipe wanted one.
    /// </remarks>
    private static RoomData LoopRoom()
    {
        var room = new RoomData
        {
            Id = LoopRoomId,
            DisplayName = "The Road, Barred",
            ChapterId = "verify",
        };

        room.Entities.Add(new EntityPlacement { InstanceId = LoopBeast, Entity = LoopCreature() });

        // `yard_to_shed`: refused by `BlockedByEntityId` while the body is Corrupted, which is a branch
        // that sits *ahead* of `IsAvailable` in `ActivateHotspot` and so cannot be probed by a flag.
        room.Hotspots.Add(new HotspotData
        {
            Id = LoopDoor,
            Label = "Further In",
            Kind = HotspotKind.Navigate,
            TargetRoomId = LoopInnerRoomId,
            BlockedByEntityId = LoopBeast,
            BlockedText = "verify: it is in the way.",
        });

        // `yard_cairn`: gated on its own flag being *false*, so it fires exactly once.
        room.Hotspots.Add(new HotspotData
        {
            Id = LoopCairn,
            Label = "Something Piled Up",
            Kind = HotspotKind.Interact,
            ExamineText = "verify: there is something under it.",
            RequiredFlag = LoopRelicFlag,
            RequiredFlagValue = false,
            SetsFlag = LoopRelicFlag,
        });

        // `yard_relic`: hidden until the cairn is searched. **An `Interact` since unit 6b, and the change of
        // kind costs nothing** — it was a `CrossToDream` because it covered the wiring from a Waking click to
        // the heist's `Begin`, and `verify_pocket_door` covers that wiring against a real pocket now. What
        // M3 asks of this hotspot has nothing to do with the Dream: it is the *flag-gated hotspot appears
        // once the cairn is searched* half of the micro-loop, and `IsAvailable` is all either check reads.
        room.Hotspots.Add(new HotspotData
        {
            Id = LoopRelic,
            Label = "The Thing Under It",
            Kind = HotspotKind.Interact,
            RequiredFlag = LoopRelicFlag,
        });

        // `yard_bitterroot`: the payoff, and it does not exist until the screen is clean.
        room.Hotspots.Add(new HotspotData
        {
            Id = LoopHarvest,
            Label = "Something Growing",
            Kind = HotspotKind.Harvest,
            ItemId = "herb_bitterroot",
            ItemAmount = 1,
            RequiredFlag = Flags.Cleansed(LoopRoomId),
        });

        return room;
    }

    /// <summary>
    /// What the waystation shed was: somewhere behind the barred door, holding a bench and a harvest
    /// node with no flag on it — the take-once-then-refuse probe, which needs a node that starts
    /// available and stops being available by being taken.
    /// </summary>
    private static RoomData LoopInnerRoom()
    {
        var room = new RoomData
        {
            Id = LoopInnerRoomId,
            DisplayName = "Behind The Road",
            ChapterId = "verify",
        };

        room.Hotspots.Add(new HotspotData
        {
            Id = LoopInnerDoor,
            Label = "Back Out",
            Kind = HotspotKind.Navigate,
            TargetRoomId = LoopRoomId,
        });

        room.Hotspots.Add(new HotspotData
        {
            Id = LoopInnerHarvest,
            Label = "Something Growing",
            Kind = HotspotKind.Harvest,
            ItemId = "herb_bitterroot",
            ItemAmount = 1,
        });

        room.Hotspots.Add(new HotspotData
        {
            Id = LoopInnerBench,
            Label = "Your Workbench",
            Kind = HotspotKind.Workbench,
            SetsFlag = LoopBenchFlag,
        });

        return room;
    }

    /// <summary>
    /// The body in the road. Shaped to the three things the state table and the micro-loop ask of it:
    /// two names so <c>Resolve</c> has a wrong field to pick, a <see cref="EntityData.HarvestItemId"/>
    /// so a Purified body can be farmed, and <see cref="EntityData.Damage"/> zeroed so ten turns of
    /// bare-handed work cannot collapse the player and navigate the gate out of its own room.
    /// <see cref="EntityData.Durability"/> is left at the authored default so the strike loop is still
    /// a loop — a one-hit body would pass the same check while measuring nothing.
    /// </summary>
    private static EntityData LoopCreature() => new()
    {
        Id = "verify_loop_creature",
        CorruptedName = LoopCorruptedName,
        PurifiedName = LoopPurifiedName,
        HarvestItemId = "mat_tallow",
        Deals = DamageType.None,
        Damage = 0,
    };

    // **`DreamRoom` and `LoopHeist` stood here and went with unit 6b.** They built a three-room synthetic
    // maze and a code-authored heist so that five surviving files could be probed from inside a run — the
    // fixture unit 3b kept alive on purpose when it deleted the authored prototype. `PocketManager` needs
    // none of it: a pocket is a manager fact, so a gate enters one with no rooms, no doors and no geometry,
    // which is the property that made this deletion a deletion rather than another migration.
    //
    // The three rooms had a subtlety worth remembering if a graph-shaped fixture is ever needed again: the
    // chain had to be **closed**, because `HeistOwningRoom` derived the maze by walking Navigate hotspots out
    // of the start room, so one door from a Dream room to a Waking one would have put the entire registry
    // inside the heist. Deriving reachability is cheap and its blast radius is the whole graph.

    private const string LoopRoomId = "verify_loop";
    private const string LoopInnerRoomId = "verify_loop_inner";

    private const string LoopDoor = "verify_loop_door";
    private const string LoopCairn = "verify_loop_cairn";
    private const string LoopRelic = "verify_loop_relic";
    private const string LoopHarvest = "verify_loop_harvest";

    private const string LoopInnerDoor = "verify_loop_inner_door";
    private const string LoopInnerHarvest = "verify_loop_inner_harvest";
    private const string LoopInnerBench = "verify_loop_inner_bench";

    private const string LoopRelicFlag = "verify.loop.relic_found";
    private const string LoopBenchFlag = "verify.loop.bench_found";

    /// <summary>
    /// The two names <c>Resolve</c> has to choose between. Compared against by the state-presentation
    /// table, and they are consts rather than literals for one reason: the table is checking that the
    /// resolver reads the <i>right field for the state</i>, and two distinct strings are all that takes.
    /// </summary>
    private const string LoopCorruptedName = "Something In The Road";
    private const string LoopPurifiedName = "A Plain Animal";


    /// <summary>
    /// A fixture for <see cref="GateMending"/>: one blight, and optionally one creature still standing
    /// on the same screen. Built in code for the reason <see cref="CueRoom"/> is — the interesting case
    /// is a screen that is <i>held</i> and <i>fouled</i> at once, which no authored room is, because
    /// every authored blight screen has to stay playable while its prose is unwritten.
    ///
    /// <para>
    /// Mending the real blights instead would be worse than synthetic: the twelve are content, and a gate
    /// that mends one leaves a flag behind that a later gate reads as progress. These two rooms are
    /// disposable, so the mechanism can be driven in both directions and reset.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The creature is a placement with an <c>InstanceId</c> and <b>no <c>Entity</c></b>, which is a
    /// shape <see cref="GateFights"/> fails authored content for. That is deliberate and it is why
    /// <c>verify_held</c> is never navigated to: <see cref="MendManager.IsHeld"/> reads the id and the
    /// state and nothing else, so a body is all this fixture needs, while <c>RoomView</c> would want an
    /// overlay to draw. The authoring sweeps skip every <c>verify_</c> room, so the shape is not a lie
    /// told to a gate — it is a room no gate reads as content.
    /// </remarks>
    private static RoomData BlightRoom(string id, bool withCreature)
    {
        var room = new RoomData { Id = id, DisplayName = "Broken Ground", ChapterId = "verify" };

        room.Hotspots.Add(new HotspotData
        {
            Id = $"{id}_blight",
            Label = "Something Broken",
            Kind = HotspotKind.Mend,
            ItemId = "herb_graveflower",
            ItemAmount = 1,
            MendPuzzle = new MendPuzzleData
            {
                Prompt = "verify: put it back together.",
                Parts = new[] { "the near end", "the far end", "the piece that is wrong" },
                Solution = new[] { 2, 0 },
            },
        });

        if (withCreature)
            room.Entities.Add(new EntityPlacement { InstanceId = HeldCreature });

        return room;
    }

    private const string HeldCreature = "verify.held_01";

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
    ///
    /// <para>
    /// <paramref name="station"/> gives the room a field station and no bench, which is the one venue
    /// shape unit 3b's break-test found nothing was standing on. See <see cref="StationRoomId"/>.
    /// </para>
    /// </summary>
    private static RoomData VerifyRoom(string id, string displayName, bool withBench,
        string station = "")
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

        if (!string.IsNullOrEmpty(station))
        {
            room.Hotspots.Add(new HotspotData
            {
                Id = $"{id}_station",
                Label = "A Station",
                Kind = HotspotKind.Station,
                StationId = station,
            });
        }

        return room;
    }

    /// <summary>
    /// A field station standing on ground that is <b>not</b> a camp — an anvil in the open, which is
    /// the shape <c>InventoryCraftingManager.AtVenueFor</c>'s Dream guard exists for and the shape
    /// nothing was probing.
    ///
    /// <para>
    /// <b>Found by a break-test rather than by reading.</b> Unit 3b's third break-test unregistered the
    /// synthetic run and expected the camp, crafting and stranding checks to go red together; the camp
    /// and stranding ones did and no crafting check moved, because there was no crafting check. The
    /// guard's own remarks say it is *load-bearing on the field-station arm only* — <c>AtCamp</c>
    /// already refuses inside a run, so the bench arm passes whether the guard is there or not — and a
    /// probe standing at a bench would have been exactly the unfalsifiable check
    /// <c>deleting-a-gated-mechanism</c> warns about. It needed a venue that is a station and nothing
    /// else, and no fixture had one.
    /// </para>
    ///
    /// <para>
    /// <b>The recipe is the one station recipe with no blueprint</b>, and that is why it is this one
    /// rather than a tool. <c>Inspect</c> checks knowledge *before* the venue and deliberately so — an
    /// unlearned recipe refuses on ignorance rather than revealing itself by complaining about where you
    /// are standing — so a probe on any of the other ten would read <c>Unknown</c> in both arms and
    /// measure nothing. Discovering the blueprint first would work and would also make this probe fail
    /// the day the discovery API moves, for a reason that has nothing to do with what it asserts.
    /// </para>
    /// </summary>
    private const string StationRoomId = "verify_station";
    private const string StationId = "mortar";
    private const string StationRecipeId = "recipe_salve_clearmind";

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
        inv.AddItem("mat_tallow", 1);
        panel.Open();
        Check("a pickup made while it was down is there on opening", panel.SlotCount == 2);
        inv.RemoveItem("mat_tallow", 1);
        Check("and the grid is back to one when it goes", panel.SlotCount == 1);

        // The load case, which is where a deferred rebuild actually bites: ImportStacks replaces
        // the whole satchel at once, and it happens while no panel is on screen.
        panel.Close();
        inv.ImportStacks(new Dictionary<string, int> { ["salve_clearmind"] = 1, ["mat_tallow"] = 2 });
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

        // -- Reading a tile without hovering it (the first playtest, 2026-08-03) --
        // *"I didn't realize I had the axe"*: a swatch is a colour, and a tooltip needs you to already
        // suspect something is there. So every slot carries its name, and the name is the manager's word
        // for the item rather than the id — a grid of `mat_scrap_iron` would pass a weaker check.
        inv.ResetAll();
        inv.AddItem("salve_clearmind", 2);
        inv.AddItem("herb_bitterroot", 1);
        panel.Open();

        var names = panel.SlotNames.ToList();
        Check("every drawn slot carries a caption", names.Count == panel.SlotCount && names.All(n => n != ""));
        Check("and it is the item's display name, not its id",
            names.Contains(inv.GetItem("salve_clearmind").DisplayName) &&
            names.Contains(inv.GetItem("herb_bitterroot").DisplayName));

        Check("a stack count is drawn dark enough to read against a pale swatch", panel.SlotCountsAreLegible);
        // The other half of that stopgap, and the half no screenshot would ever show: the override steps
        // aside the day the scene sets a colour of its own.
        Check("and the code override defers to a colour the scene sets", InventoryPanel.CountColourDefersToTheScene());

        // -- Where the caption and the count sit on the tile --
        // The second playtest, and both halves of it were code I wrote for the first. The caption stopped
        // 48px short of the right edge to keep clear of the count, which left about eight characters and cut
        // every longer name — *"Cairnsto"*, *"Corrosiv"*, *"Rendere"*. So the band takes the whole tile and
        // the count moves instead. A screenshot found this; the three checks below are what would have.
        var (caption, count, swatch) = InventoryPanel.CaptionLayoutOnATile();
        Check("the caption band is at least as wide as the swatch it labels", caption.Size.X >= swatch.Size.X);
        Check("and the stack count sits on the swatch rather than off its corner", swatch.Encloses(count));
        // *"The number of items needs to move left a tiny bit to be on the icon where it should be."*
        Check("with the caption and the count clear of one another", !caption.Intersects(count));
        // Stronger than the check above, and the one that actually pins the arrangement: *above*, not merely
        // *not overlapping*. Two disjoint boxes pass `!Intersects` by being side by side, which is the wrong
        // fix — the number belongs over the artwork, clear of the text below it.
        Check("and the count sits above the caption rather than beside it", count.End.Y <= caption.Position.Y);
        // *"Everything is right besides it being in the middle."* The round before this one made the swatch
        // square again and held the count's box on the same absolute pixels, which every check above passed —
        // and on a swatch 40px taller those pixels are the middle of the icon. So this measures the two *gaps*
        // rather than a position: whatever the swatch's height, the number is the same few pixels in from the
        // corner it is meant to be in. `Encloses` above says it is on the artwork; this says where.
        Check("with the count the same few pixels in from both of the swatch's near edges",
            Mathf.IsEqualApprox(swatch.End.X - count.End.X, InventoryPanel.CountInset) &&
            Mathf.IsEqualApprox(swatch.End.Y - count.End.Y, InventoryPanel.CountInset));
        // *"The placement is correct but it isn't square."* Asked for twice, so unlike the gate this replaced
        // it is a requirement rather than a description of whatever the last round happened to build. It holds
        // by *narrowing* the swatch to its height — the caption fixes the bottom and the scene fixes the top,
        // so the height is spoken for and the width is the only dimension left to move.
        Check("and the swatch a square, since squaring it is what narrowing it is for",
            Mathf.IsEqualApprox(swatch.Size.X, swatch.Size.Y));

        // -- The tab strip --
        // *"there should be a tab for each item type so go through them quicker/easier"* — but only for a
        // type you actually carry, so the strip is never a row of dead ends.
        Check("All is the tab on a satchel nobody has touched", panel.ActiveTabLabel == InventoryPanel.AllTabLabel);
        Check("and under it the grid is still one slot per held stack", panel.SlotCount == 2);

        Check("the two kinds carried each get a tab, beside All", panel.TabCount == 3);

        // **The reason in this label expired before the assertion did — corrected 2026-08-05.** It read
        // *"because nothing authored is Lore yet"*, and decision 11's ten pages made that false while leaving
        // the check green: the tab is absent because the satchel is not *carrying* a page, which is the rule
        // the strip actually follows. A label that explains a pass by the wrong mechanism is worse than none,
        // and no break-test can catch one — the assertion is fine, so nothing ever goes red.
        Check("and Lore gets none, because he is carrying no page",
            !panel.TabbedCategories.Contains(ItemCategory.Lore));

        panel.DebugSelectTab(ItemCategory.Salve);
        Check("a tab narrows the grid to its kind", panel.SlotCount == 1);
        Check("and says which kind that is in the player's word", panel.ActiveTabLabel == "Salves");
        Check("the one slot left is the salve",
            panel.SlotNames.Single() == inv.GetItem("salve_clearmind").DisplayName);

        // Selection is about what you last looked at, not about what is on screen now.
        panel.DebugSelect("herb_bitterroot");
        panel.DebugSelectTab(ItemCategory.Salve);
        Check("an item filtered out of the grid keeps the detail column",
            panel.DetailName == inv.GetItem("herb_bitterroot").DisplayName);

        panel.DebugSelectTab(null);
        Check("All puts everything back", panel.SlotCount == 2);

        // A tab arrives with the first of its kind and leaves with the last, which is the whole reason the
        // strip is rebuilt rather than authored: it grows as the game does.
        inv.AddItem("trap_snare", 1);
        Check("a new kind brings a tab with it", panel.TabCount == 4 &&
            panel.TabbedCategories.Contains(ItemCategory.Trap));

        panel.DebugSelectTab(ItemCategory.Trap);
        inv.RemoveItem("trap_snare", 1);
        Check("spending the last of a kind takes its tab away", panel.TabCount == 3 &&
            !panel.TabbedCategories.Contains(ItemCategory.Trap));
        // The failure this prevents is an empty grid with no obvious way out of it.
        Check("and the filter it was holding falls back to All",
            panel.ActiveTabLabel == InventoryPanel.AllTabLabel && panel.SlotCount == 2);

        // -- The Lore shelf, which has been a tab over nothing since it was built --
        // `handoff.md` §2 has said so for two units, and decision 11's pages are what fill it. Jeremy's job
        // for the tab is *"a sanity check that they have the recipe that they need"*, checkable from anywhere,
        // so the thing worth asserting is that a page reaches the grid at all.
        //
        // **The live question is whether the tab is a dead end**, because a page is the one kind of item that
        // <see cref="InventoryCraftingManager.CountsAgainstSlots"/> answers *false* for — so "gets a tab" and
        // "gets drawn" are separately deniable, and line 2029 above forbids the first without the second in as
        // many words: *the strip is never a row of dead ends*. The predicate turns out to drive ordering only,
        // which is the right answer and was not a foregone one.
        // **The fixture asserts it landed before anything reads it, which the break-test of 2026-08-05 is the
        // reason for.** Renaming this page's `Id` in `data/` made three of the four checks below go red and the
        // fourth — *"it leaves with the last page, putting the strip back"* — go **green**, because a tab that
        // never appeared is trivially gone again. `AddItem` does not refuse an unknown id, so a fixture built
        // on a file that moved fails open on every check phrased as a removal. One line here converts that
        // whole class into a red line that names the cause.
        inv.AddItem("lore_craft_edges", 1);
        Check("a page authored in data/ is a thing he can actually be handed",
            inv.Has("lore_craft_edges"));

        Check("carrying a page brings the Lore tab with it", panel.TabCount == 4 &&
            panel.TabbedCategories.Contains(ItemCategory.Lore));

        panel.DebugSelectTab(ItemCategory.Lore);
        Check("and the tab is no dead end — the page is drawn under it",
            panel.SlotCount == 1 && panel.SlotNames.Single() == inv.DisplayNameOf("lore_craft_edges"));
        Check("under the player's word for the shelf", panel.ActiveTabLabel == "Lore");

        // Restored deliberately rather than left for the next gate to reset, and the removal is not enough on
        // its own: **losing the page does not unlearn it** (`GateCraftingAndUse` asserts exactly that), so the
        // satchel comes back but the discovery set does not. `ResetAll` is what clears knowledge, and this
        // block is the first thing in the panel gates to put any in — leaving a learned craft family behind
        // would hand `GateCraftingPanel` a fixture in which his side of the book has a makeable page, which is
        // precisely the state its `SideToOpenOn` checks are written against not having.
        inv.RemoveItem("lore_craft_edges", 1);
        Check("and it leaves with the last page, putting the strip back",
            panel.TabCount == 3 && panel.SlotCount == 2 &&
            !panel.TabbedCategories.Contains(ItemCategory.Lore));
        Check("though what it taught stays taught", inv.IsDiscovered("lore_craft_edges"));

        // -- The Set button, which is where F8's invariant went --
        // **The verb `EncounterManager.Rig` spent two units without.** The fight screen cannot host it (§18:
        // rigging is free before a fight and costs a turn inside one, so it belongs on the room), so from
        // 2026-08-03 it was reachable only from a debug key and from this file. `%SetButton` retired the key
        // on 2026-08-05 and inherited the one invariant the key's probe was standing in for: **a verb the
        // player can reach fills a creature's sites out of the satchel.**
        //
        // **This is the only block in the section that navigates**, and it has to: Set is the one verb of the
        // three whose answer is a property of the *room*, so a check on it that inherits its room is checking
        // nothing in particular — the first class of default fixture, and the reason `verify_wilds` and
        // `verify_loop` are both named below rather than one being assumed.
        //
        // **It walks with the satchel shut and opens it on arrival, which is not tidiness.** The first draft
        // navigated with the panel up and left a green 945-check run that then died at teardown
        // (`0xC0000005` in `Godot.Collections.Array.Finalize`, after the summary printed — so the log said
        // pass and the exit code said crash). Closing across the move fixed it. The lesson is the one worth
        // keeping: **that state is not reachable in play** — you stand somewhere, *then* open the satchel;
        // `Hud.ToggleSatchel` is the only route in and the panel covers the screen while it is up — so the
        // gate had invented a configuration the game cannot produce, and was reading a button's colour in it.
        // A fixture the player cannot arrive at proves nothing about the button either way.
        var nav = NavigationManager.Instance;
        var encounters = EncounterManager.Instance;
        Check("the room and the encounter managers are present", nav != null && encounters != null);
        if (nav == null || encounters == null) return;

        var cameFrom = nav.CurrentRoomId;
        inv.ResetAll();
        encounters.ResetAll();

        // Derived rather than named, so a seventh trap needs no edit here — and asserted, because every
        // check below is a universal over a list that would go green by being empty.
        var setTraps = inv.Catalogue.Values
            .Where(i => i != null && i.Category == ItemCategory.Trap && !string.IsNullOrEmpty(i.Id))
            .Select(i => i.Id).OrderBy(id => id).ToList();
        Check($"the arsenal has more traps than a creature has sites ({setTraps.Count} traps, "
            + $"{GameConfig.RigSlotsPerFight} sites)",
            setTraps.Count > GameConfig.RigSlotsPerFight);
        if (setTraps.Count <= GameConfig.RigSlotsPerFight) return;

        foreach (var id in setTraps) inv.AddItem(id, 1);
        inv.AddItem("herb_bitterroot", 1);

        // Empty ground first, because the interesting half of a room-dependent verb is the room where it
        // must refuse — and a button that is live everywhere would pass every check below except this one.
        Check("open ground is walkable", nav.GoToRoom("verify_wilds") && nav.CurrentRoomId == "verify_wilds");
        Check("and nothing is standing on it", nav.SoleCreatureHere() == null);

        panel.Open();
        panel.DebugSelect(setTraps[0]);
        Check("a trap in hand with nothing to set it against is dead", !panel.SetEnabled);

        // **Pressed while greyed, on purpose.** `EmitSignal` ignores `Disabled`, so this is the pair that
        // says the grey and the refusal are one answer rather than two that happen to agree — the same
        // argument `InspectUse` and `CanUse` are built on, one verb over. A handler that read the room
        // wrongly would set the trap against nobody and this is the only line that would notice.
        Check("the button is there to be pressed at all", panel.DebugPressSet());
        Check("and pressing it anyway sets nothing, because the manager refuses too",
            nav.SoleCreatureHere() == null && encounters.RiggedAgainst(LoopBeast).Count == 0);

        // Shut before walking, per the note at the top of the block. Also why nothing here asserts that the
        // button repaints when the *room* changes under it: the panel subscribes to the satchel and the body
        // and not to arriving somewhere, which is right, because it cannot be up when that happens.
        panel.Close();
        Check("the road is walkable", nav.GoToRoom(LoopRoomId) && nav.CurrentRoomId == LoopRoomId);
        Check("and one body is standing in it",
            nav.SoleCreatureHere()?.InstanceId == LoopBeast);

        panel.Open();
        panel.DebugSelect(setTraps[0]);
        Check("the same trap on a screen with a body on it is live", panel.SetEnabled);

        panel.DebugSelect("herb_bitterroot");
        Check("but a reagent is not something you can set", !panel.SetEnabled);

        panel.DebugSelect(setTraps[0]);
        Check("pressing it puts the trap on the body",
            panel.DebugPressSet() && encounters.RiggedAgainst(LoopBeast).SequenceEqual(new[] { setTraps[0] }));

        // **The repaint nothing else would do.** Traps went permanent on 2026-08-01, so rigging *names* a
        // trap rather than spending one — no stack moves, so no `InventoryChanged` fires, so nothing but
        // `OnSetPressed`'s own `RefreshDetail` can grey this. Drop that one line and the button stays offered
        // and the next press answers `AlreadyRigged` with a line: a correct refusal to a question the screen
        // should not have allowed.
        Check("and the button greys itself behind the press, with no stack having moved",
            !panel.SetEnabled && inv.Has(setTraps[0]));

        for (var i = 1; i < GameConfig.RigSlotsPerFight; i++)
        {
            panel.DebugSelect(setTraps[i]);
            panel.DebugPressSet();
        }
        Check($"the sites fill to the dial's own number "
            + $"({encounters.RiggedAgainst(LoopBeast).Count} of {GameConfig.RigSlotsPerFight})",
            encounters.RiggedAgainst(LoopBeast).Count == GameConfig.RigSlotsPerFight);

        panel.DebugSelect(setTraps[GameConfig.RigSlotsPerFight]);
        Check("and the next trap is dead with nowhere to put it", !panel.SetEnabled);
        Check("pressing it changes nothing",
            panel.DebugPressSet() &&
            encounters.RiggedAgainst(LoopBeast).Count == GameConfig.RigSlotsPerFight);

        // Put back what this block moved. Nothing below currently reads either — `GateCraftingPanel` runs
        // next and navigates explicitly — but a gate that moves the player and leaves them moved makes its
        // own position in `RunGates` load-bearing, which is how a section's order becomes a fixture nobody
        // wrote down.
        encounters.ResetAll();
        inv.ResetAll();
        panel.Close();
        nav.GoToRoom(cameFrom);
        Check("and the room it borrowed is handed back", nav.CurrentRoomId == cameFrom);

        Check("it is left closed for whatever runs next", !panel.IsOpen);
    }

    // ---- The journal -----------------------------------------------------

    /// <summary>
    /// Unit 7a's panel: one book, two sides, and a row that is the verb. `build-plan.md` §7 decision 9 is
    /// the authority — *"the journal should just be the crafting station and the herblore station where you
    /// create them"* — and decision 11 is the authority on why an unlearned page is absent rather than grey.
    ///
    /// <para><b>Straight after the satchel</b>, because it borrows that section's argument for position and
    /// adds nothing of its own: it churns the satchel (it resets it twice) and it must run after
    /// <see cref="GateCamps"/> has a synthetic bench to stand at. It navigates explicitly rather than
    /// inheriting a room, because *which* room is the whole subject of half these checks.</para>
    ///
    /// <para><b>Two fixtures, both <c>verify_</c>-prefixed</b> so <see cref="GateAuthoredContent"/>'s
    /// files-on-disk sweep keeps ignoring them — the convention that section's own comment records after
    /// reading "6 of 4".</para>
    /// </summary>
    private static void GateCraftingPanel()
    {
        Section("journal  the two sides of the book, and pressing a page");

        var panel = _hud?.Crafting;
        Check("the journal is in the tree, whether or not a scene authored it", panel != null);
        if (panel == null) return;

        var inv = InventoryCraftingManager.Instance;
        var nav = NavigationManager.Instance;
        Check("its managers are present", inv != null && nav != null);
        if (inv == null || nav == null) return;

        Check("it starts closed", !panel.IsOpen);

        // -- Two fixtures --
        // `verify_page_craft` is his and `verify_page_physick` is hers, and each declares its hand through
        // the blueprint prefix *only* — no output item of the opposite Track — so `TrackOf`'s first arm is
        // what is being read here and not the fallback behind it.
        var hisPage = new RecipeData
        {
            Id = "verify_recipe_his",
            DisplayName = "A Page Of His",
            OutputItemId = "trap_snare",
            RequiredBlueprintId = "lore_craft_verify",
        };
        hisPage.Inputs.Add(new RecipeIngredient { ItemId = "mat_scrap_iron", Amount = 1 });
        inv.DebugRegisterRecipe(hisPage);

        var herPage = new RecipeData
        {
            Id = "verify_recipe_hers",
            DisplayName = "A Page Of Hers",
            OutputItemId = "salve_clearmind",
            RequiredBlueprintId = "lore_physick_verify",
        };
        herPage.Inputs.Add(new RecipeIngredient { ItemId = "herb_bitterroot", Amount = 1 });
        inv.DebugRegisterRecipe(herPage);

        // The engine's shape, ahead of the engine — which is still unauthored and, since 2026-08-05,
        // deliberately so: it buys the good ending and nothing else needs it, so it is a side path and the
        // recipe is deferred with the pocket that teaches it. This fixture carries **both** halves of what
        // the code owes it: appearing on two sides at once, and refusing until two hands are known.
        //
        // **It outputs a salve rather than a trap, and it costs something.** Both were changed when the
        // refusal landed. A trap output makes `AlreadyMade` fire the moment one is owned, which would mask
        // the block under test three checks after the fixture is registered; and a recipe with no inputs
        // cannot show that a knowledge refusal outranks a material one.
        var bothPage = new RecipeData
        {
            Id = "verify_recipe_both",
            DisplayName = "A Page Of Both",
            OutputItemId = "salve_clearmind",
            RequiredBlueprintId = "lore_craft_verify",
            RequiresBothTracks = true,
        };
        bothPage.Inputs.Add(new RecipeIngredient { ItemId = "mat_scrap_iron", Amount = 1 });
        inv.DebugRegisterRecipe(bothPage);

        // The same shape written in her hand instead. **This exists because `Inspect` tests two arms and one
        // fixture can only ever reach one of them**: a both-hands recipe whose own page is his refuses on
        // *her* side being blank, and no amount of that proves the mirror is wired. Half the refusal could
        // have been left out and every check below would still be green. The engine as designed is his page
        // plus one of her notes, so the arm this fixture exercises is the one the real recipe never will —
        // which is exactly why it is a fixture and not an observation about content.
        var herBothPage = new RecipeData
        {
            Id = "verify_recipe_both_hers",
            DisplayName = "A Page Of Both, In Her Hand",
            OutputItemId = "salve_clearmind",
            RequiredBlueprintId = "lore_physick_verify",
            RequiresBothTracks = true,
        };
        inv.DebugRegisterRecipe(herBothPage);

        // -- Which side a page is written on --
        Check("a page with his prefix is his", inv.TrackOf(hisPage) == KnowledgeTrack.Craft);
        Check("a page with her prefix is hers", inv.TrackOf(herPage) == KnowledgeTrack.Physick);
        Check("and where the blueprint is silent the output item answers",
            inv.TrackOf(inv.GetRecipe(StationRecipeId)) == KnowledgeTrack.Physick);

        Check("his page is on his side and not hers",
            inv.BelongsToSide(hisPage, KnowledgeTrack.Craft) &&
            !inv.BelongsToSide(hisPage, KnowledgeTrack.Physick));
        Check("the engine is on both sides at once",
            inv.BelongsToSide(bothPage, KnowledgeTrack.Craft) &&
            inv.BelongsToSide(bothPage, KnowledgeTrack.Physick));

        // Every authored recipe lands somewhere. The book is the only way to reach a recipe once F7 is
        // gone, so a page on neither side is content that cannot be made at all — and it would look
        // exactly like content that has not been authored yet.
        var authoredRecipes = inv.Recipes
            .Where(pair => pair.Value != null && !pair.Key.StartsWith("verify_"))
            .ToList();
        var homeless = authoredRecipes
            .Where(pair => !inv.BelongsToSide(pair.Value, KnowledgeTrack.Craft) &&
                           !inv.BelongsToSide(pair.Value, KnowledgeTrack.Physick))
            .Select(pair => pair.Key).ToList();
        Check($"all {authoredRecipes.Count} authored recipes are written on a side of the book{Listing(homeless)}",
            authoredRecipes.Count > 0 && homeless.Count == 0);

        // **The crossing check, and the reason `TrackOf` is derived rather than authored.** Two sources can
        // answer this question — the blueprint's prefix and the output item's `Track` — and a recipe where
        // they disagree is a page that would be filed under his hand and make her thing. The prefix wins in
        // `TrackOf`, which means a disagreement is silent: the page appears on a side, just the wrong one.
        //
        // **The count is in the message on purpose.** This is a universal over the recipes where *both*
        // sources speak, and a universal can pass by examining nothing — the eleventh blind-spot class in a
        // new coat. If the pages ever stop declaring a prefix, or the items stop authoring a `Track`, this
        // check would go green by measuring an empty set and the line would still read like cover.
        var disagreeing = new List<string>();
        var crossings = 0;
        foreach (var (id, recipe) in inv.Recipes)
        {
            if (recipe == null || id.StartsWith("verify_")) continue;

            KnowledgeTrack? byPrefix =
                recipe.RequiredBlueprintId.StartsWith("lore_physick_") ? KnowledgeTrack.Physick
                : recipe.RequiredBlueprintId.StartsWith("lore_craft_") ? KnowledgeTrack.Craft
                : null;

            var byItem = inv.GetItem(recipe.OutputItemId)?.Track;
            if (byPrefix == null || byItem == null) continue;

            crossings++;
            if (byPrefix != byItem) disagreeing.Add(id);
        }
        Check($"and on the {crossings} where both sources speak, they agree about whose it is{Listing(disagreeing)}",
            crossings > 0 && disagreeing.Count == 0);

        // -- Decision 11's crossing: everything you know, you hold; everything you hold, you know --
        //
        // **This is the check the decision was ratified with, and it is here rather than in `GateRoster`
        // because the journal is what the two sides are *for*.** Two authored collections name each other's
        // ids — recipes name a `RequiredBlueprintId`, `data/items/` holds a page whose `Id` is that same
        // string — and unit 4b's rule is that a crossing like this never gets written by whoever writes
        // either sweep. The recipe sweep checks recipes; the item sweep checks items; a recipe requiring a
        // page nobody authored, or a page teaching a recipe nobody wrote, is invisible to both.
        //
        // **What makes it airtight rather than merely two-sided is that there is no third string.** The page
        // *is* the blueprint id, so this compares a set against itself viewed through two registries — there
        // is no "which page teaches this family" field able to drift, which is exactly what the decision
        // bought and exactly what it should be spending here.
        var requiredPages = inv.Recipes
            .Where(pair => pair.Value != null && !pair.Key.StartsWith("verify_") && pair.Value.NeedsBlueprint)
            .Select(pair => pair.Value.RequiredBlueprintId)
            .Distinct().OrderBy(id => id).ToList();

        var authoredPages = inv.Catalogue.Values
            .Where(i => i != null && i.Category == ItemCategory.Lore && i.Id.StartsWith("lore_"))
            .Select(i => i.Id).OrderBy(id => id).ToList();

        var unwritten = requiredPages.Where(id => !authoredPages.Contains(id)).ToList();
        Check($"every one of the {requiredPages.Count} families a recipe demands has a page to be written on"
            + $"{Listing(unwritten)}",
            requiredPages.Count > 0 && unwritten.Count == 0);

        var orphaned = authoredPages.Where(id => !requiredPages.Contains(id)).ToList();
        Check($"and each of the {authoredPages.Count} pages teaches something{Listing(orphaned)}",
            authoredPages.Count > 0 && orphaned.Count == 0);

        // **The `lore_` prefix is the rule that keeps her silent fourth note out of the crossing above, and
        // it is a ruling rather than an observation — made 2026-08-05, because the alternative was worse.**
        // Three of her four notes teach a recipe; the fourth is narrative and teaches nothing (decision 11,
        // *"3 teach and 1 silent"*). A silent note carrying a `lore_physick_` id would land in
        // `authoredPages` and read as an orphan forever — so the honest fix is either an exception list, which
        // rots and has to be maintained by hand, or a naming rule, which needs nothing maintained at all.
        //
        // So: **`lore_` means it teaches.** A note that only speaks gets any other prefix and is a `Lore`
        // item like any other, showing up on the satchel's shelf and in no crossing. The file itself is
        // Jeremy's — a page with nothing mechanical in it is pure story, and story is last (ruled
        // 2026-08-04) — and this check is what makes dropping it in a one-file job with no gate to reopen.
        var silent = inv.Catalogue.Values
            .Where(i => i != null && i.Category == ItemCategory.Lore && !i.Id.StartsWith("lore_"))
            .Select(i => i.Id).ToList();
        var mistitled = silent.Where(id => requiredPages.Contains(id)).ToList();
        Check($"and a page outside the naming rule teaches nothing, by construction "
            + $"({silent.Count} such){Listing(mistitled)}",
            mistitled.Count == 0);

        // **No census here, deliberately.** Seven of his and three of hers is already pinned to the name — by
        // `GateRoster`'s §8 bill, off the recipe side, with a drift list. The crossing above makes the two
        // sets identical, so a page count asserted here could only go red after that one already had: a check
        // that cannot fail, which is the blind-spot class this suite exists to avoid manufacturing. The
        // ten-file bill is enforced once, upstream, and this gate borrows it rather than restating it.

        // Track, on the pages themselves. The sweep above crosses *recipes* against items by output; this
        // crosses the page's own authored `Track` against the hand its id claims, which is the field the
        // journal reads to decide which side to draw it on. They are separately authored and can disagree.
        var misfiled = authoredPages
            .Where(id => inv.GetItem(id)?.Track !=
                         (id.StartsWith("lore_physick_") ? KnowledgeTrack.Physick : KnowledgeTrack.Craft))
            .ToList();
        Check($"and every page is filed under the hand its id claims{Listing(misfiled)}",
            misfiled.Count == 0);

        // -- What you have not learned is not in the book --
        inv.ResetAll();
        nav.GoToRoom("verify_camp");
        Check("standing at a bench", CampManager.Instance?.AtCamp == true);

        Check("the book opens at a bench", panel.Open(StationId));
        Check("and the caption names the station that was clicked", panel.VenueStationId == StationId);
        Check("an unlearned page is absent rather than greyed",
            inv.Inspect("verify_recipe_his") == CraftBlock.Unknown &&
            !panel.RowRecipeIds.Contains("verify_recipe_his"));

        inv.Discover("lore_craft_verify");
        panel.DebugSelectSide(KnowledgeTrack.Craft);
        Check("learning it writes it into the book",
            panel.RowRecipeIds.Contains("verify_recipe_his"));

        // -- The room the book is drawn over --
        Check("the book covers the screen, so nothing under it can be clicked", panel.BlocksTheRoom);
        Check("it tints the room behind it", panel.DimsTheRoom);
        Check("and the tint is behind the pages rather than over them", panel.DimDrawsBehindTheRows);

        // -- A page you know and cannot make --
        // The distinction this panel exists to draw: *you know how and cannot* is drawn, disabled, and
        // says which kind of no it is; *you do not know how* is not drawn at all.
        var shortRow = RowIndexOf(panel, "verify_recipe_his");
        Check("a page you cannot afford is still drawn", shortRow >= 0);
        Check("it cannot be pressed", shortRow >= 0 && !panel.RowEnabledAt(shortRow));
        Check("and it says why in the manager's own words",
            shortRow >= 0 && panel.RowLabelAt(shortRow)
                .Contains(InventoryCraftingManager.Words(CraftBlock.MissingMaterials)));
        Check("with the input named whether or not it is held",
            shortRow >= 0 && panel.RowLabelAt(shortRow).Contains(inv.DisplayNameOf("mat_scrap_iron")));
        Check("and pressing a dead page makes nothing",
            !panel.DebugPressRecipe("verify_recipe_his") && !inv.Has("trap_snare"));

        // The venue refusal, which is the other half of §19 and the one a bench hides. Away from a bench
        // the same page is still in the book — knowledge does not depend on where you stand — and dead.
        nav.GoToRoom("verify_wilds");
        panel.Open(StationId);
        var awayRow = RowIndexOf(panel, "verify_recipe_his");
        Check("off a bench the page is still written", awayRow >= 0);
        Check("and refuses on the venue, naming the cure",
            awayRow >= 0 && !panel.RowEnabledAt(awayRow) &&
            panel.RowLabelAt(awayRow)
                .Contains(InventoryCraftingManager.Words(CraftBlock.NotAtStation)));

        // -- Pressing a live page is the verb --
        nav.GoToRoom("verify_camp");
        inv.AddItem("mat_scrap_iron", 1);
        panel.Open(StationId);
        panel.DebugSelectSide(KnowledgeTrack.Craft);

        var liveRow = RowIndexOf(panel, "verify_recipe_his");
        Check("with the material in hand the page comes alive",
            liveRow >= 0 && panel.RowEnabledAt(liveRow));
        Check("pressing it is the verb", panel.DebugPressRecipe("verify_recipe_his"));
        Check("so the thing is made", inv.Has("trap_snare"));
        Check("and the material is gone", !inv.Has("mat_scrap_iron"));

        // Unlike the camp menu, a craft leaves the book open — you are usually making the second one.
        Check("the book stays open, because crafting is repeatable", panel.IsOpen);

        // A trap is permanent (§19), so the same page is now dead on the one CraftBlock that is good news
        // rather than a refusal. This is the check that would catch it being worded as a failure.
        var madeRow = RowIndexOf(panel, "verify_recipe_his");
        Check("and the page it was is now dead, reading as news rather than refusal",
            madeRow >= 0 && !panel.RowEnabledAt(madeRow) &&
            panel.RowLabelAt(madeRow)
                .Contains(InventoryCraftingManager.Words(CraftBlock.AlreadyMade)));

        // -- The divider --
        inv.Discover("lore_physick_verify");
        Check("turning to her side is a click, not a table lookup", panel.DebugPressSideTab(KnowledgeTrack.Physick));
        Check("and the tab that is drawn pressed is hers", panel.ActiveSideLabel == "Physick");
        Check("her page is on her side", panel.RowRecipeIds.Contains("verify_recipe_hers"));
        Check("and his is not", !panel.RowRecipeIds.Contains("verify_recipe_his"));
        Check("the engine is on this side too", panel.RowRecipeIds.Contains("verify_recipe_both"));

        Check("turning back is a click as well", panel.DebugPressSideTab(KnowledgeTrack.Craft));
        Check("and his tab is the pressed one now", panel.ActiveSideLabel == "Craft");
        Check("his page is back", panel.RowRecipeIds.Contains("verify_recipe_his"));
        Check("hers is not", !panel.RowRecipeIds.Contains("verify_recipe_hers"));
        Check("and the engine is on both", panel.RowRecipeIds.Contains("verify_recipe_both"));

        Check("the heading counts the pages on the side that is open",
            panel.HeadingText.Contains("His craft") &&
            panel.HeadingText.Contains(panel.RowCount.ToString()));

        // **The check above cannot catch a wrong count**, and break-testing the `Unknown` skip is what made
        // that plain: it reads `RowCount` for the number it expects, so it only ever proves the heading and
        // the panel agree with each other. That is the failure `ActiveSideLabel`'s doc-comment warns about,
        // one layer up. So the count is pinned against a population derived here instead, and the sweep
        // below covers the whole book rather than the one fixture the absence check names.
        var shouldBeDrawn = inv.Recipes.Values
            .Where(r => r != null && inv.BelongsToSide(r, KnowledgeTrack.Craft) &&
                        inv.Inspect(r.Id) != CraftBlock.Unknown)
            .ToList();
        Check($"and there are exactly the {shouldBeDrawn.Count} pages on this side that ought to be",
            panel.RowCount == shouldBeDrawn.Count && panel.RowCount > 0);
        Check("with nothing in the book he has not learned",
            panel.RowRecipeIds.All(id => inv.Inspect(id) != CraftBlock.Unknown));

        // -- Both hands: the refusal the flag went a unit without (unit 7b, 2026-08-05) --
        //
        // **`BelongsToSide` above was the whole of `RequiresBothTracks`' readership for a unit**, so the
        // engine appeared on two sides of the book and could be built by a player who knew one of them. The
        // row *read* right and *bit* nothing — the eleventh blind-spot class — and the only reason it was
        // caught is that the method's own doc-comment wrote down what it did not do.
        //
        // **The block is `HalfKnown` and it is a second knowledge refusal rather than a widening of
        // `Unknown`.** `Unknown` answers *have you found this page*, which the recipe can ask because it
        // names the id. The second requirement has no id to name — the field asserting it is a bool, because
        // the interesting state is *both* and never *either* — so there is nothing for `Unknown` to look up.
        //
        // **Rule A, ruled by Jeremy 2026-08-05: the other hand means any page on it.** Knowledge is a flat
        // set and the prefix says whose each page is, so nothing new is authored to answer this. The rejected
        // alternative was a second blueprint id on the recipe, which buys the power to demand one specific
        // note and costs a field that seventeen recipes leave blank.
        //
        // **Nothing below is a reachability claim, and that is a correction rather than an omission.** An
        // earlier draft of this unit argued that invariant 12 now forced one of her notes to be placed in the
        // world. It does not: `combat.md` §20 is explicit that the engine buys the good ending and that *"a
        // player who reaches the finale with nothing built can still finish the game. They just finish it
        // badly."* A side path may be missable. This gate proves the refusal is real, not that it is escapable.
        //
        // **Shut before it walks**, on the note in `GateInventoryPanel`: the book covers the screen, so the
        // room under it cannot be clicked and a player can never be mid-journey with it up. The last gate to
        // navigate with a panel open left a green 945-check run that died in `Array.Finalize` at teardown.
        panel.Close();
        inv.ResetAll();
        nav.GoToRoom("verify_camp");

        // -- The softlock play found, 2026-08-05 --
        //
        // Jeremy, playtesting: *"when it is full of the craft recipes it is so long you cannot see the bottom or
        // scroll on the menu so you cannot exit the screen."* The rows lived in a bare `VBoxContainer`, so the
        // column's **minimum** height grew with the book; a `PanelContainer`'s minimum is its child's, minimums
        // beat offsets, and the backing grew past its own rect carrying ✕ off the bottom. With
        // `MouseFilter = Stop` and no Escape binding anywhere, ✕ was the only way out and it was unreachable.
        //
        // **Asserted as a relationship between two measurements, because a pixel bound would be a different
        // check every time Jeremy restyles the panel.** An empty book and a full one must demand the same
        // height: growth *is* the defect, whatever the number. `GetCombinedMinimumSize` is computed from
        // children rather than assigned by a layout pass, which is why it means something headless where `Size`
        // does not — and why every other readout on this panel had to ask about anchors and offsets instead, and
        // so could not see this.
        var emptyDemand = panel.DemandedHeight;
        var emptyWidth = panel.DemandedWidth;
        Check("with an empty book the journal demands a size at all, so this is measuring something",
            emptyDemand > 0f && emptyWidth > 0f);

        foreach (var r in inv.Recipes.Values)
            if (r?.NeedsBlueprint == true) inv.Discover(r.RequiredBlueprintId);
        inv.Discover("lore_craft_verify");
        panel.Open(StationId);

        Check($"a full book draws more rows than an empty one ({panel.RowCount} rows), or the next check is vacuous",
            panel.RowCount > 1);
        Check($"and still demands exactly the same height, so ✕ cannot be pushed off the panel "
            + $"({panel.DemandedHeight} against {emptyDemand})",
            Mathf.IsEqualApprox(panel.DemandedHeight, emptyDemand));

        // **The same claim on the other axis, and it was missing for exactly as long as it took Jeremy to open
        // the book once.** A row is one line of *name · inputs · why-not* and a `Button`'s minimum width includes
        // its text, so the exorcism engine's row pushed the panel wider than the screen — the height defect's
        // twin, left live because the fix and its gate both only thought about rows *stacking*. A panel that
        // grows sideways from an anchor-derived corner also stops being centred, which is the symptom that got
        // reported.
        Check($"and the same width, so a long row cannot push it off-centre or off-screen "
            + $"({panel.DemandedWidth} against {emptyWidth})",
            Mathf.IsEqualApprox(panel.DemandedWidth, emptyWidth));

        panel.Close();
        inv.ResetAll();

        Check("the mirror fixture is on both sides as well, so the two arms are the same shape",
            inv.BelongsToSide(herBothPage, KnowledgeTrack.Craft) &&
            inv.BelongsToSide(herBothPage, KnowledgeTrack.Physick));

        Check("knowing neither hand, a two-hand page is unknown like any other",
            inv.Inspect("verify_recipe_both") == CraftBlock.Unknown &&
            inv.Inspect("verify_recipe_both_hers") == CraftBlock.Unknown);

        // -- Her hand alone, which is the arm the real engine will never reach --
        // **This probe exists because the chain below cannot contain it.** `Inspect` tests two arms joined by
        // an `or`, and the main sequence discovers his page first and never lets go of it — so the arm asking
        // *is his side blank* would go unexercised, and half the refusal could be deleted with the suite
        // still green. It runs first and resets, rather than being bolted onto the end, because "knows her
        // page and nothing of his" is a state you can only be in before you know anything of his.
        inv.Discover("lore_physick_verify");
        Check("with her hand alone, the page in her hand refuses on his blank side",
            inv.Inspect("verify_recipe_both_hers") == CraftBlock.HalfKnown);
        Check("and the page in his hand is merely unknown, which is a different sentence",
            inv.Inspect("verify_recipe_both") == CraftBlock.Unknown);

        inv.ResetAll();
        Check("forgetting it all puts both back to unknown",
            inv.Inspect("verify_recipe_both") == CraftBlock.Unknown &&
            inv.Inspect("verify_recipe_both_hers") == CraftBlock.Unknown);

        // His half, and the material in the satchel. **The material matters**: without it the next check
        // would pass on a recipe that is merely unaffordable, and `HalfKnown` sits above
        // `MissingMaterials` precisely so the panel says which no it is.
        inv.Discover("lore_craft_verify");
        inv.AddItem("mat_scrap_iron", 1);
        Check("with his hand and the materials in hand, it still refuses on her blank side",
            inv.Inspect("verify_recipe_both") == CraftBlock.HalfKnown && !inv.CanCraft("verify_recipe_both"));

        // The other arm, which no fixture written in his hand can reach: her page unfound is not the same
        // question as his page unfound, and the mirror is refused on his.
        Check("and the page in her hand is still unknown, because its own page is not his",
            inv.Inspect("verify_recipe_both_hers") == CraftBlock.Unknown);

        // The act, through the same answer. `Craft` calls `CanCraft` rather than re-testing anything, so this
        // cannot drift from the line above — but that is the property being asserted, and it is the one that
        // would have been silently false if the refusal had been written in the panel instead.
        Check("pressing it makes nothing and spends nothing",
            !inv.Craft("verify_recipe_both") &&
            inv.GetCount("mat_scrap_iron") == 1 && !inv.Has("salve_clearmind"));

        // Knowledge outranks the venue too, on the argument `NotAtStation` makes about itself: a page you
        // half-know must not start telling you where to stand, because a refusal that names the venue has
        // admitted the recipe is otherwise yours.
        nav.GoToRoom("verify_wilds");
        Check("off a bench it is still the blank side that refuses, not the venue",
            inv.Inspect("verify_recipe_both") == CraftBlock.HalfKnown);
        nav.GoToRoom("verify_camp");

        // -- What the player actually sees, which is the two halves composing --
        // The row is drawn on her side while her side is otherwise empty. That is the only place the game
        // says out loud that something of hers is missing, and it is why `Words` does not need to name the
        // side: the panel points at it by where it puts the page.
        Check("the book opens", panel.Open(StationId));
        Check("turning to her blank side is a click", panel.DebugPressSideTab(KnowledgeTrack.Physick));

        var engineRow = RowIndexOf(panel, "verify_recipe_both");
        Check("the two-hand page is written on her side even though she has taught him nothing",
            engineRow >= 0);
        Check("it cannot be pressed", engineRow >= 0 && !panel.RowEnabledAt(engineRow));
        Check("and it says which no it is, in the manager's words",
            engineRow >= 0 && panel.RowLabelAt(engineRow)
                .Contains(InventoryCraftingManager.Words(CraftBlock.HalfKnown)));
        // Keyed on `RequiresBothTracks` rather than on the fixture's id, since 2026-08-05: the authored
        // exorcism engine is a real two-hand recipe and it is drawn on her side for the same reason the
        // fixture is. Naming the fixture made this check a statement about the fixture; asking the field
        // makes it the statement that was meant — **her side holds her work, plus whatever needs both.**
        // The counts are in the name because widening an exemption is how an `All` goes vacuous: if every
        // row on her side were a two-hand row there would be nothing left for the predicate to be about.
        var herRows = panel.RowRecipeIds.ToList();
        var bothRows = herRows.Count(id => inv.GetRecipe(id)?.RequiresBothTracks == true);
        Check($"and the only things on her side not written in her hand are the two-hand rows "
            + $"({bothRows} of {herRows.Count} rows)",
            herRows.Count > bothRows &&
            herRows.All(id => inv.GetRecipe(id)?.RequiresBothTracks == true ||
                              inv.TrackOf(inv.GetRecipe(id)) == KnowledgeTrack.Physick));

        Check("and pressing the dead row from the book makes nothing either",
            !panel.DebugPressRecipe("verify_recipe_both") && !inv.Has("salve_clearmind"));

        // Her half arrives. **This is the check that makes every refusal above mean something** — without it
        // they would be satisfied by a recipe that refuses forever, which is what a `HalfKnown` returned
        // unconditionally would look like from the outside.
        inv.Discover("lore_physick_verify");
        Check("one of her pages, any of her pages, is what lifts it — rule A",
            inv.Inspect("verify_recipe_both") == CraftBlock.None && inv.CanCraft("verify_recipe_both"));
        Check("and the mirror lifts with the same page, from the other direction",
            inv.Inspect("verify_recipe_both_hers") == CraftBlock.None);

        Check("so the thing is made", inv.Craft("verify_recipe_both") && inv.Has("salve_clearmind"));
        Check("and the material is gone this time", !inv.Has("mat_scrap_iron"));

        // The page she taught him is not the page the recipe names, which is rule A stated as a check rather
        // than as a comment: `lore_physick_verify` teaches `verify_recipe_hers` and has nothing to do with
        // this recipe, and it satisfied it anyway.
        Check("the page that lifted it teaches something else entirely",
            inv.GetRecipe("verify_recipe_both").RequiredBlueprintId == "lore_craft_verify" &&
            inv.GetRecipe("verify_recipe_hers").RequiredBlueprintId == "lore_physick_verify");

        // And the near-miss the predicate was written around. Clearmind is hers, needs no blueprint, and is
        // known from the first minute — so *knows a recipe on her side* is true at game start and would have
        // half-satisfied the engine before the story began. **Pages held, not recipes known.**
        inv.ResetAll();
        Check("at the start of the game he knows one of her recipes",
            inv.Inspect(StationRecipeId) != CraftBlock.Unknown);
        Check("but not one of her pages, which is the half the engine counts",
            !inv.KnowsAnyPageOn(KnowledgeTrack.Physick) && !inv.KnowsAnyPageOn(KnowledgeTrack.Craft));

        // -- Which side it opens at --
        // The case that made the rule worth having: at the first bench of the game, the only recipe he
        // knows is hers. Opening on Craft would show a blank book to a player who does have something to
        // brew. `ResetAll` clears the discovery set, which *is* the game-start knowledge state — the one
        // recipe with no blueprint at all is Clearmind, and Clearmind is hers.
        inv.ResetAll();
        panel.Open(StationId);
        Check("with nothing makeable anywhere the book opens on his side, as a default",
            panel.ActiveSide == KnowledgeTrack.Craft);

        foreach (var input in inv.GetRecipe(StationRecipeId).Inputs)
            inv.AddItem(input.ItemId, input.Amount);

        Check("the one recipe known at the start is hers, and makeable",
            inv.Inspect(StationRecipeId) == CraftBlock.None);
        panel.Open(StationId);
        Check("so the first bench of the game opens on her side rather than a blank page",
            panel.ActiveSide == KnowledgeTrack.Physick && panel.RowCount > 0);

        // -- The bench is what opens it --
        // A hotspot emits `CraftingRequested`; nothing else in the game reaches this panel. Fired on the
        // bus rather than by calling `Open`, because the subscription in `Hud` is the part that could be
        // missing and calling `Open` would pass with the wire cut.
        panel.Close();
        EventBus.Instance?.EmitCraftingRequested(StationId);
        Check("a bench emitting on the bus is what puts the book up", panel.IsOpen);
        Check("and it knows which station asked", panel.VenueStationId == StationId);

        // Two dims do not stack, in both directions — `GateCamps`'s pair, for the same reason: the journal
        // and the satchel both tint at 55% black and both are reachable from a screen the other is on.
        _hud.ToggleSatchel();
        Check("opening the satchel shuts the book",
            !panel.IsOpen && _hud.Inventory?.IsOpen == true);
        EventBus.Instance?.EmitCraftingRequested(StationId);
        Check("and opening the book shuts the satchel",
            panel.IsOpen && _hud.Inventory?.IsOpen != true);

        Check("the camp menu shuts it too", _hud.ToggleCampMenu() && !panel.IsOpen);

        // And the other way, which is the direction that was missing: the menu is up here, so a bench click
        // arriving underneath it has to take it down. `Hud` closes it *"even though it cannot currently be
        // up"* — the menu covers the screen and a station click comes from the world beneath it — and that is
        // precisely the argument that left this menu sitting behind the fight screen once already. The line
        // is defensive, so this check is what keeps it from being deleted as dead.
        Check("the menu is up for the other direction", _hud.Camp?.IsOpen == true);
        EventBus.Instance?.EmitCraftingRequested(StationId);
        Check("and a bench arriving underneath the menu takes it down",
            panel.IsOpen && _hud.Camp?.IsOpen != true);

        inv.ResetAll();
        panel.Close();
        Check("it is left closed for whatever runs next", !panel.IsOpen);
    }

    /// <summary>Where a named recipe is drawn in the book, or -1. Keeps the checks above to one line each.</summary>
    private static int RowIndexOf(CraftingPanel panel, string recipeId)
    {
        var ids = panel.RowRecipeIds.ToList();
        return ids.IndexOf(recipeId);
    }

    // ---- Encounters ------------------------------------------------------

    /// <summary>
    /// The turn loop: what an action costs, what a blow is worth once it has been through the loop rather
    /// than beside it, and which of three flags a win writes. `docs/combat.md` §18 is the authority on
    /// every rule below.
    ///
    /// <para><b>Late on purpose, for two reasons.</b> It churns the satchel harder than any other gate —
    /// two weapons, a coat, six traps and a fistful of salves — and <see cref="GatePersistence"/>
    /// deliberately inherits the reagents the micro-loop harvested, which is the same argument that put
    /// <see cref="GateCraftingAndUse"/> down here. And it is the only gate that <i>equips</i> anything, so
    /// running it before <see cref="GateWoundLedger"/> would mean every number that gate reads from the
    /// resolver had a coat sitting behind it.</para>
    ///
    /// <para><b>Everything happens in one room, and that is not tidiness.</b>
    /// <c>NavigationManager.FindPlacement</c> searches only the current room (by design — an instance id
    /// is a placement, and placements are room-scoped), so a fight whose opponent is elsewhere resolves
    /// to nothing and every verb below silently returns false. Which is exactly why the death gate is
    /// last: it collapses the player, and a collapse walks them to a camp.</para>
    /// </summary>
    private static void GateEncounters()
    {
        Section("encounters  the turn loop, and what a fight leaves behind");

        var encounters = EncounterManager.Instance;
        var nav = NavigationManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        var combat = CombatManager.Instance;
        var states = EntityStateManager.Instance;
        var world = WorldStateManager.Instance;
        var vitality = VitalityManager.Instance;

        Check("the manager is registered", encounters != null);
        if (encounters == null || nav == null || inv == null || combat == null
            || states == null || world == null || vitality == null) return;

        const string Axe = "weapon_splitting_axe";
        const string Maul = "weapon_bell_maul";
        const string Coat = "armour_tanners_coat";
        const string Jack = "armour_padded_jack";      // deliberately never picked up
        const string Deadfall = "trap_deadfall";       // sets itself off
        const string Oil = "trap_oil_slick";           // waits for a turn
        const string Vitriol = "trap_vitriol_flask";   // waits for a turn
        const string Salve = "salve_clearmind";
        const string Reagent = "herb_bitterroot";      // a Component, so death takes it

        nav.GoToRoom(ArenaRoomId);
        Check("the arena is walkable", nav.CurrentRoomId == ArenaRoomId);

        var missing = new[] { ArenaBeast, ArenaFreed, ArenaKilled, ArenaTough, ArenaCut, ArenaHitter,
                ArenaBurner, ArenaShown, ArenaAnchored, ArenaLoosed, ArenaFaint }
            .Where(id => Placement(id)?.Entity == null).ToList();
        Check($"and every body on it resolves{Listing(missing)}", missing.Count == 0);
        if (missing.Count > 0) return;

        encounters.ResetAll();
        combat.ResetAll();
        inv.ResetAll();
        vitality.ResetAll();

        foreach (var id in new[] { ArenaBeast, ArenaFreed, ArenaKilled, ArenaTough, ArenaCut, ArenaHitter,
            ArenaBurner, ArenaShown, ArenaAnchored, ArenaLoosed, ArenaFaint })
            states.SetState(id, EntityState.Corrupted);

        inv.AddItem(Axe, 1);
        inv.AddItem(Maul, 1);
        inv.AddItem(Coat, 1);
        inv.AddItem(Salve, GameConfig.SalveSlotsPerFight + 3);

        // Every trap in the arsenal, so gate 1 can ask for the (n+1)th without naming a file. The cap
        // check needs the refused trap to be *held*: `InspectRig` tests the satchel before it tests the
        // sites, so an unheld trap would answer NotHeld and the check would pass for the wrong reason.
        var traps = inv.Catalogue.Values
            .Where(i => i != null && i.Category == ItemCategory.Trap && !string.IsNullOrEmpty(i.Id))
            .Select(i => i.Id).OrderBy(id => id).ToList();
        foreach (var id in traps) inv.AddItem(id, 1);

        // -- 0: the slate every check below stands on ----------------------
        // **F8's probe stood here and went on 2026-08-05 with the key.** It pressed
        // `Main.RigEverythingPossibleHere` and asserted a creature's sites came out full — the
        // manufactured-probe case, because on a real playthrough the key answers *"no traps in the satchel"*
        // whether it works or not. The **invariant** it was standing in for is *a verb the player can reach
        // fills a creature's sites out of the satchel*, and that did not die with the key: it moved onto
        // `%SetButton` in `GateInventoryPanel`, where the verb now is. Keeping the invariant and moving the
        // probe is the rule; deleting the section outright would have taken the invariant with it.
        //
        // What stays is the one line that was never about the key. Everything below counts sites on a slate
        // it assumes is empty, and an assumption a section shares is exactly the kind that goes unstated.
        var arenaBodies = new[] { ArenaBeast, ArenaFreed, ArenaKilled, ArenaTough, ArenaCut, ArenaHitter,
            ArenaBurner, ArenaShown, ArenaAnchored, ArenaLoosed, ArenaFaint };
        Check("nothing is set on the arena to begin with, which every check below assumes",
            arenaBodies.Sum(id => encounters.RiggedAgainst(id).Count) == 0);

        // -- 1: two sites, and the third is refused ------------------------
        Check($"the arsenal has more traps than a creature has sites ({traps.Count} traps, "
            + $"{GameConfig.RigSlotsPerFight} sites)",
            traps.Count > GameConfig.RigSlotsPerFight);

        var rigged = new List<string>();
        for (var i = 0; i < GameConfig.RigSlotsPerFight; i++)
            if (encounters.Rig(ArenaTough, traps[i])) rigged.Add(traps[i]);

        Check($"a creature's sites fill to the dial's own number ({rigged.Count} of "
            + $"{GameConfig.RigSlotsPerFight})",
            rigged.Count == GameConfig.RigSlotsPerFight
            && encounters.RiggedAgainst(ArenaTough).Count == GameConfig.RigSlotsPerFight);

        // Four lines rather than one conjunction, and the break-test is why: `&&` short-circuits, so a
        // raised cap made the inspection go red and left the *act* unrun — which meant "it did not land"
        // passed for no reason at all. Anywhere an inspection and its act are checked together, the act
        // has to be reached whether the inspection agreed or not.
        var overflow = traps[GameConfig.RigSlotsPerFight];
        Check("the trap that gets refused is one you are actually carrying", inv.Has(overflow));
        Check("the sites say they are full", encounters.InspectRig(ArenaTough, overflow) == RigBlock.SlotsFull);
        Check("and the act refuses too, not only the inspection", !encounters.Rig(ArenaTough, overflow));
        Check("and nothing was quietly swapped out for it",
            !encounters.RiggedAgainst(ArenaTough).Contains(overflow)
            && encounters.RiggedAgainst(ArenaTough).Count == GameConfig.RigSlotsPerFight);

        // "Already set" beats "no room", which is the right order: the player who re-clicks a trap they
        // already placed is told what happened rather than told the sites are full.
        Check("setting the same trap twice is refused as already set, not as no room",
            encounters.InspectRig(ArenaTough, rigged[0]) == RigBlock.AlreadyRigged);
        Check("a salve is not something you can set", encounters.InspectRig(ArenaTough, Salve) == RigBlock.NotATrap);
        Check("and nothing at all is nothing at all",
            encounters.InspectRig(ArenaTough, "verify_no_such_item") == RigBlock.NoSuchItem);

        // Put one down and ask again — an id taken from the same list, so an arsenal that grows cannot
        // leave this check naming a file nobody carries any more.
        inv.RemoveItem(overflow, inv.GetCount(overflow));
        Check("a trap you are not carrying cannot be set",
            encounters.InspectRig(ArenaTough, overflow) == RigBlock.NotHeld);
        inv.AddItem(overflow, 1);

        foreach (var id in rigged.ToList()) encounters.Unrig(ArenaTough, id);
        Check("taking them all back off leaves the sites empty",
            encounters.RiggedAgainst(ArenaTough).Count == 0);

        // -- 2: free, and only before the fight ----------------------------
        //
        // **This block used to assert the opposite of what it now asserts**, and the reversal is the record
        // of a design being read rather than remembered. It said *"changing your mind inside a fight is an
        // action"* and priced an `Unrig` at one turn plus a blow taken, which is what `combat.md` §18 rules.
        // §22's table then struck rigging from what a fight lets you reach at all, and recorded the satchel
        // refusing to open mid-fight — so §18's price was for a verb the player could no longer perform, and
        // the gate was covering a live implementation of a dead rule. `RigBlock.InFight` replaces it.
        var hitter = Placement(ArenaHitter).Entity;
        Check("the body that hits back deals something worth measuring",
            hitter.Deals != DamageType.None && hitter.Damage > 0);

        vitality.ResetAll();
        Check("rigging before you commit costs no turn and no blood",
            encounters.Rig(ArenaHitter, Oil) && encounters.Rig(ArenaHitter, Vitriol)
            && encounters.TurnNumber == 0
            && Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        Check("committing is not itself a turn", encounters.Engage(ArenaHitter));
        Check("and the turn count is still nothing", encounters.TurnNumber == 0);

        var beforeRefusal = vitality.Current;

        // The load-bearing one: `ArenaTough` has empty sites and the deadfall is in the satchel, so without
        // the new arm this call returns `None` — a plain "yes, that would work". Every other probe in this
        // group would answer with some *other* refusal if the arm were deleted, which is the covered-half-of-
        // a-disjunction shape the fourteenth blind-spot class is about.
        Check("a trap that would otherwise go on cleanly is refused while a fight is running",
            inv.Has(Deadfall) && encounters.RiggedAgainst(ArenaTough).Count == 0
            && encounters.InspectRig(ArenaTough, Deadfall) == RigBlock.InFight);
        Check("and the creature in front of you is refused the same way, not differently",
            encounters.InspectRig(ArenaHitter, Deadfall) == RigBlock.InFight);

        // Ordered first, which is the half of the ruling a wrong order would silently drop: each of these
        // items has a *different* true answer waiting behind the fight, and the fight's answer wins.
        Check("the rule outranks the satchel",
            !inv.Has("verify_no_such_item")
            && encounters.InspectRig(ArenaHitter, "verify_no_such_item") == RigBlock.InFight);
        Check("it outranks what the thing is",
            encounters.InspectRig(ArenaHitter, Salve) == RigBlock.InFight);
        Check("and it outranks the sites being full",
            encounters.RiggedAgainst(ArenaHitter).Count == GameConfig.RigSlotsPerFight
            && encounters.InspectRig(ArenaHitter, Deadfall) == RigBlock.InFight);

        Check("the act refuses with the inspection", !encounters.Rig(ArenaTough, Deadfall));
        Check("and taking one back off is refused too, or the deleted verb arrives from the other side",
            !encounters.Unrig(ArenaHitter, Vitriol)
            && encounters.RiggedAgainst(ArenaHitter).Contains(Vitriol));
        Check("none of which cost a turn or a drop of blood — a refusal is not an action",
            encounters.TurnNumber == 0 && Mathf.IsEqualApprox(vitality.Current, beforeRefusal));

        Check("and you can leave", encounters.Flee());
        Check("out of the fight the same two calls work again",
            encounters.Unrig(ArenaHitter, Vitriol) && encounters.Unrig(ArenaHitter, Oil));

        // -- 3: the traps that set themselves off, and the ones that wait ---
        var tough = Placement(ArenaTough).Entity;
        var deadfall = inv.GetItem(Deadfall);
        var oil = inv.GetItem(Oil);

        Check("the two traps disagree about who sets them off",
            deadfall != null && oil != null && deadfall.TriggersItself && !oil.TriggersItself);

        combat.ResetAll();
        Check("both go on the sites", encounters.Rig(ArenaTough, Deadfall) && encounters.Rig(ArenaTough, Oil));

        var volley = CombatManager.DamageAgainst(deadfall.Deals, tough, deadfall.Potency);
        var waiting = CombatManager.DamageAgainst(oil.Deals, tough, oil.Potency);

        Check("committing fires the trap that fires itself", encounters.Engage(ArenaTough));
        Check("and it landed before turn 1, which is what makes placement free",
            combat.WoundsOn(ArenaTough) == volley && volley > 0 && encounters.TurnNumber == 0);
        // The clause about `waiting` is what stops the equality above being unfalsifiable: if the oil
        // were worth nothing against this body, "the total is the deadfall's alone" would hold whether
        // the oil fired or not.
        Check("and the one that waits for you has not fired, though it would have been felt",
            waiting > 0 && combat.WoundsOn(ArenaTough) == volley);

        Check("setting it off yourself lands it", encounters.Trigger(Oil));
        Check("and costs the turn the self-triggering one did not", encounters.TurnNumber == 1);
        Check("and the ledger holds both", combat.WoundsOn(ArenaTough) == volley + waiting);

        Check("a trap that has already done its work refuses a second firing", !encounters.Trigger(Deadfall));
        Check("and refusing spent nothing", encounters.TurnNumber == 1);

        Check("out again", encounters.Flee());
        encounters.Unrig(ArenaTough, Deadfall);
        encounters.Unrig(ArenaTough, Oil);

        // -- 4: fourfold, driven through Strike ----------------------------
        var axe = inv.GetItem(Axe);
        var maul = inv.GetItem(Maul);

        Check("the two weapons swing with the same weight, or the comparison means nothing",
            axe != null && maul != null && axe.Potency == maul.Potency && axe.Potency > 0);
        Check("and differ only in what they deal",
            axe.Deals == DamageType.Edge && maul.Deals == DamageType.Blunt);
        Check("and the body fears one of those and shrugs off the other",
            tough.VulnerableTo == axe.Deals && tough.ResistantTo == maul.Deals);

        combat.ResetAll();
        Check("the axe goes in hand", inv.Equip(Axe) && inv.EquippedWeapon?.Id == Axe);
        Check("and one swing lands", encounters.Engage(ArenaTough) && encounters.Strike());
        var edgeHit = combat.WoundsOn(ArenaTough);   // read before Flee: settling an anchored body closes it
        Check("out", encounters.Flee());

        combat.ResetAll();
        Check("the maul goes in hand", inv.Equip(Maul) && inv.EquippedWeapon?.Id == Maul);
        Check("and one swing lands", encounters.Engage(ArenaTough) && encounters.Strike());
        var bluntHit = combat.WoundsOn(ArenaTough);
        Check("out", encounters.Flee());

        Check($"the type it fears hurts more through the loop, not only beside it ({edgeHit} vs {bluntHit})",
            edgeHit > bluntHit && bluntHit > 0);
        Check("and by exactly the two dials' own ratio, rather than a number written here",
            edgeHit == Mathf.RoundToInt(
                bluntHit * (GameConfig.VulnerableMultiplier / GameConfig.ResistantMultiplier)));
        Check("and the loop's arithmetic is the resolver's, so a fight cannot disagree with the ledger",
            edgeHit == CombatManager.DamageAgainst(axe.Deals, tough, axe.Potency)
            && bluntHit == CombatManager.DamageAgainst(maul.Deals, tough, maul.Potency));

        // -- 5: a coat turns aside one type and no other -------------------
        var coat = inv.GetItem(Coat);
        var burner = Placement(ArenaBurner).Entity;

        Check("the coat turns aside what one of the two hitters deals",
            coat != null && coat.WardsAgainst == hitter.Deals);
        Check("and not what the other deals, which is the case that makes the check mean anything",
            coat.WardsAgainst != burner.Deals);
        Check("and the two hit for the same, so what follows is about type and nothing else",
            hitter.Damage == burner.Damage && hitter.Damage > 0);

        inv.Unequip(EquipSlot.Armour);
        vitality.ResetAll();
        Check("unprotected, you take its blow", encounters.Engage(ArenaHitter) && encounters.Strike());
        var bare = vitality.Max - vitality.Current;
        Check("out", encounters.Flee());

        vitality.ResetAll();
        Check("the coat goes on", inv.Equip(Coat) && inv.EquippedArmour?.Id == Coat);
        Check("and you take the same blow again", encounters.Engage(ArenaHitter) && encounters.Strike());
        var warded = vitality.Max - vitality.Current;
        Check("out", encounters.Flee());

        Check($"the coat halves the blow it was made for ({warded} of {bare})",
            bare > 0 && Mathf.IsEqualApprox(warded,
                Mathf.Max(1, Mathf.RoundToInt(bare * GameConfig.WardMultiplier))));
        // Halved, never negated (§18): an immunity reads as a lock and sends the player hunting a key.
        Check("and halving is not negating — a warded blow is still a blow", warded > 0);

        vitality.ResetAll();
        Check("wearing it against a type it does not ward",
            encounters.Engage(ArenaBurner) && encounters.Strike());
        var wrongType = vitality.Max - vitality.Current;
        Check("out", encounters.Flee());
        Check($"buys nothing at all ({wrongType} of {bare})", Mathf.IsEqualApprox(wrongType, bare));

        // Printed, not asserted. "No authored coat wards Fire" is true today and a legitimate thing for
        // Jeremy to change tomorrow; a gate that went red the day a fire coat was authored is a gate
        // that teaches people to stop running the suite. The fixture above is what carries the claim.
        GD.Print("       armour authored: " + string.Join(", ", inv.Catalogue.Values
            .Where(i => i != null && i.Slot == EquipSlot.Armour)
            .Select(i => $"{i.Id} wards {i.WardsAgainst}").OrderBy(s => s)));

        inv.Unequip(EquipSlot.Armour);

        // -- 6: flight is guaranteed and costs one parting strike ----------
        vitality.ResetAll();
        Check("flight from a fresh commitment succeeds",
            encounters.Engage(ArenaHitter) && encounters.Flee());
        Check("and cost exactly one parting blow",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - hitter.Damage));
        Check("and left no fight behind it", !encounters.InFight);

        vitality.ResetAll();
        Check("flight from the middle of one succeeds too",
            encounters.Engage(ArenaHitter) && encounters.Strike() && encounters.Flee());
        Check("and cost the turn's blow plus the parting one, and no roll came into it",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - hitter.Damage * 2));

        Check("with nothing to break off from, breaking off is refused", !encounters.Flee());

        // -- 6b: a fight does not outlive the screen it started on ----------
        // Found by `--shots` and by nothing else (2026-08-03). Every check above stands still, so a fight
        // that survived a door click was invisible to all of them: the panel stayed open for the rest of
        // the run, two chapters away, titled after a creature nobody was next to. Two halves, because
        // either one alone is wrong — the door is a way of breaking off and pays a parting blow like one,
        // and *every other* way of changing rooms ends the fight for nothing, so no path can leave one
        // behind. The second half is the invariant; the first is only the price.
        vitality.ResetAll();
        Check("committed, and then the door is clicked", encounters.Engage(ArenaHitter));
        Check("walking out works while a fight is live",
            InteractionService.ActivateHotspot(Hotspot(ArenaDoor)) && nav.CurrentRoomId == "verify_wilds");
        Check("and the fight did not come along", !encounters.InFight);
        Check("and the door charged the parting blow, because leaving IS breaking off",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max - hitter.Damage));

        nav.GoToRoom(ArenaRoomId);
        vitality.ResetAll();
        Check("committed again, then moved by something that is not a door at all",
            encounters.Engage(ArenaHitter));
        nav.GoToRoom("verify_wilds");
        Check("the fight is gone that way too — the backstop, which every path goes through",
            !encounters.InFight);
        Check("and that one was free, because a collapse navigates and must not be billed twice",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        nav.GoToRoom(ArenaRoomId);

        // -- 7: the two win routes, and the two flags ----------------------
        //
        // **This block asserted three routes until 2026-08-06, and the third was unreachable in play from the
        // day it shipped.** `Killed` was written when a person fell still anchored — and since
        // `EntityStateManager.Sever` has exactly one caller (a boss pocket cutting its own target, and no
        // pocket targets an ordinary creature), *every* real fight took that branch. The gates could not see
        // it because this fixture severs `ArenaFreed` **by calling `states.Sever` directly**, which is a route
        // no player has: the suite proved a branch the game cannot reach and stayed silent about the one it
        // always takes. §3's nineteenth class with a fixture standing in for the missing route.
        //
        // Jeremy's ruling, found by playing the Apothecary: *"All non-bosses live — they have minor demons
        // possessing them. They have no dream worlds to conquer. You more or less bonk them on the head and
        // wake them up."* So a person is freed however the fight ends, `killed.*` is deleted, and severing is
        // what weakens a boss rather than what saves a villager.
        foreach (var id in new[] { ArenaBeast, ArenaFreed, ArenaKilled })
        {
            world.SetFlag(Flags.Freed(id), false);
            world.SetFlag(Flags.Defeated(id), false);
        }

        combat.ResetAll();
        vitality.ResetAll();

        Check("a thing that was never a person is fought",
            encounters.Engage(ArenaBeast) && encounters.Strike() && !encounters.InFight);
        Check("and writing it down says defeated, and not freed",
            world.GetFlag(Flags.Defeated(ArenaBeast)) && !world.GetFlag(Flags.Freed(ArenaBeast)));
        Check("and the body is purified", states.GetState(ArenaBeast) == EntityState.Purified);

        states.Sever(ArenaFreed);
        Check("a person whose tether was cut first", states.GetState(ArenaFreed) == EntityState.Severed);
        Check("goes down the same way",
            encounters.Engage(ArenaFreed) && encounters.Strike() && !encounters.InFight);
        Check("and is freed rather than merely defeated",
            world.GetFlag(Flags.Freed(ArenaFreed)) && !world.GetFlag(Flags.Defeated(ArenaFreed)));

        // **The arm that matters, and the one the old block had no fixture for: a person nobody severed.**
        // This is what all 30 authored fights actually are, so it is asserted through the route a player
        // walks — engage and strike, with the body still `Corrupted` when the blow lands. Break-testing the
        // ruling means putting `wasSevered` back in `Finish`, which reddens exactly this.
        Check("a person still anchored when the last blow lands",
            states.GetState(ArenaKilled) == EntityState.Corrupted);
        Check("goes down too, because a fight is never refused for want of a sever",
            encounters.Engage(ArenaKilled) && encounters.Strike() && !encounters.InFight);
        Check("and is freed just the same — the demon in an ordinary body is minor, and it lets go",
            world.GetFlag(Flags.Freed(ArenaKilled)) && !world.GetFlag(Flags.Defeated(ArenaKilled)));
        // The enum has three members and Purified is the terminal one, so the screen cleanses either way.
        Check("and purified all the same, whichever route you took",
            states.GetState(ArenaKilled) == EntityState.Purified);
        Check("and a finished body is no longer hostile",
            encounters.InspectEngage(ArenaKilled) == EngageBlock.NotHostile);
        Check("so it cannot be fought again — the act, not only the inspection",
            !encounters.Engage(ArenaKilled) && !encounters.InFight);

        // -- 8: the bill the flag domains promise --------------------------
        // Scoped to the chapters, on GateFights' precedent: it excludes every verify_ fixture, so the
        // two numbers are the authored roster and nothing else. Unit 3b narrowed the *reason* without
        // touching the predicate — the scoping used to exclude the waystation prototype as well, and the
        // fixtures alone still require it: `verify_arena` carries eight placements and `verify_loop` one.
        // ~~**What will move this:** unit 6's three bosses. They have no PurifiedName, so they land in the
        // second number and it becomes 9~~ — **two of the three landed 2026-08-05 and it is 8, not 9.**
        // Iris stays unplaced because `items.md` leaves her pair deliberately unfixed until the finale is
        // authored, so the prediction was right about the mechanism and wrong about the arithmetic: it
        // assumed the three arrive together. **The general shape is worth more than the number** — a
        // forecast written into a check's comment is a claim about *content that does not exist*, and the
        // half of it that rots is the count rather than the reasoning. Iris takes it to 9.
        //
        // The second number now means **"frees nobody"** rather than **"was never a person"**, which is
        // Jeremy's ruling of 2026-08-05: the Blacksmith *is* a person, his spirit is broken in the Dream
        // and the body dies with the demon that was holding it up, so no purified form ever stands there
        // and `freed.*` would be a lie about the ending's bill. The domain's definition moved with him —
        // see `Flags.Defeated`, which also records why the harvest argument did **not** move with it.
        var people = 0;
        var beasts = 0;
        foreach (var (_, room) in nav.Rooms)
        {
            if (room?.ChapterId?.StartsWith("ch") != true) continue;

            foreach (var p in room.Entities ?? new Godot.Collections.Array<EntityPlacement>())
            {
                if (p?.Entity == null) continue;
                if (string.IsNullOrEmpty(p.Entity.PurifiedName)) beasts++;
                else people++;
            }
        }

        Check($"the placed roster bills 24 people, so freed.* can only ever total 24 ({people})",
            people == 24);
        // **8 → 9 on 2026-08-05, when Iris was placed and the roster closed.** `items.md` §10 predicted this
        // number the whole time — *"the roster bill sits at `defeated.* == 8` of an eventual 9"* — so this is the
        // last increment rather than a moving target: six Beasts and three bosses, and there is nothing else in
        // the game that frees nobody.
        //
        // **Iris counts here despite surviving the good ending, and that is the subtle part.** She lives only if
        // the exorcism engine releases the tether (`combat.md` §20), and that is a *repair* — not an outcome of
        // her fight. The fight frees nobody on any of the three routes, so `PurifiedName` is blank and her win
        // writes `defeated.ch3.iris`. Reading her survival as `freed.*` would have put her in the 24 and made
        // the good ending double-count the person it saves.
        Check($"and 9 that free nobody, so defeated.* can only ever total 9 ({beasts})", beasts == 9);

        // -- 9: what the end of a fight does to the ledger -----------------
        Check("the pair are one authored body standing in two states",
            Placement(ArenaTough)?.Entity?.Id == Placement(ArenaCut)?.Entity?.Id);

        combat.ResetAll();
        states.SetState(ArenaTough, EntityState.Corrupted);
        states.SetState(ArenaCut, EntityState.Severed);

        Check("the anchored one takes a wound", encounters.Engage(ArenaTough) && encounters.Strike());
        var anchoredHit = combat.WoundsOn(ArenaTough);
        Check("and it is on the ledger while the fight is on", anchoredHit > 0);
        Check("out", encounters.Flee());
        Check("and it closes the moment the fight ends, so an anchored body cannot be worn down in shifts",
            combat.WoundsOn(ArenaTough) == 0);

        Check("the severed one takes the same wound", encounters.Engage(ArenaCut) && encounters.Strike());
        Check("exactly the same, since it is the same body and the same weapon",
            combat.WoundsOn(ArenaCut) == anchoredHit);
        Check("out", encounters.Flee());
        Check("and keeps it, so cleanup can be spread across visits",
            combat.WoundsOn(ArenaCut) == anchoredHit);

        // -- 10: two salves to a fight, and nothing arbitrates -------------
        combat.ResetAll();
        vitality.ResetAll();
        vitality.Damage(80f);

        Check("both sites are loaded before the fight",
            encounters.Rig(ArenaHitter, Oil) && encounters.Rig(ArenaHitter, Vitriol)
            && encounters.RiggedAgainst(ArenaHitter).Count == GameConfig.RigSlotsPerFight);
        Check("and the satchel has more salves than the fight will allow",
            inv.GetCount(Salve) > GameConfig.SalveSlotsPerFight);

        var heldBefore = inv.GetCount(Salve);
        Check("committed, wounded", encounters.Engage(ArenaHitter));

        // The bound is a hang guard, not a rule: `Treat` refusing forever is the pass condition, and a
        // `while` over a verb that could always succeed is the shape verify-slice-blind-spots calls the
        // worst failure class.
        var used = 0;
        while (used <= GameConfig.SalveSlotsPerFight + 2 && encounters.Treat(Salve)) used++;

        Check($"treating inside a fight stops at the dial's own number ({used} of "
            + $"{GameConfig.SalveSlotsPerFight})",
            used == GameConfig.SalveSlotsPerFight && encounters.SalvesUsed == used);
        Check("each one cost a turn and nothing more", encounters.TurnNumber == used);
        Check("and the refusal spent no salve", inv.GetCount(Salve) == heldBefore - used);

        // §19: "nothing arbitrates." The two caps are separate allowances, so hitting one must not have
        // quietly cost the other — which is the failure a single shared pool would produce and which
        // would look, from the outside, exactly like a fight that had gone on a bit long.
        Check("the trap sites were never in that trade",
            encounters.RiggedAgainst(ArenaHitter).Count == GameConfig.RigSlotsPerFight);
        Check("and a trap still goes off with the salve allowance spent out", encounters.Trigger(Oil));

        Check("out", encounters.Flee());
        encounters.Unrig(ArenaHitter, Oil);
        encounters.Unrig(ArenaHitter, Vitriol);

        // -- 11: one thing in each slot ------------------------------------
        Check("the axe goes in the weapon slot", inv.Equip(Axe) && inv.EquippedWeapon?.Id == Axe);
        Check("the maul replaces it rather than joining it",
            inv.Equip(Maul) && inv.EquippedWeapon?.Id == Maul);
        Check("and the axe is still in the satchel, because wearing is not spending", inv.Has(Axe));

        Check("a coat fills the other slot and leaves the weapon where it is",
            inv.Equip(Coat) && inv.EquippedArmour?.Id == Coat && inv.EquippedWeapon?.Id == Maul);
        // Nothing chooses a slot at the call site: `Equip` reads `ItemData.Slot`, so armour cannot land
        // in the weapon slot even by mistake. Asserted as the absence rather than as a refusal.
        Check("and nothing put the coat where the weapon goes", inv.EquippedIn(EquipSlot.Weapon) == Maul);

        // Split for the reason gate 1's overflow is split: the act must be reached even when the
        // inspection has already gone red, or the act is never actually tested.
        Check("wearing what you already wear is refused",
            inv.InspectEquip(Coat) == EquipBlock.AlreadyWorn);
        Check("and the act says so too", !inv.Equip(Coat));

        Check("the coat used for the not-held case was never picked up", !inv.Has(Jack));
        Check("so it cannot be worn", inv.InspectEquip(Jack) == EquipBlock.NotHeld);
        Check("and the act refuses that too", !inv.Equip(Jack));
        Check("a salve is not equipment", inv.InspectEquip(Salve) == EquipBlock.NotEquipment);
        Check("and nothing at all is nothing at all",
            inv.InspectEquip("verify_no_such_item") == EquipBlock.NoSuchItem);

        Check("a slot empties once", inv.Unequip(EquipSlot.Weapon));
        Check("and not twice", !inv.Unequip(EquipSlot.Weapon));
        Check("and there is no slot called none", !inv.Unequip(EquipSlot.None));
        Check("bare hands are a legal way to stand", inv.EquippedWeapon == null);

        // -- 13: the screen, driven with no mouse and no eyes --------------
        // Before the death gate rather than after it, because that one walks the player to a camp and
        // the panel needs a body in the room to be about.
        var fight = _hud?.Fight;
        Check("the fight panel reached the Hud", fight != null);

        if (fight != null)
        {
            combat.ResetAll();
            vitality.ResetAll();
            Check("it starts down", !fight.IsOpen);

            Check("the maul is in hand for this", inv.Equip(Maul));

            // **The journal is deliberately left up across the commit**, because unit 7a added the line that
            // takes it down and nothing watched it. This is the camp-menu bug of 2026-08-04 with a different
            // panel: that one was declined on *"no path can produce this state"* and then produced it, so the
            // new panel gets the check the old one had to be given twice.
            EventBus.Instance?.EmitCraftingRequested("verify_bench_before_a_fight");
            Check("a journal is open when the fight commits", _hud?.Crafting?.IsOpen == true);

            Check("committing puts it up", encounters.Engage(ArenaShown) && fight.IsOpen);
            Check("and takes the journal down with it, rather than leaving a book behind the fight screen",
                _hud?.Crafting?.IsOpen != true);
            Check("and it is about the body you committed to", fight.OpponentId == ArenaShown);
            Check("which it names, in its condition",
                fight.TitleText.Length > 0 && fight.ConditionText.Length > 0);

            // §7's legibility promise, and §18's requirement that the confirm line carry *both* halves —
            // the second is what makes the coat a decision instead of a coin flip. The fixture's two
            // types were chosen to have distinct, searchable words for exactly this check.
            var reading = fight.ReadingText;
            Check($"and the reading names what it flinches from — \"fire\"",
                reading.Contains("fire", StringComparison.OrdinalIgnoreCase));
            Check("and what it comes at you with, which is the half that decides the coat",
                reading.Contains("caustic", StringComparison.OrdinalIgnoreCase));
            Check("and whether anything still has hold of it",
                reading.Contains("hold of it", StringComparison.OrdinalIgnoreCase));

            Check("there is a strike on the strip, named for what is in hand",
                fight.VerbCount >= 1
                && Enumerable.Range(0, fight.VerbCount)
                    .Any(i => fight.VerbLabelAt(i).Contains(inv.DisplayNameOf(Maul))));

            // The reading above says what the creature barely notices; until 2026-08-03 the verb never said
            // what you were about to hit it with, so both halves of the decision were on screen in two
            // vocabularies that never met. Both checks read `EncounterManager.Words` rather than the word,
            // so rewording *"weight"* moves the label and the gate together — what cannot pass is a strike
            // that names no type at all.
            var maulWords = EncounterManager.Words(inv.GetItem(Maul)?.Deals ?? DamageType.None);
            Check($"and the strike says what it will deliver — \"{maulWords}\"",
                Enumerable.Range(0, fight.VerbCount)
                    .Any(i => fight.VerbLabelAt(i).Contains(maulWords)));

            // The arm that matters, because it is the one the boot hands you: with nothing equipped the
            // label must still name a type. Repainted through the synchronous door — the strip's own
            // repaint is deferred and this harness never yields a frame, so `DebugRepaintNow` is what
            // makes the label readable here, and it is also why this pair covers `RebuildVerbs` and not
            // the subscription. That gets its own check below.
            //
            // It asks for *"bare hands"* as well as the word, and that is not belt-and-braces: the fixture's
            // maul is Blunt too, so `Contains("weight")` alone passes on a strip that never repainted and
            // still says `Bell Maul`. A check that cannot fail on the bug it is named for is the first
            // blind-spot class in `verify-slice-blind-spots`, and it took a green run to notice.
            var fistWords = EncounterManager.Words(GameConfig.BareHandedType);
            var signalsBefore = fight.SatchelSignalsSeen;
            Check("bare-handed, it names the type anyway", inv.Unequip(EquipSlot.Weapon));
            fight.DebugRepaintNow();
            Check($"which is the half a player has at the cottage — \"bare hands ({fistWords})\"",
                Enumerable.Range(0, fight.VerbCount)
                    .Any(i => fight.VerbLabelAt(i).Contains("bare hands")
                        && fight.VerbLabelAt(i).Contains(fistWords)));
            Check("and the maul goes back on for the rest of this", inv.Equip(Maul));
            fight.DebugRepaintNow();

            // That the strip *heard* the satchel, which is the half of this the harness can have: taking a
            // weapon off emits `InventoryChanged` and nothing else, and the panel ignored that signal until
            // 2026-08-03 — so a strike could go on advertising a weapon no longer in hand. The repaint it
            // triggers is deferred and this suite never yields a frame, so the check is over the handler
            // running rather than over the pixels, and `DebugRepaintNow` above is what stands in for the
            // frame. Deleting the `+=` leaves this count still.
            //
            // Two tidier spellings were tried first and both went red against correct code: a C#
            // `signal += handler` marshals into a *custom* callable, so `GetSignalConnectionList` yields an
            // entry whose `Target` is null and whose `Delegate` does not survive the round trip, and
            // `IsConnected` with a freshly-made callable to the same handler does not match it either.
            // Worth remembering before reaching for either again — a signal connected from C# is not
            // introspectable from C#.
            Check("and the strip heard the satchel change, not only the log",
                fight.SatchelSignalsSeen > signalsBefore);

            Check("and there is a way out on it",
                Enumerable.Range(0, fight.VerbCount)
                    .Any(i => fight.VerbLabelAt(i).Contains("Break off")));

            // The first playtest could see the room straight through this panel: it is the only one of the
            // three never handed `PanelTheme.tres`, so its PanelContainer fell back to Godot's default.
            // *"the battle should be center in the screen and put a tint black overlay over everything."*
            // The behaviour is gated; the centring is not — that is a layout number on a node the contract
            // never named, and Jeremy sets it in the editor. See `verify-slice-blind-spots`.
            Check("the panel draws on something opaque", fight.DrawsOnSomethingOpaque);
            Check("and the room behind it is tinted out", fight.DimsTheRoom);
            // The failure that looks exactly like the feature: a scrim in front of the strip greys out the
            // very buttons the tint was supposed to make legible, and passes every other check here.
            Check("with the tint behind the verbs and not over them", fight.DimDrawsBehindTheVerbs);

            Check("pressing the strike takes a turn",
                fight.DebugPressVerbNamed("Strike") && encounters.TurnNumber == 1);
            Check("and the transcript said something about it", fight.LogText.Length > 0);

            var turns = fight.DebugStrikeUntilDone();
            Check($"and striking until it stops does stop ({turns} turns)",
                turns > 0 && !encounters.InFight);
            Check("and the screen came down with the fight", !fight.IsOpen);
        }

        // -- 14: the aura, which is the anchor made mechanical ---------------
        //
        // `combat.md` §2 and §6, built 2026-08-05 and the first reader `EntityData.CorruptionAura` has ever
        // had. Two effects, one field, one condition: an anchored body **closes what you opened on its own
        // turn** and **spends your nerve on yours**, and cutting the tether stops both. Jeremy's framing of
        // the split — *"regen hp on enemy's turn and resolve drain on the players turn makes sense in my
        // head. Double whammy"*.
        //
        // **Ahead of block 12 and not after it**, for block 12's own stated reason: running out of nerve walks
        // the player to a camp, so this block navigates back to the arena before it hands over.
        var resolve = ResolveManager.Instance;
        var anchoredBody = Placement(ArenaAnchored)?.Entity;
        var plainBody = Placement(ArenaTough)?.Entity;
        var faintBody = Placement(ArenaFaint)?.Entity;

        // **Guarded rather than dereferenced, unlike everywhere else in this section**, and the break-test is
        // what taught it. `Placement` only ever searches the room the player is standing in, and this is the
        // one block in the file that can leave that room by itself — running the bar to nothing walks you to a
        // camp. Restoring `CorruptionAura`'s old `0.15f` default to see this section fail gave every fixture an
        // aura, banished the player out of the arena three blocks earlier, and the three lines below then threw
        // a `NullReferenceException` that took **every later gate in the suite** with it: 532 checks ran instead
        // of 1023, and the one line written to catch that exact default never got to speak. A broken build
        // should redden the check named for it, not silence half the suite.
        var standing = resolve != null && anchoredBody != null && plainBody != null && faintBody != null;
        Check("the nerve is tracked, and the bodies this needs are still in the room", standing);
        if (standing)
        {

            // **The trap this whole mechanism nearly shipped with.** While no code read the field, its `0.15f`
            // default was invisible; the moment something did, every authored creature in the game — eleven of
            // them, rats included — was a boss that drained your nerve, by never having mentioned the field. A
            // default that only becomes wrong once someone believes it is the fixture-by-default shape in
            // `verify-slice-blind-spots`, and this is the line that keeps it at zero.
            Check("a body nobody authored has no aura, so having one is typed rather than inherited",
                Mathf.IsZeroApprox(new EntityData().CorruptionAura));

            Check("the fixture boss projects one and the plain body does not, or nothing below separates them",
                anchoredBody.CorruptionAura > 0f && Mathf.IsZeroApprox(plainBody.CorruptionAura));

            // **The first of the two checks unit 6b deleted, back and no longer vacuous.** It read *"no drain
            // outside a run"*, and with the heist gone nothing in the game could drain Resolve at all, so it
            // was a universal over an empty set. There is a mechanism now.
            combat.ResetAll();
            resolve.SetTo(resolve.Max);
            Check("nothing drains the nerve just for standing in the room — the aura is never ambient",
                Mathf.IsZeroApprox(resolve.CurrentRatePerSecond())
                && Mathf.IsEqualApprox(resolve.Current, resolve.Max));

            Check("the axe is what these turns are measured in", inv.Equip(Axe) && axe != null);

            // -- no aura: a plain fight is exactly what it was ---------------
            states.SetState(ArenaTough, EntityState.Corrupted);
            var plainDealt = CombatManager.DamageAgainst(axe.Deals, plainBody, axe.Potency);

            Check("a plain body is committed to", encounters.Engage(ArenaTough));
            Check("and a turn against it lands", encounters.Strike() && plainDealt > 0);
            Check("its turn took nothing off your nerve, because it has no aura to press with",
                Mathf.IsEqualApprox(resolve.Current, resolve.Max));
            Check("and closed nothing either — the whole wound is still standing",
                combat.WoundsOn(ArenaTough) == plainDealt);
            Check("out", encounters.Flee());

            // -- an aura, with the tether intact ----------------------------
            combat.ResetAll();
            resolve.SetTo(resolve.Max);
            states.SetState(ArenaAnchored, EntityState.Corrupted);

            var bill = ArenaAura * resolve.Max;
            var closes = Mathf.Max(1,
                Mathf.RoundToInt(anchoredBody.Durability * GameConfig.AnchoredRegenFraction));
            var auraDealt = CombatManager.DamageAgainst(axe.Deals, anchoredBody, axe.Potency);

            // `AnchoredRegenFraction`'s one hard constraint, checked rather than trusted: a heal that ties with
            // or beats the weapon in your hand makes the weapon useless instead of slow, which is the
            // immunity-reads-as-a-lock failure §18 deleted the Defence scalar to avoid.
            Check($"what the axe opens outpaces what the anchor closes ({auraDealt} against {closes})",
                auraDealt > closes && closes > 0);

            Check("committed to something anchored", encounters.Engage(ArenaAnchored));
            Check("and committing spent nothing — the aura bills turns, not the engagement",
                Mathf.IsEqualApprox(resolve.Current, resolve.Max) && encounters.TurnNumber == 0);

            // **The second deleted check, back and rewritten rather than restored.** It read *"the bar holds
            // still in the Waking world"*, which the aura makes false — so what survives is the true half, and
            // it is the sharper claim: the bar moves here, but nothing about it is a clock. §6: *"the drain
            // must tick on turns or the player is punished for thinking."*
            Check("no clock is running inside the fight either — a turn is the unit, not a second",
                Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));

            Check("one turn against it", encounters.Strike());
            Check($"cost a quarter of the bar ({resolve.Current:F0} of {resolve.Max:F0})",
                Mathf.IsEqualApprox(resolve.Current, resolve.Max - bill));
            Check("and it closed part of what you opened on its own turn",
                combat.WoundsOn(ArenaAnchored) == auraDealt - closes);

            Check("a second turn bills again, so it is a rate and not an opening toll",
                encounters.Strike()
                && Mathf.IsEqualApprox(resolve.Current, resolve.Max - bill * 2f));
            Check("and the body closed again, so the fight is a race you are still winning",
                combat.WoundsOn(ArenaAnchored) == (auraDealt - closes) * 2);

            Check("out", encounters.Flee());

            // -- the same body, tether cut ----------------------------------
            // §2: *"Severing is the only thing that does this"* — and it takes **both** halves, which is the
            // half of the rule a partial implementation would pass. One `EntityData`, two placements, so the
            // only difference between this fight and the one above is a state.
            combat.ResetAll();
            resolve.SetTo(resolve.Max);
            states.SetState(ArenaLoosed, EntityState.Severed);

            Check("the loosed one is the very same body, so only its tether differs",
                ReferenceEquals(Placement(ArenaLoosed).Entity, anchoredBody));
            Check("committed to it", encounters.Engage(ArenaLoosed));
            Check("a turn against it", encounters.Strike());
            Check("costs no nerve at all, because a cut tether has nothing to press with",
                Mathf.IsEqualApprox(resolve.Current, resolve.Max));
            Check("and nothing closes — severing took the regeneration with the drain, not one of the two",
                combat.WoundsOn(ArenaLoosed) == auraDealt);
            Check("out", encounters.Flee());
            Check("and the wound stayed open, which is what severing was for",
                combat.WoundsOn(ArenaLoosed) == auraDealt);

            // -- a body too slight for the fraction -------------------------
            combat.ResetAll();
            resolve.SetTo(resolve.Max);
            vitality.SetTo(vitality.Max - 20f);
            inv.AddItem(Salve, 1);
            states.SetState(ArenaFaint, EntityState.Corrupted);

            Check("a body slight enough that the fraction rounds away to nothing",
                Mathf.RoundToInt(faintBody.Durability * GameConfig.AnchoredRegenFraction) == 0);
            Check("committed to it", encounters.Engage(ArenaFaint));
            Check("and carrying a wound worth closing",
                combat.Wound(ArenaFaint, faintBody, axe.Deals, 4) == 4);

            // Driven with a salve rather than a strike, which is worth having for its own sake: it shows the
            // heal is on *the creature's turn* and not bolted to the attack verb. The axe would also finish a
            // body this slight in one swing.
            var beforeSalve = combat.WoundsOn(ArenaFaint);
            Check("a turn passes with no blow struck at all", encounters.Treat(Salve));
            Check("and it still closed a point, because a heal that rounds to nothing is not a heal",
                combat.WoundsOn(ArenaFaint) == beforeSalve - 1);
            Check("and that turn was billed like any other", resolve.Current < resolve.Max);
            Check("out", encounters.Flee());

            // -- running out of nerve inside a fight ------------------------
            // The consequence that makes the drain a cost rather than a decoration, and **none of it is new
            // code**: `SetTo` announces the empty bar, `CampManager` hears it, and `OnRoomEntered` ends the
            // fight on arrival. What is checked here is that the aura reaches that chain — and that
            // `EndPlayerTurn` asks whether the *fight* is still running rather than whether the bar is empty,
            // since the respawn refills it before control comes back.
            combat.ResetAll();
            vitality.ResetAll();
            states.SetState(ArenaAnchored, EntityState.Corrupted);
            resolve.SetTo(bill);

            // **Armed for the length of this probe, and the first attempt at the check below was worthless
            // without it.** `Arena` builds every body in this room with `Deals = None, Damage = 0`, so
            // `CreatureStrike` returns on its first line — which means a vitality check written here reads
            // *"nothing hit you"* against a fixture that cannot hit anything, and stays green however the
            // guard it is aimed at is broken. Deleting `EndPlayerTurn`'s post-drain `if (!InFight) return;`
            // left all 1041 checks passing until these two lines existed. Restored below.
            var anchoredDeals = anchoredBody.Deals;
            var anchoredDamage = anchoredBody.Damage;
            anchoredBody.Deals = DamageType.Edge;
            anchoredBody.Damage = 7;

            Check("committed with exactly one turn of nerve left", encounters.Engage(ArenaAnchored)
                && Mathf.IsEqualApprox(resolve.Current, bill));
            Check("that turn empties it", encounters.Strike());
            Check("and the fight is over rather than carrying on at zero", !encounters.InFight);
            Check("you are at a camp, which is where an empty nerve and an empty body both send you",
                CampManager.IsCamp(nav.CurrentRoom));
            Check("with the nerve back, because arriving at a bench refills it",
                Mathf.IsEqualApprox(resolve.Current, resolve.Max));
            Check("and the work closed over, because you left an anchored body",
                combat.WoundsOn(ArenaAnchored) == 0);

            // **The one check that holds the guard the comment above describes.** Everything from here up
            // passes with `if (!InFight) return;` deleted from `EndPlayerTurn` — the respawn refills the
            // nerve and closes the wounds either way, so both of those read the same on both sides. The body
            // does not: the placement was looked up *before* the drain, so it survives the banishment, and
            // `CreatureStrike` has no fight guard of its own. Without the return, a creature rooms away lands
            // a blow on you at the bench, on a turn of a fight that is already over.
            Check("and nothing followed you — a fight that ended mid-turn cannot strike you at the camp",
                Mathf.IsEqualApprox(vitality.Current, vitality.Max));

            anchoredBody.Deals = anchoredDeals;
            anchoredBody.Damage = anchoredDamage;
            Check("and the body is disarmed again for whatever reads it next",
                anchoredBody.Damage == 0 && anchoredBody.Deals == DamageType.None);

            // Hand back to the arena, since the block above walked out of it.
            nav.GoToRoom(ArenaRoomId);
            Check("and the arena is where the section carries on", nav.CurrentRoomId == ArenaRoomId);

            combat.ResetAll();
            vitality.ResetAll();
            resolve.SetTo(resolve.Max);
            states.SetState(ArenaLoosed, EntityState.Corrupted);
        }

        // -- 12: what death takes, and what it does not --------------------
        // **Last in the section**, because a collapse walks the player to a camp and `FindPlacement`
        // only ever searches the room they are standing in.
        combat.ResetAll();
        inv.AddItem(Reagent, 2);

        // The state, not the act: the maul goes back in hand over the axe block 14 left there, and `Equip`
        // answers AlreadyWorn if some earlier block had already put it there — which is correct of it and not
        // what this line is asking.
        inv.Equip(Maul);
        inv.Equip(Coat);
        Check("dressed for it",
            inv.EquippedWeapon?.Id == Maul && inv.EquippedArmour?.Id == Coat);
        Check("and carrying something loose", inv.Has(Reagent));

        // Low enough that one warded blow finishes it — warded, because the coat has to still be on when
        // the bar empties for the check below to be about anything.
        vitality.SetTo(3f);
        Check("engaging at the end of your strength", encounters.Engage(ArenaHitter));
        Check("its answer puts you down", encounters.Strike() && vitality.Current > 3f);
        Check("and the fight is over rather than following you out of the room", !encounters.InFight);
        Check("you wake at a camp", CampManager.IsCamp(nav.CurrentRoom));

        // §19, reversing §18: a fight can take nothing permanent from you. **And as of unit 8 it can take
        // nothing at all** — the last clause of this block read *"but the loose reagents stayed on the floor
        // where you fell"* and asserted the item loss that turned out to destroy Graveflower. Reversed rather
        // than deleted, because the claim underneath is what the section is for: **a fight costs the salves
        // you poured into it and nothing else.** Losing the axe you walked in with was never the design;
        // losing the materials turned out not to be either.
        Check("still wearing what you were wearing",
            inv.EquippedWeapon?.Id == Maul && inv.EquippedArmour?.Id == Coat);
        Check("and still carrying it, because worn is not spent", inv.Has(Maul) && inv.Has(Coat));
        Check("and still carrying the traps you set", traps.All(id => inv.Has(id)));
        Check("and the loose reagents too, because a collapse takes nothing", inv.Has(Reagent));

        // -- Leave nothing behind ------------------------------------------
        inv.Unequip(EquipSlot.Weapon);
        inv.Unequip(EquipSlot.Armour);
        encounters.ResetAll();
        combat.ResetAll();
        vitality.ResetAll();

        foreach (var id in new[] { ArenaBeast, ArenaFreed, ArenaKilled })
        {
            world.SetFlag(Flags.Freed(id), false);
            world.SetFlag(Flags.Defeated(id), false);
        }

        world.SetFlag(Flags.Cleansed(ArenaRoomId), false);
    }

    // ---- Travel (unit 5) -------------------------------------------------

    /// <summary>
    /// The return-to-camp verb: reachable from any screen with no run live, refused inside a fight and
    /// inside a pocket, and pointing at the camps you have stood in. `docs/combat.md` §17 as amended
    /// 2026-08-01 is the authority.
    ///
    /// <para><b>Immediately after <see cref="GateEncounters"/>, and that is a dependency rather than a
    /// preference.</b> This section needs a live fight to be refused during, and the arena is the only
    /// fixture with bodies on it — running before that gate would leave wounds on creatures whose damage
    /// numbers it reads. Everything below it is room-independent (the Snarl, the spine and the sweeps all
    /// build their own fixtures), which is what makes it safe to leave the player somewhere new.</para>
    ///
    /// <para><b>Membership, never a count of the whole list.</b> Three <c>verify_</c> rooms hold a bench —
    /// <c>verify_camp</c>, <c>verify_camp_two</c> and <c>verify_loop_inner</c> — and the authored map holds
    /// three more, so any check phrased as <i>"two camps are known"</i> would be a check on how many
    /// fixtures happen to exist. The claims here are about named rooms being in or out of the list.</para>
    /// </summary>
    private static void GateTravel()
    {
        Section("travel  getting to a camp from anywhere");

        var camp = CampManager.Instance;
        var nav = NavigationManager.Instance;
        var world = WorldStateManager.Instance;
        var vitality = VitalityManager.Instance;
        var resolve = ResolveManager.Instance;
        var encounters = EncounterManager.Instance;
        var pockets = PocketManager.Instance;
        var states = EntityStateManager.Instance;

        Check("the verb has a manager to live on", camp != null);
        if (camp == null) return;

        // The one thing in this section a headless run cannot exercise, and the one that makes all of it
        // reachable. `IsActionPressed` on a name that is not in the map returns false rather than throwing,
        // so a typo in `project.godot` is a key that silently does nothing — and unlike the F-keys above,
        // nobody would suspect scaffolding, because this is a shipping control. Same guard as
        // `debug_wound`, for the reason that one records: a hand-edited input map fails quietly.
        Check("the key that opens the menu is bound", InputMap.HasAction("camp"));

        SetRealm(GameRealm.Waking);
        pockets.ForceClear();
        encounters.ResetAll();

        // -- A camp becomes a destination by being stood in, and by nothing else --
        // Cleared rather than assumed unset: GateCamps walked into both of these long before this ran, so
        // without this the first three checks would be testing that gate's route instead of this rule.
        world.SetFlag(Flags.CampFound("verify_camp"), false);
        world.SetFlag(Flags.CampFound("verify_camp_two"), false);

        nav.GoToRoom("verify_wilds");
        Check("open ground never becomes somewhere you can travel to",
            !CampManager.KnownCamps.Contains("verify_wilds"));
        Check("and a camp nobody has stood in is not offered either",
            !CampManager.KnownCamps.Contains("verify_camp"));
        Check("so the verb refuses it for not being found",
            camp.InspectTravel("verify_camp") == TravelBlock.NotFound);
        Check("and attempting it anyway moves nobody",
            !camp.TravelToCamp("verify_camp") && nav.CurrentRoomId == "verify_wilds");

        nav.GoToRoom("verify_camp");
        Check("standing in one is what makes it a destination",
            CampManager.KnownCamps.Contains("verify_camp"));

        // The check that keeps the verb from being a map. Without it a fresh game could travel to the
        // chapter-3 store from the cottage doorstep, which is the one way fast travel could break the
        // pacing it exists to protect.
        Check("and finding one camp does not find the others",
            !CampManager.KnownCamps.Contains("verify_camp_two"));

        // -- The verb itself, from open ground --
        nav.GoToRoom("verify_camp_two");
        nav.GoToRoom("verify_wilds");
        vitality.SetTo(vitality.Max - 40f);
        resolve.SetTo(resolve.Max * 0.4f);

        Check("the verb carries you there from open ground", camp.TravelToCamp("verify_camp"));
        Check("and you are standing in it", nav.CurrentRoomId == "verify_camp");

        // The four below are one claim: **there is exactly one arrival path**. `TravelToCamp` navigates and
        // stops, so the refill, the rebind and the line are the same code that runs when you walk in. An
        // implementation that set the current room directly would pass the two checks above and fail all
        // four of these, which is precisely the split worth having.
        Check("arriving by menu refills the body, exactly as walking in does",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));
        Check("and the nerve", Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        Check("and rebinds where you would wake", camp.ReturnCampRoomId == "verify_camp");
        Check("and the camp you came from is still a destination",
            CampManager.KnownCamps.Contains("verify_camp_two"));

        // -- The refusals that are about the destination rather than the route --
        Check("the camp you are standing in is refused, and not as a failure",
            camp.InspectTravel("verify_camp") == TravelBlock.AlreadyHere);
        Check("a room with no bench is not somewhere to travel to",
            camp.InspectTravel("verify_wilds") == TravelBlock.NotACamp);
        Check("and neither is a room that does not exist",
            camp.InspectTravel("no_such_room_anywhere") == TravelBlock.NotACamp);

        // -- §17's first refusal: not out of a fight --
        // Refused rather than priced as a flight: a door clicked mid-fight flees and pays a parting blow,
        // and a camp refills both pools, so one key doing both would be escape plus a free heal.
        nav.GoToRoom(ArenaRoomId);
        states.SetState(ArenaTough, EntityState.Corrupted);
        Check("a fight is running for the refusal to be about", encounters.Engage(ArenaTough));
        Check("the verb refuses inside a fight",
            camp.InspectTravel("verify_camp") == TravelBlock.InFight);
        Check("attempting it anyway leaves you in the room",
            !camp.TravelToCamp("verify_camp") && nav.CurrentRoomId == ArenaRoomId);

        // **The half a one-sided check cannot see, and the reason this is a pair.**
        // `EncounterManager.OnRoomEntered` ends any fight on arrival — its own comment names "the camp
        // menu" as a path it covers — so a refusal that navigated *first* and then bailed would still
        // leave the player in a room with no fight and look identical to a working guard from the three
        // checks above. This is the one that goes red when the guard is deleted.
        Check("and the fight you were in is still running", encounters.InFight);
        encounters.Flee();
        Check("and the ordinary way out of a fight still works", !encounters.InFight);

        // -- §17's second refusal: not out of a pocket --
        nav.GoToRoom("verify_camp");
        SetRealm(GameRealm.Dream);
        Check("the realm alone does not close the road out",
            camp.InspectTravel("verify_camp_two") == TravelBlock.None);
        SetRealm(GameRealm.Waking);

        // Keyed on being in a pocket, not on the realm, so the pair above and below have to disagree for the
        // right reason — a quiet pocket is the Dream with nothing draining, and a camp reached from one must
        // still be a camp (`GateCamps` asserts the same split about the bench).
        //
        // Driven with a real pocket since unit 6b; it borrowed a live heist until then. The room is captured
        // *after* entering even though a pocket does not navigate, because the claim being made is
        // "wherever you were, you are still there" and reading it from the manager would assume that.
        resolve.SetTo(resolve.Max);
        Check("a pocket is entered for the refusal", pockets.Enter(TravelPocketId));
        var insidePocket = nav.CurrentRoomId;
        Check("the verb refuses inside a pocket",
            camp.InspectTravel("verify_camp_two") == TravelBlock.InPocket);
        Check("and attempting it leaves you in the pocket",
            !camp.TravelToCamp("verify_camp_two") && nav.CurrentRoomId == insidePocket);

        // **A fourth check stood here and was deleted by its own break-test.** It read *"and the run is
        // still live, so the refusal was not an ejection"* — `run.IsRunning` — and it stayed green with the
        // pocket guard removed, because a `DreamRunManager` run survives ordinary navigation to a Waking
        // room. So it passed for a reason other than the thing it was aimed at, which is the seventh
        // blind-spot class, and it was found the way that class is always found: by predicting which checks
        // would go red together and then reading the one that did not move.
        //
        // Nothing replaced it, and that is the right outcome rather than a gap. The harm the guard prevents
        // is *standing at a bench with a run live* — invariant 2's exact shape, a workbench that is not a
        // camp — and the check above already asserts the player never got there. A check that still passes
        // while measuring nothing is worse than one check fewer; `ZoneCorruptionScalar`'s probe was retired
        // on the same reasoning in unit 2.
        pockets.Leave();
        Check("the road opens the moment you are out, from the same room",
            camp.TravelToCamp("verify_camp_two") && nav.CurrentRoomId == "verify_camp_two");
        SetRealm(GameRealm.Waking);

        // -- The menu, and the only honest way to check a `Pressed` connected from C# --
        // A C#-side signal connection is not introspectable from C# (unit 3a's re-read established that
        // over the satchel's repaint), so the wiring is asserted by firing it and reading where the player
        // ended up rather than by asking whether anything is subscribed.
        nav.GoToRoom("verify_wilds");
        var panel = _hud?.Camp;
        Check("the camp menu is in the tree, whether or not a scene authored it", panel != null);

        if (panel != null)
        {
            Check("it opens out on open ground", panel.Open());
            Check("and offers exactly the camps that are found",
                panel.RowCount == CampManager.KnownCamps.Count);
            Check("it tints the room it is drawn over", panel.DimsTheRoom);
            Check("and the tint is behind the rows rather than over them", panel.DimDrawsBehindTheRows);

            Check("pressing a row is the verb", panel.DebugPressRowNamed("A Bench Under Canvas"));
            Check("so the menu is down and you are in that camp",
                !panel.IsOpen && nav.CurrentRoomId == "verify_camp");

            // The camp you are standing in draws dimmed rather than absent — the same *locked, not spent*
            // distinction unit 1.5 drew for hotspots. A list that hid your own camp would change length as
            // you walked, and `AlreadyHere` is a statement rather than something withheld.
            Check("reopened at a bench, that bench is still listed", panel.Open());

            var here = -1;
            for (var i = 0; i < panel.RowCount; i++)
                if (panel.RowLabelAt(i).Contains("A Bench Under Canvas")) here = i;

            Check("the camp you are standing in has a row of its own", here >= 0);
            Check("it cannot be pressed, and it says why in the manager's own words",
                here >= 0 && !panel.RowEnabledAt(here) &&
                panel.RowLabelAt(here).Contains(CampManager.Words(TravelBlock.AlreadyHere)));
            Check("and pressing it moves nobody",
                !panel.DebugPressRowNamed("A Bench Under Canvas") &&
                nav.CurrentRoomId == "verify_camp");

            // -- Two dims do not stack, in both directions --
            // Both panels tint the room at 55% black, which is the stated reason a mend and a fight shut the
            // satchel. Those two could rely on the world being unclickable by the time they open; these are
            // opened by keys, so the exclusion has to be enforced rather than inherited. Driven through
            // `Hud`'s two toggles rather than the panels' own, because that is what the keys call — asserting
            // it on `CampPanel.Open` would test a method no keypress reaches.
            panel.Close();
            _hud.ToggleSatchel();
            Check("the satchel opens", _hud.Inventory?.IsOpen == true);
            Check("and opening the camp menu shuts it", _hud.ToggleCampMenu() && panel.IsOpen);
            Check("so only one dim is ever drawn", _hud.Inventory?.IsOpen != true);
            _hud.ToggleSatchel();
            Check("and opening the satchel again shuts the menu",
                !panel.IsOpen && _hud.Inventory?.IsOpen == true);
            _hud.ToggleSatchel();

            // -- Reported in play 2026-08-04: the menu was up behind the fight screen --
            //
            // Two candidate causes, and they are checked separately because the fix differs. **One**: the
            // root does not actually cover the screen, so a creature beside the centred frame stays
            // clickable and a fight can start from inside the menu — the eighth blind-spot class again.
            // **Two**: the menu is open when a fight starts by some other route, and nothing takes it down.
            //
            // The second guard was **considered and declined** while building this unit, on the reasoning
            // that no path could produce it. The report falsifies that, and the lesson is the one this
            // project keeps relearning: *a dead-latch argument is only as good as the paths you thought of.*
            // `Hud` closes the satchel on `FightStarted` for exactly this reason and the camp menu was left
            // out of it.
            Check("the menu covers the screen, so nothing under it can be clicked", panel.BlocksTheRoom);

            panel.Close();
            nav.GoToRoom(ArenaRoomId);
            states.SetState(ArenaCut, EntityState.Corrupted);
            Check("the menu opens in a room with a creature in it, before any fight", panel.Open());
            Check("and a fight starting takes it down rather than leaving it behind the fight screen",
                encounters.Engage(ArenaCut) && !panel.IsOpen);
            encounters.Flee();

            // Mid-fight the menu refuses to *open* rather than drawing a screenful of dead rows —
            // `FightPanel.Open`'s argument. The manager's refusal above is the enforcement; this is the
            // courtesy, and it is checked separately because the two can be broken independently.
            panel.Close();
            nav.GoToRoom(ArenaRoomId);
            states.SetState(ArenaCut, EntityState.Corrupted);
            Check("a second fight, to open the menu against", encounters.Engage(ArenaCut));
            Check("the menu will not even open inside a fight", !panel.Open() && !panel.IsOpen);
            Check("and that fight is still running too", encounters.InFight);

            // -- And the satchel obeys the same rule, reported in play 2026-08-04 --
            //
            // `I` opened the satchel behind the fight screen where `C` refused with a line. **The draw order
            // was the smaller half.** `InventoryCraftingManager.UseItem` had no fight guard at all, so the
            // Use button healed for free — no turn spent, nothing counted against `SalveSlotsPerFight` —
            // which is a hole in the combat economy rather than a cosmetic fault. §18 makes the turn the only
            // currency inside a fight, and this was a second one.
            //
            // Three checks because three things can break independently: the screen, the manager rule, and
            // the fight's own verb still working through the guard that blocks the satchel.
            // Emptied first rather than added to. The satchel arrives here carrying whatever
            // `GateEncounters` left in it, and the two count checks below would then be measuring that
            // gate's leftovers — the fourth blind-spot class in miniature, a check that depends on state it
            // did not set. Both were red on the first run for exactly this reason.
            var inv = InventoryCraftingManager.Instance;
            inv.ResetAll();
            inv.AddItem("salve_clearmind", 2);
            VitalityManager.Instance.SetTo(VitalityManager.Instance.Max - 20f);

            Check("the satchel will not open inside a fight either",
                !_hud.ToggleSatchel() && _hud.Inventory?.IsOpen != true);
            Check("and a salve cannot be spent from it, turn-free, while a fight is running",
                inv.InspectUse("salve_clearmind") == UseBlock.InFight);
            Check("attempting it anyway keeps the salve",
                !inv.UseItem("salve_clearmind") && inv.GetCount("salve_clearmind") == 2);

            // The other side of the same guard, and the pair is the point: block the satchel and the fight's
            // own verb must still work, or the fix has deleted `Treat` instead of protecting it. One salve
            // leaves the satchel, and it costs the turn the satchel path did not.
            var turnBefore = encounters.TurnNumber;
            Check("but the fight's own verb still treats", encounters.Treat("salve_clearmind"));
            Check("and that one cost a turn, which the satchel never did",
                encounters.TurnNumber == turnBefore + 1 && inv.GetCount("salve_clearmind") == 1);

            Check("and the fight survived all of that", encounters.InFight);
            encounters.Flee();
            inv.ResetAll();
            VitalityManager.Instance.ResetAll();
        }

        // -- Destinations survive a save, because they are flags and not a new field --
        const string path = "user://verify_travel.json";

        encounters.ResetAll();
        nav.GoToRoom("verify_wilds");
        Check("the travel save writes", SaveService.Save(path));

        world.SetFlag(Flags.CampFound("verify_camp"), false);
        world.SetFlag(Flags.CampFound("verify_camp_two"), false);
        Check("both destinations were actually cleared",
            !CampManager.KnownCamps.Contains("verify_camp") &&
            !CampManager.KnownCamps.Contains("verify_camp_two"));

        Check("the travel save loads", SaveService.Load(path));
        Check("and it puts you back on open ground, where nothing can rebind",
            nav.CurrentRoomId == "verify_wilds");
        Check("so both destinations came out of the file",
            CampManager.KnownCamps.Contains("verify_camp") &&
            CampManager.KnownCamps.Contains("verify_camp_two"));

        SaveService.DeleteSave(path);

        // -- A save written before this unit: a return point and no destinations --
        //
        // **Built as a file rather than by calling `ImportReturnCamp` directly, and the difference is the
        // whole check.** Driving the method proves only that it sets a flag, which is one line staring at
        // itself. What has to hold is an *ordering*: `SaveService` runs `ImportState` first and
        // `ImportReturnCamp` after, so the re-derived flag is written on top of the file's flag dictionary
        // rather than under it. Reverse those two lines and this pair is the only thing in 700 checks that
        // notices.
        //
        // The old format's exact shape is reachable without hand-writing JSON: stand in the camp so the
        // return point binds, walk out, clear the flag, and save. That file has a return point naming a
        // camp and no `camp.*.found` in it — which is every save written before today.
        const string oldPath = "user://verify_travel_old.json";

        nav.GoToRoom("verify_camp");
        nav.GoToRoom("verify_wilds");
        world.SetFlag(Flags.CampFound("verify_camp"), false);
        Check("a save in the pre-unit-5 shape is written",
            camp.ReturnCampRoomId == "verify_camp" &&
            !CampManager.KnownCamps.Contains("verify_camp") &&
            SaveService.Save(oldPath));

        camp.ResetAll();
        Check("the travel save loads", SaveService.Load(oldPath));
        Check("and an old file's return point is a destination again",
            CampManager.KnownCamps.Contains("verify_camp"));
        Check("so the menu it loads into is not empty",
            camp.InspectTravel("verify_camp") == TravelBlock.None);

        SaveService.DeleteSave(oldPath);
    }

    /// <summary>
    /// A Waking screen with a pocket entrance on it, authored the way §5's contract says one should be:
    /// <b>two hotspots on one flag with opposite <c>RequiredFlagValue</c>s.</b> The entrance while the pocket
    /// is unspent, the broken remnant afterwards.
    /// </summary>
    /// <remarks>
    /// It exists because unit 6's *"the artifact breaks"* is otherwise a claim about authoring that no code
    /// enforces and no check reads. There is no field for it and there should not be — the freed-NPC pattern
    /// already does the job — so the only way to prove the pattern works is to build a room that uses it.
    /// <b>This is also the fixture that keeps the authored sweep from being a wish</b>: with no pocket
    /// entrance authored anywhere in <c>data/rooms/</c> yet, a sweep saying *"every entrance names a
    /// registered pocket"* would be a universal over an empty set, which unit 4b established is a wish
    /// rather than a check.
    /// </remarks>
    private static RoomData PocketDoorRoom()
    {
        var room = new RoomData
        {
            Id = PocketDoorRoomId,
            DisplayName = "A Broken Thing On The Ground",
            ChapterId = "verify",
        };

        var taken = Flags.PocketCleared(PocketKnot);

        room.Hotspots.Add(new HotspotData
        {
            Id = PocketDoorId,
            Label = "Something Half-Remembered",
            Kind = HotspotKind.CrossToDream,
            PocketId = PocketKnot,
            RequiredFlag = taken,
            RequiredFlagValue = false,
        });

        room.Hotspots.Add(new HotspotData
        {
            Id = PocketRemnantId,
            Label = "Broken, Now",
            Kind = HotspotKind.Examine,
            ExamineText = "Whatever it was holding is out of it.",
            RequiredFlag = taken,
            RequiredFlagValue = true,
        });

        return room;
    }

    // ---- Dream pockets (unit 6a) -----------------------------------------

    private const string PocketKnowledge = "verify_pocket_knowledge";
    private const string PocketKnot = "verify_pocket_knot";
    private const string PocketBoss = "verify_pocket_boss";
    private const string PocketBlueprint = "lore_verify_pocket";

    /// <summary>
    /// A pocket that grants a <b>real authored page</b>, and the one fixture here that has to be.
    /// </summary>
    /// <remarks>
    /// <see cref="PocketBlueprint"/> is synthetic on purpose — *"so a gate never grants something the
    /// ending's bill would count"* — and that is exactly why unit 7b's whole deliverable had no witness for
    /// three units: <c>PocketManager.GrantPage</c> looks the blueprint id up <i>as an item</i> and pushes a
    /// warning when there is no such item, so a synthetic id exercises the refusal and never the grant. The
    /// knowledge half was asserted the whole time, which is what made the gap invisible.
    /// <para>
    /// So one fixture pocket grants `lore_craft_stoneworking`, which is on disk. It is a separate pocket
    /// rather than a change to the synthetic one, so the *"grants nothing the ending counts"* rule still
    /// holds for every check that relied on it, and the page is taken back out of the satchel below.
    /// </para>
    /// </remarks>
    private const string PocketPage = "verify_pocket_page";
    private const string PocketPageBlueprint = "lore_craft_stoneworking";

    /// <summary>
    /// A pocket for <see cref="GateTravel"/> to be refused inside of. Its own rather than borrowed from
    /// <see cref="GatePockets"/>, because that section completes and re-opens its fixtures repeatedly and a
    /// shared one would make each section's setup depend on the other's leftovers — which has already cost
    /// two red checks once in this file.
    /// </summary>
    private const string TravelPocketId = "verify_pocket_travel";

    private const string PocketDoorRoomId = "verify_pocket_door";
    private const string PocketDoorId = "verify_pocket_entrance";
    private const string PocketRemnantId = "verify_pocket_remnant";

    /// <summary>Points of max Resolve the synthetic knowledge pocket pays, so the check reads a relationship.</summary>
    private const int PocketResolveGrant = 7;

    /// <summary>
    /// A scene that really exists, borrowed to give a fixture pocket one for the length of two checks
    /// (unit 10's routing). <b>The reference pocket's scene rather than a synthetic path</b>, because
    /// <c>PocketHost</c> loads whatever this names and the crossing gate below asserts it resolves — a made-up
    /// path would fail both, and a path that failed to load would fall back to the same empty pocket a blank
    /// one gives, which is precisely the confusion <c>PocketData.HasScene</c> refuses to make.
    /// </summary>
    private const string ProbeScenePath = "res://scenes/dream/DreamProbe.tscn";

    /// <summary>
    /// The way into the Dream and back out: what a pocket grants, what it writes, and what it must not write
    /// when you leave without finishing. Unit 6a, built to the discussion of 2026-08-04.
    ///
    /// <para><b>Every fixture here has a blank <c>ScenePath</c>, and that is the design rather than a
    /// shortcut.</b> <c>PocketManager</c> owns being-in-a-pocket and <c>PocketHost</c> owns geometry, so a
    /// headless run enters, grants and leaves with no 3D anywhere. That separation is the only reason the
    /// Waking-side guards below can be probed at all once <c>DreamRunManager</c> is gone — and it is why the
    /// checks say <i>inside a pocket</i> rather than <i>inside a scene</i>.</para>
    ///
    /// <para><b>The one thing this section cannot test is the 3D half.</b> Whether a pocket draws, walks or
    /// reads as a place is Jeremy's playtest and always was.</para>
    /// </summary>
    private static void GatePockets()
    {
        Section("pockets  the way into the Dream, and the way back");

        var pockets = PocketManager.Instance;
        var nav = NavigationManager.Instance;
        var world = WorldStateManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        var resolve = ResolveManager.Instance;
        var camp = CampManager.Instance;
        var encounters = EncounterManager.Instance;
        var states = EntityStateManager.Instance;
        var vitality = VitalityManager.Instance;

        Check("the manager is registered", pockets != null);
        if (pockets == null) return;

        // The one thing a headless run cannot press, and the only way in until pass 2 authors an entrance.
        // `IsActionPressed` on an unbound name returns false rather than throwing, so a typo is a key that
        // silently does nothing — and this one is bound to **F10 and not F4**, because the probe scene it
        // loads already binds F4 to its own readout toggle.
        Check("the debug key that reaches the seam is bound", InputMap.HasAction("debug_pocket"));

        pockets.ForceClear();
        encounters.ResetAll();
        SetRealm(GameRealm.Waking);

        // -- The authored files on disk, counted without consulting the registry --
        //
        // The fifth blind-spot class, closed in the sitting that created the directory rather than after the
        // first silent parse failure. `data/pockets/` holds exactly the reference pocket today.
        //
        // **Synthetic pockets are excluded by key prefix, exactly as `GateAuthoredContent` excludes
        // `verify_` rooms — and 6b is what forced it.** Retiring the heist meant `GateTravel` needed a real
        // pocket to be refused inside of, and registering one turned both of these red: *2 of 1*, and a
        // dangling scene path belonging to a fixture that never wanted a scene. A gate must stand on ground
        // the verifier owns, and a *count* of authored things is the one shape where a synthetic fixture is
        // indistinguishable from content unless it is filtered out by name.
        var authoredPockets = pockets.Pockets
            .Where(kv => !kv.Key.StartsWith("verify_"))
            .Select(kv => kv.Value)
            .ToList();

        Check($"every pocket file on disk registered a pocket "
            + $"({authoredPockets.Count} of {FilesIn(GameConfig.PocketDataDir)})",
            FilesIn(GameConfig.PocketDataDir) > 0 &&
            authoredPockets.Count == FilesIn(GameConfig.PocketDataDir));

        // -- The crossing that will not appear on its own --
        // A `ScenePath` is one authored collection naming something in another, which is the shape that hid
        // 33 dangling harvest nodes behind 439 green gates.
        //
        // **Blank is no longer a failure, and the distinction is unit 10's (2026-08-05).** This used to read
        // `IsNullOrEmpty(p.ScenePath) || !Exists(...)`, which was right while a pocket without a scene was
        // useless. Now thirteen of the fourteen are deliberately scene-less stand-ins granted from the Waking
        // world, so the two conditions have to come apart: **a blank path is the author saying *not yet*, a
        // path that does not resolve is the author being wrong.** Only the second is a defect, and collapsing
        // them would have forced a choice between deleting this check and inventing fourteen fake scene paths —
        // the latter being much worse, since `PocketData.HasScene` would then read true for every stand-in and
        // the whole mechanism would silently switch itself off.
        var badScene = authoredPockets
            .Where(p => p.HasScene && !ResourceLoader.Exists(p.ScenePath))
            .Select(p => $"{p.Id}: {p.ScenePath}")
            .ToList();
        Check($"every pocket that names a scene names one that is really there{Listing(badScene)}",
            badScene.Count == 0);

        // **The debt, counted — because the check above can no longer see it.** Relaxing a gate to permit a
        // temporary state has to leave something that still reports the state, or the permission is
        // indistinguishable from the work being done. §3's recurring shape: green over an absence.
        //
        // Phrased as a census rather than a bill on purpose. There is no number of scene-less pockets that is
        // *wrong* — Jeremy builds the fourteen Dream levels at his own pace — so this must not fail while it is
        // being worked through; `--verify-content`'s row is where the shortfall is tracked. What it must not do
        // is stay silent, which is why the counts are in the message whether it passes or not.
        //
        // **The one thing it does assert is that the crossing has not gone dead.** `Enter`, `PocketHost`,
        // `Leave`, `Eject` and every 3D hook are reachable in play only through a pocket that has a scene, and
        // with thirteen stand-ins the reference pocket is the last one holding that path open — blank its
        // `ScenePath` and nothing in the game can cross into the Dream at all, with every other check here
        // still green because the fixtures drive the manager directly. That is a live route silently deleted
        // by an edit to one authored field, so it gets a check. It reddens the day someone points the probe at
        // nothing, and it retires on its own once real pockets have scenes.
        //
        // The behavioural half of this — that a scened pocket refuses `TakeFromWaking` and a scene-less one
        // accepts it — is asserted on a fixture in the door block below, where `InPocket` and `InFight` are
        // known rather than assumed. Asking it here would have been confounded by both.
        var scened = authoredPockets.Where(p => p.HasScene).ToList();
        var standIns = authoredPockets.Where(p => !p.HasScene).ToList();
        Check($"and at least one pocket is still walked rather than taken, so the crossing is reachable in play "
            + $"({scened.Count} with a scene, {standIns.Count} standing in of {authoredPockets.Count})",
            scened.Count >= 1);

        // -- Fixtures. Synthetic, so a gate never grants something the ending's bill would count --
        var knowledge = new PocketData
        {
            Id = PocketKnowledge,
            DisplayName = "Somewhere Remembered",
            Kind = PocketKind.Knowledge,
            GrantsBlueprintId = PocketBlueprint,
            GrantsMaxResolve = PocketResolveGrant,
        };
        var knot = new PocketData { Id = PocketKnot, DisplayName = "A Knot", Kind = PocketKind.Knot };
        var boss = new PocketData
        {
            Id = PocketBoss,
            DisplayName = "A Tether",
            Kind = PocketKind.Boss,
            TargetEntityId = ArenaTough,
            GrantsAbility = DreamVerb.Float,
        };

        var pagePocket = new PocketData
        {
            Id = PocketPage,
            DisplayName = "Somewhere He Wrote Down",
            Kind = PocketKind.Knowledge,
            GrantsBlueprintId = PocketPageBlueprint,
        };

        pockets.DebugRegisterPocket(knowledge);
        pockets.DebugRegisterPocket(knot);
        pockets.DebugRegisterPocket(boss);
        pockets.DebugRegisterPocket(pagePocket);

        foreach (var id in new[] { PocketKnowledge, PocketKnot, PocketBoss, PocketPage })
            world.SetFlag(Flags.PocketCleared(id), false);

        world.SetFlag(Flags.KnotCut(PocketKnot), false);
        inv.ResetAll();
        resolve.ResetAll();

        // -- Going in --
        nav.GoToRoom("verify_wilds");
        Check("out here you are not in a pocket", !pockets.InPocket);
        Check("and the shared predicate agrees", !PocketManager.InsideTheDream);

        // Sampled before the crossing, so invariant 2 is measured *across* it rather than at a moment
        // inside it — a body that moved and moved back would pass a snapshot taken in the middle.
        var vitalityOnEntry = vitality?.Current ?? 0f;

        Check("the way in is open", pockets.CanEnter(PocketKnowledge));
        Check("crossing puts you inside", pockets.Enter(PocketKnowledge) && pockets.InPocket);
        Check("and names which pocket", pockets.CurrentPocketId == PocketKnowledge);
        Check("the realm follows you in, because you are there rather than looking",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Dream);
        Check("and it remembers the screen to put you back on", pockets.ReturnRoomId == "verify_wilds");

        // -- The five invariants that had **no** probe outside a live heist --
        //
        // **Found while planning 6b, and it corrects a claim I made out loud: 6b was not "pure deletion."**
        // `GateHeistMechanics`'s own doc comment predicted this — *"unit 6 must keep a probe of this shape
        // whatever ends a pocket run, because the defect is in the subscriber and not in the ending"* — and
        // a grep confirmed every assertion of the realm lock, the HUD's dim trio, the pool-order restore and
        // **`RoomView.DisplayedRoomId`** lived inside a heist-driven block. Deleting that section first would
        // have removed 100% of the regression cover for the ejection post-mortem's fix.
        Check("the realm is locked while you are inside", RealmTransitionManager.Instance.TransitionsLocked);
        Check("the body dims to say it is not in play",
            _hud == null || _hud.VitalityOpacity < 1f);
        Check("but stays readable rather than vanishing, because it is a promise not an absence",
            _hud == null || _hud.VitalityOpacity > 0.2f);
        Check("and the nerve is never dimmed",
            _hud == null || Mathf.IsEqualApprox(_hud.ResolveOpacity, 1f));

        // **A pocket is not a room.** The most load-bearing structural claim in the unit: every room-graph
        // derivation the old model had — the maze, the pursuer's path, "is this room in the Dream" — has
        // nothing to derive, and `CurrentRoomId` cannot name a pocket. `SaveService`'s stranding guard
        // becomes unnecessary rather than migrated because of this line.
        Check("a pocket id is not a room, so navigation never learns about it",
            nav.GetRoom(PocketKnowledge) == null && nav.CurrentRoomId == "verify_wilds");

        // -- The Waking-side guards, probed from inside a *pocket* --
        //
        // Added here rather than in 6b, which makes that unit pure deletion. Each has a twin elsewhere that
        // probes the same guard from inside a live heist; the twin is what dies.
        //
        // **Two of these were written standing on `verify_wilds` and passed for the wrong reason.** Open
        // ground holds no bench and no station, so *"the bench is shut"* and *"the station is shut"* were
        // true whether the pocket guard existed or not — the seventh blind-spot class, found by a break-test
        // that predicted three reds and produced one. Each now stands on ground where the guard is the
        // **only** thing that could refuse, with a control on the same ground proving it is otherwise open.
        Check("the camp verb refuses, and says which refusal",
            camp.InspectTravel("verify_camp") == TravelBlock.InPocket);
        Check("and the HUD puts the nerve above the body, because the body cannot move in here",
            _hud == null || _hud.TopPoolLabel == "RESOLVE");

        pockets.Leave();
        inv.AddItem("herb_bitterroot", 5);
        inv.AddItem("mat_tallow", 5);

        // The bench, from a room that *is* a camp.
        nav.GoToRoom("verify_camp");
        Check("standing at a bench out of a pocket is being at camp", camp.AtCamp);
        Check("the same bench is shut from inside one",
            pockets.Enter(PocketKnowledge) && !camp.AtCamp);
        pockets.Leave();
        Check("and open again the moment you are back", camp.AtCamp);

        // The station, from a room that holds one and no bench — the fixture unit 3b built for exactly this
        // shape of question, reused because the question is the same one.
        nav.GoToRoom(StationRoomId);
        Check("a field station works out of a pocket", inv.Inspect(StationRecipeId) == CraftBlock.None);
        Check("and is shut from inside one",
            pockets.Enter(PocketKnowledge) &&
            inv.Inspect(StationRecipeId) == CraftBlock.NotAtStation);
        pockets.Leave();
        Check("and open again on the way out", inv.Inspect(StationRecipeId) == CraftBlock.None);

        inv.ResetAll();
        nav.GoToRoom("verify_wilds");
        Check("back on open ground for the rest of the section",
            pockets.Enter(PocketKnowledge) && pockets.ReturnRoomId == "verify_wilds");

        // -- One at a time --
        Check("you cannot cross into a second pocket from inside one",
            pockets.InspectEnter(PocketKnot) == PocketBlock.AlreadyInside);

        // -- Completing: the grant, the flag, the return --
        var maxBefore = resolve.Max;
        Check("the blueprint is not known yet", !inv.IsDiscovered(PocketBlueprint));

        Check("finishing the pocket reports that it did", pockets.Complete());
        Check("and it grants the craft family — the only thing in the game that ever will",
            inv.IsDiscovered(PocketBlueprint));
        Check("and raises the ceiling by exactly what it was authored to give",
            Mathf.IsEqualApprox(resolve.Max, maxBefore + PocketResolveGrant));
        Check("with the new headroom arriving full, so the reward is visible in the frame it lands",
            Mathf.IsEqualApprox(resolve.Current, resolve.Max));
        Check("it writes the pocket's flag", world.GetFlag(Flags.PocketCleared(PocketKnowledge)));
        Check("you are out, and on the screen you left",
            !pockets.InPocket && nav.CurrentRoomId == "verify_wilds");
        Check("and the realm came back with you",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("and unlocked behind you", !RealmTransitionManager.Instance.TransitionsLocked);
        Check("the camp verb works again out here",
            camp.InspectTravel("verify_camp") == TravelBlock.None);

        // -- The emphasis comes back, and Vitality never moved --
        Check("the body is the top bar again once you are out",
            _hud == null || _hud.TopPoolLabel == "VITALITY");
        Check("and undimmed, because it can be hurt out here",
            _hud == null || Mathf.IsEqualApprox(_hud.VitalityOpacity, 1f));

        // Invariant 2, measured across the whole crossing rather than at a moment inside it. Nothing in the
        // Dream may cost the body, and a pocket is the only place that promise can be broken.
        Check("Vitality never moved while you were in there",
            vitality == null || Mathf.IsEqualApprox(vitality.Current, vitalityOnEntry));

        // -- What the *view* was drawing, which is the ejection post-mortem's regression cover --
        //
        // The defect that made this necessary is in the subscriber, not the ending: a nested navigation
        // delivers its `RoomEntered` first and leaves the stale one to land last, so a view that trusts the
        // payload finishes by painting the room you were just pulled out of. Managers stayed correct
        // throughout, which is why every other check passed while the game was visibly broken.
        //
        // A pocket cannot reproduce the nesting — `Complete` is called by a scene, not from inside a
        // `RoomEntered` handler — but the **guard** in `RoomView.OnRoomEntered` and `Hud.OnRoomEntered` is
        // what must stay covered, and this is now the only place asking either view what it is drawing.
        Check("the view shows the screen you came back to",
            _view == null || _view.DisplayedRoomId == "verify_wilds");
        Check("and the HUD names it, rather than the pocket you left",
            _hud == null || _hud.DisplayedRoomName == "Open Ground");

        // The control the post-mortem insists on: without it, *"the view never shows the wrong room"* would
        // also be satisfied by a view that draws nothing at all. An ordinary navigation, no Dream involved —
        // which is a better home for it than the Dream room it used to live in.
        nav.GoToRoom("verify_camp");
        Check("and it tracks an ordinary navigation too, so it is not simply blank",
            _view == null || _view.DisplayedRoomId == "verify_camp");
        nav.GoToRoom("verify_wilds");

        // **And the guard itself, which the two checks above do not reach.** They prove the views follow
        // navigation; the post-mortem's actual fix is that they *refuse a payload that is no longer current*,
        // and only a nested emit produced that in the old model. A pocket cannot nest — `Complete` is called
        // by a scene rather than from inside a `RoomEntered` handler — so rather than lose the cover, the
        // stale payload is raised by hand, the way `GateCamps` fakes a stale arrival at a bench.
        //
        // **This is a better probe than the one it replaces**, and worth saying so: the heist version
        // reproduced *one situation* that happened to trigger the bug (walking into the pursuer), while this
        // asserts the guard directly. It cannot stop working because the Dream changed shape.
        EventBus.Instance.EmitRoomEntered(PocketDoorRoomId);
        Check("a stale RoomEntered does not repaint the view over where you actually are",
            _view == null || _view.DisplayedRoomId == "verify_wilds");
        Check("nor rename the room in the HUD",
            _hud == null || _hud.DisplayedRoomName == "Open Ground");

        // -- The artifact is broken: the entrance is spent --
        Check("a finished pocket cannot be entered again",
            pockets.InspectEnter(PocketKnowledge) == PocketBlock.AlreadyTaken);
        Check("and attempting it does nothing", !pockets.Enter(PocketKnowledge) && !pockets.InPocket);

        // -- And the grant cannot land twice --
        // Reachable on purpose. `Enter` already refuses a taken pocket, so `Complete`'s own guard would be a
        // dead latch with no probe — the mistake this codebase removed once already. The gate reaches it the
        // way a buggy pocket scene would: be inside, with the flag set, and call `Complete` again.
        var maxAfterOne = resolve.Max;
        world.SetFlag(Flags.PocketCleared(PocketKnowledge), false);
        Check("re-opened for the double-grant probe", pockets.Enter(PocketKnowledge));
        world.SetFlag(Flags.PocketCleared(PocketKnowledge));
        Check("completing a pocket whose flag is already set still returns you", pockets.Complete());
        Check("but grants nothing a second time — eight points is the whole budget",
            Mathf.IsEqualApprox(resolve.Max, maxAfterOne));

        // -- The page arrives with the knowledge, which is the whole of unit 7b --
        // Nothing watched this until now: the blueprint above is synthetic, so every completion in this file
        // took `GrantPage`'s *"no such item"* branch and the suite stayed green with the grant deleted. See
        // `PocketPage`'s remarks. The order below matters — the page must be shown to be a real registered
        // item **before** anything is phrased as an arrival, because `AddItem` accepts an id that resolves to
        // nothing and `Has` would then answer for a stack of a thing that does not exist.
        Check("the page this pocket teaches is an item on disk, not just a discovery key",
            inv.GetItem(PocketPageBlueprint)?.Category == ItemCategory.Lore);
        Check("and it is not in the satchel yet", !inv.Has(PocketPageBlueprint));
        Check("nor is the family known", !inv.IsDiscovered(PocketPageBlueprint));

        Check("crossing into the pocket that teaches it", pockets.Enter(PocketPage));
        Check("and finishing it reports that it did", pockets.Complete());
        Check("teaches the family", inv.IsDiscovered(PocketPageBlueprint));
        Check("and puts the page itself in the satchel, so the journal has something to show",
            inv.Has(PocketPageBlueprint));
        Check("exactly one of it, because a page is a record and not a stack",
            inv.GetCount(PocketPageBlueprint) == 1);

        // Taken back out: this is a real page, and one left behind is a material a later gate did not put
        // there. The discovery cannot be taken back — `InventoryCraftingManager` has no un-discover, by
        // design — so it rides until the next `ResetAll`, which is why this probe grants a *page* family and
        // not a tool one that opens a door.
        inv.RemoveItem(PocketPageBlueprint, 1);
        Check("and the satchel is put back the way it was found", !inv.Has(PocketPageBlueprint));

        // -- Leaving early writes nothing, which is the softlock rule --
        Check("a fresh pocket is enterable", pockets.Enter(PocketKnot));
        Check("leaving reports that it did", pockets.Leave());
        Check("and you are back outside",
            !pockets.InPocket && nav.CurrentRoomId == "verify_wilds");

        // The three claims that keep an ejection survivable. Nothing in the Dream can kill you, so leaving
        // early is a pocket's *only* failure — and if it broke the entrance, a bad night would permanently
        // delete a craft family and a point of max Resolve. A dead save reached by playing badly.
        Check("it wrote no flag", !world.GetFlag(Flags.PocketCleared(PocketKnot)));
        Check("it cut no knot", !world.GetFlag(Flags.KnotCut(PocketKnot)));
        Check("and the way in is still open, so an ejection costs only the trip",
            pockets.InspectEnter(PocketKnot) == PocketBlock.None);

        // -- The knot pocket's whole mechanical job --
        Check("crossing the knot pocket again", pockets.Enter(PocketKnot));
        Check("and finishing it cuts the knot",
            pockets.Complete() && world.GetFlag(Flags.KnotCut(PocketKnot)));

        // -- The boss pocket: the only way a tether is ever cut (invariant 4) --
        nav.GoToRoom(ArenaRoomId);
        states.SetState(ArenaTough, EntityState.Corrupted);
        Check("the body is anchored before the pocket",
            states.GetState(ArenaTough) == EntityState.Corrupted);
        Check("crossing into the boss pocket", pockets.Enter(PocketBoss));

        // The verb before the sever, because it is the half that had no field until 2026-08-04 and the half a
        // reader would assume rather than check. Both come from one act — pulling the anchor.
        Check("the verb is not held before the anchor is pulled", !PocketManager.Holds(DreamVerb.Float));
        Check("and finishing it severs the tether",
            pockets.Complete() && states.GetState(ArenaTough) == EntityState.Severed);
        Check("and teaches the verb the anchor was holding",
            PocketManager.Holds(DreamVerb.Float) &&
            world.GetFlag(Flags.VerbLearned(DreamVerb.Float)));
        Check("from the room the entrance was on", nav.CurrentRoomId == ArenaRoomId);

        // -- The overkill order: boss killed first, pocket taken after (2026-08-05) --
        //
        // **Found in play, and it resurrected a corpse.** Jeremy killed the Lost Trapper the hard way — no
        // sever, aura draining the whole fight — and then clicked the pocket entrance, which severed a body
        // that was already `Purified`. The severed form draws, so the boss came back without his aura.
        // `EncounterManager.Finish` has always *said* `Purified` is terminal; nothing made it so.
        //
        // **Both halves of Jeremy's ruling are asserted, because the fix could easily have taken the second
        // with it**: *"we want it so that if you do it that way that the form is gone. And you just complete
        // the 3D for the ability."* A guard that skipped the whole grant would have satisfied the first
        // sentence and silently deleted a Dream verb from that playthrough — and the verbs are a budget of
        // exactly two, so it would have been unrecoverable rather than merely wrong.
        world.SetFlag(Flags.PocketCleared(PocketBoss), false);
        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);
        states.SetState(ArenaTough, EntityState.Purified);

        Check("a body that already went down cannot be severed",
            !states.Sever(ArenaTough) && states.GetState(ArenaTough) == EntityState.Purified);

        Check("so taking its pocket afterwards leaves the corpse where it fell",
            pockets.Enter(PocketBoss) && pockets.Complete() &&
            states.GetState(ArenaTough) == EntityState.Purified);

        Check("and it still pays the verb, because the anchor was pulled either way",
            PocketManager.Holds(DreamVerb.Float) &&
            world.GetFlag(Flags.VerbLearned(DreamVerb.Float)) &&
            world.GetFlag(Flags.PocketCleared(PocketBoss)));

        // **Suppress is held from the first frame and carries no flag**, which is the one thing about the
        // verb model that a reader would get wrong. A flag nothing can fail to have is a flag every caller
        // has to remember to ignore, so geometry gated on Suppress is geometry gated on nothing.
        Check("Suppress is held from the start, with no flag behind it",
            PocketManager.Holds(DreamVerb.Suppress) &&
            !world.GetFlag(Flags.VerbLearned(DreamVerb.Suppress)));
        Check("and a verb nobody has been taught is not held",
            !PocketManager.Holds(DreamVerb.Flare));

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);

        // -- The three nodes a cloned pocket is built from --
        //
        // They exist so that fourteen cloned scenes carry no copies of the seam logic — cloning is precisely
        // how fourteen copies would drift, silently and identically, which the 59 generated rooms already
        // paid for. What is asserted here is the *logic*: the collision wiring and whether a scene actually
        // contains an exit are 3D authoring, in rule 7's unfalsifiable category.
        //
        // Built loose rather than added to the tree, deliberately. `Pull` and `Apply` are the whole
        // behaviour and neither needs `_Ready`, so a gate that parented them would be testing Godot's
        // signal plumbing instead of this code.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to reach an anchor in", pockets.Enter(PocketKnot));

        // Freed explicitly at the end of the block rather than queued: `QueueFree` defers to a frame that a
        // synchronous gate run never reaches, so these would be reported as leaked RIDs at exit — an error
        // line under a green run, which trains people to ignore the log.
        var anchor = new PocketAnchor();
        Check("pulling the anchor finishes the pocket", anchor.Pull() && !pockets.InPocket);
        Check("and it writes the flag the entrance reads",
            world.GetFlag(Flags.PocketCleared(PocketKnot)));

        // Once and once only. A body can re-enter an `Area3D` in the frame it left, and the second call
        // would print a second grant line — which reads as a bug even when nothing is wrong.
        Check("and it cannot be pulled twice", !anchor.Pull());

        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to walk out of", pockets.Enter(PocketKnot));
        var exit = new PocketExit();
        Check("the exit leaves without finishing", exit.Leave() && !pockets.InPocket);
        Check("and it wrote nothing, so the entrance survives",
            !world.GetFlag(Flags.PocketCleared(PocketKnot)) &&
            pockets.InspectEnter(PocketKnot) == PocketBlock.None);

        // The verb gate, both ways round. `Suppress` is the case a scene would get wrong — held from the
        // first frame with no flag behind it — so it is checked as the always-open one.
        var gated = new Node3D();
        var gate = new PocketGate { Requires = DreamVerb.Float };
        gated.AddChild(gate);

        gate.Apply();
        Check("a Float gate is shut before the verb is learned", !gate.IsOpen && !gated.Visible);

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float));
        gate.Apply();
        Check("and open once it is", gate.IsOpen && gated.Visible);

        gate.Inverted = true;
        gate.Apply();
        Check("inverted, it is the piece the verb takes away", !gate.IsOpen);

        gate.Inverted = false;
        gate.Requires = DreamVerb.Suppress;
        gate.Apply();
        Check("a Suppress gate is always open, because that verb needs no flag", gate.IsOpen);

        gate.Requires = DreamVerb.None;
        gate.Apply();
        Check("and an unset gate lets the player through rather than walling off a pocket", gate.IsOpen);

        // -- The collision half, on the type that made it a no-op --
        //
        // **Everything above this checks `Visible` and nothing checked collision, which is how the bold warning
        // on `SetCollision` came to be violated by `SetCollision`.** It walked for `CollisionObject3D`;
        // `CsgShape3D` derives from `GeometryInstance3D` and carries `UseCollision` with its own layer, so a
        // blockout ledge was skipped entirely — invisible on a shut gate and still solid. **And that is the only
        // kind of geometry a pocket blockout is**, so the one case in the sandbox was the one case the walk
        // could not see. A fixture in the tree, because the write is deferred and has to be flushed to be read.
        var csgGated = new CsgBox3D { UseCollision = true, CollisionLayer = 1, CollisionMask = 1 };
        var csgGate = new PocketGate { Requires = DreamVerb.Float };
        csgGated.AddChild(csgGate);
        (Engine.GetMainLoop() as SceneTree)?.Root.AddChild(csgGated);

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);
        csgGate.Apply();
        Check("a shut gate takes a CSG blockout's collision away and not just its mesh, "
            + $"so you cannot stand on it (layer {csgGated.CollisionLayer}, visible {csgGated.Visible})",
            !csgGated.Visible && csgGated.CollisionLayer == 0 && csgGated.CollisionMask == 0);

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float));
        csgGate.Apply();
        Check("and opening it hands the collision back rather than leaving the ledge a ghost",
            csgGated.Visible && csgGated.CollisionLayer == 1 && csgGated.CollisionMask == 1);

        (Engine.GetMainLoop() as SceneTree)?.Root.RemoveChild(csgGated);
        csgGated.Free();

        // The branch that was always there, now that the writes are direct and can actually be read back. It is
        // checked for the same reason the CSG one is: **an unverifiable half of a method is where a missing
        // half hides**, and this one guarded the trap the class doc-comment leads with.
        var bodyGated = new StaticBody3D { CollisionLayer = 1, CollisionMask = 1 };
        var bodyGate = new PocketGate { Requires = DreamVerb.Float };
        bodyGated.AddChild(bodyGate);

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);
        bodyGate.Apply();
        Check("a shut gate does the same to a StaticBody3D, and stops it processing",
            bodyGated.CollisionLayer == 0 && bodyGated.CollisionMask == 0
            && bodyGated.ProcessMode == Node.ProcessModeEnum.Disabled);

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float));
        bodyGate.Apply();
        Check("and gives both back when it opens",
            bodyGated.CollisionLayer == 1 && bodyGated.ProcessMode == Node.ProcessModeEnum.Inherit);

        bodyGated.Free();

        // -- Granting a verb *after* a gate has read, which is what both debug paths do --
        //
        // **The reported symptom, gated: "the HighLedge in the scene is invisible".** `PocketGate` reads on tree
        // entry and that is right — in the real game a verb comes from an anchor and an anchor ends the pocket,
        // so nothing can be learned while one is open. `F11` and `PocketStandalone` are both exceptions, and
        // `PocketStandalone` deferred its grant by a frame *specifically* so the gates would read first, on the
        // belief that this avoided a race. It does not avoid it; it loses it, on every run.
        var late = new Node3D();
        var lateGate = new PocketGate { Requires = DreamVerb.Float };
        late.AddChild(lateGate);

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);
        lateGate.Apply();
        Check("a gate that read before the grant is shut", !lateGate.IsOpen && !late.Visible);

        // The middle claim is the one that matters, and it asserts the *absence* of magic: setting the flag is
        // not enough on its own, so anything granting out of band owes the re-read. Without this line the check
        // below could pass because gates poll, which they deliberately do not.
        world.SetFlag(Flags.VerbLearned(DreamVerb.Float));
        Check("and stays shut on the grant alone, because gates do not poll for a change that cannot happen",
            !lateGate.IsOpen && !late.Visible);

        Check("re-reading the subtree is what opens it, and it finds every gate in there",
            PocketGate.ReapplyAll(late) == 1 && lateGate.IsOpen && late.Visible);

        // -- The other half of that deferral: the grant landing after the scene is gone --
        //
        // **Found by the gate suite's own output rather than by a check**, which is the only reason it is here:
        // one `ERROR: Parameter "data.tree" is null` printed after the last line of the run, with a cheerful
        // *"running 'dream_probe' as a pocket"* under it. `_Ready` queues `Engage` a frame ahead, so the sweep
        // further down — which builds a pocket scene, drives it and frees it inside one frame — left the call to
        // fire on a detached node. It entered a pocket from geometry that no longer existed and then asked a
        // freed tree for its root. Harmless in play, where nothing tears a pocket down that fast, and exactly the
        // kind of noise that trains you to read past errors in a suite that is meant to be silent.
        //
        // The fixture is a bare `Node` wearing a real `ScenePath` so the helper can resolve a pocket from it.
        // **Without that the check would pass on the "cannot tell which scene this is" warning instead** and
        // prove nothing — the tree check has to be the only thing left standing between it and entry.
        var scenedPocket = pockets.Pockets.Values.FirstOrDefault(p => p.HasScene);
        if (scenedPocket != null)
        {
            var orphanHolder = new Node { SceneFilePath = scenedPocket.ScenePath };
            var orphan = new PocketStandalone();
            orphanHolder.AddChild(orphan);

            world.SetFlag(Flags.PocketCleared(scenedPocket.Id), false);
            Check($"'{scenedPocket.Id}' would take a standalone run as things stand, so the refusal below is "
                + "about the tree and not about the pocket",
                !PocketManager.InsideTheDream && pockets.InspectEnter(scenedPocket.Id) == PocketBlock.None);

            orphan.Engage();
            Check("a helper that has left the tree does not cross you into a pocket built from geometry that "
                + "has been freed", !orphan.Engaged && !PocketManager.InsideTheDream);

            orphanHolder.Free();
        }

        anchor.Free();
        exit.Free();
        gated.Free();
        late.Free();
        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);

        // -- Refused inside a fight, and the fight survives the refusal --
        // Not tidiness: a pocket is not a room, so entering does **not** navigate, so
        // `EncounterManager.OnRoomEntered` — the backstop that stops a fight outliving its screen — never
        // fires. Without this guard you could tour a 3D scene and come back still committed. Unit 3a's
        // defect in a new costume, and the pair is what proves the guard rather than the backstop.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        states.SetState(ArenaCut, EntityState.Corrupted);
        Check("a fight is running for the refusal to be about", encounters.Engage(ArenaCut));
        Check("a pocket refuses to open inside a fight",
            pockets.InspectEnter(PocketKnot) == PocketBlock.InFight);
        Check("attempting it leaves you in the room",
            !pockets.Enter(PocketKnot) && !pockets.InPocket && nav.CurrentRoomId == ArenaRoomId);

        // **A check reading *"and the fight you were in is still running"* stood here and was deleted by its
        // own break-test — the second dud in two units, and this one has a sharper lesson than the first.**
        // It was copied from the camp verb, where it is genuinely load-bearing: travel *navigates*, so a
        // broken guard would trip `EncounterManager.OnRoomEntered` and end the fight, making a one-sided
        // check pass over a real defect. Entering a pocket does **not** navigate — which is the whole reason
        // the guard exists — so the fight survives whether the guard is there or not, and the sentence that
        // was a probe over there measures nothing here.
        //
        // The general form is worth more than the deletion: **a check is not portable just because its words
        // are true in both places.** What made it a probe was the navigation, not the phrasing. The harm this
        // guard actually prevents — being in a 3D scene still committed to a creature — is caught by the
        // *"leaves you in the room"* line above, which asserts you never got in.
        encounters.Flee();

        // -- A pocket that does not exist --
        Check("an unregistered id is a content error, not a player state",
            pockets.InspectEnter("no_such_pocket_anywhere") == PocketBlock.NoSuchPocket);
        Check("and entering it does nothing", !pockets.Enter("no_such_pocket_anywhere"));

        // -- Running out of nerve: one path for both bars (unit 8) --
        //
        // Ruled 2026-08-04: *"we should use the same function if VIT or Resolve hit 0 that the player goes to
        // the most recent camp."* This is the Resolve half, and it **revises `combat.md` §6**, which had an
        // empty bar in a boss fight mean forced flight instead.
        //
        // Driven by emptying the bar rather than by calling `Eject` directly, because the whole point is the
        // chain: `ResolveManager.Deplete` → `ResolveDepleted` → `CampManager` → eject, then respawn. Unit 6b
        // deleted the only listener there was, so for one unit an empty nerve did nothing at all.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        nav.GoToRoom("verify_camp");     // so the return point is bound and known
        nav.GoToRoom("verify_wilds");
        inv.ResetAll();
        inv.AddItem("herb_graveflower", 3);

        Check("a pocket to run out of nerve in", pockets.Enter(PocketKnot));
        resolve.SetTo(1f);
        resolve.SetTo(0f);

        Check("emptying the nerve throws you out of the pocket", !pockets.InPocket);
        Check("and puts you at the last camp, exactly as an empty body does",
            nav.CurrentRoomId == "verify_camp");
        Check("with the nerve back, because arriving at a camp refills both",
            Mathf.IsEqualApprox(resolve.Current, resolve.Max));

        // The softlock rule, on the path most likely to break it. A banish and running out are the same
        // call, so this covers both.
        Check("the pocket was not spent by being thrown out of",
            !world.GetFlag(Flags.PocketCleared(PocketKnot)) &&
            pockets.InspectEnter(PocketKnot) == PocketBlock.None);
        Check("and it took nothing off you on the way",
            inv.GetCount("herb_graveflower") == 3);

        // Invariant 2 from the other side, and it is why `RespawnAtCamp`'s guard had to change *meaning*
        // rather than be deleted: the guard reads `InsideTheDream`, and an ejection is inside the Dream by
        // definition. It works because the eject happens first — so if this ever goes red, the order slipped.
        Check("and the realm came back with you rather than stranding you in the Dream",
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);

        inv.ResetAll();

        // -- The hotspot, which is the only thing the player actually touches --
        //
        // Everything above drives `PocketManager` directly, and none of it would notice if
        // `InteractionService`'s `CrossToDream` arm were wired to the wrong manager or to nothing. That gap
        // was found by reading this section's own output rather than by a failing check, which is the
        // recurring shape: a section built around one seam tests the seam and not the door into it.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        nav.GoToRoom(PocketDoorRoomId);

        var entrance = Hotspot(PocketDoorId);
        var remnant = Hotspot(PocketRemnantId);

        Check("the entrance is there before it is used",
            entrance != null && InteractionService.IsAvailable(entrance));
        Check("and the broken remnant is not, because nothing is broken yet",
            remnant != null && !InteractionService.IsAvailable(remnant));

        // ---- One door, two destinations (unit 10, 2026-08-05) ----
        //
        // `InteractionService` routes a `CrossToDream` click on whether the pocket has a scene, so **the same
        // hotspot is driven twice** with the fixture's `ScenePath` set and then blank. One door rather than two
        // is the point: it is the *pocket* that decides, never the hotspot, so a screen authored today needs no
        // editing the day its Dream level is built.
        //
        // The scened arm runs **first** so the fixture ends this block back in its documented blank state —
        // every guard after it, and `PocketHost`'s own contract, depend on a scene-less pocket being enterable.
        knot.ScenePath = ProbeScenePath;

        Check("a pocket with a scene refuses to hand its reward over from out here",
            pockets.InspectTakeFromWaking(PocketKnot) == PocketBlock.MustBeWalked);
        Check("clicking the entrance crosses you over",
            InteractionService.ActivateHotspot(entrance) && pockets.InPocket &&
            pockets.CurrentPocketId == PocketKnot);
        Check("and finishing puts you back on the screen the door was on",
            pockets.Complete() && nav.CurrentRoomId == PocketDoorRoomId);

        // **The artifact breaks, proved where the player would see it.** Two authored hotspots on one flag
        // with opposite `RequiredFlagValue`s — the freed-NPC pattern — so the entrance becomes the remnant
        // with no new field and no code that knows about either.
        Check("the entrance is gone once the pocket is taken",
            !InteractionService.IsAvailable(entrance));
        Check("and the broken thing it left behind is what is there instead",
            InteractionService.IsAvailable(remnant));
        Check("clicking the spent entrance crosses nobody over",
            !InteractionService.ActivateHotspot(entrance) && !pockets.InPocket);

        // ---- The stand-in: the same door, on a pocket whose scene does not exist yet ----
        //
        // **This is the arm the 2D game is played through until the 3D pass lands**, and the claim worth
        // proving is not that it grants — it is that *everything else is identical*. The flag, the broken
        // entrance and the remnant are what the rest of the game reads, so if those match, a pocket getting
        // its scene later changes nothing outside the pocket.
        knot.ScenePath = "";
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        world.SetFlag(Flags.KnotCut(PocketKnot), false);

        Check("with no scene yet, the door is still open and the remnant still hidden",
            InteractionService.IsAvailable(entrance) && !InteractionService.IsAvailable(remnant));
        Check("and now it is the *taking* that is offered rather than the walking",
            pockets.InspectTakeFromWaking(PocketKnot) == PocketBlock.None);

        Check("clicking it grants what the pocket holds without crossing you over",
            InteractionService.ActivateHotspot(entrance) && !pockets.InPocket);
        Check("you never left the screen",
            nav.CurrentRoomId == PocketDoorRoomId &&
            RealmTransitionManager.Instance.CurrentRealm == GameRealm.Waking);
        Check("the knot is cut all the same, which is the whole reward of this one",
            world.GetFlag(Flags.KnotCut(PocketKnot)));

        // The three facts the rest of the game actually reads, asserted against the stand-in rather than
        // against a crossing — so *identical downstream* is a measured claim and not a hope.
        Check("the pocket counts as cleared, exactly as finishing it would",
            world.GetFlag(Flags.PocketCleared(PocketKnot)));
        Check("so the entrance breaks the same way",
            !InteractionService.IsAvailable(entrance));
        Check("and leaves the same remnant behind",
            InteractionService.IsAvailable(remnant));

        // **What stops a second grant on this path, measured where it actually happens — and the first version
        // of these two checks was written the wrong way round.** It read *"clicking the spent entrance a second
        // time grants nothing again"* and stayed **green with `GrantOnce`'s flag guard deleted**, because the
        // spent entrance is no longer `IsAvailable`, so the click never reaches the manager at all: §3's seventh
        // class, a check passing for a reason other than the one it names. The break-test found it by reddening
        // only the pre-existing `Complete` check and not this one.
        //
        // So they are split into the two claims that are really here, in the order the refusals fire:
        world.SetFlag(Flags.KnotCut(PocketKnot), false);
        Check("the spent entrance is inert, so a second click cannot even reach the grant",
            !InteractionService.ActivateHotspot(entrance) &&
            !world.GetFlag(Flags.KnotCut(PocketKnot)));
        Check("and asking the manager directly is refused as already taken, which is what guards the grant here",
            pockets.InspectTakeFromWaking(PocketKnot) == PocketBlock.AlreadyTaken &&
            !pockets.TakeFromWaking(PocketKnot) &&
            !world.GetFlag(Flags.KnotCut(PocketKnot)));

        // **`GrantOnce`'s own flag guard is belt to that brace on this path, and saying so is more useful than
        // pretending otherwise.** `AlreadyTaken` above is what a second click meets; the guard inside
        // `GrantOnce` is only reachable through `Complete`, where the *"grants nothing a second time"* check has
        // covered it since unit 6a — which is exactly the check the break-test reddened. Two callers, one
        // once-only rule, and each caller's route to it is asserted where that route exists.

        // -- What the stand-in *says*, which is the only thing 13 of the 14 pockets ever show the player --
        //
        // **Found by break-test on 2026-08-06: `PocketManager.Describe` could return `"nothing"` for every
        // pocket in the game and all 1101 checks stayed green.** It is named nowhere in this file, and the
        // notification it composes is the entire reward feedback on the taking path — no 3D scene runs, so a
        // click on an entrance produces one line of text and that line is the whole event. A player taking the
        // Trapper's Tether would have been told they received nothing, and the suite would have agreed.
        //
        // **Asserted over the authored pockets rather than the fixture**, deliberately. A fixture proves the
        // method concatenates; what matters is that each of the fourteen has something to say and that every
        // arm of the sentence is reachable from content that exists — the fixture knot pocket exercises one
        // arm and would have left the other four able to be deleted in silence.
        var described = (PocketManager.Instance?.Pockets ?? new Dictionary<string, PocketData>())
            .Where(kv => !kv.Key.StartsWith("verify_") && kv.Key != ContentReport.ReferencePocketId)
            .Where(kv => kv.Value != null)
            .ToList();

        // A local rather than five copies of the same shape. Each arm collects the pockets whose line fails to
        // mention a grant they were authored with, so a failure names the pocket and not just the count.
        List<string> Silent(Func<PocketData, bool> carries, Func<PocketData, string> expected) => described
            .Where(kv => carries(kv.Value))
            .Where(kv => !PocketManager.Describe(kv.Value).Contains(expected(kv.Value)))
            .Select(kv => kv.Key)
            .OrderBy(s => s)
            .ToList();

        var mute = described
            .Where(kv => PocketManager.Describe(kv.Value) == "nothing")
            .Select(kv => kv.Key)
            .OrderBy(s => s)
            .ToList();

        Check($"every authored pocket can say what it gives you ({described.Count} pockets){Listing(mute)}",
            mute.Count == 0);

        // The page arm reads the catalogue the way the method does — the display name when the page is a real
        // item, the raw id when it is not — because asserting the *pretty* one would fail on a pocket whose page
        // is unauthored, which is a different defect with its own check in the `roster` section.
        var unnamedPage = Silent(
            p => !string.IsNullOrEmpty(p.GrantsBlueprintId),
            p => InventoryCraftingManager.Instance?.GetItem(p.GrantsBlueprintId)?.DisplayName
                 ?? p.GrantsBlueprintId);
        Check($"and the ones that teach name the page{Listing(unnamedPage)}", unnamedPage.Count == 0);

        var unnamedResolve = Silent(p => p.GrantsMaxResolve > 0, p => $"+{p.GrantsMaxResolve} max Resolve");
        Check($"and the ones that raise the ceiling say by how much{Listing(unnamedResolve)}",
            unnamedResolve.Count == 0);

        // Through `Words` rather than the enum name, because the sentence is written in the game's voice —
        // *"he can leave the ground"*, not *"Float"*. Reading it from the same map the method reads is the same
        // construction the page arm uses: what is asserted is that the verb is **mentioned at all**, which
        // deleting the arm breaks, and not a wording this file would then own a second copy of.
        var unnamedVerb = Silent(p => p.GrantsAbility != DreamVerb.None,
            p => PocketManager.Words(p.GrantsAbility));
        Check($"and the two that teach a verb name it{Listing(unnamedVerb)}", unnamedVerb.Count == 0);

        var unnamedKnot = Silent(p => p.Kind == PocketKind.Knot, _ => "knot");
        Check($"and the three that cut a knot say so{Listing(unnamedKnot)}", unnamedKnot.Count == 0);

        var unnamedTether = Silent(p => !string.IsNullOrEmpty(p.TargetEntityId), p => p.TargetEntityId);
        Check($"and the three that sever name whose tether it is{Listing(unnamedTether)}",
            unnamedTether.Count == 0);

        // -- The debug key's one judgement, which no check could reach until it was split out --
        //
        // **The same read-back, and this is the older and worse shape: not an unwatched guard but an
        // unwatchable one.** `F10` skipping scene-less pockets is the only thing between a press of that key
        // and a black screen with no `PocketExit` in it — thirteen of the fourteen authored pockets have no
        // geometry — and the skip lived inside a private handler that nothing but a keypress could call, so
        // deleting it would have changed no measurable behaviour anywhere. Unit 9a's `EndPlayerTurn` guard in a
        // different costume. `Main.DebugPocketToCrossInto` is the choice, extracted for exactly this.
        //
        // **The state is constructed rather than discovered**, which is §3's eighteenth class: whether some
        // earlier fixture happens to have spent the probe is not something this check should depend on, and a
        // check that silently goes vacuous when it does is worse than no check.
        var withScenes = pockets.Pockets
            .Where(kv => kv.Value is { HasScene: true })
            .Select(kv => kv.Key)
            .OrderBy(id => id)
            .ToList();

        var flagWas = withScenes.ToDictionary(id => id, id => world.GetFlag(Flags.PocketCleared(id)));
        foreach (var id in withScenes) world.SetFlag(Flags.PocketCleared(id), false);

        var picked = Main.DebugPocketToCrossInto(pockets);
        Check($"the debug crossing picks a pocket that has a scene "
            + $"({withScenes.Count} of {pockets.Pockets.Count} do) [{(picked == "" ? "none" : picked)}]",
            withScenes.Count > 0 && withScenes.Contains(picked));

        // The half that is the softlock. With every built pocket spent it must offer **nothing** — the failure
        // being guarded against is that it falls through to the first stand-in on the list, which is a room you
        // cannot walk out of because there is no room.
        foreach (var id in withScenes) world.SetFlag(Flags.PocketCleared(id), true);

        var fallback = Main.DebugPocketToCrossInto(pockets);
        Check($"and with those spent it offers nothing rather than a pocket with no way out "
            + $"[{(fallback == "" ? "none" : fallback)}]",
            fallback == "");

        foreach (var (id, was) in flagWas) world.SetFlag(Flags.PocketCleared(id), was);

        // -- What the verbs cost, and what a creature takes (unit 8) --
        //
        // §19: all three verbs drain **while held**, and they share one meter, so the rate is a **sum**. Taking
        // the maximum instead would delete the section's own sentence — *"floating spends the same meter
        // suppression does, so the climb is paid for out of the hiding"* — by letting you hide for free while
        // climbing.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        DreamVerbs.ReleaseAll();

        // Fills the bar without touching the ceiling, for the reason spelled out at the end of this block: a
        // knowledge pocket raised `Max` earlier in the section and the save round trip below reads it.
        resolve.SetTo(resolve.Max);

        Check("nothing drains while you are awake",
            Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));
        Check("and a verb cannot be held out here at all", !DreamVerbs.BeginHold(DreamVerb.Suppress));

        Check("a pocket to spend a verb in", pockets.Enter(PocketKnot));
        Check("Suppress can be held, because you have it from the first frame",
            DreamVerbs.BeginHold(DreamVerb.Suppress));
        Check("and it costs what it is priced at",
            Mathf.IsEqualApprox(resolve.CurrentRatePerSecond(), GameConfig.SuppressDrainPerSecond));

        Check("a verb you have not been taught cannot be held", !DreamVerbs.BeginHold(DreamVerb.Float));

        world.SetFlag(Flags.VerbLearned(DreamVerb.Float));
        Check("and can be the moment it is learned", DreamVerbs.BeginHold(DreamVerb.Float));
        Check("two verbs held cost the sum, not the larger",
            Mathf.IsEqualApprox(resolve.CurrentRatePerSecond(),
                GameConfig.SuppressDrainPerSecond + GameConfig.FloatDrainPerSecond));

        Check("letting one go stops paying for it",
            DreamVerbs.EndHold(DreamVerb.Float) &&
            Mathf.IsEqualApprox(resolve.CurrentRatePerSecond(), GameConfig.SuppressDrainPerSecond));

        // Every exit drops every hold. Without it a verb held as you are thrown out would keep draining in the
        // Waking world, which §6 rules out permanently — and `RatePerSecond` does not ask where you are.
        pockets.Leave();
        Check("leaving a pocket releases whatever was held",
            DreamVerbs.HeldVerbs.Count == 0 && Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));

        // -- The keys behind the verbs (`DreamVerbHold`) --
        //
        // Driven through `Poll()` rather than a frame, because `_Process` never ticks meaningfully in a headless
        // run — the seam `DebugRepaintNow` exists for.
        var verbInput = new DreamVerbHold();

        // Totality, and this pair is the shape that goes red on its own: add a fourth verb and forget its
        // action, or rename an action in `project.godot`, and one of these names the offender. The second is a
        // **crossing** — the node's table and the input map are two collections naming each other, which is the
        // check that never appears on its own.
        var keyless = System.Enum.GetValues<DreamVerb>()
            .Where(v => v != DreamVerb.None && !DreamVerbHold.Actions.ContainsKey(v))
            .Select(v => v.ToString()).ToList();
        Check($"every Dream verb has a key behind it{Listing(keyless)}", keyless.Count == 0);

        var unbound = DreamVerbHold.Actions.Values.Where(a => !InputMap.HasAction(a)).ToList();
        Check($"and the project binds every action the node reads{Listing(unbound)}", unbound.Count == 0);

        // -- The readout only claims the verb keys where something can answer them --
        //
        // **Reported as *"none of the abilities work anymore when I press F6"*, and the report was against the
        // look bench.** `DreamProbe.tscn` has no `PocketStandalone` and no poller — correctly, it is not a pocket
        // and no `PocketData` names it — so the keys did nothing, silently. What made that a defect rather than a
        // misunderstanding is that `DreamLookTuner`'s readout listed *"Q hide · Space float · E flare"* as static
        // text in every scene it is dropped into, and the HUD is the only evidence available while playing.
        // **A debug surface that lists a key it cannot deliver reads as a broken feature, not as the wrong
        // scene.**
        var benchLike = new Node();
        Check("a scene with nothing behind the verb keys is not called wired",
            !DreamLookTuner.VerbsWiredIn(benchLike));

        // Buried a level down, because a walk that only looked at direct children would pass a fixture built the
        // obvious way and miss every real scene — the poller hangs off the standalone, not off the root.
        var deep = new Node();
        var deepHolder = new Node();
        deep.AddChild(deepHolder);
        deepHolder.AddChild(new DreamVerbHold());
        Check("a poller anywhere in the scene is, at any depth", DreamLookTuner.VerbsWiredIn(deep));

        // The helper counts as well as the poller: it attaches one a frame later, and a label that told the truth
        // only from frame two would be wrong in exactly the moment it is first read.
        var helped = new Node();
        helped.AddChild(new PocketStandalone());
        Check("and so does the helper that brings one, so the label is right on the first frame too",
            DreamLookTuner.VerbsWiredIn(helped));

        benchLike.Free();
        deep.Free();
        helped.Free();

        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to press a key in", pockets.Enter(PocketKnot));
        resolve.SetTo(resolve.Max);

        // **The check that would have caught the defect this block was written after.** The poller comes from
        // `PocketHost`, not from the pocket scene, so a pocket nobody remembered to wire still has verbs — and
        // this fixture proves it on a pocket with no scene at all. Note the two halves are *composed* rather
        // than end-to-end: this says a poller is attached, and the checks below say a poller does the right
        // thing. Nothing here drives the attached one, because `_Process` does not tick headlessly.
        Check("and the host gave it a verb poller, with no scene authoring one",
            _host == null || _host.VerbInputLive);

        Input.ActionPress("dream_suppress");
        verbInput.Poll();
        Check("holding the key holds the verb", DreamVerbs.IsHolding(DreamVerb.Suppress));
        Check("and the meter is spent for it",
            Mathf.IsEqualApprox(resolve.CurrentRatePerSecond(), GameConfig.SuppressDrainPerSecond));

        Input.ActionRelease("dream_suppress");
        verbInput.Poll();
        Check("letting the key go ends the hold and the cost",
            !DreamVerbs.IsHolding(DreamVerb.Suppress) && Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));

        // Flare rather than Float, because Float was learned twenty lines above.
        Input.ActionPress("dream_flare");
        verbInput.Poll();
        Check("a key for a verb you have not been taught spends nothing",
            !DreamVerbs.IsHolding(DreamVerb.Flare) && Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));
        Input.ActionRelease("dream_flare");

        // The control for the check below, and the reason this is a pair: a refusal asserted where the thing
        // was never available is not a check (unit 6a's lesson). Same key, same verb, one realm apart.
        pockets.Leave();
        Check("and the poller goes away with the pocket", _host == null || !_host.VerbInputLive);

        Input.ActionPress("dream_suppress");
        verbInput.Poll();
        Check("and out here that same key on that same verb does nothing",
            !DreamVerbs.IsHolding(DreamVerb.Suppress) && Mathf.IsZeroApprox(resolve.CurrentRatePerSecond()));
        Input.ActionRelease("dream_suppress");

        // **Why the node keeps no set of its own, asserted by the one sequence that can tell the difference.**
        // `PocketManager.Return` calls `ReleaseAll` on the way out, so a private record of what was held would
        // survive that release still believing Suppress was down — and the key would be dead on the next
        // crossing until the player let go of a button they were already holding.
        //
        // **The key has to go down *inside* and never come up**, which is the whole content of this fixture: a
        // press that begins out here never enters a cached set either, so the obvious shorter version passes
        // against both implementations. Written the short way first and caught by planning its break-test.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to be holding a key inside of", pockets.Enter(PocketKnot));
        Input.ActionPress("dream_suppress");
        verbInput.Poll();
        Check("the key is down and the verb is held", DreamVerbs.IsHolding(DreamVerb.Suppress));

        pockets.Leave();
        Check("leaving drops the hold even with the key still down",
            !DreamVerbs.IsHolding(DreamVerb.Suppress));

        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("crossing back in, the key never having been released", pockets.Enter(PocketKnot));
        verbInput.Poll();
        Check("picks the hold up again rather than waiting for a release",
            DreamVerbs.IsHolding(DreamVerb.Suppress));

        Input.ActionRelease("dream_suppress");
        verbInput.Poll();
        pockets.Leave();
        verbInput.Free();

        // -- The author's flood light (`DreamAuthorLight`) --
        //
        // **Debug scaffolding, gated for one reason: it changes nodes it did not create.** Everything else in
        // the Dream owns its own state, so leaving it half-applied is not possible; this reaches into a
        // `WorldEnvironment` and a `SubViewportContainer` that belong to an authored pocket and puts them
        // back afterwards. That is the eighth blind-spot class by construction — a half-applied arrangement
        // that looks applied — and the failure would be silent and permanent-looking: a pocket that stays
        // washed out and un-posterised until you notice the *look* is gone rather than the light being on.
        //
        // Driven on a synthetic tree rather than a pocket scene, because a headless run has no geometry at
        // all and the thirteen scene-less stand-ins never will. What is being checked is the restore, and the
        // restore does not care whose nodes they are.
        // **The fixture is parented into the live scene tree, and that is not tidiness.** Written first as a
        // detached `new Node()` with children hung off it, the restore-on-exit check below failed — because
        // `_ExitTree` never fires for a node that was never *in* the tree, so `RemoveChild` on an orphan runs
        // no teardown at all. The code was right and the fixture was measuring the wrong thing. Same family as
        // unit 5's check that stayed green with its guard removed, caught here only because this one went red.
        var floodRoot = new Node();
        var floodTree = Engine.GetMainLoop() as SceneTree;
        Check("a live scene tree to hang the fixture in, or its teardown is untestable", floodTree != null);
        floodTree?.Root.AddChild(floodRoot);

        var floodEnv = new WorldEnvironment { Environment = new Godot.Environment { AmbientLightEnergy = 0.04f } };
        var floodLook = new ShaderMaterial();
        var floodContainer = new SubViewportContainer { Material = floodLook };
        floodRoot.AddChild(floodEnv);
        floodRoot.AddChild(floodContainer);

        var flood = new DreamAuthorLight();
        floodRoot.AddChild(flood);

        Check("the flood starts down", !flood.IsFlooded);

        flood.Toggle();
        Check("pressing it lights the pocket at once",
            flood.IsFlooded && floodEnv.Environment.AmbientLightEnergy > 1f);

        // **The half that makes it useful, and the half most likely to be dropped as an optimisation.** Ambient
        // alone hands a bright room to `DreamLook`, which posterises it into a few greys under a clamp — so a
        // flooded pocket comes out one flat value and the author can see *less* than by lamplight.
        Check("and takes the monochrome off, or flooding would make it less legible rather than more",
            floodContainer.Material == null);

        flood.Toggle();
        Check("pressing it again puts the ambient back exactly",
            !flood.IsFlooded && Mathf.IsEqualApprox(floodEnv.Environment.AmbientLightEnergy, 0.04f));

        // Reference equality on purpose: the look must be *the same* material, not an equivalent one.
        // `DreamLook.tres` is shared across every pocket, so a flood that rebuilt it would be a debug key
        // quietly forking the game's art.
        Check("and the same look material, not a replacement",
            ReferenceEquals(floodContainer.Material, floodLook));

        // Leaving while it is up is the realistic mistake — F9 on, then walk out — and the restore has to be
        // driven by the node going away rather than by the author remembering.
        flood.Toggle();
        Check("flooded again, to leave while it is up", flood.IsFlooded);
        floodRoot.RemoveChild(flood);
        Check("the light going away restores what it changed, with no second press",
            Mathf.IsEqualApprox(floodEnv.Environment.AmbientLightEnergy, 0.04f)
            && ReferenceEquals(floodContainer.Material, floodLook));

        // **Two lights in one tree, because there are two ways to look at a pocket.** Crossing in from the
        // game gets one from `PocketHost`; pressing F6 on the scene — the way a pocket is actually authored —
        // has no host, so that workflow wants one in the scene, and a pocket that carries one will then have
        // both the moment it is crossed into. Both polling would toggle twice in a frame and F9 would read as
        // dead, which is a silent failure in the one workflow this feature exists for.
        // Back into the tree: the check above took it out, which released the claim along with the restore.
        floodRoot.AddChild(flood);

        var second = new DreamAuthorLight();
        floodRoot.AddChild(second);
        Check("with two lights in the tree, the first one in holds the key",
            flood.OwnsTheKey && !second.OwnsTheKey);

        // **The key is actually pressed here, and it has to be.** Polling both with no key down would leave
        // both dark whether the claim was read or not — a universal over an empty set, which unit 4b already
        // paid for once. Writing that check is what turned the toggle from a raw key into an action: a
        // *physical* key cannot be put down from code at all (`ParseInputEvent` does not move what
        // `IsPhysicalKeyPressed` reports), so the guard underneath it was unmeasurable by construction.
        Check("the key behind the flood is bound", InputMap.HasAction(DreamAuthorLight.ToggleAction));

        Input.ActionPress(DreamAuthorLight.ToggleAction);
        second.Poll();
        flood.Poll();
        Input.ActionRelease(DreamAuthorLight.ToggleAction);
        second.Poll();
        flood.Poll();

        Check("pressing the key reaches the holder", flood.IsFlooded);
        Check("and not the spare, which would toggle it straight back and read as a dead key",
            !second.IsFlooded);

        flood.Toggle();

        flood.Free();
        second.Poll();
        Check("the key passes on when the holder goes, rather than dying with it", second.OwnsTheKey);

        floodTree?.Root.RemoveChild(floodRoot);
        floodRoot.Free();

        // The host attaches it for the same reason it attaches the poller: a thing every pocket must have,
        // that takes no configuration and has no position, is wiring rather than authoring — and this fixture
        // proves it on a pocket with no scene to have wired one in.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to author by", pockets.Enter(PocketKnot));
        Check("and the host gave it a flood light, with no scene authoring one",
            _host == null || _host.AuthorLight != null);

        pockets.Leave();
        Check("which goes away with the pocket", _host == null || _host.AuthorLight == null);

        // -- The boundary rig (`PocketBounds`) --
        //
        // The same three jobs in all fourteen pockets from one number, so the thing worth gating is that the
        // number means what it says: the walls stand **outside** the clear volume. Half a thickness the wrong
        // way and every pocket is quietly a metre smaller than it was authored, along all four sides, with no
        // symptom but geometry poking through a wall you cannot see.
        var bounds = new PocketBounds { Size = new Vector3(20.0f, 8.0f, 30.0f), Thickness = 2.0f };
        floodTree?.Root.AddChild(bounds);

        Check("four walls and a lid", bounds.ShapeCount == 5);

        // A shade inside each face, and a shade outside it. The pair is the check — *inside is clear* alone
        // passes for a rig that built nothing at all.
        var justInside = new[]
        {
            new Vector3(9.9f, 4.0f, 0.0f), new Vector3(-9.9f, 4.0f, 0.0f),
            new Vector3(0.0f, 4.0f, 14.9f), new Vector3(0.0f, 4.0f, -14.9f),
            new Vector3(0.0f, 7.9f, 0.0f), new Vector3(0.0f, 0.1f, 0.0f),
        };
        var justOutside = new[]
        {
            new Vector3(10.1f, 4.0f, 0.0f), new Vector3(-10.1f, 4.0f, 0.0f),
            new Vector3(0.0f, 4.0f, 15.1f), new Vector3(0.0f, 4.0f, -15.1f),
            new Vector3(0.0f, 8.1f, 0.0f),
        };

        Check("the volume you typed is the volume you get, right up to each face",
            justInside.All(p => bounds.Contains(p) && !bounds.Blocks(p)));
        Check("and a step past any face is solid, including straight up, which is what stops Float",
            justOutside.All(p => bounds.Blocks(p)));

        // **The floor is deliberately open**, so a seam in the authored floor drops you rather than being
        // silently patched — which is the whole reason the catch volume (C5) is a separate, deferred thing.
        Check("the floor is left open on purpose", !bounds.Blocks(new Vector3(0.0f, -0.1f, 0.0f)));

        // Doubling is invisible when it happens — the second set of walls sits exactly on the first.
        bounds.Rebuild();
        bounds.Rebuild();
        Check("rebuilding replaces the box rather than stacking another inside it", bounds.ShapeCount == 5);

        bounds.BuildCap = false;
        bounds.Rebuild();
        Check("and the lid can be taken off for a pocket with a real ceiling", bounds.ShapeCount == 4
            && !bounds.Blocks(new Vector3(0.0f, 8.1f, 0.0f)));

        // **The border is drawn where the wall is, which is the whole reason it replaced fog.** Depth fog
        // travels with the camera and cannot mark anything fixed; a mesh parented to the collision shape is
        // at the boundary by construction and cannot drift from it.
        Check("no shell until it is asked for", !bounds.ShellFaces.Any());

        bounds.BuildCap = true;
        bounds.BorderShell = true;
        bounds.BorderValue = 0.55f;
        bounds.Rebuild();

        var faces = bounds.ShellFaces.ToList();
        Check($"asked for, every wall is drawn and none is drawn twice ({faces.Count} of {bounds.ShapeCount})",
            faces.Count == bounds.ShapeCount && bounds.ShapeCount == 5);

        // **Unshaded, or the border is black.** A lit surface past the lamp's radius shows nothing at all,
        // which is exactly the "boundary you cannot see" this exists to fix — so the shading mode is the
        // feature, not a detail of it.
        Check("and drawn unshaded, so it reads out past the lamp instead of going black",
            faces.All(f => f.MaterialOverride is StandardMaterial3D
            {
                ShadingMode: BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode: BaseMaterial3D.CullModeEnum.Disabled,
            }));

        // Grey, for the same reason the fog had to be: chroma is the signal channel.
        Check("and in grey, because a tinted border would be promoted to the objective white",
            faces.All(f => f.MaterialOverride is StandardMaterial3D m
                && Mathf.IsEqualApprox(m.AlbedoColor.R, m.AlbedoColor.G)
                && Mathf.IsEqualApprox(m.AlbedoColor.G, m.AlbedoColor.B)));

        floodTree?.Root.RemoveChild(bounds);
        bounds.Free();

        // -- The Dream's shared environment, and the one way fog can go badly wrong here --
        //
        // `DreamLook.tres`'s argument applied to the atmosphere: an `Environment` embedded per scene is
        // fourteen copies that drift, so there is one file and every pocket points at it.
        var dreamEnv = ResourceLoader.Load<Godot.Environment>("res://scenes/dream/DreamEnvironment.tres");
        Check("the Dream has one shared environment", dreamEnv != null);

        if (dreamEnv != null)
        {
            // **Fog is off, and that is a finding rather than a default.** It was turned on to mark the
            // border and cannot: depth fog measures distance from the *camera*, so it travels with the player
            // — always clear for the first `begin` metres in every direction, wherever you stand. `FogVolume`,
            // which would place fog in a box, is Forward+ only and this project runs `gl_compatibility`.
            // The border is `PocketBounds.BorderShell` now, which is geometry and therefore stays put.
            //
            // This is not asserted either way: fog as *atmosphere* is a live option and turning it back on
            // should not redden a suite. What is asserted is the trap underneath it.
            var fog = dreamEnv.FogLightColor;
            var chroma = Mathf.Max(Mathf.Max(fog.R, fog.G), fog.B) - Mathf.Min(Mathf.Min(fog.R, fog.G), fog.B);

            // **`DreamLook.gdshader` promotes anything with chroma at or above `signal_chroma` to pure
            // white** — the one visual reserved for *go here*. A tinted fog becomes a glowing wall the player
            // reads as the objective. Godot's own default fog colour is bluish enough to trip this, which is
            // what makes it a real check rather than a restatement of the file.
            Check($"the Dream's fog has no colour in it, on or off (chroma {chroma:0.000}, "
                + $"enabled {dreamEnv.FogEnabled}, mode {dreamEnv.FogMode}, "
                + $"{dreamEnv.FogDepthBegin:0.#}→{dreamEnv.FogDepthEnd:0.#})",
                chroma < 0.04f);

            // Reversed by a typo is silent — the fog simply never appears — and it stays worth checking while
            // the values sit there waiting to be switched on.
            Check("and a depth band that runs outward rather than inward",
                dreamEnv.FogDepthEnd > dreamEnv.FogDepthBegin);
        }

        // -- Suppress actually puts the lamp out (`DreamSuppressibleLight`) --
        //
        // The cost has been gated since unit 8 and the *act* had nothing over it, which is the shape of gap
        // that leaves a verb feeling unbuilt while every check about it is green.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to put the lamp out in", pockets.Enter(PocketKnot));
        resolve.SetTo(resolve.Max);

        var lamp = new DreamSuppressibleLight { OmniRange = 6.0f, FadeSeconds = 0.2f, SuppressedFraction = 0.0f };
        floodTree?.Root.AddChild(lamp);

        // **The tuner keeps the lamp while you are lit, and this is the check that says so.** A node driving
        // the radius every frame would stomp `,` / `.` on the frame after each nudge — the lamp would spring
        // back and the keys would read as broken, with nothing anywhere to explain it.
        lamp.OmniRange = 9.0f;
        lamp.Tick(1.0);
        Check("while you are lit it does not touch the radius, so the tuner still owns it",
            Mathf.IsEqualApprox(lamp.OmniRange, 9.0f) && Mathf.IsEqualApprox(lamp.BaseRange, 9.0f));

        Check("holding the verb", DreamVerbs.BeginHold(DreamVerb.Suppress));
        lamp.Tick(1.0);
        Check("puts the lamp out", Mathf.IsZeroApprox(lamp.OmniRange) && Mathf.IsZeroApprox(lamp.Factor));

        DreamVerbs.EndHold(DreamVerb.Suppress);
        lamp.Tick(1.0);
        Check("and letting go brings back the radius it was tuned to, not the one it was built with",
            Mathf.IsEqualApprox(lamp.OmniRange, 9.0f));

        // **The 100%-or-90% question, kept as a number.** Whether hiding should be total or leave a stub you
        // can still read the floor by is a feel question that play settles, so it is one authored field —
        // and this asserts the field is really consulted rather than being a slider wired to nothing.
        lamp.SuppressedFraction = 0.25f;
        DreamVerbs.BeginHold(DreamVerb.Suppress);
        lamp.Tick(1.0);
        Check("and how much lamp is left while hiding is authored, not fixed at nothing",
            Mathf.IsEqualApprox(lamp.OmniRange, 9.0f * 0.25f));

        DreamVerbs.EndHold(DreamVerb.Suppress);
        lamp.Tick(1.0);

        // **Flare is the mirror, on the same channel.** §19 calls it *"the exact mirror of Suppress"*, and
        // once Suppress owned the radius the mirror was a multiplier rather than a second mechanism. This is
        // the light only — revealing geometry and being seen are a gate variant and creature AI, neither
        // written, which is worth a check saying what *is* here rather than assuming the verb is done.
        world.SetFlag(Flags.VerbLearned(DreamVerb.Flare), true);
        lamp.FlaredMultiple = 3.0f;
        Check("holding Flare", DreamVerbs.BeginHold(DreamVerb.Flare));
        lamp.Tick(4.0);
        Check("throws the lamp wide instead of putting it out",
            Mathf.IsEqualApprox(lamp.OmniRange, 9.0f * 3.0f));

        // **Both at once is a contradiction the player can pay for, so it needs a settled answer.** The rates
        // are additive, so holding Q and E together drains 8 a second; going dark is the safe resolution,
        // because the alternative hands someone who fumbled a key a tripled lamp in a room that hunts light.
        // Asserted as *Suppress's own number* rather than as "dark", which is the stronger claim: the lamp
        // lands on the suppressed fraction still in force (0.25 from the check above), so this cannot pass by
        // the light happening to be off for some other reason.
        DreamVerbs.BeginHold(DreamVerb.Suppress);
        lamp.Tick(4.0);
        Check("and hiding beats flaring when both are held, rather than the louder one winning",
            Mathf.IsEqualApprox(lamp.OmniRange, 9.0f * 0.25f));
        Check("while paying for both, because the meter sums them",
            Mathf.IsEqualApprox(resolve.CurrentRatePerSecond(),
                GameConfig.SuppressDrainPerSecond + GameConfig.FlareDrainPerSecond));

        DreamVerbs.EndHold(DreamVerb.Suppress);
        DreamVerbs.EndHold(DreamVerb.Flare);
        world.SetFlag(Flags.VerbLearned(DreamVerb.Flare), false);
        floodTree?.Root.RemoveChild(lamp);
        lamp.Free();
        pockets.Leave();

        // -- The creature roster on disk --
        //
        // **The fifth blind-spot class, for the one collection that has no registry.** Rooms, items, recipes,
        // entities and pockets are each loaded by a manager, so an unparseable file shows up as a count that
        // does not match. A `DreamCreatureData` is reached through a `DreamCreature`'s `[Export]` and nothing
        // loads them all — so a file that fails to parse is a creature that silently costs nothing, which
        // `DreamCreature._Ready` already calls out as indistinguishable from one the player is avoiding.
        var creatureFiles = new List<string>();
        using (var creatureDir = DirAccess.Open(GameConfig.DreamCreatureDataDir))
        {
            if (creatureDir != null)
            {
                creatureDir.ListDirBegin();
                for (var f = creatureDir.GetNext(); f != ""; f = creatureDir.GetNext())
                    if (!creatureDir.CurrentIsDir() && (f.EndsWith(".tres") || f.EndsWith(".tres.remap")))
                        creatureFiles.Add($"{GameConfig.DreamCreatureDataDir}/{f.TrimSuffix(".remap")}");
            }
        }

        var creatures = creatureFiles
            .Select(path => ResourceLoader.Load<DreamCreatureData>(path))
            .Where(c => c != null)
            .ToList();

        Check($"every creature file on disk loaded ({creatures.Count} of {creatureFiles.Count})",
            creatureFiles.Count > 0 && creatures.Count == creatureFiles.Count);

        var namelessCreature = creatures
            .Where(c => string.IsNullOrEmpty(c.Id) || string.IsNullOrEmpty(c.DisplayName))
            .Select(c => string.IsNullOrEmpty(c.Id) ? "<no id>" : c.Id)
            .ToList();

        // The display name is not decoration: `DreamCreature.Called` falls back to "Something", so a blank one
        // degrades the only line the player ever reads about a creature, quietly and everywhere.
        Check($"each one has an id and something to be called{Listing(namelessCreature)}",
            namelessCreature.Count == 0);

        var duplicateCreature = creatures
            .GroupBy(c => c.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        Check($"and the ids are distinct{Listing(duplicateCreature)}", duplicateCreature.Count == 0);

        // **The authored lie, and the reason this block is worth its length.** `Touch` ejects before it reads
        // `ResolveCost` when `Banishes` is set, so a cost on a boss is a number that looks tuned, reads as
        // tuned, and is never once consulted. Same class as the 33 harvest nodes paying items that did not
        // exist: a field that means nothing where it sits.
        var costlyBanisher = creatures
            .Where(c => c.Banishes && c.ResolveCost != 0)
            .Select(c => $"{c.Id}: {c.ResolveCost}")
            .ToList();
        Check($"a creature that banishes authors no chunk, because nothing would read it{Listing(costlyBanisher)}",
            costlyBanisher.Count == 0);

        // And the mirror. A minor creature costing zero is not gentle — it is invisible: it drifts about
        // taking nothing, which looks exactly like one you are successfully avoiding.
        var freeMinor = creatures
            .Where(c => !c.Banishes && c.ResolveCost <= 0)
            .Select(c => c.Id)
            .ToList();
        Check($"and one that does not banish costs something, or it is scenery{Listing(freeMinor)}",
            freeMinor.Count == 0);

        // **A rule with a citation rather than a preference.** `DreamVerb.Suppress` is documented as *"the only
        // verb boss pocket 1 can be solved with, since Float is what is inside it"* — which is only true while
        // the thing hunting you there stops hunting when your light goes out. A relentless boss is a legitimate
        // design and it is not this one: flipping this field makes that pocket unsolvable with what the player
        // holds, so it should cost a red line and a decision rather than happen quietly.
        var relentless = creatures
            .Where(c => c.Banishes && !c.PursuesOnlyWhenLit)
            .Select(c => c.Id)
            .ToList();
        Check($"and it stops hunting when the light goes out, or Suppress is not an answer to it{Listing(relentless)}",
            relentless.Count == 0);

        // -- What a pocket scene has to be, checked by building one --
        //
        // **`PocketHost` asks a scene for four things and complains about none of them.** It hides the room
        // view, adds the scene and turns the viewports on; if the container has no look material the pocket is
        // simply in colour, and if there is no way out you are in there until the bar empties. Both are
        // *authoring* faults with no error and no red line — `whats-left.md`'s "a wrong node name fails
        // silently", one realm over.
        //
        // Only scenes a `PocketData` actually names are swept. That is the honest definition of a pocket
        // scene, it keeps `DreamProbe.tscn` — a tuning bench, not a pocket — out of it, and it means each of
        // the fourteen is covered on the day it is pointed at rather than on the day someone remembers.
        foreach (var pocketScene in authoredPockets.Where(p => p.HasScene && ResourceLoader.Exists(p.ScenePath)))
        {
            var packed = ResourceLoader.Load<PackedScene>(pocketScene.ScenePath);
            var root = packed?.Instantiate();

            Check($"{pocketScene.Id}'s scene builds", root != null);
            if (root == null) continue;

            // **Clear the verbs before the scene enters the tree, or the gate check below measures nothing.**
            // `PocketGate._Ready` reads `Holds` the moment `AddChild` runs, and an earlier block in this suite
            // had left Float learned — so every gate opened on entry, and the check that they are open *after*
            // the grant passed whether or not anything re-read them. It stayed green with the fix deleted.
            // §3's seventh class, and the ordinary shape of it: the assertion was fine and the *precondition*
            // was the thing being taken on trust. A gate over an ordering bug has to establish the ordering.
            if (WorldStateManager.Instance is { } gateWorld)
            {
                foreach (DreamVerb verb in System.Enum.GetValues<DreamVerb>())
                {
                    if (verb != DreamVerb.None) gateWorld.SetFlag(Flags.VerbLearned(verb), false);
                }
            }

            floodTree?.Root.AddChild(root);

            var all = new List<Node>();
            void Walk(Node n) { all.Add(n); foreach (var c in n.GetChildren()) Walk(c); }
            Walk(root);

            // A `Control`, because the host adds it beside `RoomView` and hides that instead of changing
            // scene. A `Node3D` root would simply not draw, with nothing to say why.
            Check($"{pocketScene.Id} is a Control, so it can sit beside the room view", root is Control);

            var containers = all.OfType<SubViewportContainer>().ToList();
            Check($"{pocketScene.Id} draws its 3D through a SubViewportContainer", containers.Count > 0);

            // **The monochrome lives on the container, not in the scene.** Forget it and the Dream is in full
            // colour — which looks like a shader bug and is a missing drag-and-drop.
            Check($"{pocketScene.Id} wears the look pass, or the Dream is in colour",
                containers.Any(c => c.Material != null));

            // **`stretch_shrink` is what makes the pocket render at 480x270, and forgetting it is invisible in
            // a screenshot.** With `stretch` on and no shrink, the container resizes the `SubViewport` to its
            // own 1920x1080, so everything still draws and still looks monochrome — but `DreamLook` derives
            // its Bayer grid from `textureSize(TEXTURE, 0)`, so an 8x8 cell lands on 8 screen pixels instead
            // of 32. The shader's own comment names the outcome: *"the pattern is four times too fine and
            // reads as noise instead of as texture."*
            //
            // **What it actually looked like was a colour, which is why this needed a gate rather than an
            // eye.** Reported twice as green fog at the white/grey boundary and only while moving. Nothing in
            // the frame is green — both of the shader's exits are `vec3(x)`, so R, G and B are equal by
            // construction — but a near-1-pixel alternating pattern lights alternating subpixels on an
            // RGB-striped panel, and green is the middle one. Standing still your eye integrates it back to
            // grey; moving, the pattern crawls across the geometry and the fringe reads as colour. **A
            // rendering fault that is not in the render is exactly the kind a screenshot cannot settle**, and
            // the resolution is a number in the scene, so it belongs here.
            //
            // Asserted as "shrinks at all" rather than "== 4": 4 is the tuned value, but a pocket authored at
            // 2 or 8 is a look decision, and only 1 is the accident.
            Check($"{pocketScene.Id} shrinks its viewport, or the dither lands on the wrong grid",
                containers.All(c => c.StretchShrink > 1));

            // Nearest, or the upscale interpolates the pixels the shrink just bought back into a smear — the
            // shrink is only half of the low-resolution look and the other half is one enum.
            Check($"{pocketScene.Id} upscales without filtering, so the pixels stay pixels",
                containers.All(c => c.TextureFilter is CanvasItem.TextureFilterEnum.Nearest
                    or CanvasItem.TextureFilterEnum.NearestWithMipmaps));

            var viewports = all.OfType<SubViewport>().ToList();
            Check($"{pocketScene.Id} has a viewport for the host to switch off while you are awake", viewports.Count > 0);

            // **The trap `dream-probe.md` names in capitals.** A private `World3D` costs nothing at runtime and
            // makes every node in it invisible in the editor's 3D view — so the pocket can be run and never
            // authored. It reads as tidiness and it is the one setting that removes the ability to build.
            var privateWorld = viewports.Where(v => v.OwnWorld3D).ToList();
            Check($"{pocketScene.Id} shares the parent world, or none of it can be authored ({privateWorld.Count} private)",
                privateWorld.Count == 0);

            // A pocket you cannot walk out of is not a softlock — the bar empties and you are carried to a
            // camp — but it charges a whole visit for a wrong turn, and nothing about it looks unfinished.
            var ways = all.Count(n => n is PocketExit or PocketAnchor);
            Check($"{pocketScene.Id} has a way back out of it ({ways})", ways > 0);

            // **`PocketStandalone` has to tell two runs apart, so both are driven here.** Under F6 it is the
            // only thing that can make a pocket a pocket; under F5 `PocketHost` has already crossed you in and
            // it must keep its hands off — otherwise every real crossing would silently top up the bar and
            // hand over two verbs the player has not earned. One node, two answers, and the wrong one is
            // invisible in play.
            var standalones = all.OfType<PocketStandalone>().ToList();
            if (standalones.Count > 0)
            {
                // **There is deliberately no check here that it stands down under F5, and the reason is worth
                // more than the check would have been.** One was written, and its break-test — deleting the
                // `InsideTheDream` guard — stayed **green**: `PocketManager.Enter` independently refuses with
                // `AlreadyInside`, so nothing observable changes either way. §3's seventh class, caught by
                // planning the break-test rather than by the suite. The guard stays, because without it every
                // legitimate crossing pushes a warning nobody should be trained to ignore — but it is
                // defence-in-depth over a rule that is already gated where it lives, and a check over it would
                // pass whether it existed or not.
                //
                // What *is* falsifiable is the half only this node does: working out which pocket the scene
                // belongs to, from `ScenePath` rather than an authored id, so a copied scene cannot carry a
                // stale one.
                // The gates' state *before* Engage is the bug, held still: they read on tree entry, above, with
                // no verb granted. Captured rather than asserted, because a pocket with no gated geometry is a
                // legitimate pocket and there is nothing to be shut.
                var sceneGates = all.OfType<PocketGate>().ToList();
                var shutBefore = sceneGates.Count(g => !g.IsOpen);

                foreach (var standalone in standalones) standalone.Engage();
                Check($"{pocketScene.Id}'s helper crosses you in when nothing else has, and picks its own pocket",
                    standalones.All(s => s.Engaged) && pockets.CurrentPocketId == pocketScene.Id);

                // **The reported symptom, in the real scene rather than a fixture: *"the HighLedge in the scene
                // is invisible"*.** Every gate in the pocket read `Holds` on tree entry, a frame before this
                // node granted the verbs, so a Float-gated ledge was hidden and nothing re-read it — for the
                // whole session, since a standalone run never leaves and re-enters. `GrantVerbs` hands over
                // every verb, so afterwards *no* gate in the scene has grounds to be shut.
                //
                // Worth having beside the `ReapplyAll` fixture rather than instead of it: the fixture proves the
                // mechanism, this proves it is wired to the thing the author sees. The fixture was green through
                // the entire bug.
                if (standalones.Any(s => s.Engaged && s.GrantVerbs) && sceneGates.Count > 0)
                {
                    Check($"{pocketScene.Id}'s gated geometry is back after the grant, not hidden for the "
                        + $"session ({shutBefore} of {sceneGates.Count} were shut before it)",
                        sceneGates.All(g => g.IsOpen));
                }

                // **Being inside a pocket is not the same as the keys working, and the first version of this
                // node shipped believing it was.** `DreamVerbHold` is attached by `PocketHost` — the one thing
                // a standalone run does not have — so entering only moved the verbs from *refused, with a
                // reason in the log* to *silently doing nothing*, which is the worse of the two.
                Check($"{pocketScene.Id}'s helper also brings a verb poller, or the keys reach nothing",
                    standalones.All(s => s.VerbInputLive));

                // The other side of the bench lesson: here the readout's verb line *should* appear, and it now
                // appears for a reason. The fixtures above prove the walk; this proves the scene passes it.
                Check($"{pocketScene.Id} advertises the verb keys because they are wired, not because the text is "
                    + "static", DreamLookTuner.VerbsWiredIn(root));

                // **The predicate joined to the text it governs**, which is the half that was actually wrong: the
                // walk could be perfect and the readout still print the same line either way. Asserted only in a
                // wired scene, because that is the branch a real pocket takes — the unwired branch is the bench,
                // and the bench is not swept.
                if (root.GetNodeOrNull<Label>("%Readout") is { } sceneReadout)
                {
                    Check($"and its readout names them on screen, where they are read from ({pocketScene.Id})",
                        sceneReadout.Text.Contains("Q hide") && sceneReadout.Text.Contains("E flare"));
                }

                // **The F6 path end to end, in the scene the key is actually pressed in.** Everything above and
                // in the `DreamVerbHold` block is *composed*: this scene has a poller, and a poller somewhere
                // opens a hold, and a lamp somewhere dims. Composition cannot see a scene where the grant, the
                // poller and the meter are each fine and the keys still do nothing — which is how *"none of the
                // abilities work when I press F6"* arrives against a green suite. Through `Input.ActionPress` so
                // the binding is part of the claim, and through `Poll`/`Tick` because neither `_Process` ticks
                // meaningfully in a headless run.
                var poller = standalones
                    .Select(s => s.GetNodeOrNull<DreamVerbHold>("VerbInput"))
                    .FirstOrDefault(p => p != null);

                if (poller != null)
                {
                    Input.ActionPress("dream_flare");
                    poller.Poll();
                    Check($"pressing the Flare key inside {pocketScene.Id} opens the hold, binding and all",
                        DreamVerbs.IsHolding(DreamVerb.Flare));

                    Input.ActionRelease("dream_flare");
                    poller.Poll();
                    Check("and letting the key go closes it", !DreamVerbs.IsHolding(DreamVerb.Flare));

                    // **The visible half, because the report is always about the screen and never about a flag.**
                    // A hold nobody can see is indistinguishable from a key that does nothing, and the only thing
                    // Suppress *has* is the lamp.
                    foreach (var sceneLamp in all.OfType<DreamSuppressibleLight>())
                    {
                        Check($"{pocketScene.Id}'s lamp starts lit, so the dimming below is a change",
                            sceneLamp.BaseRange > 0.0f
                            && Mathf.IsEqualApprox(sceneLamp.OmniRange, sceneLamp.BaseRange));

                        var dimmed = sceneLamp.BaseRange * Mathf.Clamp(sceneLamp.SuppressedFraction, 0.0f, 1.0f);

                        Input.ActionPress("dream_suppress");
                        poller.Poll();
                        sceneLamp.Tick(1.0);
                        Check($"and holding Suppress in the scene takes it down to {dimmed:0.00} "
                            + $"from {sceneLamp.BaseRange:0.00} (it is at {sceneLamp.OmniRange:0.00})",
                            Mathf.IsEqualApprox(sceneLamp.OmniRange, dimmed));

                        Input.ActionRelease("dream_suppress");
                        poller.Poll();
                        sceneLamp.Tick(1.0);
                        Check("and letting go gives the light back",
                            Mathf.IsEqualApprox(sceneLamp.OmniRange, sceneLamp.BaseRange));
                    }
                }

                pockets.Leave();
                foreach (DreamVerb granted in System.Enum.GetValues<DreamVerb>())
                    if (granted != DreamVerb.None) world.SetFlag(Flags.VerbLearned(granted), false);
            }

            floodTree?.Root.RemoveChild(root);
            root.QueueFree();
        }

        // -- A minor creature takes a chunk, and the cooldown is what makes that survivable --
        var minor = new DreamCreatureData { Id = "verify_minor", ResolveCost = 9, TouchCooldownSeconds = 5f };
        var creature = new DreamCreature { Data = minor };

        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("a pocket to be touched in", pockets.Enter(PocketKnot));
        resolve.SetTo(resolve.Max);

        var before = resolve.Current;
        Check("a touch costs a chunk of nerve rather than health",
            creature.Touch() && Mathf.IsEqualApprox(resolve.Current, before - minor.ResolveCost));
        Check("and the body is untouched, because nothing in the Dream may cost it",
            Mathf.IsEqualApprox(vitality.Current, vitality.Max));

        // **The load-bearing one.** `BodyEntered` can fire every frame while a player is pinned, so without a
        // cooldown a single contact empties the meter in well under a second and the ejection reads as
        // arbitrary. This is the only check that would catch that, and it is invisible in code.
        var afterFirst = resolve.Current;
        Check("a second touch inside the cooldown costs nothing",
            !creature.Touch() && Mathf.IsEqualApprox(resolve.Current, afterFirst));
        Check("and it landed exactly once", creature.Touches == 1);

        // -- A boss banishes, which is the same call as running out --
        var bossCreature = new DreamCreature
        {
            Data = new DreamCreatureData { Id = "verify_boss_thing", Banishes = true },
        };

        Check("still inside for the banish", pockets.InPocket);
        Check("a banish throws you out", bossCreature.Touch() && !pockets.InPocket);
        Check("and lands you at a camp, exactly as an empty bar does",
            CampManager.IsCamp(nav.CurrentRoom));
        Check("having taken the pocket from nobody",
            !world.GetFlag(Flags.PocketCleared(PocketKnot)) &&
            pockets.InspectEnter(PocketKnot) == PocketBlock.None);

        creature.Free();
        bossCreature.Free();
        world.SetFlag(Flags.VerbLearned(DreamVerb.Float), false);
        DreamVerbs.ReleaseAll();

        // `SetTo(Max)` and **not** `ResetAll()`: that also resets the *ceiling*, which the save round trip
        // below needs raised to mean anything — it asserts a bar above the starting 100 to prove `ImportMax`
        // ran before `SetTo`. Both of those checks went red the first time this block cleaned up after itself,
        // which is the same slip twice in this file now: a tidy-up that clobbers a later block's premise.
        resolve.SetTo(resolve.Max);
        nav.GoToRoom("verify_wilds");

        // -- The ceiling survives a save, and a pocket does not --
        const string pocketPath = "user://verify_pockets.json";

        nav.GoToRoom("verify_wilds");
        var savedMax = resolve.Max;
        Check("the ceiling is above the starting one, so the round trip means something",
            savedMax > GameConfig.ResolveMax);
        Check("the pocket save writes", SaveService.Save(pocketPath));

        resolve.ResetAll();
        Check("the ceiling was actually reset", Mathf.IsEqualApprox(resolve.Max, GameConfig.ResolveMax));

        Check("the pocket save loads", SaveService.Load(pocketPath));
        Check("and the raised ceiling came out of the file", Mathf.IsEqualApprox(resolve.Max, savedMax));

        // The ordering claim inside `SaveService`: `ImportMax` runs before `SetTo`, because `SetTo` clamps to
        // `Max` — so a bar above the starting ceiling proves the ceiling was restored first. Reverse the two
        // lines and the bar comes back trimmed to 100 while the ceiling still reads high.
        Check("and the bar with it, rather than trimmed to the old ceiling",
            resolve.Current > GameConfig.ResolveMax);

        // A save taken inside a pocket is not a thing: 3D position is not in this format, so loading lands
        // you in the Waking room the file names with the entrance intact and nothing granted.
        //
        // The flag is cleared rather than assumed clear: the hotspot block above *completed* this pocket, so
        // without this line these two checks would be measuring that block's leftovers and both went red the
        // first time. Second instance in two units of the same slip — a check reading state it did not set —
        // so the habit worth keeping is to set the fixture in the block that reads it, every time.
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        Check("re-entered for the mid-pocket save", pockets.Enter(PocketKnot));
        Check("the save still writes from in there", SaveService.Save(pocketPath));
        Check("and loading it puts you outside, with no pocket live",
            SaveService.Load(pocketPath) && !pockets.InPocket);
        Check("on an ordinary Waking screen", nav.CurrentRoomId == "verify_wilds");
        Check("and the entrance was not spent by the round trip",
            pockets.InspectEnter(PocketKnot) == PocketBlock.None);

        SaveService.DeleteSave(pocketPath);

        // -- Leave the world as it was found --
        pockets.ForceClear();
        world.SetFlag(Flags.PocketCleared(PocketKnot), false);
        world.SetFlag(Flags.KnotCut(PocketKnot), false);
        states.SetState(ArenaTough, EntityState.Corrupted);
        resolve.ResetAll();
        inv.ResetAll();
        SetRealm(GameRealm.Waking);
        nav.GoToRoom("verify_wilds");
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

        // -- What unit 3b took out, asserted rather than assumed --
        // A deletion pass leaves nothing behind in the code to notice a file surviving in somebody's
        // working tree: the loaders scan directories, so a re-appearing `.tres` simply loads, and every
        // sweep in this file would keep passing while the game quietly grew a screen nobody can reach.
        // These three are the notice. They are cheap and they will be deleted the day the thing they
        // name becomes authorable again — which for heists is unit 6, and for the other two is never.
        var waystation = rooms.Where(kv => kv.Value?.ChapterId?.StartsWith("waystation") == true)
            .Select(kv => kv.Key).OrderBy(k => k).ToList();
        Check($"the waystation prototype is gone{Listing(waystation)}", waystation.Count == 0);

        var gravecap = InventoryCraftingManager.Instance.Catalogue.Keys
            .Where(id => id.StartsWith("herb_gravecap")).OrderBy(k => k).ToList();
        Check($"and so is the item only it paid out{Listing(gravecap)}", gravecap.Count == 0);

        // A check that `data/heists/` stayed empty stood here until unit 6b deleted the directory, the loader
        // and the constant. It had done its job: it existed to go red the moment anyone authored content into
        // a mechanism already scheduled for deletion, and nobody ever did. **Nothing replaced it, because
        // there is no directory left to count** — the pocket registry has its own files-on-disk check in the
        // `pockets` section, which is the same guard aimed at the collection that now exists.

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

        // -- Unit 6: a pocket entrance must name a pocket that exists --
        //
        // The crossing rule in its usual form: one authored collection naming an id in another, which nobody
        // writing either sweep is prompted to check. A mistyped `PocketId` is otherwise a door that swallows
        // the click and does nothing, and `PocketManager.Enter` is deliberately silent about it *because*
        // this check exists — the diagnostic belongs to a sweep, not to a click.
        //
        // ~~**Zero entrances are authored today, so the count is printed.**~~ **Fourteen are, since unit 10 —
        // corrected 2026-08-06.** The count stays in the message for the reason it was put there: a universal
        // over an empty set is a wish (unit 4b), and the number is what tells a reader which of the two this
        // line currently is.
        var entrances = rooms
            .SelectMany(kv => kv.Value.Hotspots
                .Where(h => h is { Kind: HotspotKind.CrossToDream })
                .Select(h => (Room: kv.Key, Hotspot: h)))
            .ToList();

        var danglingPockets = entrances
            .Where(e => PocketManager.Instance?.GetPocket(e.Hotspot.PocketId) == null)
            .Select(e => $"{e.Room}/{e.Hotspot.Id}: {e.Hotspot.PocketId}")
            .ToList();

        Check($"every authored pocket entrance names a registered pocket "
            + $"({entrances.Count} authored){Listing(danglingPockets)}",
            danglingPockets.Count == 0);

        // -- Unit 10: the same crossing read the other way, which is a different claim --
        //
        // **Found by break-test on 2026-08-06, in the read-back over unit 10.** Pointing `ch3_tear`'s entrance
        // at a second pocket left `ch3_demon` — the Tear, where Iris's tether is cut and the game ends — with
        // no door anywhere in the world, and **all 1101 checks passed**. `--verify-content` went on printing
        // *pocket entrances authored on Waking screens 14 / 14* over a game that could not be finished, because
        // **moving a door does not change how many doors there are.**
        //
        // The check above says *every door leads somewhere*. This one says *everywhere is reached by a door*,
        // and the two stop being the same statement the moment one pocket can be named twice. §3's recurring
        // class in the shape that keeps recurring: keyed on a count where the rule is about a pairing.
        //
        // **A knowledge pocket was already covered, and only by accident** — `GateRoster`'s page-reachability
        // sweep asks whether anything opens the pocket that teaches each page, so losing that door reddens a
        // check about *pages*. Knot and boss pockets grant no page and were invisible, which is exactly
        // backwards: those six are the three chapter endings and the finale.
        //
        // `dream_probe` is excluded by name, as it is from the content bill and for the same reason — it is the
        // reference implementation, it stands on no screen, and `F10` is deliberately its only way in.
        var doorsPerPocket = entrances
            .GroupBy(e => e.Hotspot.PocketId)
            .ToDictionary(g => g.Key, g => g.Count());

        var authoredPockets = (PocketManager.Instance?.Pockets
                               ?? new Dictionary<string, PocketData>())
            .Keys
            .Where(id => id != ContentReport.ReferencePocketId && !id.StartsWith("verify_"))
            .OrderBy(id => id)
            .ToList();

        var doorless = authoredPockets.Where(id => !doorsPerPocket.ContainsKey(id)).ToList();
        Check($"and every authored pocket has one of its own "
            + $"({authoredPockets.Count} pockets){Listing(doorless)}",
            doorless.Count == 0);

        var openedTwice = doorsPerPocket
            .Where(kv => kv.Value > 1)
            .Select(kv => $"{kv.Key} ×{kv.Value}")
            .OrderBy(s => s)
            .ToList();

        Check($"and no two entrances lead into the same one{Listing(openedTwice)}", openedTwice.Count == 0);

        // -- The remnant, which is the half of the pair that is not the door --
        //
        // The authoring checklist asks for **two** hotspots per pocket: the `CrossToDream` shown while
        // `pocket.<id>.cleared` is false, and an `Examine` shown once it is true, standing in the same place as
        // the broken thing the crossing left behind. Miss the second and the screen simply loses a region the
        // moment the pocket is taken — no error, no refusal, just a spot that used to be interactive and now
        // is not. The mechanism is gated on a fixture in the `pockets` section; **the authored content was not**,
        // and 14 pairs is 14 chances to have typed one flag.
        var unpaired = entrances
            .Where(e => !rooms[e.Room].Hotspots.Any(h =>
                h is { Kind: HotspotKind.Examine } &&
                h.RequiredFlagValue &&
                h.RequiredFlag == Flags.PocketCleared(e.Hotspot.PocketId)))
            .Select(e => $"{e.Room}/{e.Hotspot.Id}")
            .OrderBy(s => s)
            .ToList();

        Check($"and each one leaves a remnant behind when it is spent{Listing(unpaired)}", unpaired.Count == 0);

        // -- Unit 10: a pocket that severs names a body, and the body has to be on the map --
        //
        // **Also found by break-test on 2026-08-06, and it is the more dangerous of the two.** Retyping
        // `ch3_demon`'s target as `ch3.iris_typo` left every check green: `EntityStateManager.Sever` takes any
        // string, writes the state of an instance nobody placed, and returns true — so the finale would report
        // that it had cut the tether, Iris would stand where she was, and the only symptom is in the ending.
        //
        // The bill counts *boss pockets that sever somebody* by asking whether the field is **non-empty**, which
        // is the weaker property again — it was written when the bosses were unplaced and blank was the honest
        // state. Now that all three are on the map the field can be checked against them, and this is the check
        // that goes red the day a boss is renamed in one file and not the other.
        var placedInstances = rooms.Values
            .SelectMany(r => r.Entities ?? new Godot.Collections.Array<EntityPlacement>())
            .Where(p => p != null)
            .Select(p => p.InstanceId)
            .ToHashSet();

        var severing = (PocketManager.Instance?.Pockets ?? new Dictionary<string, PocketData>())
            .Where(kv => !kv.Key.StartsWith("verify_") && !string.IsNullOrEmpty(kv.Value?.TargetEntityId))
            .ToList();

        var phantomTethers = severing
            .Where(kv => !placedInstances.Contains(kv.Value.TargetEntityId))
            .Select(kv => $"{kv.Key}: {kv.Value.TargetEntityId}")
            .OrderBy(s => s)
            .ToList();

        Check($"every pocket that cuts a tether names a body that is standing somewhere "
            + $"({severing.Count} sever){Listing(phantomTethers)}",
            phantomTethers.Count == 0);

        // -- And severing is a boss verb, which is now a ruling rather than an accident of authoring --
        //
        // **Jeremy, 2026-08-06:** *"Sever is for weakening bosses (3) or regaining old knowledge."* The code
        // already obeys it structurally — `PocketManager.Grant` cuts the tether inside `case PocketKind.Boss`
        // and nowhere else — so a `TargetEntityId` on a knowledge or knot pocket is **inert rather than
        // dangerous**. That is exactly why it wants a check: an authored field that silently does nothing is
        // §3's eleventh class, and the shape of it here is a designer typing a name into the wrong `.tres` and
        // believing a tether will be cut. Three severing pockets, three bosses, and no fourth.
        var strayTethers = severing
            .Where(kv => kv.Value.Kind != PocketKind.Boss)
            .Select(kv => $"{kv.Key} ({kv.Value.Kind})")
            .OrderBy(s => s)
            .ToList();

        Check($"and only a boss pocket names one at all{Listing(strayTethers)}", strayTethers.Count == 0);

        // -- Unit 5: the three camps the verb points at, and the walk out of each --
        //
        // **A census by chapter rather than a count of three**, on unit 4b's lesson: a bare `== 3` is
        // satisfied by three benches in chapter 1 and says nothing, while *one per chapter* is the promise
        // §17 actually makes — one camp per chapter, and they are the fast-travel network. It also fails
        // usefully: the offender list names the chapter that has none or the chapter that has two.
        var campChapters = camps
            .Select(id => rooms[id].ChapterId ?? "")
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        var campCensus = new List<string>();
        foreach (var chapter in new[] { "ch1", "ch2", "ch3" })
        {
            var count = campChapters.GetValueOrDefault(chapter);
            if (count != 1) campCensus.Add($"{chapter}: {count}");
        }
        Check($"every chapter has exactly one camp{Listing(campCensus)}", campCensus.Count == 0);

        // **The walk out is the price the amendment exists to charge**, so it has to be authored. §17 as
        // amended is explicit that a camp is an ordinary screen with ordinary exits and the menu is a
        // shortcut *in* — a camp with no `Navigate` on it would be the pure overlay the amendment rejected,
        // and it would be reachable only by the verb and escapable only by dying. Until this check that
        // sentence lived in a document and nowhere else.
        var noWayOut = camps
            .Where(id => !rooms[id].Hotspots.Any(h => h is { Kind: HotspotKind.Navigate } &&
                                                     !string.IsNullOrEmpty(h.TargetRoomId)))
            .ToList();
        Check($"and every camp keeps a door you can walk out of{Listing(noWayOut)}", noWayOut.Count == 0);

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

        // -- Every flag a hotspot waits on must have something that writes it --
        //
        // **The most expensive gap the suite has had, found in play on 2026-08-05 and closed here on
        // 2026-08-06.** Jeremy beat the Apothecary and her notes stayed locked. The cause was not that screen:
        // a win wrote `freed.*` only if the body was `Severed` when it fell, severing an ordinary creature has
        // no route in the game, and so **all 24 `freed.*` hotspots in the world were permanently shut** —
        // two of her three Physick notes among them, and with those the two fused traps whose blueprints they
        // are. The ruling that fixed it is in `EncounterManager.Finish`; this is the check that would have
        // found it without a playthrough.
        //
        // **Every sweep in this file asked a weaker question.** *"Every freed / defeated gate names a creature
        // that is placed"* was green, because the creatures were placed — the missing term was whether the
        // flag could ever be **written**. `--verify-content` printed *people freed 24 / 24* for the same
        // reason: it counted the hotspots. §3's nineteenth class, and the general form of the fix is to model
        // the writers rather than the names.
        //
        // A flag is writable if content sets it (`SetsFlag` on a hotspot or a recipe) or if a manager derives
        // it from content that exists. The derived half is spelled out rather than pattern-matched on the
        // prefix, because **the prefix is exactly what lies**: `freed.ch2.apothecary_01` looked well-formed
        // for months. `Freed` is writable only for a body with a `PurifiedName` and `Defeated` only for one
        // without, since that is the single question `Finish` asks.
        var writable = new HashSet<string>();

        foreach (var (roomId, room) in rooms)
        {
            writable.Add(Flags.Cleansed(roomId));
            writable.Add(Flags.CampFound(roomId));

            foreach (var h in room.Hotspots)
            {
                if (h == null) continue;
                if (!string.IsNullOrEmpty(h.SetsFlag)) writable.Add(h.SetsFlag);
                if (h.Kind == HotspotKind.Mend) writable.Add(Flags.Mended(h.Id));
                if (h.Kind == HotspotKind.Harvest) writable.Add(Flags.Harvested(h.Id));
            }

            foreach (var p in room.Entities ?? new Godot.Collections.Array<EntityPlacement>())
            {
                if (p?.Entity == null) continue;
                writable.Add(string.IsNullOrEmpty(p.Entity.PurifiedName)
                    ? Flags.Defeated(p.InstanceId)
                    : Flags.Freed(p.InstanceId));
            }
        }

        foreach (var recipe in InventoryCraftingManager.Instance?.Recipes.Values
                               ?? Enumerable.Empty<RecipeData>())
            if (!string.IsNullOrEmpty(recipe?.SetsFlag)) writable.Add(recipe.SetsFlag);

        foreach (var blueprint in InventoryCraftingManager.Instance?.Recipes.Values
                                  .Select(r => r?.RequiredBlueprintId)
                                  .Where(id => !string.IsNullOrEmpty(id)) ?? Enumerable.Empty<string>())
            writable.Add(Flags.Learned(blueprint));

        foreach (var pocketId in PocketManager.Instance?.Pockets.Keys ?? Enumerable.Empty<string>())
        {
            writable.Add(Flags.PocketCleared(pocketId));
            writable.Add(Flags.KnotCut(pocketId));
        }

        foreach (var chapter in rooms.Values.Select(r => r?.ChapterId).Where(c => !string.IsNullOrEmpty(c)))
            writable.Add(Flags.MaterialRefill(chapter));

        foreach (DreamVerb verb in System.Enum.GetValues(typeof(DreamVerb)))
            writable.Add(Flags.VerbLearned(verb));

        var unwritable = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
            {
                if (h == null) continue;

                var gates = new List<string>();
                if (!string.IsNullOrEmpty(h.RequiredFlag)) gates.Add(h.RequiredFlag);
                foreach (var extra in h.RequiredFlags ?? System.Array.Empty<string>())
                    if (!string.IsNullOrEmpty(extra)) gates.Add(extra);

                foreach (var gate in gates.Where(g => !writable.Contains(g)))
                    unwritable.Add($"{roomId}/{h.Id}: {gate}");
            }

        Check($"every flag a hotspot waits on has something that writes it "
            + $"({writable.Count} writable){Listing(unwritable)}",
            unwritable.Count == 0);

        // -- `placement.md` rule 17: every optional fight must be worth having taken --
        //
        // **A `--verify-content` row for one day, promoted here on 2026-08-06 the hour its one violation was
        // settled.** `ch1_bramble` held a fight whose only payoff was the person it freed, which §1 explicitly
        // does not count; a heartwood cache went onto that screen with the chapter 2 material pass, which is
        // the cheapest fix either problem was going to get. A rule with no violations left is a gate rather
        // than a report — the row stays because its counts (13 optional, 9 of them a board) are how anyone
        // checks the derivation is still the one §1 was written against.
        //
        // The walk lives in `ContentReport` and is called rather than copied, because two answers to *which
        // fights are optional* would drift the first time a door moved.
        var unpaidFights = ContentReport.OptionalFightsPayingNothing(rooms, out var optionalCount, out _);

        Check($"every optional fight pays something beyond the person it frees "
            + $"({optionalCount} optional){Listing(unpaidFights)}",
            optionalCount > 0 && unpaidFights.Count == 0);

        // -- A chapter gate must be affordable to a player who bought everything offered so far --
        //
        // **Jeremy's second play finding, 2026-08-05: he reached the end of chapter 2 with the kit built and
        // could not afford the mine cart.** The cart wants 4 Scrap Iron, 2 Heartwood and 1 Pitch, and chapter 2
        // contained **no Heartwood at all** — 7 in chapter 1, 0 in chapter 2, 3 in chapter 3 — while the
        // mandatory bridge had already taken 4 of the 7. Everything craftable in the window wanted 13 Heartwood
        // against a supply of 7. `items.md` §19 calls that out in its own words: *"A common must never be the
        // reason a craft is refused. If the player is ever short of Heartwood, Heartwood was misclassified."*
        //
        // **Nothing saw it because every existing material check is global.** `GateRoster`'s *"every item in the
        // catalogue can be reached from the ground"* asks whether each recipe's inputs exist **somewhere in the
        // game**, and chapter 3 has Heartwood, so the sum came out fine. §1's rule 13 had the windowed form
        // written down and deferred — *"the per-window form needs blueprint placement first"* — and blueprints
        // landed with unit 10 without anyone cashing it.
        //
        // **What is asserted is the narrow claim that actually bit, not the general one, and the difference
        // matters.** The general form — *no chapter window is ever short of any common* — is **false today and
        // correctly so**: chapter 1 is woodland and holds no Scrap Iron, so the Spiked Pit and the Chime cannot
        // be built until the village. That is a timing choice (decision 9 is built on it) and not a lockout,
        // because nothing in chapter 1 is *gated* on those traps. What is never acceptable is a **repair that
        // opens the next chapter** being unaffordable, because that is a save the player cannot finish —
        // invariant 12. So the check is scoped to the materials each repair itself names.
        //
        // Supply counts base yields only and ignores `RefillsAtMilestone`, which is the conservative direction:
        // a refill arrives at the chapter's boss and the gate may be reached before it.
        // §19's own table: the four commons that are a **one-time startup cost**, spent building a kit you then
        // own. Bitterroot and Tallow are deliberately excluded — they carry the repeating salve economy, so a
        // one-shot demand count says nothing true about them. The two scarce materials are excluded too, and
        // for the opposite reason: Cairnstone and Graveflower are *meant* to be tight enough to make a craft a
        // decision, so asserting they cover every recipe would gate away the choice §1 exists to create.
        var DormantCommons = new HashSet<string>
        {
            "mat_heartwood", "mat_scrap_iron", "mat_cured_hide", "mat_pitch",
        };

        var chapterOrder = new[] { "ch1", "ch2", "ch3" };

        var stationChapter = rooms
            .SelectMany(kv => kv.Value.Hotspots
                .Where(h => h != null && !string.IsNullOrEmpty(h.StationId))
                .Select(h => (h.StationId, kv.Value.ChapterId)))
            .GroupBy(t => t.StationId)
            .ToDictionary(g => g.Key, g => g.First().ChapterId);

        int Window(string chapter) => System.Array.IndexOf(chapterOrder, chapter ?? "");

        // A recipe with no station is made at the camp bench, which exists from chapter 1.
        int RecipeWindow(RecipeData r) => string.IsNullOrEmpty(r.RequiresStation)
            ? 0
            : stationChapter.TryGetValue(r.RequiresStation, out var ch) ? Window(ch) : 0;

        var recipes = InventoryCraftingManager.Instance?.Recipes.Values
                          .Where(r => r != null).ToList()
                      ?? new List<RecipeData>();

        var unaffordable = new List<string>();

        foreach (var repair in recipes.Where(r => r.RepairsWorld).OrderBy(r => r.Id))
        {
            var window = RecipeWindow(repair);

            var supply = new Dictionary<string, int>();
            foreach (var room in rooms.Values.Where(r => Window(r.ChapterId) <= window))
                foreach (var h in room.Hotspots)
                {
                    if (h is not { Kind: HotspotKind.Harvest or HotspotKind.Mend }) continue;
                    if (string.IsNullOrEmpty(h.ItemId)) continue;
                    supply[h.ItemId] = supply.GetValueOrDefault(h.ItemId) + h.ItemAmount;
                }

            var demand = new Dictionary<string, int>();
            foreach (var r in recipes.Where(r => RecipeWindow(r) <= window))
                foreach (var input in r.Inputs)
                {
                    if (input == null || string.IsNullOrEmpty(input.ItemId)) continue;
                    demand[input.ItemId] = demand.GetValueOrDefault(input.ItemId) + input.Amount;
                }

            foreach (var input in repair.Inputs.Where(i => i != null && DormantCommons.Contains(i.ItemId)))
            {
                var have = supply.GetValueOrDefault(input.ItemId);
                var want = demand.GetValueOrDefault(input.ItemId);
                if (want > have)
                    unaffordable.Add($"{repair.Id} by {chapterOrder[window]}: "
                        + $"{input.ItemId} {have} in the world against {want} wanted");
            }
        }

        Check($"every repair that opens a chapter is affordable to a player who bought everything so far "
            + $"({recipes.Count(r => r.RepairsWorld)} repairs){Listing(unaffordable)}",
            unaffordable.Count == 0);

        GateWorldGraph(rooms);
        GateCombatShapes(rooms);
        GateFights(rooms);
        GateMending(rooms);
        GateStations(rooms);
        GateRoster(rooms);
    }

    /// <summary>
    /// Stations, materials and the three access tools — unit 4a (<c>combat.md</c> §19).
    /// </summary>
    /// <remarks>
    /// <b>The first two checks here are the reason this section exists, and they were red when written.</b>
    /// 33 Harvest hotspots across the skeleton pass yielded <c>mat_heartwood</c>, <c>mat_pitch</c>,
    /// <c>mat_scrap_iron</c>, <c>mat_cured_hide</c> and <c>mat_cairnstone</c>, and not one of those item
    /// files had been written. 439 checks passed over it, because the content sweep asks whether a
    /// Harvest hotspot's <c>ItemId</c> is *non-empty* and never whether it resolves — so
    /// <c>AddItem</c> wrote a phantom stack under the raw id, the grid displayed
    /// <c>mat_heartwood</c> as its own name, and <c>LoseUnprotectedItems</c> read the missing category
    /// as <c>Component</c> and burned it on death.
    /// <para>
    /// That is the fifth blind-spot class in <c>handoff.md</c> §3 in its purest form: <i>any "for each X,
    /// assert P(X)" sweep is silently conditional on X having loaded</i>. The room sweep and the item
    /// catalogue are two collections that had never once been crossed, so every property either one
    /// claimed was true — of itself. <b>The lesson generalises past items: whenever one authored
    /// collection names an id belonging to another, the crossing is the check, and it will not appear on
    /// its own.</b>
    /// </para>
    /// </remarks>
    private static void GateStations(Dictionary<string, RoomData> rooms)
    {
        Section("stations  the tally, the venues, and the three tools");

        var inv = InventoryCraftingManager.Instance;
        var world = WorldStateManager.Instance;
        var nav = NavigationManager.Instance;

        // Authored recipes only, on the same `verify_` convention the room sweep uses at line 1677 and
        // for the same reason: the gates register two recipes in code to exercise the blueprint rules,
        // and a fixture is not content. Found by this section's own files-on-disk check reading 6 of 4 —
        // which is precisely the discrepancy that check exists to surface, arriving on its first run.
        var authored = inv.Recipes
            .Where(pair => !pair.Key.StartsWith("verify_") && pair.Value != null)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        // -- 1. Every id the world names has to resolve --
        // Scoped to Harvest and Mend, the two kinds that pay out an item. Not scoped to `ch*`: a
        // dangling id is a defect wherever it is, fixtures included.
        var danglingYield = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots ?? new Godot.Collections.Array<HotspotData>())
            {
                if (h is not { Kind: HotspotKind.Harvest or HotspotKind.Mend }) continue;
                if (string.IsNullOrEmpty(h.ItemId)) continue;
                if (inv.GetItem(h.ItemId) == null) danglingYield.Add($"{roomId}/{h.Id} -> {h.ItemId}");
            }

        Check($"every item a hotspot pays out exists in the catalogue{Listing(danglingYield)}",
            danglingYield.Count == 0);

        // -- 2. The same hole, from the recipe side --
        var danglingRecipe = new List<string>();
        foreach (var (recipeId, recipe) in authored)
        {
            foreach (var input in recipe.Inputs)
            {
                if (input == null || string.IsNullOrEmpty(input.ItemId)) continue;
                if (inv.GetItem(input.ItemId) == null)
                    danglingRecipe.Add($"{recipeId} needs {input.ItemId}");
                if (input.Amount <= 0)
                    danglingRecipe.Add($"{recipeId} asks for {input.Amount} {input.ItemId}");
            }

            // A world repair outputs no item on purpose; anything else must output one that exists.
            if (!recipe.RepairsWorld && inv.GetItem(recipe.OutputItemId) == null)
                danglingRecipe.Add($"{recipeId} makes {recipe.OutputItemId}");
        }

        Check($"every item a recipe names exists too{Listing(danglingRecipe)}",
            danglingRecipe.Count == 0);

        // -- 3. Files on disk against the registry, for both content types --
        // Only `data/rooms/` had this count before today. A malformed `.tres` returns null from
        // GD.Load and is simply *absent* — every sweep above then passes over a smaller world without
        // saying so, which is the failure this catches and nothing else does.
        Check($"every item file on disk registered an item ({inv.Catalogue.Count} of {FilesIn(GameConfig.ItemDataDir)})",
            FilesIn(GameConfig.ItemDataDir) > 0 && inv.Catalogue.Count == FilesIn(GameConfig.ItemDataDir));
        Check($"every recipe file on disk registered a recipe ({authored.Count} of {FilesIn(GameConfig.RecipeDataDir)})",
            FilesIn(GameConfig.RecipeDataDir) > 0 && authored.Count == FilesIn(GameConfig.RecipeDataDir));

        // -- 4. The vocabulary ceiling, as a check rather than a comment --
        // §19: eight materials, and "a ninth material wanting to exist is a signal that a recipe wants
        // redesigning". The yields are scoped to `ch*` because the ceiling is a rule about *authored
        // content* — the check name says so. That was not the original reason: the scoping was put here
        // to tolerate `herb_gravecap`, a ninth noun sitting on prototype ground, and unit 3b deleted
        // both. It is redundant now, and deliberately kept — see GateWorldGraph's spine block for the
        // one check that holds the redundancy true rather than leaving five sweeps assuming it.
        string[] roster =
        {
            "herb_bitterroot", "mat_tallow", "herb_graveflower",
            "mat_heartwood", "mat_pitch", "mat_scrap_iron", "mat_cured_hide", "mat_cairnstone",
        };

        var missingRoster = roster.Where(id => inv.GetItem(id)?.Category != ItemCategory.Component).ToList();
        Check($"the eight named materials all exist as components{Listing(missingRoster)}",
            missingRoster.Count == 0);

        // **A `Lore` page is the one sanctioned non-material yield, corrected 2026-08-05.** This swept *every*
        // `ItemId` against the eight and called anything else off-roster — but §19's ceiling is a rule about
        // **materials** (*"a ninth material wanting to exist is a signal that a recipe wants redesigning"*), and
        // a page is not a material. It is knowledge made into an object (decision 11), and `handoff.md` §2's own
        // queue instructs placing her three notes as exactly this: *"one `Harvest` hotspot each, with `ItemId`
        // set to the page id exactly."* So the check as written would have gone red on anyone who followed the
        // written instruction, which is how it was found — by following it.
        //
        // **It is the recurring class with the polarity flipped, and that is worth naming.** Every other instance
        // today was a check keyed on a *weaker* property than its rule and therefore passing on content the rule
        // forbids. This one was keyed on a **stronger** property than its rule, and so failed on content the rule
        // permits — the same defect, and the direction that gets a correct gate deleted by someone in a hurry
        // rather than quietly banking a bug.
        //
        // Widened to *material or page*, not to *anything in the catalogue*: a node paying out a Bell Maul or a
        // salve is still off-vocabulary and still caught, which is the coverage the old form had incidentally and
        // that must not be lost while fixing what it had wrongly.
        var offRoster = new List<string>();
        foreach (var (roomId, room) in rooms)
        {
            if (room.ChapterId is not ("ch1" or "ch2" or "ch3")) continue;
            foreach (var h in room.Hotspots ?? new Godot.Collections.Array<HotspotData>())
            {
                if (h is not { Kind: HotspotKind.Harvest or HotspotKind.Mend }) continue;
                if (string.IsNullOrEmpty(h.ItemId) || roster.Contains(h.ItemId)) continue;
                if (inv.GetItem(h.ItemId)?.Category == ItemCategory.Lore) continue;
                offRoster.Add($"{roomId}/{h.Id} -> {h.ItemId}");
            }
        }

        Check($"and no authored node pays out anything off the roster{Listing(offRoster)}",
            offRoster.Count == 0);

        // -- 5. The slot predicate, on its own since unit 8 --
        //
        // **It was a pair, and the partner is gone.** The second check read *"and is not the negation of
        // `SurvivesDeath`"*, which is how unit 4b proved the tally rule had not silently become a copy of the
        // death rule. `SurvivesDeath` was deleted with item loss, so that comparison has nothing to make —
        // and re-aiming it rather than deleting it is the point: what it was really protecting is that
        // **`CountsAgainstSlots` answers a question of its own**, which is still assertable by naming the two
        // ends the rule exists to separate.
        //
        // Totality first, and for a bool-returning `switch` with a `_` arm that can only be asked as *does
        // every member get an answer at all* — the predicate cannot throw, so the check is that it is total
        // rather than that it is correct.
        var categories = System.Enum.GetValues<ItemCategory>();

        Check($"CountsAgainstSlots answers for all {categories.Length} categories",
            categories.All(c => InventoryCraftingManager.CountsAgainstSlots(c) ||
                                !InventoryCraftingManager.CountsAgainstSlots(c)));

        // And it discriminates, which is what stops it being a constant wearing a switch. A tally entry and a
        // carried thing are the two ends `combat.md` §19 draws, so if these ever agree the rule has collapsed.
        Check("and it separates a tally entry from a carried thing",
            !InventoryCraftingManager.CountsAgainstSlots(ItemCategory.Component) &&
            InventoryCraftingManager.CountsAgainstSlots(ItemCategory.Equipment));
        Check("a component is a tally entry, not a carried thing",
            !InventoryCraftingManager.CountsAgainstSlots(ItemCategory.Component));
        Check("but a salve you carry occupies something",
            InventoryCraftingManager.CountsAgainstSlots(ItemCategory.Salve));

        // -- 6. Both directions of the venue rule, driven live --
        // Only the first can regress silently: a venue check that stopped refusing would let crafting
        // happen anywhere and no other gate would notice, whereas one that stopped allowing turns every
        // craft red at once.
        inv.ResetAll();
        world.ResetAll();
        inv.AddItem("mat_heartwood", 1);
        inv.AddItem("mat_scrap_iron", 1);
        inv.Discover("lore_craft_edges");

        nav.GoToRoom("verify_wilds");
        Check("a station recipe is refused where there is no station",
            inv.Inspect("recipe_tool_bill_hook") == CraftBlock.NotAtStation);

        // `ch2_forge_row`, and the room name is load-bearing rather than incidental. The anvil was in
        // `ch2_smithy` until 2026-08-05, which `ledger-ch2` row 10 had never agreed with — and the reason
        // it survived is that section 10's census keys on the station *id* and asks only whether some room
        // built it, so it was green either way. This line is now the only assertion in the suite about
        // *which* screen the one anvil stands on: move it back and this check goes red, which is the whole
        // reason to navigate to a real room here instead of a fixture. Same shape as section 3's boss-room
        // class — a check keyed on a weaker property than the ledger it enforces passes on content the
        // ledger forbids.
        nav.GoToRoom("ch2_forge_row");
        Check("and allowed at the anvil it names",
            inv.Inspect("recipe_tool_bill_hook") == CraftBlock.None);
        Check("while the mortar's work is not done at the anvil",
            InventoryCraftingManager.RoomHasStation(nav.CurrentRoom, "mortar") == false);

        nav.GoToRoom("verify_camp");
        Check("and the camp bench satisfies every station",
            inv.Inspect("recipe_tool_bill_hook") == CraftBlock.None);

        // -- 7. Crafting a tool sets its flag, and the flag opens the door authored against it --
        // Driven through the graph rather than asserted about the flag, because the flag agreeing with
        // itself is what the unit-1 authoring already guaranteed. What was never true until today is
        // that anything *sets* it: RecipeData had no SetsFlag, so no recipe in the project could open
        // anything. Reachability is the only phrasing that would have caught that.
        // The room named here is the door's own target, not the blight behind it. `ch1_stonefield` was
        // the obvious choice and is wrong: it sits behind the Bill Hook *and*
        // `freed.ch1.messenger_01`, so crafting one tool would not reach it and the gate would have
        // failed for a reason that had nothing to do with crafting. One door, one flag, one claim.
        Check("the bramble is shut before the tool exists", !Reaches(rooms, "ch1_bramble"));
        Check("the tool is made", inv.Craft("recipe_tool_bill_hook"));
        Check("which sets the flag its door was authored against",
            world.GetFlag("learned.tool_bill_hook"));
        Check("and the bramble opens", Reaches(rooms, "ch1_bramble"));

        // -- 8. A tool cannot be forged twice --
        Check("the same tool refuses a second forging",
            inv.Inspect("recipe_tool_bill_hook") == CraftBlock.AlreadyMade);
        Check("and the refusal survives losing the tool itself",
            inv.RemoveItem("tool_bill_hook", 1) &&
            inv.Inspect("recipe_tool_bill_hook") == CraftBlock.AlreadyMade);

        // -- 9. The unit 6 dependency, stated as a gate --
        // Every tool recipe needs a blueprint that nothing in the authored world hands over yet: `items.md`
        // §8 pays for each craft family with a Dream knowledge pocket, and pockets are unit 6. Until then
        // the honest state is *the recipe exists, the page exists, the flag works, and nothing gives it to
        // you*. This is what unit 6 turns green, and the failure is the notice to delete the debug key.
        //
        // **Re-pointed 2026-08-05, and the reason is the whole lesson of the page pass.** This check used to
        // read *"no authored `Lore` item has this id"* and it went red the day decision 11's seven
        // `lore_craft_*` files landed — correctly, by its own comment, and yet nothing about the world had
        // changed: no pocket grants one, no room holds one, and `F6` was still the only way to learn
        // anything. **It was testing a page's existence as a proxy for a page's reachability, and authoring
        // the pages broke the proxy without touching the truth.** So the two facts are now two checks. The
        // generalisation is worth more than the repair: a check standing on "the content does not exist yet"
        // measures the calendar, and it expires on the day the content arrives rather than the day the
        // mechanism does.
        inv.ResetAll();
        world.ResetAll();

        var toolRecipes = authored.Values
            .Where(r => r.SetsFlag.StartsWith("learned.tool_"))
            .ToList();

        Check($"all three access tools are authored as recipes ({toolRecipes.Count} of 3)",
            toolRecipes.Count == 3);
        Check("each names a blueprint",
            toolRecipes.All(r => r.NeedsBlueprint));

        // The first of the two facts, and it is the one the old check had backwards. A page per family is
        // decision 11's bill; deleting one of the seven files turns this red, where before it turned the
        // suite *green*.
        var pagelessTools = toolRecipes
            .Where(r => inv.GetItem(r.RequiredBlueprintId)?.Category != ItemCategory.Lore)
            .Select(r => r.RequiredBlueprintId).ToList();
        Check($"and each has a page authored for it to be written on{Listing(pagelessTools)}",
            pagelessTools.Count == 0);

        // **The second half was the other self-retiring ledger, and unit 10 retired it, 2026-08-05.** It read
        // *"but nothing in the world hands one over yet — a pocket or a placed page turns this red"*, and the
        // fourteen pockets turned it red on cue. The claim it existed to make visible — that the three access
        // tools were unreachable without `F6` — is now false in the good direction, so it inverts: **each tool's
        // page is handed over by something in the world.**
        //
        // The same `PagesTheWorldHandsOver` sweep, read the other way round, so a pocket repointed at a
        // different family or an entrance deleted from a screen turns this red rather than quietly returning the
        // three tools to being debug-only. That is the failure this used to be pointed at, and it is a worse one
        // now that the mechanism exists: before, unreachable was the honest state.
        var handedOver = PagesTheWorldHandsOver(rooms);
        var unlearnable = toolRecipes
            .Where(r => !handedOver.Contains(r.RequiredBlueprintId))
            .Select(r => r.RequiredBlueprintId).ToList();
        Check($"and something in the world hands each of them over"
            + $" ({handedOver.Count} pages reachable){Listing(unlearnable)}",
            unlearnable.Count == 0);

        // Unchanged and now doing more work than it was: the pages are reachable, so *knowledge is still the
        // gate* has to be re-proved rather than assumed. A fresh game has walked nowhere, so every tool is still
        // unknown — which is the distinction between a page existing in the world and a page being in your hand.
        Check("so every tool is unknown on a fresh game",
            toolRecipes.All(r => inv.Inspect(r.Id) == CraftBlock.Unknown));

        // And the debug key that stands in for the pockets. Read off the registry rather than a list of
        // names, so unit 4b's recipes are covered the moment they are authored.
        foreach (var r in toolRecipes) inv.Discover(r.RequiredBlueprintId);
        Check("learning every craft family makes all three known",
            toolRecipes.All(r => inv.Inspect(r.Id) != CraftBlock.Unknown));
        Check("the debug key it runs from is bound",
            InputMap.HasAction("debug_learn_all"));

        // Both halves of the scaffolding, gated together. Learning every family is useless without a way
        // to spend it, which is the defect Jeremy walked into on 2026-08-03: materials gathered, every
        // recipe known, and no reachable verb. An unbound action is a silent no-op — `IsActionPressed`
        // on a name that is not in the map returns false rather than throwing — so the failure mode is a
        // key that simply does nothing, which is indistinguishable from the bug it exists to work around.
        Check("and so is the one that spends what it learned",
            InputMap.HasAction("debug_craft_here"));

        // The third of the same family, bound 2026-08-03: `EncounterManager.Rig` had no caller outside this
        // file, and the fight screen cannot be the one — rigging is free before a fight and costs a turn
        // inside it (§18), so the verb belongs on the room. A key stands in until the satchel grows a Set
        // button, the same way the two above stand in for the journal. Checked here rather than in the
        // encounters section because what is being asserted is scaffolding, not combat: the mechanism's
        // rules are gated thoroughly a thousand lines up, and none of that made it reachable.
        Check("and the one that reaches the rig, which nothing else does",
            InputMap.HasAction("debug_rig_here"));

        // -- 10. Every station id agrees in both directions --
        // A typo is otherwise a bench that silently makes nothing: the hotspot draws and takes the
        // click, and the panel it opens is empty. Neither direction is redundant — the first catches a
        // recipe naming a station nobody built, the second a station nobody can use.
        var worldStations = new Dictionary<string, string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots ?? new Godot.Collections.Array<HotspotData>())
                if (h != null && !string.IsNullOrEmpty(h.StationId))
                    worldStations[h.StationId] = $"{roomId}/{h.Id}";

        var namedByRecipe = authored.Values
            .Where(r => !string.IsNullOrEmpty(r.RequiresStation))
            .Select(r => r.RequiresStation)
            .ToHashSet();

        var unbuilt = namedByRecipe.Where(s => !worldStations.ContainsKey(s)).ToList();
        var unused = worldStations.Keys.Where(s => !namedByRecipe.Contains(s)).ToList();

        Check($"every station a recipe names exists in the world{Listing(unbuilt)}", unbuilt.Count == 0);
        Check($"and every station in the world is named by a recipe{Listing(unused)}", unused.Count == 0);

        // A Station hotspot with no id is the one authoring slip neither direction above can see: it
        // belongs to no station, so it appears in no list and opens an unfilterable panel.
        var namelessStations = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots ?? new Godot.Collections.Array<HotspotData>())
                if (h is { Kind: HotspotKind.Station } && string.IsNullOrEmpty(h.StationId))
                    namelessStations.Add($"{roomId}/{h.Id}");

        Check($"and no station hotspot is nameless{Listing(namelessStations)}",
            namelessStations.Count == 0);

        // -- 11. With all three tools, every blight is reachable --
        // The exact complement of the mending section's check that three of the twelve are *not*. That
        // one proves the tool doors are load-bearing; this one proves they are the only *permanent*
        // thing in the way, so the falling-action activity the good ending is costed against can
        // actually be finished.
        //
        // Ordinary progression has to be granted first, or this measures the wrong thing: half the
        // world sits behind narrative flags (`freed.ch1.messenger_01` and its kin) which play grants
        // and which say nothing about tools. So every flag any door asks for is set *except* the tool
        // flags, the tools are then added, and what remains unreachable is unreachable for a reason no
        // amount of playing fixes. Granting them wholesale is safe here precisely because it is the
        // end of the section and the flags are reset immediately after.
        foreach (var room in rooms.Values)
            foreach (var door in Doors(room))
                foreach (var flag in Gates(door))
                    if (!flag.StartsWith("learned.tool_"))
                        world.SetFlag(flag);

        var blightRooms = rooms
            .Where(pair => pair.Value.Hotspots?.Any(h => h is { Kind: HotspotKind.Mend }) == true)
            .Select(pair => pair.Key)
            .ToList();

        // Non-vacuity, and this section is the last place entitled to skip it: an empty sweep makes
        // the check below pass by finding nothing, which is the fifth blind-spot class again.
        Check($"the twelve blights are all still here ({blightRooms.Count} of 12)",
            blightRooms.Count == 12);

        var beforeTools = blightRooms.Where(id => !Reaches(rooms, id)).OrderBy(id => id).ToList();
        Check($"three of them are still shut with progression alone{Listing(beforeTools)}",
            beforeTools.Count == 3);

        world.SetFlag("learned.tool_bill_hook");
        world.SetFlag("learned.tool_pry_bar");
        world.SetFlag("learned.tool_mine_pick");

        var unreachableBlights = blightRooms.Where(id => !Reaches(rooms, id)).OrderBy(id => id).ToList();
        Check($"and the three tools open the last of them{Listing(unreachableBlights)}",
            unreachableBlights.Count == 0);

        inv.ResetAll();
        world.ResetAll();
        nav.GoToRoom("verify_camp");
    }

    /// <summary>
    /// The arsenal — unit 4b's 14 items and 13 recipes, checked against the rules `items.md` §§2–4
    /// and `combat.md` §19 currently state only in prose. Added 2026-08-03 with the content itself.
    /// </summary>
    /// <remarks>
    /// <b>Every check here replaces a sentence in a design doc</b>, which is the only reason a content
    /// sweep earns 30 gates. The four that would fail *dangerously* — quietly, in a way no other gate
    /// and no amount of playing would reveal — are worth naming, because they are why this section
    /// exists rather than being folded into the stations sweep:
    /// <list type="number">
    /// <item>An <c>Equipment</c> item with <see cref="ItemData.Slot"/> left at
    /// <c>None</c>. It catalogues, it crafts, it displays; it simply can never be worn, and the symptom
    /// arrives in unit 3 as "the loadout screen is missing a coat".</item>
    /// <item>A weapon dealing Fire or Caustic. §4 keeps those two types on the brew side *because* that
    /// is what makes traps worth rigging at all — one Fire axe and the whole trap roster becomes
    /// optional, with nothing anywhere reading as broken.</item>
    /// <item>A Fused trap that triggers itself. It works, it does damage, and the two-step ignition
    /// combination <see cref="GameConfig.RigSlotsPerFight"/> was set to two to permit stops existing.</item>
    /// <item>An item nothing in the world can pay for. The closure at the bottom is the general form of
    /// the Snare bug §3 records — *"chapter 1 could build no trap at all"* — and it is the check that
    /// found <c>catalyst_ashincense</c> to be unobtainable on its first run.</item>
    /// </list>
    /// <para>
    /// <b>Read off the catalogue, never off a list of ids</b>, with two deliberate exceptions: the seven
    /// Craft family ids and the one known obtainability hole. Both are *census* checks, where the point
    /// is precisely that a hardcoded expectation disagrees with the content — a sweep that derived the
    /// family list from the recipes could never notice a typo inventing an eighth family, and it is the
    /// check that would have caught the Pry Bar sharing <c>lore_craft_edges</c> in either direction.
    /// </para>
    /// </remarks>
    private static void GateRoster(Dictionary<string, RoomData> rooms)
    {
        Section("roster  the arsenal, the two hands that make it, and whether you can reach any of it");

        var inv = InventoryCraftingManager.Instance;
        var world = WorldStateManager.Instance;
        var nav = NavigationManager.Instance;

        var items = inv.Catalogue.Values.Where(i => i != null).ToList();

        // Same `verify_` filter as the stations sweep, same reason: a fixture is not content.
        var authored = inv.Recipes
            .Where(pair => !pair.Key.StartsWith("verify_") && pair.Value != null)
            .Select(pair => pair.Value)
            .ToList();

        // -- 1. The shape of the roster, so nothing below can pass by sweeping an empty set --
        var traps = items.Where(i => i.Category == ItemCategory.Trap).ToList();
        var equipment = items.Where(i => i.Category == ItemCategory.Equipment).ToList();
        var weapons = equipment.Where(i => i.Slot == EquipSlot.Weapon).ToList();
        var armours = equipment.Where(i => i.Slot == EquipSlot.Armour).ToList();

        Check($"the arsenal is all here ({weapons.Count} weapons, {armours.Count} armours, {traps.Count} traps)",
            weapons.Count == 2 && armours.Count == 3 && traps.Count == 6);

        // -- 2. Equipment totality, over all four combinations --
        var slotless = equipment.Where(i => i.Slot == EquipSlot.None).Select(i => i.Id).ToList();
        Check($"every equippable names a slot{Listing(slotless)}", slotless.Count == 0);

        var wrongSlot = items
            .Where(i => i.Category != ItemCategory.Equipment && i.Slot != EquipSlot.None)
            .Select(i => $"{i.Id} is {i.Category} in the {i.Slot} slot").ToList();
        Check($"and nothing that is not equipment names one{Listing(wrongSlot)}", wrongSlot.Count == 0);

        var badWeapons = weapons
            .Where(i => i.Deals == DamageType.None || i.WardsAgainst != DamageType.None)
            .Select(i => $"{i.Id} deals {i.Deals} and wards {i.WardsAgainst}").ToList();
        Check($"a weapon deals a type and wards nothing{Listing(badWeapons)}", badWeapons.Count == 0);

        var badArmours = armours
            .Where(i => i.WardsAgainst == DamageType.None || i.Deals != DamageType.None)
            .Select(i => $"{i.Id} wards {i.WardsAgainst} and deals {i.Deals}").ToList();
        Check($"and armour wards a type and deals nothing{Listing(badArmours)}", badArmours.Count == 0);

        // -- 3. The two types that belong to her, and the hole that is on purpose --
        var armedWithBrew = weapons
            .Where(i => i.Deals is DamageType.Fire or DamageType.Caustic)
            .Select(i => $"{i.Id} deals {i.Deals}").ToList();
        Check($"no weapon deals Fire or Caustic — those are what a trap is for{Listing(armedWithBrew)}",
            armedWithBrew.Count == 0);

        var fireproof = armours.Where(i => i.WardsAgainst == DamageType.Fire).Select(i => i.Id).ToList();
        Check($"and no coat wards Fire — the gap is deliberate{Listing(fireproof)}", fireproof.Count == 0);

        // Non-vacuity for the pair above: three coats covering one type each is what makes "no Fire coat"
        // a decision rather than an accident of there being nothing to ward with.
        var warded = armours.Select(i => i.WardsAgainst).Distinct().OrderBy(t => t).ToList();
        Check($"the three coats cover three different types ({string.Join(", ", warded)})",
            warded.Count == armours.Count &&
            warded.SequenceEqual(new[] { DamageType.Edge, DamageType.Blunt, DamageType.Caustic }));

        // -- 4. §11's no-tiers rule, as arithmetic --
        // A body is EntityData.Durability 30 and one clean blow is 10, so a second weapon hitting for 12
        // would not read as a different tactic — it would read as an upgrade, which is the one thing the
        // damage model refuses to have.
        var potencies = weapons.Select(i => i.Potency).Distinct().OrderBy(p => p).ToList();
        Check($"both weapons hit for the same amount ({string.Join(", ", potencies)}) — §11 has no tiers",
            potencies.Count == 1 && potencies[0] > 0);

        var pricedArmour = armours.Where(i => i.Potency != 0).Select(i => $"{i.Id} = {i.Potency}").ToList();
        Check($"and a coat carries no magnitude — WardsAgainst halves, which needs none{Listing(pricedArmour)}",
            pricedArmour.Count == 0);

        // -- 5. The Fused/Mechanical split, as a check instead of a table --
        // Stated as an equivalence so both directions fail: a Mechanical trap that needs lighting is as
        // wrong as a Fused one that does not, and neither is visible in the inspector.
        var wrongTrigger = traps
            .Where(i => i.TriggersItself == (i.Deals is DamageType.Fire or DamageType.Caustic))
            .Select(i => $"{i.Id} deals {i.Deals} and {(i.TriggersItself ? "triggers itself" : "needs lighting")}")
            .ToList();
        Check($"a Fused trap needs a second step and a Mechanical one does not{Listing(wrongTrigger)}",
            wrongTrigger.Count == 0);

        var fused = traps.Count(i => !i.TriggersItself);
        Check($"and the split is two against four ({fused} fused, {traps.Count - fused} mechanical)",
            fused == 2 && traps.Count - fused == 4);

        var stackedTraps = traps.Where(i => i.MaxStack != 1).Select(i => $"{i.Id} x{i.MaxStack}").ToList();
        Check($"a trap is an owned thing, count 0 or 1 — §19 made them permanent{Listing(stackedTraps)}",
            stackedTraps.Count == 0);

        // -- 5b. What the trap roster is *for*, as a census rather than a universal --
        // <see cref="GateCombatShapes"/> used to demand that every trap deal damage, which was vacuously
        // green until this roster landed and then red against correct content — `items.md` §3 authors two
        // that deal nothing on purpose. The claim that actually holds is coverage: four traps, four
        // distinct types, and the only route to two of them. Stated as an ordered set so a fifth damaging
        // trap doubling up on Edge fails too, because a duplicate is a trap with no reason to be rigged.
        var trapTypes = traps.Where(i => i.Deals != DamageType.None)
            .Select(i => i.Deals).Distinct().OrderBy(t => t).ToList();
        Check($"the four armed traps cover four different types ({string.Join(", ", trapTypes)})",
            trapTypes.Count == traps.Count(i => i.Deals != DamageType.None) &&
            trapTypes.SequenceEqual(new[]
                { DamageType.Edge, DamageType.Blunt, DamageType.Fire, DamageType.Caustic }));

        // And the two that deal nothing are the two the design names. A universal would be unfalsifiable
        // here — there is no field yet for *holds* or *reveals* (that is unit 3's rig model), so nothing
        // could distinguish a utility trap from a half-authored one. Naming them is what makes an inert
        // seventh trap fail, and the reverse arm makes arming one of these two fail as well.
        string[] utilityTraps = { "trap_snare", "trap_chime" };

        var inert = traps.Where(i => i.Deals == DamageType.None).Select(i => i.Id).OrderBy(id => id).ToList();
        var inertDrift = inert.Where(id => !utilityTraps.Contains(id)).Select(id => $"unexpectedly inert {id}")
            .Concat(utilityTraps.Where(id => !inert.Contains(id)).Select(id => $"{id} now deals damage"))
            .ToList();
        Check($"and the two that deal nothing are the Snare and the Chime{Listing(inertDrift)}",
            inertDrift.Count == 0);

        // -- 6. The blueprint census, and §8's bill of eight pockets --
        var families = authored
            .Where(r => r.NeedsBlueprint)
            .Select(r => r.RequiredBlueprintId)
            .Distinct()
            .ToList();

        var untracked = families
            .Where(f => !f.StartsWith("lore_craft_") && !f.StartsWith("lore_physick_")).ToList();
        Check($"every blueprint id says which hand it belongs to{Listing(untracked)}", untracked.Count == 0);

        // The hardcoded half, on purpose — see the remarks. A typo otherwise invents a silent family
        // that no pocket will ever grant, and the recipe behind it is unlearnable forever.
        string[] expectedCraft =
        {
            "lore_craft_edges", "lore_craft_prying", "lore_craft_stoneworking",
            "lore_craft_mechanisms", "lore_craft_fusion", "lore_craft_forging", "lore_craft_leatherwork",
        };

        var craft = families.Where(f => f.StartsWith("lore_craft_")).ToList();
        var familyDrift = craft.Where(f => !expectedCraft.Contains(f)).Select(f => $"unexpected {f}")
            .Concat(expectedCraft.Where(f => !craft.Contains(f)).Select(f => $"missing {f}"))
            .ToList();

        Check($"his seven craft families are exactly the seven expected{Listing(familyDrift)}",
            familyDrift.Count == 0);
        Check($"which is §8's bill of eight pockets less the unauthored engine ({craft.Count} + 1 of 8)",
            craft.Count + 1 == 8);

        var physick = families.Where(f => f.StartsWith("lore_physick_")).ToList();
        Check($"and hers are three pages of her notes rather than pockets ({physick.Count} of 3)",
            physick.Count == 3);

        // -- 7. The salve economy §2 leaves as a sentence --
        var restorers = items.Where(i => i.VitalityRestore > 0f).ToList();
        var wrongRestorer = restorers
            .Where(i => i.Category != ItemCategory.Salve)
            .Select(i => $"{i.Id} is {i.Category} and restores {i.VitalityRestore}").ToList();
        Check($"only a salve closes a wound{Listing(wrongRestorer)}", wrongRestorer.Count == 0);

        var emptySalve = items
            .Where(i => i.Category == ItemCategory.Salve && i.VitalityRestore <= 0f)
            .Select(i => i.Id).ToList();
        Check($"and every salve closes one ({restorers.Count} of them){Listing(emptySalve)}",
            emptySalve.Count == 0 && restorers.Count == 2);

        var clearmind = inv.GetItem("salve_clearmind");
        var deepmend = inv.GetItem("salve_deepmend");
        var clearmindCost = MaterialUnits(inv.GetRecipe("recipe_salve_clearmind"));
        var deepmendCost = MaterialUnits(inv.GetRecipe("recipe_salve_deepmend"));

        Check($"Deepmend is the bigger salve ({deepmend?.VitalityRestore} against {clearmind?.VitalityRestore})",
            clearmind != null && deepmend != null && deepmend.VitalityRestore > clearmind.VitalityRestore);
        Check($"and it costs more to brew ({deepmendCost} units against {clearmindCost})",
            deepmendCost > clearmindCost && clearmindCost > 0);

        // The guard that keeps the lesser salve alive, and the one §2 actually asks for: two slots is a
        // real cap, so a Deepmend that were *also* the material-efficient choice would make Clearmind
        // dead content the moment you learned the better page. Clearmind wins per unit of herb; Deepmend
        // wins per slot carried into the fight. Neither dominates, so both stay worth brewing.
        Check("but Clearmind heals more per unit of material, so neither salve dominates the other",
            clearmind != null && deepmend != null && clearmindCost > 0 && deepmendCost > 0 &&
            clearmind.VitalityRestore / clearmindCost > deepmend.VitalityRestore / deepmendCost);

        // -- 8. Which hand made it --
        // Only checked for things that are *made*. A found material has no maker, so its Track is
        // meaningless and asserting one would be inventing a rule to have something to check.
        var wrongTrack = new List<string>();
        foreach (var i in items)
        {
            var expected = i.Category switch
            {
                ItemCategory.Salve => KnowledgeTrack.Physick,
                ItemCategory.Trap or ItemCategory.Equipment or ItemCategory.Relic => KnowledgeTrack.Craft,
                ItemCategory.Component when i.Id.StartsWith("brew_") => KnowledgeTrack.Physick,
                _ => i.Track,
            };

            if (i.Track != expected) wrongTrack.Add($"{i.Id} is filed under {i.Track}, should be {expected}");
        }

        Check($"every made thing is filed under the hand that makes it{Listing(wrongTrack)}",
            wrongTrack.Count == 0);
        Check("and both hands own something",
            items.Any(i => i.Track == KnowledgeTrack.Craft) &&
            items.Any(i => i.Track == KnowledgeTrack.Physick));

        // -- 9. The permanence rule, driven live --
        // Asserted about the running bench rather than about the fields, because `AlreadyMade`'s two arms
        // read different storage — a flag for a repair, what you own for a trap or a coat — and only one of
        // them is exercised by the tool recipes the stations sweep already covers.
        //
        // **This block used to assert the opposite for equipment** — *"and a spare is allowed, the cache
        // exists partly to bank one"* — and it is worth leaving that on the record, because a green gate
        // asserting a repealed rule is worse than no gate: it is a check standing guard over the bug. The
        // reason it cited had already been struck by §19 in the same breath as equipment began surviving
        // death (*"the shared cache is retired… equipment surviving death removes that purpose"*), and
        // nothing re-read this check when that landed. Jeremy found it by playing, 2026-08-07.
        inv.ResetAll();
        world.ResetAll();
        nav.GoToRoom("verify_camp");

        inv.Discover("lore_craft_mechanisms");
        inv.Discover("lore_craft_leatherwork");
        inv.AddItem("mat_heartwood", 2);
        inv.AddItem("mat_pitch", 2);
        inv.AddItem("mat_cured_hide", 6);

        Check("a trap with no station named is built at the camp bench", inv.Craft("recipe_trap_snare"));
        Check("and refuses a second, because you already own the one",
            inv.Inspect("recipe_trap_snare") == CraftBlock.AlreadyMade);
        Check("with the materials for another still in the satchel, so the refusal is not poverty",
            inv.GetCount("mat_heartwood") == 1 && inv.GetCount("mat_pitch") == 1);

        Check("a coat is made", inv.Craft("recipe_armour_tanners_coat"));
        Check("and refuses a second, on the same terms as the trap",
            inv.Inspect("recipe_armour_tanners_coat") == CraftBlock.AlreadyMade);
        Check("with the hide for another still to hand, so that refusal is not poverty either",
            inv.GetCount("mat_cured_hide") >= 3);

        // The cache is retired but `Deposit` still works, so it is a live route to launder the permanence
        // rule: put the one you own in the box and the bench sees an empty satchel. Both arms, because the
        // trap arm shipped with this hole and only the coat was reported.
        Check("the snare goes into the cache", inv.Deposit("trap_snare") && inv.GetCount("trap_snare") == 0);
        Check("and the bench still refuses it — owning one anywhere is owning one",
            inv.Inspect("recipe_trap_snare") == CraftBlock.AlreadyMade);
        Check("a banked coat is refused the same way",
            inv.Deposit("armour_tanners_coat") &&
            inv.Inspect("recipe_armour_tanners_coat") == CraftBlock.AlreadyMade);
        // The other side of the rule, made twice rather than merely inspected: a `!= AlreadyMade` on a
        // recipe nobody has learned would pass on `Unknown` and measure nothing.
        inv.Discover("lore_physick_deepmend");
        inv.AddItem("herb_bitterroot", 6);
        inv.AddItem("mat_tallow", 4);
        Check("a salve is still repeatable, so permanence did not leak into the stackables",
            inv.Craft("recipe_salve_deepmend") && inv.Craft("recipe_salve_deepmend") &&
            inv.GetCount("salve_deepmend") == 2);

        // -- 10. The field nothing authored sets yet, and the reason is not just "not yet" --
        // (It is no longer unread: `Inspect` refuses on it as `CraftBlock.HalfKnown` since 2026-08-05, gated
        // against fixtures in `GateCraftingPanel`. What is still empty is the *authored* side of it.)
        // `combat.md` §19 says a fused trap "cannot be made by either track alone", which reads like a
        // standing order to set this on the Oil Slick and the Vitriol Flask. It would be wrong, and the
        // same section says why forty lines further down: her brews are **an authored cache she left you**,
        // in your hands from the first hour, and learning to make them is late-game. A flag demanding you
        // know her recipe would make the oil she left unusable until you could already replace it —
        // deleting the inversion §19 calls "the best thing here".
        //
        // So the two-hand requirement on a fused trap is carried by the *ingredient*, which is exactly the
        // right resolution: "you needed her chemistry" is a thing an input can say. What no input can say
        // is "you need two blueprints", and that is the exorcism engine — a Dream pocket for his Craft and
        // one of her notes for her Physick (`items.md` §8). This field is reserved for that one recipe, and
        // authoring it is what turns this red.
        // **Retired 2026-08-05, by the thing it was waiting for.** It read "nothing needs two blueprints
        // yet — the exorcism engine turns this red", and the engine landed, so the negative form has done
        // its whole job. What replaces it is the same claim from the other side, and it is the stronger
        // one: `items.md` §6 says the engine is *"the only recipe requiring both tracks"*, so a second one
        // appearing is the thing now worth a red line. The lesson this pays for a second time is that a
        // self-retiring check has to be retired *by whoever trips it*, or it reads as a regression.
        var bothHands = authored.Where(r => r.RequiresBothTracks).Select(r => r.Id).OrderBy(id => id).ToList();
        Check($"the exorcism engine is the one recipe that needs both hands{Listing(bothHands)}",
            bothHands.Count == 1 && bothHands[0] == "recipe_repair_exorcism_engine");

        // Non-vacuity for the claim above: a fused trap is priced in a brew, so its two-hand requirement
        // is real and merely stored somewhere other than the flag. Without this the check would be
        // satisfied by a roster that had no fused traps at all.
        var fusedRecipes = authored
            .Where(r => inv.GetItem(r.OutputItemId)?.Deals is DamageType.Fire or DamageType.Caustic)
            .ToList();
        var unfused = fusedRecipes
            .Where(r => !r.Inputs.Any(i => i != null && i.ItemId.StartsWith("brew_")))
            .Select(r => r.Id).ToList();
        Check($"each of the {fusedRecipes.Count} fused traps is priced in one of her brews instead{Listing(unfused)}",
            fusedRecipes.Count == 2 && unfused.Count == 0);

        // -- 11. Obtainability, as a closure over the world --
        // Invariant 12 as a check. The seed is what the ground pays out plus what he walks in owning;
        // everything else has to be earned through a recipe whose inputs are already in the set.
        var obtainable = new HashSet<string>(GameConfig.StartingItems);
        foreach (var room in rooms.Values)
            foreach (var h in room.Hotspots ?? new Godot.Collections.Array<HotspotData>())
                if (h is { Kind: HotspotKind.Harvest or HotspotKind.Mend } && !string.IsNullOrEmpty(h.ItemId))
                    obtainable.Add(h.ItemId);

        // The third source, added 2026-08-05 with decision 11's pages: a knowledge pocket hands over the page
        // for the family it teaches, so a pocket is a way to *get an item* and not only a way to learn one.
        // Contributes nothing today — there are no authored knowledge pockets — and the point of wiring it now
        // is that the seven `lore_craft_*` pages then become reachable the day their pocket lands, with no gate
        // to remember to widen. The one collection that could have gone stale here is the closure's idea of
        // what a source is, which is exactly what went stale in `GateStations`.
        foreach (var page in PagesTheWorldHandsOver(rooms))
            obtainable.Add(page);

        // Not scoped to `ch*`, unlike the roster arm of the stations sweep: that one asks what the
        // authored chapters *should* pay out, this one asks what the player can actually get hold of.
        // The distinction was easier to see when the two sets differed — `herb_gravecap` used to be
        // obtainable on prototype ground and off the roster at the same time — and it still holds, so
        // the widest reading stays deliberate rather than accidental.
        Check($"the ground pays out something to start from ({obtainable.Count} ids before any bench)",
            obtainable.Count > GameConfig.StartingItems.Length);

        // A fixed point rather than one pass, because the arsenal is two deep: Oil Slick is priced in
        // Burning Oil, which is itself brewed, so a single sweep would call the trap unreachable.
        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var r in authored)
            {
                if (r.RepairsWorld || string.IsNullOrEmpty(r.OutputItemId)) continue;
                if (obtainable.Contains(r.OutputItemId)) continue;
                if (r.Inputs.Any(i => i == null || !obtainable.Contains(i.ItemId))) continue;

                obtainable.Add(r.OutputItemId);
                grew = true;
            }
        }

        // The one hole, named rather than scoped away. `catalyst_ashincense` is the only Catalyst in the
        // game and has no recipe: the catalyst economy — *craftable, so a failed heist never softlocks* —
        // is unbuilt, and line 1249 of this very file already calls it "craftable" in a comment. Listed
        // here so a *second* unobtainable item cannot arrive quietly, and checked from both sides so
        // authoring the recipe also turns this red, which is the notice to delete the entry.
        string[] knownHoles = { "catalyst_ashincense" };

        // **Pages are a class, not ten more entries in `knownHoles` — 2026-08-05.** Decision 11's ten
        // `lore_*` files are all unreachable, and naming them one by one would take a gate whose whole virtue
        // is *"there is exactly one hole and here it is"* and turn it into a list nobody reads. They also
        // retire on two different events rather than one: his seven when a knowledge pocket is authored, her
        // three when Jeremy places them in a room. So the ledger below keeps the single genuine one-off, and
        // the pages get their own check with their own reason.
        var pages = items.Where(i => i.Category == ItemCategory.Lore).ToList();

        var newHoles = items
            .Where(i => i.Category != ItemCategory.Lore)
            .Select(i => i.Id)
            .Where(id => !obtainable.Contains(id) && !knownHoles.Contains(id))
            .OrderBy(id => id).ToList();
        var closedHoles = knownHoles.Where(id => obtainable.Contains(id)).ToList();

        Check($"every item in the catalogue can be reached from the ground{Listing(newHoles)}",
            newHoles.Count == 0);
        Check($"and the only one that cannot is still the catalyst{Listing(closedHoles)}",
            closedHoles.Count == 0);

        // **This was a self-retiring ledger and it retired on schedule, 2026-08-05.** It read *"and no blueprint
        // page has a source yet, all 10 of them — a pocket or a placed page turns this red"*, and unit 10's
        // fourteen pockets turned it red exactly as its own message predicted. Its job was to make an
        // unreachability visible; the unreachability is gone for his seven families, so the claim is inverted
        // rather than deleted — a check that only ever said *"nothing yet"* has nothing to say once something
        // does.
        //
        // **Which pages are expected to have a source is derived, not listed.** A knowledge pocket's
        // `GrantsBlueprintId` *is* the page id (decision 11), so the set of paged-from-a-pocket ids comes from
        // the pockets themselves — a hardcoded list of seven would go stale the day a pocket is repointed, and
        // would go stale silently, since a stale list still passes on the content it was written against. Her
        // three notes are excluded because they are *found*, and placing them is not unit 10's job; the row in
        // `--verify-content` tracks that.
        var pagesFromPockets = (PocketManager.Instance?.Pockets.Values ?? new List<PocketData>())
            .Where(p => p != null && !string.IsNullOrEmpty(p.GrantsBlueprintId))
            .Select(p => p.GrantsBlueprintId)
            .ToHashSet();

        var unsourced = pages
            .Where(i => pagesFromPockets.Contains(i.Id) && !obtainable.Contains(i.Id))
            .Select(i => i.Id).OrderBy(id => id).ToList();
        Check($"every page a pocket grants is reachable from the ground, all {pagesFromPockets.Count} of them"
            + $"{Listing(unsourced)}",
            pagesFromPockets.Count > 0 && unsourced.Count == 0);

        var unpayable = authored
            .Where(r => r.Inputs.Any(i => i == null || !obtainable.Contains(i.ItemId)))
            .Select(r => r.Id).ToList();
        Check($"and every recipe is priced in things the world pays out{Listing(unpayable)}",
            unpayable.Count == 0);

        inv.ResetAll();
        world.ResetAll();
        nav.GoToRoom("verify_camp");
    }

    /// <summary>
    /// How many units of material a recipe costs, summed across its inputs. Used by
    /// <see cref="GateRoster"/> for the one comparison `items.md` §2 makes between the two salves —
    /// *"5 against 3"* — which is about the total weight of herb, not the number of distinct lines.
    /// </summary>
    private static int MaterialUnits(RecipeData recipe) =>
        recipe?.Inputs == null ? 0 : recipe.Inputs.Where(i => i != null).Sum(i => i.Amount);

    /// <summary>
    /// Every blueprint page an <b>authored</b> source actually puts in the player's hands, by whichever of
    /// the two routes decision 11 allows: a knowledge pocket that grants the family, or a page lying in a
    /// room. Empty as of 2026-08-05, and saying so is its job.
    /// </summary>
    /// <remarks>
    /// <b>One helper rather than two copies, because two collections name each other here and the crossing
    /// is what nobody writes on their own.</b> The ten pages authored with decision 11 are objects with ids
    /// that recipes already required — so *page exists* and *page reachable* are separate facts, and the
    /// second one has two possible sources sitting in two different registries. Writing the union out twice
    /// (once for <see cref="GateStations"/>'s unit 6 notice, once for <see cref="GateRoster"/>'s obtainability
    /// closure) is how they end up disagreeing about whether a pocket counts.
    /// <para>
    /// <b><c>verify_</c> is excluded on both arms and that is load-bearing here, not hygiene.</b> A gate must
    /// stand on ground the verifier owns, and the inverse applies just as hard to a gate whose whole claim is
    /// *"nothing authored does this yet"* — a synthetic pocket registered by some unrelated fixture would
    /// satisfy the claim and read as content. That is the same trap the pocket file census fell into during
    /// unit 6b, in the one shape where a fixture is indistinguishable from content unless filtered by name.
    /// </para>
    /// <para>
    /// <b>The <c>Mend</c> arm is deliberate rather than copied.</b> Mending pays out an item, so a page could
    /// in principle be the reward for repairing something — the closure in <see cref="GateRoster"/> already
    /// treats the two hotspot kinds alike, and diverging here would make the notice quietly narrower than the
    /// invariant it stands in for.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b><c>internal</c> rather than private since 2026-08-05, so <see cref="ContentReport"/> can ask the same
    /// question.</b> Its *"blueprint-gated recipes with no in-world source"* line had been counting gated
    /// recipes instead of unreachable ones; the fix needed this exact set, and a second copy of "what the world
    /// hands over" is the drift `GrantPage` and decision 11 exist to avoid. One home, two tools.
    /// </remarks>
    internal static HashSet<string> PagesTheWorldHandsOver(Dictionary<string, RoomData> rooms)
    {
        var pages = new HashSet<string>();
        var inv = InventoryCraftingManager.Instance;
        if (inv == null) return pages;

        bool IsPage(string id) => inv.GetItem(id)?.Category == ItemCategory.Lore;

        foreach (var id in GameConfig.StartingItems)
            if (IsPage(id)) pages.Add(id);

        foreach (var (roomId, room) in rooms)
        {
            if (roomId.StartsWith("verify_") || room?.Hotspots == null) continue;

            foreach (var h in room.Hotspots)
                if (h is { Kind: HotspotKind.Harvest or HotspotKind.Mend } && IsPage(h.ItemId))
                    pages.Add(h.ItemId);
        }

        // **Which pockets the player can actually open, and this clause was missing until 2026-08-05.** The
        // sweep used to credit every knowledge pocket's page whether or not anything in the world opened that
        // pocket — so on the day the fourteen `PocketData` files were authored with no entrances placed, it
        // reported *7 pages reachable* and every page was in fact unreachable. Caught by reading the count in a
        // passing check's own message, not by a failure.
        //
        // **It is the recurring class in its exact form: a check keyed on a weaker property than the thing it
        // enforces.** *"A pocket grants this page"* is not *"the world hands this page over"* — the missing term
        // is the door, and the weaker reading is the one that feels like it is not hardcoding anything. Same
        // shape as the boss placement gate that asked whether a body stood in *some* room, and the station census
        // that asked whether *some* room built the anvil.
        var openablePockets = rooms
            .Where(kv => !kv.Key.StartsWith("verify_") && kv.Value?.Hotspots != null)
            .SelectMany(kv => kv.Value.Hotspots)
            .Where(h => h is { Kind: HotspotKind.CrossToDream })
            .Select(h => h.PocketId)
            .ToHashSet();

        foreach (var (pocketId, pocket) in PocketManager.Instance?.Pockets
                                          ?? new Dictionary<string, PocketData>())
        {
            if (pocketId.StartsWith("verify_") || pocket == null) continue;
            if (pocket.Kind != PocketKind.Knowledge) continue;

            // The door, and it is the term that was missing. A pocket nothing opens hands nothing over,
            // whether it is walked or taken on a click — unit 10 changed which of those happens, not whether
            // an entrance is required.
            if (!openablePockets.Contains(pocketId)) continue;

            // `PocketManager.GrantPage` hands over the page for whatever it just taught, on the strength of
            // the id being the same string. So the grant field *is* the page id — reading it as one here is
            // the same construction, not an assumption about it.
            if (IsPage(pocket.GrantsBlueprintId)) pages.Add(pocket.GrantsBlueprintId);
        }

        return pages;
    }

    /// <summary>
    /// How many <c>.tres</c> files sit in a content directory. The registry side of a
    /// files-on-disk count — see <see cref="GateStations"/> for why one is needed per loader.
    /// </summary>
    private static int FilesIn(string dir)
    {
        var count = 0;
        using var handle = DirAccess.Open(dir);
        if (handle == null) return 0;

        handle.ListDirBegin();
        for (var f = handle.GetNext(); f != ""; f = handle.GetNext())
            if (!handle.CurrentIsDir() && (f.EndsWith(".tres") || f.EndsWith(".tres.remap")))
                count++;

        return count;
    }

    /// <summary>
    /// Whether a room can be walked to from the starting room right now, honouring flag gates. Used
    /// by <see cref="GateStations"/> to ask a question about the world that no field can answer:
    /// *does making this tool actually open anything?*
    /// </summary>
    /// <remarks>
    /// The <i>live</i> counterpart to the static walk in <see cref="GateMending"/>, and the difference
    /// is the whole point of having both. That one asks a question about the content — *which blights sit
    /// behind a <c>learned.tool_</c> door* — by skipping those doors whatever the flags say. This one
    /// asks a question about the run: it consults
    /// <see cref="World.InteractionService.IsAvailable"/>, so a door opens here exactly when it would
    /// open under the player's hand. Only the live form can show that crafting a thing *changed*
    /// anything, which is the one claim `RecipeData.SetsFlag` exists to support.
    /// </remarks>
    private static bool Reaches(Dictionary<string, RoomData> rooms, string targetRoomId)
    {
        var seen = new HashSet<string> { GameConfig.StartingRoomId };
        var queue = new Queue<string>(seen);

        while (queue.Count > 0)
        {
            if (!rooms.TryGetValue(queue.Dequeue(), out var room)) continue;

            foreach (var door in Doors(room))
            {
                if (!World.InteractionService.IsAvailable(door)) continue;
                if (seen.Add(door.TargetRoomId)) queue.Enqueue(door.TargetRoomId);
            }
        }

        return seen.Contains(targetRoomId);
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
        // Scoped to `ch*` chapters because these are the ledger's rules, and the waystation and its Dream
        // graph were never authored to them. **Unit 3b deleted those eight rooms, which made this
        // predicate and four others like it redundant** — `rooms` already excludes the verifier's own
        // fixtures by key prefix, so `ch*` and "every authored room" are now the same set.
        //
        // The plan for 3b said to widen all five. They stay instead, and the redundancy is *asserted*
        // immediately below rather than assumed, which buys the same thing for one line: no sweep
        // changes meaning, and an off-spine authored room goes red by name here instead of being
        // skipped silently by five sweeps at once and covered by none of them.
        var spine = rooms
            .Where(kv => kv.Value?.ChapterId?.StartsWith("ch") == true)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        Check("the chapter spine is authored", spine.Count > 0);

        var offSpine = rooms.Keys.Where(k => !spine.ContainsKey(k)).OrderBy(k => k).ToList();
        Check($"and every authored room is on it, so the `ch*` sweeps skip nothing{Listing(offSpine)}",
            offSpine.Count == 0);

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

            // A weapon with no damage type is a weapon that cannot be swung.
            //
            // **This covered traps too until 2026-08-04, and it was wrong on a premise that had already
            // been reversed.** The reasoning was "traps are consumed win or lose (§13), so an inert one
            // spends materials on nothing" — but §19 made traps permanent on 2026-08-01, and more to the
            // point `items.md` §3 authors two on purpose that deal nothing at all: the Snare holds and the
            // Chime reveals. The gate was vacuously green for as long as no trap existed, and went red the
            // hour the roster landed — against correct content. What survives is the weapon half; the
            // claim worth making about traps is a census rather than a universal, and it lives in
            // <see cref="GateRoster"/> beside the rest of the roster.
            if (item.Category == ItemCategory.Equipment && item.Slot == EquipSlot.Weapon &&
                item.Deals == DamageType.None)
                armedWithNothing.Add(id);

            // Warding is the armour half of the pair. On a weapon it is a field someone filled in
            // hopefully, and nothing reads it.
            if (item.WardsAgainst != DamageType.None && item.Slot != EquipSlot.Armour)
                wardNotArmour.Add(id);
        }

        Check($"no equipment is authored without a slot to wear it in{Listing(equipNoSlot)}",
            equipNoSlot.Count == 0);
        Check($"no non-equipment claims an equip slot{Listing(slotNotEquip)}",
            slotNotEquip.Count == 0);
        Check($"every weapon deals some kind of harm{Listing(armedWithNothing)}",
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

        // -- One body per screen, which is a rule the code now leans on --
        // **Held in someone's head for two units.** The retired F8 key rigged against every placement it
        // could find, so it never had to decide *which* body a room-level verb means; the satchel's Set
        // button does, and it answers with `NavigationManager.SoleCreatureHere` — the first placement in the
        // file. That is only a correct answer while there is one, and nothing anywhere said there was.
        //
        // So this is the check that doc comment cites. Break it and Set rigs against whichever placement the
        // inspector happens to have listed first, silently and consistently, with the second body drawn on
        // the same screen and untouchable. The failure is invisible in play: a trap that lands somewhere
        // reasonable looks exactly like a trap that landed where you aimed.
        //
        // **Every placement counts, resolved or not.** A room with one real body and one broken one is not
        // half-wrong — repairing the broken one is a content fix that would introduce the ambiguity without
        // touching this room's placement count, so the ambiguity is already here. `noEntity` above is what
        // reports the broken half; this reports the crowd.
        var crowded = rooms
            .Where(kv => kv.Value?.Entities != null)
            .Select(kv => (Room: kv.Key, Bodies: kv.Value.Entities.Count(p => p != null)))
            .Where(t => t.Bodies > 1)
            .Select(t => $"{t.Room}: {t.Bodies}")
            .OrderBy(s => s).ToList();

        // Counted in the message and required non-zero, because "no room holds two" is the sort of universal
        // that a map with no creatures on it passes triumphantly.
        var peopled = rooms.Count(kv => kv.Value?.Entities?.Any(p => p != null) == true);
        Check($"no room holds two creatures for a room-level verb to choose between "
            + $"({peopled} peopled screens){Listing(crowded)}",
            peopled > 0 && crowded.Count == 0);

        // -- Twelve creatures because four damage types give twelve pairs (items.md §10) --
        // Two creatures answering to the same tool and resisting the same tool are one creature in two
        // costumes, so a repeated pair means either a copy-paste or a roster that has outgrown the
        // matrix. The Beast's three variants deliberately share a pair, and share it by *design* —
        // they are keyed by entity id, so they are three placements of three files with one pair, and
        // this check has to allow that. It compares pairs across ids that differ in more than a
        // variant suffix, which is why the Beast files are named `beast_*` and the humans `creature_*`.
        // **A boss is the second sanctioned duplicate, added 2026-08-05, and it is gated rather than
        // exempted.** `items.md` is explicit that each boss's pair *deliberately* echoes a regular creature
        // in its own chapter — the Trapper takes the Logger's, the Blacksmith takes the Innkeeper's — so that
        // the chapter's first lesson is also the answer to its last fight. Skipping `boss_` the way `beast_`
        // is skipped would have been one line and would have thrown that away: two bosses sharing a pair with
        // *each other*, or with a creature from the wrong chapter, is not free teaching and nothing would
        // have noticed. So bosses are held out of the uniqueness set and then **required to collide**, which
        // is the design's own sentence turned into a predicate.
        //
        // The distinction is worth keeping for the next sanctioned duplicate: `beast_` is exempt because its
        // three variants are one creature in three costumes and the collision carries no meaning. A boss's
        // collision *is* the meaning, so an exemption would have covered the wrong half.
        var pairs = new Dictionary<string, string>();
        var repeated = new List<string>();
        var bosses = new List<EntityData>();
        foreach (var e in PlacedEntities(rooms))
        {
            if (e.VulnerableTo == DamageType.None || e.ResistantTo == DamageType.None) continue;
            if (e.Id.StartsWith("beast_")) continue;
            if (e.Id.StartsWith("boss_")) { bosses.Add(e); continue; }

            var key = $"{e.VulnerableTo}/{e.ResistantTo}";
            if (!pairs.TryAdd(key, e.Id)) repeated.Add($"{key}: {pairs[key]} + {e.Id}");
        }

        Check($"no two regular creatures answer to the same pair of types{Listing(repeated)}",
            repeated.Count == 0);

        // The chapter each id is placed in, so "its own chapter" can be asked rather than assumed. Keyed by
        // entity id, which is safe here only because the check above has just established that no two
        // regular creatures share a pair — a duplicate pair would make "which creature did the boss echo"
        // ambiguous and this lookup would answer for whichever loaded first.
        var chapterOf = new Dictionary<string, string>();
        foreach (var (_, room) in rooms)
        {
            if (room?.ChapterId?.StartsWith("ch") != true) continue;
            foreach (var p in room.Entities ?? new Godot.Collections.Array<EntityPlacement>())
                if (p?.Entity != null) chapterOf.TryAdd(p.Entity.Id, room.ChapterId);
        }

        var unechoed = new List<string>();
        foreach (var b in bosses)
        {
            var chapter = chapterOf.GetValueOrDefault(b.Id, "?");
            var echo = PlacedEntities(rooms).FirstOrDefault(e =>
                !e.Id.StartsWith("boss_") && !e.Id.StartsWith("beast_") &&
                e.VulnerableTo == b.VulnerableTo && e.ResistantTo == b.ResistantTo &&
                chapterOf.GetValueOrDefault(e.Id) == chapter);

            if (echo == null)
                unechoed.Add($"{b.Id} ({b.VulnerableTo}/{b.ResistantTo} in {chapter}) echoes nobody");
        }

        Check($"and each boss answers to a pair its own chapter already taught ({bosses.Count} placed)"
            + $"{Listing(unechoed)}",
            bosses.Count > 0 && unechoed.Count == 0);

        // -- Who projects an aura, which must only ever be a boss (`aura-scoped-to-bosses`) --
        //
        // **A printed census rather than a count asserted at three**, and the distinction is the eleventh
        // blind-spot class read the other way round. The honest answer today is *none*: the three bosses are
        // unauthored, so `"exactly three creatures have an aura"` would be red against correct content for as
        // long as chapter 1 is unsealed, and a check that is red for a year gets edited rather than heeded. So
        // the number goes in the message, where it is in front of Jeremy on every run and where the three
        // arriving is visible the day it happens.
        //
        // **The asserted half is vacuous today and will stay so until the bosses land — say it plainly.** The
        // field is a fraction of the Resolve bar spent per player turn, §6 sizes a real one to empty a full bar
        // in about six turns, and a negative is not a gentler aura but a typo `ActiveAura` reads as no aura at
        // all; half a bar a turn is a two-turn fight and nothing in the design wants one. All true, and all
        // unfalsifiable over a set of zero, so this line **cannot be break-tested** — the eleventh blind-spot
        // class, admitted rather than dressed up. It is here to be waiting when the three arrive, and the count
        // in the message is the half that works in the meantime.
        //
        // That count reading 0 is itself the standing witness that flipping `EntityData.CorruptionAura`'s
        // default to 0 landed: at `0.15f` every line in this list would be a rat.
        var auras = PlacedEntities(rooms)
            .Where(e => !Mathf.IsZeroApprox(e.CorruptionAura))
            .Select(e => $"{e.Id}: {e.CorruptionAura:0.###}")
            .Distinct().OrderBy(s => s).ToList();

        var mistuned = PlacedEntities(rooms)
            .Where(e => e.CorruptionAura < 0f || e.CorruptionAura >= 0.5f)
            .Select(e => $"{e.Id}: {e.CorruptionAura:0.###}")
            .Distinct().OrderBy(s => s).ToList();

        var kinds = PlacedEntities(rooms).Select(e => e.Id).Distinct().Count();
        Check($"every authored aura is a fraction of a bar rather than a rout "
            + $"({auras.Count} of {kinds} creatures project one){Listing(mistuned)}",
            mistuned.Count == 0);

        // -- The last collection that could hide behind a green run (handoff §3, fifth class) --
        //
        // Rooms, items and recipes are each counted against the files on disk; heists are asserted at zero.
        // **Creatures were the hole**, and a different-shaped one: there is no entity registry at all, so an
        // `EntityData` reaches the game only by being referenced from a placement. A creature authored and
        // never placed is therefore invisible to every check in this section — they all begin by walking
        // `rooms` — and the symptom in play is simply a screen that is empty, which looks exactly like a
        // screen nobody has authored yet.
        //
        // Distinct ids rather than placements: nine humans stand three times each and the Beast has three
        // files, so the count that can equal the file count is the set of *types* actually reachable.
        var placedTypes = PlacedEntities(rooms).Select(e => e.Id).Distinct().Count();
        Check($"every entity file on disk is placed somewhere "
            + $"({placedTypes} of {FilesIn(GameConfig.EntityDataDir)})",
            FilesIn(GameConfig.EntityDataDir) > 0 &&
            placedTypes == FilesIn(GameConfig.EntityDataDir));

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
        // Scoped to `ch*` for the same reason the spine checks in GateWorldGraph are, and the scoping
        // has outlived what it was for: `waystation.rat_01` was authored to the *old* model, where a
        // purified creature was farmable wildlife reached through EntityState rather than a person
        // reached through a hotspot, so it had a PurifiedName and owed nobody. It was the one placement
        // in the game that legitimately failed this, and unit 3b deleted it. The predicate is redundant
        // now and kept — the spine block asserts that it skips nothing.
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
        // chapter spine only, so the figures are the bill's figures. That mattered while the prototype
        // rat was quietly making every total one too many; unit 3b deleted it, and the scoping is kept
        // for the reason the spine block gives. Today it reads 30 / 11 / 3, and 24 freed to 6
        // defeated; unit 6 takes the fights to 33.
        var spine = rooms.Where(kv => kv.Value?.ChapterId?.StartsWith("ch") == true)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var aftermath = spine
            .SelectMany(kv => kv.Value.Hotspots.Where(h => h != null))
            .SelectMany(Gates)
            .ToList();

        // -- §5: the first fight a fresh game can walk into is winnable with what the boot gives you --
        // Added 2026-08-03, and nothing asserted it before. It is the kind of fact that stops being true
        // silently: the boot room already moved once (unit 3b), the roster moves again in unit 6, and
        // neither change touches anything that would go red. Jeremy, on being told nothing covered it:
        // *"I figured you would have designed it this way."*
        //
        // **Every claim here is a relationship between authored values — no turn count, no durability and
        // no damage figure is written down.** §5's 3–6 turn band is tuning, and a gate on tuning goes red
        // the first time Jeremy wants a fight to feel different, which is how a suite gets ignored. So
        // "winnable" is not "≤ 6 turns", it is *fells it before it fells you*: two derived numbers
        // compared against each other, with the whole economy free to move underneath them.
        //
        // "Can walk into" is walked rather than assumed. A fresh save holds no flags, so a door wanting
        // one is shut; a door a creature bars needs a fight won first. The frontier stops at both, which
        // leaves the screens a player sees before anything at all has been earned.
        // Both stopping rules are arguments rather than baked in, because a break-test caught the walk
        // being *unfalsifiable* with them baked in: commenting out all three `continue`s took the frontier
        // from 9 fights on 16 screens to 30 on 59, and **every check stayed green**, because the nearest
        // fight happens to sit two ungated doors out either way. That is the seventh blind-spot class — a
        // guard nothing can reach — and the fix is the two differential checks below, which compare the
        // real walk against a walk that ignores each rule in turn.
        static Dictionary<string, int> Walk(Dictionary<string, RoomData> map, bool wardensBar, bool flagsBar)
        {
            var seen = new Dictionary<string, int> { [GameConfig.StartingRoomId] = 0 };
            var frontier = new Queue<string>(seen.Keys);

            while (frontier.Count > 0)
            {
                var from = frontier.Dequeue();
                if (!map.TryGetValue(from, out var room)) continue;

                foreach (var door in Doors(room))
                {
                    if (wardensBar && !string.IsNullOrEmpty(door.BlockedByEntityId)) continue;
                    if (flagsBar && door.RequiredFlags?.Length > 0) continue;
                    if (flagsBar && !string.IsNullOrEmpty(door.RequiredFlag) && door.RequiredFlagValue)
                        continue;

                    if (seen.TryAdd(door.TargetRoomId, seen[from] + 1)) frontier.Enqueue(door.TargetRoomId);
                }
            }

            return seen;
        }

        var fresh = Walk(rooms, wardensBar: true, flagsBar: true);
        var openMap = Walk(rooms, wardensBar: false, flagsBar: false);

        // Differentials, not counts. Asserting "16 screens" would go red the day chapter 1 grows a screen;
        // asserting that *ignoring a rule opens screens the rule was closing* is a claim about the map's
        // shape that survives any amount of content, and it is red the moment the walk stops honouring it.
        //
        // **Both differentials measure from `openMap`, one rule at a time, since 2026-08-05.** They used to
        // measure from `fresh` with the *other* rule left switched on, and the warden one went red the hour
        // the three major repairs landed — because every warden-barred door in the game sits in chapter 2
        // (`ch2_mill_lane`, `ch2_shambles`, `ch2_tannery`), and gating the ch1→ch2 seam on
        // `passage.bridge.rebuilt` put all three behind a flag. With flags still barring, both walks stopped
        // at the seam, the counts came out equal, and a green check turned red on content that broke nothing.
        // The old form was not measuring the warden rule; it was measuring *whatever the flag rule left
        // reachable*, which is a moving frontier. Holding the other rule off isolates the variable, so each
        // check now fails only if that rule genuinely closes nothing — which is what it always claimed.
        // Blind-spot class: **a differential whose baseline is itself a filtered walk.**
        Check("a creature standing in a doorway shuts screens off from a fresh game",
            Walk(rooms, wardensBar: true, flagsBar: false).Count < openMap.Count);
        Check("and so does a door wanting a flag nobody has earned yet",
            Walk(rooms, wardensBar: false, flagsBar: true).Count < openMap.Count);
        // And the two rules together are at least as strict as either alone — the fresh walk cannot be
        // *wider* than a walk that honours one of the same two stopping rules.
        Check("and a fresh game sees no more than either rule alone allows",
            fresh.Count <= Walk(rooms, wardensBar: true, flagsBar: false).Count
            && fresh.Count <= Walk(rooms, wardensBar: false, flagsBar: true).Count);

        // Scoped to the *nearest* fight rather than to every reachable one, and the distinction is a
        // design decision rather than a convenience: a later creature the walk happens to reach is
        // allowed to be unwinnable today, because flight is guaranteed (§3) and "come back with the
        // right tool" is the shape the whole equipment ladder is built on. What may not be true is that
        // the *first* one turns a player away, since they have nothing to come back with.
        var withFights = fresh
            .Where(kv => rooms.TryGetValue(kv.Key, out var r) && r?.Entities?.Any(p => p?.Entity != null) == true)
            .ToList();

        Check($"a fresh game can walk into a fight at all "
            + $"({withFights.Count} of {fresh.Count} reachable screens hold one)",
            withFights.Count > 0);

        var doorsAway = withFights.Count == 0 ? 0 : withFights.Min(kv => kv.Value);
        var nearest = withFights
            .Where(kv => kv.Value == doorsAway)
            .OrderBy(kv => kv.Key)
            .SelectMany(kv => rooms[kv.Key].Entities
                .Where(p => p?.Entity != null && !string.IsNullOrEmpty(p.InstanceId))
                .Select(p => (Room: kv.Key, P: p)))
            .ToList();

        var kitWeapons = GameConfig.StartingItems
            .Select(id => InventoryCraftingManager.Instance?.GetItem(id))
            .Where(i => i is { Slot: EquipSlot.Weapon, Potency: > 0 } && i.Deals != DamageType.None)
            .ToList();

        Check($"and the boot kit arms them for it ({kitWeapons.Count} weapon(s) in "
            + $"{GameConfig.StartingItems.Length} starting item(s))",
            kitWeapons.Count > 0);

        // Ceilings, because a fight is turns and a fraction of a turn does nothing. Unwarded on purpose:
        // the armour slot starts empty (`items.md` §4 — *"the first coat should be a moment rather than a
        // default"*), so the boot's exposure is the real exposure.
        static int TurnsToFell(EntityData e, DamageType type, int potency)
        {
            var perTurn = CombatManager.DamageAgainst(type, e, potency);
            return perTurn <= 0 ? int.MaxValue : Mathf.CeilToInt(e.Durability / (float)perTurn);
        }

        static int TurnsToFall(EntityData e) =>
            e.Deals == DamageType.None || e.Damage <= 0
                ? int.MaxValue
                : Mathf.CeilToInt(GameConfig.VitalityMax / e.Damage);

        var unwinnable = new List<string>();
        var noLesson = new List<string>();
        var punchable = new List<string>();

        foreach (var (roomId, p) in nearest)
        {
            var e = p.Entity;
            var fall = TurnsToFall(e);
            var armed = kitWeapons.Count == 0
                ? int.MaxValue
                : kitWeapons.Min(w => TurnsToFell(e, w.Deals, w.Potency));
            var fists = TurnsToFell(e, GameConfig.BareHandedType, GameConfig.BareHandedPotency);

            // `<=` and not `<`: the player acts first (EncounterManager opens with `_playersTurn = true`),
            // so trading blow for blow to a dead heat is a win for whoever swung first.
            if (armed > fall) unwinnable.Add($"{roomId}/{p.InstanceId}: {armed} to fell, {fall} to fall");

            // The ratified reading of the first fight: it teaches resistance by *being* the pairing the
            // starting tool answers. Left as the check rather than tuning the numbers — Jeremy: *"this
            // seems the smart option"* — together with the fight screen naming your own damage type.
            if (kitWeapons.All(w => w.Deals != e.VulnerableTo)) noLesson.Add($"{roomId}/{p.InstanceId}");

            // And the gap, which is the whole point: `BareHandedPotency` exists so that a player out of
            // items has verbs rather than none, and its own summary says it is *"deliberately outside
            // §5's band"*. If fists were a plan, the tool would not be a lesson.
            if (fists <= fall) punchable.Add($"{roomId}/{p.InstanceId}: {fists} to fell, {fall} to fall");
        }

        Check($"the nearest fight ({doorsAway} door(s) out) falls to the boot kit before it "
            + $"falls you{Listing(unwinnable)}",
            nearest.Count > 0 && unwinnable.Count == 0);
        Check($"and the kit is the thing it is weak to, which is what the fight teaches{Listing(noLesson)}",
            noLesson.Count == 0);
        Check($"and bare hands lose it, which is what makes that worth teaching{Listing(punchable)}",
            punchable.Count == 0);

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
        //
        // "Because it is the first fight" was a sentence in this comment for a day and is now checked
        // against the walk above, which is worth the one line: the choice of screen is the *reason* the
        // probe means anything, and re-pointing the boot room would have left it silently probing an
        // ordinary fight while still claiming to probe the first one.
        const string firstFight = "ch1.logger_01";
        Check("and the screen driven below is that nearest fight, not a screen chosen once and left",
            nearest.Any(f => f.P.InstanceId == firstFight));

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

    /// <summary>
    /// The twelve blights, and the rule that finishes a screen — added 2026-08-03 with unit 2
    /// (<c>combat.md</c> §22, <c>build-plan.md</c> §4).
    ///
    /// <para>
    /// <b>The bill is asserted here, unlike the fight counts.</b> Twelve is not a content total that a
    /// later unit will grow: §1 puts twelve Graveflower in the world, §20 prices the exorcism engine at
    /// eight of them, and §23 rules that <i>"fifteen mends stays fifteen"</i> — the other three are the
    /// major repairs, which are recipes and land in unit 4 with no new blight hotspot. So a thirteenth
    /// blight is not new content, it is a Graveflower the ending was not priced against, and that is
    /// worth a red line.
    /// </para>
    ///
    /// <para>
    /// Three of the checks below exist because of failures that are <i>silent in both the inspector and
    /// in play</i>: an out-of-range <c>Solution</c> index (a board that can never be finished), a
    /// <c>MendPuzzle</c> that failed to deserialise (a blight that opens nothing), and a harvest node
    /// yielding Graveflower (the hoard inflating past the price of the good ending). None of them throws,
    /// and none of them looks wrong on screen.
    /// </para>
    /// </summary>
    private static void GateMending(Dictionary<string, RoomData> rooms)
    {
        Section("mending  the twelve blights, and what finishes a screen");

        const string flower = "herb_graveflower";

        // -- The bill --
        var blights = new List<(string RoomId, string Chapter, HotspotData Data)>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
                if (h is { Kind: HotspotKind.Mend })
                    blights.Add((roomId, room.ChapterId ?? "", h));

        var perChapter = new[] { "ch1", "ch2", "ch3" }
            .Select(c => blights.Count(b => b.Chapter == c))
            .ToArray();

        Check($"the world holds exactly 12 blights ({blights.Count})", blights.Count == 12);
        Check($"split 2 / 4 / 6 across the chapters "
            + $"({perChapter[0]} / {perChapter[1]} / {perChapter[2]})",
            perChapter[0] == 2 && perChapter[1] == 4 && perChapter[2] == 6);

        // The remainder, which the split above cannot see: a blight authored into a room the chapter
        // ledgers never account for is one the ending never counted. It caught the waystation and the
        // dream pockets until unit 3b deleted them, and it is what unit 6's pockets will land against.
        Check("and none of them is authored off the chapter spine",
            blights.Count == perChapter.Sum());

        // -- The yield, which is the whole economy of the good ending --
        Check("Graveflower is in the item catalogue",
            InventoryCraftingManager.Instance?.Catalogue.ContainsKey(flower) == true);

        var wrongYield = new List<string>();
        var flagCollision = new List<string>();
        var wouldRefill = new List<string>();
        var flags = new Dictionary<string, string>();

        foreach (var (roomId, _, h) in blights)
        {
            var where = $"{roomId}/{h.Id}";

            if (h.ItemId != flower || h.ItemAmount != 1)
                wrongYield.Add($"{where}: {h.ItemId} x{h.ItemAmount}");

            // Mended flags are derived from the hotspot id, so a collision is two blights that are one
            // blight: mend either and the other reads as done, on a screen nobody touched.
            if (!flags.TryAdd(h.ResolvedMendedFlag, where))
                flagCollision.Add($"{h.ResolvedMendedFlag}: {flags[h.ResolvedMendedFlag]} + {where}");

            // Inert rather than harmful — the chapter refill only clears Harvest flags — but a field
            // that reads as "this comes back" on the one hotspot in the game that must not is worth
            // catching, because the fix if it ever *did* work is a second Graveflower per chapter.
            if (h.RefillsAtMilestone) wouldRefill.Add(where);
        }

        Check($"every blight pays exactly 1 Graveflower{Listing(wrongYield)}", wrongYield.Count == 0);
        Check($"and no two share a mended flag{Listing(flagCollision)}", flagCollision.Count == 0);
        Check($"and none of them is marked to come back{Listing(wouldRefill)}", wouldRefill.Count == 0);

        // Graveflower grows on mended ground and nowhere else (§22), so a mend is its only source. A
        // harvest node handing it out is the cheap way to break the ending's arithmetic, and it would
        // look exactly like every other herb node on screen.
        var otherSource = new List<string>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
            {
                if (h == null || h.Kind == HotspotKind.Mend) continue;
                if (h.ItemId == flower) otherSource.Add($"{roomId}/{h.Id} ({h.Kind})");
            }

        Check($"nothing but a mend yields Graveflower{Listing(otherSource)}", otherSource.Count == 0);

        // -- The boards --
        // Reported as five separate offender lists rather than one IsSolvable() sweep, because the five
        // causes want five different fixes and the first one is not an authoring mistake at all: a null
        // MendPuzzle on a hotspot that has one in its .tres means the sub-resource failed to load, which
        // is the malformed-file failure that otherwise passes every registry check in this suite.
        var noPuzzle = new List<string>();
        var noParts = new List<string>();
        var noSolution = new List<string>();
        var outOfRange = new List<string>();
        var repeatedStep = new List<string>();

        foreach (var (roomId, _, h) in blights)
        {
            var where = $"{roomId}/{h.Id}";
            var puzzle = h.MendPuzzle;

            if (puzzle == null) { noPuzzle.Add(where); continue; }
            if (puzzle.Parts == null || puzzle.Parts.Length == 0) { noParts.Add(where); continue; }
            if (puzzle.Solution == null || puzzle.Solution.Length == 0) { noSolution.Add(where); continue; }

            var steps = new HashSet<int>();
            foreach (var step in puzzle.Solution)
            {
                if (step < 0 || step >= puzzle.Parts.Length)
                    outOfRange.Add($"{where}: step {step} of {puzzle.Parts.Length} parts");
                else if (!steps.Add(step))
                    repeatedStep.Add($"{where}: step {step} twice");
            }
        }

        Check($"every blight carries a puzzle that loaded{Listing(noPuzzle)}", noPuzzle.Count == 0);
        Check($"every puzzle has parts to click{Listing(noParts)}", noParts.Count == 0);
        Check($"and a solution to click them in{Listing(noSolution)}", noSolution.Count == 0);
        Check($"no solution names a part that is never drawn{Listing(outOfRange)}",
            outOfRange.Count == 0);
        Check($"and no step is asked for twice{Listing(repeatedStep)}", repeatedStep.Count == 0);

        // -- §22: a chapter 3 blight must not be solvable in chapter 1 --
        // Checked against the door graph rather than against a field, because that is where the fact
        // actually lives: none of the three tool blights carries a RequiredFlag, on purpose, since the
        // screen it stands on is already unreachable without the tool. A gate reading a field here would
        // pass while the geography quietly changed underneath it — which is the version that can happen,
        // since the doors are content and the blights are not.
        var reached = new HashSet<string> { GameConfig.StartingRoomId };
        var queue = new Queue<string>(reached);
        while (queue.Count > 0)
        {
            if (!rooms.TryGetValue(queue.Dequeue(), out var room)) continue;
            foreach (var door in Doors(room))
            {
                if (Gates(door).Any(f => f.StartsWith("learned.tool_"))) continue;
                if (reached.Add(door.TargetRoomId)) queue.Enqueue(door.TargetRoomId);
            }
        }

        var behindATool = blights.Where(b => !reached.Contains(b.RoomId)).ToList();
        var toolRooms = behindATool.Select(b => b.RoomId).OrderBy(r => r).ToList();

        Check($"exactly 3 blights cannot be reached with no tool learned{Listing(toolRooms)}",
            behindATool.Count == 3);
        Check("one in each chapter, so no chapter's tool form arrives early",
            behindATool.Select(b => b.Chapter).Distinct().Count() == 3);

        // -- And now the mechanism, on ground that is safe to break --
        var world = WorldStateManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        var states = EntityStateManager.Instance;
        var nav = NavigationManager.Instance;

        var fouled = nav.GetRoom("verify_blighted");
        var held = nav.GetRoom("verify_held");
        Check("the mending fixtures registered", fouled != null && held != null);
        if (fouled == null || held == null) return;

        var site = fouled.Hotspots[0];
        var before = inv?.GetCount(flower) ?? 0;

        world.SetFlag(site.ResolvedMendedFlag, false);
        world.SetFlag(Flags.Cleansed(fouled.Id), false);

        Check("an unmended blight leaves its screen fouled", MendManager.IsFouled(fouled));
        Check("and so the screen is not cleansed", !MendManager.IsCleansed(fouled));
        Check("a blight nobody has touched can be mended", MendManager.CanMend(site));

        Check("mending it reports success", MendManager.Mend(site));
        Check("the mended flag is set", world.GetFlag(site.ResolvedMendedFlag));
        Check("the flower is paid, once", (inv?.GetCount(flower) ?? 0) == before + 1);
        Check("the screen is no longer fouled", !MendManager.IsFouled(fouled));

        // Mending the last thing on the screen is what finishes it — the direction that matters, and
        // the one the whole Waking colour scheme hangs off.
        Check("and mending the last thing on it cleansed it", MendManager.IsCleansed(fouled));
        Check("with the flag to match", world.GetFlag(Flags.Cleansed(fouled.Id)));

        // The Harvest shape: refuse, do not pay twice. Without this a blight is a Graveflower tap.
        Check("mending it again is refused", !MendManager.Mend(site));
        Check("and pays nothing the second time", (inv?.GetCount(flower) ?? 0) == before + 1);
        Check("nor can it be mended", !MendManager.CanMend(site));

        // Arrival re-derives, which is what corrects a save written before the flag existed and what
        // keeps the ~47 screens with nothing on them from being permanently out of step.
        world.SetFlag(Flags.Cleansed(fouled.Id), false);
        nav.GoToRoom(fouled.Id);
        Check("walking onto a screen re-derives its verdict",
            world.GetFlag(Flags.Cleansed(fouled.Id)));

        // -- Held: a mended screen with something still standing on it is not finished --
        // Driven without navigating; see BlightRoom's remarks for why this fixture stays off screen.
        var heldSite = held.Hotspots[0];
        world.SetFlag(heldSite.ResolvedMendedFlag, false);
        world.SetFlag(Flags.Cleansed(held.Id), false);
        states?.SetState(HeldCreature, EntityState.Corrupted);

        Check("a creature still standing holds its screen", MendManager.IsHeld(held));
        Check("mending the blight there does not cleanse it", MendManager.Mend(heldSite));
        Check("because the screen is still held", !MendManager.IsCleansed(held));
        Check("and the flag stayed shut", !world.GetFlag(Flags.Cleansed(held.Id)));

        states?.Purify(HeldCreature);
        Check("purifying it is what flips the screen", MendManager.Reevaluate(held));
        Check("and the flag follows", world.GetFlag(Flags.Cleansed(held.Id)));

        // Both directions, which is what makes the flag a cache rather than a second source of truth.
        // Nothing in the game can currently re-corrupt a creature; the false branch is the reason the
        // debug panel can print the verdict beside the flag and expect them to agree.
        states?.SetState(HeldCreature, EntityState.Severed);
        Check("a severed body still holds the screen", MendManager.IsHeld(held));
        Check("and re-deriving takes the flag back down", !MendManager.Reevaluate(held));
        Check("so the flag never outlives the verdict", !world.GetFlag(Flags.Cleansed(held.Id)));

        // -- And now one real authored blight, on screen, through the panel --
        // Everything above this point reads .tres files or synthetic rooms. None of it proves a blight
        // *appears*, or that clicking it reaches the board: the Mend case in InteractionService emits an
        // event, the Hud loads the panel from a path string, and both failures are silent. This is the
        // same gap the fights section had to close, closed the same way — on the first blight in the
        // game, which §22 names as the tutorial.
        const string tutorial = "ch1_fern_gully";
        nav.GoToRoom(tutorial);

        var room1 = nav.GetRoom(tutorial);
        var blight = room1?.Hotspots.FirstOrDefault(h => h is { Kind: HotspotKind.Mend });
        Check("the first blight in the game is authored", blight != null);

        if (blight != null && _hud?.Mend != null)
        {
            // The flag it will set must not already be set, or every check below is measuring a mend
            // some earlier gate performed.
            world.SetFlag(blight.ResolvedMendedFlag, false);
            var paidBefore = inv?.GetCount(flower) ?? 0;

            var drawn = Find(Live(), blight.Id);
            Check("it is on screen", Blind || drawn != null);
            Check("and drawn with its own cue", Blind || drawn?.ShowCue == true);
            Check("and not shut", Blind || drawn?.Locked == false);

            Check("clicking it is allowed", InteractionService.ActivateHotspot(blight));
            Check("the board came up", _hud.Mend.IsOpen);
            Check("showing that blight", _hud.Mend.BlightId == blight.Id);
            Check($"with its 5 parts on it ({_hud.Mend.PartCount})", _hud.Mend.PartCount == 5);

            // -- the board and the journal do not stack, in both directions --
            // Unit 7a's two remaining unwatched exclusions, checked here rather than in the journal section
            // because this is where a *real* board is up: `MendPanel.Open` refuses a blight with no solvable
            // puzzle, so a synthetic fixture would have exercised the refusal instead of the exclusion.
            EventBus.Instance?.EmitCraftingRequested("verify_bench_over_a_board");
            Check("a bench click from under the board shuts the board",
                !_hud.Mend.IsOpen && _hud.Crafting?.IsOpen == true);
            Check("the blight takes a second click", InteractionService.ActivateHotspot(blight));
            Check("which shuts the book", _hud.Crafting?.IsOpen != true);
            Check("and has the board back, about the same blight",
                _hud.Mend.IsOpen && _hud.Mend.BlightId == blight.Id);

            Check("solving it closes the board", _hud.Mend.DebugSolve());
            Check("the blight is mended", world.GetFlag(blight.ResolvedMendedFlag));
            Check("and it paid a Graveflower", (inv?.GetCount(flower) ?? 0) == paidBefore + 1);

            // The screen still holds a corrupted creature, so the mend is not the last thing here.
            Check("but the screen is not cleansed, because something still stands on it",
                !world.GetFlag(Flags.Cleansed(tutorial)));

            world.SetFlag(blight.ResolvedMendedFlag, false);
            inv?.RemoveItem(flower, 1);
            MendManager.Reevaluate(room1);
        }
        else
        {
            Check("the mend panel reached the Hud", _hud?.Mend != null);
        }

        // -- §6: the three major repairs, which are boards on stations rather than blights --
        // Added 2026-08-05 with the content. Everything above this line counts `HotspotKind.Mend`, and
        // that is exactly why this block has to exist separately: a major repair is a `Station` carrying a
        // `MendPuzzle` (`Enums.cs`, `MendManager.CarriesABoard`), so **every sweep in this section walks
        // straight past it** — the bill of twelve, the Graveflower price, the five board sweeps, the fouled
        // rule. That is the design, not an oversight: §20 prices the exorcism engine at eight of the twelve
        // flowers, so a repair that fouled a screen or paid a flower would be a thirteenth nobody costed.
        // But "the sweeps ignore it" and "nothing checks it" are one keystroke apart, and the first is only
        // safe while the second is false.
        var repairs = new List<(string RoomId, HotspotData Data)>();
        foreach (var (roomId, room) in rooms)
            foreach (var h in room.Hotspots)
                if (h is { Kind: HotspotKind.Station } && h.MendPuzzle != null)
                    repairs.Add((roomId, h));

        Check($"three stations carry a board rather than a bench ({repairs.Count} of 3)",
            repairs.Count == 3);

        // Both directions of the pairing, and neither is redundant. `MendManager.IsAssembled` derives
        // "has this been built" from the recipe's own `SetsFlag`, so a station with no repair authored for
        // it is a bridge that can never become a board — and it refuses silently, looking exactly like one
        // that is merely unbuilt. The reverse is a recipe you can craft with nothing to show for it.
        var authoredRepairs = InventoryCraftingManager.Instance.Recipes.Values
            .Where(r => r != null && !r.Id.StartsWith("verify_") && r.RepairsWorld)
            .ToList();

        var unpaired = repairs
            .Where(r => MendManager.RepairAt(r.Data.StationId) == null)
            .Select(r => $"{r.RoomId}/{r.Data.Id}").ToList();
        var boardless = authoredRepairs
            .Where(r => !repairs.Any(x => x.Data.StationId == r.RequiresStation))
            .Select(r => r.Id).ToList();

        Check($"three recipes rebuild the world instead of making a thing ({authoredRepairs.Count} of 3)",
            authoredRepairs.Count == 3);
        Check($"every board on a station has a repair that assembles it{Listing(unpaired)}",
            unpaired.Count == 0);
        Check($"and every world repair has a board that finishes it{Listing(boardless)}",
            boardless.Count == 0);

        // The economy, stated from this side too. `IsFouled` counting `Mend` alone is what keeps it true,
        // and that is one word in one predicate.
        var repairPays = repairs
            .Where(r => !string.IsNullOrEmpty(r.Data.ItemId))
            .Select(r => $"{r.RoomId}/{r.Data.Id} -> {r.Data.ItemId}").ToList();
        var repairFouls = repairs
            .Where(r => MendManager.IsFouled(nav.GetRoom(r.RoomId)) && !blights.Any(b => b.RoomId == r.RoomId))
            .Select(r => r.RoomId).ToList();

        Check($"no repair pays out an item, so twelve Graveflower stays twelve{Listing(repairPays)}",
            repairPays.Count == 0);
        Check($"and a broken bridge does not foul the screen it is on{Listing(repairFouls)}",
            repairFouls.Count == 0);

        // The five board sweeps above, re-run over the three. Same failures, same silence: an out-of-range
        // step is a span that can never be seated.
        var brokenRepairBoards = repairs
            .Where(r => r.Data.MendPuzzle?.IsSolvable() != true)
            .Select(r => $"{r.RoomId}/{r.Data.Id}").ToList();
        Check($"and all three boards are solvable{Listing(brokenRepairBoards)}",
            brokenRepairBoards.Count == 0);

        // A repair whose `SetsFlag` is blank is the one authoring slip that leaves the whole thing inert:
        // the board opens, the pieces seat, and the bridge is still broken. Also what makes the drive
        // below safe to write flags off.
        var opensNothing = repairs
            .Where(r => string.IsNullOrEmpty(r.Data.SetsFlag))
            .Select(r => $"{r.RoomId}/{r.Data.Id}").ToList();
        Check($"and each one opens something when it is finished{Listing(opensNothing)}",
            opensNothing.Count == 0);

        // -- The two clicks, in order, on a real station --
        // The order is the whole design (§22): the panel first, because that is where the commons are paid
        // and where the reclaimed memory gates the row; the board second, because it is the animation and
        // not the gate. Driven through `InteractionService` rather than by asking `CarriesABoard`, since
        // the widening's failure mode is a click that reaches the wrong panel — and both panels are loaded
        // from a path string, which fails silently.
        //
        // ~~Picked as *the repair no flag holds shut* rather than by name: the engine sits behind a defeated
        // creature, and a gate that hardcoded a room id would start measuring availability the day Jeremy
        // gates the other two behind their bosses.~~
        //
        // **That was the right worry and the wrong fix, and it cost 20 checks on 2026-08-05.** Placing the
        // two bosses put a `RequiredFlag` on the span and the cart, so all three repairs were gated, so
        // `FirstOrDefault(no gates)` found nothing and the whole walkthrough below **silently stopped
        // running** — 1073 checks became 1053 and only one line went red. Avoiding a hardcoded room id was
        // correct; the mistake was making the fixture *depend on authored content leaving a door open*,
        // which is the fixture-that-arrives-by-default class with the default supplied by `data/`.
        //
        // **A fixture must construct the state it needs, not discover it.** So: take the first repair
        // whatever holds it shut, drive the shut case as a real check, then open its gates by hand and drive
        // the two clicks. The gate flags are restored at the end with everything else. Two properties this
        // buys that the old shape could not have: the block can never go dark again (a repair existing is
        // asserted, and `repairs.Count` is asserted at 3 above), and the *shut* half of the gating Jeremy
        // just authored is now covered — previously nothing anywhere asked whether a boss actually holds
        // its repair closed.
        var open = repairs.FirstOrDefault();
        Check($"there is a repair to drive the two clicks through ({repairs.Count} placed)", open.Data != null);

        if (open.Data is { } station && _hud?.Mend != null && _hud?.Crafting != null)
        {
            var recipe = MendManager.RepairAt(station.StationId);
            var shut = Gates(station).ToList();
            world.SetFlag(recipe.SetsFlag, false);
            world.SetFlag(station.ResolvedMendedFlag, false);
            world.SetFlag(station.SetsFlag, false);
            foreach (var g in shut) world.SetFlag(g, false);

            nav.GoToRoom(open.RoomId);

            // The shut case first, because it is only reachable before the fixture opens the gates — the
            // same ordering the two-hand refusal needed (§3, fourteenth class): a state that a later line
            // destroys cannot be asserted after that line.
            var shutSpan = Find(Live(), station.Id);
            Check($"a repair whose boss still stands is drawn shut (held by {string.Join(", ", shut)})",
                shut.Count > 0 && (Blind || shutSpan?.Locked == true));
            Check("and a shut repair takes no clicks at all",
                Blind || shutSpan?.MouseFilter == Control.MouseFilterEnum.Ignore);
            Check("and it is not a board while it is shut", !MendManager.CanMend(station));

            // `RebuildHotspots` runs off `WorldFlagChanged`, so the span below is a different node than the
            // one above — do not hold the old reference across this line.
            foreach (var g in shut) world.SetFlag(g);
            Check("dealing with the boss opens it", shut.All(f => world.GetFlag(f)));

            var drawnSpan = Find(Live(), station.Id);
            Check("the broken thing is on screen", Blind || drawnSpan != null);
            Check("and not shut", Blind || drawnSpan?.Locked == false);

            Check("an unassembled repair is not a board yet", !MendManager.CarriesABoard(station));
            Check("so it cannot be mended", !MendManager.CanMend(station));

            Check("clicking it is allowed", InteractionService.ActivateHotspot(station));
            Check("and it opens the book, not the board",
                _hud.Crafting.IsOpen && !_hud.Mend.IsOpen);
            Check("at its own station", _hud.Crafting.VenueStationId == station.StationId);
            Check("with the repair on the page",
                _hud.Crafting.RowRecipeIds.Contains(recipe.Id));

            // And the click did not open the passage on its way past. `SetsFlag` is withheld from a repair
            // station for exactly this reason: on every other station it announces a venue, on this one it
            // is the bridge, and a passage that opens because you looked at it is not a repair.
            Check("and looking at the bridge did not rebuild it", !world.GetFlag(station.SetsFlag));

            world.SetFlag(recipe.SetsFlag);
            Check("assembling the repair makes it a board", MendManager.CarriesABoard(station));
            Check("which can now be mended", MendManager.CanMend(station));

            Check("the second click is allowed too", InteractionService.ActivateHotspot(station));
            Check("and this time it is the board", _hud.Mend.IsOpen && !_hud.Crafting.IsOpen);
            Check("about that station", _hud.Mend.BlightId == station.Id);
            Check($"with every part on it ({_hud.Mend.PartCount} of {station.MendPuzzle.Parts.Length})",
                _hud.Mend.PartCount == station.MendPuzzle.Parts.Length);

            var flowersBefore = inv?.GetCount(flower) ?? 0;
            Check("solving it closes the board", _hud.Mend.DebugSolve());
            Check("the repair is recorded", world.GetFlag(station.ResolvedMendedFlag));
            Check("the passage it was authored against is open", world.GetFlag(station.SetsFlag));
            Check("and it paid no Graveflower", (inv?.GetCount(flower) ?? 0) == flowersBefore);
            Check("a repair cannot be done twice", !MendManager.Mend(station));

            world.SetFlag(recipe.SetsFlag, false);
            world.SetFlag(station.ResolvedMendedFlag, false);
            world.SetFlag(station.SetsFlag, false);

            // The gates the fixture opened, shut again. `defeated.ch1.lost_trapper` left standing would be a
            // boss a later gate never fought — and one block's cleanup is the next block's fixture (§3).
            foreach (var g in shut) world.SetFlag(g, false);
            MendManager.Reevaluate(nav.GetRoom(open.RoomId));
        }

        // -- Every repair waits on the thing that was standing in front of it --
        //
        // **This was a self-retiring check for one day and it retired on schedule.** It read *"each boss that
        // is placed holds its repair shut"* and skipped any boss that was not placed, because on 2026-08-05
        // the two `RequiredFlag` lines had to come back off the span and the cart: a gate naming an unplaced
        // creature is a hotspot that can never open, and `GateWorldGraph` fails on it, correctly. Naming the
        // flag would have been red for something that was not a defect; naming the *relationship* was green
        // then and went red the hour Jeremy placed either boss, which is exactly when the gate had to go back
        // on. It did, the same day, so the `if (!placed) continue` is gone and the pairs are asserted flat.
        //
        // **What is worth keeping is the shape, not this instance.** An obligation that cannot be gated
        // directly can often be gated in reverse — key it on the content arriving rather than on the
        // calendar, and it fires on the day the work becomes possible instead of nagging until then. The
        // failure mode of the other direction is `:5334`'s: a check that means "not yet" teaches Jeremy that
        // red means ignore me.
        //
        // Kept as a table rather than folded into the sweep above, because the sweep can only ask whether a
        // gate *resolves* — it cannot know that the bridge is the Trapper's and the cart is the Blacksmith's.
        // That pairing is authored in `items.md` and this is the only thing in the repo that holds it.
        // **The room is in the table, and the first version of this check left it out — which cost a wrong
        // placement that went green.** I put both bodies on the *repair* screens, one past where they belong,
        // and `both set-piece bosses are placed` asked only whether the instance stood in *some* room, so 1080
        // checks passed on content the ledgers contradict. The ledgers are unambiguous and agree in three
        // places: `ledger-ch1` row 9 is the Trapper's trap line and row 10 is the bridge; `ledger-ch2` row 11
        // is the Blacksmith's forge and row 12 is the cart road; and `ledger-ch2`'s *Screen | Fight* table
        // lists the Blacksmith against screen **11**, while `ledger-ch1`'s path reads *"Hunter #2 → the Lost
        // Trapper → the bridge"*. **You fight him, then the repair he was standing in front of opens** — which
        // is the entire point of gating the repair on his flag, and is wrong if he stands on the repair screen
        // himself.
        //
        // The general form is §3's own recurring one: **a check keyed on a weaker property than the document it
        // enforces is green on content that document forbids.** "Placed somewhere" is weaker than "placed
        // here", and the weaker reading is the one that reads as done.
        //
        // Kept as a table rather than folded into the sweep above, because the sweep can only ask whether a
        // gate *resolves* — it cannot know that the trap yard is the Trapper's and the forge is the
        // Blacksmith's, nor that the repair each one gates is one screen on. That pairing is authored in the
        // ledgers and this is the only thing in the repo that holds it.
        string[][] bosses =
        {
            new[] { "ch1.lost_trapper", "ch1_trap_yard", "ch1_bridge_span" },
            new[] { "ch2.blacksmith", "ch2_smithy", "ch2_cart_road_cart" },
        };

        var ungatedRepair = new List<string>();
        var unplacedBoss = new List<string>();
        foreach (var pair in bosses)
        {
            var standing = rooms.Values
                .Where(r => r?.Entities?.Any(p => p?.InstanceId == pair[0]) == true)
                .Select(r => r.Id)
                .ToList();
            if (!standing.Contains(pair[1]))
                unplacedBoss.Add(standing.Count == 0
                    ? $"{pair[0]} is placed nowhere (wanted {pair[1]})"
                    : $"{pair[0]} stands in {string.Join(" + ", standing)}, not {pair[1]}");

            var hotspot = repairs.FirstOrDefault(r => r.Data.Id == pair[2]).Data;
            if (hotspot == null || !AftermathOf(hotspot).Contains(pair[0]))
                ungatedRepair.Add($"{pair[2]} does not wait on {pair[0]}");
        }

        // Both halves, because either one alone passes on the other's absence: a misplaced boss cannot fail
        // the gating check, which never looks at rooms, and a gated repair says nothing about whether the
        // creature it names is standing anywhere. This is the pair the old shape collapsed into one.
        Check($"both set-piece bosses stand on their authored screen{Listing(unplacedBoss)}",
            unplacedBoss.Count == 0);
        Check($"and each one holds the repair on the screen after it shut{Listing(ungatedRepair)}",
            ungatedRepair.Count == 0);

        // The third repair is not in that table and should not be: the engine waits on the Breach-Thing,
        // which is a Beast rather than a set-piece, and its gate resolves through the ordinary sweep.
        var engine = repairs.FirstOrDefault(r => r.Data.StationId == "exorcism_engine").Data;
        Check("and the engine waits on the thing at the portal",
            engine != null && AftermathOf(engine).Any(id => id.Contains("breach_thing")));

        // -- Put the world back --
        // The fixtures are disposable but the flags and the satchel are not: a Graveflower left behind
        // is a material a later gate did not put there, and a mended flag is progress nobody earned.
        world.SetFlag(site.ResolvedMendedFlag, false);
        world.SetFlag(heldSite.ResolvedMendedFlag, false);
        world.SetFlag(Flags.Cleansed(fouled.Id), false);
        world.SetFlag(Flags.Cleansed(held.Id), false);
        states?.SetState(HeldCreature, EntityState.Corrupted);

        var surplus = (inv?.GetCount(flower) ?? 0) - before;
        if (surplus > 0) inv?.RemoveItem(flower, surplus);

        var forms = blights.Count(b => b.Data.MendPuzzle?.IsOrdered == true);
        GD.Print($"       {blights.Count} blights: {blights.Count - forms} spot / {forms} ordered, "
            + $"{behindATool.Count} of them behind a tool; {blights.Count} Graveflower in the world, "
            + "and the engine costs 8");
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
    /// the same check unfalsifiable — <c>handoff.md</c> §3's third class, "a check that cannot fail".
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

        // -- The room card keeps out of the HUD's corner (2026-08-05) --
        //
        // Jeremy, from a screenshot: *"I don't like how the room display lines up with the debug one."* The card
        // drew its plate at a flat `(24, 24)` and the HUD's `TopLeft` VBox sits at `(40, 32)`, so the shipping
        // room name lay across the debug id and the card's second line lay across "WAKING WORLD".
        //
        // **Checkable only because the position is a pure function now.** A headless run lays nothing out, so
        // `Size` is zero and the drawn rect is unavailable — the same blind spot that hid the crafting panel's
        // softlock earlier the same day. `RoomCard.PlateOrigin` takes the viewport height and the plate height
        // and returns the corner, so a gate can ask it at a nominal 1080 with no viewport at all.
        //
        // The claim is a **relationship**, not a coordinate: the plate's top must fall below the bottom of the
        // HUD's top-left block, whatever either one's size. `HudTopLeftBottom` is measured from the HUD's own
        // constants rather than guessed, so restyling either one keeps the check honest instead of stale.
        const float NominalHeight = 1080f;
        const float PlateHeight = 200f;
        const float HudTopLeftBottom = 32f + 30f + 20f + 8f; // position, room label, realm label, spacing
        var plateTop = RoomCard.PlateOrigin(NominalHeight, PlateHeight).Y;
        Check($"the room card's plate clears the HUD's top-left block "
            + $"(plate top {plateTop}, HUD block ends {HudTopLeftBottom})",
            plateTop > HudTopLeftBottom);
        Check($"and stays on screen rather than falling off the bottom (plate bottom "
            + $"{plateTop + PlateHeight} of {NominalHeight})",
            plateTop + PlateHeight < NominalHeight);

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

    // ---- The docs, as an interface ---------------------------------------

    /// <summary>
    /// Every reference in <c>src/</c> to a section of <c>docs/handoff.md</c> still resolves.
    ///
    /// <para><b>Agreed 2026-08-04, after a doc pass broke two of them and finding that out by hand cost a
    /// write-then-revert.</b> The handoff was cut from 44k words to 6.5k and two <c>§</c>-citations were left
    /// pointing at nothing. That is worth a gate rather than care, because the failure is silent in both
    /// directions: nothing in a build reads a comment, and nobody re-reads fourteen comments after editing a
    /// document. <b>A doc section with citations in code is an interface, whatever it reads like</b> — which
    /// is the argument that put §3's essay back after it was cut as a duplicate.</para>
    ///
    /// <para><b>Both spellings, because the project uses both and only one of them is policy.</b> §6 of the
    /// handoff says comments cite section <i>names</i>, not numbers, since the numbers have churned three
    /// times — so every <c>§N</c> in <c>src/</c> is already a small violation, and they are exactly the ones
    /// that broke. Named citations are matched as a <i>prefix</i> of a heading, because
    /// <c>"Placeholders and traps"</c> is how the code cites a section actually titled <c>"Placeholders and
    /// traps for the unwary"</c>, and demanding the full title would fail on a correct citation.</para>
    ///
    /// <para><b>The guard that makes the rest mean anything is the count.</b> A walker that found nothing
    /// would report every citation resolving and go green — the fifth blind-spot class exactly, a universal
    /// over an empty set. So the number found is asserted to be plausible before any of it is believed.</para>
    /// </summary>
    private static void GateDocCitations()
    {
        Section("docs  the citations in code still resolve");

        // -- The document, and the headings it actually has --
        var doc = ReadTextFile("res://docs/handoff.md");
        Check("docs/handoff.md is readable from the running game", !string.IsNullOrEmpty(doc));
        if (string.IsNullOrEmpty(doc)) return;

        var numbered = new Dictionary<int, string>();
        var titles = new List<string>();

        foreach (var raw in doc.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("## ")) continue; // `###` are sub-headings; nothing cites one by number

            var title = line[3..].Trim();

            // `## 3. Placeholders and traps for the unwary` — the number is what a §N citation names, and
            // the remainder is what a named citation names. Both come off the same line, so they cannot
            // disagree about which section they belong to.
            var dot = title.IndexOf(". ", StringComparison.Ordinal);
            if (dot > 0 && int.TryParse(title[..dot], out var n))
            {
                numbered[n] = title[(dot + 2)..].Trim();
                titles.Add(title[(dot + 2)..].Trim());
            }
            else
            {
                titles.Add(title);
            }
        }

        Check($"it has numbered sections to cite ({numbered.Count})", numbered.Count > 0);
        Check($"and headings to cite by name ({titles.Count})", titles.Count > 0);

        // -- Every citation in src/ --
        var numberCites = new List<string>();   // "file:§N"
        var nameCites = new List<string>();     // "file:Name"
        var unresolved = new List<string>();

        foreach (var path in SourceFiles("res://src"))
        {
            var text = ReadTextFile(path);
            if (string.IsNullOrEmpty(text)) continue;

            var file = path[(path.LastIndexOf('/') + 1)..];

            // The numbered form: the token, optionally with its extension and optionally with a closing
            // `</c>` tag, then a section sign and a number. The tag is optional because half of these sit in
            // doc comments and half in line comments. Spelled as a pattern rather than as examples, for the
            // reason given over the named form below — this method's own comment is one of the files it
            // reads, and break-test 5 caught it citing an example section as though it meant it.
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(
                         text, @"handoff(?:\.md)?(?:</c>)?\s*§\s*(\d+)"))
            {
                var n = int.Parse(m.Groups[1].Value);
                numberCites.Add($"{file} §{n}");
                if (!numbered.ContainsKey(n)) unresolved.Add($"{file} cites §{n}, which does not exist");
            }

            // The named form: the token, a comma, then a quoted section title. In one citation that title
            // wraps across two comment lines, so the span is taken out of the file as a whole and then
            // un-commented rather than being matched inside a single line.
            //
            // **The examples that used to sit here were deleted, and that is worth recording rather than
            // tidying away.** Spelling them out literally made this method's own doc comment a citation,
            // and the gate correctly failed on it the first time it ran: a wrapped example carries a
            // literal `\n`, which is not the real thing an un-commenter has to survive. A gate that reads
            // source is a gate that reads its own source, so its explanation has to describe the pattern
            // instead of instantiating it.
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(
                         text, @"handoff(?:\.md)?(?:</c>)?,\s*""",
                         System.Text.RegularExpressions.RegexOptions.None))
            {
                var from = m.Index + m.Length;
                var to = text.IndexOf('"', from);
                if (to < 0) continue;

                var cited = NormaliseCitation(text[from..to]);
                if (cited.Length == 0) continue;

                nameCites.Add($"{file} \"{cited}\"");

                // A prefix match, both ways round: the code may cite a shortened title, and it may also
                // wrap mid-title so that what survives is shorter than the heading.
                if (!titles.Any(t => t.StartsWith(cited, StringComparison.OrdinalIgnoreCase)))
                    unresolved.Add($"{file} cites \"{cited}\", which names no section");
            }
        }

        // **Before believing any of the above.** A regex that matched nothing, or a walker that opened no
        // files, produces an empty offender list and a green run — the fifth blind-spot class, and this gate
        // would be its purest instance. The numbers are lower bounds rather than exact counts, so the check
        // does not go red the day someone adds a fourteenth comment.
        Check($"the walk actually found citations ({numberCites.Count} numbered, {nameCites.Count} named)",
            numberCites.Count >= 4 && nameCites.Count >= 3);

        Check($"and every one of them resolves{Listing(unresolved)}", unresolved.Count == 0);
    }

    /// <summary>Read a text file under <c>res://</c>, or "" if it is not there.</summary>
    /// <remarks>
    /// <c>.cs</c> and <c>.md</c> files are not imported resources, so <c>FileAccess</c> is the only way to
    /// see them and <c>res://</c> is the project directory itself. That holds for a run from source, which
    /// is the only way <c>--verify-slice</c> is ever invoked; in an exported build these files are absent and
    /// the first check in <see cref="GateDocCitations"/> would say so rather than passing quietly.
    /// </remarks>
    private static string ReadTextFile(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return file == null ? "" : file.GetAsText();
    }

    /// <summary>Every <c>.cs</c> file under a directory, recursively.</summary>
    private static List<string> SourceFiles(string root)
    {
        var found = new List<string>();
        var pending = new Queue<string>();
        pending.Enqueue(root);

        while (pending.Count > 0)
        {
            var dirPath = pending.Dequeue();
            using var dir = DirAccess.Open(dirPath);
            if (dir == null) continue;

            dir.ListDirBegin();
            for (var entry = dir.GetNext(); entry != ""; entry = dir.GetNext())
            {
                if (dir.CurrentIsDir()) pending.Enqueue($"{dirPath}/{entry}");
                else if (entry.EndsWith(".cs")) found.Add($"{dirPath}/{entry}");
            }
            dir.ListDirEnd();
        }

        return found;
    }

    /// <summary>
    /// Turn a quoted section name as it appears in source — possibly wrapped across lines, with comment
    /// markers in the middle — into the bare title.
    /// </summary>
    private static string NormaliseCitation(string raw)
    {
        // Comment markers first, then all whitespace to single spaces, so a title split over two lines comes
        // back as one. The trailing comma in `"…ejection failure,"` is inside the quotes in the one citation
        // that has it, so punctuation comes off the end as well.
        var text = System.Text.RegularExpressions.Regex.Replace(raw, @"^\s*///?/?", "",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return text.TrimEnd(',', '.', ';', ':');
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
