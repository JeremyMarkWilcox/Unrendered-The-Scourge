using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.Tools;

/// <summary>
/// The content bill, measured against what is actually on disk. Run with
/// <c>godot --headless --path . -- --verify-content</c>.
///
/// <para><b>It always exits 0, and that is the point</b> (docs/build-plan.md §6). The gate suite asserts what
/// must be true *now* and is run unconditionally; this measures how much of `placement.md` §1 has been
/// authored *yet*. The two cannot be the same command: a bill folded into the gates would sit red for months
/// while chapters are being built, and a red result that is nobody's fault is how a suite gets ignored. The
/// same numbers become gates when a chapter is declared sealed — the deciding factor is *when you promote
/// them*, never what they say.</para>
///
/// <para><b>This is the one file in the project allowed to compare an authored value to a constant.</b>
/// Everywhere else the rule is that a check is a relationship between two authored values, because a
/// hard-coded number goes red the day Jeremy retunes something. A bill is inherently a comparison against a
/// design target, and the target is the thing being tracked — so the numbers below are transcribed from
/// `placement.md` §1 with the section cited on every row, and a disagreement means *either* the content or
/// the doc has moved, which is exactly the conversation worth having.</para>
///
/// <para><b>Nothing here prints <c>ok</c> for an empty set.</b> §6's own warning is that this file will write
/// dozens of universals over content that does not exist, and unit 4b established what that costs: a rule
/// quantified over nothing is a wish. So every row prints its counts, and a row with a target and nothing
/// authored reads <c>todo</c> rather than passing.</para>
/// </summary>
public static class ContentReport
{
    private static int _todo;
    private static int _wrong;

    public static int Run()
    {
        _todo = 0;
        _wrong = 0;

        var rooms = NavigationManager.Instance?.Rooms
            .Where(kv => !kv.Key.StartsWith("verify_"))
            .ToDictionary(kv => kv.Key, kv => kv.Value)
            ?? new Dictionary<string, RoomData>();

        Header("the bill  what placement.md §1 asks for, against what is on disk");
        Screens(rooms);
        Creatures(rooms);
        Pockets();
        Mending(rooms);
        Materials(rooms);
        Arsenal(rooms);
        OptionalFights(rooms);

        Header("rules that cannot be checked, and are documented instead");
        Unfalsifiable();

        Header("rules already gated in --verify-slice");
        AlreadyGated();

        GD.Print("");
        GD.Print(new string('-', 78));
        GD.Print($"  CONTENT REPORT — {_todo} row(s) still to author, {_wrong} disagreeing with the bill");
        GD.Print("  This never fails. It is a to-do list until a chapter is sealed (build-plan.md §6).");
        GD.Print(new string('-', 78));

        // Always 0. See the class summary.
        return 0;
    }

    // ---- The bill --------------------------------------------------------

    private static void Screens(Dictionary<string, RoomData> rooms)
    {
        // Per chapter, because the bill is per chapter and a total of 59 hides a chapter with none.
        foreach (var (chapter, want) in new[] { ("ch1", 16), ("ch2", 19), ("ch3", 23) })
            Row($"{chapter} Waking screens", rooms.Values.Count(r => r.ChapterId == chapter), want,
                approximate: true);

        Row("camps (one per chapter)", rooms.Values.Count(CampManager.IsCamp), 3);

        // **Hotspots you cannot click because another one is on top of them.** Reported rather than gated, and
        // that split is the point: *where* a region sits is judged by eye and is Jeremy's (`handoff.md` §2), so
        // a check that failed would fight him every time he drags something — but two overlapping regions mean
        // the lower one is unreachable, which is a real bug he cannot see from the inspector.
        //
        // **Added 2026-08-05 after placing fourteen pocket doors on one uniform placeholder rect that sat under
        // the right-hand exit in thirteen of them.** Jeremy found it by playing: *"most of the pockets do
        // overlap with the doors to the right."* Nothing could have caught it — regions had never been crossed
        // against each other, which is unit 4b's rule (*whenever one authored collection names an id belonging
        // to another, the crossing is the check*) applied to a collection crossing **itself**.
        //
        // **A pair sharing a flag with opposite values is excluded, because it can never both be live.** That is
        // the entrance-and-remnant pattern from unit 6a, where the two *should* occupy the same spot: they are
        // one object at two times. Deriving the exemption from the flags rather than from the ids means the
        // freed-NPC pattern and anything else built the same way is covered without being listed.
        var stacked = new List<string>();
        foreach (var (roomId, room) in rooms)
        {
            var spots = (room?.Hotspots ?? new Godot.Collections.Array<HotspotData>())
                .Where(h => h != null).ToList();

            for (var i = 0; i < spots.Count; i++)
            for (var j = i + 1; j < spots.Count; j++)
            {
                var (a, b) = (spots[i], spots[j]);
                if (!a.Region.Intersects(b.Region)) continue;
                if (!string.IsNullOrEmpty(a.RequiredFlag) &&
                    a.RequiredFlag == b.RequiredFlag &&
                    a.RequiredFlagValue != b.RequiredFlagValue) continue;
                stacked.Add($"{roomId}: {a.Id} / {b.Id}");
            }
        }

        Note(stacked.Count == 0
            ? $"no two hotspots overlap, so nothing is unclickable ({rooms.Count} screens swept)"
            : $"overlapping hotspot regions, so the lower one cannot be clicked: {stacked.Count} "
              + $"({string.Join("; ", stacked.Take(6))}{(stacked.Count > 6 ? "; …" : "")})", false);
    }

    private static void Creatures(Dictionary<string, RoomData> rooms)
    {
        var placements = rooms.Values.SelectMany(r => r.Entities).Where(p => p?.Entity != null).ToList();

        // **12 → 11 on 2026-08-05, and the 12 was an arithmetic error rather than missing content.**
        // `items.md` §10's pair matrix is the authority and it has exactly **nine rows**, of which row 9 is
        // *"**The Beast** — the only non-human, and the only one seen more than three times"*. So the Beast is
        // **one of the nine types**, not an addition to them, and rule 16 agrees from the other side: *"nine
        // creature types, each seen three times (the Beast six)"*, which is 8×3 + 6 = 30 regular fights + 3
        // bosses = 33.
        //
        // Nine types become **eleven files** because the Beast is authored as three variants (decision 5 — one
        // archetype, three looks). `build-plan.md` §2 got there as *3 + 3 + 3 = 9 archetypes, plus Beast ×3
        // variants* = 12, which counts the Beast twice: once as chapter 3's third type and again as its own
        // variants.
        //
        // **It survived because it was off by exactly one in the direction that looks like work to do.** A row
        // reading `11 / 12` says *author one more creature*, and there is no creature to author — every one of
        // the 33 fights is placed and *bodies that free nobody* reads 9 of 9. Jeremy, shown the derivation:
        // *"I think you're correct and it was a mistake written in at some point."* The lesson is the one this
        // tool's own class comment already states — a disagreement between content and bill means **either** has
        // moved, and the bill is the one that had.
        Row("creature and beast files on disk", FilesIn(GameConfig.EntityDataDir, "boss_", excluding: true), 11);

        // The parenthetical said "(Iris outstanding)" until she was placed the same day. Kept as a plain count:
        // naming the missing one was useful while one was missing and is a stale comment the moment it lands.
        Row("— boss files", FilesIn(GameConfig.EntityDataDir, "boss_"), 3);
        Row("fights placed", placements.Count, 33);

        // Split the way §1 splits it, because 33 = 30 regular + 3 bosses and the bosses are the missing half.
        // ~~A boss is the one creature with no `PurifiedName` *and* a pocket to be severed in — but no boss is
        // authored yet, so this counts what it can: never-human bodies, which today is the Beast only.~~
        //
        // **Two of the three landed 2026-08-05, so this row now reads 8 of 9 and the remainder is Iris.** The
        // struck sentence also carried a definition that Jeremy's ruling that day retired: a boss is *not*
        // "the creature with no `PurifiedName` and a pocket to be severed in". The Blacksmith has a pocket and
        // is a person, and he still has no `PurifiedName` — because the name records **who walks away**, and
        // nobody walks away from his fight. What makes something a boss is the aura (`EntityData.CorruptionAura`,
        // the boss marker); what a blank `PurifiedName` marks is a fight that frees nobody. Those were the same
        // set for as long as the Beast was the only member, which is how one word did both jobs unnoticed.
        var neverHuman = placements.Count(p => string.IsNullOrEmpty(p.Entity.PurifiedName));
        Row("— of those, bodies that free nobody (Beasts + bosses)", neverHuman, 9);

        Row("people freed (flag-gated NPC hotspots)",
            rooms.Values.SelectMany(r => r.Hotspots)
                .Count(h => h is { Kind: HotspotKind.Examine } &&
                            h.RequiredFlag.StartsWith("freed.")), 24);

        Row("her scattered notes", rooms.Values.SelectMany(r => r.Hotspots)
            .Count(h => h != null && h.SetsFlag.StartsWith("note.")), 4);
    }

    /// <summary>
    /// The reference pocket, excluded from the content bill by name. <b>It is scaffolding, not one of the
    /// fourteen</b> — `handoff.md` calls it *"a reference implementation, not content"*, it stands on no screen,
    /// and it grants nothing so that it spends none of the eight-point Resolve budget.
    /// </summary>
    /// <remarks>
    /// Added 2026-08-05 with unit 10, and it is the same lesson `GateTravel` paid for when a synthetic pocket
    /// turned a count check red at *2 of 1*: **a count of authored things cannot tell a fixture from content
    /// unless the fixture is filtered out by name.** Before the fourteen existed the probe read as 1 of 14 and
    /// looked like progress; the moment they were authored it would have read 15 of 14 and printed `!!`, which
    /// this tool reserves for the content and the bill genuinely disagreeing.
    /// <para>
    /// It is deliberately *not* filtered from `--verify-slice`'s census, which asserts at least one pocket still
    /// has a scene. There the probe is doing real work: with fourteen stand-ins it is the only thing keeping the
    /// crossing reachable in play.
    /// </para>
    /// </remarks>
    /// <para>
    /// <b>Internal rather than private since 2026-08-06</b>, because <see cref="SliceVerifier"/>'s door sweeps
    /// need the same exclusion and for the same reason. A second copy of the string would be a second answer to
    /// *which pocket is scaffolding* — and the two would disagree the day the probe is renamed, in the direction
    /// that makes the reference pocket look like content missing a door.
    /// </para>
    internal const string ReferencePocketId = "dream_probe";

    private static void Pockets()
    {
        var pockets = PocketManager.Instance?.Pockets
            .Where(kv => !kv.Key.StartsWith("verify_") && kv.Key != ReferencePocketId)
            .Select(kv => kv.Value)
            .ToList() ?? new List<PocketData>();

        // Counted from the registry rather than off `FilesIn`, so the reference pocket can be excluded by name.
        // `FilesIn` cannot do that, and the two agree on everything else because the "every file registered"
        // crossing in `--verify-slice` says so.
        Row("pocket files on disk", pockets.Count, 14);
        Row("— knowledge pockets", pockets.Count(p => p.Kind == PocketKind.Knowledge), 8);
        Row("— knot pockets", pockets.Count(p => p.Kind == PocketKind.Knot), 3);
        Row("— boss pockets", pockets.Count(p => p.Kind == PocketKind.Boss), 3);

        // **The 3D debt, and after unit 10 this is the only row that carries it.** Authoring the fourteen
        // `PocketData` files closes every row above and the entrances row below, because all of that is what a
        // pocket *gives* — 2D content that was parked behind 3D scenes it never needed. What is genuinely
        // outstanding is the walking, and this is the row that says so.
        //
        // Without it the report would read `14 / 14` pockets and be lying by omission: the rows above cannot
        // tell a pocket you walk from one you click, and nine `todo` rows collapsing to `ok` in a single
        // sitting is exactly the shape that reads as *finished*. §3's blind-spot classes are mostly this —
        // a measurement that stopped being able to see the thing it was hired to watch.
        Row("— of those, with a 3D scene built (the rest grant on click — unit 10)",
            pockets.Count(p => p.HasScene), 14);

        // **The one thing unit 10 leaves genuinely owed, recorded so it fires rather than being remembered.**
        // A boss pocket severs `TargetEntityId` and that is the only way a tether is ever cut, so a boss pocket
        // naming nobody is a fight that can never be won. `ch3_demon` is authored blank on purpose because Iris
        // is not placed — `items.md` §10 leaves her damage pair unfixed until the finale is written — and
        // naming her here would be a pocket pointing at a creature that does not exist.
        //
        // Written in the direction that **survives**, which is the repair stations' lesson from earlier the same
        // day: it is quiet while she is unplaced and becomes a to-do row the moment a third boss body exists.
        // Naming the id today would have been red for something that is not a defect.
        var bossPockets = pockets.Where(p => p.Kind == PocketKind.Boss).ToList();
        var severing = bossPockets.Count(p => !string.IsNullOrEmpty(p.TargetEntityId));
        Row("— boss pockets that sever somebody (blank until that boss is placed)",
            severing, bossPockets.Count);

        // Rule 6, and the one row where being *over* is as wrong as being under: eight pockets, eight
        // upgrades, and nothing else in the game may add to that count.
        Row("max-Resolve grants (rule 6 — exactly one per knowledge pocket)",
            pockets.Sum(p => p.GrantsMaxResolve), 8);

        // Rule 6's other half, and **the shape of this check is a correction the tool earned on its first
        // run.** It originally flagged any pocket where `Kind == Knowledge` disagreed with
        // `GrantsMaxResolve > 0`, and immediately reported the reference pocket as a violation — which it is
        // not. `dream_probe` is a `Knowledge` pocket granting **zero** on purpose, so that the placeholder
        // spends none of the eight-point budget.
        //
        // The rule as written has two halves that fail differently: **a knot or boss pocket granting a point
        // is a violation** (it puts a ninth upgrade in the game), while **a knowledge pocket granting none is
        // merely unfinished** — which the total row above already reports as a to-do. Conflating them made an
        // unfinished thing look broken, which is the same mistake the row logic made.
        var overBudget = pockets
            .Where(p => p.Kind != PocketKind.Knowledge && p.GrantsMaxResolve > 0)
            .Select(p => p.Id)
            .ToList();
        var overGranting = pockets.Where(p => p.GrantsMaxResolve > 1).Select(p => p.Id).ToList();
        var offenders = overBudget.Concat(overGranting).Distinct().ToList();

        Note(offenders.Count == 0
            ? $"rule 6: no pocket grants a point it should not ({pockets.Count} checked — a knowledge "
              + "pocket granting none is unfinished, not wrong)"
            : $"rule 6 VIOLATED by {string.Join(", ", offenders)}", offenders.Count > 0);

        // Rule-6-shaped but with a tighter number, ruled 2026-08-04: **exactly two verbs are ever granted**,
        // from the anchors of boss pockets 1 and 2. Suppress is held from the first frame so nothing grants
        // it, and boss pocket 3 grants nothing because it is the finale.
        var verbGrants = pockets.Where(p => p.GrantsAbility != DreamVerb.None).ToList();
        Row("Dream verbs granted (Float and Flare, from boss anchors)", verbGrants.Count, 2);

        // Two ways this goes wrong that a count cannot see: a non-boss pocket teaching a verb, and two
        // pockets teaching the same one. Both are over-budget rather than unfinished, so both are `!!`.
        var verbOffenders = verbGrants.Where(p => p.Kind != PocketKind.Boss).Select(p => p.Id)
            .Concat(verbGrants.GroupBy(p => p.GrantsAbility).Where(g => g.Count() > 1)
                .SelectMany(g => g.Select(p => p.Id)))
            .Concat(verbGrants.Where(p => p.GrantsAbility == DreamVerb.Suppress).Select(p => p.Id))
            .Distinct()
            .ToList();
        Note(verbOffenders.Count == 0
            ? $"verbs: each granted once, by a boss pocket, and never Suppress ({verbGrants.Count} checked)"
            : $"verb grants VIOLATED by {string.Join(", ", verbOffenders)}", verbOffenders.Count > 0);

        // **Counted as *pockets that have a door*, not as doors — corrected 2026-08-06.** It read
        // `Count(h => h is { Kind: CrossToDream })`, and a count of doors cannot see a door pointed at the wrong
        // pocket: retyping one entrance's `PocketId` left the finale unreachable and this row still printed
        // `14 / 14`. That is the report's version of the gate `GateContent` grew the same day, and the honest
        // form of the row is the one that matches the rule — *every pocket is opened by something* — rather than
        // the one that matches the file count.
        var doorsTo = NavigationManager.Instance?.Rooms
            .Where(kv => !kv.Key.StartsWith("verify_"))
            .SelectMany(kv => kv.Value.Hotspots)
            .Where(h => h is { Kind: HotspotKind.CrossToDream })
            .Select(h => h.PocketId)
            .ToHashSet() ?? new HashSet<string>();

        Row("pockets with an entrance on a Waking screen", pockets.Count(p => doorsTo.Contains(p.Id)), 14);
    }

    /// <summary>
    /// <b>`placement.md` rule 17 — every optional fight must be worth having taken.</b> The last placement rule
    /// with no witness anywhere, measurable since the three bosses were placed.
    /// </summary>
    /// <remarks>
    /// <b>"Optional" is derived from the door graph: a fight is optional when the finale is still reachable with
    /// its screen taken out of the world.</b> That reading is not a guess — it reproduces the rule's own
    /// arithmetic exactly. §1 says *"the reward may be a blight (9 of the 13 are)"*, and this walk finds
    /// **13** optional fights of which **9** carry a mend board (8 blights plus the exorcism engine, whose board
    /// hangs on a `Station` — `MendManager.CarriesABoard`, widened 2026-08-05). A definition that lands on both
    /// numbers is the one the sentence was written against.
    /// <para>
    /// <b>The competing reading was measured and rejected.</b> *"Optional means removing the screen loses no
    /// screen at all"* — the leaf-node reading — finds **7**, matches neither number, and would have made the
    /// rule vacuously satisfied. It is recorded because it is the reading a second pass would arrive at, and
    /// because the two differ on exactly one screen that matters.
    /// </para>
    /// <para>
    /// <b>A row and not a gate, on purpose.</b> `ch1_bramble` fails it: the messenger stands in the corridor to
    /// the Stonefield and the screen pays **nothing else** — and §1 is explicit that freeing a person does not
    /// count, since all 30 regular fights do that. Whether that is a hole to author into or a screen that should
    /// not have been counted optional is a design call, so this states the measurement and names the offender
    /// rather than turning Jeremy's map red. **It becomes a `--verify-slice` gate the day that screen is
    /// settled**, which is the direction that survives: a report that already names the one exception is
    /// cheaper to promote than a gate authored around it.
    /// </para>
    /// <para>
    /// The finale is derived rather than typed — the chapter 3 screen holding a body that projects an aura,
    /// which is the boss marker (`combat.md` §6) — so renaming a room moves the anchor with it. With no chapter
    /// 3 boss placed there is nothing to measure against and the row says so instead of quietly passing.
    /// </para>
    /// </remarks>
    private static void OptionalFights(Dictionary<string, RoomData> rooms)
    {
        var unpaid = OptionalFightsPayingNothing(rooms, out var optional, out var boards);

        Note(optional < 0
                ? "rule 17 — no chapter 3 boss is placed, so there is no finale to measure 'optional' against"
                : unpaid.Count == 0
                    ? $"rule 17 — every optional fight pays something ({optional} optional, {boards} of them a board)"
                    : $"rule 17 — {optional} optional fights, {boards} carrying a board, but "
                      + $"{string.Join(", ", unpaid)} pays nothing but the person it frees",
            unpaid.Count > 0);
    }

    /// <summary>
    /// The derivation behind rule 17, shared with <see cref="SliceVerifier"/>. Returns the optional fights that
    /// pay nothing; <paramref name="optional"/> comes back <c>-1</c> when there is no finale to measure against.
    /// </summary>
    /// <remarks>
    /// <b>One derivation with two consumers, and the split is deliberate.</b> The row above prints the counts
    /// because they are how anyone checks the definition is still the one §1 was written against (13 and 9);
    /// the gate in <c>GateContent</c> asserts the list is empty, because since 2026-08-06 it is. Copying the
    /// walk into the verifier would have been two answers to *which fights are optional*, and the two would
    /// have drifted the first time a door moved.
    /// </remarks>
    internal static List<string> OptionalFightsPayingNothing(
        Dictionary<string, RoomData> rooms, out int optional, out int boards)
    {
        optional = -1;
        boards = 0;

        var finale = rooms
            .Where(kv => kv.Value?.ChapterId == "ch3")
            .Where(kv => kv.Value.Entities.Any(p => p?.Entity is { CorruptionAura: > 0f }))
            .Select(kv => kv.Key)
            .OrderBy(id => id)
            .FirstOrDefault();

        if (finale == null) return new List<string>();

        var doors = rooms.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Hotspots
                .Where(h => h is { Kind: HotspotKind.Navigate } && !string.IsNullOrEmpty(h.TargetRoomId))
                .Select(h => h.TargetRoomId)
                .ToList());

        // Flags are deliberately ignored, exactly as the camp-reachability walk in `GateContent` ignores them:
        // a door you have to earn is design, and what is being asked here is whether the *map* forces you onto
        // this screen at all.
        HashSet<string> ReachableWithout(string missing)
        {
            var seen = new HashSet<string> { GameConfig.StartingRoomId };
            var queue = new Queue<string>(seen);
            while (queue.Count > 0)
            {
                if (!doors.TryGetValue(queue.Dequeue(), out var exits)) continue;
                foreach (var next in exits)
                    if (next != missing && seen.Add(next)) queue.Enqueue(next);
            }
            return seen;
        }

        var peopled = rooms
            .Where(kv => kv.Value.Entities.Any(p => p?.Entity != null))
            .Select(kv => kv.Key)
            .OrderBy(id => id)
            .ToList();

        var optionalRooms = peopled
            .Where(id => id != finale && ReachableWithout(id).Contains(finale))
            .ToList();

        // What §1 accepts as payment: a blight, a major repair's station, a Dream pocket, a field station, one
        // of her notes or a material cache — the last two both being `Harvest`. Freeing a person is **not** on
        // this list and must never be added to it: every regular fight frees someone, so counting it would make
        // the rule true by construction, which is precisely the trap §1 calls out in its own wording.
        bool Pays(string id) => rooms[id].Hotspots.Any(h => h is
        {
            Kind: HotspotKind.Mend or HotspotKind.Station or HotspotKind.CrossToDream or HotspotKind.Harvest,
        });

        optional = optionalRooms.Count;
        boards = optionalRooms.Count(id => rooms[id].Hotspots.Any(h =>
            h is { Kind: HotspotKind.Mend or HotspotKind.Station }));

        return optionalRooms.Where(id => !Pays(id)).ToList();
    }

    private static void Mending(Dictionary<string, RoomData> rooms)
    {
        var blights = rooms.Values.SelectMany(r => r.Hotspots)
            .Where(h => h is { Kind: HotspotKind.Mend }).ToList();

        // Counted over *every* hotspot rather than off `blights`, because the three major repairs hang their
        // board on a `Station` (`MendManager.CarriesABoard`, widened 2026-08-05) while this row was keyed on
        // `HotspotKind.Mend` alone. So it read `12 / 15` for the whole day after the boards landed, naming in
        // its own label the three things it could not see — the same blind-spot class as the anvil census that
        // asked only whether *some* room built the station. **The kind is not what makes a board; the board
        // is.** Deliberately *not* asked through `CarriesABoard` itself, which additionally requires the
        // repair to have been assembled: that is live flag state, and a census of authored content must not
        // move when a player crafts something.
        var boards = rooms.Values.SelectMany(r => r.Hotspots)
            .Where(h => h?.MendPuzzle != null).ToList();

        Row("blights", blights.Count, 12);
        Row("jigsaws (12 blights + 3 major repairs)", boards.Count, 15);

        foreach (var (chapter, want) in new[] { ("ch1", 2), ("ch2", 4), ("ch3", 6) })
            Row($"— {chapter}", rooms.Where(kv => kv.Value.ChapterId == chapter)
                .SelectMany(kv => kv.Value.Hotspots)
                .Count(h => h is { Kind: HotspotKind.Mend }), want);
    }

    private static void Materials(Dictionary<string, RoomData> rooms)
    {
        var nodes = rooms.Values.SelectMany(r => r.Hotspots)
            .Where(h => h is { Kind: HotspotKind.Harvest }).ToList();

        // The two scarce nouns, both finite by construction (rule 13). Graveflower is not a node at all — it
        // is a mend yield — so the two are counted from different places on purpose.
        Row("Cairnstone spots (scarce, never refills)",
            nodes.Count(h => h.ItemId == "mat_cairnstone"), 6);
        Row("Graveflower yielded by mending (rule 13's other scarce noun)",
            rooms.Values.SelectMany(r => r.Hotspots)
                .Where(h => h is { Kind: HotspotKind.Mend })
                .Sum(h => h.ItemId == "herb_graveflower" ? h.ItemAmount : 0), 12);

        Row("Beast carcass harvests", nodes.Count(h => h.ItemId == "mat_tallow" &&
            h.RequiredFlag.StartsWith("defeated.")), 6);

        // Rule 13 as arithmetic is the check §6 singles out — *"the one that would have caught the Cairnstone
        // softlock the day it was authored"* — and it needs a demand side that does not exist yet: which
        // recipes are reachable in which window depends on where blueprints are granted, and no pocket grants
        // one. Reported as blocked rather than faked, because a supply-versus-demand sum with demand
        // hard-coded would be this file agreeing with itself.
        var cairnDemand = InventoryCraftingManager.Instance?.Recipes.Values
            .Where(r => r != null)
            .Sum(r => r.Inputs.Count(i => i.ItemId == "mat_cairnstone")) ?? 0;
        Note($"rule 13: {nodes.Count(h => h.ItemId == "mat_cairnstone")} Cairnstone in the world against "
            + $"{cairnDemand} recipe(s) that want it — the per-window form needs blueprint placement first",
            false);
    }

    private static void Arsenal(Dictionary<string, RoomData> rooms)
    {
        var inv = InventoryCraftingManager.Instance;

        // Both targets were reconciled against `build-plan.md` §2 on 2026-08-05, and each was wrong in a
        // different direction. **Items: 39 → 38, and the row is no longer approximate.** §2 derives the total
        // item by item — 8 materials, a catalyst, 4 salves and brews, 6 traps, 2 weapons, 3 coats, 3 tools,
        // 11 pages — and the 27 non-page files on disk match the 27 non-page entries exactly, so the only
        // thing owed is the eleventh page (her fourth note). 39 was never derived from anything; it was typed.
        // The `±` convention belongs to targets the map is allowed to settle, and §2 marks the screens and the
        // commons that way while writing this one as a flat 38 — so an off-by-one here is a missing file, not
        // slack, and `~` would file it under intentional. This row is the one this file's own `Slack` remark
        // warns about (twelve unwritten items behind a friendlier glyph); exact is the direction that remark
        // argues for.
        Row("items", FilesIn(GameConfig.ItemDataDir), 38);

        // **Recipes: 18 → 20, exact, and the roster is complete.** §2's list reads *4 salves, 6 traps, Bell
        // Maul, 3 coats, 3 tools, the engine* = 18, written before the three major repairs landed on
        // 2026-08-05 — but **the engine is one of those three repairs, not a recipe beside them**:
        // `repair_exorcism_engine.tres` is the `ch3_breach` station's recipe, priced at §20's 8 Graveflower,
        // and it is the one file carrying `RequiresBothTracks`. So the total is 17 + 3 = 20, and the naive
        // correction to 21 double-counts it. Every recipe in the game is now enumerable by name, which is why
        // the `~` §2 carried is retired with the number: a tilde means *the map may still settle this*, and
        // there is nothing left here for a map to settle.
        //
        // **This retires "the engine recipe is deferred" wherever it is still written** (`build-plan.md` §8's
        // chapter-2 row, `handoff.md`'s 17-of-18 count). What §20 defers is the *route* — the engine is
        // good-ending-only, so nothing forces her notes or the 8 Graveflower to be placed — and that was read
        // as deferring the file. It rode in with the repairs the same day and nobody moved the count.
        Row("recipes", FilesIn(GameConfig.RecipeDataDir), 20);

        // Rule 15, and it is checkable today because the arsenal is complete. Everything that is not a Salve
        // must be an object you own for the rest of the game.
        var consumables = inv?.Catalogue.Values
            .Where(i => i != null && i.Category != ItemCategory.Salve && i.VitalityRestore > 0f)
            .Select(i => i.Id).ToList() ?? new List<string>();
        Note(consumables.Count == 0
            ? $"rule 15: nothing but a Salve is consumable ({inv?.Catalogue.Count ?? 0} items checked)"
            : $"rule 15 VIOLATED by {string.Join(", ", consumables)}", consumables.Count > 0);

        // Which recipes cannot be reached at all — the honest measure of what F6 is standing in for.
        //
        // **It was not measuring that, and unit 10 is what exposed it (2026-08-05).** It counted
        // `r.NeedsBlueprint` — every blueprint-*gated* recipe, not every *unreachable* one — so it printed a
        // flat `16` whether the world handed over nothing or everything, and went on printing 16 on the day
        // fourteen pockets made thirteen of those recipes learnable by playing. The label had always said "with
        // no in-world source" and the arithmetic had never asked about a source.
        //
        // **A count that cannot move is not a measurement**, and this one was load-bearing: it is the number
        // that decides whether `F6` can retire. I read it as 16 and told Jeremy the key was free to go, which
        // was wrong in the direction that deletes a debug key the game still needs.
        var pagesHandedOver = SliceVerifier.PagesTheWorldHandsOver(rooms);
        var unreachable = inv?.Recipes.Values
            .Where(r => r != null && r.NeedsBlueprint && !pagesHandedOver.Contains(r.RequiredBlueprintId))
            .Select(r => r.Id).OrderBy(id => id).ToList() ?? new List<string>();

        // Outstanding work rather than a contradiction, so it does not count against the summary — the
        // mechanism is built and gated, and what is left is placement.
        Note(unreachable.Count == 0
            ? $"every blueprint-gated recipe has an in-world source ({inv?.Recipes.Values.Count(r => r?.NeedsBlueprint == true) ?? 0} checked) — F6 has nothing left to stand in for"
            : $"blueprint-gated recipes with no in-world source: {unreachable.Count} "
              + $"({string.Join(", ", unreachable)}) — F6 is still the only opener for these", false);
    }

    // ---- What this tool deliberately does not do -------------------------

    private static void Unfalsifiable()
    {
        // §6: these belong with the unfalsifiable guards the handoff says to write down instead of gating.
        // Listed rather than omitted, so they are visible as *decisions* rather than gaps someone has to
        // rediscover — the same reason a rejected gate gets recorded in prose.
        Note("rule 1  — is a creature on the critical path with a makeable counter? "
            + "A judgement about level design; no field encodes 'critical path'", false);
        Note("rule 7  — are chapter 1's pockets flat? 3D geometry, and Jeremy's eye", false);
        Note("rule 8  — is the finale winnable on path materials alone? "
            + "Needs a solver over the whole graph, and the engine's optional route makes it a judgement", false);
        Note("rules 3 and 16 — nothing respawns, every fight happens once. "
            + "Behavioural, and gated in --verify-slice's encounters section rather than countable here", false);
    }

    private static void AlreadyGated()
    {
        // Named rather than silently omitted. A report that lists only what it measures reads as though the
        // rest is unchecked, which would send someone to re-implement guards that already exist.
        Note("rule 2  — one creature per screen, never the same type adjacent → `fights` section", false);
        Note("rule 4  — no screen is bare → `content` section", false);
        Note("rule 10 — every screen can be cleansed → `mending` section", false);
        Note("rule 12 — battles are 1v1 → `encounters` section", false);
        Note("rule 14 — fifteen mends, no skip route → `mending` section", false);
        // Rule 17 stood here reading *"not yet gated anywhere; needs 'optional' to be derivable, which the door
        // graph can answer once bosses land"*. **The bosses landed, and 2026-08-06 cashed the promise** — see
        // `OptionalFights` above, which measures it. It is a row rather than a gate for one reason, recorded
        // there: one screen violates it today, and which way that resolves is Jeremy's call.
    }

    // ---- Plumbing --------------------------------------------------------

    /// <summary>
    /// One bill row. <paramref name="approximate"/> marks a target the map was always allowed to settle
    /// (`placement.md` §1 marks those with ±), so being off by a little is information rather than a fault.
    /// </summary>
    private static void Row(string label, int have, int want, bool approximate = false)
    {
        // `approximate` means *the map was allowed to settle this by one or two*, not *any number will do*.
        // Tolerating an arbitrary gap here hid twelve unwritten items behind a `~` on the first run, which is
        // the same flaw as the two above wearing a friendlier glyph: a status that makes outstanding work
        // look intentional.
        const int Slack = 2;

        string status;
        if (have == want) status = "ok  ";
        else if (approximate && System.Math.Abs(have - want) <= Slack) status = "~   ";
        else if (have < want) { status = "todo"; _todo++; }

        // **Only over-budget is a contradiction**, and getting this wrong was the first thing the tool
        // taught me about itself. The first version read `have != want && have != 0` as `!!`, so *partially
        // authored* looked identical to *wrong* — 30 fights of 33 was flagged as a fault rather than as
        // progress, and the summary cried wolf on nine rows of perfectly healthy mid-authoring state. That
        // is precisely the failure §6 warned this file would have. Under-target is a to-do; over-target
        // means the bill and the content genuinely disagree, and for the scarce nouns it means a softlock.
        else { status = "!!  "; _wrong++; }

        GD.Print($"  {status} {label,-58} {have,3} / {want,-3}");
    }

    /// <summary>A statement rather than a count. Collected and reprinted after the summary.</summary>
    private static void Note(string text, bool wrong)
    {
        if (wrong) _wrong++;
        GD.Print($"  {(wrong ? "!!  " : "--  ")} {text}");
    }

    private static void Header(string title)
    {
        GD.Print("");
        GD.Print($"== {title} ".PadRight(78, '='));
    }

    /// <summary>
    /// Count `.tres` files in a directory without consulting whatever registry loaded them — the fifth
    /// blind-spot class, and the reason this is measured on disk rather than from a manager.
    /// </summary>
    /// <summary>
    /// Counts authored files, optionally only those whose name begins with <paramref name="prefix"/> —
    /// or, when <paramref name="excluding"/> is true, only those that do not.
    ///
    /// <para><b>The filter arrived with the bosses on 2026-08-05, and the reason is worth keeping.</b> The
    /// creature row's target of 12 is the pair matrix of `items.md` §10, which counts the *regular* roster;
    /// two boss files pushed the count to 13 and `Row` correctly printed `!!`, because over-target is the
    /// one status this tool treats as a contradiction rather than as progress. Widening the target to 15
    /// would have been the wrong repair — it makes one number mean two bills and the `!!` stops being able
    /// to fire on either. Two rows counting two things is what the bill actually says.</para>
    ///
    /// <para>Filtering on the filename rather than on placements is deliberate: a boss authored and never
    /// put anywhere has to stay visible, which is the whole point of counting disk against registry
    /// (`handoff.md` §3, fifth class).</para>
    /// </summary>
    private static int FilesIn(string dir, string prefix = "", bool excluding = false)
    {
        var count = 0;
        using var handle = DirAccess.Open(dir);
        if (handle == null) return 0;

        handle.ListDirBegin();
        for (var f = handle.GetNext(); f != ""; f = handle.GetNext())
        {
            if (handle.CurrentIsDir()) continue;
            if (!f.EndsWith(".tres") && !f.EndsWith(".tres.remap")) continue;
            if (prefix.Length > 0 && f.StartsWith(prefix) == excluding) continue;

            count++;
        }

        return count;
    }
}
