# Handoff — where we left off

Last updated: **2026-07-30.** Read this first, then `docs/combat.md` — which is the source of
truth for combat, Resolve, the Dream and camps, and whose §15–17 are the newest material.

**Nothing is currently known to be broken.** The whole slice plays end to end. Commands live in
`README.md`; Claude runs `--verify-slice` before and after code changes, so there is no start-of-
session ritual to perform.

**Newest change: camps are built and gated** — `CampManager` (autoload #10), the shared cache,
crafting gated to a bench, and the 0-Vitality collapse finally carried out. 63 new gates → **279**.
The inventory panel landed the same day (`scenes/ui/InventoryPanel.tscn`, the shared
`PanelTheme.tres`, `UseBlock`/`CanUse`, `I` to open).

**One thing needs Jeremy before crafting works in play again:** a camp is a room with a
`HotspotKind.Workbench` hotspot in it, and **no room has one yet.** Until you place it, crafting and
the cache refuse with `NotAtCamp`. The gates cover the mechanic against synthetic rooms, and the
whole suite is proven to stay green with a bench in *any* room, `waystation_yard` included — so put
it wherever it belongs. See §1, "Done 2026-07-30: camps".

**Build order changed 2026-07-30: all systems first, then all content in one hand-authored pass.**
That un-blocks the item vocabulary conversation — it gated authoring, and authoring is now deferred.
The next system is the **crafting panel**. See §1, "Next".

---

## 1. Next session — M4, camps and crafting

### Done 2026-07-30: camps

Built to `combat.md` §17, "Ratified". `CampManager` is the tenth autoload and is registered **last**,
because it asks the other managers questions rather than answering them.

**A room is a camp precisely when it holds a `HotspotKind.Workbench` hotspot.** `RoomData.IsSanctuary`
was *deleted* rather than renamed to `IsCamp`: two facts that must always agree are better stored as
one, and Jeremy's phrasing settled which one to keep — the camp is "the room you put the workbench
in". So it is impossible to author a bench outside a camp or a camp with no bench, and `IsCamp` is a
pure static function of a `RoomData` that the gates can ask about a room the player is not in.

What arriving does, and what it deliberately does not:

- **Everything happens on arrival; there is nothing to click.** Reaching a camp refills **both** pools
  and silently becomes your return point. No rest button, no claim button. The general principle,
  worth carrying to the next system that wants a confirmation step: **if the player would never
  decline, do not ask.** A prompt nobody refuses is bug-like behaviour dressed as a choice.
- **It only speaks when something changed.** Walking in and out while topped up narrates nothing —
  the same restraint `VitalityManager.SetTo` applies to a wound that cannot lower the bar.
- **Invariant 2 is enforced here, not merely respected.** A camp refills Vitality, so a camp is only
  recognised in the Waking realm with no run live. Peeking with `Tab` keeps the bench open (peeking is
  a look, not a place); *being* in the Dream closes it. A workbench authored into a Dream room is
  therefore a harmless authoring mistake rather than a broken invariant.

**The cache is one shared box, readable from any bench** — per-camp storage was considered and
rejected. `Deposit`/`Withdraw` sit behind `InspectDeposit`/`InspectWithdraw` returning a `StashBlock`,
the same `Inspect*` → `Can*` → act shape as crafting, so a panel can grey a button without owning any
rules. Persisted as `SaveGame.Stash`, alongside `SaveGame.ReturnCampRoomId`.

**Crafting now requires a bench** (`CraftBlock.NotAtCamp`). The order of the checks inside `Inspect`
is load-bearing and gated: knowledge is tested **before** venue, so an unknown recipe stays `Unknown`
out in the open rather than confirming it exists by complaining about the bench.

**The 0-Vitality consequence exists at last: death destroys what you can remake and keeps what you
cannot.** Reagents and catalysts are lost, salves, lore and relics survive — the rule is one static
`InventoryCraftingManager.SurvivesDeath(ItemCategory)`, keyed on the category rather than a per-item
flag, so a new item cannot forget to declare itself —
the cache is never touched, both pools refill, and you wake at the last camp — or at
`GameConfig.StartingRoomId` if you have never reached one, which is why `RespawnAtCamp` refills the
pools itself rather than leaving that to the arrival handler. `VitalityManager` still knows nothing
about rooms or items; it announces `VitalityDepleted` and `CampManager` carries it out.

**63 new gates → 279, and 13 break-tests.** Two lessons came out of writing them, both general:

- **A gate must never depend on which room Jeremy decides is a camp.** Arriving at a camp mutates
  *both* pools, so any gate that carried a part-drained bar across a navigation was hostage to
  authoring. The first break-test exposed a live landmine this way: `GatePersistence` saved a wounded
  bar in `waystation_yard` and would have gone red the moment a bench was authored there. The fix is
  structural — the verifier registers its own synthetic rooms (`verify_camp`, `verify_camp_two`,
  `verify_wilds`) into `NavigationManager` and stands on them, and `GateAuthoredContent` skips
  anything `verify_`-prefixed so a failure can never point at a room Jeremy cannot open. With that in
  place, a workbench was simulated into `waystation_yard` at runtime and the whole suite stayed green.
- **Subscription order governs what a gate can observe, not just what the game does.** The depletion
  gate first tried to read `Current` from inside a `VitalityDepleted` handler and saw `Max`:
  `CampManager` subscribed at boot, so its refill had already run. It watches `VitalityChanged` and
  records the lowest value seen instead — the zero goes out over the bus before the collapse is even
  announced, so the floor is observed as it really happened rather than as its consequence.

Two gates deliberately **not** written. Nothing asserts "a camp is authored" — it would be red until
Jeremy places the bench, and a red gate that means "not yet" teaches him that red means ignore me; a
to-do list masquerading as a gate. And nothing asserts per-camp storage is absent, because the data
model cannot express it.

Ordering inside `SliceVerifier.Run` is now commented where it matters: `GateCamps` runs *after*
`GatePersistence` because reaching a bench refills the bars, and `GateCraftingAndUse` runs last
because it churns the satchel that persistence inherits from the harvest loop.

### Done 2026-07-30: the inventory panel

**Jeremy handed the guided build back rather than doing it himself**, so it went in as the full
Phase 2 inventory deliverable instead of the open-and-close teaching pass it had been scoped to.
The narrow scope only existed to make a first workflow lesson small.

What exists now:

- **`scenes/ui/InventoryPanel.tscn`** — the whole published node contract: a 5-wide
  `%SlotContainer`, a hidden `%SlotTemplate` carrying `SlotSwatch` and `SlotCount`, `%ItemName`,
  `%ItemDescription`, `%UseButton`, `%CloseButton`. Layout is plain on purpose — **restyle it
  freely**, the script only knows the names.
- **`scenes/ui/PanelTheme.tres`** — the shared theme, made on the first panel rather than the third
  per "Two decisions already made". Palette lifted from `Hud.cs` so nothing clashes while it is grey.
- **`src/UI/InventoryPanel.cs`** — a thin view. It asks the manager what is held and whether the
  selection can be used, and decides neither.
- **`UseBlock` + `InspectUse`/`CanUse`** in `InventoryCraftingManager`, mirroring `CraftBlock`.
  `UseItem` now switches on `InspectUse` rather than re-testing the conditions, so the Use button
  and the act of using cannot disagree. Each refusal still gets its own sentence.
- **Wired**: `Hud.Inventory` for `Main` and the gates, `I` toggles, `Escape` closes it when open
  (and is left alone when it is not).
- **28 new gates → 216.** Six were break-tested: a mistyped scene path, a dropped
  `InventoryChanged` subscription, a dropped `VitalityChanged` subscription, both halves of the
  deferred rebuild, and a Use button hard-wired enabled. All six go red when broken.

**The workflow lesson is now a worked example rather than an exercise.** Read `InventoryPanel.cs`
beside its `.tscn` and the whole shape is visible: `%UniqueName` lookups in `_Ready`, every
subscription undone in `_ExitTree`, `IsOpen` reading `Visible` rather than a bool of its own, and
no game logic anywhere. **The crafting panel is the same job again** — same contract style, same
manager underneath, already specified in "The panel node contract" — and it is the better one to
build by hand, because nothing about it is new except the content.

Three traps this build turned up, all of which will bite in the editor:

- **`%UniqueName` does not resolve inside a duplicated node.** Unique names are registered against
  the scene's owner, and a node from `Duplicate()` has no owner until you set one. So the script
  finds the swatch and count *by node name* inside each slot. Keep the template's children named
  `SlotSwatch` and `SlotCount`; the unique-name flag on them is optional and unused.
- **A hidden panel must not rebuild itself.** Every satchel mutation emits `InventoryChanged`,
  including `ImportStacks` on load, so a panel that rebuilds on every event churns a grid nobody is
  looking at. It marks itself dirty while down and catches up in `Open()`. Both paths are gated,
  because the deferred one shows up as a stale grid after a load — the blind spot §5 already names.
- **Restoring a file without touching its timestamp gives you a stale assembly.** A scripted
  patch-build-test-restore cycle that preserves mtime leaves MSBuild convinced nothing changed, so
  the correct source on disk sits behind a `.dll` built from the broken version. It surfaced as
  three inexplicable red gates against clean code. `touch` after any scripted restore.

### Deferred: one section per item type

The grid is a single flat 5-wide list. **Eventually it splits into sections per item type** —
salves, reagents, traps, equipment — but not until the type vocabulary below is settled, because
sections are a claim about what the categories *are* and drawing them now would freeze a guess.
The ordering already anticipates it: `Ordered()` sorts by `ItemCategory` first, so the stacks are
grouped in the flat list and turning the groups into headed sections is a layout change in the
`.tscn` plus a loop in `Refresh()` — no manager work, no new gates beyond one asserting a stack
lands under its own heading.

### Next — and the order changed, deliberately

**Decided 2026-07-30: build every system first, then author all the content by hand in one pass.**
Feature-by-feature means content authored against a half-settled vocabulary, and then re-authored
when the vocabulary moves — the panel sections above are a small instance of exactly that. So the
sequence is now *all plumbing, then all content*, and content authoring is a distinct later phase
rather than something that trails each system.

The consequence that matters: **the item/trap/equipment vocabulary is no longer blocking.** It was
blocking only because it gated authoring recipes and items, and authoring is now deferred by
choice. Systems can be built against the *shape* of an item (a category, a cost, an effect hook)
without knowing which items exist — which is already how `ItemData` and `RecipeData` are written.
Have the conversation when the content pass starts; it is the first thing that pass needs.

So, systems still unbuilt, roughly in dependency order:

1. **The crafting panel** against the published contract — the same job as the inventory panel with
   nothing new in it, which is what makes it the good one to build by hand. Note that it now has a
   third state to draw: `CraftBlock.NotAtCamp`, "you need a bench for this".
2. **Combat** — the turn loop, fixed Defence, traps resolving, the six-turn nerve clock. The
   largest by far and the one with the most unratified design under it.
3. **The journal's knowledge model**, then its UI. **Audio last** (see memory).
4. **2.5D in the Dream** — the one remaining architectural risk, and the only item here that could
   invalidate work already done.

The crafting panel is next — and placing the first `Workbench` hotspot is what makes it testable in
play, since without a bench every craft is legitimately refused.

### Authoring a hazard into a room — done, and what it taught

**`yard_cairn` is a hazard** (`VitalityCost = 10`), authored 2026-07-29 and confirmed in play.
`HotspotData` carries `VitalityCost` and `HazardText`; `InteractionService` charges it on any
successful activation. `F2` still takes a debug wound, which is the quickest way to exercise the
collapse path without hunting for a hazard.

**Two authoring mistakes came out of that first pass, and both now have gates** (see "content" in
`--verify-slice`), because both were invisible in the inspector and nearly invisible in play:

- **`RequiredFlagValue = false` with an empty `RequiredFlag` does nothing.** `IsAvailable` skips the
  entire check when the flag name is blank, so a hazard meant to fire once stayed repeatable — and a
  repeatable 10-point hazard walks you to zero, which currently has no recovery. Setting the *value*
  without the *name* is the trap.
- **The inspector's "New HotspotData" on an existing array element silently replaces it.** That is
  how `yard_to_shed` became a blank hotspot: the shed went unreachable and a dead click-zone
  appeared at the default region in the middle of the yard. Restored from the seeder's values. If
  you mean to add a hotspot, grow the array — never re-type an existing slot.

The content sweep is deliberately **value-agnostic**: it asserts a hazard exists somewhere and that
no hotspot is quietly inert, and never pins a cost, label or room, because those are Jeremy's to
retune and a gate that argues with the author is a gate that gets deleted.

**`F3` is the authoring aid.** The debug panel's top block now carries `vitality` beside `resolve`
(with no rate, because the body has no clock) and a `hazards` line listing every hazard in the
current room with its cost and kind. So a `VitalityCost` you typed in the inspector can be
confirmed without clicking into it: if the line still says "(none in this room)", the edit did not
land. Both live in the top block on purpose — the flag list underneath can grow past the bottom of
the screen, and an authoring aid you cannot see is no aid.

**A hazard is not a trap.** Traps are the player's own crafted tools (`combat.md` §4) and are
blocked on M4. A hazard is the world costing you the body, and is blocked on nothing.

Recipe, in the inspector, on any existing hotspot:

| Field | Value |
|---|---|
| `VitalityCost` | how much body it takes; keep it modest (see the zero-Vitality note below) |
| `HazardText` | what it feels like when it lands |
| `Label` | **has to warn you** — "Cross the Broken Floor", not "Floor" |

It composes with the kinds that already exist, which is why there is no `HotspotKind.Hazard`: a
cost on `Navigate` is a route you may take but will pay for, on `Harvest` it is thorns, on
`Interact` it is reaching into something you should not have. Hazards repeat by default; use
`RequiredFlag` / `SetsFlag` to make any single one a one-time event.

**The design constraint, which is not optional.** `combat.md:231` records that Resolve was
deliberately given **no ambient drain**, on the grounds that a drain the player cannot act on
teaches nothing. A per-click cost is discrete and avoidable *by construction*, which is exactly
why it was built as a cost on hotspots rather than as a per-room or per-step tax. If you later
reach for "walking through this region hurts", that is the rejected pattern coming back in through
a side door.

Invariant 2 is enforced in **code**, not authoring: `ApplyHazard` refuses to charge if you were in
the Dream or inside a live run when you clicked, so authoring a Dream-side hazard is harmless
rather than dangerous — it simply will not bite. Gated both ways.

**Closed 2026-07-30.** Authoring a hazard made 0 Vitality reachable in play for the first time, and
for a day it had no consequence — the documented placeholder, chosen over inventing a stopgap
respawn, because "you wake at a camp" had nowhere to point until camps existed. Camps now exist, so
emptying the bar costs you your reagents and sends you back. Keep hazard costs modest anyway: the
collapse is now a real punishment rather than a dead end.

### Where the project stands

M0–M3 plus the Dream heist, M4's crafting layer and camps, 279 automated gates. The playable loop,
with no art:

```
[WAKING]  search the cairn  →  the relic is exposed  →  touch it
[DREAM]   six-room maze, Resolve draining, the Mind-Stalker hunting you
          →  reach the Baron's solar, take the anchor
[WAKING]  ejected, Resolve refilled. the rat is Severed — grey, slumped, road clear
          →  finish it  →  zone cleanses, sun comes out, bitterroot appears to harvest
```

Plus spectral vision, navigation, flag-gated hotspots, harvest-once nodes, zero-respawn
persistence, JSON save/load, a debug panel (`F3`) and procedural placeholder art for everything.

**Content is 2 Waking rooms, 6 Dream rooms, 1 entity, 1 heist, 5 items, 1 recipe, 1 zone, and no
camp** — all `.tres` under `data/`, all editable in the Godot inspector. New rooms are authored there
from now on; do not run `--seed-content`, which overwrites.

The missing camp is the one gap between "gated" and "playable": add a hotspot with
`Kind = HotspotKind.Workbench` to whichever room should be the first camp, and crafting, the cache and
the respawn destination all come alive at once. Nothing else about the hotspot matters — the label and
region are yours, and `CampManager` only looks at the kind.

### M4: what is built, and what is left

**Built 2026-07-29 — the whole resolution layer, no UI.** All of it in
`InventoryCraftingManager`, all of it gated:

- **`RecipeData`** + **`RecipeIngredient`** resources, authored as `.tres` under `data/recipes/`.
  One real recipe exists (Clearmind Salve from 2 bitterroot + 1 tallow).
- **`Inspect` / `CanCraft` / `Craft`.** A craft is all-or-nothing: every input is checked before the
  first is removed, so a refused craft consumes nothing. `Inspect` returns a `CraftBlock` so the
  panel can say *which* kind of no it is — not knowing how is a different sentence from being short
  of bitterroot, and an unknown recipe must not advertise its inputs.
- **Blueprint discovery**, built in from the first recipe as the handoff insisted. Knowledge lives
  in the manager, not in the satchel, so it survives spending the page you read it from. Picking up
  an `ItemCategory.Lore` item teaches it. Persisted as `SaveGame.DiscoveredBlueprints`. The journal
  becomes a *view* of this rather than an owner of it.
- **`UseItem`.** Salves restore Vitality, never Resolve. Refuses at full Vitality rather than
  wasting her work (invariant 7). Catalysts are deliberately *not* usable this way — they are a
  Dream loadout spent by crossing, not something you click in a satchel. Since 2026-07-30 it
  switches on **`InspectUse`** / **`UseBlock`**, the sibling of `CraftBlock`, so a panel can grey a
  Use button from the same source of truth the act of using consults.

**Built 2026-07-30 — the inventory panel.** See §1. `Hud.Inventory`, `I` to open, and the grid,
selection, detail column and Use button all gated through what they draw.

**Built 2026-07-30 — camps.** See §1. `CampManager`, the shared cache behind `StashBlock`, crafting
gated to a bench, and the collapse that loses what you can remake.

**Left:**

- **The crafting panel** — Phase 2, authored as a scene. See "The panel node contract" in §2. The
  inventory panel is the worked example to copy.
- **Her cache** as a real place with finite named salves in it. The storage box exists as of
  2026-07-30; what is missing is the *authored* cache — a camp with her finite salves already in it,
  which is content rather than code.
- **The item / trap / equipment vocabulary**, before any more recipes are authored. See §4.

### Two decisions already made that affect how you build it

1. **Author the new panels as `.tscn` scenes**, not in code. `Hud.cs` stays as it is — it works and
   its layout never changes. But grids, slots and hover states are layout you need to see while
   tuning, and there are three panels coming. Rules: scenes own layout, scripts own behaviour;
   reference nodes with `%UniqueName` or an `[Export] NodePath`, never `GetNode("A/B/C")` string
   paths; make the shared theme `.tres` on the *first* panel, not the third; and keep panels
   rendering from `ItemData.PlaceholderColor` so nothing depends on icon art existing.
2. **Keep the logic in the managers.** `--verify-slice` works because it drives
   `InteractionService` and the managers directly, and asserts against *them*. Crafting resolution
   belongs in `InventoryCraftingManager`, with the panel a thin view over it. Put recipe logic in a
   panel script and it becomes untestable headlessly.

   Note the correction: the verifier does *not* skip the UI — `Main` builds `RoomView` and `Hud`
   before calling it, and both are live and reacting to every signal in a headless run. What it
   lacked was any gate that asked them what they were showing, which is how the ejection failure
   hid. So when you add the M4 panels, hand them to `SliceVerifier.Run` the same way and assert the
   state they display, not only the manager state behind them.

### Settled: no item restores Resolve

`ItemData.ResolveRestore` is now **`VitalityRestore`** (2026-07-29). It had been `SanityRestore`,
then renamed mechanically, and in that form it contradicted `combat.md` §6 — which restores Resolve
only by leaving the Dream or resting at a camp, never from an item.

The name *is* the decision, and it is defended by a gate asserting a salve leaves Resolve untouched.
Letting an item restore Resolve puts a crack in invariant 11: it is a short step from "this steadies
your nerve" to a consumable that substitutes for severing, and that takes invariant 1 with it.

`salve_clearmind.tres` was hand-edited rather than reseeded (a scalar, so none of the typed-array
risk), and a gate asserts the 35 survived — a stale property name in a `.tres` deserialises to zero
in silence.

### What Vitality is and is not, as built

**Built:** `VitalityManager` (an autoload, registered just after `ResolveManager` — the order in
`project.godot` is the subscription order, and it matters: see invariant 13),
`GameConfig.VitalityMax`, `EventBus.VitalityChanged`
and `VitalityDepleted`, a `VitalityMeter`, a `SaveGame.Vitality` field that round-trips,
`HotspotData.VitalityCost` / `HazardText` with the charging logic in `InteractionService`, an `F2`
debug wound, and 41 gates — including one asserting the bar does not move across an entire heist
(invariant 2) and one asserting the manager has **no `_Process`**, because giving the body a clock
is the easiest way to break `combat.md` §6 by accident.

**The HUD emphasises whichever pool can actually move.** `Hud.RefreshPools` does two things: it
puts that pool on top, and it fades the other to 45% opacity. So out here Vitality is uppermost and
full-strength; inside a heist Resolve takes the top slot and the body dims.

Three decisions baked into that, each with a gate:

- **Keyed on `DreamRunManager.IsRunning`, never on the current realm.** Spectral vision flips the
  realm on every `Tab` press while Resolve does not move at all, so realm-keying would make both
  bars hop and dim every time the player peeked — and it would be *wrong*, because peeking is a
  look, not a place (invariant 4): you are still standing in the Waking world with a body that can
  be hurt. Entering a heist already changes the room and locks the realm, so a reflow there reads
  as part of that transition. When Waking boss fights land, this widens to include a fight against
  a still-`Corrupted` boss — the question it asks, "which pool can move right now?", survives.
- **Resolve is never faded** (decided 2026-07-29). In a heist it is the clock you are playing
  against; out here it still tells you what you have to cross with.
- **Vitality dims but is never hidden.** Invariant 2 promises nothing in the Dream can kill you, and
  a visible-but-recessive bar *states* that promise where an absence would leave the player to
  infer it — and horror players infer the worst. It also keeps the contrast that teaches the
  two-pool split: one bar guttering beside one that will not move. A gate asserts the opacity stays
  above 0.2 for exactly this reason. Bonus: at 45% the warm bar reads nearly grey, which keeps it
  out of the way of the monochrome Dream's rule that one coloured thing should scream.

The opacity is **snapped, not tweened** — `_Process` does not tick meaningfully headlessly, so a
tween would leave the gates asserting an intended value rather than the drawn one. Inside a heist
transition the snap is invisible.

`Hud.TopPoolLabel`, `VitalityOpacity` and `ResolveOpacity` expose what is actually drawn, so the
gates assert the result rather than re-deriving the condition.

**Deliberately not built — do not read the current behaviour as the design:**

- ~~**Recovery from zero.**~~ ~~**The 0-Vitality consequence.**~~ **Both closed 2026-07-30 by
  camps.** `EventBus.VitalityDepleted` was described here as "the seam the camp respawn plugs into",
  and that is exactly what happened: `CampManager` subscribes to it and `VitalityManager` still knows
  nothing about rooms or items. The one thing left is authoring — with no `Workbench` hotspot placed,
  the collapse falls back to `GameConfig.StartingRoomId`.
- **Its interaction with the two-route economy.** Until something spends Vitality, **the two
  routes in `combat.md` §1 still cost the same thing**, so invariant 1 remains unexercised. The
  pool existing does not fix that; a damage source does.

One implementation note worth keeping, because it is load-bearing and invisible: `SetTo` fires the
collapse only on the *transition* down to zero, since a wound that cannot lower an already-empty
bar is not a change and raises no event. `Import` exists separately for load, and does **not**
announce — a load restores state, it does not replay the events that produced it.

---

## 2. Who does what

Agreed 2026-07-29. Not a new rule so much as making two existing decisions explicit: panels are
authored as scenes, and game logic lives in managers.

| | |
|---|---|
| **Jeremy, in the Godot editor** | Anything judged by eye: rooms, hotspot regions, hazard costs, labels and flavour text, panel layout, slot spacing, hover states, colour, and which content exists at all. |
| **Claude, in code** | Anything that has to be provably correct: managers, `.tres` resource *shapes*, the save format, and the gates. Runs `--verify-slice` before and after every change. |

**The interface between the two is a node-name contract.** Before a panel gets built, Claude writes
down the uniquely-named nodes the script will look for; Jeremy lays out whatever he likes around
them so long as those names exist. Neither side blocks the other, and wiring becomes a five-minute
job instead of a renegotiation. See "The panel node contract" below.

Three standing rules that make this work, all learned the hard way:

- **Claude never runs `--seed-content`** — it overwrites everything under `data/`, which is
  Jeremy's work.
- **Claude never hand-edits a `.tres` typed array** without a gate that asserts the contents
  loaded. A malformed `Array[...]` line yields an empty collection and no error. The recipe added
  in Phase 1 is gated exactly this way.
- **Run the gates unconditionally; add gates sparingly.** Settled 2026-07-30. Measured on
  Jeremy's machine: a full run is **0.57 s** at 188 checks and has not moved measurably at 279, an
  incremental build 2.5 s, so the whole
  edit→verified loop is ~3 s and it sits in Claude's turn, not Jeremy's authoring loop. Deciding
  whether a change is "worth checking" costs more than checking — and gets it wrong precisely on
  the changes that matter, because the ones that break distant things are the ones that don't look
  like they could. The live example: moving `GateCraftingAndUse` later in the run was pure test
  ordering, and it broke the **save round-trip** gate, because crafting consumed reagents a later
  gate expected to still exist. So: run on any code or `.tres` change, batched by *coherent unit*
  rather than per file-write; skip only for docs, which cannot fail a gate. The selectivity belongs
  on the other side — a **new** gate costs a break-test to prove it isn't tautological, so write one
  only when something actually broke or a new invariant becomes load-bearing.

### Phases

1. **Now — parallel.** Jeremy authors hazards into rooms and tunes costs. Claude builds M4's
   resolution layer. ✅ *Claude's half is done — see §1.*
2. **The panels.** Claude publishes the node contract and the manager beneath it; Jeremy builds the
   inventory grid and crafting panel as `.tscn`; Claude wires and gates them.
3. **Camps.** ✅ *Claude's half is done — see §1.* Jeremy's half is one `Workbench` hotspot in
   whichever room should be the first camp. It was the clean case of the division working: the
   mechanic was built and gated against synthetic rooms without waiting on the judged call, and the
   suite is proven to survive the bench landing anywhere.

### The panel node contract

For Phase 2. Build the layout however it looks best; the scripts only need these names to exist,
set as **scene-unique** (right-click a node → *Access as Unique Name*, so it resolves as `%Name`).
Anything not listed is yours to add, move or restyle freely — extra nodes are ignored.

**`scenes/ui/InventoryPanel.tscn`** — root `Control`, `Visible = false`:

| `%UniqueName` | Type | Script does |
|---|---|---|
| `%SlotContainer` | any `Container` (a `GridContainer` is the obvious choice) | fills with one slot child per held stack; empty it and rebuild on `InventoryChanged` |
| `%SlotTemplate` | `Control`, a child of the container, `Visible = false` | duplicated per stack. Needs `%SlotSwatch` (`ColorRect` — tinted from `ItemData.PlaceholderColor`, so nothing depends on icon art) and `%SlotCount` (`Label`) inside it |
| `%ItemName` | `Label` | selected item's `DisplayName` |
| `%ItemDescription` | `Label` | selected item's `Description` |
| `%UseButton` | `Button` | calls `UseItem`; disabled when the selection cannot be used |
| `%CloseButton` | `Button` | hides the panel |

**`scenes/ui/CraftingPanel.tscn`** — root `Control`, `Visible = false`:

| `%UniqueName` | Type | Script does |
|---|---|---|
| `%RecipeList` | any `Container` | one row per recipe |
| `%RecipeTemplate` | `Control`, child of the list, `Visible = false` | duplicated per recipe. Needs `%RecipeName` (`Label`) and `%RecipeStatus` (`Label` — carries the `CraftBlock` reason) |
| `%IngredientList` | any `Container` | inputs of the selected recipe, held-vs-needed |
| `%IngredientTemplate` | `Control`, child of it, `Visible = false` | needs `%IngredientName` and `%IngredientCount` (`Label`s) |
| `%CraftButton` | `Button` | calls `Craft`; disabled unless `CanCraft` |
| `%CloseButton` | `Button` | hides the panel |

Three things worth knowing while laying these out:

- **An unknown recipe must not show its ingredients.** When `Inspect` returns `CraftBlock.Unknown`,
  the row shows a name (or a blank) and nothing else — leave room for a "you don't know this yet"
  state that is visibly different from "you're short of materials".
- **Templates stay in the scene, hidden.** Duplicating a hidden child is how the script builds rows,
  so don't delete them once the layout looks right.
- **`%UniqueName` does not work *inside* a template.** Learned building the inventory panel: unique
  names resolve through the scene's owner, and a duplicate has none, so the script finds a
  template's children by node name. The names in the tables above (`SlotSwatch`, `RecipeName`, and
  the rest) are therefore the contract; marking them unique as well is harmless but does nothing.
- **The shared theme `.tres` belongs on the first panel, not the third** — that decision is already
  in §1, and it is much cheaper to honour now than to retrofit.

Input actions `inventory` (`I`) and `journal` (`J`) already exist in `project.godot`, unused.

**The conversation to have at the end of Phase 2:** the item, trap and equipment-type vocabulary.
`combat.md` §4's trap table and §7's type sketch are explicitly unratified. The plumbing does not
care, but *recipes and items cannot be authored* until it is settled, so it will block Jeremy
before it blocks anything else. Best had once there is an inventory grid on screen to look at.

---

## 3. Placeholders and traps for the unwary

Things that *work* but are not the real implementation, or that will mislead you:

| Where | What |
|---|---|
| `SliceVerifier.Run(view, hud)` | Sets `ResolveManager.Active = false` so drain cannot race the assertions. **Nothing timing- or drain-related is tested.** Both arguments are null-tolerant so the gates stay runnable from anywhere — but `GateHarness` asserts they arrived, so a wiring mistake in `Main` fails the run instead of silently skipping the five view checks. |
| `verify_`-prefixed rooms | `RegisterVerifyRooms` injects `verify_camp`, `verify_camp_two` and `verify_wilds` into `NavigationManager` at the top of a run, so camp behaviour is tested against ground the verifier owns rather than against whatever Jeremy authored. They are **not** in `data/` and `GateAuthoredContent` skips them. **Any new gate whose meaning depends on a room's contents belongs on a `verify_` room**, and anything asserting "the game has content" must keep excluding them, or a synthetic room will satisfy a check about authored work. |
| `GateHeistMechanics` | Calls `run.ForceClear()` before `GatePersistence`, so save/load is only tested from a Waking room with no run. This is exactly where the save/load soft lock hid. |
| headless runs | `_Process` never ticks meaningfully, so the pursuer only moves when a test calls `DebugSetPursuerRoom`. **Pursuit timing is untested.** |
| the view, in gates | `RoomView` and `Hud` *are* live in a headless run and now have gates asserting the room they display — but only the room. Backdrops, overlays, tweens and hotspot rects are still unasserted, and `--shots` is the only thing that looks at a pixel. |
| `ContentSeeder.cs:219` | The entry puzzle is a plain `Interact` hotspot ("search the cairn"). The minigame does not exist. |
| `InteractionService.cs:136` | Finishing a `Severed` body is **one click**. This is where the turn-based item fight goes. |
| **No camp is authored** | A camp is a room holding a `HotspotKind.Workbench` hotspot, and no `.tres` has one. So **crafting and the cache are unreachable in play** — every attempt returns `NotAtCamp` — and the collapse falls back to `GameConfig.StartingRoomId`. All of it is gated against synthetic `verify_` rooms, so the gates being green says nothing about this. One hotspot fixes it. |
| `ItemData.VitalityRestore` | Read at last, by `UseItem`. Only salves use it. |
| Everywhere | **No audio at all.** Deliberately last — but see the note in `combat.md` §14. |
| Everywhere | Craft / Physick / Attunement are designed, unbuilt. |
| Everywhere | **Defence does not exist as a field.** Decided 2026-07-27 to be fixed per unit and unaffected by severing (`combat.md` §2). `EntityData` has no such property yet — do not reach for `CorruptionAura`, which carries regen and the Resolve drain instead. |
| Everywhere | Traps are designed in detail, unbuilt. They need combat to resolve into, and the type vocabulary before they can be authored. |
| `RoomData.BaseCorruption` | **Not vestigial — an earlier entry here said it was, and that was wrong.** It no longer drives any drain (Waking rooms do not drain, Dream rates are per-heist), but `RoomView.cs:187` reads it as the placeholder backdrop's **fog density**: `fog: 0.35f + BaseCorruption * 0.65f`. Deleting it would visibly change every Waking room. Treat it as a look knob, not dead weight. |
| ~~`RoomData.IsSanctuary`~~ | **Deleted 2026-07-30, not renamed.** It was going to become `IsCamp`; instead camp-ness is derived from the workbench hotspot, because a flag and a bench that must always agree are two copies of one fact. If you find the name in an old comment, that is why it is gone. |
| `PlaceholderBackdrop`, `EntityOverlay` | All art is procedural. Every texture field is nullable and unset. |

**When a bug reproduces in play but the gates are green, there are two things to suspect, in this
order.** First, a **state the verifier normalises** — an active run, a live clock, a mid-sequence
save. That is where the save/load soft lock hid: `GateHeistMechanics` cleared the Dream run before
`GatePersistence` ever saved, so a save taken mid-heist restored a Dream room with no run and no
door out. Second, a **layer it never questions**: everything downstream of the manager state the
gates assert — which is where the ejection failure hid (see "Closed post-mortem"). Do not assume the logic is fine
because the assertions are; check that the assertions are pointed at the thing that is actually
broken. One bug of each class has shipped and been fixed, and in both cases the gates were green
and correct about what they measured.

**There is a third class, and it is the gates' own fault: a check that cannot fail.** Found while
writing the Vitality gates on 2026-07-29. Two of them were tautological — one compared a value to
a copy of itself taken a line earlier, and one tested "loading an empty bar raises no event" from a
bar that was *already* empty, so the clamp satisfied it and the behaviour it named was never
exercised. Both printed `ok`. Neither measured anything.

The discipline that catches this is cheap and it is not optional: **break the code on purpose and
watch the specific check go red.** All four load-bearing Vitality gates were verified that way —
damage injected into `DreamRunManager.Begin` to break invariant 2, `Import` rewired to `SetTo`, a
`_Process` added to the manager, and the save field dropped — producing exactly four named
failures, then reverted. A gate you have never seen fail is a comment with a `Check(` in front of
it.

**And a fourth, found writing the camp gates on 2026-07-30: a gate that goes red when Jeremy
authors something.** Camps made room entry mutate both pools, which meant any gate carrying a
part-drained bar across a navigation depended on which rooms happen to be camps. `GatePersistence`
saved a wounded bar in `waystation_yard` and would have failed the day a bench was authored
there — and the failure would have accused the save system of a bug that was really a content
change. It surfaced only because a break-test's wide blast radius lit it up; nothing was failing at
the time.

This one is worse than a tautology, because a tautology merely fails to catch bugs while this
*invents* them, and a red gate that turns out to be nobody's fault is how a suite gets ignored. The
rule that falls out: **a gate must stand on ground the verifier owns.** Hence the `verify_` rooms.
The check that you have got it right is the mirror of the break-test — simulate the authoring change
at runtime (add the hotspot to the live `RoomData`, never to a `.tres`) and confirm the suite stays
green.

The related judgement call, made the same day: **do not write a gate that means "not yet".** One
asserting "a camp is authored" was considered and rejected — it would sit red until Jeremy placed
the bench, which teaches him that red means ignore me. Missing content is reported in prose, here and
in the table above. Gates assert what must be true, never what is merely wanted.

---

## 4. Open questions, in the order they'll bite

1. ~~**What shape is a hazard?**~~ **Answered 2026-07-29:** a `VitalityCost` on the existing hotspot
   kinds, charged per successful click, rather than a `HotspotKind.Hazard` or a per-room tax. Built
   and gated — see §1. What remains is authoring, not design.
2. **The item, trap and equipment-type vocabulary** — to be discussed before crafting is built.
   `combat.md` §4's trap table and §7's type sketch are both explicitly **unratified**: they were
   written in a design pass, not decided, and one row (the salt line) was struck on 2026-07-27
   for contradicting the tether rule. Nothing in them blocks M4's crafting *plumbing*, which is
   content-agnostic — but do not implement that table as though it were a spec.
2. ~~**`ResolveRestore` vs `VitalityRestore`**~~ **Answered 2026-07-29:** renamed to
   `VitalityRestore`; no item restores Resolve. See §1.
3. **Trap placement** — authored sites per room, or free-form on a grid? Authored is far less work
   and probably reads better on painted backgrounds. Nothing blocks on it.
4. **The entry puzzle** is a placeholder, and is now the first real instance of Waking-world puzzle
   content — so it sets the grammar for every puzzle after it.
5. **The journal** must land before the first real observation puzzle, though it comes after
   crafting. See the callout at the end of `combat.md` §15 for why.
6. **Is the pure-BFS pursuer fair?** Never tested against a real player. Much smaller problem now
   that it only runs inside boss pockets.

---

## 5. Closed post-mortem: the ejection failure

Kept because the way it hid is more useful than the bug itself. `RoomView.cs` and
`SliceVerifier.cs` both cite it by name in comments. **Nothing here is open.**

Reported in play: reached the tether, Resolve stopped draining, and could not leave the Dream.

**It was never in `End()`.** The leading hypothesis — an exception between `ActiveHeist = null`
and `GoToRoom` — was wrong. Every manager was correct the whole time; the *view* was showing a
room the player had already been pulled out of.

The pursuer starts in the solar, so walking into it is the most likely thing to happen on a fast
run. Doing so calls `DreamRunManager.NotifyPlayerMoved` — which is subscribed to `RoomEntered` —
and that captures you and ejects you **from inside that emit**. So:

1. `GoToRoom("dream_way_solar")` sets `CurrentRoomId`, emits `RoomEntered("dream_way_solar")`.
2. `DreamRunManager` is first in the subscriber list (autoload), sees the pursuer, calls `End()`.
3. `End()` navigates to `waystation_yard` — a **nested** `GoToRoom`, which emits and completes
   its own `RoomEntered`. `RoomView` and `Hud` handle it and correctly show the yard.
4. The outer emit resumes, and *now* reaches `RoomView` with the stale `"dream_way_solar"`.
   The view repaints the Dream room over the top.

Result: no drain (the run really is over), the Dream room still on screen, and — because the
realm is now Waking while the room is a Dream room whose hotspots are all `DreamOnly` — **not one
clickable thing anywhere.** The HUD confirmed it by still naming the Baron's Solar.

The fix is a stale-payload guard in both subscribers — `RoomView.OnRoomEntered` and
`Hud.OnRoomEntered` return early when the payload is no longer `NavigationManager.CurrentRoomId`.
Stated as invariant 13 under "Invariants" and in the README under "Signal payloads can arrive stale," because
the trap belongs to the bus, not to these two files.

**Why nothing caught it:** `GateHeistMechanics` already walked into the pursuer and asserted
`CurrentRoomId == "waystation_yard"` — and that assertion was *true*. The gates had never asked
the view what it was drawing. They do now, in five places: `RoomView.DisplayedRoomId` is asserted
after an ordinary door, after the capture ejection, after the success ejection and after the
mid-heist load, and `Hud.DisplayedRoomName` is asserted after the capture ejection.

The ordinary-door one is the control, and it matters: without it, "the view never shows the wrong
room" would also be satisfied by a view that draws *nothing*. It passed before the fix and after.
The other two new gates — the capture ejection and the HUD — were watched to fail on the unfixed
build (`2 FAILURE(S) of 95 checks`) and pass on the fixed one. The remaining two, the success
ejection and the load, are not nested and so passed either way; they are regression cover, not
evidence.

> The general lesson, worth more than the bug: when play and the gates disagree, suspect the
> boundary the gates do not cross. One bug hid in a state the verifier normalises (see "Placeholders and traps"); this one
> hid in a layer the verifier never questioned.

---

## 6. Invariants — break these and the design collapses

Learned the hard way over several design passes. Each one is load-bearing:

1. **The two routes must cost different currencies.** Sever costs Resolve, time and a catalyst;
   overkill costs finite supplies. If they ever cost the same thing, the choice becomes a
   difficulty slider. This is the reason for most of the rules below.
2. **Nothing in the Dream can kill you.** Every ending is an ejection; only the price differs.
   Mindless creatures cost Resolve, bosses eject — and **Vitality must never appear in the Dream**,
   or this collapses along with the two-pool split. Three places enforce it rather than trusting
   authoring: `InteractionService` refuses to charge a hazard, `CampManager` refuses to recognise a
   camp, and `RespawnAtCamp` pushes an error rather than swallowing a collapse that reached it from
   inside a run — a silent return there would hide the upstream bug.
3. **The anchor→body loop runs one way only.** Severing weakens the body. Wounding the body does
   *not* weaken the spirit. (Proposed and explicitly rejected.)
4. **Spectral vision is a look, not a place.** `Tab` shows Form 2 and the aura; you cannot sever
   through it. Otherwise the relic and the whole heist are pointless.
5. **No stat multiplies damage.** All progression gates *access*. Out-statting the horror is what
   kills horror. Crafting grants variety and access — tools, not tiers.
6. **Fights run 3–6 turns.** Overkill must be expensive, never long.
7. **Her named salves are finite and uncraftable.** One cache, forever. Crude substitutes only —
   but they must be *sufficient* to win, or failure quietly deletes the overkill route. Salves
   survive death; the only way to lose them is to spend them. Enforced since 2026-07-30 by
   `InventoryCraftingManager.SurvivesDeath`, keyed on `ItemCategory` so nothing has to remember to
   opt in, and by the cache being untouched by a collapse.
8. **Views never branch on entity state.** They ask `EntityStateManager.Resolve`, which is the
   single place that knows the Form 1/2/3 mapping.
9. **Nothing material leaves the Dream.** Knowledge, perception, Attunement and state only.
10. **Keep game logic in managers, not in UI.** It is what makes `--verify-slice` able to test the
    whole game headlessly, and that harness is the project's best asset.
11. **Severing is the only thing that removes regeneration or the Resolve drain.** No trap, item,
    ward or consumable does either, in whole or in part. A cheap Waking-world substitute for the
    anchor's effects — a "poor man's sever" — turns the Dream from *the* way you weaken a boss
    into a convenience, and takes invariant 1 with it. `combat.md` §4 records the salt line being
    struck for exactly this.
12. **Defence is fixed per unit, and moderate.** Severing does not change it; the right equipment
    type is meant to be clearly *better*, never the only thing that functions. Absolutes read as
    locks and invite key-hunting, and they turn a missing answer into a dead save. `combat.md` §7.
13. **Never trust a signal payload as current state.** `EventBus` emission is synchronous and in
    subscription order, so a subscriber ahead of you can act on the signal — including navigating
    again — before you are reached, which delivers the newer event first and leaves the stale one
    to land last. Anything caching per-signal state must re-check the manager
    (`CurrentRoomId != roomId → return`). This is a code rule, not a design one, but it earns its
    place here: breaking it is what produced the ejection failure, and M4 adds three more bus subscribers.

> **Revised 2026-07-27:** the old rule *"Resolve never moves in the Waking world"* is gone.
> Resolve now drains in boss fights, but **only while the boss's tether is unbroken** — sever it
> and the same fight costs none (`Corrupted` drains, `Severed` does not). The bar stays unambiguous
> because empty means the same thing in both realms — *you are removed from the confrontation.*
> See `combat.md` §6.

---

## 7. Where things live

| | |
|---|---|
| `README.md` | architecture, **the commands**, the state × realm table, and the EventBus stale-payload contract (invariant 13) |
| `docs/combat.md` | **source of truth** for combat, Resolve, the Dream, presentation and camps. Supersedes the GDD in several places. §15–17 are the newest. |
| `docs/handoff.md` | this file |
| `src/Tools/SliceVerifier.cs` | what "working" means, as executable assertions — and see "Placeholders and traps" for what it does *not* cover |
| `data/recipes/` | `RecipeData` `.tres` files. Hand-authored, and gated — a malformed typed array loads as empty with no error. |
| `src/Tools/ContentSeeder.cs` | wrote the starting `data/*.tres`. Retired — **overwrites**, so never run it now that rooms are edited in the inspector. Harmless as it sits; it only runs on an explicit `--seed-content`. |

Code comments cite handoff **section names**, not numbers — numbers have churned twice now.

### Superseded parts of the GDD

Do not re-derive these from the design doc:

- The **Snap duel** is deleted. Resolve-empty and being-touched are the same ejection.
- **Sanity** is gone as both a concept and a name; it is **Resolve**, and Vitality is separate.
- The Pixel Realm is **stealth and theft**, not a tactical duel.
- **"1st-Person"** means Myst-style perspective in the Waking world. The Dream is going 2.5D.
