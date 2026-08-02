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
| **12 blights, 15 jigsaws** | a Blight kind + a puzzle seam | ❌ **nothing.** The old `VitalityCost` hazard was a different thing and was deleted 2026-08-01 (§22, decision 6) |
| **14 Dream pockets** | a 3D pocket format | ❌ **the probe scene only**, wired into nothing. The 2D heist model is what exists and it is being deleted |
| **Stations, tool gates, permanent traps** | `RecipeData` / `ItemCategory` / `CraftBlock` | ❌ **§19's last table, unbuilt** |
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
| Her notes | — | ± | ± | **4** *(2 granting, 2 silent)* |
| `ItemData` | | | | **~39** — 8 materials, 4 salves, 6 traps, 2 weapons, 3 coats, 3 tools, ~13 lore/blueprints |
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
| **The knot beat** | `WorldStateManager.NextKnotBeatOrdinal` counts cuts, so cutting the last-listed knot first still plays beat 1 — break-tested against an index-keyed implementation, which passed four checks and failed exactly the two that encode the decision. The three beat *texts* are pass-2 prose with **no home until unit 6**; the authoring rule they must obey (a beat may not name the room it fires in) is recorded on the method. |

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

**Unit 2 — the blight and mend layer (§22).** *Medium.* A blight is not today's hazard: it charges
nothing, gates the screen's cleansing, presents a jigsaw, and pays one Graveflower, once. **The hazard
it is not was deleted on 2026-08-01** (decision 6), so this unit builds onto a clear field rather than
around a superseded one. Includes the
**cleansed-screen rule** — a screen with nothing Held, Blocked or Fouled left flips to
`WakingBackgroundPurified`, free — which is the visual payoff for the whole Waking world and currently
has no owner in code. **This is also where the zone machinery dies** (decision 4): per-screen cleansing
replaces `ZonePhase`, the lieutenant, `IsZoneLieutenant`, the Corrupted→ActiveFront flip and the
`zone.*.purified` flag, and it takes over driving `WakingBackgroundPurified`. Keep the **puzzle seam
thin**, exactly as §19 requires for recipes: solving writes
one flag and nothing else, so `--verify-slice` never touches a jigsaw.

**Unit 3 — the encounter state machine.** *Largest.* The one piece of combat still missing: rig two
slots, click to opt in, alternating turns, placement free and acting costing a turn, flee guaranteed.
`CombatManager` already owns the arithmetic and has 39 gates over it; this gives it a caller. **Unblocks
30 of the ~58 screens**, since a fight screen is usually also something else. Carries two small things
from decision 5: an **optional texture override on `EntityPlacement`** so the Beast is one archetype with
three looks, and a ruling on what winning against a never-human thing sets.

**Unit 4 — the station layer and §19's shapes.** *Medium.* `HotspotKind.Station`, `RecipeData.`
`RequiresStation` / `RequiresBothTracks` / `SetsFlag`, `NotAtCamp` → `NotAtStation`, `Reagent` →
`Component`, materials as a **tally with no slots**, `SurvivesDeath` gaining `Equipment` **and `Trap`**,
a salve cap beside `RigSlotsPerFight`. **A crafted trap is an owned flag, not a stack** — the bench
refuses a second one. This is also what makes `RecipeData.SetsFlag` exist, which is how a tool gates a
door: learn the Bill Hook → flag → the bramble hotspot opens. **No new gating field is needed for tools.**

**Unit 5 — the return-to-camp verb (§17 as amended 2026-08-01).** *Small, and it got smaller.* Decision
3 settled that **a camp is still a screen you are teleported to, and you walk out of it**, which means
almost nothing about the built camp system is wrong: `IsCamp` derived from a `Workbench` hotspot, the
shared cache, arrival refill, the collapse-and-respawn path and `SurvivesDeath` all stand. **What is
added is one verb** — *return to camp*, available from anywhere in the Waking world with no run live, and
three camps to point it at. **Fusion stays camp-bench-only**, so `NotAtStation` still has to tell a field
station from the bench. Two rules to settle while building it: the verb is refused **inside a fight** and
**inside a pocket** (§17's surviving-pressures table depends on both), and a camp keeps ordinary exits,
because the walk out is the price the amendment exists to charge.

**Unit 6 — the Dream pocket format.** *Large, and it is the only unit with an unknown in it.* Handoff
units 2–4: a greybox pocket, then the seam into `Main` with its gates and the atomic grant-and-consume,
then rigid bodies. **Its deliverable for this plan is a format** — what a pocket scene is, what it grants,
how a door opens it, how a cut knot reports back — because 14 pockets cannot be authored against a
prototype. Retire `DreamHeistData`, `DreamRunManager` and the six `dream_way_*` rooms as part of the
seam, not before: they are what makes the slice play end to end today.

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
| Blights | `mend.<room>` — one flag, one mend, never repeatable |
| Pockets | `pocket.<id>.taken` — the atomic grant-and-consume writes it |
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
finale winnable on path materials). These belong with the unfalsifiable guards §5 of the handoff already
says to write down instead of gating.

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

Eight, each of the last two turned up by settling the one before it. **All eight were settled the day this was written.**

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
swap.** Jeremy: *"I am ready to retire it all now that we are building lol. The dream world is
deprecated as well for the 3D world being worked on separately."*

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
> | with generation | the waystation, the six `dream_way_*` rooms, the rat, `StartingRoomId`, and re-anchoring 154 stranded checks | pass 1 |
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
> The six `dream_way_*` rooms were remapped `PlaceholderStyle = 2` → `1` rather than deleted, so 154
> checks keep their fixture until there is a real one.

> **A Dream creature is not an `EntityData` — 2026-08-02.** Jeremy: *"enemies in the spirit realm will
> have completely different functionality than the 2d battle types."* Right, and it changes what unit 6
> has to design rather than only how it renders. `EntityData` is built for **the fight grammar** —
> vulnerable/resistant, durability, damage, tether state, what severing frees. A Dream creature has none
> of that: `dream-puzzles-vs-pursuer-tension` makes them **mindless hazards that cost Resolve**, and the
> pursuer is a chase, not a turn order. **So unit 6 authors its own resource type and does not extend
> `EntityData`** — which is also why `SpiritTexture` and friends were dead weight rather than a head
> start. The one genuine overlap is the boss, which exists in both realms; that is a *link between two
> files*, not a shared schema.

---

## 8. Order of work, and what "done" means

| | Then | Done when |
|---|---|---|
| ~~now~~ | ✅ ledger ch1 drafted · ✅ unit 1 · ✅ **spectral vision deleted** · ✅ **unit 1.5** · ✅ **pass 1, all 59 rooms** · ✅ **the boot moved to `ch1_cottage`, bench placed** · unit 2 | flag vocabulary published; a blight mends and a screen cleanses in the slice |
| **now** | **retiring the prototype** *(re-anchor, then delete)* · unit 3 · unit 4 | the waystation, the six 2D Dream rooms and the rat are **gone** and every gate that stood on them names a synthetic `verify_` room instead; a fight can be started, won and fled in a real room; a tool opens a door |
| **then** | unit 5 · unit 6 · ch1 pass 2 | **the vertical slice: one chapter-1 arm end to end** — walk in, fight, take the pocket, mend the blight, cleanse the screen, camp |
| **then** | chapter 1 complete · seal it | `--verify-content` green on ch1's block, and it plays start to bridge |
| **then** | chapter 2 · unit 7 | sealed the same way |
| **then** | chapter 3 · the Snarl · the finale | all three endings reachable |
| **last** | pass 3 everywhere · audio | — |

**The vertical slice is the real milestone**, not chapter 1. It is the first moment the pipeline is
proven, and everything after it is repetition at known cost. Chapter 1 is where the estimate for
chapters 2 and 3 comes from.

**The honest costs, stated so they are not discovered later.** Nothing is played until unit 3 lands, so
the gates remain the only verification until then — and green gates never proved playability. The
~58-screen art bill is untouched by any of this and is the longest pole in the project. And unit 6 is
the only unit with a genuine unknown left in it; if it runs long, `combat.md` §23's lever applies —
**shrink a pocket, do not drop one.**
