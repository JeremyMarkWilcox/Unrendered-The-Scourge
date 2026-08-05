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
        Arsenal();

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
    }

    private static void Creatures(Dictionary<string, RoomData> rooms)
    {
        var placements = rooms.Values.SelectMany(r => r.Entities).Where(p => p?.Entity != null).ToList();

        Row("creature and beast files on disk", FilesIn(GameConfig.EntityDataDir, "boss_", excluding: true), 12);
        Row("— boss files (Iris outstanding)", FilesIn(GameConfig.EntityDataDir, "boss_"), 3);
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

    private static void Pockets()
    {
        var pockets = PocketManager.Instance?.Pockets
            .Where(kv => !kv.Key.StartsWith("verify_"))
            .Select(kv => kv.Value)
            .ToList() ?? new List<PocketData>();

        Row("pocket files on disk", FilesIn(GameConfig.PocketDataDir), 14);
        Row("— knowledge pockets", pockets.Count(p => p.Kind == PocketKind.Knowledge), 8);
        Row("— knot pockets", pockets.Count(p => p.Kind == PocketKind.Knot), 3);
        Row("— boss pockets", pockets.Count(p => p.Kind == PocketKind.Boss), 3);

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

        Row("pocket entrances authored on Waking screens",
            NavigationManager.Instance?.Rooms
                .Where(kv => !kv.Key.StartsWith("verify_"))
                .SelectMany(kv => kv.Value.Hotspots)
                .Count(h => h is { Kind: HotspotKind.CrossToDream }) ?? 0, 14);
    }

    private static void Mending(Dictionary<string, RoomData> rooms)
    {
        var blights = rooms.Values.SelectMany(r => r.Hotspots)
            .Where(h => h is { Kind: HotspotKind.Mend }).ToList();

        Row("blights", blights.Count, 12);
        Row("jigsaws (12 blights + 3 major repairs)", blights.Count(h => h.MendPuzzle != null), 15);

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

    private static void Arsenal()
    {
        var inv = InventoryCraftingManager.Instance;

        Row("items", FilesIn(GameConfig.ItemDataDir), 39, approximate: true);
        Row("recipes", FilesIn(GameConfig.RecipeDataDir), 18, approximate: true);

        // Rule 15, and it is checkable today because the arsenal is complete. Everything that is not a Salve
        // must be an object you own for the rest of the game.
        var consumables = inv?.Catalogue.Values
            .Where(i => i != null && i.Category != ItemCategory.Salve && i.VitalityRestore > 0f)
            .Select(i => i.Id).ToList() ?? new List<string>();
        Note(consumables.Count == 0
            ? $"rule 15: nothing but a Salve is consumable ({inv?.Catalogue.Count ?? 0} items checked)"
            : $"rule 15 VIOLATED by {string.Join(", ", consumables)}", consumables.Count > 0);

        // Which recipes cannot be reached at all — the honest measure of what F6 is standing in for.
        var unreachable = inv?.Recipes.Values
            .Count(r => r != null && r.NeedsBlueprint) ?? 0;
        // Outstanding work rather than a contradiction, so it does not count against the summary — the
        // mechanism is built and gated, it simply has nothing to grant yet.
        Note($"blueprint-gated recipes with no in-world source: {unreachable} "
            + "(F6 is the only opener until a knowledge pocket is authored)", false);
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
        Note("rule 17 — every optional fight pays something → not yet gated anywhere; "
            + "needs 'optional' to be derivable, which the door graph can answer once bosses land", false);
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
