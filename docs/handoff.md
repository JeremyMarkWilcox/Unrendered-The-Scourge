# Handoff

**What is live, what is next, and the reference a session needs on the way in.** Nothing dated lives
here any more. On 2026-08-04 this file was **44,266 words** of accumulated session write-ups, and the
current status sat at line 1,625 under a heading reading *"Next session — M4, camps and crafting"* —
three units after M4 shipped. The narrative was **cut rather than moved to a `history.md`**, because
`docs/` is tracked and the pre-cut text is one command away (`git show HEAD:docs/handoff.md`), whereas a
hand-maintained history file is a copy of what version control already does, that nobody re-reads.

**Where the cut content went.** The **lessons** were harvested into the memory store, which is where they
belong — a lesson is a reason, and reasons do not drift. The **rulings** were already in
`docs/build-plan.md`, `docs/combat.md` and `docs/items.md`, which are versioned and can be checked against
the code. What was neither — narration of work already visible in `src/` and in the gate suite — was
dropped.

**§3's essay on the blind-spot classes was cut as a duplicate and then put back, and the reason is worth
knowing before anyone cuts it again.** It *is* a near-verbatim copy of a memory file, so it looked like the
clearest case in the pass. But **14 comments across 6 source files cite those classes by number**
(`GameConfig`, `Main`, `MendManager`, `SliceVerifier`, `FightPanel`, `InventoryPanel`), and the memory store
is **not in the repo** — it is not versioned, it is not visible in the editor, and a `.cs` comment cannot
point at it. So of the two copies, the repo's is the one that cannot go and the memory's is the derivative.
The general form: **before deleting a doc section, grep `src/` for it.** A section with citations in code is
an interface, whatever it reads like.

**The rule this file is kept by, and it is a discipline rather than a structure: closing a question means
deleting the question.** An answered question left in place with its old diagnosis still attached is how
the file grew the first time — and how, on the day of the cut, a question ruled that morning was still
being described here as open. Open questions are therefore the **first** section, where a stale entry is
conspicuous instead of buried.

Section **names** are stable and code comments cite them; the numbers are cosmetic and have churned three
times.

---

## Now

**Last verified: `ALL GATES PASSED (860 checks)`, exit 0, `dotnet build` clean with 0 warnings — and
uncommitted.** Every pass is left verified for Jeremy to commit; nothing here commits.

**The crossing and the verb economy have both been played, 2026-08-04.** `F10` in and out of the probe
pocket, then all three verbs on `Q` / `Space` / `E` draining the bar at their own rates. That is the first
thing *play* has confirmed about the Dream rather than the gates, and it retires steps 1 and the crossing
from the outstanding-risk list. **The verbs cost but do nothing** — floating, dousing the light and pushing
it out are movement and rendering, which are Jeremy's 3D pass; the hook they read is
`DreamVerbs.IsHolding`. Still unplayed: the exit, the anchor, gates and creatures, in `build-plan.md` §8's
ladder order.

**What is actually on disk, and this is the line to trust when a doc disagrees: 59 rooms, 11 entities,
12 blights, 30 fights, 3 camps, 2 field stations, 27 items, 17 recipes, 1 pocket.** Rooms, items, recipes,
entities **and pockets** are each counted against the files on disk as well as against what got loaded, so a
parse failure or an unplaced file cannot hide behind a green run. Heists are asserted at zero. **That closes
the fifth blind-spot class for every collection the game has** — and pockets got the check in the sitting that
created the directory, rather than after the first silent failure.

**The one pocket is a reference implementation, not content.** `data/pockets/dream_probe.tres` points at the
3D probe and grants nothing, so it consumes none of the eight max-Resolve budget. **`F10`** enters or
leaves it — not `F4`, which the probe scene already binds to its own readout toggle.

### What just landed

- **Unit 9 — the verbs have keys. 844 → 860 checks.** `DreamVerbs` shipped as *the cost with no button*, so
  nothing in the game could hold a verb: **`src/Dream/DreamVerbHold.cs`** polls three rebindable actions and
  turns them into holds — `Q` / `Space` / `E` for Suppress, Float and Flare — and **`PocketHost` attaches it**,
  so no pocket scene has to carry anything. Plus **`F11`**, which grants the two grantable verbs so a
  `PocketGate` can be walked through before any boss pocket exists.
  - **It was shipped as a node to drop into each scene, and that was wrong within the hour.** Jeremy pressed
    `F11`, got *"learned 2 Dream verbs"*, and no key did anything — because the grant lives in `Main` and the
    polling lived in a node nobody had added. **A thing every pocket must have, that takes no configuration
    and has no position, is not authoring — it is wiring, and wiring that can be forgotten fourteen times
    will be.** It moved into the host and the checklist item became *"nothing to add"*.
  - **It also opened the last live node the gates could not see.** `SliceVerifier.Run` now takes the
    `PocketHost` alongside the `RoomView` and `Hud`, with `GateHarness` asserting it arrived. The suite drives
    `PocketManager`, which owns *being in a pocket*; it had no handle on the thing that owns *what is in the
    pocket with you* — §3's second class exactly, one step downstream of the manager state the gates assert.
  - **It polls rather than handling events**, because `_UnhandledInput` is not reliable inside a
    `SubViewport` and a pocket is always inside one — the reason `DreamProbeWalker` reads WASD the same way.
  - **It keeps no record of what is held** and asks `DreamVerbs.IsHolding` every frame, so there is no second
    copy to disagree with the first. That is load-bearing rather than tidy: `PocketManager.Return` calls
    `ReleaseAll` on the way out, so a cached set would survive the release and the key would be dead on the
    next crossing until you let go of a button you were already holding.
  - **Four break-tests; two predicted exactly, one over by a cascade, and one caught a defect in my own
    check.** The first version of *"picks the hold up rather than waiting for a release"* pressed the key
    **outside** the pocket — which never enters a cached set either, so it passed against both
    implementations. The fixture now presses inside and never releases, and the cached-set break reddens it
    alone. **Planning the break-test is what found it; running the check never would have.**

- **Unit 8 — item loss is gone, and both empty bars do one thing. 826 → 844 checks.** The first half was a
  **live game-breaker**: `SurvivesDeath` keyed on `ItemCategory`, and `herb_graveflower` and `mat_cairnstone`
  do not serialise one — so both fell to `Component`, both burned, and **dying while carrying Graveflower
  permanently deleted the good ending.** Twelve exist in the whole game, they never re-arm, the engine costs
  eight. Found by checking a claim rather than by anything failing: Jeremy thought `LoseUnprotectedItems` was
  deprecated, and it was worse than that.
- **Both bars now share `RespawnAtCamp`** — `ResolveDepleted` mirrors `VitalityDepleted`, which also closes a
  hole unit 6b left open (deleting the heist removed the only listener, so an empty nerve did *nothing* for a
  unit). Inside a pocket the ejection happens first and writes nothing, so the entrance survives. **This
  revises `combat.md` §6**, which had a boss fight's empty bar mean forced flight.
- **The Dream Resolve economy.** `DreamVerbs.BeginHold` / `EndHold` for the 3D layer to call — all three verbs
  drain while held and the rate is a **sum**, because §19 is explicit that floating is paid for out of the
  hiding. Plus `DreamCreatureData` + a `DreamCreature` node: contact costs a chunk of Resolve rather than
  health, with a **cooldown**, without which one touch empties the meter in a handful of frames. A boss's
  `Banishes` is the same call as running out.

- **The Dream verbs are wired** — `DreamVerb { Suppress, Float, Flare }`, `PocketData.GrantsAbility`, and
  `PocketManager.Holds` as the one place anything asks whether a verb is held. Plus **three reusable nodes**
  (`PocketAnchor`, `PocketExit`, `PocketGate`) so a cloned pocket scene needs no scripting at all. **826
  checks.** The ruling that made it possible: *pulling the anchor* teaches the verb, not the fight afterwards.
- **`--verify-content`, the bill checker** — `build-plan.md` §6, proposed 2026-08-01 and built today.
  `godot --headless --path . -- --verify-content` prints the bill from `placement.md` §1 against what is on
  disk and **always exits 0**, because a bill is a to-do list while a chapter is being built. It also names
  the rules it *cannot* check (1, 7, 8) and the ones already gated in the suite, so nothing reads as
  unchecked by omission. **First run: 11 rows still to author, 0 disagreeing with the bill.**

- **Unit 6b — the heist is retired, 843 → 810 checks.** `DreamRunManager`, `DreamHeistData`,
  `data/heists/`, `HeistDataDir`, `HotspotData.HeistId`, `HotspotKind.TakeAnchor`, `HeistOutcome`, the heist
  signals, `PursuerProximityChanged`, the dread label, the heist drain lookup and `SaveService`'s stranding
  guard are gone. **`InsideTheDream` lost one clause and the seven guards reading it were not edited** — the
  whole payoff of adding it in 6a. 33 checks retired, every one of them *about* the heist.
- **I called 6b "pure deletion" and it was not.** Five surviving invariants had no probe outside a
  heist-driven block — the realm lock, the HUD's dim trio, the pool-order restore, Vitality-never-moves, and
  **all four assertions of `RoomView.DisplayedRoomId`**, which are the entire regression cover for the
  ejection post-mortem. Deleting first would have removed it silently. 13 pocket-side probes went in first
  and were break-tested before anything was removed. Details in `build-plan.md` §4's 6b callout.
- **Unit 6a — the way into the Dream, 758 → 830 checks.** A `CrossToDream` hotspot crosses you into a 3D
  scene and back; the entrance is **scenery, never an item**, and it breaks once taken (two hotspots on one
  flag, opposite `RequiredFlagValue`s — no new field). A pocket scene makes two calls back: `Complete()`
  grants and flags, `Leave()` writes nothing. Quiet — no pursuer, no creatures, no drain. Details in
  `build-plan.md` §4's 6a callout; the design reasoning in `combat.md` §15.
- **Max Resolve can rise, which it could not before.** `ResolveManager.Max` was `=> GameConfig.ResolveMax`, a
  **const** — so §6 rule 6's eight knowledge-pocket grants had no mechanism at all, and *a rule about a
  number nothing can change reads as satisfied.* Now a field with `RaiseMax`, persisted as
  `SaveGame.ResolveMax`, and the import runs **before** the bar is restored or a raised ceiling would be
  trimmed on every load.
- **The seam was mostly already built, in the probe.** `DreamProbe.tscn` is a `Control` wrapping a full-rect
  `SubViewportContainer` with the monochrome shader on the container — so the Dream greys, the HUD over it
  does not, and a pocket drops in beside `RoomView` with **no scene change**, so autoloads and every panel's
  state survive a crossing.
- **Unit 5 — the return-to-camp verb, 687 → 758 checks.** `C` opens a menu of the camps you have stood in;
  a row *is* the verb. Refused **inside a fight** and **inside a pocket**, and the fight refusal is gated as
  a pair with *"the fight is still running"*, because the arrival backstop would otherwise make a deleted
  guard look fine. It needed one ruling — **you choose the destination** rather than being returned to the
  newest camp — and the reason is that the smaller reading contradicts §20's revised good ending. Full
  reasoning in `combat.md` §17; the shape of the work in `build-plan.md` §4.
- **The citation gate**, riding along as agreed. It walks `src/` for references to this file's sections and
  asserts each resolves. **It went red on its first run, on its own doc comment** — which had spelled the
  citation pattern out literally instead of describing it. A gate that reads source reads its own source.
- **The entity census**, the optional companion, and it was free. Creatures have no registry at all — an
  `EntityData` reaches the game only by being referenced from a placement — so one authored and never placed
  was invisible to every check that walks rooms. Its break-test moved **exactly one check out of 758**, which
  is the cleanest evidence available that a hole was real rather than already covered.
- **The equipment-gating question is ruled** — `build-plan.md` §7 **decision 9**, 2026-08-04. Equipment
  may gate content in the chapter before the chapter that makes it. The measurement that settled it:
  **one anvil in the game, in `ch2_smithy`, and one mortar, in `ch2_apothecary`.** Consequences worth
  knowing before authoring anything in chapter 1 — it can craft **the six traps and nothing else**, and
  it is **sleep-only for healing**, because both salves are mortar recipes.

> **Two lessons from unit 5's break-tests, both about the gates rather than the game.** A check asserting
> *"the run is still live, so the refusal was not an ejection"* stayed green with the pocket guard removed —
> §3's **seventh** class, and it was **deleted rather than repaired**, because the harm it aimed at was
> already covered by the check above it. And stripping a camp's exits was predicted to redden one check and
> reddened six: `GateWorldGraph`, `GateMending`, `GateFights`, `GateStations` and `GateRoster` are all called
> *from inside* `GateAuthoredContent`, so a fixture injected there is visible to them. Worth knowing before
> injecting one — the run order inside that gate is not what its position in `RunGates` suggests.

### What is next

**The remaining work was re-cut into two lanes on 2026-08-04 — `build-plan.md` §8's *"What remains"* is the
schedule, and this is the summary.** The tail of that table had become four coarse rows whose first entry
bundled Jeremy's 3D pass with Claude's code into one undefined unit. The lanes touch different files, so
they run in parallel: **Jeremy builds pocket 1 and plays it**, **Claude builds the crafting panel**.

**Chapter 1 pass 2 — the vertical slice.** Walk in, fight, take the pocket, mend the blight, cleanse the
screen, camp. **Every listed beat is reachable**: camp landed with unit 5, the pocket with 6a, and 6b left
the Dream with exactly one model rather than two. What remains is authored content plus **Jeremy's own 3D
pass**, which is a separate workflow by his own framing — *"3D dream world is its own project."*

**But the bench you camp at does nothing.** `EventBus.CraftingRequested` fires from
`InteractionService.cs:135` **with nothing subscribed**, and `src/UI/` holds no `CraftingPanel.cs` — the
panel does not exist in any form, not as an unauthored `.tscn` but as no code at all. Four documents
describe it as an editor-pass detail; it is the one thing standing between chapter 1 and being playable,
since the chapter holds six trap recipes and has **no healing but camp rest** (decision 9). It is now
**unit 7a**, and it moved ahead of sealing chapter 1.

**Two decisions closed with the re-cut, and both cost nothing to build** — `build-plan.md` §7. **Decision
10:** a major repair is gated on the boss flag, not a key item, and the *puzzle* is what makes it major —
which the ch1 and ch2 ledgers had already drawn (*"Gated on the boss memory"*) and nobody had read back.
**Decision 11:** a blueprint page is an item whose id **is** the blueprint id, so the page is a view of
the knowledge rather than a second copy that could disagree; the Lore tab in the satchel is what it is
for, and it is checkable anywhere while crafting needs a venue.

**And `--verify-content` now answers "what is chapter 1 short of" without anyone guessing.** Its first run
says everything outstanding traces to **two things — the three bosses and the fourteen pockets**: the major
repairs want a reclaimed memory, the eight max-Resolve grants want knowledge pockets, and most of the twelve
missing items are the lore pages those pockets hand out. Eleven rows, not eleven independent jobs.

**The one thing no content can do yet: nothing grants a blueprint.** `Main.LearnEveryCraftFamily()` (`F6`) is
still the only way to open any of the 16 gated recipes. The mechanism that will grant them is built and gated
— a `Knowledge` pocket's `Complete()` calls `Discover` and `RaiseMax` — it simply has nothing to grant until a
real knowledge pocket is authored. Authoring the first one retires `F6` and `F10` together.

**What a pocket needs, as a checklist — and it needs no scripting as of 2026-08-04:**

1. A scene shaped like `DreamProbe.tscn` — a `Control` wrapping a full-rect `SubViewportContainer`.
2. A **`PocketAnchor`** node on an `Area3D`. Reaching it finishes the pocket. That is the placeholder anchor.
3. A **`PocketExit`** node on an `Area3D`. Walking into it leaves, writing nothing. **Every pocket wants one** —
   without it the only way out is finishing, and the Dream has no death to fall back on, so *"I'll come back
   when I can Float"* becomes *"I am stuck in here."*
4. Optionally **`PocketGate`** nodes, with `Requires = Float` or `Flare`, on anything a verb should gate.
4a. **Nothing, for the verbs.** `Q` / `Space` / `E` work in every pocket because `PocketHost` attaches the
   poller — there is no node to add and nothing to forget. *(This checklist briefly said to add a
   `DreamVerbHold` yourself; the first pocket played had no verbs because nobody did.)*
4b. Optionally **`DreamCreature`** nodes, each pointing at a `DreamCreatureData` `.tres`. Minor ones cost a
   chunk of Resolve on contact and pursue only while your light is on; a boss's `Banishes` throws you out. The
   **cooldown** is the field that matters — how aggressively anything chases is your pass, not the node's.
5. A `PocketData` `.tres` in `data/pockets/` naming the scene — **after** the scene exists, or the ScenePath
   crossing check goes red.
6. A `CrossToDream` hotspot on a Waking screen naming the pocket, paired with a second `Examine` on
   `pocket.<id>.cleared` as the broken remnant.

**Those three nodes exist so a clone carries no logic.** Fourteen scenes each holding their own call to
`Complete()` would be fourteen copies that drift — and cloning is exactly how they would drift, silently and
identically, which the 59 generated rooms already paid for once. Build one pocket, **play it**, then clone.

**The verbs are `Suppress` / `Float` / `Flare`, and only two are ever granted.** Suppress is held from the
first frame and carries no flag; Float and Flare come from pulling the anchors of boss pockets 1 and 2. Boss
pocket 3 grants nothing. And the rule that cannot be gated: **every boss pocket must be completable with the
verbs you already hold when you get there** — pocket 1 with Suppress alone, since Float is what is inside it.

**The eight max-Resolve grants are a hard budget.** One per knowledge pocket, nothing else in the game may
grant a point, and `data/pockets/dream_probe.tres` deliberately grants zero so the reference pocket spends
none of it.

**Story, narration and tutorial content are dead last** — behind pass 3, beside audio, ruled 2026-08-04.
Two notes are parked in `build-plan.md` §8's **last** row and neither is work queued early: `RecipeData.CraftNarration`
with somewhere to draw it, and the equip tutorial the deliberately-unequipped starting axe exists to be
the subject of. An `[Export]` with a default does not invalidate existing `.tres` files, so **neither
needs building now** — the field can arrive with the thing that reads it.

**The outstanding risk is that none of it has been played.** Unit 3a shipped a fight loop with 655 green
gates and a defect no gate could see, because every check in that section stands still in one room. The
gates prove structure; they have never proved playability.

**Unit 5 is the first unit that was played on the day it landed, and the thing play found was not in unit
5.** Jeremy: *"the issue I found was with the 'I' satchel opening during combat. When you pressed 'C' it
refused to open, with a text saying so."* The camp verb was right; **the satchel had no fight guard**, and
`C`'s behaviour was the model he wanted copied. Two keys behaving differently is far easier to notice than
one key behaving wrongly — **which is the argument for playing every unit, not just the risky ones: a new
verb audits the old ones by standing next to them.**

**The cosmetic half was the smaller half.** `InventoryCraftingManager.UseItem` had no fight guard at all, so
the satchel's Use button healed **for free — no turn spent, nothing counted against `SalveSlotsPerFight`.**
A second currency inside a fight, behind a key, for a unit and a half. Fixed as `UseBlock.InFight` plus an
`asFightTurn` flag only `Treat` passes, defaulting to the refusing direction; the satchel also refuses to
open mid-fight, which is what was asked for. Full reasoning in `combat.md` §19's callout. **The trap that
hid it:** `Treat` routes through `UseItem` deliberately so *"nothing to treat"* cannot drift between them,
which reads as though the two paths are equivalent — but joining two callers so a rule cannot drift says
nothing about a rule that should apply to only one of them.

**A second defect was found while chasing the first and is kept:** a fight starting while the camp menu was
open left the menu behind the fight screen. That guard had been considered during the build and **declined**
on the reasoning that the menu covers the whole screen. The coverage claim is true and is now asserted; the
reasoning was wrong. Worth writing down because §3's rule against unreachable dead-latch guards is what made
declining feel principled — **that rule is about a guard with no reachable arm, not about a state you failed
to enumerate**, and confusing the two errs toward leaving a bug in.

**The diagnosis is the transferable part: it took one run instead of an argument.** Two candidate causes were
written as **two separate checks** and the suite said which. 749 → 758, and each fix break-tests to its own
checks — the panel guard and the manager guard move different ones, which is how you know they are two fixes
and not one written twice.

`CampPanel` still has no authored scene, so what is on screen is `BuildFallback`'s code-built tree. Its
*behaviour* is driven headlessly — it opens, lists, refuses, and its rows are pressed by firing the signal,
because a `Pressed` connected from C# is not introspectable from C# — and the tint is asserted by anchors
and offsets rather than by `Size`, since a headless run lays nothing out. **So the arrangement is checked
and the appearance is not**, which is exactly how a dim sat at 0×0 for a whole unit. It has been looked at
now and it reads as a menu — but it is a placeholder tree, and `scenes/ui/CampPanel.tscn` authored against
the contract in §2 is what retires the builder.

---

## 1. Open questions, in the order they'll bite

Answered questions are **deleted from this list**, not struck through — the answer lives in
`docs/build-plan.md` §7, `docs/combat.md` or `docs/items.md`, and a closed question left here is what
made the previous version of this file untrustworthy.

1. **The entry puzzle** is a placeholder, and is the first real instance of Waking-world puzzle content —
   so it sets the grammar for every puzzle after it. What a pocket entrance *is* belongs to unit 6.
2. **Is the pure-BFS pursuer fair?** Never tested against a real player. Much smaller problem now that it
   only runs inside boss pockets — and the "level mechanics that slow the pursuer down" in `combat.md`
   §15's boss pockets is a better answer than tuning the BFS would be.
3. **Does Fire belong to Craft or to Physick?** §18 builds against three Craft types and Caustic alone on
   her side, because the asymmetry matches §8's "few of her notes, far apart". Moving Fire across makes it
   two and two and is a one-line change; recorded so the split is not mistaken for an accident.
4. **Does the fight need a defensive verb?** A shield is deferred, not rejected, and the version worth
   building is *brace* — spend a turn, take much less. Unanswerable until three fights have been played,
   and safe to defer because slots are an enum and an item declares its own.

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

- ~~**Claude never runs `--seed-content`**~~ — **deleted 2026-08-01, rule and tool together.** The
  rule was right and it was the weaker of the two available fixes: a discipline where a deletion
  would do. The thing it protected — *`data/` is Jeremy's work* — is unchanged and now holds against
  a mistake as well as against a habit.
- **Claude never hand-edits a `.tres` typed array** without a gate that asserts the contents
  loaded. A malformed `Array[...]` line yields an empty collection and no error. The recipe added
  in Phase 1 is gated exactly this way.
- **Run the gates unconditionally; add gates sparingly.** Settled 2026-07-30. Measured on
  Jeremy's machine: a full run is **0.57 s** at 188 checks and has not moved measurably at 328, an
  incremental build 2.5 s, so the whole
  edit→verified loop is ~3 s and it sits in Claude's turn, not Jeremy's authoring loop.
  ~~skip only for docs, which cannot fail a gate~~ — **amended 2026-08-04: docs can now fail a gate.**
  The citation gate walks `src/` for references to this file's sections, so **renaming or deleting a
  section here reddens the suite**, and a docs-only pass that touches a cited heading has to be verified
  like code. Adding to a section, renumbering an uncited list, or editing prose still cannot fail
  anything. The clause was written on 2026-07-30 and the gate arrived on 2026-08-04, which is the ordinary
  way a standing rule goes quietly out of date. Deciding
  whether a change is "worth checking" costs more than checking — and gets it wrong precisely on
  the changes that matter, because the ones that break distant things are the ones that don't look
  like they could. The live example: moving `GateCraftingAndUse` later in the run was pure test
  ordering, and it broke the **save round-trip** gate, because crafting consumed reagents a later
  gate expected to still exist. So: run on any code or `.tres` change, batched by *coherent unit*
  rather than per file-write; skip only for docs, which cannot fail a gate. The selectivity belongs
  on the other side — a **new** gate costs a break-test to prove it isn't tautological, so write one
  only when something actually broke or a new invariant becomes load-bearing.

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

**`scenes/ui/CampPanel.tscn`** — root `Control`, `Visible = false`. **Unit 5 built this one in code first**,
so unlike the two above there is a working tree to look at: `CampPanel.BuildFallback` is the contract,
instantiated. Authoring the scene against these names deletes the builder.

| `%UniqueName` | Type | Script does |
|---|---|---|
| `%CampList` | any `Container` | one row per camp you have stood in; emptied and rebuilt on every open |
| `%CampTemplate` | `Button`, a child of the list, `Visible = false` | duplicated per camp. **The row is the verb** — pressing it travels, and there is deliberately no *Travel* button beside the list, because a destination picked and then confirmed is a confirmation step with one answer (§17: *if the player would never decline, do not ask*) |
| `%CloseButton` | `Button` | hides the panel. Unlike the fight screen's ✕ this really is just dismiss — leaving a menu costs nothing |
| `%Heading` | `Label` | *optional.* How many camps. Leave it out and nothing complains |
| `%Dim` | `ColorRect` | *optional.* The tint over the room. Absent, the script builds one at `InventoryPanel.tscn`'s 55% black — so author it if you want a different value, and **anchors *and* offsets**, per the eighth trap below |

The row you are standing in is drawn **disabled with its reason appended**, not omitted: a list that hid your
own camp would change length as you walked, and *"you are already here"* is a statement rather than something
withheld. The reason text comes from `CampManager.Words`, so the panel and the verb speak one vocabulary.

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
  made, and it is much cheaper to honour now than to retrofit.

Input actions `inventory` (`I`) and `journal` (`J`) already exist in `project.godot`, unused.

~~**The conversation to have at the end of Phase 2:** the item, trap and equipment-type
vocabulary.~~ **Had on 2026-07-30, and settled — `combat.md` §18.** The prediction in this paragraph
was right about *who* it would block (Jeremy, before anything else) and wrong about *when*: it was
had before the crafting panel rather than after, because the vocabulary turned out to depend on the
combat grammar and not on having an inventory grid to look at. What replaces it as the thing standing
between Jeremy and authoring items is now code, not a decision — the data shapes in §18's final
table.

---
## 3. Placeholders and traps for the unwary

Things that *work* but are not the real implementation, or that will mislead you:

| Where | What |
|---|---|
| `SliceVerifier.Run(view, hud)` | Sets `ResolveManager.Active = false` so drain cannot race the assertions. **Nothing timing- or drain-related is tested.** Both arguments are null-tolerant so the gates stay runnable from anywhere — but `GateHarness` asserts they arrived, so a wiring mistake in `Main` fails the run instead of silently skipping the five view checks. |
| `verify_`-prefixed rooms | `RegisterVerifyRooms` injects them into `NavigationManager` at the top of a run, so behaviour is tested against ground the verifier owns rather than against whatever Jeremy authored. They are **not** in `data/`, they carry `ChapterId = "verify"`, and `GateAuthoredContent` skips them **by key prefix** — which is why the `ch*` predicates on five sweeps are belt-and-braces rather than the thing doing the work. **Any new gate whose meaning depends on a room's contents belongs on a `verify_` room**, and anything asserting "the game has content" must keep excluding them, or a synthetic room will satisfy a check about authored work. As of unit 3b there are nine: `verify_camp`, `verify_camp_two`, `verify_wilds`, `verify_arena` (3a's eight placements, and its bodies must stay Corrupted for `GateEncounters`), `verify_loop` / `verify_loop_inner` (the retired waystation loop's shape), two chained synthetic Dream rooms plus a code-built `DreamHeistData`, and `verify_station` — a mortar with **no bench under it**, which is the only probe that the crafting venue guard exists. |
| `GateHeistMechanics` | Calls `run.ForceClear()` before `GatePersistence`, so save/load is only tested from a Waking room with no run. This is exactly where the save/load soft lock hid. |
| headless runs | `_Process` never ticks meaningfully, so the pursuer only moves when a test calls `DebugSetPursuerRoom`. **Pursuit timing is untested.** |
| the view, in gates | `RoomView` and `Hud` *are* live in a headless run and now have gates asserting the room they display — but only the room. Backdrops, overlays, tweens and hotspot rects are still unasserted, and `--shots` is the only thing that looks at a pixel. |
| ~~`yard_cairn`, in `data/rooms/waystation_yard.tres`~~ | **Gone 2026-08-03 with unit 3b.** The prototype's entry puzzle was a plain `Interact` hotspot ("search the cairn") and the minigame never existed. Its *shape* survives as a once-firing `Interact` on `verify_loop`, because that is what the migrated M3 checks probe; what a pocket entrance actually is belongs to unit 6. *(Cited `ContentSeeder.cs:219` until that file was deleted 2026-08-01, then the authored `.tres`, and now neither.)* |
| `InteractionService.cs:136` | Finishing a `Severed` body is **one click**. This is where the turn-based item fight goes. |
| ~~**No camp is authored**~~ | **Closed 2026-07-30.** `shed_bench` in `waystation_shed.tres` went from `Kind = 4` (Interact) to `Kind = 7` (Workbench) — a one-value edit to a hotspot that was already authored, labelled and positioned. Crafting, the cache and the respawn destination are live. Two gates now hold it: a camp exists, and it is walkable to from `StartingRoomId`. |
| `ItemData.VitalityRestore` | Read at last, by `UseItem`. Only salves use it. |
| `EntityData.CorruptionAura` | **Declared, defaulted to `0.15f`, and read by nothing.** `ResolveManager.CurrentRatePerSecond()` returns `DreamVerbs.RatePerSecond()` and nothing else, so **no fight drains Resolve** — the boss aura is unbuilt, and the two checks 6b retired as universals over an empty set come back with it. Found in the 2026-08-04 re-cut. |
| `RecipeData.RequiresBothTracks` | **Same class: gated as a *shape*, read by no manager**, so a both-tracks recipe behaves like any other today. Exactly one customer — the exorcism engine, which is the unauthored 18th recipe. This is the whole of unit 7b. |
| `PocketGate` | **It reads *have you been taught the verb*, evaluated once when the pocket loads — not *is the key down*.** So a Flare wall becomes permanently visible the moment Flare is learned, and a Suppress gate is **always open** (that verb carries no flag), which makes it a node that can never do anything. Neither matches its own first line, *"a wall that only Flare shows you"*. **Deliberately not changed** — 2026-08-04, Jeremy: *"I rather leave the internal working of the 3D open so that I have more freedom."* Switching it to `DreamVerbs.IsHolding` is a ten-line change to make when a real pocket wants it, and picking now would narrow the 3D side before anything has been built against it. |
| `InventoryPanel`'s Lore tab | Built (`InventoryPanel.cs:451`) and an **empty shelf** — no `lore_*` item exists on disk. Decision 11's 11 pages are what fill it; until then it is a tab over nothing. |
| Everywhere | **No audio at all.** Deliberately last — but see the note in `combat.md` §14. |
| Everywhere | Craft / Physick / Attunement are designed, unbuilt. |
| Everywhere | **Defence will never exist as a field.** It was decided on 2026-07-27 to be fixed per unit; `combat.md` §18 then **deleted the number entirely** on 2026-07-30 in favour of a vulnerability and a resistance per creature (right type 2×, wrong type 0.5×). So do not add a `Defence` property, and do not reach for `CorruptionAura` either — that carries regen and the Resolve drain. |
| Everywhere | Traps are designed in detail, unbuilt. They need combat to resolve into. ~~and the type vocabulary before they can be authored~~ ~~what is missing is `ItemCategory.Trap` and the fields beside it~~ — **both closed 2026-07-30.** The vocabulary is settled (`combat.md` §18) and the fields exist, so a trap can be *authored* now; what it still cannot do is *fire*, which is the encounter loop. |
| Everywhere | ~~**No combat data shapes yet.**~~ **Built 2026-07-30.** `DamageType`, `EquipSlot`, `KnowledgeTrack`, `ItemCategory.Trap`/`.Equipment`, the combat `[ExportGroup]`s on `EntityData` and `ItemData`, and `GameConfig.RigSlotsPerFight`. **Item and creature authoring is open.** But the fields are *shapes only* — nothing reads them yet, so authoring a weapon does not make it swingable. Six content-sweep gates cover them and all six are **dormant** (no authored content sets a combat field), so their green means nothing until you author something. |
| `RoomData.BaseCorruption` | **Not vestigial — an earlier entry here said it was, and that was wrong.** It no longer drives any drain (Waking rooms do not drain, Dream rates are per-heist), but `RoomView.cs:187` reads it as the placeholder backdrop's **fog density**: `fog: 0.35f + BaseCorruption * 0.65f`. Deleting it would visibly change every Waking room. Treat it as a look knob, not dead weight. |
| ~~`RoomData.IsSanctuary`~~ | **Deleted 2026-07-30, not renamed.** It was going to become `IsCamp`; instead camp-ness is derived from the workbench hotspot, because a flag and a bench that must always agree are two copies of one fact. If you find the name in an old comment, that is why it is gone. |
| `PlaceholderBackdrop`, `EntityOverlay` | All art is procedural. Every texture field is nullable and unset. |
| `project.godot` `[rendering]` | **`renderer/rendering_method="gl_compatibility"`.** Reassessed 2026-07-30 and **probably fine to leave alone** — Compatibility has no volumetric fog, SDFGI, SSAO/SSIL or SSR, but at 480×270 through a posterise pass almost none of that survives anyway. See "The Dream in 3D" step 0. The one reopen condition is **many small lights in one pocket**, since Compatibility caps lights per object, and the player-carried lighting model makes that less likely rather than more. If it is ever switched, switch it *before* placing lights — lighting does not transfer between backends. |


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

**And then it was written, hours later, once the bench existed.** The rule did not bend — the same
sentence is a to-do list before the content lands and an invariant after, and the deciding factor is
*when you write it*, never what it says. The lesson worth carrying is the second-order one: **a
rejected gate has an expiry date nobody writes down.** Re-examine it the moment the thing it would
have complained about is done. Same shape as the item-vocabulary deferral that outlived its reason.

The pair that went in is instructive about *which* half was worth having. "A camp is authored" is the
obvious one and the weaker one; "and you can walk to one from where the game starts" is the one that
earns its place, because a bench in a room with no door into it would remove crafting from the entire
game while every other camp gate stayed green on its synthetic `verify_` rooms. Break-tested both
ways: severing the bench reddens both, severing only the yard→shed door reddens the second alone.

**A fifth, and the nastiest so far, found generating the 59 skeleton rooms on 2026-08-02: content that
never reached the registry is invisible to every gate that reads the registry.** The first generated
batch had one malformed line in its typed-array declaration, and all 59 files failed to parse. Nothing
turned red. The suite printed the same 359 it had printed before the files existed, because `GD.Load`
returns null, `LoadRoomRegistry` pushes a warning into a headless console with three thousand lines of
Godot output in it, and **every content sweep in `SliceVerifier` begins by enumerating
`NavigationManager.Rooms`**. Fifty-nine rooms of nothing is indistinguishable from fifty-nine rooms of
perfect content when your first act is to ask the registry what it holds.

The general shape is worth naming, because it is not specific to rooms: **a gate that enumerates a
collection can only ever check the members that got in.** Every "for each X, assert P(X)" is silently
conditional on X being loaded, and the louder and more numerous those checks are, the more convincing
the false green. The answer is one check per collection that does *not* consult the collection — here,
counting `.tres` files on disk with `DirAccess` and comparing to `rooms.Count`, which is the first line
of the `graph` section and the only thing in it that distrusts its own input.

**Every collection the game has now carries that check, as of unit 5** — rooms, items, recipes, entities,
and heists asserted at zero. The last one in was **entities, and it is a different shape worth knowing**:
there is no entity registry at all, so an `EntityData` reaches the game only by being referenced from a room
placement. The check therefore compares files on disk to the count of distinct entity *types* actually
placed, and what it catches is not a parse failure but a creature **authored and never put anywhere** — which
plays as an empty screen, indistinguishable from a screen nobody has authored yet. Its break-test moved
exactly one check out of 758: nothing else in the suite could see it.

There is a second, smaller lesson in the same failure. The batch was **generated**, so the defect was
uniform: all 59 or none. A hand-authored mistake would have hit one file and been caught by eye the next
time that room was opened. Generation converts small random errors into large systematic ones, which is
an argument for gating generated content *in the same sitting that generates it* — the `graph` section
went in beside the rooms, not after them, and it caught the second defect (`HotspotKind` misread, every
stub an `Interact` and every node an `Examine`) before anyone had to click a hotspot to find it.

**A sixth, found the same day moving the boot room, and it is about the harness rather than the gates:
a suite that hangs is not a suite that failed, and the two look identical from a terminal.** Twice in
one afternoon a broken fixture produced no result at all — an empty chapter spine reaching `.First()`,
then a mistyped room id leaving every downstream lookup null. Godot's C# layer logs the exception and
keeps the main loop alive, so `--verify-slice` sat there until a `timeout` killed it, and the useful
information (which check went red *before* the throw) never printed. There were two holes and both are
now shut: `SliceVerifier.Run` wraps the gate sequence in a `try`/`catch` that records the throw as a
failure named `[crash]` and falls through to the summary — so the run exits 1 in seconds with a short
count that says how far it got — and `Main.StartGame` calls `Quit(1)` instead of returning when the
starting room will not load under `--verify-slice`, since that path never reaches the verifier at all.

The reason to care is that this class *hides* the other five. Every rule above is about a green run
that should have been red; this one is about a run that is neither, and the honest reflex on seeing no
output is to assume the machine is slow. **Always run the verifier under a `timeout` and read the file
afterwards** (`> /tmp/vs.txt 2>&1`) — piping to `grep` has hung the terminal too. A short check count
in the summary now means "a gate threw", and is worth more attention than a long list of failures.

**A seventh, and it is the third class's shy cousin: a guard whose only reachable arm is covered by a
different guard, so deleting it changes nothing a green run can see.** Class three is a check that *cannot*
fail; this one is a check that fails for a reason other than the thing it is aimed at. It has shipped twice
and both were found by break-test rather than by reading:

- `InventoryCraftingManager.AtVenueFor` has two arms — `AtCamp`, which refuses inside a live run on its own,
  and `RoomHasStation`, which is the only arm the `IsRunning` guard is load-bearing on. **Every crafting
  check stood on the first arm**, so the guard could have been deleted and the suite stayed green. Found by
  a break-test that *predicted which checks would go red together* and then read the one that did not move.
  Fixed by `verify_station` — a mortar in a room with no bench under it.
- The fresh-game walk added on 2026-08-03 has three stopping rules (a barred door, a flag-gated door, a
  flag-array door). Commenting out **all three** took the frontier from 9 fights on 16 screens to 30 on 59
  and **every check stayed green**, because the nearest fight sits two ungated doors out either way. Fixed
  by two *differentials*: the walk is parameterised, and the suite asserts that ignoring each rule opens
  screens the rule was closing.

- Unit 5's camp verb asserted *"the run is still live, so the refusal was not an ejection"* after a refused
  travel inside a pocket. **It stayed green with the pocket guard deleted**, because a `DreamRunManager` run
  survives ordinary navigation to a Waking room — so the check could not distinguish a working guard from a
  missing one. **It was deleted rather than repaired**, and that is the part worth copying: the harm the
  guard prevents is *standing at a bench with a run live*, and the check beside it already asserted the
  player never got there. A check that still passes while measuring nothing is worse than one check fewer.

- Unit 6a wrote *"the bench is shut inside a pocket"* and *"a field station is shut too"* while standing on
  `verify_wilds` — **open ground, which holds neither a bench nor a station**, so both were true whether the
  pocket guard existed or not. A break-test predicted three reds and produced one. Each now stands on ground
  where the guard is the only thing that could refuse, **with a control on the same ground** proving it is
  otherwise open: at `verify_camp` the bench works, then does not; at `verify_station` the mortar works, then
  does not. The general form: *a refusal asserted where the thing being refused was never available is not a
  check.*

- The same unit **deleted** a check reading *"the fight you were in is still running"*, and this one has the
  sharpest lesson of the three. It was **copied from the camp verb**, where it is genuinely load-bearing:
  travel navigates, so a broken guard trips `EncounterManager.OnRoomEntered` and ends the fight, and a
  one-sided check would pass over a real defect. Entering a pocket does **not** navigate — that is the whole
  reason its guard exists — so the fight survives either way and the sentence measures nothing. **A check is
  not portable just because its words are true in both places.** What made it a probe was the navigation, not
  the phrasing, so ask what mechanism a copied check was relying on before trusting it in a new home.

**A tenth, from unit 8, and it is about the break-test rather than the gate: a break-test that reddens nothing
may be a broken break-test.** Reintroducing item loss was expected to redden the two checks that guard the
scarce nouns, and the run came back **green**. The instinct that follows — *the check is weak* — was wrong: the
break called `RemoveItem("herb_graveflower", 99)`, and `RemoveItem` **refuses when you do not hold that many**,
so it removed nothing and the code under test never changed. Retried with a quantity the satchel actually had
and both checks went red immediately.

That is the mirror of the third class. A check that cannot fail is dangerous because it passes; a break that
cannot break is dangerous because it *also* passes, and it retires a good check by making it look useless. So:
**a green break-test is a result to distrust, not to accept.** Confirm the break took effect — read the value
you changed, or make the break something whose absence would itself fail — before concluding anything about the
check.

The rule that catches it: **write down which checks you expect to go red together, then read the ones that
did not move.** A break-test that only asks "did something go red" cannot see this class at all — something
always does. And where a guard has arms, the fixture has to reach *each* arm; a fixture that satisfies the
easy arm makes the hard one decorative.

**A corollary, learned the same day and cheaper to read than to rediscover: a wrong prediction is also a
finding.** Stripping a camp's exits was predicted to redden one check and reddened six, because
`GateWorldGraph`, `GateMending`, `GateFights`, `GateStations` and `GateRoster` are all called *from inside*
`GateAuthoredContent` — so a fixture injected there is visible to them, which the order of calls in
`RunGates` does not suggest. The five extras were the honest blast radius of a disconnected map rather than
false alarms, but the mis-prediction is what surfaced the call structure.

**And an eighth, half a day later, which is smaller and purely mechanical: `SetAnchorsPreset` preserves the
node's current rect instead of filling the parent, so anchors without offsets is half a rect.** A fight-screen
dim sat behind four green checks having never drawn a pixel. Use `SetAnchorsAndOffsetsPreset`, and when a
check settles for *part* of an arrangement, assert all of it.

**A ninth, from unit 5, and it is the only one that is funny: a gate whose subject is the source tree
includes its own source.** The citation gate walks `src/` for references to this file's sections and asserts
each resolves. Its first run went red on **its own doc comment**, which had listed the citation forms it
matches as literal examples — so the method explaining the pattern was, correctly, read as citing a section
that does not exist. The fix is to *describe* the pattern rather than instantiate it.

The general form is worth more than the incident, because the same shape reaches anything that measures the
project as an artifact — line counts, TODO sweeps, naming conventions, a future `--verify-content`: **a check
that reads the repo will read the check.** So its own text is part of its input, and an example in a comment
is indistinguishable from the real thing. Two consequences: prefer a description to a specimen, and **never
believe such a gate before it asserts that its walk found something.** A regex that silently matches nothing
reports every citation resolving — which is the fifth class wearing the ninth's clothes, and the reason the
found-count is checked first.

**An eleventh, and it has now shipped three times: a field with no reader reads as satisfied.** Unit 6a
named it about one number — `ResolveManager.Max` was `=> GameConfig.ResolveMax`, a **const**, so §6 rule 6's
eight max-Resolve grants had no mechanism at all and *a rule about a number nothing can change looks obeyed*.
The 2026-08-04 re-cut found two more of exactly the same shape: **`EntityData.CorruptionAura`** is declared,
defaulted and read by nothing, so no fight drains Resolve; **`RecipeData.RequiresBothTracks`** is declared and
*gated*, but no manager consults it, so a both-tracks recipe behaves like any other.

The reason this class is hard to see is that the gates over it are honest. Six content sweeps assert things
about `CorruptionAura` and they all pass, because **a sweep over authored data checks the data, never the
behaviour** — and a shape with no reader has no behaviour to be wrong. The tell is not a red check, it is a
field you cannot name a caller for. **So when a unit lands a data shape ahead of the system that will read
it, write the reader's absence into "Placeholders and traps" the same day** — all three of these were
findable by `grep` and none was found by one for weeks.

---

## 4. Invariants — break these and the design collapses

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
4. **You cannot sever from the Waking world.** The anchor is only reachable by *travelling* into
   the pocket. Otherwise the relic and the whole heist are pointless. This used to read "spectral
   vision is a look, not a place" — `Tab` showed you Form 2 and the aura and refused to let you cut
   through them. The peek died on 2026-08-02; **the refusal is the part that mattered and it stands**,
   now enforced by there being no way to reach the anchor at all from out here.
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

## 6. Where things live

| | |
|---|---|
| `README.md` | architecture, **the commands**, the state × realm table, and the EventBus stale-payload contract (invariant 13) |
| `docs/combat.md` | **source of truth** for combat, Resolve, the Dream, presentation and camps. Supersedes the GDD in several places. §15–17 are the newest. |
| `docs/handoff.md` | this file |
| `src/Tools/SliceVerifier.cs` | what "working" means, as executable assertions — and see "Placeholders and traps" for what it does *not* cover |
| `data/recipes/` | `RecipeData` `.tres` files. Hand-authored, and gated — a malformed typed array loads as empty with no error. |
| ~~`src/Tools/ContentSeeder.cs`~~ | **Deleted 2026-08-01**, with `--seed-content` and the "Claude never runs it" rule. It wrote the starting `data/*.tres` and then overwrote them on every run. *"Harmless as it sits"* was true beside five rooms and stops being true beside ~58 (`build-plan.md` §6), and its payload was superseded content anyway — the rat, the 2D dream maze, the heist. The writer idiom returns in the ledger generator, **write-if-absent**. Original: `git show bb665ad:src/Tools/ContentSeeder.cs`. |

Code comments cite handoff **section names**, not numbers — numbers have churned twice now.

### Superseded parts of the GDD

Do not re-derive these from the design doc:

- The **Snap duel** is deleted. Resolve-empty and being-touched are the same ejection.
- **Sanity** is gone as both a concept and a name; it is **Resolve**, and Vitality is separate.
- The Pixel Realm is **stealth and theft**, not a tactical duel.
- **"1st-Person"** means two different things and both are first-person. The **Waking world** is
  first-person in a **2D** world — Myst-style, painted screens, click to move. The **Dream** is
  first-person in a **3D** world. Settled 2026-07-30 — **including the creatures, which are 3D models
  with no billboards.** That half read as "still open" in three documents for five days after it was
  closed, which is what this file's cut was for.
