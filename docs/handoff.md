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

**Last verified: `ALL GATES PASSED (1080 checks)`, exit 0, `dotnet build` clean with 0 warnings — and
uncommitted.** Every pass is left verified for Jeremy to commit; nothing here commits.

**Run the suite with the `--` separator: `godot --headless --path . -- --verify-slice`.** `Boot` reads
`OS.GetCmdlineUserArgs()`, which returns nothing before a `--`, so the flag is silently ignored without
it — the game just boots and a headless run then sits in an idle scene tree forever. **That looks
exactly like a hung verifier**, and on 2026-08-05 it cost two ten-minute timeouts and a bisection
through code that was never at fault. It is a *no output at all* signature: no section header, no first
check. If you see that, check the command line before you suspect the build.

**`godot` is not on the PATH a tool-run shell inherits** — it is
`"/c/Users/Owner/Desktop/Game Dev/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"`,
quoted for the space. Bare `godot` fails with **exit 127**, which is a *different* signature from the one
above (an error line, no checks) and means the binary was never found, not that anything is wrong with the
project.

**The crossing and the verb economy have both been played, 2026-08-04.** `F10` in and out of the probe
pocket, then all three verbs on `Q` / `Space` / `E` draining the bar at their own rates. That is the first
thing *play* has confirmed about the Dream rather than the gates, and it retires steps 1 and the crossing
from the outstanding-risk list. **The verbs cost but do nothing** — floating, dousing the light and pushing
it out are movement and rendering, which are Jeremy's 3D pass; the hook they read is
`DreamVerbs.IsHolding`. Still unplayed: the exit, the anchor, gates and creatures, in `build-plan.md` §8's
ladder order.

**What is actually on disk, and this is the line to trust when a doc disagrees: 59 rooms, 13 entities,
12 blights, 3 major repairs, 32 fights, 3 camps, 2 general field stations + 3 repair stations, 37 items,
20 recipes, 1 pocket.** *(Items were
27 until the ten lore pages landed on 2026-08-05; recipes 17 until the three repairs landed the same day; entities
11 and fights 30 until the Lost Trapper and the Blacksmith were placed, also the same day. **Iris is the one
creature still unwritten**, which is what keeps entities off 14 and fights off 33 — `items.md` §10 leaves her
damage pair unfixed until the finale is authored, so she cannot be derived the way those two were. If
you are re-reading this line, re-measure it; a count is the fastest thing in this file to go stale, and this is the
sentence other docs are told to defer to.)* **A major repair is not a thirteenth blight** — it is a `Station`
hotspot carrying a `MendPuzzle`, counted separately on purpose, because the twelve Graveflower the good ending
is priced against come from the twelve `Mend` hotspots and nothing else. Rooms, items, recipes,
entities **and pockets** are each counted against the files on disk as well as against what got loaded, so a
parse failure or an unplaced file cannot hide behind a green run. Heists are asserted at zero. **That closes
the fifth blind-spot class for every collection the game has** — and pockets got the check in the sitting that
created the directory, rather than after the first silent failure.

**The one pocket is a reference implementation, not content.** `data/pockets/dream_probe.tres` points at the
3D probe and grants nothing, so it consumes none of the eight max-Resolve budget. **`F10`** enters or
leaves it — not `F4`, which the probe scene already binds to its own readout toggle.

### What just landed

- **Unit 7a — the journal, and it is the crafting screen. 860 → 913 checks.** `src/UI/CraftingPanel.cs`,
  built to decision 9: **one book, two sides**, his Craft and her Physick, and each side is where you make
  that side's things. There is no separate crafting screen and never will be — decision 9 deleted a screen
  rather than adding one. `%CraftTab` / `%PhysickTab` / `%RecipeList` / `%RecipeTemplate` / `%CloseButton`
  are the contract, published in §2 with `BuildFallback` standing in until the `.tscn` lands, so a bench
  does something today.
  - **A row is the verb**, on the camp menu's precedent — a *Make* button beside a selected row is a
    confirmation step with one possible answer. **An unlearned page is absent, not greyed**, so the journal
    does not read as a checklist of content still to find. **Which side a page is on is derived, not
    authored**: `TrackOf` reads the blueprint prefix and falls back to the output item's `Track`, with a gate
    crossing both sources on all 16 recipes where they speak.
  - **`F7` and `CraftEverythingPossibleHere` are gone**, retired by what replaced them. `F6` stayed, against
    the note that said it would go too: a journal shows what you know, it does not teach you, and nothing in
    the game grants a blueprint yet.

- **The pages, 913 → 929 checks (2026-08-05).** Ten `.tres` in `data/items/` — his seven craft families and
  three of her four notes — plus `PocketManager.GrantPage`, which hands the page over beside the knowledge
  whenever a `Knowledge` pocket grants a family. **Decision 11's crossing is what holds them: *everything you
  know, you hold; everything you hold, you know*** — two arms in `GateCraftingPanel`, counts in both messages,
  and no third string to drift because the page id **is** the blueprint id. The eleventh file and the
  placement of her three are Jeremy's; see §2's table.

- **The satchel's *Set* button, 929 → 945 checks (2026-08-05).** `%SetButton`, a third sibling beside Use and
  Equip on the same optional-contract terms. It is **the first of the three verbs whose answer is a property
  of the room** rather than of the satchel and the body: `NavigationManager.SoleCreatureHere` finds the
  target and `EncounterManager.CanRig` decides both the grey and the refusal, so there is one answer to one
  question. **No target picker**, because a room holds one body. `F8` and `RigEverythingPossibleHere` retired
  with it.
  - **The invariant the key was standing on is now written down.** `SoleCreatureHere` returns the first
    placement, which is only correct while there is one, and *"one creature per screen"* was gated
    **nowhere** — `placement.md` rule 2's adjacency half was, its counting half was not, and
    `--verify-content` had been reporting the whole rule as covered. `GateFights` asserts it across the 30
    peopled screens, count in the message so an empty map cannot pass it.

- **The two-hand refusal, 945 → 968 checks (2026-08-05) — unit 7b's first half.** **`CraftBlock.HalfKnown`**,
  returned by `Inspect` immediately after `Unknown`, because it *is* `Unknown` asked about the other hand.
  Second reader of `RecipeData.RequiresBothTracks`, whose first reader drew the engine on both sides of the
  book a unit earlier and refused nothing — **the field read correctly and bit nothing for a whole unit**.
  - **Rule A, ruled by Jeremy: the other hand means *any* page on it**, answered by `KnowsAnyPageOn` off the
    flat discovery set, so no field was authored (`items.md` §6). The predicate is shaped around one
    near-miss: **pages *held*, not recipes *known*** — Clearmind is hers, needs no blueprint and is known from
    the first minute, so *knows a recipe on her side* is true at game start and the flag would have refused
    nothing it was added to refuse.
  - **A withdrawn claim, recorded because it was wrong in the direction that adds work.** I argued the
    refusal made placing one of her notes an invariant-12 requirement; Jeremy pushed back — *"this recipe
    could be on a side path because it isn't needed to finish the game"* — and `combat.md` §20 agrees. The
    engine buys the good ending and gates nothing, so **no gate forces note placement.**

- **Unit 9a — the aura, 968 → 1023 checks (2026-08-05).** `combat.md` §2's anchored body finally does the two
  things the section has described since the first draft, and Jeremy set how they divide: *"regen hp on
  enemy's turn and resolve drain on the players turn makes sense in my head. Double whammy."* On the player's
  turn `EncounterManager.EndPlayerTurn` bills `CorruptionAura` × the full bar through the new
  `ResolveManager.SpendForTurn`; on the creature's turn `Regenerate` closes
  `GameConfig.AnchoredRegenFraction` of its durability through the new `CombatManager.Mend`. **One condition
  over both** — `ActiveAura` returns 0 unless the tether is `Corrupted` — so severing cannot take one half and
  leave the other, which is the arm a partial build would have passed.
  - **Three things to know before touching it.** `AnchoredRegenFraction` (0.05) must stay strictly **below**
    what a wrong-type weapon deals, or a resisted weapon is futile instead of slow — §18's
    immunity-reads-as-a-lock failure from behind. A **positive aura is the boss marker**; there is no
    `IsBoss` flag and adding one would give the fact two homes. And **banishment needed no code**: an empty
    bar already routes `Deplete` → `CampManager` → navigate → `OnRoomEntered` → `EndFight`, so a caller must
    ask whether the *fight* is still running, never whether the bar is empty — the respawn refills both pools
    synchronously, as `OnCollapsed` already warns.
  - **The default was the live defect, not the absence** — `0.15f` unread for weeks, which would have made
    all eleven authored creatures bosses the moment anything looked. It is `0` now and auras are opt-in, so
    **the three bosses need theirs typed** — two of the three were typed at `0.167` when they were placed later
    the same day; only Iris is outstanding. §3's eleventh class carries the general form.
  - **Mid-fight rigging was deleted rather than built, and I had recommended building it.** §18 priced it at
    a turn; §22's table then **struck** rigging from what a fight reaches, so the branch served an overruled
    rule and no UI could reach it. `RigBlock.InFight` refuses now, first in `InspectRig`. **The lesson is the
    recommendation, not the deletion** — I proposed it from the code, and the docs settled it against me.
  - **Eight break-tests, all eight predicted, and one found a defect in my own gate** — see §3's tenth class,
    which now carries it. **One new check is honestly vacuous and says so in the file**: the aura census's
    assertion arm is a universal over an empty set until the bosses exist, so it cannot be break-tested.

- **A read-back over 7a, 7b, 8 and 9a — 1023 → 1042 checks (2026-08-05).** Not a feature; the question asked
  was *what did those four units ship that nothing watches.* Method: take the uncommitted diff as the unit
  boundary, enumerate the new surface, then **break-test anything thin** rather than trusting a reference
  count — name-counting misleads in both directions here (`SpendForTurn` is named nowhere in the verifier and
  is thoroughly covered; `if (!InFight) return;` sits among coverage-looking neighbours and was covered by
  nothing). Unit 8's respawn, `CombatManager.Mend`, `TrackOf`, the *Set* button and 9a's default / constraint /
  tether / banishment-outcome all came back properly gated. Three gaps did not:
  - **A guard whose deletion left 1041 green: `EndPlayerTurn`'s post-drain `if (!InFight) return;`.** The
    placement is looked up *before* the drain, so it survives banishment, and `CreatureStrike` has no fight
    guard of its own — that one line is all that stops a creature rooms away striking you at the bench on a
    turn of a fight that already ended. The banishment block asserted the camp, the refilled nerve and the
    closed wounds, and **never the body**. Worse, the comment above it *described* the guard in words, which
    is how it read as covered for three units. **Prose naming a guard is not a check on it.**
  - **Unit 7b's whole deliverable had no witness.** `PocketManager.GrantPage` looks the blueprint id up *as an
    item*; the fixture pocket's blueprint is synthetic on purpose (*"so a gate never grants something the
    ending's bill would count"*), so every completion in the suite took the *"no such item"* branch and the
    grant could be deleted with the suite green. The knowledge half was asserted the whole time, which is what
    hid it. Fixed with a **fourth** fixture pocket granting the real `lore_craft_stoneworking` — a separate
    pocket, so the synthetic one's rule still holds for the checks that relied on it — and the page is taken
    back out of the satchel after.
  - **Four of 7a's six panel-exclusion lines in `Hud` were unwatched**, including `_crafting?.Close()` on
    `FightStarted` — whose own doc-comment records unit 5's declined-then-reinstated camp-menu guard, and
    which was then left in exactly that state for the new panel. Deleting it did redden one check, *"it starts
    closed"*, from a **different section** by way of `GateM3_MicroLoop` clicking a bench: coverage by
    accident, with a message that sends you to panel hygiene rather than to fights. All four have named
    witnesses now, each break-tested to one red.
  - **Two §3 classes, one genuinely new.** The **fifteenth** is the one worth reading: my first attempt at the
    guard check above was itself unfalsifiable, because every `Arena()` fixture deals 0 damage, so it asserted
    that a creature which cannot hit anything did not hit anything. **A check phrased as an absence needs a
    fixture that can produce the presence** — and only the break-test could say so. The **sixteenth** was
    already known and only ever recorded in memory, so it got paid for twice: a verifier run after a **failed
    build** reports the previous build's green, and the check count cannot tell you, because a stale count and
    a correct no-op count are the same number.

- **The three major repairs, 1042 → 1073 checks (2026-08-05).** `items.md` §6's broken bridge, dead mine cart
  and exorcism engine, authored into `ch1_bridge`, `ch2_cart_road` and `ch3_breach` — **a `Station` hotspot
  plus a `SetsFlag` recipe each, and the two passage doors are gated on them now.** The recipe half was
  already built (`RecipeData.RepairsWorld`, handled by the manager and the panel since unit 4b); what was
  missing was the board's **carrier**, which is the only code this took: `MendManager.CarriesABoard` widened
  from a bare kind test to *`Mend`, or a `Station` with a puzzle that has been assembled*, with
  `MendPanel.Open` and `InteractionService` both asking it instead of the kind.
  - **Two clicks, in this order, and the order is the whole design.** The station opens **the book** until its
    repair has been crafted and **the board** once it has — so the commons are paid and the knowledge is
    gated in the panel, and §22's *"the puzzle is not a gate, it is the animation"* has somewhere to leave the
    price. `SetsFlag` is **withheld from the click** on a repair station: on every other station it announces
    a venue, on this one it is the passage, and a bridge that rebuilds because you looked at it is not a
    repair. A gate asserts exactly that.
  - **Only `CarriesABoard` widened, and the other three predicates were left alone deliberately.**
    `IsFouled`, `IsCleansed` and the room sweep still count `HotspotKind.Mend` alone, so **a broken bridge
    does not foul its screen and pays no Graveflower.** That is not tidiness: §20 prices the engine at 8 of
    the world's 12 flowers, so a thirteenth would be a flower nobody costed. `IsAssembled` reads the
    repair's own `RequiresStation` / `SetsFlag` rather than a new field, so **a station and its repair cannot
    be authored into disagreeing** — no enum member and no `HotspotData` field were added.
  - **Four failures, and only one was a defect in the new content.** The two-blueprint check was *"nothing
    needs two hands yet"* — retired by the thing it was waiting for, and re-pointed to the positive claim.
    The journal check *"the only thing on her side not written in her hand"* named a **fixture id** where it
    meant a **field**; the real engine needs both hands for the same reason, so it is keyed on
    `RequiresBothTracks` now, with the row counts in the message because widening an exemption is how an
    `All` goes vacuous. **The real defect was mine**: I gated the two stations on `defeated.ch1.lost_trapper`
    and `freed.ch2.blacksmith`, and neither boss is placed — a gate naming an unplaced creature is a hotspot
    that can never open, which `GateWorldGraph` catches correctly. Both lines came back out, and the
    obligation is recorded in the direction that **survives**: a check that is green while a boss is unplaced
    and red the hour Jeremy places one. Naming the flag would have been red today for something that is not
    a defect.
  - **And a differential that a content change falsified — §3's seventeenth class.** *"A creature standing in
    a doorway shuts screens off from a fresh game"* compared the fresh walk against a walk with wardens
    ignored **but flags still barring** — and every warden-barred door in the game lives in chapter 2, so
    gating the ch1→ch2 seam put all three behind a flag, both walks stopped at the seam, and the counts came
    out equal. It was never measuring the warden rule; it was measuring whatever the flag rule left
    reachable. Both differentials now vary **one rule from a common unfiltered baseline**, which is strictly
    stronger than what was there.

- **Two of the three bosses placed, 1073 → 1080 checks (2026-08-05).** `data/entities/boss_lost_trapper.tres`
  and `boss_blacksmith.tres`, standing in **`ch1_trap_yard` and `ch2_smithy`** — his trap line and his forge,
  *one screen before* the repair each one gates — with the `RequiredFlag` back on the span and the cart.
  **This was derivable rather than judged by eye, which is why it is in my lane and the
  previous sitting's note was wrong to hand it over**: `items.md` §10's boss table already fixes both display
  names and all three damage types, and `combat.md` §6 fixes the aura at a rate that empties a bar in six
  turns. Only `Durability` and `Damage` were uninvented — 34/12 and 38/14, chosen so a matched weapon at
  Potency 10 doubled kills inside the six-turn Resolve budget. `BlocksPath` is **false** on both: the only
  blocker in the roster is the Stable Master, and the passage each boss guards is already gated on their
  defeat flag, so blocking would additionally seal the way back out of the chapter behind them.
  - **Jeremy settled the Blacksmith, and it cost no code at all.** `combat.md` §20 says he *begs, is freed,
    thanks you, and dies*, which read as `freed.*`; `GateRoster` predicted `defeated.*`. His ruling: *"his
    spirit is broken in the spirit realm and his demonic form is weakened and he is killed when the demon dies,
    not keeping his body alive any longer without the spirit."* So `defeated.*`, and **`Flags.Defeated`'s
    domain is now about the *outcome*, not the *nature*** — it used to mean "was never a person" and now means
    "frees nobody", because the ending's bill must not count him as saved. Which flag a win writes is
    **derived** from a blank `EntityData.PurifiedName` and never authored, so implementing this was leaving a
    field empty. The roster bill moved `defeated.* 6 → 8`, reaching 9 with Iris; `freed.* + killed.* == 24`
    did not move, which is the whole point of the split.
  - **The carcass did not widen with the domain, and that asymmetry is a ruling.** Jeremy, the same day:
    *"no loot, since it would be dishonourable to loot a human corpse."* Harvest is scoped to the Beast **by
    what it is** now, where it used to ride along on what `defeated.*` meant — so a `Harvest` hotspot behind
    `defeated.ch2.blacksmith` **would pass every gate in the suite and be wrong**, and `Flags.Defeated`'s
    remark says so in as many words. `EntityData.HarvestItemId` stays blank on both bosses.
  - **The pair-echo rule is gated rather than exempted, and that was a real choice.** `items.md` §10 has each
    boss answer to a pair its own chapter already taught — free teaching. The cheap move was to exempt bosses
    from pair-uniqueness the way `beast_` is exempted; instead they are held out of the uniqueness set and then
    **required to collide with a chapter-mate.** An exemption would have permitted two bosses sharing a pair
    with each other, or with a creature from the wrong chapter, and said nothing.
  - **The reverse check retired on schedule** — its comment predicted the day and got it right. It read *"each
    boss that is placed holds its repair shut"* and is now two flat gates, because either half alone passes on
    the other's absence: an unplaced boss cannot fail a gating check that skips it, and a gated repair says
    nothing about whether the creature it names is standing anywhere. The break-tests confirmed the split is
    two claims rather than one — removing the cart's flag reddens only the gating half, renaming the placement
    reddens only the placed half plus the dangling-gate sweep.
  - **And the room was wrong for an hour, green the whole time.** I put both bodies on the *repair* screens
    — `ch1_bridge` and `ch2_cart_road` — because that is where the `RequiredFlag` lives, and the ledgers put
    each boss one screen back: `ledger-ch1` row 9 against row 10, `ledger-ch2` row 11 against row 12, with
    `ledger-ch2`'s *Screen | Fight* table naming the Blacksmith at **11** and `ledger-ch1`'s path reading
    *"Hunter #2 → the Lost Trapper → the bridge"*. **1080 checks passed on it**, because my own new gate asked
    whether each boss was placed in *some* room. It asks for the room now, and the break-test reproducing the
    misplacement reddens exactly one check with both room names in the message. The lesson is the recurring
    one: **a check keyed on a weaker property than the document it enforces reads as done on content that
    document forbids** — and the weaker reading is the one that feels like it is not hardcoding anything.
    (The `▸` column in the ledgers is the **pocket door**, not the body; the blank under *Held by* is why I
    misread it, and both rows now name the body too.)
  - **And one defect that cost twenty checks going dark with nothing red.** §3's **eighteenth** class,
    and the most expensive kind of green — the two-click fixture selected *the repair nothing is holding shut*,
    so gating the last ungated station deleted the block. It constructs its state now instead of discovering
    it. Also non-vacuous for the first time: `every authored aura is a fraction of a bar rather than a rout`
    reads **2 of 13** where its own comment admitted it was *"vacuous today and will stay so until the bosses
    land."*

**Units 5 to 9, kept to what still constrains work.** Each unit's detail is in `build-plan.md` §4's callouts
and its design reasoning in `combat.md`; the gate lessons are all in §3 below. What is here is the half a next
session can trip over.

- **Unit 9 — the verbs have keys (844 → 860).** `DreamVerbHold` turns `Q` / `Space` / `E` into holds and
  **`PocketHost` attaches it, so a pocket scene carries nothing**; `F11` grants the two grantable verbs so a
  `PocketGate` is walkable before a boss pocket exists. Two constraints: it **polls** rather than handling
  events, because `_UnhandledInput` is unreliable inside a `SubViewport` and a pocket is always in one; and it
  **caches nothing**, asking `IsHolding` every frame, because `PocketManager.Return` calls `ReleaseAll` and a
  cached set would outlive the release and leave the key dead on the next crossing. It first shipped as a node
  to add per scene and no key worked — **wiring that can be forgotten fourteen times will be.**
- **Unit 8 — item loss is gone, and both empty bars do one thing (826 → 844).** `SurvivesDeath` keyed on a
  field two scarce items do not serialise, so **dying while carrying Graveflower permanently deleted the good
  ending**. Both bars now share `RespawnAtCamp`, which **revises `combat.md` §6** — an empty nerve in a boss
  fight is a camp respawn, not forced flight. Inside a pocket the ejection happens first and writes nothing,
  so the entrance survives.
- **The Dream Resolve economy.** All three verbs drain while held and the rate is a **sum**, because §19 pays
  for floating out of the hiding. `DreamCreature` contact costs Resolve rather than health and the
  **cooldown** is the field that matters — without it one touch empties the meter in a few frames.
- **`--verify-content`, the bill checker.** `godot --headless --path . -- --verify-content` prints
  `placement.md` §1's bill against what is on disk and **always exits 0**, because a bill is a to-do list
  while a chapter is being built. It names the rules it *cannot* check (1, 7, 8) so nothing reads as covered
  by omission.
- **Unit 6b — the heist is retired (843 → 810).** `DreamRunManager`, `data/heists/`, the heist signals and the
  stranding guard are gone; `InsideTheDream` lost one clause and the seven guards reading it were not edited.
  I called it pure deletion and it was not — five surviving invariants had no probe outside a heist-driven
  block, so 13 pocket-side probes went in **before** anything was removed.
- **Unit 6a — the way into the Dream (758 → 830).** A `CrossToDream` hotspot crosses you into a 3D scene and
  back; the entrance is **scenery, never an item**, and breaks once taken (two hotspots on one flag, opposite
  `RequiredFlagValue`s). `Complete()` grants and flags, `Leave()` writes nothing. **The seam needs no scene
  change** — a pocket drops in beside `RoomView`, so autoloads and every panel's state survive a crossing.
  And **max Resolve can rise now** (`RaiseMax`, persisted); the import runs **before** the bar is restored, or
  a raised ceiling is trimmed on every load.
- **Unit 5 — the return-to-camp verb (687 → 758).** `C` lists the camps you have stood in and a row is the
  verb; refused inside a fight and inside a pocket. One ruling: **you choose the destination**, because the
  smaller reading contradicts §20's revised good ending. The **citation gate** rode along — it walks `src/`
  for references to this file's sections, which is why renaming §2 or §3 reddens the suite.
- **Decision 9, the equipment-gating ruling** (`build-plan.md` §7). Equipment may gate content in the chapter
  before the chapter that makes it. The measurement that settled it: **one anvil, in `ch2_smithy`; one mortar,
  in `ch2_apothecary`** — so chapter 1 can craft **the six traps and nothing else**, and is **sleep-only for
  healing**, both salves being mortar recipes. Know this before authoring anything in chapter 1.

### What is next

**The remaining work was re-cut into two lanes on 2026-08-04 — `build-plan.md` §8's *"What remains"* is the
schedule, and this is the summary.** The tail of that table had become four coarse rows whose first entry
bundled Jeremy's 3D pass with Claude's code into one undefined unit. The lanes touch different files, so
they run in parallel: **Jeremy builds pocket 1 and plays it**, **Claude builds the crafting panel**.

### Jeremy's queue — the exact shape of each

**Written 2026-08-05, when Claude's half of unit 7a closed.** Everything below is blocked on a judged call
or on the editor, so none of it is Claude's to do. Ordered by what unblocks the most; each says what turns
green, because two of them turn something **red** and that red is the design working rather than a mistake.

1. **Her fourth note** — one `.tres` in `data/items/`, and **the first decision is whether it teaches**,
   which reopened on 2026-08-05: *"you say there is an extra note I'm sure it is for it. You need graveflowers
   to build it so you probably miswrote that blueprint part back then."* Both answers are cheap and the naming
   rule covers each without amendment, because `lore_` means *it teaches*:
   - **If it is story only** — any id **not** beginning `lore_`. That prefix is what the decision-11 crossing
     sweeps, so a narrative page carrying it would be accused of teaching a family that does not exist.
   - **If it is the engine's Physick page** — `lore_physick_<family>`, plus a recipe naming it, and then it is
     not a one-file job any more. **Note that rule A means you do not need it to be**: her three existing
     notes each satisfy the engine's Physick half already, so the fourth can stay pure story and the engine
     still works. Deferred with the engine either way.

   Otherwise a `Lore` item like any other — `Category = 4`, `Track = 1` (Physick), and whatever `DisplayName`
   / `Description` you want. Nothing goes red on the story reading; the shelf gates count it and the
   naming-rule gate confirms it teaches nothing.

2. **Place her three notes** — one `Harvest` hotspot each in `data/rooms/`, with `ItemId` set to the page id
   exactly: `lore_physick_burning_oil`, `lore_physick_corrosive_draught`, `lore_physick_deepmend`. Any
   Waking screen; her notes are found, not learned. **Two gates go red when the first one lands, on
   purpose** — `GateRoster`'s *"and no blueprint page has a source yet, all 10 of them"* and
   `GateStations`' *"but nothing in the world hands one over yet — a pocket or a placed page turns this
   red"*. They are a **self-retiring ledger**: they exist to make the unreachability visible, and their
   going red is the signal to delete them, which is Claude's ten-second job. **It is not your bug and it is
   not a merge conflict — say the word and the ledger comes out.** His seven pages cannot be placed this
   way; they come from Dream pockets, which is item 4.

3. **Optionally, the satchel's three verb buttons** — `%EquipButton`, `%SetButton` and `%CategoryTabs` are
   now published in §2's contract table above. All three are stopgapped, so **the satchel is fully usable
   today** and authoring them is a layout improvement rather than a dependency. Worth doing while you have
   the scene open for anything else.

4. **Pocket 1, and play it** — unchanged, still the largest thing in your lane and still the thing that
   unblocks the most: it retires `F6` and `F10`, gives his seven pages a source, and starts the eight
   max-Resolve budget. The checklist is below and it needs no scripting.

5. **The rest, unchanged and further out** — `CampPanel.tscn` and `CraftingPanel.tscn` against their
   published contracts; ~~the three bosses and the 12th creature file~~ **the 12th creature file, and Iris —
   the other two bosses came off this list on 2026-08-05** when you asked why they could not be placed the
   way the enemies were, and the answer was that they could: the docs fix everything but two numbers, so it
   was the derivable half of content and mine to write. **Iris genuinely is still blocked**, but on a
   *design* call rather than a 3D one — `items.md` §10 leaves her damage pair unfixed until the finale is
   authored, so she is not waiting on your pocket pass. Then chapter 1 sealed, at which point
   `--verify-content`'s ch1 block turns from a report into gates.

**One thing worth knowing before you play the satchel:** *Set* is a **before-the-fight verb only**. The
satchel refuses to open once a fight starts, so you rig traps by standing in front of a creature and
setting them *before* engaging. **Rigging mid-fight is not a missing verb, it is a deleted one** — unit 9a
struck it on 2026-08-05 (`combat.md` §22) and replaced the priced branch with a refusal, so *"you cannot rig
a trap once the fight has started"* is the rule rather than a gap. See §2's table.

**Chapter 1 pass 2 — the vertical slice.** Walk in, fight, take the pocket, mend the blight, cleanse the
screen, camp. **Every listed beat is reachable**: camp landed with unit 5, the pocket with 6a, and 6b left
the Dream with exactly one model rather than two. What remains is authored content plus **Jeremy's own 3D
pass**, which is a separate workflow by his own framing — *"3D dream world is its own project."*

~~**But the bench you camp at does nothing** — `CraftingRequested` fires with nothing subscribed and `src/UI/`
holds no `CraftingPanel.cs`.~~ **Closed by unit 7a on 2026-08-05:** the panel exists (`src/UI/CraftingPanel.cs`),
a bench opens the book, and `BuildFallback` stands in until `scenes/ui/CraftingPanel.tscn` is authored against
§2's contract. It was the one thing standing between chapter 1 and being playable, and what stands there now is
authored content — the bosses and the pockets.

**Two decisions closed with the re-cut, and both cost nothing to build** — `build-plan.md` §7. **Decision
10:** a major repair is gated on the boss flag, not a key item, and the *puzzle* is what makes it major —
which the ch1 and ch2 ledgers had already drawn (*"Gated on the boss memory"*) and nobody had read back.
**Decision 11:** a blueprint page is an item whose id **is** the blueprint id, so the page is a view of
the knowledge rather than a second copy that could disagree; the Lore tab in the satchel is what it is
for, and it is checkable anywhere while crafting needs a venue.

**And `--verify-content` now answers "what is chapter 1 short of" without anyone guessing.** Its first run
says everything outstanding traces to **two things — the three bosses and the fourteen pockets**: the major
repairs want a reclaimed memory, the eight max-Resolve grants want knowledge pockets, and most of the twelve
missing items are the lore pages those pockets hand out. Eleven rows, not eleven independent jobs. **Both of
those clauses have since moved and in opposite directions**: the repairs no longer want a memory at all
(decision 10 made the boss *flag* the gate) and two of the three bosses are placed, so what is left of the
first thing is Iris alone — while the fourteen pockets have not moved at all. **The report is down to 12
rows, and it is one job now rather than two.**

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

**Unit 5, three units back, is the one data point on what play finds — and what it found was not in unit 5.**
Jeremy noticed the satchel opening during a fight because `C` had just refused to, and behind that cosmetic
report was a free heal: `InventoryCraftingManager.UseItem` had no fight guard, so the Use button healed with
no turn spent and nothing counted against `SalveSlotsPerFight` — a second currency inside a fight, behind a
key, for a unit and a half. Fixed as `UseBlock.InFight` plus an `asFightTurn` flag only `Treat` passes,
defaulting to the refusing direction. **Full reasoning in `combat.md` §19's callout**, including the trap
that hid it; the guard-declined-on-bad-reasoning lesson is now §3's mirror of the seventh class. The one
line to carry forward: **a new verb audits the old ones by standing next to it** — two keys behaving
differently is far easier to notice than one key behaving wrongly, which is the argument for playing every
unit rather than only the risky ones.

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

  > **Amended 2026-08-05: Claude may edit `.tres` files that already exist, having agreed the change
  > first.** The old form was *write only files that do not exist, never replace one*, and Jeremy retired
  > it himself: *"this rule was made before I knew how efficient you were at building the rooms… you
  > placing everything roughly and me altering it would be a faster workflow."* So the sequence is
  > **agree, place roughly, he alters** — the agreement is what replaced the file-existence test, and it
  > is cheap because what needs agreeing is usually one line of *what goes in which room*.
  >
  > **What did not change is the reason the old rule existed.** Two hazards survive it and both are
  > mechanical rather than about ownership: a `.tres` open in the editor will be written back over
  > anything Claude put in it, so **say which files are being touched before touching them**; and the
  > typed-array rule below still holds, because a hand-edited `Array[...]` fails silently. *Judged by
  > eye stays his* — regions, prose, which room is beautiful — which is §2's original line and not
  > something this amendment reaches.
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
| `%EquipButton` | `Button` | *optional.* Calls `Equip`; disabled when the selection cannot be worn. Absent, the script builds one immediately after `%UseButton` in the same parent |
| `%SetButton` | `Button` | *optional.* Calls `Rig` against the creature on this screen; disabled when there is nobody here, or the selection is not a trap, or the sites are full. Absent, the script builds one immediately after `%EquipButton` |
| `%CategoryTabs` | any `Container` | *optional.* The category strip above the grid — one tab per `ItemCategory` he is actually carrying, plus All. Absent, the script builds one |
| `%CloseButton` | `Button` | hides the panel |

**The three verb buttons want to read left to right in the order the fiction does — drink it, wear it,
leave it somewhere for something else** — which is why the builders chain off each other rather than all
hanging off `%UseButton`. Author any subset: each name found in the scene is used as-is and its stopgap
stops running, so there is no half-authored state to get wrong. **`%SetButton` is the only one of the
three whose grey depends on the *room* rather than the satchel**, so it is worth looking at while standing
somewhere empty as well as in front of a body.

**`scenes/ui/CraftingPanel.tscn`** — root `Control`, `Visible = false`. **This table was rewritten on
2026-08-05, and the old one is worth knowing about**: it listed `%IngredientList`,
`%IngredientTemplate`, `%CraftButton` and a `%RecipeName`/`%RecipeStatus` pair inside the row template
— the *select a recipe, read its ingredients, press Craft* screen. Decision 9 (`build-plan.md` §7,
2026-08-02) deleted that screen: **the journal is the crafting screen**, one book with two sides. The
table below is what unit 7a actually built, and `CraftingPanel.BuildFallback` is it instantiated, so
like the camp menu there is a working tree to look at. Authoring against these names deletes the builder.

Anyone who had authored against the old table would have built four nodes nothing reads and no
side tabs — which is the point of recording it rather than quietly swapping it out.

| `%UniqueName` | Type | Script does |
|---|---|---|
| `%RecipeList` | any `Container` | one row per page **on the open side**; emptied and rebuilt on every open, every craft and every turn of the tab |
| `%RecipeTemplate` | `Button`, a child of the list, `Visible = false` | duplicated per page. **The row is the verb** — pressing it makes the thing, and there is no *Craft* button, on the camp menu's reasoning above. The whole line is one `Text`: name · inputs · why-not. No child labels, so nothing inside it is part of the contract |
| `%CraftTab` | `Button` | his Craft side. Set `ToggleMode`; the script draws the open side **pressed** rather than disabled, so it reads as *you are here* and not *unavailable* |
| `%PhysickTab` | `Button` | her Physick side, same treatment |
| `%CloseButton` | `Button` | hides the panel. Dismiss only, like the camp menu's |
| `%Heading` | `Label` | *optional.* Which side, and how many pages. Leave it out and nothing complains |
| `%Dim` | `ColorRect` | *optional.* Same 55% black and the same **anchors *and* offsets** warning as the camp menu's |

Three things about this one that the layout has to leave room for:

- **A page he has not learned is not in the book at all** — not greyed, not a "you don't know this
  yet" row. `CraftBlock.Unknown` means absent, because Daniel writes these pages himself once the
  Dream gives the craft back (decision 11), and because a greyed row would make the journal readable
  as a checklist of content still to find. So the list is short early and grows; do not design a
  layout that needs a full page to look right.
- **A page he *knows* and cannot make right now is drawn, disabled, with the reason on it.** That
  distinction — *you know how and cannot* against *you do not know how* — is the panel's whole job.
  The reason text comes from `InventoryCraftingManager.Words`, so the book and the bench speak one
  vocabulary, and one of those reasons (`AlreadyMade`) is deliberately worded as news rather than a
  refusal. Do not style disabled rows as errors.
- **The engine appears on both sides at once.** `RecipeData.RequiresBothTracks` is the eighteenth
  recipe (unit 7b, not yet authored), and it is the one page that is in the book twice.

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

- ~~**An unknown recipe must not show its ingredients.** When `Inspect` returns `CraftBlock.Unknown`,
  the row shows a name (or a blank) and nothing else — leave room for a "you don't know this yet"
  state that is visibly different from "you're short of materials".~~ **Superseded 2026-08-05: there
  is no such row.** The rule was right about the harm — an unlearned recipe must not advertise its
  inputs — and unit 7a went further than the rule asked, because the weaker version leaves the
  journal readable as a checklist of unfound content. Absent, not blank. See the journal's own
  bullets above.
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
| ~~`EntityData.CorruptionAura`~~ | ~~**Declared, defaulted to `0.15f`, and read by nothing.**~~ **Closed 2026-08-05, unit 9a.** `EncounterManager.ActiveAura` reads it, and it drives both halves of `combat.md` §2 — the Resolve drain on the player's turn (`ResolveManager.SpendForTurn`) and wound regeneration on the creature's turn (`CombatManager.Mend`) — with `Corrupted`/`Severed` as the one switch over both. **The `0.15f` default was the trap, not the missing reader:** the first reader would have given all eleven authored creatures a boss's aura. It is `0` now, so an aura is something typed rather than inherited, and one check exists purely to hold it there. What is still open is **content and art** — no `.tres` sets the field (the three bosses are Jeremy's), and the visual shimmer is unbuilt. |
| `RecipeData.RequiresBothTracks` | **Same class: gated as a *shape*, read by no manager**, so a both-tracks recipe behaves like any other today. Exactly one customer — the exorcism engine, which is the unauthored 18th recipe. This is the whole of unit 7b. |
| `PocketGate` | **It reads *have you been taught the verb*, evaluated once when the pocket loads — not *is the key down*.** So a Flare wall becomes permanently visible the moment Flare is learned, and a Suppress gate is **always open** (that verb carries no flag), which makes it a node that can never do anything. Neither matches its own first line, *"a wall that only Flare shows you"*. **Deliberately not changed** — 2026-08-04, Jeremy: *"I rather leave the internal working of the 3D open so that I have more freedom."* Switching it to `DreamVerbs.IsHolding` is a ten-line change to make when a real pocket wants it, and picking now would narrow the 3D side before anything has been built against it. |
| ~~`InventoryPanel`'s Lore tab~~ | **Filled 2026-08-05.** Ten of decision 11's eleven pages are on disk, the shelf has its first gates (a page brings the tab, is drawn under it, and leaves with the last copy), and a crossing holds the set both ways. What is still open is *reachability*, below. |
| The ten pages, in the world | **Authored but unobtainable, and that is the honest state rather than a defect.** His seven are learned from Dream pockets, which unit 6 builds; her three are meant to be found in Waking rooms and **nothing places them yet** — that is a `Harvest` hotspot per note in `data/rooms/`, which is Jeremy's half. Two gates say so out loud and go red the day either arrives: `GateRoster`'s *"no blueprint page has a source yet, all 10 of them"* and `GateStations`' *"but nothing in the world hands one over yet"*. `PocketManager.GrantPage` is already written, so a knowledge pocket hands the page over the moment one exists. |
| Her silent fourth note | **Not authored, and the id is Jeremy's to pick** — a page that teaches nothing is pure story, and story is last. **The rule that keeps it out of the crossing is a naming rule, not an exception list: `lore_` means it teaches.** A narrative note gets any other prefix, is a `Lore` item like any other on the shelf, and belongs to no crossing. One gate enforces it (*"a page outside the naming rule teaches nothing, by construction"*), so dropping the file in is a one-file job with nothing to reopen. |
| Rigging, *inside* a fight | **The satchel's `Set` button is a before-the-fight verb only, and mid-fight rigging has no verb at all.** `Hud.ToggleSatchel` refuses while `InFight`, and `Hud.cs:589` is the only route to `_inventory.Open()`, so the satchel is unreachable once a fight starts — meaning `EncounterManager.Rig`'s mid-fight arm (which spends the turn, `Rig` → `EndPlayerTurn`) is live code the player cannot invoke. **Deliberately half-built rather than half-designed**: §18 charges a turn for it, so the verb must sit in the turn order, which makes it the fight screen's job and not the satchel's. `FightPanel` has the read-only `%RigText` strip and no rig verb. |
| `CraftBlock.HalfKnown` has no authored recipe | **The refusal is live and nothing in the game sets the flag it reads.** `RequiresBothTracks` belongs to the exorcism engine alone, and that recipe is deferred (2026-08-05) because it buys the good ending and gates nothing — so the block is gated entirely against `verify_recipe_both` and its mirror, on purpose: a check phrased over authored content would be a universal over an empty set, green today and green the day the real recipe lands wrong. `GateRoster`'s *"nothing needs two blueprints yet — the exorcism engine turns this red"* is the ledger line that notices it arriving. Rule A means the recipe needs **no** second blueprint field when it does. |
| `RigBlock` has no `NotYourTurn` | Related and **deliberately absent.** `EncounterManager.Rig` refuses silently when it is not your turn (`if (fightingThis && !_playersTurn) return false;`), which `InspectRig` does not report despite its doc claiming a complete account. Adding the enum member today would manufacture a data shape with **no reader** — §3's eleventh class — because no UI can reach that path while the satchel is shut. It goes in with the fight-screen verb above, not before. |
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

**The inverse polarity of that rule bit on 2026-08-05, and it is subtler because the gate is green: a check
standing on "the content does not exist yet" expires on the day the content *arrives* rather than the day the
mechanism does.** Two read *"none is learnable from anything authored yet"* and *"no item is Lore yet"*, and
both tested whether a page **existed** as a proxy for whether the world could **hand one over**. Authoring the
ten pages broke the proxy without touching the truth — nothing had become reachable — so the suite accused a
pure content pass of a bug, which is the fourth class arriving by the back door. The repair is not a weaker
check but **splitting the proxy into the two facts it conflated**: pages exist (green, and stays green) and
pages are reachable (still red, goes green on placement, computed from the real closure by
`PagesTheWorldHandsOver`). **So when a gate's message names a future unit, ask which of the two it measures** —
if the sentence would become false by adding a file, it is the calendar, not the mechanism.

**And a fixture built on an authored file fails *open* on every check phrased as a removal.** Renaming one
page's `Id` in `data/` reddened three of four new satchel checks and turned the fourth **green**: *"it leaves
with the last page, putting the strip back"* passes trivially when the tab never appeared. `AddItem` does not
refuse an unknown id, so the fixture silently did nothing and every assertion about the *absence* of something
was satisfied by its never having been present — and the whole family inverts the same way (*"and it goes away
again"*, *"the count drops back"*, *"the flag clears"*). **One assertion that the fixture landed converts all
of them into a red line that names the cause.** Note that the gate is correct here and the `data/` file under
it is what stopped meaning anything, so reading the gate cannot find it.

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

**The mirror of this class, and it cost a shipped bug in unit 5: this rule is about a guard with no reachable
arm, not about a state you failed to enumerate.** A fight starting while the camp menu was open left the menu
behind the fight screen. The guard *had* been considered during the build and **declined**, on the reasoning
that the menu covers the whole screen — and declining felt principled precisely because this section argues
against guards nothing can reach. The coverage claim was true and is now asserted; the conclusion drawn from
it was wrong, because "no arm can be reached" and "I did not think of that arm" look identical from inside
the build. Confusing the two errs toward leaving a bug in, which is the more expensive direction, so when
this class is the argument for *not* writing a guard, the burden is to name the states and say which arm is
unreachable — not to reason from what the screen looks like.

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

**A second corollary, from unit 9a: a break-test that *crashes* the suite is a red that tells you nothing.**
Restoring `CorruptionAura`'s old `0.15f` default — to watch the check written for that exact default go red —
gave every fixture an aura, drained the bar three blocks earlier, banished the player out of the arena, and
left the next lookup dereferencing null. **532 checks ran instead of 1023**, thirteen of them red, and the
line the break-test existed to exercise never printed at all. So predict the **count** as well as the checks:
a break that reddens the right name while halving the run has told you about your fixture, not your gate. The
repair generalises past this one block — **`NavigationManager.FindPlacement` only searches the room the player
is standing in**, so any gate that can navigate away from its own fixture room must guard (`?.Entity` plus one
check that the bodies are still there) rather than dereference. Hardened, the same break gives 15 reds of 983
with the named guard among them.

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

**Unit 9a closed the other one, and it closed with a bug attached — which is the part worth carrying.**
`CorruptionAura` has a reader now (`EncounterManager.ActiveAura`). But the field had been sitting at a
default of `0.15f` the whole time it was unread, and *that* was the live defect rather than the absence:
the moment anything consulted it, every authored creature in the game was a boss. **A field with no reader
is not inert, it is armed** — its default is a decision nobody has had to defend yet, and the first reader
is when the bill arrives. So the eleventh class has a corollary: when you write the reader, **check the
default before you check the logic**, and expect the default to be wrong, because nothing has ever pushed
back on it. The verifier now holds it at `0` with a check of its own, and a census prints who has an aura
on every run.

**Unit 7a half-closed one of those two, and half is the interesting part.** `RequiresBothTracks` now has a
reader — `BelongsToSide` puts the engine on both sides of the journal, and a gate fires it — but `Inspect`
still ignores the field, so knowing one hand is enough to make the engine. **A field can have a reader and
still not bite.** The `grep` that would have found the original problem now finds a caller and returns
clean, which makes the remaining half *harder* to see than it was before. So: when a field's display half
and its refusal half land in different units, say which half shipped, in the field's own doc-comment and in
the plan. Both now do.

**A twelfth, from unit 7a, and it is not about a gate lying — it is about the gate never running.**
`--verify-slice` appeared to hang: two runs killed at five and ten minutes, no section header, no first
check, exit 124. The cause was the command line. `Boot` reads `OS.GetCmdlineUserArgs()`, which returns
nothing before a `--`, so `godot --headless --verify-slice` sets no flag at all — the game boots normally,
and a headless boot with nothing to do sits in an idle scene tree forever. **A suite that never started is
indistinguishable from a suite that hung, and both are indistinguishable from a build you broke.** An hour
went into bisecting code that was never at fault, on the strength of *"the last known-good run was before
these edits."*

The tell is the *shape* of the output, not its content: autoload prints, then nothing. Every gate prints as
it runs, so **zero section headers means zero gates ran**, which points at the harness or the command line
and never at a check. The general lesson is the one `Main`'s own quit-on-bad-starting-room comment already
wrote down for a different cause — *"returning leaves a headless run sitting in an empty scene tree
forever, which reads as a hung machine instead of a broken build"* — and it was written on 2026-08-02 by
someone who then hit the same signature from a new direction three days later. **When a run produces no
output, suspect the harness before the code.**

**A thirteenth, from the *Set* button, and it is the twelfth's mirror: the suite finished, printed
`ALL GATES PASSED (945 checks)`, and then exited 139.** A fatal `0xC0000005` in
`Godot.Collections.Array.Finalize` during shutdown, *after* the summary. **The log said pass and the
process said crash, and nothing in this repo has ever read the exit code** — every session to date has
redirected to a file and read the tail, which is precisely the habit that would have carried this
indefinitely. So: **check `$?`, not just the last line.** A suite whose green is only in its stdout is a
suite that cannot ever be wired to anything.

The cause was not an engine bug, and this is the half worth keeping. Bisecting (disable the new block →
exit 0) pointed at a gate that **navigated between rooms with the satchel panel open** — a state the game
cannot produce, because `Hud.ToggleSatchel` is the only route in and the panel covers the screen while it
is up. **A fixture the player cannot arrive at proves nothing about the thing it is measuring, in either
direction**: had the button misbehaved only in that configuration, the red would have been a lie too. It is
the "fixture that arrives by default" class turned inside out — that one inherits a state nobody chose,
this one constructs a state nobody can reach. The fix was to walk with the panel shut, which is what a
player does, and it removed the crash and made the check honest in the same edit. **When a gate has to
assemble an unusual state to ask its question, ask first whether the game can assemble it.**

**A fourteenth, from the two-hand refusal: a gate can cover a branch and leave its sibling unreachable, and
nothing about the green says which.** `Inspect` refuses when `!KnowsAnyPageOn(Craft) || !KnowsAnyPageOn(Physick)`.
Twenty checks exercised it, all green — and the *first* arm was never reached, because the fixture chain
learns his page in its second line and never lets go of it, so **is his side blank** was only ever asked in
states where it was false. Half the condition could have been deleted with the suite passing. This is not the
eleventh class (a shape with **no** reader) nor a universal over an empty set: the code is read, the checks
are specific, they measure a real refusal — they simply all enter through one door.

It was caught while writing down what each break-test *should* redden, before running any of them: predicting
"delete the Craft arm → 1 red" surfaced the fact that no check could produce that red. **The prediction
found it; the suite could not have.** Which is the argument for the habit itself — a break-test you predict
correctly confirms the gate; a break-test whose prediction you cannot even write is a gate with a hole. The
fix was a second fixture entering through the other door (`verify_recipe_both_hers`, the same shape written
in her hand) plus a probe that runs *before* the main chain and resets, since "knows hers and nothing of his"
is a state you can only be in before you know anything of his. **When a refusal is a disjunction, count the
fixtures, not the checks: N arms need N ways in.**

**A fifteenth, from the 7a–9a read-back on 2026-08-05, and I wrote it myself an hour before it caught me: a
check that asserts an *absence* is worthless against a fixture that cannot produce the presence.** The gap
found was real — deleting `EndPlayerTurn`'s post-drain `if (!InFight) return;` left all 1041 checks green,
so a creature rooms away could strike you at the bench on a turn of a fight that was already over. The check
written for it read *"nothing followed you"* as `vitality.Current == vitality.Max` after the banishment, and
it stayed green with the guard deleted. `Arena()` builds every fixture body in that room with
`Deals = None, Damage = 0`, so `CreatureStrike` returns on its first line: the probe asserted that a creature
which cannot hit anything did not hit anything. Two lines arming the body — and restoring it after — turned
the same check into the one red the deletion deserves.

This is the third class's ("a check that cannot fail") harder half, and the distinction is worth holding: the
third class is a **tautology in the predicate**, visible by reading it. This one has a correct predicate and
an **inert fixture**, and reading it tells you nothing — the words are about damage, the fixture's damage is
in another function, in another file. So: **when a check is phrased as a negative, prove the positive is
reachable first.** Not by reasoning about it — the reasoning here was "`CreatureStrike` has no fight guard",
which was true and irrelevant. Run the break-test. The eighth-class corollary above says a wrong prediction
is a finding; this is the case where the wrong prediction was *mine about my own new check*, and the only
thing that surfaced it was refusing to report a check as written until it had gone red once.

**A sixteenth, and it is the only one here that was already known and simply never written down in this
file: a green run taken after a failed build is the previous build's green.** `dotnet build` failed on one bad
enum name, and the verifier — invoked in the same command line — ran off the stale DLL and printed **ALL
GATES PASSED (1041 checks), exit 0**. This is the *second* time it has been paid for; the first was
2026-08-02, where a build error produced **385 green off a stale assembly** and the rule that came out of it
— **`dotnet build` must be *read*, not chained into the run after it** — went into memory and into the run
recipe, but never into this register. So it stayed invisible to anyone reading §3 for the list of ways a
green can lie, which is exactly what this section is for.

Recorded properly now, with the tell: nothing in the verifier's output says which binary it is, and the
signature is that the count is **exactly** the count from before the edit — which is also what a correct
no-op edit looks like, so *the count cannot distinguish them.* The habit that removes the class is a
`grep -E "error|Build succeeded"` on the build before the run, and the wider lesson is about this file rather
than about the harness: **a rule that lives only in memory will be re-paid for.** Memory is loaded by
description and consulted when relevant; §3 is read start-to-finish before writing a check. A gate-lying
class belongs here, and in memory as a pointer.

**A seventeenth, from authoring the three major repairs on 2026-08-05: a differential check whose baseline is
itself a filtered walk is not measuring the rule it names — it is measuring whatever the *other* filter left
reachable.** §5 of `GateWorldGraph` proves that the two stopping rules on a fresh game are load-bearing, and
it does it the right way in principle: not by asserting a screen count, which content would move, but by
comparing the real walk against a walk that ignores one rule. The bug was in the comparison. *"A creature
standing in a doorway shuts screens off from a fresh game"* read `Walk(wardensBar: false, flagsBar: true)`
against `Walk(wardensBar: true, flagsBar: true)` — **wardens varied, flags left switched on in both**. Every
warden-barred door in the game happens to live in chapter 2, so the hour the ch1→ch2 seam was gated on
`passage.bridge.rebuilt`, both walks stopped at the seam, neither one ever saw a warden, the counts came out
equal and a correct check went red on content that broke nothing.

The failure is not the red — the red was cheap and loud. It is that **the check had never been testing what
its own name said**, and the only reason it passed for three units was that the flag frontier happened to sit
beyond the wardens. Move the frontier and the claim evaporates; and a frontier is precisely the thing content
moves. So the general form: **when a check varies one condition, hold every other condition off, and measure
from a baseline that filters nothing.** Both differentials do that now (`openMap`), plus a third asserting
that the fresh walk is no wider than either rule alone allows.

This is the eleventh class's relative — a default that makes a whole population uniform — seen from the
opposite side: there, one field's value made every creature the same; here, one filter's reach made two
different walks the same. The tell is shared and worth memorising: **when a differential's two sides are
equal, ask what they have in common before you ask what changed.**

**An eighteenth, from placing the two bosses on 2026-08-05: a fixture chosen by a predicate over content is a
fixture content can empty, and when it empties the checks do not go red — they stop existing.** The two-click
repair block ran under `if (open.Data is { } station && …)`, where `open` was
`repairs.FirstOrDefault(r => !Gates(r.Data).Any())` — *the first repair nothing is holding shut.* I wrote that
deliberately and left a comment approving of it, on the grounds that it does not hardcode a room id. Then this
unit put a `RequiredFlag` back on the span and the cart, which were two of the three repairs, and the third was
already gated: **the predicate matched nothing, the `is { }` failed, and twenty checks silently vanished.**
Exit stayed 0.

**The tell is a count that goes *down* while nothing goes red**, and it is a bad tell for two reasons: the
count is printed once at the end where a passing run invites nobody to look, and *"1053 checks"* against
yesterday's *"1073"* reads as a suite that got smaller, not as a suite that got quieter. It is only because
three unrelated content gates went red in the same run that I read the count at all. Had the placement been
clean, this would have shipped and the mend path would have had no coverage at all in a green suite.

Not the eleventh class (a shape with no reader) and not the fifteenth (an inert fixture): here the check is
live, the predicate is sound, and the fixture is *correct whenever it exists.* The defect is that whether it
exists is decided by `data/`. The repair was to stop discovering the state and **construct** it — take any
repair at all, then set its gates false, assert the shut case, then open them — which both removed the
dependency and covered gating that nothing had asked about before. And a fixture that constructs its own state
still needs to say so out loud: `shut.Count > 0` sits in the first assertion for exactly this reason, and the
break-test proved it earns its place. With `Gates()` forced empty, *"a shut repair takes no clicks"* and *"it is
not a board while it is shut"* **both stayed green** — the flag they depend on happened to be false ambiently,
so they passed on state the fixture had not built. Only the non-vacuity guard went red on the thing that was
actually wrong.

So: **a check that runs inside an `if` is a check with a hidden precondition, and content is allowed to falsify
it.** Prefer constructing the fixture to selecting it. Where selection is unavoidable, assert that the
selection succeeded — `Check("there is a repair to drive the two clicks through (3 placed)", …)` is one line and
turns a disappearance into a red. And when a break-test's prediction comes out 2 reds against 3 predicted, the
missing red is the finding: it means an assertion is riding on ambient state rather than on the fixture, which is
the fifteenth class waiting to happen in a block that currently looks fine.

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
