# Handoff — where we left off

Last updated: **2026-08-01.** Read this first, then `docs/combat.md` — which is the source of
truth for combat, Resolve, the Dream and camps, and whose §19–22 are the newest material. **§18 is
the ratified fight design and the authority on how combat works** — it closes the item, trap and
equipment vocabulary that the rest of that document defers, and it deletes Defence as a number.

**If what you need is "how much of everything is there and where does it go," read
[`placement.md`](placement.md)** — the placement bill, written 2026-08-01. Every creature, fight,
pocket, blight, camp, NPC, node, tool and set piece counted per chapter, with the constraint on each,
so the mock-up map can be drawn. It is a rearrangement of `combat.md` §17–23 and `items.md`, not a new
authority: **where it disagrees with them, they win.**

**Nothing is currently known to be broken.** The whole slice plays end to end. Commands live in
`README.md`; Claude runs `--verify-slice` before and after code changes, so there is no start-of-
session ritual to perform.

**Newest change, 2026-07-31: the whole item and crafting economy was redesigned, and it is docs-only
so far.** `combat.md` **§19** is the new authority on crafting, materials, salves, stations and the
catalyst; it supersedes the material economy assumed by §4, §6, §10, §11 and §17, and **reverses two
things §18 ratified** (equipment is no longer destroyed on death; Fire is now fused rather than his).
Every superseded section carries a revision note pointing at §19 rather than being left to be
discovered. **No code has changed and the suite is still 328, exit 0** — the code pass is the next
thing, and §19's last table is the list.

The short version, because it is a large section: **crafting is knowledge, not logistics.** A recipe
is learned by solving a logic puzzle **once**, at a station, and every craft after that is instant.
Materials are a **tally that takes no slots** — eight nouns in the whole game, six common and two
scarce, gated by story milestone with two refills. **Carry limits are deleted** (§18's rig cap already
made that decision, and made it better — informed, in front of the creature, instead of blind at
camp), so the caps are two traps and two salves per fight and nothing else. Her two combat salves are
**ingredients for his traps** rather than things you throw, which makes Fire and Caustic fused damage
types and puts the love story in the tech tree. And her salves are **found first, learned last**: you
ration a finite cache all game and late on earn the right to make them, which is the best single beat
in the design.

**What reversed and why it is worth reading before building anything:** the reason this conversation
happened at all was Jeremy noticing that *"advanced crafting could kill my project with scope creep
and honestly not be as interesting."* §19 opens with the sharper form of that — **crafting depth is
paid for in content forever, a puzzle is paid once** — and every decision in the section falls out of
it. Two things also got *deleted* along the way that had already been designed: the carry-slot economy
with its upgrades, and a per-region Resolve-drain flag. Both were replaced by rules that already
existed, which is the pattern worth noticing.

**Then the whole roster was written the same day: [`items.md`](items.md).** Every material, salve, trap,
weapon, coat and tool, named and costed, with per-chapter availability and the scarce budget. It closed
the last blocking item in M4 — no station can be authored until the material list exists, because every
station is balanced against it. **The chapters are a woodland, a village and a dark mine**, and the
roster prices the Dream at **11 pockets** (8 knowledge, 3 boss), accepted as the authoring bill.

**And the ending is designed: `combat.md` §20.** In the last Dream pocket **the anchor is Iris**. The one
thing to read there before touching combat code is *why the engine does not break invariant 11*: the rule
says cutting the tether is the only way to remove regeneration, and never said the **Dream** was the only
way to cut a tether. The engine is a severing by a third route, not a trap buying the aura off — and it
stays legal only while it is a unique authored object rather than an `ItemCategory.Trap`.

---

## Newest, 2026-08-01 — camps became a menu, hazards became the good ending, and traps became permanent

Still docs-only; **the suite is still 328, exit 0.** **Seven decisions**, and the first one caused the
next four. **`combat.md` §22 is new** and is the authority on how a Waking-world screen works; **§23 is
new** and is the authority on chapter 3's gate.

**Two of the seven reverse earlier ratified decisions**, and both reversals keep the old text
struck-through with a note on why it was right when written: **traps are permanent** (item 6, reversing
§13 and §19) and **chapter 3 gains three pockets** (item 7, amending the *"fewer pockets in chapter 3"*
rule). The pay-to-skip deletion in item 2 also voided a constraint ratified the previous session.

**1. A camp is a menu, not a place (§17).** One camp per chapter, reachable from anywhere, still refilling
both pools automatically. Jeremy: *"the hotspot is a crutch that very weak story RPGs use."* The three
camps are the chapter arc — **ch1 his empty cottage, ch2 the first house you liberate (filling), ch3 a
mine camp with one occupant, and he is dead.** The honest cost is that Waking-world hazards lose their
teeth; §22 is the answer, and it does not try to save attrition.

**2. Hazard is renamed Blight, and mending one is how you get Graveflower (§22).** The chain now runs
**blight → restoration site → Graveflower → the exorcism engine → Iris lives**, so the built hazard
machinery becomes the authoring surface for the **twelve** repairs instead of being deleted. **Mending
costs a puzzle, not materials** — paying makes the good ending a grind, solving makes it an attention
gate. A screen is unfinished for exactly three reasons: **Held** (a creature), **Blocked** (a tool),
**Fouled** (a blight).

> **Amended later the same day — it is a jigsaw, it is required, and the pay-to-skip is gone.** Jeremy:
> *"you cannot fail a slider puzzle"* (so "required" never meant hard, and every condition written to
> protect a stuck player was guarding a wall that cannot exist) and *"no game or event means you look at
> something and get a flower though lol"* (so the puzzle is what earns the flower). A commons skip could
> not have been priced anyway — commons are abundant by rule, so it is either cheap and kills the puzzle,
> or dear and breaks the commons rule. **The out already exists one level up:** blights sit behind optional
> fights, so a player who does not want them skips the optional content and finishes by severing.
> **The three major repairs get a jigsaw too — fifteen in total, one interaction, no branching.** The
> framing that settles it is Jeremy's: *"I thought they would look cool to complete the objects"* — the
> puzzle is not a gate, it is the animation. And the 12 → 8 Graveflower slack now measures **exploration**
> rather than tedium tolerance, which is a better thing for the good ending to hang on.

**3. The lore of it: Daniel repairs, he does not build.** A blight is a *broken made-thing* — a well
cover, a fence, a wheel — and the corruption spreads through the works of human hands left undone. That
kills the idolatry problem Jeremy raised, makes the puzzle and the fiction the same act, and finally
explains the material: **Graveflower grows where the land is right again.**

**4. Three Dream verbs on one meter, and eight upgrades (§19).** **Suppress** from the start, **Float**
after boss 1 (vertical traversal, because ladders in the spirit realm make no sense), **Flare** after
boss 2 (reveals the hidden and makes everything see you). **Each of the eight knowledge pockets raises
maximum Resolve.** Maximum Vitality never does — it is not Attunement, and §18 deleted scalar defence for
the same reason. Boss drain stays an **authored per-boss value**, not a formula; the six-turn figure was
always arbitrary.

**5. The good ending is now "you went back for it" (§20).** Menu camps made backtracking nearly free, so
the engine is reachable after you learn about it. The chain is **the Blacksmith's spirit gives the
principle → an optional pocket guarded by the breach-thing gives the blueprint → twelve mended blights
give the material.** The ghost never knows the engine exists as a buildable thing, which is what stops
him functioning as a hint. A player who mended as they went finishes at the climax with no backtrack; a
player who skipped gets a real second chance. Neither is punished.

**Also settled:** **Swarm is cut** from §18 (battles are 1v1 always — the Hunter is demoted to an ordinary
path creature, and the encounter body-list stays as unexercised insurance, **not** to be gated). The
**Beast is 3 on the path + 3 optional** with **three visual variants** sharing one damage pair. The two
material refills hang on the **ch1 and ch2 boss pockets**; chapter 3 has no refill and uses the three
optional Beasts as its valve.

**Then the placement bill: [`placement.md`](placement.md).** Every creature, fight, pocket, blight, camp,
NPC, node, tool and set piece counted per chapter with the constraint on each, plus a derived screen bill
(**~58 Waking screens, 14 pocket interiors, 3 camp scenes**) and a **fifteen-rule checklist** to hold a
drawn map against. It is a rearrangement of §17–23 and `items.md`, **not** a new authority.

Building it turned up three things that did not follow from the counts underneath them, and the third is
not cosmetic:

- **It is 24 people freed, not 27.** The 27 assumed nine creatures × three sightings. The Beast is seen
  **six** times and frees nobody, so it is `8 human creatures × 3 = 24`, and `24 + 6 = 30` fights.
- ***"The optional sighting guards a repair site"* cannot be one-to-one** — ch2 has 3 optional fights and 4
  blights. Resolved by **one blight per chapter being gated on that chapter's tool** instead of a fight.
- **~~⚠~~ The Cairnstone budget could softlock the game — `placement.md` §8. FOUND AND FIXED THE SAME
  DAY.** The **Deadfall cost 1 Cairnstone and was a consumable**, which is exactly the mistake
  `items.md` §2 removed from the salves (*"a fixed world budget cannot supply a consumable"*) — it was
  just sitting in the trap table instead. A player who rigged five Deadfalls in chapter 2 could not forge
  the **Bell Maul**, which the chain needs before the Apothecary, and the refill does not arrive until
  after that fight. **That was a dead save, against invariant 12.** Fixed by item 6 below — traps are
  permanent, so there is nothing repeatable left to overspend.

**6. Traps are crafted once and kept forever (`items.md` §3, `combat.md` §19).** Jeremy's proposal, and
it reverses a decision `combat.md` had rejected twice — the struck-through rejections are kept at §4,
§13 and §19 because they were **right when written**. What changed: §19's rule that a common must never
refuse a craft had already made five of the six traps effectively unlimited, so *"consumed"* was
bookkeeping rather than cost. Counting Deadfalls was also the last inventory mechanic in a game that had
deleted carry limits, moved the loadout to the rig screen and turned camps into a menu.

> **The knock-on that matters most: invariant 1's overkill column is now *her salves*.** Jeremy: *"it
> makes salves the biggest finite resource and it should be — there are ways to win without using
> salves, so to me that is why it works well."* **A fight can now take nothing from you at all** except
> what you choose to spend; the honest cost is that death is free, and the recorded fallback if it plays
> too soft is *a trap that fires is spent for that fight and returns afterwards* — **not** death
> penalties. The rig cap of two is untouched and is where the real scarcity lives.
>
> **Two follow-ons.** The **Cairnstone budget was cut 3 / 5 / 8 → 1 / 3 / 2** (16 stones to 6, against a
> whole-game demand of exactly 4: Bell Maul, Smock, Mine Pick, Deadfall) — Jeremy's target was *"rare
> mats should be for the equipment, one time, and the amounts should line up or be over a little."* And
> the commons split in two: **Bitterroot and Tallow live all game** (salves), while **Heartwood, Scrap
> Iron, Cured Hide and Pitch go dormant** once the kit is built. The answer to *"does this open room for
> more items?"* was **no** — the fix is fewer sources, not invented sinks. Beast carcasses now drop
> **Tallow only**.

**7. Chapter 3 gets a gate: the Snarl (`combat.md` §23 — new).** Jeremy spotted the structural hole:
chapters 1 and 2 put a **major repair** between you and the boss (the bridge, the mine cart) and chapter
3 had nothing. **The demon has knotted the way shut.** Three **cords** run out of the Snarl into the
rock; follow each to a door and cut its **knot** in a hunted Dream pocket.

> **"Anchor" was his word and it is already taken** — an anchor is the object a tethered soul is bound
> to (§21), and Iris's is the open question the finale turns on. **"Knot" is better anyway:** the demon
> tethers a person and knots a *way*, same craft, and your answer is the same verb — you cut it. "Force
> field" was the wrong register in a game whose vocabulary is rope, iron and rot.
>
> **Three, because there are three Dream verbs and this is their exam** — the **lit knot** (Flare: the
> light that shows it to you shows you to the pursuer), the **high knot** (Float: the climb is paid out
> of the hiding), the **far knot** (Suppress: you cannot see it until you are on it). **All three are
> committed** — Jeremy: *"Want them to beat all 3."* Four is not on the table, since there is no fourth
> verb. **If the 3D bill hurts, the lever is size, not count** (a pocket is seen once and asks one
> question — shrink it); the earlier "build two" fallback is withdrawn.
>
> **What it costs: three pocket interiors (11 → 14) and one Waking screen.** What it does *not* cost —
> all checked — no max-Resolve upgrade (**eight pockets, eight upgrades** survives), no jigsaw or fourth
> major repair (a knot is **cut**, not mended; fifteen mends stays fifteen), no fight, no creature, no
> material, no craft family. The doors sit on three existing critical-path fight screens.
>
> **The *"fewer pockets in chapter 3"* rule is amended, not reversed.** His objection was to being stuck
> in the Dream ***thinking***; knot pockets are chases. So: **fewer knowledge pockets** (still two, the
> fewest of any chapter) **and more action pockets.** Jeremy: *"demons in the mine, more action-packed
> dream worlds, breaking the knots to do the final battle."* Side effect — "hunted" stops meaning "boss";
> the two kinds of pocket are properly **quiet and hunted**.
>
> **What a cut knot pays — closed the same day.** Jeremy: *"So yeah just more falling action / narrative
> I agree."* The slot was empty **by construction**: a shortcut is worthless (camps are a menu), a power
> is spoken for (eight pockets, eight upgrades), and a material is banned. What is left is the currency
> the Dream already trades in — **knowledge**. So each cut shows Daniel the *binding itself*, which he
> has never seen up close, and which he can read because he is a maker: **the work → the hand → where
> else he has seen it.** Zero systems, and it retro-justifies the Snarl (the demon would rather you did
> not arrive knowing). **Three guards:** the beat hangs off the **ordinal**, not the pocket, because the
> order is free; it must **not** close §21 (*what* Iris is tethered to stays open — name the recognition,
> not the object); and it hands over **no object of any kind**, or §1's budgets reopen. **The beats are
> a three-act structure that only works at three**, which is what closed the two-knot fallback: cutting
> one would not remove a third of the content, it would remove the middle term of an argument.

---

## 2026-07-31 — the bestiary, and the ending reversed

Still docs-only. **`combat.md` §21 is new and is the authority on who survives a fight**, and
[`items.md`](items.md) **§10 is the creature roster**. Three things changed that invalidate earlier
reading, so they are called out rather than left to be discovered:

**1. Severing Iris now kills her, and §20's route table was rewritten.** The previous version had
severing as the sad-but-survivable route. It is not: **the tether has two ends**, severing destroys the
anchor, and the anchor is her. So there are two ways to lose her and one to save her, and **the exorcism
engine is the only good ending**. This is not a change of mind — it is §21's rule applied consistently,
and §20 keeps the superseded version visible so the reasoning is traceable.

**2. Every corrupted creature is a person, and one rule decides who lives.**

> **Tethered dies. Untethered survives.**

The demon personally tethered three things — the Trapper's corpse, the Blacksmith, Iris. Everything else
is spillover, and beating spillover just frees the person. So the nine regular creatures all survive, the
bosses all die, and the escalation is *Trapper (says nothing, already dead) → Blacksmith (begs you) →
Iris*, which is how the player learns the cost of the ending before they have to choose it.

**3. Graveflower is the exorcism engine's material and nothing else.** A real arithmetic bug: the first
roster spent a **12-unit fixed world budget** on three *consumable* salves. Fixed by moving both damage
components onto commons and onto **two freed apothecaries in chapter 2**, and pricing the engine at
**8 of 12**. Knock-on: **chapter 2 is now the arsenal chapter and chapter 3 is the construction
chapter** — see `items.md` §7.

**The roster is 9 regular creatures + 3 bosses = 12**, because four damage types give exactly twelve
unique (vulnerable, resistant) pairs and a thirteenth would have to repeat one. Each creature appears
**three times** — two on the critical path, one guarding something optional — so **30 regular fights and
3 boss fights**. Each of the twenty-seven human fights frees a different named person; the corrupted form
repeats and the recovered one does not, which is the theme rather than a compromise. *(The Beast is the
exception at six: **3 path + 3 optional**, three visual variants, one damage pair.)*

**4. Four smaller things settled later the same day, each of which changes something built:**

- **The Blacksmith suspects and does not care; Daniel does not know until he surfaces.** The first draft
  gave Daniel the knowledge going in, which broke the escalation table — if he already knows, the
  Blacksmith teaches him nothing. He settles his affairs (*"there's a key under the anvil"*) so the
  player can tell in hindsight, and his last word is **"Thank you…"** Jeremy: *"this isn't on Daniel, it
  is on the demons"* — the death has to read as tragedy, not as punishment for playing correctly, three
  hours before §20 asks for the real choice.
- **An anchor is an object of the person's trade, never person-shaped and never with a face.** The
  Trapper's is **a snare, sprung and empty** — his trade, his whole character in one silhouette, the
  cheapest model in the game, and it makes the verb literal: the first thing you cut is a rope. Iris's
  specific object is left open.
- **The Beast is seen five or six times, not three**, increasing as the mine goes deeper, so chapter 3
  runs ~12 fights against 9 elsewhere. **Chapter 3 is rising action, not falling** — the Dream is where
  you go to think, and the climax should not be thinking.
- **The exorcism engine needs its own knowledge pocket**, which the first bill priced at zero. The bridge
  and cart take their memory from the chapter's boss pocket; the engine cannot, because you build it
  before the final fight. It is also **the only recipe requiring both tracks** — the first real use of
  `RecipeData.RequiresBothTracks`. Hence 11 pockets, not 10.

**What this changes for code, when the code pass happens** (none of it urgent, all of it cheaper before
content exists):

- `EntityData.SpiritTexture` / `ShatteredTexture` / `EchoTexture` are **dead** — written when both realms
  were 2D. Delete them and fix the class comment that still says "six in total".
- `EntityData.HarvestItemId` is **unused, and stays unused even though Beasts drop materials.** The drop
  is a **harvest hotspot the room places, gated on the fight's flag** — exactly mirroring the NPC hotspot
  a freed person spawns as. Same machinery, no new fields, and two Beasts can drop different things
  without two `EntityData` files. The rule: **you can only harvest what was never human.**
- **A freed human is an NPC, not an entity state.** Same hotspot-and-flag machinery as above.
- **Bosses have two forms, not three:** blank `PurifiedName`. They are destroyed, never purified.
- `data/entities/beast_giant_rat.tres` is the old model (an animal, with both a Dream pocket and a
  purified harvest — now a boss-only combination). Jeremy: *"the rat was just a test, no real loss
  here."*

**Creature *data* is unblocked and creature *art* is not.** The types, counts and placements in
`items.md` §10 are settled enough to author `.tres` files against; what is still waiting is the
billboards-or-models call for the Dream, and the commission itself. §21's art brief: commission one full
character and a style sheet, approve, then order the rest — and **silhouette carries everything**, since
nine trades have to be tellable apart at 80 pixels with no detail legible.

**Newest change: the 3D Dream is walkable.** `scenes/dream/DreamProbe.tscn` — open it, press **F6**,
click to look, `WASD` to walk, and scrub the look with the keys listed in "Done: unit 1 — the probe"
at the end of §1's Dream section. It is the look shader, the 480×270 3D viewport, the first-person
controller and the lighting model, in one scene **wired into nothing**, so the suite is still **328,
exit 0**. What it is for is being looked at: the ramp's numbers are yours to settle by eye, and
nothing about them is decided. Steps 4–6 — the greybox pocket, the creatures and environmental
warding — are still plan.

**Before that, the Dream's direction was settled, and it moved to the front of the queue.** The
**Waking world is first-person in a 2D world** and the **Dream is first-person in a 3D world** —
that part is closed. The only thing still open is whether the *creatures* in it are 2D billboards or
3D models. See **"The Dream in 3D"** in §1 for the startup order, which is what Jeremy asked to have
written down, and open question 8, which is now a far smaller question than its old name made it
look.

**Also 2026-07-30: the wound ledger is built.** `CombatManager` is autoload **#11** — the 2× / 0.5×
resolver, per-instance damage with the severed/anchored asymmetry, the derived condition ladder, and
the save round trip. 39 new gates → **328**. It is the arithmetic half of a fight only: **no turn
machine, no UI, no way to start one yet.** That is the next unit. Earlier the same day: the combat
data shapes (`DamageType`, `EquipSlot`, `KnowledgeTrack`, `ItemCategory.Trap`/`.Equipment`, the
combat `[ExportGroup]`s, `GameConfig.RigSlotsPerFight`); before that camps and the inventory panel
(`CampManager` as autoload #10, the shared cache, crafting gated to a bench, the 0-Vitality collapse;
`scenes/ui/InventoryPanel.tscn`, the shared `PanelTheme.tres`, `UseBlock`/`CanUse`, `I` to open).

**The shed is the first camp.** `shed_bench` in `waystation_shed.tres` was already authored as a
`Craftsman's Bench` — it was `Kind = 4` (Interact) and is now `Kind = 7` (Workbench), which is the
whole change. Nothing was lost: `InteractionService` handles `Workbench` and still sets
`waystation.bench_found` and prints the same examine text. So crafting, the cache and the respawn
destination are all live in play, and **the last thing this file said was waiting on Jeremy is done.**
Put more benches wherever they belong — the suite is proven to stay green with one in any room.

**Build order changed 2026-07-30: all systems first, then all content in one hand-authored pass.**
That un-blocked the item vocabulary conversation — it gated authoring, and authoring was deferred.
The next system is the **crafting panel**. See §1, "Next".

**And then, later the same day, the fight was designed and the vocabulary settled anyway** —
`combat.md` §18. The reason it stopped waiting is worth knowing: it was never blocked by the content
pass, it was blocked by the *grammar*, and combat is the grammar. The single most useful thing to
come out of it: **rig sites hang off the creature, not the room, so `RoomData` and `HotspotData` gain
nothing from combat and Waking-world room authoring is unblocked now.** Laying out, connecting and
decorating rooms cannot be invalidated by the encounter loop. Dream rooms are still on hold, for an
unrelated reason — see "The Dream in 3D" in §1.

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

> **The first of those was written after all, once the bench existed** (2026-07-30, same day).
> `waystation_shed` became a camp, and *then* "at least one authored room is a camp" went in, paired
> with "and you can walk to one from where the game starts". The reversal is not a change of mind
> about the rule — it is the rule working. A gate asserting authored content is a to-do list while
> the content is missing and an invariant once it exists, and the two are told apart by *when* you
> write it, never by what it says. The paired reachability check is the one that earns its place: a
> bench in a room with no door into it would silently remove crafting from the entire game, and every
> other camp gate would stay green because they stand on synthetic `verify_` rooms.

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
salves, reagents, traps, equipment. That was blocked on the type vocabulary, because sections are a
claim about what the categories *are* and drawing them early would freeze a guess. **The vocabulary
landed on 2026-07-30 (`combat.md` §18) and `ItemCategory` grows by exactly two — `Trap` and
`Equipment` — so the sections are now knowable.** Still worth doing after the crafting panel rather
than before it, but no longer waiting on a decision.
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

The consequence that mattered at the time: **the item/trap/equipment vocabulary stopped blocking.**
It was blocking only because it gated authoring recipes and items, and authoring became deferred by
choice. Systems can be built against the *shape* of an item (a category, a cost, an effect hook)
without knowing which items exist — which is already how `ItemData` and `RecipeData` are written.

**Then the conversation happened anyway, on 2026-07-30, and the vocabulary is settled** —
`combat.md` §18. What made it possible earlier than planned is that the blocker was never the content
pass but the **grammar**: "what does this trap do" has no answer until turns exist, and once the turn
rule was ratified every remaining question had a unit to be answered in. So the ordering lesson is
narrower than "content last" — it is *settle the rules before the nouns*, and combat was the rules.

So, systems still unbuilt, in the order they will be done. **Reordered 2026-07-30 — the 3D Dream
moved from last to first**, for the reasons in §1a:

1. **The 3D Dream prototype.** Detailed startup order in **"The Dream in 3D"** below. First for two independent
   reasons, and it would be worth doing on either one alone: Jeremy wants something to *show* — to
   his wife, and to the artist he intends to recruit — and this list already called the Dream's
   direction *"the one remaining architectural risk, and the only item here that could invalidate
   work already done."* Showing it and de-risking it are the same task.
2. **Combat** — the turn loop, traps resolving, the six-turn nerve clock. Still the largest by far,
   but **no longer the one with the most unratified design under it**: `combat.md` §18 settled the
   staging, the turn currency, damage types, equipment and the archetypes. Three increments, in order:
   ~~the **data shapes**~~ ✅ *done 2026-07-30 — three enums, two `ItemCategory` members, the combat
   `[ExportGroup]`s and `GameConfig.RigSlotsPerFight`; **item and creature authoring is unblocked***;
   ~~the **wound ledger**~~ ✅ *done 2026-07-30 — `CombatManager`, the multiplier, per-instance damage
   with the severed/anchored split, the condition ladder, the save round trip*; then the **encounter
   state machine** — the only piece left, and the details are at the end of the wound-ledger section
   below.
3. **The station layer, then the crafting panel.** ~~The same job as the inventory panel with nothing
   new in it.~~ **Changed 2026-07-31 by `combat.md` §19, and this item grew.** The panel is no longer a
   recipe browser — it is a **station screen**, and the manager work in front of it is now the larger
   half:
   - **The shapes first**, per §19's last table: `HotspotKind.Station`, `RecipeData.RequiresStation` /
     `RequiresBothTracks` / `SetsFlag`, `CraftBlock.NotAtCamp` → `NotAtStation`, `ItemCategory.Reagent`
     → `Component` with key items unified, `CountsAgainstSlots(ItemCategory)` as a static sibling of
     `SurvivesDeath`, `SurvivesDeath` gaining `Equipment`, and a salve cap beside `RigSlotsPerFight`.
     **Added 2026-08-01: `SurvivesDeath` must also return true for `Trap`** — traps are permanent
     (`items.md` §3), so **`Salve` is the only category a fight can consume.** A crafted trap also wants
     to be an **owned flag rather than a stack**: the bench should refuse to build a second one instead
     of letting a count climb. That is the shape most likely to be got wrong by copying the salve path.
   - **Then the puzzle seam**, which is deliberately thin: solving a puzzle writes to
     `DiscoveredBlueprints` and nothing else, so `Craft(recipeId)` is unchanged and `--verify-slice`
     never touches a puzzle. **Keep it that way** — a score crossing that seam would put a game rule in
     the one layer the gates cannot reach.
   - **Then the panel**, in the editor pass with the others.

   Good news for sequencing: **materials being a tally rather than slot items makes this smaller than
   the old plan**, and deleting carry limits deletes slot logic, slot upgrades and the stash UI outright.
4. **The journal's knowledge model**, then its UI. **Audio last** (see memory). Note §19 gives the
   journal a second job it did not have: fused recipes need **both** knowledge tracks, so the two sides
   now interact instead of running parallel, and the panel has a third state to draw — *known on one
   side only*.

**What moving the Dream to the front costs, stated so it is not discovered later.** `CombatManager`
sits with no caller until item 2 lands, so the arithmetic of a fight stays unexercised by anything
except its own gates. That is a real cost and a small one — gates do not rot, and the 39 that cover
it were break-tested. Nothing else in the list is disturbed at all, because **combat is Waking-world
only and so are traps** (`combat.md` §4), so the two tracks cannot invalidate each other. That
independence is most of why combat was built first, and it is now paying out in the other direction.

> **Widened 2026-07-30: the editor pass now includes the panels.** Jeremy's call — *"build all of the
> systems then implement the in-editor stuff… I'll have to kind of learn everything then but it will
> be worth it."* So `CraftingPanel.tscn`, the combat UI and every room are **one pass at the end**,
> not a parallel track. The list above is therefore Claude's list, in order, and none of it is
> "waiting on Jeremy" any more.
>
> **What that costs, stated plainly so it is not discovered later: nothing is played until that
> pass.** The gates stop being a safety net and become the only verification there is — and green
> gates never proved playability (see §5). Two rules follow for everything built from here:
> anything a played fight would have settled must be built **easy to change rather than ratified**
> (the brace verb and the two-rig-slot count are the live examples), and **a system is not finished
> until its node contract is published**, because the editor pass is only mechanical if every
> contract is already waiting for him.

The crafting panel's *manager* half is already done and its contract is published (§2, and it needs
no change for `NotAtCamp` — `%RecipeStatus` carries any `CraftBlock`). Its `.tscn` moves to the
editor pass. `waystation_shed` holds the first `Workbench`, so a craft there is no longer refused for
want of a venue whenever that panel does get built.

**What Jeremy can author in parallel, as of 2026-07-30:** all Waking-world rooms — layout, Navigate
hotspots, Examine text, hazards, harvest nodes, and more benches. Combat adds nothing to `RoomData`
or `HotspotData`, so none of that can be invalidated by items 2–3. **Items and creatures are now
open too** — the shapes landed, so a `.tres` written today will not need reopening; fill in
`VulnerableTo`, `ResistantTo`, `Deals` and `Durability` on a creature and `Deals`/`Slot`/
`WardsAgainst` on a weapon or trap, and the content sweep will tell you if a combination is inert.

> **One placement rule, ratified 2026-07-31 and restated the same day, and it cannot be gated so it has
> to be remembered.** It used to name chapters — *"in chapters 1–2, no Fire- or Caustic-vulnerable
> creature on the critical path"* — which stopped being right when both types became learnable inside
> chapter 2. It now says what it was always protecting:
>
> **A creature is only placed on the critical path if the player can already make the type it is
> vulnerable to by the time they reach it. Otherwise it guards something optional.**
>
> In the shipped roster that binds exactly one creature — the chapter 1 **Messenger**, which is
> Fire-vulnerable in a chapter with no Fire, so **all three of its sightings are optional** rather than
> the usual two-path-and-one. The chapter 2 Blacksmith looks like a violation and is not: he is
> Caustic-vulnerable, but the apothecary who teaches Corrosive Draught is on the path ahead of him.
> **Each chapter teaches the answer to its own boss** — that is the intended shape, not an exception.
>
> On an optional route it is a **locked door you can see through** — fight it expensively if you like
> (the wrong type halves damage and never zeroes it, and flight is guaranteed), or note it and come back.
> That reads as anticipation. On the critical path the same creature reads as friction; Jeremy's own word
> for the risk was *"annoying"*, and annoying is a smell rather than a difficulty setting.
>
> **It also earns the backtrack.** Restocking chapter 1 after the chapter 2 boss was a walk through
> cleared ground with nothing in it. Now the creatures you could not kill are still standing there and
> you can — **the restock trip and the come-back-later trip are the same trip.** Neither was designed to
> make the other work.
>
> No gate can check this: "is this creature on the critical path" is a judgement about level design, not
> a property of a `.tres`. It belongs in the same category as the unfalsifiable guards §5 says to
> document rather than gate.

Two things still to hold off on, and the reasons are now different from each other:

- **Dream rooms.** Still on hold, but no longer *pending a decision* — pending a format. A
  first-person 3D pocket has no use for `RoomData.DreamBackground`, so the 2D Dream authoring model
  is going regardless of how the creature question lands. The hold now has a definite end — when
  step 4 of "The Dream in 3D" produces a pocket format — rather than a decision-shaped one.
- **Creature art**, though not creature *data*. `EntityData`'s combat fields are safe to fill in
  today — and as of 2026-07-31 there is a roster to fill them *from*, in [`items.md`](items.md) §10:
  twelve entities with their vulnerable/resistant/deals triples and chapter placement already settled.
  What a spirit *looks like* waits on billboard-versus-model, because that call decides whether the
  deliverable is a sprite sheet or a mesh. This is the only thing open question 8 still blocks, which
  is a much shorter list than it used to be.

### The Dream in 3D — the startup order

Written 2026-07-30, at Jeremy's request, when the direction settled. ~~**Nothing below is built.**~~
**Unit 1 is built** — steps 1, 2 and 3's floor, plus the lighting model, as one scene. See
"Done: unit 1 — the probe" at the end of this section. Steps 4–6 are still plan.

**The steps below are the right order and the wrong grouping, which is worth knowing before
following them.** Numbered 0–6 they read as seven deliverables; three of them cannot be delivered
separately, because a posterise pass with nothing behind it, a viewport with nothing in it and a
walker in an unlit void are each unlookable-at. The regrouping actually used:

| | | |
|---|---|---|
| **unit 1** | steps 1 + 2 + step 3's floor + the lighting model | ✅ built |
| **unit 2** | step 4, and a throwaway step 5 inside it | next |
| **unit 3** | the seam into `Main`, its gates, the atomic grant-and-consume | |
| **unit 4** | rigid bodies + step 6, once a puzzle names one | |

Two of those regroupings are judgement calls rather than tidying, and both are argued where they
land: **the lighting model is half the look** and so cannot follow the shader (below, before step 1),
and **step 5 is cheap and gates someone else's work**, so a throwaway version belongs inside unit 2
rather than after it (below, at step 5).

**The settled part, in one line:** the Waking world is first-person in a 2D world; the Dream is
first-person in a 3D world. The open part is whether the creatures in it are 2D billboards or 3D
models.

> **The old name for this question was wrong, and the wrong name cost real time.** "2.5D versus full
> 3D" reads as a question about the *world*, and on that reading it blocked the entire Dream
> direction and every Dream room with it. But both answers always had 3D geometry — the memory that
> recorded the 2.5D plan describes *"3D geometry with pixel-art textures, and the hostile entities as
> 2D sprites"*, and "full 3D" only ever meant modelling the creatures too. So the question was never
> about the world at all. Restated correctly it blocks **creature art and nothing else**. Worth
> keeping as a general lesson: *a question named after the wrong noun blocks the wrong work*, and it
> does so silently, because nobody re-reads a deferral to check what it is actually about.

**Two reasons this is first**, and it clears the bar on either one alone. Jeremy wants something to
show his wife and the artist he means to recruit, and an artist cannot be recruited to a text
document. Separately, this file has called the Dream's direction the one remaining architectural risk
since before that conversation. The showable thing and the de-risking thing are the same build, which
is unusually lucky and is why it is worth taking out of order.

#### The order, and why each step is where it is

**0. The value ramp — how many greys, and what pure white is reserved for.** This is the foundational
decision, and it is the one everything downstream inherits. Jeremy's direction, 2026-07-30: the world
sits in **black through mid-grey**, and **points of interest carry white**. So white is not a colour
in this game, it is a *signal*, and the ramp has to be designed around that reservation rather than
having it applied afterwards by discipline.

**Enforce the reservation in the shader, not in the art.** Clamp ordinary environment surfaces below
1.0 so geometry physically *cannot* reach white, and let only flagged or emissive materials hit the
top of the ramp. This is the same move the project already makes twice — camp-ness derived from the
bench rather than a flag, `SurvivesDeath` keyed on category rather than a per-item opt-in — and it
has the same payoff: a mistake becomes impossible rather than merely discouraged, and dropped-in
greybox is on-style automatically.

**Ratified 2026-07-30 — the ramp is semantic, in three tiers:**

| | |
|---|---|
| **Creatures** | the **darkest** thing on screen — a hole in the world, not a highlight in it |
| **Items and points of interest** | reach for **white**, and are the only things allowed to |
| **Everything else** | the middle, and only the middle |

This settles the collision that would otherwise have appeared later: the art direction says to hold
*enemies and interactables* at full contrast while Resolve degrades the rest, and if white meant both
"take this" and "run from this" the signal would mean neither. Using both ends of the ramp gives each
its own channel, and putting creatures at the bottom is better horror than putting them at the top.

**The authoring consequence is real and easy to miss: the world gets a compressed value band.** With
both extremes reserved, environment surfaces live in roughly the middle of the ramp — a narrower range
than it sounds. So **the dither pattern, not the value, has to do most of the texture work.** That is
a constraint on how the shader is tuned in step 1, not a limitation to design around later.

**How many greys — start low and raise, but the three tiers set a hard floor.** Jeremy's instinct to
start low and go up as needed is right, and it is free *because the assets are greyscale*: the source
data always holds more than the ramp shows, so raising the level count later costs nothing, while
authoring against a generous ramp and then posterising down destroys lighting you have already tuned.
Same reversibility logic as authoring greyscale and dithering in the shader.

But do not start at the bottom, because the semantic ramp constrains it arithmetically: one band is
spent on creatures and one on white interactables, so **at four levels the entire world gets two**, and
two bands cannot separate an object from the wall behind it — which is the only job value has here.
The floor is roughly **two reserved plus three or four for the world, so six to eight**. Two practical
notes: **level count and dither strength trade against each other** (strong dithering between adjacent
bands simulates the values in between, so a heavily dithered six can read richer than a clean twelve),
so tune them together rather than in sequence. And treat a creeping level count as a **symptom** — if
things are hard to tell apart, the cause is almost always light placement, not the ramp, and raising
the count buys legibility at the price of the look. Make it a live parameter bound to a key in the
prototype and scrub it; this is a screenshot decision, not an argument.

**The renderer is a smaller question than I first wrote, and Jeremy is right about why.** The plan was
to switch off `gl_compatibility` for volumetric fog, SDFGI, SSAO/SSIL and SSR. But at 480×270 through
a posterise pass, **that list is mostly thrown away**: bounce GI quantises into a couple of grey
bands, a volumetric shaft becomes a chunky gradient that a fog term and one textured quad reproduce
more controllably, shadow-map artifacts vanish into the pixel grid, and reflections are irrelevant in
a dithered monochrome space. Compatibility is a reasonable default, it is faster, and it keeps the
web export alive. The one thing that would genuinely reopen it is **many small lights at once**, since
Compatibility caps lights per object — so revisit it if a pocket ever wants a dozen, and otherwise
leave it alone.

**What does not change is that monochrome makes light *placement* matter more, not less.** With no
colour, value is the only tool left for separating an object from the wall behind it, and value comes
from where the lights are. So the craft is cheap lights placed deliberately and posterised into
distinct bands — which is authoring, not a rendering backend. That inversion is the useful thing to
hold on to: *monochrome raises the value of lighting and lowers the value of expensive lighting
features at the same time.*

#### The lighting model, decided 2026-07-30: the player carries the near field, placed lights own the far field

Jeremy: *"I think I want the lighting to be connected to the player so things appear as you get closer
to them… And small lights to give you some heads up."*

**Both halves are one system, and it is the right call for a reason beyond cost.** A player-carried
light means **a pocket costs zero lighting authoring** — build the geometry, walk in, it is lit. For
one person authoring every room that is the difference between lighting being a per-room task and
lighting being free. It is also the horror-native model: darkness as a limit on *knowledge* rather
than a limit on vision, and it makes approach itself the reveal.

**But the near-field light and the far-field lights are not two ideas — the second one repairs what
the first one breaks.** A player light alone makes everything past its radius uniformly black, and
uniform black destroys **silhouette**, which is the only thing carrying navigation in a realm with no
colour and no minimap. It would also quietly break two things already written down: the *distinctive
landmark per pocket* in step 4 (a landmark you cannot see across a room is not a landmark), and the
white-for-points-of-interest reserve (a white interactable outside the radius is as black as the wall).
Placed lights fix all three at once by making the far field a set of **islands** rather than a void —
you steer between lit points, and the lit points are exactly where the things worth walking to are.

That turns light placement into an **authoring verb** rather than a lighting chore: putting a small
light somewhere says *this matters, come here*. Same derived-from-content shape the project already
uses — a camp is the room with the bench, and `SurvivesDeath` is keyed on category.

> **The catch, and it is the one worth deciding before a pocket is authored: three separate systems
> would now encode distance as value.** Light falloff, dither-rising-with-distance (step 1), and the
> Resolve degrade all say "further away → less readable". Stacked naively they do not reinforce, they
> mush — the far field goes to flat black and *none* of the three is legible as a signal. **Pick one
> owner for distance.**
>
> **Decided 2026-07-30: falloff owns distance, dither owns texture, and the Resolve degrade is a
> shrinking light radius.** Jeremy: *"I like this option to lower the lighting instead of trying to
> balance more complex things."* Each cue now means exactly one thing, the dither becomes the Resolve
> channel and nothing else, and the degrade gets more diegetic than a global ramp — the dark closes
> in on you. It is also a stronger version of the death spiral already designed: touched → Resolve
> lost → the light shrinks → you see less → you get touched more.
>
> **What this deletes is the point.** Dither-rising-with-distance comes out of the shader (falloff
> already does that job), and the Resolve degrade is not a shader parameter at all — it is one
> number on the player's light. Step 1 drops from five parameters to three. Fewer knobs, and no two
> of them fighting over the same pixel.

**Two consequences for the ramp above, both cheap now and expensive later.**

- **A player light is in direct tension with "creatures are the darkest thing."** That rule works
  beautifully in the near field, where a black shape occludes a lit wall — the creature is visible
  because it *blocks* light, which is the best possible version of the idea. But past the radius, a
  black creature in black space is not a subtle threat, it is nothing at all. **Fix adopted
  2026-07-30: rim light on creatures specifically** — a thin bright edge on a black body. That reads as
  a hole in the world with an outline, stays findable in the dark, and it is already in the art
  direction's compensate-with-value list. It is also the natural thing to hold at full strength while
  everything else degrades, and it is the one exception to the white-clamp: a creature's rim is allowed
  near the top of the ramp even though its body sits at the bottom.
- **Compatibility gets safer, not riskier.** One light on the player is trivial in any backend, and
  *sparse* placed lights stay under the per-object cap. The reopen condition stated above — a dozen
  small lights in one pocket — is now a thing to watch during authoring rather than a hypothetical.

> **Therefore the lighting ships with the shader, in the same unit — added 2026-07-30 while building
> it.** This section sits before step 1 and reads like context for a later authoring pass, but the
> decision inside it makes that impossible: *falloff owns distance*, so all value separation comes
> from where the lights are. Posterise tuned against a flatly ambient-lit greybox would be tuned
> against an image the game is never going to show. It is not a fifth step either — a player light
> and two placed ones is about six lines of scene, which is the whole point of choosing that model.

**1. The look shader — the highest-impact thing in this list.** A screen-space pass on the Dream
`SubViewport` doing greyscale, posterise and dither. The art direction already settled the pipeline
order and it makes the hard choice free: **author greyscale, dither in the shader**, because
greyscale posterises down to 1-bit trivially and 1-bit can never be recovered upward. **This is where
step 0's ramp is implemented**, so it carries five parameters: posterise levels, the environment
white-clamp that keeps pure white reserved, dither strength, dither-rising-with-distance, and later a
degrade amount driven by Resolve. Note the lighting decision above may retire the fourth and move the
fifth out of the shader entirely — if falloff owns distance and the Resolve degrade is a shrinking
light radius, this pass keeps only posterise, clamp and dither strength. Build it with all five, then
delete what the lighting makes redundant once a pocket is walkable; that ordering is cheaper than
guessing.

Two things it buys beyond looking good. Dither density rising with distance is a **depth cue**, which
is the one real craft problem monochrome creates — 3D normally leans on colour for depth separation
and there is none here. And because it is a parameter it can be animated, so the realm can visibly
degrade as Resolve drains: a diegetic timer with no UI, exactly §5's *"never a digit"*. Degrade
distance and environment only; hold creatures and interactables at full contrast, or it blinds the
player precisely when they most need to read the space.

**It is second because it is what makes every later step showable.** Under this shader untextured
boxes read as intentional rather than as unfinished, which means step 4 produces a screenshot worth
sending to an artist rather than a greybox worth apologising for. For the audience Jeremy actually
has in mind, this step does the persuading.

**2. The `SubViewport` seam — and keep it off `Main`.** The Dream already renders through a 480×270
`SubViewport` with nearest filtering and a 4× upscale, and that viewport can host a 3D subscene, so
`RealmTransitionManager`, `EntityStateManager` and the save format are all untouched by this entire
section. That is the whole integration cost.

Until the look question is answered, build it as a **standalone scene that is not wired into
`Main`**. A prototype that nothing loads cannot redden a gate, cannot destabilise the 328 that pass
today, and can be deleted without ceremony if the answer is "this does not read". Wire it into the
Dream layer only once it has earned the place.

**3. First-person movement — and a physics handler, which is a reversal.** Walk and look, plus
manipulate. **Changed 2026-07-30:** the plan of record was observation-only with no physics handler,
staged so the first Dream needed no new system. Jeremy has overruled it — *"There will actually be a
physics handler because of the puzzles"* — and the puzzles are the reason, so the verb staging goes
with them.

**Say what that spends, because it was written down in advance precisely so it could be noticed.**
`combat.md` §11 is an explicit complexity budget, and the puzzles note attached a caution to exactly
this: puzzles as *content* reuse machinery that already exists and are free, but **3D physical
manipulation is a new system** and should be counted honestly against the budget. It is now being
counted. That is the budget working as intended rather than being breached — a caution that gets
spent deliberately did its job; one that gets forgotten did not.

The distinction that still holds and is worth keeping straight: a `CharacterBody3D` sliding along
static collision is the *floor*, and it was never the thing being deferred. What arrives now is rigid
bodies — pushing, dropping, blocking, and whatever the puzzles turn out to want.

**Tune the walk speed in this step, and treat it as a design decision rather than a feel knob.**
`combat.md` §14 records the real risk of going 3D and it is mechanical, not artistic: real-time
movement adds a *reflex* requirement, and a player who cannot dodge is walled out of the sever route
and forced into overkill — which is the difficulty slider invariant 1 exists to prevent.
Ejection-on-touch keeps invariant 2 intact; the reflex requirement is the part that needs designing.
So: deliberate rather than twitchy, slow, generous grab windows, readable telegraphs. Slower is also
more frightening, so this is probably not a trade at all.

**Step 6 turns out to be most of the answer to this, which is worth noticing.** If creatures are
warded off by using the environment, the skill the Dream tests is *what you understood about the
room*, not how fast you can sidestep — a knowledge test rather than a reflex one. That is the exact
shape invariant 5 asks for ("all progression gates *access*"), and it means the walk speed stops
having to carry the fairness argument by itself. It does not fully retire the concern — you still
have to execute while something is coming — but it downgrades it from a design problem to a tuning
one.

**4. One quiet pocket, greyboxed.** A **knowledge pocket**, not a boss pocket — three reasons, in
increasing order of importance. Observation-only is the first verb and a knowledge pocket is the one
that wants it. A pursuer-free space isolates the *visual* question from the *chase* question, and
mixing them would mean an unimpressive result had two possible causes. And the pocket format this
step invents is what unblocks Dream room authoring, which is currently stalled on not having one.

Author it with **one deliberately distinctive landmark**, because the pockets decision has a known
failure mode and it is not the obvious one: uniform monochrome solves cohesion for free, so the risk
is not that pockets read as disconnected levels but that they are **samey and disorienting**. With no
minimap, no colour and 480×270, silhouette and structure carry every navigational cue there is.
Testing whether that actually works is worth as much as testing whether it looks frightening.

**The pocket format this step invents now has a spec, written 2026-07-30.** A knowledge pocket is
entered by finding a Waking-world item tied to crafting (or another trigger), contains creatures with
**no tracking — blind and mindless, anchored in place or wandering aimlessly** — and asks you to solve
the puzzle and reach a **memory fragment** before Resolve runs out. A boss pocket is entered only by
finding that boss's **conduit** item, and the boss **chases while you run**, with environmental
elements so a less skilled player can still win. Full table in `combat.md` §15.

Two things this settles that were open. **The two-versions split holds, but on a different axis than
the one in doubt** — the line is not whether creatures can be warded off, it is *whether anything is
hunting you*, and blind-versus-pursuing is a hard distinction no amount of counterplay softens. And
**the BFS-fairness question leaves most of the Dream**: blind creatures do not pathfind, so pursuit
tuning is confined to the three boss pockets. Step 4 builds a knowledge pocket, so the prototype needs
**no pathfinding at all** — a random walk and a collision shape.

**Re-entry — answered 2026-07-30.** The trigger item is **consumed on success, never on failure**, so
a failed run costs time and nothing else. Consuming on entry would make failing a puzzle you do not
yet understand cost you *access to the puzzle*, which is the dead-save shape invariant 12 exists to
prevent. And free retry is not a leak here: the ordinary Dream is an **observation loop** (nothing
material crosses over, puzzles are about noticing, a clock bounds the trip), so knowledge retained
across retries is the progression rather than an exploit. The clock's job is to make one trip tense,
not to ration how much of a pocket a player may ever see. Full reasoning in `combat.md` §15.

Two things that follow, both of which constrain how a pocket gets authored in this step:

- **A pocket is tense exactly once** — run one is exploration under pressure, run two is a known
  errand. So **size a pocket so a good first run can finish it**; one that cannot be cleared in a
  single Resolve bar by construction guarantees its fragment is collected on the boring run.
- **One pocket, one thing — ratified 2026-07-30, and it was already the design.** A knowledge pocket
  yields exactly one recipe; a boss pocket yields exactly one severed anchor; there is nothing else in
  there and no reason to return. That closes the mirror risk (consuming the key can never strand
  anything) and it also stops the Resolve clock from producing frantic exhaustive searching — with
  exactly one objective, the clock pressures *route-finding* rather than *sweeping*, which is the
  behaviour the observation-puzzle design wants. The fail state stays binary too, so the save needs
  one flag per pocket rather than a set.

  Two consequences for this step. **Pocket count is pinned to recipe *families*, not recipes** —
  corrected 2026-07-30, and it is the largest workload saving available. Starting recipes cost no
  pocket; the **base** recipe of a family is reclaimed from the Dream and costs one; **upgrades** are
  Waking-world memory items gated on already holding the base, and cost none; the apothecary side
  costs none. A family of three is one pocket instead of three. The gate that must not move: a *new
  craftable thing* always comes from the Dream, and only refinement is found out here. Guard the word
  *upgrade* — the model is Zelda tools, not Dark Cloud tiers, so an upgrade makes a **different**
  thing (a different damage type is pure access) or a **cheaper** one, never a *better* one. Full
  reasoning and the two authoring risks in `combat.md` §15.

  And **grant the knowledge and consume the trigger in one atomic operation** — if they can be
  separated, a crash between them eats the key without granting the recipe and the content is
  unreachable, which is the lockout arriving through the back door.

Note the catalyst rule in `combat.md` §6 is **not** in conflict: catalysts are charged on crossing
into a **boss pocket**, which is the sever route and the thing invariant 1 prices. Knowledge pockets
are the crafting track, not one of the two routes to killing a boss, so they carry no catalyst cost.

**5. The enemy question, answered by looking at it.** Put the same creature in the same lit space
twice — once as a Doom/Dusk-style billboard, once as a low-poly model — and look. This is a prototype
question rather than an argument, which is the same reasoning that got the whole realm prototyped.

| | Buys | Costs |
|---|---|---|
| **2D billboard** | no rigs, no animation blending, no 3D character art — this is the assumption that made 3D affordable at all, and the reason the old months-of-work estimate was wrong. It also lets a creature be *drawn* rather than modelled, which is a higher ceiling on a single memorable image | breaks down close up and from above, and cannot be circled |
| **3D model** | survives any camera angle, which matters for anything you walk around | rigging and animation — the expense that made this "months of work" in the first place. At 480×270 monochrome, low-poly reads as style, which narrows the gap more than it usually would |

**The discriminator is camera, not importance — and that cuts against the obvious answer.** The
question to ask of each creature is *does the player orbit this thing, or flee it?* A pursuer seen
mostly head-on while you run is a billboard's best case. An anchor boss you circle and study is a
billboard's worst one.

Which means the two hybrids point opposite ways and both have a real case, so this should be looked at
rather than reasoned out:

- **Models for bosses, billboards for the mindless things** — optimises *spatial legibility* for the
  creature you move around, and spends the art budget where the camera is hardest.
- **Billboards for bosses, models elsewhere** (Jeremy's instinct) — optimises *image quality* for the
  creature the player looks at longest and remembers. A hand-drawn boss at 480×270 can be more
  striking than anything low-poly, and bosses are the game's three or four signature images.

The tie-breaker is probably how much of a boss fight is spent circling it. If the anchor encounter is
a chase toward a fixed goal, the billboard case gets much stronger; if it is a study-the-thing
encounter, it gets much weaker.

#### Leaning 2026-07-30: billboards for bosses, models for the mindless — and the discriminator flipped

Jeremy: *"I am thinking that only bosses will be 2.5D because it will make them look much more
otherworldly and chaotic. But TBH…"* — so this is a lean, not a ruling, and the trailing "TBH" should
be read as still open to a screenshot. But **two things now argue for it that did not before, and one
of them is an argument I made against it.**

**First, the reason is better than the one I attributed to him.** I had this hybrid down as optimising
*image quality* — a drawn boss out-striking a low-poly one at 480×270. That is true but it is the
weaker case. The real case is that **a billboard's defect is the boss's characterisation.** A sprite
that always turns to face you does not obey the space; it reads as a cut-out pasted onto the world
rather than a thing standing in it. For an ordinary creature that is a flaw to hide. For an intrusion
into reality it is the entire point. Same trick as the dither: an artifact of the technique doing the
narrative work, which is the cheapest kind of art direction there is.

**Second, the camera discriminator now points the same way, which is a reversal of what is written
above.** The objection to boss billboards was that you circle a boss and a billboard cannot be
circled. But the pocket structure settled the same day says a boss **chases and you run** — which is a
head-on or over-the-shoulder camera for nearly the whole encounter, the billboard's best case. And the
mindless creatures are **anchored or wandering blindly**, meaning you walk around them at leisure with
no pressure at all — the case that most demands a model. So the encounter designs answer the tie-breaker
the paragraph above left open, and they answer it *for* the lean rather than against it. **The
discriminator was right; the input to it was missing.**

What still wants a screenshot: whether multi-angle billboards (Doom-style rotations) are needed at all
given the chase camera, since they multiply the drawing cost per pose and the chase may make them
unnecessary. Build one boss both ways in the lit space and look — that is cheaper than deciding.

> **A throwaway version of this belongs *inside* step 4, not after it — added 2026-07-30.** The
> stated reason to defer it is that a pursuer-free space isolates the visual question from the chase
> question, and that reason is sound. But it defers the wrong thing: billboard-versus-model is not
> the *frightening* question, it is the *legibility in space* question, and a quiet room is where
> that is best tested. Meanwhile this is **the only open call in the whole section that gates another
> person's work** — an artist cannot be briefed without being told whether the deliverable is a
> sprite sheet or a mesh, and briefing an artist is one of the two reasons this section jumped the
> queue. A capsule and a white-on-black quad in an already-lit room costs almost nothing. Keep the
> careful version of step 5 for later; put the cheap one where it unblocks somebody.

**6. Warding creatures off with the environment — widened 2026-07-30, and it is the other reversal.**
The plan of record was *level mechanics that slow the pursuer down*, scoped to **boss pockets only**.
Jeremy has widened it: *"There will be ways to use puzzles/environments to ward off boss and normal
creatures alike."* So it covers the mindless wanderers too, and it stops being a boss-pocket feature
and becomes the Dream's general verb.

**This reopens something that was explicitly rejected, and that is worth stating rather than
absorbing quietly.** On 2026-07-27 the option "the puzzle *is* the evasion" was considered and turned
down, with the note that the nearest acceptable version would be boss encounters only. The new
direction goes past that. Nothing about the earlier reasoning was wrong — it was a complexity
argument — and the reason it can be overturned now is that the complexity is being bought anyway for
step 3's physics handler. That is a legitimate reversal: *the thing that made it expensive is already
on the bill.* The rejection should be read as spent, not as still standing.

**The word "ward" collides with invariant 11 and the meaning does not.** Invariant 11 forbids any
trap, item or ward that removes **regeneration or the Resolve drain** — the "poor man's sever" that
would collapse the two-route economy. A Dream-side environmental ward that keeps a creature at a
distance does neither of those things, so the door stays shut. Worth writing down because a future
reader will hit the word before the meaning.

Two things to watch as this gets discovered in play, offered as things to notice rather than rules —
Jeremy's framing is *"we will explore and create as I build and play"*, and the roster should stay
provisional the way the trap roster does while its columns are real:

- **Warding must not deflate the pressure it is counterplay to.** The design has a deliberate
  escalation — get touched, lose Resolve, the dither worsens, you see less, you get touched more. A
  ward that is free and permanent replaces that spiral with a checklist. Local, temporary or paid-for
  keeps both.
- **It changes what the pockets are for.** A knowledge pocket where creatures can be handled is closer
  to a boss pocket than the original split assumed, and the two-versions distinction is worth
  re-checking once a pocket has actually been played.

What it costs in code is unchanged and small: Dream-side interactables that act on creatures, landing
in this pass rather than as a separate blocker.

#### The two routes past a creature — ratified 2026-07-30, and this is what closes the reflex risk

Jeremy: *"I think if you are really good at running and dodging you can get away from enemies or
bosses. Enemies even easier of course. But I will create trap/puzzle environments for people who
aren't as good. I feel like this is a fair way to do it."*

**This is the answer to the fairness objection in step 3, and it answers it structurally rather than
by tuning.** The objection was that real-time movement adds a reflex requirement, and a player who
cannot dodge gets walled out of the sever route and pushed into overkill — the difficulty slider
invariant 1 exists to prevent. Jeremy's answer removes the wall: **reflex is one route past a creature,
not the route.** Fail at dodging and you are not ejected from the Dream, you are pointed at the
environment instead.

**It also rhymes with the game's central choice, which is a good sign rather than a coincidence.** Two
ways past the same obstacle, paying different currencies — reflex costs risk, environment costs setup
and attention — is structurally the same shape as sever-versus-overkill. The Dream's internal decision
and the game's headline decision teach each other.

**One thing to watch, and it is specific: the Resolve clock can quietly convert skill into currency.**
If dodging is instant and free while the environmental route costs time, and time costs Resolve, then
the skilled player pays less for the same outcome — and across many visits that compounds into exactly
the difficulty slider the design is trying not to have. Two routes are only two routes while they cost
comparably. So keep the environmental route **roughly Resolve-neutral against dodging**, or keep the
gap small enough that it does not accumulate. The reverse failure is milder but worth knowing: if
traps are reliable and dodging is risky, nobody ever dodges and the reflex layer is decorative. Which
of those two you would rather have is a real question, and a play question.

#### Done 2026-07-30: unit 1 — the probe

`scenes/dream/DreamProbe.tscn`. Open it and press **F6** (not F5 — that runs `Boot.tscn`). Or:
`godot --path . scenes/dream/DreamProbe.tscn -- --probe-shot` writes one labelled frame to `.shots/`
and exits, which is how the look can be re-checked after any shader change without playing it.

**[`dream-probe.md`](dream-probe.md) is the driving guide** — the keys, getting around the 3D editor,
and which node holds which lighting and material setting. Written for a first go at Godot 3D, and
separate from this file because it is *which button* rather than *why*.

**Wired into nothing, deliberately.** `Main.tscn` does not know the file exists, so it cannot redden
a gate and can be deleted without ceremony. `RoomView.tscn`'s `disable_3d = true` is untouched; the
suite is **still 328, exit 0**. The consequence to state plainly: for the first time there is a piece
of this repo with no gates on it at all, and that is correct rather than an oversight — the unit's
entire success criterion is whether a room reads, and `--verify-slice` cannot have an opinion about
that. Gates arrive with unit 3, when the seam does.

Five files: the shader, the material, the two scripts, the scene.

- **`scenes/dream/DreamLook.gdshader`** — greyscale, posterise, 8×8 ordered dither, and the
  three-tier ramp *enforced* rather than requested. Above `signal_threshold` a pixel is snapped to
  pure white and never dithered; everything else is quantised into `levels` bands across
  `[0, white_clamp]` and so physically cannot reach the top. Same move as camp-ness from the bench.
- **`scenes/dream/DreamLook.tres`** — the material as a *file*, which is the one piece of unit 1
  that is not throwaway. Unit 3 assigns this to `RoomView`'s `DreamContainer` and the look arrives
  with no second implementation to keep in sync. Inlining it as a subresource would have meant
  building it twice and finding out from a screenshot.
- **`src/Dream/DreamLookTuner.cs`** — the knobs, the readout, `F12` capture. **The deliverable of
  this unit is knobs, not values**; whatever the keys settle on gets written back into the `.tres`.
- **`src/Dream/DreamProbeWalker.cs`** — `CharacterBody3D` on CSG collision at 2.2 m/s. No jump, no
  rigid bodies: the complexity budget stays attached to a consumer, and unit 4 has the consumer.

**The starting values are `levels 6`, `white_clamp 0.82`, `dither 0.70`, `signal_threshold 0.90`,
player light `range 7.0`.** Keys: `[ ]` greys, `; '` clamp, `- =` dither, `9 0` signal threshold,
`, .` light radius, `F4` hide the readout, `F12` shoot. Holding a key scrubs.

**What the first frame taught, and it is the thing this section predicted about itself.** The
pipeline was right immediately and the *composition* was wrong: a 16×16 room lit by a 6-unit radius,
with the placed lights high enough to wash the ceiling instead of revealing anything, and the
landmark parked in pitch black so its own test could not be run. The temptation is to reach for the
ramp. **The ramp was never touched — `levels` stayed at 6 through both frames.** The fix was an 11×11
room, a 7-unit radius, and the placed lights moved down onto the objects they are pointing at. That
is exactly the warning already written above — *treat a creeping level count as a symptom; the cause
is almost always light placement* — arriving on the first screenshot rather than after a pocket was
authored. Worth trusting the next time the instinct is to add greys.

**Two authoring rules fell out, both cheap now:**

- **The ceiling wants its own darker material.** With both ends of the ramp reserved the world has a
  compressed value band, and a large flat ceiling at the same value as the floor eats the frame and
  doubles the dither noise. Dark ceiling / mid floor / mid walls separates them inside the band and
  is horror-native anyway.
- **Specular has to be off on materials *and* lights.** Not a taste call: a highlight pushes a wall's
  luminance past `signal_threshold` and the wall flashes white, which breaks the white reservation
  from the inside — the reserve defeated by the renderer rather than by an authoring mistake.

**Four technical findings, all of them things that would have cost an afternoon to rediscover:**

- **There is no `MODULATE` built-in in a `canvas_item` shader here** (Godot 4.7.1, Compatibility) —
  the first version used it and failed to compile. The fix is better than the original: on entry
  `COLOR` *already is* the texture times the modulate chain, so reading it gets the alpha for free.
  Which means the pass survives `RoomView` cross-fading the realms with no change, and the modulate
  only touches alpha, so luminance is unaffected.
- **The dither must be indexed off viewport pixels, not `FRAGCOORD`.** The pass runs on the container,
  after the 4× upscale, so `FRAGCOORD` is in 1920×1080 screen pixels — a Bayer index off it is four
  times too fine and reads as noise rather than as texture. `floor(UV * textureSize(TEXTURE, 0))`
  puts the pattern back on the 480×270 grid.
- **A capture must be of the window, not the `SubViewport`.** The look lives on the container, so a
  `SubViewport` capture silently saves the raw 3D image with no posterise, dither or clamp in it —
  a screenshot that looks fine and shows none of the thing it exists to show. The 4× nearest upscale
  comes along and that is correct; the pixels are still legible as 4×4 blocks.
- **Dither-rising-with-distance is doubly dead, and the second reason is new.** The lighting decision
  retired it because falloff owns distance. Independently, a `canvas_item` pass over a `SubViewport`
  has no depth buffer to have done it with, so it was also the expensive parameter of the five.
  Recorded so nobody adds it back casually on the grounds that it "was in the plan".

**What unit 1 does not answer.** Whether the room is *frightening* — it is a lit box with a capsule
in it. Whether the landmark idea works, which needs two pockets to be confused between. And the
creature reads beautifully as a black hole against a lit wall but vanishes past the light radius,
exactly as this section predicted — the rim light is unbuilt, and it belongs with unit 2's creatures.

#### Decided 2026-07-30: the spirit light, and what Resolve is actually for

The Dream's light is **the player's own spirit**, not a torch — Jeremy's lore correction, and it changed
the mechanic rather than the word. A torch attracting monsters is a convention; a living spirit's light
in a realm of spirits drawing what lives there is simply what would happen.

**Because the light is you, its default is ON, and you suppress it rather than switch it on.**

| | You are | Cost |
|---|---|---|
| **Spirit shown** | seen, and you can see | creatures come to you |
| **Spirit suppressed** | hidden, and half-blind | Resolve burns faster |

One axis with a price at each end, not two costs for one action — the player always pays something and
chooses which. **This is what finally gives §5's Resolve clock a cause:** it used to drain because you
were in the Dream, which is arbitrary; it now drains because you are holding your own spirit down.
Jeremy expects suppression to become a puzzle verb rather than only a stealth toggle — TBD.

**Boss senses, same conversation:** always drawn to the light, acquire by proximity, noise or light,
chase on sight, and **line of sight must stay broken for 10 seconds** before the chase ends. This
retires open question 7 — the pure-BFS chase is replaced by something with diegetic, learnable
counterplay, and *anything that blocks sight is now the environmental ward* the design wanted. It also
lands the first hard constraint on boss-pocket authoring: **a boss pocket cannot be an open room**,
because staying unseen for ten seconds requires geometry to break sight with.

**Permanent pool upgrades, also decided:** Vitality and Resolve maxima grow across the game as a reward
for thoroughness. `Max` is not `Current`, so the settled "no item restores Resolve" rule is intact — the
guard is that upgrades must be **permanent, never consumable**, or a cheaper-suppression consumable
becomes a Resolve restore wearing a hat. Two consequences: Resolve capacity now means *how long you can
stay hidden*, and since nothing material leaves the Dream, a Resolve maximum increase cannot be an item
found in there — **it is Attunement**, which has been designed as "grows by surviving the Dream" and
never had a mechanic until now. Vitality maxima are Waking-world finds. Each pool upgrades in the realm
it serves.

#### Next in the probe — the queue, and the criterion that ordered it

Agreed 2026-07-30. These are built **in the test room**, not in a pocket: Jeremy's call — *"I think we can
keep adding features in this test room and build things we know that we want to."* Right, because each
one is cheaper to get wrong here than inside a format that then copies the mistake into every pocket.

**The criterion is information per hour, not features per hour.** Anything that tests an unproven
decision outranks anything that adds content, because only the first kind can invalidate other work.
That is why the list below is not in the order the systems will eventually be used.

1. **The spirit light toggle, plus a throwaway Resolve bar.** The inverted default — shown by
   construction, suppressed at a cost — is the newest and most load-bearing decision in the design and
   **nobody has felt it for one second.** It governs every moment of play, so if it reads badly a great
   deal downstream changes. Cheap: a key, two radii, and a *fake local* bar. **Do not wire the real
   `ResolveManager` in** — that would spend the probe's wired-into-nothing property, which is what keeps
   it from being able to redden a gate.
2. **Interaction — look at a thing, get a prompt, activate it.** Every pocket needs it: the memory
   fragment, the anchor, climb points, puzzle elements, doors are all one verb. It also carries the
   architecture decision that **is** most of the pocket format: the Waking world already does this in 2D
   with `HotspotData` and `InteractionService`, so does the Dream reuse them with a 3D position or get
   its own? Deciding before the first pocket means every pocket inherits it; deciding after means
   retrofitting all of them.
3. **The blind wanderer, and light attraction.** Cheap and already specified — random walk plus a
   collision shape, no pathfinding. It ranks this high for one reason: **without something that reacts
   to your light, suppression has no visible cost.** Items 1 and 3 together are the core loop; either
   alone is half an experiment.
4. **One pushable rigid body — as a *look*, not a system.** Answers a five-minute question: does a moving
   object read at 480×270 through two greys, and does its dither hold up in motion? **The physics system
   stays deferred** — rigid bodies designed with no puzzle in hand get designed against imagined puzzles
   and then rebuilt. Wait for a puzzle that names what it wants. Keeping the *risk check* and the
   *system* separate is the whole point of this entry.
5. **The objective, and finishing.** A memory fragment you walk to, take, and the run ends — the smallest
   complete pocket. Last of the five only because it depends on 2.

**Deliberately not yet: boss sight, chase and the ten-second search.** The largest of the creature
behaviours, it applies to three pockets only, and it depends on the light and noise senses being settled
first. Building it now is building against inputs that are still moving — the specific mistake this file
keeps catching.

**Recommended start: 1 and 3 together, then 2.** That produces a room where the player chooses between
seeing and hiding with something that responds to the choice — the Dream's loop in miniature, and the
last thing that could still invalidate the direction.

> **Why the probe is being polished rather than replaced, stated because it is easy to mistake for
> gold-plating.** Jeremy: *"I switched to 3D so it would be 'real' and she wouldn't think 'yeah okay,
> right'."* The prototype's job is **credibility with a collaborator**, not only de-risking — an artist
> cannot be recruited to a text document, and a greybox that plays badly is worse than none. So time
> spent making the room *feel* like the game is on the critical path to the artist conversation, which is
> a few days out. **The look values are frozen until after it** so she sees a considered thing and her
> opinion lands on something settled rather than mid-flight.

#### What this replaces, and what survives

| | |
|---|---|
| `RoomData.DreamBackground` | **Dies.** A 2D texture per room means nothing in a 3D pocket. |
| `EntityPlacement.DreamPosition` / `DreamScale` | Position becomes a `Vector3` under **either** enemy answer. `DreamScale` survives for billboards and stops meaning much for models — the one field the open question actually touches. |
| the six `dream_way_*` rooms | Throwaway, and already recorded as such. They were prototype content and they did their job. |
| the node-stealth heist | **Survives, and is not to be deleted until the 3D pocket beats it.** It answered "is being hunted frightening?" at low fidelity and the answer was yes, which is the evidence this whole section rests on. It is also the fallback if the 3D pocket does not read. |
| `RealmTransitionManager`, `EntityStateManager`, `SaveService` | Untouched. None of them cares what the Dream looks like. |

### Done 2026-07-30: the wound ledger (`CombatManager`, autoload #11)

Built to `combat.md` §18. Registered **last**, after `CampManager`, for the same reason that one is:
it asks other managers questions rather than answering them. It reads entity state and never writes
it — severing stays `EntityStateManager`'s business, so exactly one place decides what a body *is*.
**No `_Process`, and there must never be one:** a clock inside a fight would make turns stop being
the only currency.

What it owns, and the one non-obvious decision in each:

- **`DamageAgainst(type, target, potency)`** — static, so a gate can call it with no scene tree. The
  right type doubles, the wrong one halves, everything else stands. **Floored at 1**, because a
  resisted hit that rounds to nothing is an *absolute* discovered at the low end of the scale, where
  it looks like a bug rather than a rule — and §7 forbids absolutes outright.
- **The ledger**, a sparse `instanceId → damage` dictionary. Absent means unwounded; there is
  deliberately no second spelling. **Clamped at `Durability`**, so overkill is spent rather than
  banked — an unclamped ledger would make the condition readout depend on how hard the *last* hit was.
- **`SettleEngagement(instanceId)`** — the single place the persistence rule lives, and the reason it
  is a method rather than a flag on the caller: **it takes no outcome parameter.** Won, fled or
  collapsed, the *body's state* decides. Anchored bodies close their wounds; severed ones keep them.
  Fleeing must not become a way to bank progress against something still regenerating. The state is
  read when the engagement **ends**, not when the hit lands, so severing mid-fight keeps the work
  that got you there — that ordering is gated.
- **The condition ladder**, derived from remaining durability. Words are overridable per creature
  (`EntityData.ConditionWords`, exactly four); **bands never are.** A wrong-length array falls back to
  shared prose *in play* and fails the content sweep — the game stays readable, the suite does the
  complaining.
- **`SaveGame.Wounds`**, additive with no version bump, same argument as `Vitality` and
  `ReturnCampRoomId`. That closes §13's save-format question, and the answer turned out to be small:
  only the severed half can ever reach disk, because the anchored half is already gone by the time an
  engagement is over.

**39 new gates → 328**, all break-tested across seven runs. Worth recording *which* break each caught,
because it says what the section is actually protecting: removing the damage floor reddened exactly
one check; swapping the two multipliers reddened the ordering pair; deleting the overkill clamp and
reversing the bottom rung reddened four, with the offender list naming the exact wound total where
the ladder went backwards; inverting the severed check reddened five including the save round trip.

Two honest notes on the section's limits:

- The **first-hit** check (`a hit reports what it took off`) could not fail on its own — on a fresh
  ledger "what it took off" and "the running total" are the same number. A second hit was added
  specifically to separate them, and only that one goes red when `Wound` returns the total. Worth
  generalising: *a check written against the first iteration of an accumulator is usually vacuous.*
- The `wounds <= 0` guard in `ConditionOf` is **unfalsifiable at the current tuning** — a full bar
  divides to 1.0 and falls through the comparisons correctly anyway. It is kept because it stops
  being luck the moment `BleedingBelow` is retuned to 1.0, and its comment says so, but no gate
  covers it and none can without gating the thresholds themselves.

**What is left of combat: the encounter state machine.** Nothing can start a fight yet. That unit
owns the turn loop over a *list* of bodies (the list exists from day one and stays — but **Swarm was cut
2026-08-01**, so it is unexercised insurance and must not be gated), the
opt-in confirm step with §7's legibility readout naming both what the creature fears and what it
deals, alternating turns, the turn-cost rule, traps firing per `TriggersItself`, guaranteed flee with
a parting strike, the weak baseline attack that replaces `InteractionService`'s one-click Severed
finish, and the boss aura draining Resolve while `Corrupted` only.

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

M0–M3 plus the Dream heist, M4's crafting layer and camps, 328 automated gates. The playable loop,
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
  inventory panel is the worked example to copy. **Now a station screen rather than a recipe browser —
  `combat.md` §19.**
- **Her cache** as a real place with finite named salves in it. The storage box exists as of
  2026-07-30; what is missing is the *authored* cache — a camp with her finite salves already in it,
  which is content rather than code. **Promoted 2026-07-31 from a nice-to-have to a load-bearing beat:**
  §19 makes her two fused salves **found first and learned last**, so this cache is the thing the player
  rations for most of the game. It is also where the tutorial's free burning oil salve comes from.
- ~~**The eight material names**, and especially the scarce pair.~~ **Done 2026-07-31 —
  [`items.md`](items.md) is the whole roster**, and it was the one genuinely blocking item in M4.
  Materials, salves, traps, weapons, armour, tools, the two repair scales, per-chapter availability and
  the scarce budget, all named and costed. Two of the eight materials (**Bitterroot**, **Tallow**)
  already exist as authored `.tres` items and `salve_clearmind` is already `2 Bitterroot + 1 Tallow`, so
  existing content validates the vocabulary instead of needing rework.

  Three things in it that a code reader needs to know, because they are not obvious from the tables:
  **Fire and Caustic are trap-only** (no weapon deals them, and no armour wards Fire), **her two combat
  salves are `Component` rather than `Salve`** despite their names, and **the roster prices the Dream at
  10 pockets** — 7 knowledge, 3 boss — which Jeremy accepted as the authoring bill.
- ~~**The item / trap / equipment vocabulary**, before any more recipes are authored.~~ **Settled
  2026-07-30 — `combat.md` §18.** What is left of it is code: two enums and a handful of `[Export]`s,
  which must land before items or creatures are authored.

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
  nothing about rooms or items. ~~The one thing left is authoring~~ — **and that landed the same day:**
  `waystation_shed` holds the first `Workbench`, so a collapse now wakes you at the bench rather than
  falling back to `GameConfig.StartingRoomId`. The fallback still exists for a death before you have
  reached any camp, and is still gated.
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
  Jeremy's machine: a full run is **0.57 s** at 188 checks and has not moved measurably at 328, an
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
3. **Camps.** ✅ *Both halves done — see §1.* It was the clean case of the division working: the
   mechanic was built and gated against synthetic rooms without waiting on the judged call, and the
   suite was proven to survive the bench landing anywhere. Jeremy's half turned out to be one value —
   the shed's bench already existed as an `Interact`, and Jeremy's own "can't you make the existing
   workbench a workbench?" was the whole design decision. Worth remembering: *the judged call had
   already been made in the content; only the `Kind` had not caught up.*
4. **Combat — and this is the widest the parallel has been.** `combat.md` §18 established that rig
   sites hang off the creature rather than the room, so combat touches neither `RoomData` nor
   `HotspotData`. Jeremy's half is therefore **every Waking-world room** — layout, navigation, examine
   text, hazards, harvest nodes, the first bench — none of which the encounter loop can invalidate.
   Claude's half is the data shapes first (so item and creature authoring can start too), then the
   loop. ~~The two things still to hold back are **items and creatures**, until the shapes land, and
   **Dream rooms**, until the 2.5D call.~~ *The shapes landed the same day; see the revised hold-off
   list at the end of "Next".*
5. **The 3D Dream — and the balance inverts.** Reordered ahead of the rest of combat on 2026-07-30;
   see "The Dream in 3D". Worth flagging because **this is the first unit where Claude's half is the
   smaller one.** Claude owns the renderer switch, the look shader, the first-person controller, the
   `SubViewport` seam and whatever gates the seam earns — all of it provably-correct work with no
   judgement in it. Jeremy owns everything the prototype actually exists to answer: how the dither
   and posterise are tuned, whether the greybox pocket reads as frightening, whether its landmark is
   legible, and the billboard-versus-model call. **Every open question in that section is one only he
   can close, by looking at it.** So the useful thing Claude can do is get him to the looking as fast
   as possible, and then stop having opinions.

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
| `verify_`-prefixed rooms | `RegisterVerifyRooms` injects `verify_camp`, `verify_camp_two` and `verify_wilds` into `NavigationManager` at the top of a run, so camp behaviour is tested against ground the verifier owns rather than against whatever Jeremy authored. They are **not** in `data/` and `GateAuthoredContent` skips them. **Any new gate whose meaning depends on a room's contents belongs on a `verify_` room**, and anything asserting "the game has content" must keep excluding them, or a synthetic room will satisfy a check about authored work. |
| `GateHeistMechanics` | Calls `run.ForceClear()` before `GatePersistence`, so save/load is only tested from a Waking room with no run. This is exactly where the save/load soft lock hid. |
| headless runs | `_Process` never ticks meaningfully, so the pursuer only moves when a test calls `DebugSetPursuerRoom`. **Pursuit timing is untested.** |
| the view, in gates | `RoomView` and `Hud` *are* live in a headless run and now have gates asserting the room they display — but only the room. Backdrops, overlays, tweens and hotspot rects are still unasserted, and `--shots` is the only thing that looks at a pixel. |
| `ContentSeeder.cs:219` | The entry puzzle is a plain `Interact` hotspot ("search the cairn"). The minigame does not exist. |
| `InteractionService.cs:136` | Finishing a `Severed` body is **one click**. This is where the turn-based item fight goes. |
| ~~**No camp is authored**~~ | **Closed 2026-07-30.** `shed_bench` in `waystation_shed.tres` went from `Kind = 4` (Interact) to `Kind = 7` (Workbench) — a one-value edit to a hotspot that was already authored, labelled and positioned. Crafting, the cache and the respawn destination are live. Two gates now hold it: a camp exists, and it is walkable to from `StartingRoomId`. |
| `ItemData.VitalityRestore` | Read at last, by `UseItem`. Only salves use it. |
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

---

## 4. Open questions, in the order they'll bite

1. ~~**What shape is a hazard?**~~ **Answered 2026-07-29:** a `VitalityCost` on the existing hotspot
   kinds, charged per successful click, rather than a `HotspotKind.Hazard` or a per-room tax. Built
   and gated — see §1. What remains is authoring, not design.
2. ~~**The item, trap and equipment-type vocabulary.**~~ **Answered 2026-07-30 — `combat.md` §18.**
   Four damage types (**Edge, Blunt, Fire, Caustic**, the last taught only by the Physick side of the
   journal), two equipment slots (one weapon carrying a type, one armour halving a type), three enemy
   archetypes that need no field at all, and nothing that degrades. §4's trap roster is still
   provisional *content* — the salt line stays struck — but its two right-hand columns are now real.
3. ~~**`ResolveRestore` vs `VitalityRestore`**~~ **Answered 2026-07-29:** renamed to
   `VitalityRestore`; no item restores Resolve. See §1.
4. ~~**Trap placement** — authored sites per room, or free-form on a grid?~~ **Answered 2026-07-30:
   neither. The rig sites come with the creature**, two of them, counted in `GameConfig`. This is the
   answer that unblocked room authoring, because it means a room needs no trap data at all.
5. **The entry puzzle** is a placeholder, and is now the first real instance of Waking-world puzzle
   content — so it sets the grammar for every puzzle after it.
6. **The journal** must land before the first real observation puzzle, though it comes after
   crafting. See the callout at the end of `combat.md` §15 for why. Note that §18 gave it a second
   job: **Caustic damage is Physick knowledge**, so her half of the journal is now a combat gate and
   not only a medicine cabinet.
7. **Is the pure-BFS pursuer fair?** Never tested against a real player. Much smaller problem now
   that it only runs inside boss pockets — and the "level mechanics that slow the pursuer down" in
   `combat.md` §15's boss pockets is a better answer than tuning the BFS would be.
8. ~~**2.5D versus full 3D in the Dream.**~~ **CLOSED 2026-07-30 — 3D models for everything, no
   billboards.** Both halves are answered. The Waking world is first-person in a 2D world and the
   Dream is first-person in a 3D world; and the creatures in it are **models**. Jeremy, having looked
   at a camera-facing quad beside two 3D capsules in the probe: *"It will probably be best to just
   commit to 3d models so the feel is the same."*

   **His reason is better than the one the analysis below reaches for, and it is the one to keep.**
   Everything written under step 5 optimises *per creature* — image quality here, spatial legibility
   there — and lands on a hybrid. He is optimising the **coherence of the space**: mixed techniques
   would make bosses feel imported from a different game, and not deliberately. Consistency beats
   per-unit optimisation when the player's sense of place is the product. The lesson generalises past
   this decision.

   **What made it affordable was the screen-space line pattern, and that is worth understanding
   because it is why the earlier reasoning expired.** The billboard's whole case was *it does not
   obey the space, and for an intrusion into reality that defect is characterisation.* Screen-space
   line noise delivers exactly that on a model — so the billboard's unique advantage was eaten by a
   feature built for a different purpose, leaving only its costs (no silhouette when circled, breaks
   close up and from above, per-pose drawing, the Doom-rotation question). **A deferral can be closed
   by something built for an unrelated reason**, which is the same shape as step 6's rejection being
   overturned by step 3's physics arriving anyway.

   What this buys: no sprite sheets, no multi-angle rotations, no per-pose art, creature art is one
   mesh each, and open question 8 stops gating the artist brief. The billboard quad stays in the
   probe as the evidence and can be deleted whenever.

   **Leaning by the end of 2026-07-30: billboards for bosses, models for the mindless creatures** —
   Jeremy's call, with a trailing "TBH", so treat it as a lean awaiting a screenshot rather than a
   ruling. Two things back it: a billboard that will not obey the space is *characterisation* for an
   intrusion into reality rather than a defect, and the pocket structure settled the same day (bosses
   chase, mindless creatures are blind and static) means bosses are seen head-on and the mindless
   things are the ones you stroll around — so the camera discriminator, which used to argue against
   this, now argues for it.

   Three consequences of the restatement, because the old name was doing damage. It **no longer
   blocks Dream rooms** *by decision* — they are blocked by not yet having a 3D pocket format, which
   step 4 produces. It **no longer blocks creature data** — only creature art, since the call decides
   sprite sheet versus mesh. And it **moved from third in the queue to first**, on Jeremy's ask and on
   its own merits as the last architectural risk. Combat is still immune to it either way, which is
   most of why combat went first.
9. **Does Fire belong to Craft or to Physick?** §18 builds against three Craft types and Caustic
   alone on her side, because the asymmetry matches §8's "few of her notes, far apart". Moving Fire
   across makes it two and two and is a one-line change; recorded so the split is not mistaken for an
   accident.
10. **Does the fight need a defensive verb?** A shield is deferred, not rejected, and the version
    worth building is *brace* — spend a turn, take much less. Unanswerable until three fights have
    been played, and safe to defer because slots are an enum and an item declares its own.

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
- **"1st-Person"** means two different things and both are first-person. The **Waking world** is
  first-person in a **2D** world — Myst-style, painted screens, click to move. The **Dream** is
  first-person in a **3D** world. Settled 2026-07-30; the only thing left open is whether the
  creatures are billboards or models.
