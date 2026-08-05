# The build plan — turning the bill into levels

**Written 2026-08-01, after the placement audit.** [`placement.md`](placement.md) counted everything and
gave each thing a constraint. This says **in what order it gets built, by whom, and what has to exist
first.** It is a schedule, not an authority — where it disagrees with `combat.md`, `items.md` or
`placement.md`, they win.

**The one-line version:** the design is finished enough to author against, but **six systems stand
between the bill and a level**, and three of them are new since the handoff's numbered list was written.
Meanwhile the map does not exist and only Jeremy can draw it. So the plan runs **two streams in
parallel** — Claude closing the systems, Jeremy drawing the map and laying out screen skeletons — and
they meet at a chapter-1 vertical slice.

---

## 1. Why the content cannot simply be authored today

`placement.md` is a budget. A `.tres` file is a set of fields. The gap between them is that **half the
nouns in the bill have no field to live in.** Checked field by field against `src/Data/`:

| The bill says | Where it would live | State |
|---|---|---|
| ~58 screens, connected | `RoomData` + `HotspotKind.Navigate` | ✅ **exists and is stable** — four milestones of gates over it |
| Examine text, harvest nodes, flag gates | `HotspotData` | ✅ **exists and is stable** |
| A freed person becomes an NPC | flag-gated `Examine` hotspot | ✅ **exists** — no new fields (`combat.md` §21) |
| A Beast carcass drops Tallow | flag-gated `Harvest` hotspot | ✅ **exists** — `EntityData.HarvestItemId` stays unused |
| **30 + 3 fights** | an encounter loop | ❌ **the wound ledger only.** No turn machine, no way to start one |
| **12 blights, 15 jigsaws** | a Blight kind + a puzzle seam | ✅ **built 2026-08-03 (unit 2)** — `HotspotKind.Mend`, `MendPuzzleData`, `MendManager`, `MendPanel`, and all 12 authored. The 3 majors wait on unit 4's recipes, not on this. The old `VitalityCost` hazard was a different thing and was deleted 2026-08-01 (§22, decision 6) |
| **14 Dream pockets** | a 3D pocket format | ❌ **the probe scene only**, wired into nothing. The 2D heist model is what exists and it is being deleted |
| **Stations, tool gates, permanent traps** | `RecipeData` / `ItemCategory` / `CraftBlock` | ✅ **built 2026-08-03 (unit 4a)** — §19's last table is in code, `SetsFlag` opens the three tool doors, and traps and equipment now survive death. What 4b still owes is the **roster** (~20 items, ~14 recipes) and the **crafting panel** — the signal fires and nothing listens |
| **3 camps, reachable from anywhere** | `CampManager` | ⚠️ **built, and mostly right.** Camp = the room with the bench still holds (decision 3); what is missing is the *return to camp* verb |
| **3 chapters, 2 refills, 3 knots, a Snarl** | a progression spine | ❌ **the word "chapter" appears nowhere in `src/`** |
| **Both knowledge tracks on one recipe** | the journal model | ❌ unbuilt — the engine is the only recipe that needs it |

**Three of those are new work that the handoff's "systems still unbuilt" list does not contain**, because
they were created by decisions taken after it was written: the **blight/mend layer** (§22), the **camp
menu rework** (§17 amended), and the **chapter spine** (§23 plus the material milestones). They should go
into that list rather than being discovered during authoring.

---

## 2. The content bill, in files

What "all the levels with all their content" is, counted as things that have to be made. Derived from
`placement.md` §1 and `items.md` §1–8; **±** marks a number the map will settle.

| | ch1 | ch2 | ch3 | Total |
|---|---|---|---|---|
| `RoomData` — Waking screens | ~16± | ~19± | ~23± | **~58±** |
| Camp scenes *(ch2 painted ×3)* | 1 | 1 | 1 | **3** |
| 3D pocket scenes | 3 | 5 | 6 | **14** |
| `EntityData` — creatures | 3 + boss | 3 + boss | 3 + boss | **12** *(Beast ×3 variants — see §7)* |
| Entity placements — fights | 10 | 10 | 13 | **33** |
| Blights | 2 | 4 | 6 | **12** |
| Jigsaw instances *(3 forms)* | 3 | 5 | 7 | **15** |
| Freed-NPC hotspots | 9 | 9 | 6 | **24** + Merchant, ghost, 2 apothecaries |
| Cairnstone spots | 1 | 3 | 2 | **6** |
| Beast carcass harvests | — | — | 6 | **6** |
| Commons harvest nodes | thick | thick | Bitterroot/Tallow thick, rest thin | **±** |
| Her notes | — | ± | ± | **4** *(**3 teach**, one per blueprint-gated Physick recipe, and 1 silent — corrected 2026-08-04, decision 11)* |
| `ItemData` | | | | **38** — 8 materials, a catalyst, 4 salves and brews, 6 traps, 2 weapons, 3 coats, 3 tools, **11 pages** *(7 craft families + her 4 notes)*. 27 on disk; the 11 pages are **generated from ids the recipes already name**, not authored |
| `RecipeData` | | | | **~18** — 4 salves, 6 traps, Bell Maul, 3 coats, 3 tools, the engine |
| Max-Resolve grants | 2 | 4 | 2 | **8** — one per knowledge pocket, and nothing else may grant one |

**Writing bill, counted separately because it is the thing most likely to be underestimated:** 24 freed
lines, 12 blight descriptions, 3 knot beats keyed to the *ordinal* not the pocket, 4 notes, 3 boss
scenes, and an examine line for most of ~58 screens.

---

## 3. The two streams

**They do not block each other, and that is the whole reason the plan is shaped this way.**

| | Stream A — systems | Stream B — the map and the skeleton |
|---|---|---|
| **Who** | Claude | Jeremy draws, Claude generates *(decision 1, §7)* |
| **What** | the units in §4 | the screen ledger (§5), then generated skeletons, then layout and prose in the editor |
| **Needs** | nothing | nothing — pass 1 uses only fields that already exist |
| **Verified by** | `--verify-slice` | the bill checker (§6) |

The interface between them is the same one that has worked since 2026-07-29: **a published contract**.
For panels that is `%UniqueName`s; for content it is the **flag vocabulary and id conventions** in §5.

---

## 4. Stream A — the units, in order

Ordered by *how much authoring each one unblocks*, not by size. Each ends green on `--verify-slice`
with break-tested gates, as every unit so far has.

**Unit 1 — the chapter spine and the flag vocabulary.** ✅ **BUILT 2026-08-02 — 344 gates, all green.**
*Small, and first because everything references it.* **`ZoneData` → `ChapterData` and
`RoomData.ZoneId` → `ChapterId`** (decision 4 — a rename, done before pass 1 while it costs 8 rooms
instead of 58), the material milestone refill, a **multi-flag gate** so the Snarl can require three cut
knots (`HotspotData.RequiredFlag` was singular), and the ordinal-keyed knot beat. Publishes the flag
namespaces every later `.tres` will use, in `src/Core/Flags.cs`.

What landed, and the three shape decisions inside it:

| | |
|---|---|
| **The rename** | `RoomData.ZoneId` → `ChapterId`, 8 `.tres` rewritten. The *class* was renamed and then deleted the same day — see the callout below; only the field survives, which is the half that mattered. `ZonePhase`, the lieutenant and `zone.*.purified` were **deliberately not renamed** — unit 2 deletes them, and renaming a field on its way to deletion is work paid twice. The vocabulary is mixed for exactly one unit. |
| **`HotspotData.RequiredFlags`** | a plain `string[]`, all-true, alongside the existing scalar. No per-entry polarity: the only gate that ever needs a `false` is hide-once-done, which is always a single flag. That keeps it a string array rather than an array of sub-resources — cheap to author, and it dodges the typed-array-load hazard. |
| **`HotspotData.RefillsAtMilestone`** | **defaults to false, and the direction is the decision.** A node the generator forgets to mark fails toward a supply *tighter* than intended — visible in play, one field to fix — rather than toward a scarce material that farms forever, which would delete the economy silently. |
| **The knot beat** | `WorldStateManager.NextKnotBeatOrdinal` counts cuts, so cutting the last-listed knot first still plays beat 1 — break-tested against an index-keyed implementation, which passed four checks and failed exactly the two that encode the decision. ~~The three beat *texts* are pass-2 prose with **no home until unit 6**~~ — **corrected 2026-08-04: they land with story, which is dead last, not with unit 6.** Unit 6 owns the *slot* — a knot pocket's `Complete()` writes `knot.<id>.cut` and no more — and the mechanism the knots exist for, the three-flag Snarl door, has been built and gated since this very unit. Jeremy, asked whether he needed to rule on it: *"the three knots are just to create a closed door before the final boss"* — which is exactly the part that is done. The authoring rule the texts must obey (a beat may not name the room it fires in) is recorded on the method. |

> **A conflict between this plan and `items.md` §7, settled by Jeremy toward the two-trigger reading.**
> This section said *"the two material milestone refills hanging off the ch1 **and ch2** boss pockets"*;
> `items.md` said once, at the chapter 2 boss, *"since both refill at the same milestone."* Ruling:
> **each chapter refills when that chapter's own boss is defeated.** Chapter 3 is terminal — its boss
> is the finale.
>
> **The doc's stated reason for pooling was wrong, and finding that out is what made the ruling free.**
> `items.md` justified one milestone by saying ch1 + ch2 must pool to give 4 Cairnstone against 3
> sinks. They do pool — but **scarce nodes never re-arm at any milestone**, so refill timing never
> governed Cairnstone in the first place. The 4-against-3 slack comes from keeping what you picked up,
> not from when nodes come back. Nothing in the scarce economy moved. `items.md` §7 corrected.
>
> Built as `RefillMaterialNodes(rooms, "ch1")` and `(rooms, "ch2")`, fired at their own bosses; the
> trigger flag is `Flags.MaterialRefill(chapterId)`. Nothing sets it yet — bosses arrive in units 3
> and 6. The gate now proves **a chapter 1 boss leaves chapter 2's nodes alone**, which is a check the
> pooled call was structurally unable to make.

> **`ChapterData` was deleted the same day it was renamed, and that is the right outcome.** The class
> had no reader and never had one: `GameConfig.ZoneDataDir` was declared and never used, nothing
> loaded `data/zones/`, and everything chapter-shaped runs off `RoomData.ChapterId` strings and
> `EntityPlacement.IsZoneLieutenant`. Unit 2's choice was "build it a registry or delete it"; Jeremy:
> *"no point in having more overhead than needed."* So `ChapterData.cs`, `data/chapters/` and
> `ChapterDataDir` are gone, and **`RoomData.ChapterId` — the load-bearing half of the rename — stays**
> as a bare string.
>
> The class comes back the moment something needs to *show* a chapter: a title card, a save-slot
> label. Not before, and with a loader when it does. **One thing moved out with it** — the knot beat
> text had been given a slot on `ChapterData`, and now lands in **unit 6** with the rest of the
> pocket's grant-and-report contract. The authoring rule it carried (a beat may not name the room it
> fires in) is recorded on `WorldStateManager.NextKnotBeatOrdinal` so it cannot be lost in the gap.

**Unit 2 — the blight and mend layer (§22).** ✅ **BUILT 2026-08-03 — 439 gates, all green.** *Medium.*
A blight is not today's hazard: it charges
nothing, gates the screen's cleansing, presents a jigsaw, and pays one Graveflower, once. **The hazard
it is not was deleted on 2026-08-01** (decision 6), so this unit builds onto a clear field rather than
around a superseded one. Includes the
~~**cleansed-screen rule** — a screen with nothing Held, Blocked or Fouled left flips to~~
**cleansed-screen rule** — a screen with nothing **Held or Fouled** left flips to
`WakingBackgroundPurified`, free — which is the visual payoff for the whole Waking world and ~~currently
has~~ had no owner in code. **This is also where the zone machinery dies** (decision 4): per-screen cleansing
replaces `ZonePhase`, the lieutenant, `IsZoneLieutenant`, the Corrupted→ActiveFront flip and the
`zone.*.purified` flag, and it takes over driving `WakingBackgroundPurified`. Keep the **puzzle seam
thin**, exactly as §19 requires for recipes: solving writes
one flag and nothing else, so `--verify-slice` never touches a jigsaw.

What landed, and the four shape decisions inside it:

| | |
|---|---|
| **`HotspotKind.Mend`, not `Blight`** | every member of that enum is a verb bar `Entity` and `Workbench`, and the same kind serves a broken bridge in unit 4 — a bridge labelled `Blight` would be a lie. §22's *"**Blight** the thing, **Mend** the verb"* is about the noun, and the noun survives in the flag: `blight.<id>.mended`. |
| **Blocked is not a cleansing reason** | struck above, and §22 carries the revision. A tool gate resolves for **every** screen at once, so a screen whose only unfinished thing is a door would cleanse with no act performed on it. Costs nothing against the ledgers — every Blocked screen also holds a creature or a blight — with the **Snarl** as the one wart, cleansed from the start until unit 6 puts something on it. |
| **The yield is authored, not hard-coded** | `ItemId` / `ItemAmount` are reused rather than a new field, because a hard-coded 1 Graveflower would make the bill check a **tautology**. As authored data it is a fact the sweep can check twelve of — and it is what catches a blight quietly paying two. |
| **`MendManager` is static** | it holds no state, so an eleventh autoload would exist only to be registered. The seam is one method (`Mend`) that the panel calls and the verifier drives, so **no gate ever touches a puzzle** — which is the thin seam this section asked for, kept literally. |

The gate section is `mending`: the 12-blight bill split 2 / 4 / 6 by chapter, one Graveflower each and
none anywhere else in the game, five separate offender lists for a board that cannot be solved, exactly
three blights unreachable with no tool learned **by walking the graph with `learned.tool_*` doors shut**,
and the whole mechanism driven live on `ch1_fern_gully`. `SaveGame` **lost a field** — the only time the
format ever has — and gained a pair of checks that load a hand-authored save in the old shape, because
*"an old save still loads"* had been a claim living in a comment.

**It unblocked 12 ledger cells, not 15.** The three major repairs need `items.md` §6's key items (a
recipe — unit 4) and a reclaimed memory (a boss drop — unit 3/6), so they inherit this unit's jigsaw
later at a cost of 3 files and no system. Fifteen mends is still the total.

**Unit 3 — the encounter state machine.** *Largest.* The one piece of combat still missing: rig two
slots, click to opt in, alternating turns, placement free and acting costing a turn, flee guaranteed.
`CombatManager` already owns the arithmetic and has 39 gates over it; this gives it a caller. **Unblocks
30 of the ~58 screens**, since a fight screen is usually also something else. Carries two small things
from decision 5: an **optional texture override on `EntityPlacement`** so the Beast is one archetype with
three looks, and a ruling on what winning against a never-human thing sets.

> **✅ Built as unit 3a, 2026-08-03, 510 → 655.** It was the largest, and by the widest margin: 145 checks
> against 38 for the whole arsenal. One of decision 5's two small things was built (the win routing, as a
> derivation off `PurifiedName`) and the other was **dropped as unnecessary** — see decision 5. The
> retirement is unit 3b and is still ahead.
>
> **The last seven checks were not planned.** `--shots` — the one tool with no suite over it — caught a fight
> outliving the screen it started on, invisible to all 648 gates because every check in the `encounters`
> section stands still in one room. Same shape as unit 4a's 33 dangling harvest nodes: the plan's own
> verification step found a real defect, and the fix came with gates. Details in `handoff.md`.

**Unit 4 — the station layer and §19's shapes.** *Medium.* `HotspotKind.Station`, `RecipeData.`
`RequiresStation` / `RequiresBothTracks` / `SetsFlag`, `NotAtCamp` → `NotAtStation`, `Reagent` →
`Component`, materials as a **tally with no slots**, `SurvivesDeath` gaining `Equipment` **and `Trap`**,
a salve cap beside `RigSlotsPerFight`. **A crafted trap is an owned flag, not a stack** — the bench
refuses a second one. This is also what makes `RecipeData.SetsFlag` exist, which is how a tool gates a
door: learn the Bill Hook → flag → the bramble hotspot opens. **No new gating field is needed for tools.**

> **Split in two while building it, and ✅ both halves are built 2026-08-03 — 4a (439 → 472 checks) and
> 4b (472 → 510).** The line is where the discipline draws it — *shapes and rules first, roster second* —
> and the split paid for itself: 4b needed **no new field on any resource and no new enum member**, which
> was the stated test of whether 4a was right.
>
> **4a landed everything in the paragraph above** plus the content the mechanism needs to be *provable*:
> the **5 missing materials**, the **3 access tools** and their recipes, and **2 field stations** (the
> anvil in `ch2_smithy`, the mortar in `ch2_apothecary`). The salve cap became
> `GameConfig.SalveSlotsPerFight = 2` — this paragraph's *"a salve cap"* was loose, and `items.md` §1 is
> explicit that **there is no carry cap**; §2's number is a *per-fight slot* cap, and the two must never
> merge. `AlreadyMade` joined `CraftBlock` for the trap rule rather than reusing `MissingMaterials`,
> which would have been the smaller change and a lie.
>
> **4b was the arsenal, and it ✅ landed the same day: 14 items and 13 recipes**, so the project held 28
> items and 17 recipes — **27 since unit 3b deleted `herb_gravecap`**, which is the count on disk today.
> Two salves, two brews, six traps, two weapons and three coats — `items.md` §§2–4
> transcribed, with three departures from this paragraph worth naming:
>
> - **No trapper's stump.** It was to be the venue for the mechanical traps, and a blank `RequiresStation`
>   already means *the bench* — so the stump and the *"fused traps require `bench`"* ruling were the same
>   behaviour written twice, and one hotspot invented to enforce a restriction that buys nothing. All six
>   trap recipes leave the field blank.
> - **The 3 major repairs are still not authored**, and that is now a decision rather than an omission:
>   §6 prices each as *a reclaimed memory + key items + a jigsaw*, and two of the three have no field yet.
>   Authoring them with commons standing in for a memory would write a lie into `.tres`.
> - **The Pry Bar got its own blueprint.** 4a had it sharing `lore_craft_edges` with the Bill Hook, which
>   quietly made the authored pocket bill **7 against §8's 8** — invisible because no pocket exists to
>   count. Split, and the eight is now a gated census by name.
>
> The **two known gaps 4b inherited are still open and are both the same gap**: the **tally strip** and the
> **crafting panel**. `EventBus.CraftingRequested` still fires with nothing subscribed, so the arsenal is
> twenty-seven files that only a debug key can spend — `F7` crafts everything craftable where you stand and
> names the block reason for the rest. Both land in the editor pass, and decision 9 makes that screen *the
> journal*.
>
> **The unit's real find was a defect, not a feature: 33 harvest nodes paid out items that had never been
> written, and 439 gates were green over it.** The room sweep and the item catalogue were two authored
> collections that had never been crossed. The general rule that falls out of it: **whenever one
> authored collection names an id belonging to another, the crossing is the check, and it will not appear
> on its own.** §6's bill checker should treat that as its first class of rule.
>
> **4b's find is the sibling of that one, and it is a warning about the bill checker itself.** The verifier
> had asserted *"every weapon and trap deals some kind of harm"* since 4a and it had **never once been
> tested**, because no trap existed — so the first content that satisfied the design turned it red.
> `items.md` §3 authors two traps with no damage type on purpose (the Snare holds, the Chime reveals), and
> the gate's own comment cited a §13 rule §19 reversed on 2026-08-01. **A universal quantified over an
> empty set is not a check, it is a wish**, and §6 will write dozens of them — so a bill rule should be
> written with the content it measures, or it will encode whatever the doc said on the day it was typed.
> What replaced it is a **census**: four armed traps covering four distinct damage types, and the two inert
> ones named as `trap_snare` and `trap_chime` from both sides.

**Unit 5 — the return-to-camp verb (§17 as amended 2026-08-01).** ✅ **BUILT 2026-08-04 — 687 → 758
checks, all green.** *Small, and it got smaller.* Decision
3 settled that **a camp is still a screen you are teleported to, and you walk out of it**, which means
almost nothing about the built camp system is wrong: `IsCamp` derived from a `Workbench` hotspot, the
shared cache, arrival refill, the collapse-and-respawn path and `SurvivesDeath` all stand. **What is
added is one verb** — *return to camp*, available from anywhere in the Waking world with no run live, and
three camps to point it at. **Fusion stays camp-bench-only**, so `NotAtStation` still has to tell a field
station from the bench. Two rules to settle while building it: the verb is refused **inside a fight** and
**inside a pocket** (§17's surviving-pressures table depends on both), and a camp keeps ordinary exits,
because the walk out is the price the amendment exists to charge.

What landed, and the four things worth knowing:

| | |
|---|---|
| **It offers a choice of destination** | The one ruling the unit needed, and the docs pointed both ways. Settled toward *choose among the camps you have reached*, because **§20's revised good ending is built on backtracking being nearly free** — return-to-the-newest-only makes a chapter-3 player's trip back to chapter 1 the whole map on foot, so the smaller reading contradicts a ratified section rather than merely costing less. §17's *"no picker"* line is about the **collapse return point**, decided before this verb existed, and `ReturnCampRoomId` is untouched. Full reasoning in `combat.md` §17. |
| **The save format did not move** | "Reached" is `camp.<room>.found`, written by the same arrival that rebinds the return point — unit 2's `ZonePhases` → `screen.<id>.cleansed` trade. Old saves re-derive their one destination from the return point they already carry, and that is checked by **building a file in the pre-unit-5 shape** rather than by calling the import method, which makes it an ordering test: reverse `ImportState` and `ImportReturnCamp` and it is the only thing in 758 checks that notices. |
| **Refused in a fight, not priced as a flight** | The codebase contained the other answer — a door mid-fight flees and pays a parting blow — and copying it would have made one key *escape plus a free heal*. Gated as a **pair**: the refusal, and that the fight is still running, because `EncounterManager.OnRoomEntered` ends any fight on arrival and would have made a broken guard look fine. |
| **The panel is `FightPanel`-shaped** | No `.tscn` yet, so `CampPanel.BuildFallback` puts the published contract up in code and the verb is playable today. **A row *is* the verb** — no *Travel* button beside the list — which is §17's own *"if the player would never decline, do not ask"* rather than a shortcut. Reviewing the diff found the one thing the gates had not: `I` then `C` left both panels up, **stacking two 55% dims**, which is the exact problem `Hud.OnMendRequested` already guards against. The mend and fight cases could rely on the world being unclickable by the time they open; two key-opened panels cannot, so the exclusion moved into `Hud.ToggleSatchel` / `ToggleCampMenu` — one policy, one place — and is gated in both directions. |

**Two things rode along, both agreed on 2026-08-04 and both paid off.** The **citation gate** walks `src/`
for references to `handoff.md` sections and asserts each resolves; it went red on its first run, on this
gate's *own doc comment*, which had spelled the pattern out literally instead of describing it — a gate
that reads source reads its own source. And the **entity census** closes the last collection that could
hide a hole: creatures have no registry at all, so one authored and never placed was invisible to every
check that walks rooms. Its break-test moved **exactly one check out of 758**, which is the cleanest
possible evidence that the hole was real rather than already covered.

> **One check was deleted by its own break-test, and it is the most useful thing in the unit.** A line
> asserting *"the run is still live, so the refusal was not an ejection"* stayed **green** with the pocket
> guard removed — a `DreamRunManager` run survives ordinary navigation to a Waking room, so it passed for a
> reason other than the thing it was aimed at. That is §3's **seventh** blind-spot class, found the only way
> that class can be: by writing down which checks should go red *together* and then reading the one that did
> not move. Nothing replaced it — the harm the guard prevents is *standing at a bench with a run live*, and
> the check above it already asserts the player never got there. **A check that still passes while measuring
> nothing is worse than one check fewer**, which is how `ZoneCorruptionScalar`'s probe was retired in unit 2.
>
> **And one prediction was simply wrong, which is worth recording too.** Stripping a camp's exits was
> predicted to redden one check and reddened six: `GateWorldGraph` and `GateMending` are called *from inside*
> `GateAuthoredContent`, so they see a mutation made there. The extra five are the honest blast radius of a
> disconnected map, not false alarms — but the run order inside that gate is not what its position in
> `RunGates` suggests, and the next person to inject a fixture there should know it.

**Unit 6 — the Dream pocket format.** *Large, and it is the only unit with an unknown in it.* Handoff
units 2–4: a greybox pocket, then the seam into `Main` with its gates and the atomic grant-and-consume,
then rigid bodies. **Its deliverable for this plan is a format** — what a pocket scene is, what it grants,
how a door opens it, how a cut knot reports back — because 14 pockets cannot be authored against a
prototype.

> **Amended by unit 3b, 2026-08-03.** This paragraph used to end *"retire `DreamHeistData`,
> `DreamRunManager` and the six `dream_way_*` rooms as part of the seam, not before: they are what makes
> the slice play end to end today."* Half of that is spent. **The six rooms and the authored heist are
> gone**; the two *classes* survive, held up by a `DreamHeistData` built in code and two synthetic Dream
> rooms in `SliceVerifier`, because ~15 checks use a live run as a **probe** for four files that outlive
> unit 6 — `Hud`'s invariant 2, `CampManager`'s bench-in-a-run refusal, the crafting venue guard and
> `SaveService.HeistOwningRoom`'s stranding rule. So unit 6 inherits **no content to delete and two
> classes to replace**, and the thing to carry across is that fixture: whatever a pocket turns out to be,
> those four guards need something to be *inside* of, or they go unfalsifiable the moment
> `DreamRunManager` does.

> **✅ Unit 6a is BUILT, 2026-08-04 — 758 → 830 checks, exit 0.** *The door, the contract and the way back.*
> Scope settled in discussion: *"3D dream world is its own project and workflow. We only want to create a way
> to go to the 3D."* So 6a is the seam and the format; **Jeremy builds the pockets**, and 6b is the retirement.
>
> **The seam was mostly solved already, by how the probe was built.** `scenes/dream/DreamProbe.tscn` is a
> `Control` whose child is a full-rect `SubViewportContainer` with `DreamLook.tres` on the **container** — so
> the monochrome is a post-process on the viewport's output, anything drawn over it stays in colour, and a
> pocket drops in beside `RoomView` with no adapter. `PocketHost` hides the room view, loads on entry, frees
> on exit, and sets `RenderTargetUpdateMode = Disabled` while you are awake so the game is not paying to
> render a 3D world behind every 2D screen. **No scene change**, so autoloads, the HUD and every panel's
> state survive a crossing untouched.
>
> | | |
> |---|---|
> | **Entrance** | a `CrossToDream` hotspot naming a `PocketId`. **Never an item** — *"an environment that looks like an item, like the graveflower minigame elements."* `HotspotKind.CrossToDream` therefore **survives** the heist's retirement with a new owner, and its palette colour stops being provisional; `TakeAnchor` is the one that goes, since the objective now lives inside the 3D scene |
> | **Spent state** | two hotspots on one flag with opposite `RequiredFlagValue`s — the entrance until taken, a broken `Examine` remnant after. **Zero new fields**, the freed-NPC pattern, and now gated on a synthetic room that authors it that way |
> | **The contract** | two calls a pocket scene makes: `Complete()` grants, flags and returns; `Leave()` only returns. *"Going back will be written in the 3D world."* |
> | **All 14 the same** | boss pockets enter identically; `PocketKind` branches in exactly one method |
> | **Max Resolve can rise** | it could not before — `ResolveManager.Max` was `=> GameConfig.ResolveMax`, a **const**, so §6 rule 6's eight grants had no mechanism at all. Now a field with `RaiseMax`, persisted as `SaveGame.ResolveMax`. **A rule about a number nothing can change reads as satisfied**, which is the class of omission worth naming |
> | **One shared predicate** | `PocketManager.InsideTheDream` — *a pocket is live **or** a heist is*. Eight guard sites moved onto it once, so 6b deletes a clause instead of re-editing eight files |
>
> **The 758 green after the guard migration is the interesting part**: the ~15 existing probes drive a live
> *heist*, so they validated the migration through the predicate's second clause without a line of new test.
>
> **`Enter` refuses inside a fight, and that is correctness rather than taste.** A pocket is not a room, so
> entering does **not** navigate — so `EncounterManager.OnRoomEntered`, the backstop that stops a fight
> outliving its screen, never fires. Without the guard you could tour a 3D scene and come back still
> committed to a creature: unit 3a's defect in a new costume.
>
> **Leaving early writes nothing**, which is the softlock rule and has its own three checks: nothing in the
> Dream can kill you, so an ejection is a pocket's only failure, and if it broke the entrance a bad night
> would permanently delete a craft family and a point of max Resolve.
>
> **`F4` enters or leaves the reference pocket** (`data/pockets/dream_probe.tres`, pointing at the probe
> scene). It deliberately **cannot `Complete`**: completing breaks the entrance for that save, so a key that
> completed would be *destructive* scaffolding. It retires when the first real entrance is authored.
>
> > **Nine break-tests, and two of them found defects in the gates rather than the code.** A guard-removal
> > predicted three reds and produced one — because two checks were standing on `verify_wilds`, which holds no
> > bench and no station, so *"the bench is shut inside a pocket"* was true whether the guard existed or not.
> > Both now stand on ground where the guard is the only thing that could refuse, each with a control on the
> > same ground. And a check reading *"the fight you were in is still running"* was **deleted**: copied from
> > the camp verb where it is load-bearing (travel navigates, so a broken guard ends the fight), it measures
> > nothing here because entering a pocket does not navigate. **A check is not portable just because its words
> > are true in both places** — what made it a probe was the navigation, not the phrasing.
> >
> **✅ Unit 6b is BUILT, 2026-08-04 — 843 → 810 checks, exit 0.** *The heist is gone.* What went:
> `DreamRunManager`, `DreamHeistData`, `data/heists/`, `GameConfig.HeistDataDir`, `HotspotData.HeistId`,
> `HotspotKind.TakeAnchor` and its palette entry, `HeistOutcome`, the `HeistStarted`/`HeistEnded` pair,
> `PursuerProximityChanged`, `Hud`'s dread label, the heist drain lookup in `ResolveManager`, and
> `SaveService`'s stranding guard. **`PocketManager.InsideTheDream` lost one clause and the seven guards
> reading it were not edited at all**, which is the entire payoff of introducing it in 6a.
>
> **I called this "pure deletion" and it was not, which is the finding worth keeping.** A grep before
> touching anything showed that **five surviving invariants had no probe outside a heist-driven block**: the
> realm lock, the HUD's dim trio, the pool-order restore, Vitality-never-moves-in-the-Dream, and — the
> serious one — **all four assertions of `RoomView.DisplayedRoomId` / `Hud.DisplayedRoomName`**, which are
> the whole regression cover for the ejection post-mortem. Deleting first would have removed it and the suite
> would have gone green over the loss. `GateHeistMechanics`'s own doc comment had predicted exactly this;
> I had read that section and still mis-described the unit.
>
> So **13 pocket-side probes went in first (830 → 843)** and were break-tested before anything was removed —
> removing the stale-payload guard reddens the new checks alongside the old ones, and the realm lock and the
> HUD dim each redden their own. Only then the deletion, which cost **33 checks, every one about the heist**.
>
> | retired | how |
> |---|---|
> | 13 checks | **moved** to the `pockets` section, standing inside a real pocket |
> | 4 checks | **died with the mechanism** — the heist registry, the maze start room, the pursuer ending a run, the anchor |
> | 2 checks | **deleted on purpose** — *"no drain outside a run"* and *"the bar holds still"*. Nothing in the game can drain Resolve now, so both became universals over an empty set. The rule survives in prose on `ResolveManager.CurrentRatePerSecond`; the checks return with the boss aura |
>
> **One replacement is better than what it replaced.** The old `DisplayedRoomId` probe reproduced *one
> situation* that triggered the bug — walking into the pursuer nested a navigation inside a `RoomEntered`
> handler. A pocket cannot nest that way, so instead of losing the cover the stale payload is now raised by
> hand, asserting the guard directly. It cannot stop working because the Dream changed shape again.
>
> **Two things were deleted as *unnecessary* rather than migrated, and the distinction matters.**
> `SaveService`'s stranding guard existed because a Dream room was an ordinary `RoomData` with no way out; a
> pocket never enters `NavigationManager`, so `CurrentRoomId` cannot name one and there is nothing left to
> detect. The class of bug was removed by the model changing, not fixed. Same for the `data/heists/`
> emptiness check — there is no directory left to count.
>
> **`HotspotKind.TakeAnchor`'s number is deliberately not reused.** `Workbench` stays at 7 with a hole at 6:
> every authored `.tres` stores `Kind` as an integer, so renumbering would silently repoint 59 rooms' worth
> of hotspots at a different verb. A hole in an enum is free; a shifted one is a content migration.
>
> > **And the deletion found one gate standing on ground it did not own.** `GateTravel` needed a real pocket
> > to be refused inside of, and registering a synthetic one turned the pocket **count** check red — *2 of 1* —
> > plus a dangling scene path belonging to a fixture that never wanted a scene. Fixed by excluding `verify_`
> > pockets by key prefix, exactly as `GateAuthoredContent` excludes `verify_` rooms. Worth naming because a
> > *count of authored things* is the one check shape where a synthetic fixture is indistinguishable from
> > content unless it is filtered out by name.

> > **And one self-inflicted scare worth recording as a rule already written down.** A break-test was reverted
> > with `git checkout -- <file>`, which restored to **HEAD** and wiped a whole unit's uncommitted work in
> > that file. Recovered in full from the `/tmp` snapshot, and the check count is what proved it — same 830
> > either side. `verify-slice-blind-spots` already says never to use git to revert a break-test in this tree;
> > the reason it says so is this.

**Unit 1.5 — the placeholder readability layer.** *Small, and it slots between the great swap and
generation.* Asked for 2026-08-02: *"could we just colour code the different game objects with colours so
it is straightforward when I swap them out later? As well as text over all of the images with its name?"*
**Yes, and drawn rather than baked** — see the callout below. Five pieces:

- **A room card** — `ch2_inn_yard` · The Inn Yard · the exit list. Over 59 grey screens, *which one am I
  standing in* is a worse problem than *what is this hotspot*
- **Hotspot fill tinted by `HotspotKind`**, replacing today's single blue outline, with the **`Label`
  drawn in place** and the **destination room id** on a `Navigate` — palette below
- **A locked hotspot renders dimmed rather than not at all.** Today a hotspot whose `RequiredFlag` is
  unmet simply is not there; during playtest you need to see *this exists and it is shut*, which is how a
  gate gets tested without the inspector, and it is the ledger's **Blocked** column made visible
- **The entity's name over its silhouette**, from `EntityData`
- **A totality gate: every `HotspotKind` has a colour.** One of the few guards that genuinely goes red on
  its own — add a kind, forget the colour, the suite fails. **Unit 4 adds `Station`**, so this fires for
  real within two units

#### The palette, agreed 2026-08-02

**What decided it was the backdrop, not taste.** `PlaceholderBackdrop` draws two palettes — uncleansed
dark cold slate (`#2b3138` / `#3a4149` / `#22262b`) and cleansed sky-blue over sage (`#7fa8c9` /
`#6f8f5e` / `#4a5c3b`). Both are desaturated, so saturated colour pops on either; the two hues needing
care are **mid-value green**, which vanishes on purified ground, and **mid-value blue**, which vanishes
on purified sky. That is why the harvest colour is a pushed lime rather than the obvious herb green.

| Kind | | Hex | Why this one |
|---|---|---|---|
| `Navigate` | cyan | `#38d6ff` | Continuity with today's outline, and exits are what you scan a new screen for |
| `Harvest` | lime | `#a3e635` | "Take this." High-value on purpose, to separate from purified ground |
| `Entity` | crimson | `#ff4d4d` | The fight verb. Nothing in either backdrop palette is red |
| `Interact` | amber | `#ffa83c` | Something happens, but it is not taking or moving |
| `Examine` | slate | `#c8ccd2` | Deliberately the quietest — the verb where nothing happens should not compete |
| `Workbench` | gold | `#ffd24a` | The camp; the one hotspot you are glad to see |
| `CrossToDream` | violet | `#b07cff` | Otherworldly, and absent from both palettes. **Provisional** — unit 6 retires the heist system that owns this kind |
| `TakeAnchor` | white | `#ffffff` | The objective. **Provisional**, same reason |

> **Hover brightens the kind's own colour rather than washing it with a fixed cream** *(Jeremy's pick,
> of two options)*. This is the better behaviour on its own terms — it reads as *"hover tints it"* more
> literally, which is the shipping affordance's own description — and it **dissolves a collision instead
> of dodging it**: `Workbench` gold `#ffd24a` sits very close to today's hover cream `#fff3d6`, and with
> no fixed hover hue there is nothing for it to collide with. The alternative was moving the bench to a
> teal, which would have cost the one hotspot that should feel warm. On painted screens there is no kind
> colour, so hover falls back to today's warm highlight — the shipped/placeholder split already agreed
> above.
>
> **This also settles the third-draw-state problem** noted in the bug callout below. Idle-cue, hovered
> and hidden are no longer three colours competing: idle and hovered are **one hue at two intensities**,
> and only hover survives into the painted game.
>
> **The label carries the meaning; the colour is redundant coding.** Because `Label` is drawn in place,
> nobody has to learn that lime means harvest — it is readable. That is also what makes this palette
> colour-blind-safe without contorting it: the riskiest pair under deuteranopia is crimson/lime, and
> they are separated by value (~0.47 against ~0.79) as well as by the text on top. Two pairs do merge in
> greyscale — cyan/amber and lime/slate — but each is far apart in hue and neither is a red/green
> confusion, so they only collapse in a screenshot someone desaturated. **If a kind ever loses its
> label, it loses its safety net** — that is the constraint to keep, not the specific hexes.

> **It draws when there is no art, not when a flag is set.** `RoomView` already knows
> `_room.WakingBackground is null`; when it is, every hotspot on that screen draws itself unprompted.
> **Paint the screen and the cues vanish for it** — nothing to remember, nothing to clean up, no way to
> ship them by accident. `ShowHotspotOutlines` stays as the manual override for screens that *do* have
> art, because a painting can hide a region as well as a grey box can.
>
> **Why not generated placeholder images**, which was the original shape of the ask and which Jeremy
> withdrew — *"I don't think we need to generate placeholder images"*: 59 PNGs plus 59 `.import` files in
> git; every `.tres` pointing at a placeholder texture that has to be nulled by hand when real art lands,
> 59 times, where forgetting one ships a labelled grey box; and **a baked label goes stale silently** when
> a room is renamed in the inspector, which is worse than no label. A drawn label reads the resource, so
> it cannot lie.
>
> **It also fixes a bug found in play.** Jeremy: *"you would see the outline of the square once you
> hovered but it didn't show until you did hover."* Cause: `RoomView._showOutlines` defaults to `false`,
> so `Hotspot._Draw` returns early unless hovered — an unpainted screen's hotspots are invisible until the
> cursor finds them by luck. The cue is the fix. **Note the third draw state this creates** — idle cue,
> hovered, hidden — so the hover colour must clearly beat the cue colour or the existing hover feedback is
> lost inside it. *(Settled by the palette above: hover is the same hue brightened, so the two states
> cannot compete.)*
>
> **CLOSED 2026-08-02 — the shipping affordance is hover-tint, so this layer stays debug-only.** The
> open question was whether the final game shows interactables without hovering, because if it did, the
> placeholder cue and the real affordance would be one feature built twice. Jeremy: *"it will be an
> image on the background and when you hover it tints it a color."* **No shimmer, no hold-to-highlight,
> no always-on indicator** — an interactable is painted into the backdrop, and hovering tints its
> region. The default assumption was right, and the two are genuinely different features:
>
> | | shipping | placeholder |
> |---|---|---|
> | **when** | always | only while `WakingBackground is null` |
> | **idle** | nothing — the art is the affordance | outline + `Label` + kind colour |
> | **hovered** | region tints | region tints, more strongly than the cue |
>
> **Most of the shipping half already exists.** `Hotspot._Draw` tints on hover today; what it lacks is
> only the unpainted case. So unit 1.5 is genuinely additive, and the "third draw state" warning above
> resolves cleanly: idle-cue and hidden are both placeholder states, and **hover is the one state that
> outlives the placeholder**, which is why it has to read as the strongest of the three.
>
> **One sub-question left, and it is small.** *"Tints it a color"* is singular. This layer tints by
> `HotspotKind` — a door one colour, a herb another — and that is unambiguously useful on a grey box.
> Whether the **shipped** tint also varies by verb, or is one neutral highlight, is a look-and-feel call
> best made against painted art. **Building it as: kind colours are placeholder-only, the shipped hover
> tint stays the single warm highlight that exists today.** Promoting kind colours to shipping later is
> a one-line change; demoting them is not, so the cheap direction is the default.

**Ordering, confirmed 2026-08-02.** *"unit 1 → the great swap → this → then generate — yes exactly."* The
swap deletes `BackdropStyle.Dream`, `RealmVisibility` and the hotspot realm filter, all of which this
layer touches; building it first means writing three files twice. **Same double-rewrite argument already
accepted for spectral vision.**

> **Unblocked 2026-08-02.** All three are gone (§7 decision 8, executed). `BackdropStyle` is now
> `{Wilderness, Interior}` and `PlaceholderBackdrop` has two cases, not three — so this layer is
> writing each file exactly once, as intended. **This is next.**

> **BUILT 2026-08-02. All five pieces, 338 → 359 checks, exit 0.** New: `PlaceholderCues` (palette and
> the label-with-backing helper), `RoomCard`. Reworked: `Hotspot._Draw` into three named states,
> `RoomView.AddAuthoredHotspots`. The cue condition is `_room.WakingBackground is null`, exactly as
> specced, exposed as `RoomView.CuesVisible`.
>
> **One thing the spec left implicit, decided in the build: *locked* and *spent* are different, and only
> locked gets drawn.** A hotspot held shut by a `RequiredFlag` is waiting to open, so it draws dimmed with
> `⊘ {flag}` under its label — that is the Blocked column. A harvest node whose `HarvestedFlag` is set is
> *finished*, and drawing it forever would fill every cleansed screen with ghosts of things already
> picked. Both halves are gated.
>
> **The regression this unit could have caused, and the guard on it.** `RoomView` now instantiates
> hotspots that `InteractionService.IsAvailable` refuses — if one ever became clickable, **every flag gate
> in the game would be bypassable by walking into the room**. So *drawn* and *inert* are asserted
> separately (`Locked`, `MouseFilter == Ignore`, no `Activated` subscription), with availability
> re-asserted underneath them. Break-tested: forcing `MouseFilter = Stop` turns exactly that check red.
>
> The totality gate was break-tested too, and it does what it was built to do — dropping `Workbench` and
> colliding `Examine` with `Interact` produced two failures, each naming the offender. So did the central
> promise: pinning `_unpainted = true` so art never switches the cues off failed three checks, not one.

**Then unit 7 — the journal's knowledge model**, needed before chapter 3 content because the engine is
the only recipe requiring both tracks. **Audio last**, as recorded.

> **The journal *is* the crafting screen — decision 9, 2026-08-02.** Jeremy: *"The journal should just be
> the crafting station and the herblore station where you create them."* It is not a lore reader with a
> crafting tab bolted on; it is **one panel with two sides — Craft and Physick — and each side is where
> you build that side's things.** Three consequences worth having in writing:
>
> - **It deletes a screen rather than adding one.** "Crafting UI" and "journal UI" were two entries in the
>   editor pass; they are now one, and `KnowledgeTrack { Craft, Physick }` — the one enum that memory
>   already asked for — is what draws the divider instead of being metadata nobody looks at.
> - **Knowing a recipe and being able to make it are the same view**, so the *"Deadfall — needs Scrap
>   Iron"* reading that `items.md` §3 leans on happens for free, in the place you would look anyway.
> - **A field station is this panel with one side open.** `placement.md` §2 wants single-track crafting
>   away from camp; that is now *"show the Craft half"*, not a second panel.

> **What Stream A deliberately does not include: the panels.** The crafting panel, the combat UI and the
> journal UI stay in the one editor pass, per the 2026-07-30 decision. Each unit above still has to
> **publish its node contract** when it lands, or that pass stops being mechanical.

---

## 5. Stream B — the screen ledger, and the three passes

### The ledger is the missing artifact

`placement.md` says chapter 2 has 4 blights. It does not say which screen. **The ledger is one row per
screen** and it is the thing the `.tres` files get generated or typed from:

| id | chapter | name | exits | Held by | Blocked by | Fouled by | door | nodes | NPCs |
|---|---|---|---|---|---|---|---|---|---|
| `ch1_track_02` | 1 | The Deer Track | `ch1_track_01`, `ch1_hollow` | Logger #1 (path) | — | — | — | Bitterroot ×2 | freed: the woodcutter |
| `ch1_hollow` | 1 | The Burnt Hollow | `ch1_track_02` | Hunter #1 (path) | — | — | **Mechanical traps** *(after the fight)* | Pitch | freed: the snarer |
| `ch1_wellyard` | 1 | The Well Yard | … | Messenger #1 *(optional)* | — | **well cover** *(jigsaw: order)* | — | Heartwood | — |

Ten columns, ~58 rows, and **filling it is the act of drawing the map.** Every one of `placement.md`'s
seventeen rules is a check you can run against this table before a single file is written — which is the
cheapest possible place to catch the class of error §8 caught by hand.

**All three are drawn as of 2026-08-02.**

| | rows | bill said | state |
|---|---|---|---|
| **[`ledger-ch1.md`](ledger-ch1.md)** | **17** | ~16 | drafted 2026-08-01, **map confirmed 2026-08-02** |
| **[`ledger-ch2.md`](ledger-ch2.md)** | **19** | ~19 | drafted and **confirmed 2026-08-02** |
| **[`ledger-ch3.md`](ledger-ch3.md)** | **23** | ~23 | drafted 2026-08-02 |
| | **59** | **~58** | one over, in the row the bill marks as its only guess |

Everything in the Held / Blocked / Fouled / door columns is forced by the bill; the graph is Claude's to
draw and Jeremy's to overrule *(see [[division-of-labour]] — an earlier version of this paragraph said
chapters 2 and 3 were "better drawn than derived" and left them to him, which was the wrong call)*.

> **All three chapters are drawn before any of them is generated.** Jeremy, 2026-08-02: *"this is 100%
> because they affect one another."* Borne out immediately — laying out chapter 2 is what turned chapter
> 1's vague *"rule 11 does not quite fit"* into a countable table, and it caught a chapter-1 trap that
> could not be built. **A ledger asks "what can the player make on this screen, today, with what they are
> standing next to", and that question does not stay inside one chapter.**
>
> **Drawing all three then found the rule itself was backwards.** With one chapter you soften rule 11;
> with two you propose an exception named after a creature; **with three you can see that inverting it —
> *every blight sits behind an optional fight or that chapter's tool* — is exact 12 out of 12 and needs
> no exception.** `placement.md` rules 11 and 17, settled 2026-08-02. That correction was not available
> from any one ledger, which is the argument for this ordering in its strongest form.

### The naming contract

Flags today are `waystation.bench_found`, `zone.waystation.purified`, `harvested.{id}`. Extended, and
**fixed before 58 screens invent 58 conventions**:

| | |
|---|---|
| Rooms | `ch<n>_<place>` — `ch2_smithy`, `ch3_cart_run` |
| Fights | `fight.<entity>.<n>` — set when the encounter settles |
| Freed | `freed.<name>` — what an NPC hotspot gates on |
| Blights | ~~`mend.<room>`~~ **`blight.<hotspot>.mended`** — one flag, one mend, never repeatable. Keyed to the hotspot rather than the room because the noun is the blight, and a screen cleansing is a *separate* flag (`screen.<room>.cleansed`) derived from this one plus what is standing there. Published in unit 1, set from unit 2 |
| Camps | `camp.<room>.found` — a camp stood in, and so a destination the return-to-camp verb offers. Added by unit 5, and **it is why that unit changed no save field**: flags already export, import and rebroadcast. Keyed to the room because a camp *is* a room with a bench, which is the one fact `CampManager` derives everything else from |
| Pockets | ~~`pocket.<id>.taken`~~ **`pocket.<id>.cleared`** — `PocketManager.Complete` writes it, and nothing else does. **The code shipped `.cleared` in unit 1 and this row said `.taken`**; the code wins (`Flags.PocketCleared`), and the row is corrected rather than the helper renamed. It is the flag the entrance hides itself on, so it is also what *breaks the artifact* — see unit 6a |
| Tools / recipes | `knows.<recipe>` — written by `RecipeData.SetsFlag` |
| Knots | `knot.lit` / `knot.high` / `knot.far` — the beat reads the **count** |

### The three passes over every screen

**This amends "systems first, then all content in one pass" and the amendment should be accepted or
rejected explicitly.** The doctrine exists to stop content being authored against a half-settled
vocabulary and then re-authored. Pass 1 touches **only** `RoomData` identity, `Navigate`, `Examine` and
`Harvest` — fields that have been stable across four milestones and are swept by `GateAuthoredContent`.
They cannot be invalidated by units 1–6, so the reason for the doctrine does not apply to them.

| | Pass | Touches | Blocked by |
|---|---|---|---|
| **1** | **Skeleton** — rooms, ids, the navigation graph, placeholder style, examine stubs, harvest nodes | fields that exist today | **nothing.** Starts the day the ledger's chapter block is done |
| **2** | **Content** — fights, blights, pocket doors, NPC hotspots, stations, tool gates | the units in §4 | each part lands as its unit lands; every one is a pass over rooms that already exist |
| **3** | **Finish** — painted backgrounds, jigsaw boards, final prose, audio | art and writing | never blocks play; a `Texture2D` swaps into a field |

> **PASS 1 BUILT 2026-08-02 — all three chapters at once, 59 rooms, 359 → 367 checks, exit 0.**
>
> Generated from the three ledgers by a one-shot script kept in `/tmp`, not in the repo (§7 decision 1:
> *run once per chapter block, not kept as a live pipeline*). It wrote only files that did not exist.
> Each room got its `Id` / `DisplayName` / `ChapterId` / `PlaceholderStyle`, one `Navigate` per ledger
> exit labelled with the destination's display name, one `Examine` stub, and one `Harvest` node per
> material in the nodes column. Hotspot regions are laid out to a fixed template — **exits read
> spatially: left, right, centre-up is away from you, centre-down is back toward you** — with harvest
> nodes along the bottom and the examine in the middle, so unit 1.5 draws a screen you can actually read
> and walk before a single rect is placed by eye. The fourth slot was a bottom-right corner in the first
> two batches and moved to centre-down the same day, on Jeremy's reading that the exits *are* the screen's
> node layout; only three rooms have four exits and none of their harvest nodes reach that far right.
>
> **The ledger did not say everything.** Three things were derived rather than read, and each is one
> field to overrule: (a) `PlaceholderStyle` per room, judged from the name; (b) ch2 row 18's "materials
> cache in bulk", given no counts in the ledger, emitted as Cured Hide ×3 + Scrap Iron ×3 — node counts
> are supply lever 1 and provisional by design; (c) five materials have **no `ItemData` file yet** and
> are referenced by string id (`mat_heartwood`, `mat_pitch`, `mat_scrap_iron`, `mat_cured_hide`,
> `mat_cairnstone`). `items.md` §1 defers those files on purpose and `AddItem` accepts an uncatalogued id
> without complaint, so the nodes work now and get names later.
>
> **`RefillsAtMilestone` is set on every common node and on no Cairnstone**, per rule 13 — the field's
> own doc is the contract, not the live/dormant split, which is about demand rather than re-arming.
>
> **The camp bench is not here.** Pass 1's column stops at harvest nodes and `Workbench` is a station,
> which is pass 2 — but `GateAuthoredContent` requires a reachable authored camp, and today the only one
> is `waystation_shed`. **So ch1's cottage bench must land with, or before, the retirement**, not after.
> *Placed the same day* as `ch1_cottage_bench`, out of pass order and on purpose, because the boot moved
> with it and the two are the same decision.
>
> **What the pass actually cost was two silent defects, and one of them was invisible to the suite.**
> The first generated batch had a malformed typed-array line and *every one of the 59 failed to parse* —
> `GD.Load` returns null, `LoadRoomRegistry` warns to a console nobody reads headlessly, the rooms are
> simply absent, and **every sweep in the verifier passed, because they all iterate the registry**. The
> suite went green on a run where none of the content existed. That is a new entry in the blind-spot
> list: *a gate that reads the registry cannot see a room that never reached it.* The fix is the first
> check in the new `graph` section — count the `.tres` files on disk and compare. The second defect was
> `HotspotKind` misread (`Examine = 1`, `Harvest = 3`, not 4 and 1), which made every stub an `Interact`
> and every node an `Examine`; the new *"every Examine hotspot says something"* check caught it for real.
>
> **The gate is a new section, `graph  the world as doors, not as rooms`** — seven checks that treat the
> world as a graph rather than a bag of rooms: files-on-disk versus rooms-registered, every door names a
> room that exists, no two harvest nodes share an `Id` (and so a `harvested.` flag), the spine is
> authored, every door can be walked back through, the spine is one connected component across all three
> chapters, and no painted screen still reads `[unwritten]`. Break-tested six ways — unparseable file,
> blank `Id`, misspelled door target, duplicate harvest id, one-way door, chapter 3 cut loose — each red
> then restored green. The seventh is dormant by design, exactly like `GateCombatShapes`: there is no art
> in the repo, so it cannot fail today, and the failure it exists to catch is a finished screen still
> emitting `[unwritten]` when clicked, which nobody would find except by clicking every hotspot in the game.

> **THE BOOT MOVED THE SAME DAY — 367 → 369, exit 0.** `GameConfig.StartingRoomId` now reads
> `ch1_cottage`, so a new game begins on the real map and all 59 screens are walkable in play. The
> prototype is *unreferenced*, not deleted: the waystation, the shed, the six `dream_way_*` rooms and the
> rat still load and still hold their gates, and retiring them stays a separate operation.
>
> **This was offered as "cheap and independent of the retirement" and it was not.** Thirty-two checks
> went red instantly, then the run hung on a null. M1, M2 and M3 never navigated to the yard — they acted
> on wherever the boot had left them and *silently meant* the yard, so the boot was an unwritten fixture
> for a third of the suite. The whole repair is one line, `GoToRoom("waystation_yard")` at the head of
> M1, and it is the first slice of the re-anchoring §5 already schedules. Keep the general form when
> doing the rest: **a gate that names its fixture can have the fixture swapped under it; a gate that
> inherits one cannot**, and the day the default changes it reads as unrelated breakage.
>
> Three checks came out of it, two in `harness` and one replacing M1's opener. `harness` now asserts
> that the game begins in the room `GameConfig` names — `Main.StartGame` gives up on a false return, so
> this catches a navigation that lands elsewhere — and that **that room is a camp**, which is load-bearing
> rather than tidy: `CampManager` falls back to the starting room when you collapse before finding a
> bench, so a boot room without one strands an early death somewhere they can neither craft nor bind a
> return point. M1's `"starts in the Waystation yard"` became `"the prototype yard is still walkable"`,
> which goes red on the day the retirement lands — which is precisely when someone should read M1.
>
> **And the harness stopped hanging.** Two fixture breaks that afternoon produced no output at all
> instead of a failure, because Godot's C# layer logs an exception and keeps the loop alive. `Run` now
> wraps the gate sequence in a `try`/`catch` that records a `[crash]` failure and prints the summary, so
> the run exits 1 in seconds with a short count showing how far it got; and `Main.StartGame` calls
> `Quit(1)` rather than returning when the starting room will not load under `--verify-slice`, since that
> path never reaches the verifier. See `handoff.md` §3 — it is the sixth blind-spot class, and the one
> that hides the other five.

**Art is a stream that never blocks anything.** Every room draws a procedural backdrop until
`WakingBackground` is set, so the whole game is playable grey and paintable in any order. The one art
decision that *does* gate work is **billboards versus models for Dream creatures**, and it gates the
commission, not the authoring — unit 6 can greybox its pursuer.

---

## 6. The bill checker — turn the audit into a program

`GateAuthoredContent` already sweeps authored content for silent mistakes, and its rule is the right one:
**every check is a relationship between two authored values, never a value.** The placement bill is
almost entirely relationships. So it can be checked.

✅ **BUILT 2026-08-04.** `src/Tools/ContentReport.cs`, run as
`godot --headless --path . -- --verify-content`. **Exits 0 unconditionally** — the separation from
`--verify-slice` is the whole design, and the first run produced the numbers in the callout below.

Proposed as **`--verify-content`, a separate command that prints a table and does not fail**, until a
chapter is declared sealed. That is the resolution of the standing tension: *a gate asserting authored
content is a to-do list while the content is missing and an invariant once it exists.* A report is
honest during authoring; the same checks become gates the moment a chapter is done.

Machine-checkable, from `placement.md` §5:

| Rule | Check |
|---|---|
| 2, 12 | one entity placement per screen; never the same creature on adjacent screens |
| 4 | no screen has zero interactables *(warn, not fail)* |
| 5 | no pocket door on a screen that carries a blight; every door gated on its screen's fight flag |
| 6 | exactly 8 max-Resolve grants, one per knowledge pocket, none on boss or knot pockets |
| 9 | commons available before each recipe's first need — the abundance rule as arithmetic |
| 10 | every screen's Held / Blocked / Fouled set is resolvable, so every screen can cleanse |
| 11 | every optional sighting is on a path to a blight |
| **13** | **scarce-material supply ≥ demand inside every gate window** — this is the check that would have caught the Cairnstone softlock the day it was authored |
| 14 | exactly 15 mend instances, none with a skip route |
| 15 | nothing but `Salve` is consumable |
| — | the counts: 12 blights, 12 Graveflower, 6 cairn spots, 24 freed, 33 fights, 14 pockets |

**Not checkable, and therefore documented rather than gated:** rule 1 (is this creature on the critical
path — a judgement about level design), rule 7 (are ch1 pockets flat — 3D geometry), rule 8 (is the
finale winnable on path materials). These belong with the unfalsifiable guards **§3** of the handoff already
says to write down instead of gating — §5 there is the ejection post-mortem, and this line cited it for
months.

> **What the first run said, 2026-08-04 — 11 rows still to author, 0 disagreeing with the bill.** The
> honest inventory, and it is a shorter list than it felt like:
>
> | | on disk | bill |
> |---|---|---|
> | screens · camps · blights per chapter · freed NPCs · her notes · Cairnstone spots · Graveflower · carcass harvests | **all matching** | — |
> | creature files | 11 | 12 |
> | fights placed | 30 | 33 *(the three bosses)* |
> | pocket files · entrances · knot · boss | 1 · 0 · 0 · 0 | 14 · 14 · 3 · 3 |
> | max-Resolve grants | 0 | 8 |
> | jigsaws | 12 | 15 *(the three major repairs)* |
> | items · recipes | 27 · 17 | ~39 · ~18 |
>
> **Everything outstanding traces to two things: the three bosses and the fourteen pockets.** The major
> repairs need a reclaimed memory, the max-Resolve grants need knowledge pockets, and the missing items are
> mostly the lore pages those pockets hand out — so the list is not eleven independent jobs, it is Jeremy's
> 3D pass plus the boss content that hangs off it.
>
> **The tool corrected itself three times on its first two runs, and each correction is the same mistake:
> a status that made outstanding work look like something else.**
>
> - It read *"authored but short of target"* as a **contradiction**, so 30 fights of 33 was flagged as a
>   fault. Under-target is a to-do; only **over**-target means the bill and the content genuinely disagree —
>   and for the scarce nouns, over-target is a softlock.
> - It reported **rule 6 violated by `dream_probe`**, which is a `Knowledge` pocket granting zero on purpose
>   so the placeholder spends none of the eight-point budget. The rule has two halves that fail differently:
>   a knot or boss pocket granting a point is a violation, a knowledge pocket granting none is *unfinished*.
> - `~` for an approximate target swallowed **twelve unwritten items** behind a friendly glyph. It now means
>   *within two*, which is what `placement.md`'s ± was ever claiming.
>
> All three were found by reading the output rather than by anything failing, which is the only way a tool
> that cannot fail can be checked at all. **That is the standing cost of a report over a gate**, and it is
> worth stating beside §6's argument for having one: a gate tells you when it is wrong, and this never will.

> **One safety item before Stream B starts — done 2026-08-01.** `--seed-content` **overwrote everything
> under `data/`**. With five rooms authored that is a bootstrap tool; with 58 it is a loaded gun in the
> same directory as a month of work.
>
> **`ContentSeeder` and the flag are deleted**, rather than made to refuse. Jeremy: *"should we remove
> it completely?"* — and completely is right, for a reason beyond the danger: **its payload was
> superseded content.** It seeded the giant rat, the six 2D `dream_way_*` rooms and the heist, all three
> of which the design has since deleted, so "regenerate the starting content" had stopped being a thing
> anyone would want to do. What was worth keeping is about fifteen lines: `ResourceSaver.Save` followed
> by `TakeOverPath`, so a room references its entity instead of embedding a private copy of it. **That
> idiom comes back in the ledger generator, write-if-absent** — the generator may create a file and may
> never replace one. Git holds the original at `bb665ad:src/Tools/ContentSeeder.cs`.
>
> The standing rule *"Claude never runs `--seed-content`"* went with it. It was the weaker of the two
> fixes available — a discipline where a deletion would do — and the thing it protected is now safe
> against a mistake rather than only against a habit.

---

## 7. Decisions this plan needs

Eight, each of the last two turned up by settling the one before it. **All eight were settled the day this was written.** Decisions 9, 10 and 11 were added 2026-08-04 — **10 and 11 came out of the re-cut in §8**, and both closed the same way: the thing that looked like a missing mechanism turned out to be already drawn in the ledgers or already true in the code.

**1. May Claude generate skeleton `.tres` files from the ledger? — YES, ratified 2026-08-01.** The
standing division was *Jeremy authors in the editor, Claude writes managers and gates*. Pass 1 is ~58
files of ids, exits and flags — mechanical, error-prone by hand, and exactly what the ledger makes
derivable. Jeremy: *"If the plan showed no issues I think we are ready to."*

> **What this does and does not change.** It moves **derivable** authoring to the generator: ids, the
> exit graph, flag names, which hotspot kinds a screen carries. It does **not** move authoring that is
> judgement — hotspot regions over painted art, examine prose, which room is beautiful. Those stay in
> the editor, which is where they can be looked at.
>
> **Three guards, and they are what makes the answer safe rather than merely faster.** The generator
> **writes only files that do not exist** — the `--seed-content` lesson, applied before the first file
> rather than after the first loss. The **ledger is reviewed before generation, not after**: a wrong
> row becomes a wrong file, so the cheap place to catch it is the table. And the generator is **run
> once per chapter block**, not kept as a live pipeline — regenerating over hand-edited rooms is the
> exact failure the deleted tool existed to demonstrate.

**2. Is the three-pass amendment in §5 accepted? — YES, ratified 2026-08-01.** Jeremy: *"Breaking it up
seems smarter."* So the skeleton pass may run against the fields that already exist, without waiting for
the units in §4. The doctrine it amends — *systems first, then all content in one pass* — keeps its force
everywhere its reason applies: **pass 2 still waits.** Nothing that touches a field a unit is going to
move gets authored before that unit lands.

**3. Is a camp still a screen? — YES, ratified 2026-08-01.** Jeremy: *"it should be a place you are
teleported to. The cabin, Inn, Campsite. It will add more of a cost of having to retravel then a pure
overlay inventory."* So the menu is a shortcut **in**, and you **walk out**. `combat.md` §17 carries the
amendment and the three knock-ons that were checked. Two consequences here: **the 3 camps are ordinary
rooms in the ledger** with ordinary exits *(ch1's is his cottage, which is plausibly also the starting
screen)*, and **unit 5 shrank** from a rework to one added verb.

**4. Where does the chapter live? — THE CHAPTER IS THE ZONE, ratified 2026-08-01.** ✅ **DONE
2026-08-02, and it turned out to be smaller than this.** Jeremy: *"A chapter is the level/map."* That is
`ZoneData`'s definition already — *a region of the map* — and every `RoomData` carries a `ZoneId` today,
so the answer is a **rename, not a new field**: `ZoneData` → `ChapterData`, three files,
`RoomData.ZoneId` → `ChapterId`.

> **What actually shipped: the field, not the class.** Doing the rename exposed that `ZoneData` had no
> reader and never had one, so the class was renamed and then deleted the same day (unit 1's callout in
> §4). **A chapter lives in a string on `RoomData`, and nowhere else.** The decision above is still the
> right one — the chapter *is* the zone — it just costs one field instead of a resource.

> **Do the rename in unit 1, before pass 1.** It touches 8 authored rooms today and ~58 after the
> skeleton pass. Same shape as the `--seed-content` argument: the cheap moment is now.
>
> **What the chapter keeps and what it sheds.** It keeps identity — id, display name, and the things
> that are genuinely per-chapter: the camp room, the chapter's tool, its boss, its material milestone.
> It sheds the **lieutenant / `ZonePhase` / Form-3 machinery**, which is the superseded design: §22
> cleanses **per screen**, so "kill the lieutenant and the region clears" has no customer left. That
> machinery is live and persisted today (`WorldStateManager` phases, `NavigationManager`'s
> Corrupted→ActiveFront flip, the `zone.waystation.purified` flag, the save round trip) and it currently
> drives the purified backdrop — so **unit 2 deletes it as part of building per-screen cleansing**, not
> unit 1. Rename first, gut second. `EntityPlacement.IsZoneLieutenant` goes with it.
>
> **Done 2026-08-03, and the gutting cost the suite nothing** — every invariant the machinery held moved
> onto `screen.<room>.cleansed`, and only `ZoneCorruptionScalar`'s probe was retired outright, because
> nothing had read it since Resolve drain became boss-only. `SaveGame.ZonePhases` went too, which is the
> **one field the save format has ever lost**; old saves survive because unknown JSON members are ignored
> *and* because arrival re-derives the flag.
>
> **One thing stays separate on purpose: which chapter a *room* is in, and how far the *player* has
> got.** The first is static data on the room; the second is world flags. Storing progress on the
> chapter resource would be the `IsSanctuary` mistake again — two facts that must agree, stored twice.

**5. Is the Beast three `EntityData` files or one? — ONE, ratified 2026-08-01.** Jeremy: *"Beast is one
file if they are all the same. I'm assuming we can change the images in the tres — individual object of
the enemies."*

> **The assumption is not true yet, and making it true is one field.** Textures live on `EntityData`
> (the archetype); `EntityPlacement` (the placed object) carries only id, position and scale. So unit 3
> adds an **optional texture override on the placement** — the Beast becomes one `EntityData` holding
> the damage pair, and the crosser / settled one / breach-thing are three placements that look
> different. One damage pair stored once, which is the right shape: the three variants **cannot drift
> apart** because there is nothing to drift.
>
> **The guard, because this field is easy to misuse.** The other eight creatures appear three times each
> and must look *identical* — the corrupted form repeating is the theme (§21), and per-placement art
> would quietly delete it. So the override exists **for the Beast**, and the content sweep can hold it
> to that as a relationship rather than a count: *a placement may only override its art if its entity
> has a blank `PurifiedName`* — never human, which is true of the Beast and the three bosses and nothing
> else.
>
> **Left to unit 3:** what winning against a never-human thing actually sets. The Beast frees nobody and
> leaves a carcass, so `Severed` / `Purified` may not both apply to it.
>
> **✅ Both halves closed 2026-08-03 by unit 3a, and the first half went the other way.** The texture
> override was **never built, and does not need to be**: pass 2 authored the Beast as three separate
> `EntityData` files (`beast_crosser`, `beast_settled`, `beast_breach_thing`) sharing one damage pair by
> hand, so there is nothing left to override and the guard this note worried about guards an empty set.
> The drift risk the ruling was written to remove is real but small — three files, one pair — and it is
> cheaper to leave than to add a field with one legitimate user. *One word to overrule:* collapse the
> three files and add the override after all.
>
> The second half is answered as a **derivation, not a field**: a win routes off `PurifiedName` +
> `EntityState` into one of three flags — blank name → `Flags.Defeated` either way; a name → `Freed` if
> `Severed`, **`Flags.Killed`** if still `Corrupted`. So the never-human thing sets `defeated.*` and the
> promise becomes `freed.* + killed.* == 24` with `defeated.* == 6`. And yes, `Severed` and `Purified` both
> apply to the Beast — every win route ends `Purified`, because the enum has three members and that is the
> terminal one. See `combat.md` §18's ✅ subsection.

**6. `VitalityCost` and `yard_cairn` — THE FIELD IS DELETED, ratified and done 2026-08-01.** Jeremy:
*"vit cost was when the environment damaged you and since nothing does now we don't really need it."*
`VitalityCost`, `HazardText`, `IsHazard`, `ApplyHazard`, the debug panel's hazard list and `GateHazards`
are gone; the suite went **328 → 315**, thirteen checks, all of them about the feature. §22's condition
was met before cutting: the collapse, `SurvivesDeath` and respawn gates drive `vitality.Damage(...)`
directly, so nothing that survives lost coverage. Details and the two things kept are in `combat.md` §22.

> **`yard_cairn` itself is a different question, and the answer is "not yet, and not on its own."**
> It reads as a hazard because it carried the cost, but its `Kind` is `Interact` and it sets
> `waystation.relic_found` — **it is the first step of the prototype loop** (search the cairn → the relic
> appears → the heist starts), so it *is* building something: the M3 gate. The cost is off it; the
> hotspot stays.
>
> **What Jeremy's question actually exposes is bigger and belongs in this plan — see decision 7.**

**7. When does the waystation prototype retire? — ALL OF IT, ratified 2026-08-01; the *when* is the ch1
swap. ✅ EXECUTED 2026-08-03 as unit 3b, 680 → 670 checks, exit 0.** Jeremy: *"I am ready to retire it
all now that we are building lol. The dream world is deprecated as well for the 3D world being worked on
separately."*

> **Three corrections the execution forced, all of them to this decision's own arithmetic.**
>
> **The item budget was stale.** "5 items, 1 recipe, 1 zone" was written before pass 2. The zone died in
> unit 2; bitterroot, tallow and cairnstone were re-authored into 23, 23 and 6 chapter rooms by 4b and
> survive; no recipe was prototype-only. **`herb_gravecap` was the only thing left to delete** — one
> source in the entire game, zero recipes, so an item you could neither get nor spend.
>
> **The fixture was far cheaper than the callout at `:847` assumed.** It warned about *"hand-building
> synthetic Dream fixtures for those 25 heist checks."* `DreamRunManager.IsDreamRoom` derives from
> **walking the Navigate graph out of `StartRoomId`**, not from a flag on a room, so **one synthetic Dream
> room is enough to make a room "inside a heist."** Two chained rooms and a `DreamHeistData` built in code
> covered every surviving check. Read the derivation before costing the fixture.
>
> **"The retirement makes five gate sections simpler" was half right, for a reason it could not have
> known.** Five sweeps carried a comment explaining they were scoped to `ch*` *to exclude the prototype*,
> and the plan was to widen all five. They were not widened. `GateAuthoredContent` snapshots the registry
> **by key prefix** — `!kv.Key.StartsWith("verify_")` — so the `ch*` predicates were **already redundant
> for the fixtures**, and had been all along; what the deletion removed was the *other* half of the
> reason. Widening five sweeps buys nothing a single check does not, so the predicates stayed and one
> check was added asserting the redundancy is real: *"and every authored room is on it, so the `ch*`
> sweeps skip nothing."* An off-spine authored room now goes red **by name**, instead of being skipped
> silently by five sweeps and covered by none of them. Only `:2522` reads `nav.Rooms` unfiltered and
> genuinely needs its predicate.

> **Retire it by replacement, in one move, not by deletion in two.** The constraint is small and hard:
> `GameConfig.StartingRoomId` has to point at a room that exists, so the game needs somewhere to boot
> the moment the waystation is gone. That makes the retirement **the same operation as the ch1 skeleton
> landing** — generate, re-point, delete — rather than a thing to do first and clean up after.
>
> **Doing it in two moves costs a double gate rewrite**, which is the pattern this project keeps
> catching. Every gate section that stands on the prototype (M1, M2, M3, the heist) would have to be
> rewritten once to survive without the waystation and again when ch1 gives it real content to stand
> on. Nothing is gained by the first rewrite.
>
> **Split the gates by what they cover, not by what they touch.** Migrate only the ones covering code
> that survives — navigation, entity state, persistence, crafting, camps, the panel — onto synthetic
> `verify_` rooms, the technique `GateCamps` already uses. **Delete the heist gates outright rather
> than migrating them**: they cover `DreamRunManager` and `DreamHeistData`, which unit 6 deletes.
> Migrating a gate onto a corpse is work you do twice and throw away once.

**What retires:** `yard_cairn` is part of
2 Waking rooms, 6 Dream rooms, 1 entity, 1 heist, 5 items, 1 recipe and 1 zone that exist to prove the
slice. **The ch1 map replaces all of it**, and roughly four gate sections stand on it (M1's two-screen
navigation, M2's state×realm table, M3's micro-loop, and the heist gates).

> **The order is what matters, and the technique already exists.** `GateCamps` registers synthetic
> `verify_` rooms into `NavigationManager` precisely so a gate never depends on a room Jeremy owns.
> **Move the M1/M2/M3 gates onto synthetic rooms *before* deleting the waystation**, and the deletion
> becomes a content change with no gate consequences. Delete first and the choice is between losing the
> coverage and rewriting under pressure. The heist gates need no such care — they die with
> `DreamRunManager` in unit 6 regardless.

**8. Does spectral vision survive? — NO. `Tab` goes, ratified 2026-08-01. ✅ EXECUTED 2026-08-02,
347 → 338 checks, exit 0.** Jeremy: *"not unless we
find a purpose for it in the game. I don't see one so I would say yes... Tab goes we don't need it."*
And the honest epitaph, his: ***"I think it was just to test tbh."*** It was — it proved the dual-realm
rendering path in M2, and the design walked somewhere else afterwards without anyone going back to turn
it off.

**The old shape of the question.** `Tab` toggles a
Waking-side look into the Pixel Realm: Form 2 bodies, the aura, Dream-only hotspots. It is the
README's first line and the original hook.

> **The finding: it is dead by omission, and nothing has said so.** `combat.md`, `items.md` and
> `placement.md` — every ratified document written since the design settled — mention it **zero times**
> between them. §22 gives the Waking world a complete interactivity vocabulary (Held / Blocked /
> Fouled, and *"everything else is examine text"*) with no realm toggle in it. Creatures are people you
> fight, not spirits you peek at (§21). Severing happens in a boss pocket. The Dream is a place you
> walk into in 3D, through a door.
>
> **Why it cannot wait for unit 6: the fields are per-hotspot and per-placement.** If the overlay is
> dead then so are `HotspotData.RealmVisibility`, `EntityPlacement.DreamPosition` / `DreamScale`,
> `EntityData.SpiritTexture` / `ShatteredTexture` / `EchoTexture` *(already known dead)*,
> `RoomData.DreamBackground`, and the Dream half of `EntityPresentation`'s state×realm table. **Every
> one of ~58 generated screens would otherwise carry `RealmVisibility` on every hotspot** — a field the
> generator has to decide a value for, on thousands of rows, for a mechanic that may not exist.
> Deleting it before pass 1 costs 8 rooms; after, it costs 58 and a regeneration.
>
> **This is a real deletion and it should be said out loud rather than inferred**, which is why it was a
> decision and not a finding I acted on. It removes the thing the README opens with.

**What goes, and what is carefully kept.** Deleted: `HotspotData.Visibility` and the `RealmVisibility`
enum, `InteractionService.MatchesRealm` and the realm filter in `IsAvailable`, `EntityPlacement.`
`DreamPosition` / `DreamScale`, `EntityData.SpiritTexture` / `ShatteredTexture` / `EchoTexture`,
`RoomData.DreamBackground`, `BackdropStyle.Dream`, the Dream column of `EntityPresentation`'s table, the
`Tab` binding, and every gate that asserts realm-filtered behaviour.

**Kept: `GameRealm` and `RealmTransitionManager`.** They stop meaning *which way you are looking* and
start meaning *where you are* — you are in the Dream when you are inside a pocket, which is what unit 6
needs and what invariant 2 is already written against (*no Vitality in the Dream*, *no camp with a run
live*). **The realm concept survives the deletion of the realm toggle**, and conflating the two would
delete something unit 6 has to rebuild. Also kept: `EntityData.SpiritName`, until a boss pocket says
whether a tethered spirit is named in there.

> **When: inside the ch1 swap, not before it.** Same argument as decision 7 and the same trap avoided —
> the heist and the six Dream rooms are precisely the content spectral vision serves, so deleting the
> overlay first means rewriting the heist gates to survive without it, days before deleting them. **One
> move: delete the overlay, delete the prototype, generate ch1, re-point, migrate what survives.**

> **Split three ways on execution, 2026-08-02.** "One move" turned out to be wrong about the *ordering*
> while right about the *dependencies*. The confirmed micro-order is unit 1 → swap → unit 1.5 → generate,
> and unit 1.5 touches `BackdropStyle.Dream` and the realm filter — so the overlay has to die before it,
> and the prototype rooms do not. So:
>
> | | what moved | when |
> |---|---|---|
> | **done** | the overlay — every field, the `Tab` binding, the `SubViewport`, the gates | 2026-08-02 |
> | with generation | the waystation, the six `dream_way_*` rooms, the rat, `StartingRoomId`, and re-anchoring the stranded checks (**76**, not the 154 this row used to claim — corrected 2026-08-03) | pass 1 |
> | unit 6 | `DreamHeistData` / `DreamRunManager`, with the seam | unit 6 |
>
> Deleting the overlay first cost **nothing** in heist-gate rewrites, contrary to the worry above: the
> heist gates never asserted realm-*filtered* behaviour, only realm *state*, which survives. What did
> need care was five M1 checks and five M2 checks, and the judgement in every case was the same one —
> **keep the invariant, move the probe**. A check whose subject was the peek was deleted; a check that
> used the peek to probe something still true was reworded to probe it another way. Two survived that
> way (the HUD's pool order, the camp bench), because the quiet pocket is the same shape as a peek:
> in the Dream, nothing hunting you.
>
> The six `dream_way_*` rooms were remapped `PlaceholderStyle = 2` → `1` rather than deleted, so the
> stranded checks keep their fixture until there is a real one. **The count in this callout was never
> measured**; counted per section on 2026-08-03, the four sections standing *wholly* on the prototype
> total **76** — M1 3, M2 10, M3 38, heist 25 — and the rest is spread through save/load, camps and the
> wound ledger and is still uncounted. The number was never the real blocker anyway: `DreamRunManager`
> retires in unit 6, so retiring the rooms first means hand-building synthetic Dream fixtures for those
> 25 heist checks and gaining nothing.

> **A Dream creature is not an `EntityData` — 2026-08-02.** Jeremy: *"enemies in the spirit realm will
> have completely different functionality than the 2d battle types."* Right, and it changes what unit 6
> has to design rather than only how it renders. `EntityData` is built for **the fight grammar** —
> vulnerable/resistant, durability, damage, tether state, what severing frees. A Dream creature has none
> of that: `dream-puzzles-vs-pursuer-tension` makes them **mindless hazards that cost Resolve**, and the
> pursuer is a chase, not a turn order. **So unit 6 authors its own resource type and does not extend
> `EntityData`** — which is also why `SpiritTexture` and friends were dead weight rather than a head
> start. The one genuine overlap is the boss, which exists in both realms; that is a *link between two
> files*, not a shared schema.

**9. May equipment gate content in a chapter earlier than the one that can make it? — YES, ratified
2026-08-04.** Jeremy: *"Him going back isn't a problem either. I will add a narration when the Bill Hook
is created to prompt him returning to use if the player wants."*

The question was opened by the roster reading as **wrong in three places** — two coats a chapter late, one
a chapter early, and the Bill Hook opening a chapter-1 cul-de-sac (`ch1_bramble` → `ch1_stonefield` →
`ch1_boundary`) while needing a chapter-2 material. `damage-types-and-equipment` blamed a **shared gating
material**, and measuring it found that diagnosis wrong:

> **There is one anvil in the game and it is in `ch2_smithy`. One mortar, in `ch2_apothecary`. That is the
> entire station list.** Eleven of the seventeen recipes name one of them, so **every anvil recipe arrives
> the moment you reach the smithy no matter what its ingredients are.** Moving `mat_scrap_iron` into
> chapter 1 would have accomplished nothing — the station is the dominant lock and the material was never
> load-bearing. *Three locks, and the one that was written down was the least of them.*

So this is not a defect at all, it is a premise already sitting in the content: **chapter 1 is the chapter
before Daniel gets his workshop back.** He has a bench, one axe and his hands. The smithy is him becoming
himself again, which is the beat the Blacksmith's ghost already exists for.

> **What made the answer "yes" is the narration, and it moved where the reward lands.** The objection to a
> chapter-early door was that the pocket's payoff does not land — you reclaim the memory, cannot act on it
> for a chapter, and the reclaimed knowledge becomes an IOU rather than *a road you can walk down*
> (invariant 5). The narration on crafting fixes that by making it **three beats instead of two**: the
> pocket gives the **memory**, the forge gives the **object**, and the line on crafting gives the **road**.
> A return trip you are *reminded* of is a prompt; one you had to remember is homework. It also reads as
> the premise working — *"…I know what that was for now"* is what a scrambled craft coming back should
> sound like.
>
> **Two things follow for placement, and placement is the only lever that matters here** — no `.tres`
> changes. The **three tool blueprints move to ch2/ch3 pockets**, so remembering, the forge and the
> material arrive together and the chapter-1 door is the *one* thing left behind. And the **Limewashed
> Smock's blueprint goes late**, near the ch3 Beasts that deal Caustic, which fixes "a chapter early" from
> the blueprint side instead of by moving the recipe. Nothing in `data/` grants a blueprint yet, so all of
> this is still free.

> **Stations stay landmarks and do not move to the camps — ruled the same day**, against Jeremy's own prior
> inclination: *"I was going to push for the stations to be at the camps but both being in chapter 2 makes
> them feel more like landmarks and meaningful. Not long trip to them from anywhere."* Worth recording
> precisely because it reverses a push he was about to make, and because §19 of `combat.md` already protects
> the other half of it — a station is **not** a camp, or every herb patch would refill both pools.

> **Chapter 1 has no healing but camp rest, and that is now intended.** Both salves need the ch2 mortar and
> no chapter-1 room hands one out, so every fight in the chapter is fought on the Vitality you walked in
> with. Jeremy: *"This is fine just more trips back to the camp and chapter 1 is easy atm."* **§7 of
> `items.md` is wrong on two rows and is corrected there** — it lists Clearmind as craftable in ch1 and the
> Bill Hook as a ch1 tool, both of which predate the single-forge reading.

> **One unbuilt hook, deliberately deferred:** `RecipeData` has no field for a line of text when a recipe
> fires, and nothing in the crafting path can show one. It needs a `CraftNarration` string and somewhere to
> draw it. **Not now** — Jeremy: *"We don't want to do anything with story or tutorial until everything is
> built and tested."* It joins the end-of-build row in §8 rather than any current unit, and the reason to
> write it down at all is that the field wants to exist **before** the content pass writes fifteen of them.

**10. What gates a major repair? — THE BOSS FLAG, and the puzzle is the difference. Ratified
2026-08-04.** `items.md` §6 priced a major repair as *a reclaimed memory + key items + a jigsaw*, and two
of those three had nowhere to live: `MendPuzzleData` carries no cost field and `HotspotData` gates on
flags alone. Jeremy: *"there doesn't need to be an item because the boss is blocking it anyways and it
adds zero... the puzzle would be what we should add/change if we choose to."*

> **The ledgers had already drawn it this way and nobody had read them back.** `ch1_bridge` row 10 and
> `ch2_cart_road` row 12 both read *"Gated on the boss memory"*, and `ch2_bridgehead` row 1 opens with
> *"arrival. The mended bridge behind you."* The transition beat was authored a month ago; what was
> missing was the observation that it needs no mechanism.
>
> **So it costs nothing in code.** A boss win already writes a flag (unit 3a's `freed` / `killed` /
> `defeated` derivation) and `HotspotData.RequiredFlags` already gates on flags. **The three jigsaws stop
> being blocked the moment the three bosses are placed** — 12 of 15 becomes 15 of 15 with no new field.
>
> **And no key item, deliberately.** An item that must always agree with a flag is two copies of one
> fact — the `IsSanctuary` mistake, deleted once already. If the object ever wants to exist for the
> fiction it is a keepsake: not consumed, not checked, not the gate.
>
> **The arithmetic works because repair 3 is not a transition.** Two chapter bridges and the exorcism
> engine, not three doorways — which is why *"the item comes from the boss before"* does not run out at
> chapter 3. It was the objection that looked fatal to this ruling and it dissolves on reading the ch3
> ledger.
>
> **What a "major" board is: a bigger board.** `MendPuzzleData` is `Parts[]` plus an ordered
> `Solution[]`, so *major* means more parts and a longer sequence — authored data, no code. The
> constraint that does not move is §22's, **no fail state and no timer**; Jeremy: *"it is for atmosphere
> mostly."* A major board may be longer and busier and may never be punishing, because the good ending
> hangs off attention rather than skill.
>
> **The chapter-transition beat** — *"an animation type thing to make level shifting more meaningful"* —
> fires on the mend completing, which is a flag that already gets written. Pure pass 3, blocks nothing,
> and nothing needs building now to keep it available.

**11. Are blueprints objects? — YES, AND A PAGE IS A VIEW OF THE KNOWLEDGE, not a second copy of it.
Ratified 2026-08-04.** Jeremy: *"You break anchors and get the crafting recipe on the list... you see the
papers in the 2D world and click and it does something similar."* Plus the fiction that makes the craft
half work: **Daniel writes those pages himself**, once the Dream gives the knowledge back.

> **What makes it safe is that the page id and the blueprint id are the same string.**
> `PocketManager.Complete()` already calls `Discover("lore_craft_edges")`; the page is `AddItem` on that
> same id beside it, and `InventoryCraftingManager.cs:187` already discovers on pickup — so the two
> directions agree by construction and there is no second fact to drift. One crossing check holds it:
> **everything you know, you hold; everything you hold, you know.** That is unit 4b's rule about two
> authored collections naming each other, applied before the content rather than after the defect.
>
> **11 files, and they are derivable rather than invented:** 7 craft pages, whose ids are already fixed
> by the recipes that name them, plus her 4 notes. Names generated, descriptions pass 3.
>
> **The count did not shrink, and an earlier reading of this said it would.** Dropping the pages entirely
> would have taken the item bill to 31; keeping them puts it at **38**, near where `placement.md` had it
> all along. What changed is not the number but who pays it — generated from ids that already exist,
> rather than eleven lore objects somebody has to invent.
>
> **The Lore tab already exists** (`InventoryPanel.cs:451`) and has been an empty shelf since it was
> built. Its job is Jeremy's — *"a sanity check that they have the recipe that they need"* — and, his
> addition, **checkable from anywhere**, since the satchel opens in the field while crafting needs a
> venue. So the satchel answers *what could I make if I walked back*, holding what you carry and what you
> know in one place, and the journal at the bench answers *make it*. Two views with different jobs, not
> one list twice.
>
> **Her notes are 3 teaching pages and 1 silent, not 2 and 2.** §2's bill said two granting, and there
> are **three** blueprint-gated Physick recipes — Deepmend, Burning Oil and the Corrosive Draught
> (Clearmind is known from the start). Two teachers against three locks leaves one of her recipes
> unlearnable for the entire game. Jeremy: *"one recipe per potion, simple as that."* The verifier
> already had the right number — *"hers are three pages of her notes rather than pockets (3 of 3)"* — and
> §2 is corrected to match. **It was invisible because nothing grants anything yet**, so the crossing
> that would catch it has no members on either side. Same shape as the 33 dangling harvest nodes, caught
> one step earlier.

---

## 8. Order of work, and what "done" means

| | Then | Done when |
|---|---|---|
| ~~now~~ | ✅ ledger ch1 drafted · ✅ unit 1 · ✅ **spectral vision deleted** · ✅ **unit 1.5** · ✅ **pass 1, all 59 rooms** · ✅ **the boot moved to `ch1_cottage`, bench placed** · ✅ **pass 2, all 30 fights** · ✅ **unit 2** · ✅ **unit 4a** · ✅ **unit 4b** | ✅ flag vocabulary published; a blight mends and a screen cleanses in the slice; **a tool opens a door**; ✅ **the arsenal exists** — 27 items and 17 recipes, so there are traps to rig and weapons to carry |
| ~~now~~ | ✅ **unit 3a — the encounter state machine** | ✅ a fight can be rigged, started, won, fled, walked out of and died in; ✅ equipping exists; ✅ `freed` / `killed` / `defeated` are three domains and the bill counts **24 + 6**; **510 → 655 checks**, the last seven because `--shots` found a fight outliving its room. **Unplayed** — the walk is the only thing outstanding |
| ~~now~~ | ✅ **unit 3b — retiring the prototype** *(re-anchor, then delete)* | ✅ the waystation, the six 2D Dream rooms, the rat and `herb_gravecap` are **gone** and every gate that stood on them names a synthetic `verify_` room instead; **680 → 670 checks**, rooms 59/59, items 27/27, five break-tests red on the right lines. The subtraction was the easy half — the useful half was **three gates that were passing on nothing**, found by break-testing rather than by reading |

> **The row was split, and 3a landed the half that matters.** §8 wrote both halves as one row on the
> assumption that a fight and a prototype retirement are one unit of work. They are not: `waystation` /
> `giant_rat` appear **48 times across 6 source files**, and bundling a mechanical sweep with a new system
> means the first red gate has two possible parents. So 3a ended in **a fight you can play** and 3b is a
> refactor with a known-green baseline behind it. Unit 4's precedent, and it paid the same way.
>
> **3b has one real hazard, already recorded twice:** *the fixture that arrives by default*. Two things it
> must not lose — `waystation_shed` is one of the camps, and `yard_cairn` is the first step of the M3 heist
> gate. It is now cheaper than it was, because 3a added a `verify_arena` room with eight placements that can
> absorb some of what the yard currently carries. It should still be planned against a green 3a, not before.

> **4b before 3, and it is the same argument that put 4a first.** Unit 3's ratified shape is *rig two
> slots, then commit* (§18), and its entire difficulty surface is 2× / 0.5× against
> `EntityData.VulnerableTo` — so with `data/items/` holding no weapon, no coat and no trap, unit 3 would
> be tuned against a one-button fight and re-tuned after the arsenal landed. Unit 3 is not *blocked*: it
> could author a starting Axe itself and ship with the baseline attack. It would just be paid for twice.
>
> **✅ Settled by the outcome, 2026-08-03.** 4b landed and the argument held better than it was made: unit 3
> now has **two weapons dealing Edge and Blunt, three coats warding Edge / Blunt / Caustic, and six traps**
> to put in the two rig slots, so 2× / 0.5× against `EntityData.VulnerableTo` can be exercised on authored
> content from the first line of the state machine. It also inherits three numbers it does not have to
> invent: `RigSlotsPerFight` and `SalveSlotsPerFight` are both 2, and **weapon `Potency` is 10 against a
> default `Durability` of 30**, so one clean blow is a third of a body. And it turned out that *"it could
> author a starting Axe itself"* was doing more work than it looked — the Splitting Axe is the one item in
> the game with **no recipe**, which needed `GameConfig.StartingItems` and a `GrantStartingKit()` call
> rather than a `.tres`. Unit 3 would have discovered that while building a fight.
| ~~now~~ | ✅ **reading 3a's gates back** *(no new mechanism)* | ✅ **670 → 687 checks**, exit 0. Not a unit — a day spent asking what unit 3a shipped *ungated*, and the answer was four things: the strike verb never said which damage type was in your hand, the satchel's repaint after a fight was uncovered because **a signal connected from C# is not introspectable from C#**, `EncounterManager.Rig` shipped with **no reachable verb at all** (now F8, retiring with a satchel *Set* button), and **nothing checked that the first fight a fresh game walks into is winnable with what the boot gives you**. Jeremy, told that last one: *"I figured you would have designed it this way."* The eight winnability checks contain **no turn count, durability or damage figure** — every claim is a relationship between authored values |

| ~~now~~ | ✅ **unit 5 — the return-to-camp verb** *(§4, and it shrank to one verb)* | ✅ *return to camp* is reachable from any screen with no run live, refused **inside a fight** and **inside a pocket**, and points at the camps you have stood in; every authored camp is gated for a door you can walk out of, and there is **exactly one per chapter** by name. **687 → 758 checks**, exit 0. The unit needed one ruling — *you choose the destination, you are not merely returned to the newest camp* — because return-to-the-newest-only contradicts §20's revised good ending. Two riders landed with it: the **citation gate**, which went red on its own doc comment, and the **entity census**, whose break-test moved exactly one check out of 758 |

> **Why 5 and not 6, with 3b green behind both.** Unit 5 is the smallest thing left that the vertical
> slice cannot do without — the slice's last beat is *camp*, and there is no verb that gets you there.
> Unit 6 is the only unit with a genuine unknown in it, and it now starts from **nothing authored**: 3b
> deleted the six `dream_way_*` rooms, so a pocket format has no prototype to be measured against and
> nothing to migrate. Doing the small certain one first keeps a green baseline under the uncertain one,
> which is the same argument that put 4a before 3 and 3a before 3b, and it has paid three times.
>
> **What 3b left on unit 5's desk, and it is one line.** `AtVenueFor` already refuses inside a live run,
> and as of 3b that refusal has **its own probe** rather than borrowing the camp's — `verify_station` is a
> mortar in a room with no bench, so *"a field station is shut inside a live run"* and *"open again the
> moment the run ends, same room, same satchel"* are a pair that can only be passed by the guard being
> there. Unit 5's *"fusion stays camp-bench-only, so `NotAtStation` still has to tell a field station from
> the bench"* is therefore already gated from both sides. The verb's own two refusals are not.

| ~~now~~ | ✅ **unit 6a — the way into the Dream** *(the door and the contract, not the pockets)* | ✅ a `CrossToDream` hotspot crosses you into a 3D scene and back; `Complete()` grants a craft family and a point of max Resolve — **which nothing could do before, because the ceiling was a `const`** — writes `pocket.<id>.cleared` and breaks the entrance; `Leave()` writes nothing, so an ejection costs only the trip. Quiet: no pursuer, no creatures, no drain. **758 → 830 checks**, exit 0, nine break-tests, two of which found defects in the gates rather than the code |
| ~~now~~ | ✅ **unit 6b — the heist is retired** | ✅ `DreamRunManager`, `DreamHeistData`, `data/heists/`, `HeistDataDir`, `HotspotData.HeistId`, `HotspotKind.TakeAnchor`, `HeistOutcome`, `HeistStarted`/`HeistEnded`, `PursuerProximityChanged`, the dread label, the heist drain lookup and `SaveService`'s stranding guard are **gone**; `InsideTheDream` lost a clause and the seven guards reading it were not touched. **843 → 810 checks**, exit 0 — 33 removed, every one of them *about* the heist. **It was not pure deletion, and I said it would be**: five surviving invariants had no probe outside a heist-driven block, including all four assertions of `RoomView.DisplayedRoomId`. 13 pocket-side probes went in first |
| ~~now~~ | ✅ **unit 8 — no item loss, and one collapse** | ✅ `SurvivesDeath` and `LoseUnprotectedItems` are **gone** — they burned `Component`, and both scarce nouns default to `Component`, so **dying deleted the good ending**. Both empty bars now share `RespawnAtCamp` (revising `combat.md` §6's forced flight), which also closes the hole 6b left when it removed the only listener for an empty nerve. Plus the Dream economy: `DreamVerbs` hold-drain summed across verbs, and `DreamCreatureData` + `DreamCreature` with the cooldown. **826 → 844 checks** — Part A subtracted 8, B and C added 26 |

### What remains, in two lanes

**Re-cut 2026-08-04.** The tail of the table above had become four coarse rows, and the first of them —
*"unit 6c · ch1 pass 2"* — bundled Jeremy's 3D pass with Claude's code into one undefined unit. Units 1
to 8 were each sized by what they unblocked; the tail was not. The two lanes below touch different files
(3D scenes and `data/pockets/` on one side, `src/UI/` and `data/items/` on the other), so they run in
parallel and meet at the vertical slice.

**Measured against the code on the day of the re-cut**, not read out of this document: **1** pocket scene
of 14, **0** entrances, **0** of 8 max-Resolve grants, **11** creature files of 12, **30** fights of 33,
**12** jigsaws of 15, **27** items of 38, **17** recipes of 18 — and **no crafting panel in any form**.
`src/UI/` holds no `CraftingPanel.cs`, and `EventBus.CraftingRequested` fires from
`InteractionService.cs:135` with nothing subscribed, so **clicking the cottage bench in play does nothing
at all.**

**Four debug keys are four missing verbs**, and each retires against a named piece of work below: `F6`
learn-all (the first knowledge pocket), `F7` craft-here (the panel), `F8` rig-here (a satchel *Set*
button), `F10` pocket (the first authored entrance). When all four are gone, the game has no mechanism
that only a keyboard shortcut can reach. **`F11` joined them on 2026-08-04** and retires with the second
boss pocket.

> **✅ Unit 9 landed first, 844 → 860, because Jeremy's lane was blocked on it.** `DreamVerbs` was built as
> *the cost with no button*, so no verb could be held by anything: `DreamVerbHold` turns three rebindable
> actions (`Q` / `Space` / `E`) into holds, `PocketHost` attaches it so no pocket scene carries anything, and
> `F11` grants the two grantable verbs so a `PocketGate` is walkable before a boss pocket exists. **Steps 1
> and 4 of the ladder below are now unblocked**, and the crafting panel stays next in the Claude lane.
>
> **It shipped wrong first and play caught it in under an hour, which is the ladder working as intended.** The
> poller was a node to add to each scene; nobody added one, so `F11` reported two verbs learned and no key did
> anything. The fix moved it into the host — *wiring that can be forgotten fourteen times will be* — and
> `SliceVerifier.Run` now takes the `PocketHost`, closing the last live node the gates had no handle on.

> **The lanes were re-cut again the same day, and this one is Jeremy's call on how the 3D gets built.**
> *"I honestly want to build the spirit 3D over time separately and just hook up the different points as I
> build it. We are over engineering. I rather leave the internal working of the 3D open so that I have more
> freedom to move it in many directions... I think we should defer the 3D world for now and focus on the 2D
> as much as we can."*
>
> **So the 3D lane becomes open-ended rather than scheduled, and nothing further is built *for* it in
> advance.** The seam is already complete and stable — `PocketManager.Complete()` / `Leave()`,
> `DreamVerbs.BeginHold` / `EndHold` / `IsHolding`, the four nodes, `PocketData` — so a pocket can be built
> against it at any pace and new hooks added when a level asks for one rather than before. Two consequences
> recorded rather than acted on: **`PocketGate`'s learned-vs-held semantics stay as they are** (handoff §3),
> and **what Flare *is* stays undecided** until playing a pocket says what the levels need.
>
> **The 2D queue is almost entirely Claude's**, which is what makes the deferral free: the two lanes were
> never competing for the same hands.

| | Jeremy — the editor and the 3D pass | Claude — code, gates, derivable content |
|---|---|---|
| **now** | **open-ended.** Build the 3D world at your own pace and hook points up as they arise; nothing in the 2D queue waits on it | **unit 7a — the journal / crafting panel**, the 11 pages (decision 11), and a satchel *Set* button. Retires `F7` and `F8`, and makes a bench do something |
| **then** | the first `CrossToDream` entrance pair and its `PocketData` → retires `F6` and `F10` → **play the chapter 1 arm end to end**: walk in, fight, take the pocket, mend the blight, cleanse the screen, camp | the three major-repair boards and their hotspots — decision 10, so **no code**; it takes jigsaws 12 → 15 the moment the bosses are placed |
| **then** | clone to 14 pockets · the 3 bosses and the 12th creature file · `CampPanel.tscn` and `CraftingPanel.tscn` against their published contracts | **the boss aura drain** — `EntityData.CorruptionAura` has no reader today, and the two checks 6b retired as universals over an empty set come back with it |
| **then** | chapter 1 sealed — `--verify-content`'s ch1 block turns from a report into gates | |
| **then** | chapter 2 | **unit 7b** — one rule with exactly one customer: `RecipeData.RequiresBothTracks` has no reader either, and the engine is the only recipe that wants it |
| **then** | chapter 3 · the Snarl · the finale | all three endings reachable |
| **last** | pass 3 everywhere — art, jigsaw boards, prose | audio · **narration and tutorial** · the chapter-transition beat on the bridge mend |

**Unit 7a also closes a standing question rather than merely scheduling one.** `handoff.md` §1 carried
*"the journal must land before the first real observation puzzle, though it comes after crafting"*
(`combat.md` §15) as open. Decision 9 made the journal and the crafting screen **one panel**, and this
order puts it first — so the constraint is satisfied by the schedule, and the question is deleted from
that list per its own rule.

**Two play rows are in that table on purpose.** *"Nothing has been played"* has been the stated
outstanding risk since unit 3a and has never had a row to live in. Unit 5 is the only unit played on the
day it landed, and **what play found was not in unit 5** — the satchel had no fight guard, and `C`'s
behaviour was the model it should have copied. A new verb audits the old ones by standing next to it,
which is the argument for playing after each lane's first item rather than after the chapter.

**What the re-cut found, and it is the reason to do this again at the next tail:** three of the four
things that looked like remaining *systems* were already built, or needed nothing. The two-track
knowledge model exists (`KnowledgeTrack`, `ItemData.Track`, `RequiresBothTracks`, all gated), major
repairs need no mechanism (decision 10), and blueprint pages need no new field (decision 11). **What was
actually missing was a panel nobody had listed** — the crafting screen, which four documents describe as
an editor-pass detail, and which is the one thing standing between chapter 1 and being playable.

> **Story and tutorial content is last, ruled 2026-08-04.** Jeremy: *"We don't want to do anything with
> story or tutorial until everything is built and tested."* Two known items are parked here rather than in
> the unit that surfaced them: **`RecipeData.CraftNarration`** plus somewhere to draw it (decision 9 — the
> line that sends you back for the Bill Hook's door), and **the equip tutorial** the deliberately-unequipped
> starting axe exists to be the subject of (`damage-types-and-equipment`). **Both are notes, not work
> queued early** — nothing is built now, including the field.
>
> **Why the field can wait, since the opposite was argued for an hour and it was wrong.** An
> `[Export] string` with a default does **not** invalidate existing `.tres` files: a resource that never
> mentions the property just gets the default, so it can be added after all fifteen recipes are authored at
> no cost. That is different from `DamageType` / `EquipSlot` / `ItemCategory`, which had to land before the
> content pass — **those are fields the author must fill in, and this one is optional.** The rule worth
> keeping is that distinction, not "add schema fields early": *a field the author must supply has to exist
> before authoring; an optional field with a default can be added whenever the thing that reads it is built.*

**The vertical slice is the real milestone**, not chapter 1. It is the first moment the pipeline is
proven, and everything after it is repetition at known cost. Chapter 1 is where the estimate for
chapters 2 and 3 comes from.

**The honest costs, stated so they are not discovered later.** ~~Nothing is played until unit 3 lands~~
— **restated 2026-08-04, because that sentence expired and nothing replaced it.** Unit 3 landed, and the
game is *playable* without having been *played*: 844 green checks over one unit that was played on the
day it shipped. Green gates still have never proved playability, and the two play rows above are the
only thing in this document that addresses it. The **59-screen art bill** is untouched by any of this
and remains the longest pole in the project. And the 3D pocket pass is the last place a genuine unknown
lives; if it runs long, `combat.md` §23's lever applies — **shrink a pocket, do not drop one.**
