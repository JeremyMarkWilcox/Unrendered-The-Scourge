# The placement bill — everything that needs a home

**Written 2026-08-01, at Jeremy's request: *"are we close to being able to give every item, enemy,
pocket, camp and boss a home on a mock-up map?"*** Yes. This is the budget; the map is his.

**What this is.** A per-chapter sheet of every creature, fight, pocket, blight, camp, NPC, node, tool
and set piece the design has committed to, with counts and the constraint on each. Nothing here is new —
it is [`combat.md`](combat.md) §17–22 and [`items.md`](items.md) §1–10 rearranged into the shape you draw
a map from. **Where this disagrees with those documents, they win** and this file is stale.

**What it is not.** It does not say which screen anything goes on, how the woodland connects to the
village, or what a room looks like. Those are the decisions this sheet exists to make possible.

---

## 1. The totals

| | ch1 woodland | ch2 village | ch3 mine | **Total** |
|---|---|---|---|---|
| **Regular fights** | 9 | 9 | 12 | **30** |
| — on the critical path | 4 | 6 | 7 | **17** |
| — optional | 5 | 3 | 5 | **13** |
| **Boss fights** | 1 | 1 | 1 | **3** |
| **Distinct creatures** | 3 | 3 | 3 | **9** + 3 bosses |
| **People freed** | 9 | 9 | 6 | **24** |
| **Knowledge pockets** | 2 | 4 | 2 | **8** |
| **Knot pockets** | — | — | **3** | **3** |
| **Boss pockets** | 1 | 1 | 1 | **3** |
| **Blights to mend** | 2 | 4 | 6 | **12** |
| **Graveflower yielded** | 2 | 4 | 6 | **12** (engine costs 8) |
| **Jigsaws to author** | 3 | 5 | 7 | **15** — 12 blights + 3 majors |
| **Cairnstone spots** | 1 | 3 | 2 | **6** |
| **Max-Resolve upgrades** | 2 | 4 | 2 | **8** |
| **Major repairs** | the bridge | the mine cart | the exorcism engine | **3** |
| **Camps** | his cottage | the liberated house | the mine camp | **3 scenes** |

**Required outnumbers optional, 17 to 13**, which is Jeremy's stated preference: *"I like needed being
larger than optional. A lot of games flip it and it feels like a chore."*

### The screen bill this implies

Rough, and the traversal rows are the only guesses. Creatures, interactables and pocket doors share
screens by rule, so a fight screen is usually also something else.

| | ch1 | ch2 | ch3 | Total |
|---|---|---|---|---|
| Fight screens | 9 | 9 | 12 | 30 |
| Blight screens | 2 | 4 | 6 | 12 |
| Major repair / set piece | 1 | 1 | 1 | 3 |
| **The Snarl** *(added 2026-08-01)* | — | — | **1** | **1** |
| Traversal and item-only *(guess)* | 3–4 | 4–5 | 3–4 | ~11 |
| **Waking screens** | **~16** | **~19** | **~23** | **~58** |
| **3D pocket interiors** | 3 | 5 | **6** | **14** |
| **Camp scenes** *(painted 3× for ch2)* | 1 | 1 *(×3 states)* | 1 | 3 |

---

## 2. Chapter 1 — the woodland

**The lesson of the chapter:** the right tool is cheap and the wrong tool is expensive. One damage type,
one trap, and creatures that resist things you cannot make yet.

### Creatures

| Was | Vulnerable | Resistant | Deals | Sightings | Placement |
|---|---|---|---|---|---|
| **Logger** | Edge | Blunt | Edge | 3 | 2 path, 1 optional |
| **Hunter** | Edge | Fire | Blunt | 3 | 2 path, 1 optional |
| **Messenger** | **Fire** | Edge | Fire | 3 | **all 3 optional** |

**The Messenger is the one exception to the cadence in the whole game.** It is Fire-vulnerable in a
chapter with no Fire, so the placement rule forces every sighting off the path. That is why chapter 1 is
4 path / 5 optional instead of 6 / 3 — and it is also why backtracking to chapter 1 later has something
in it: three fights you could not win, standing where you left them.

**Boss — the Lost Trapper** (Edge / Blunt / Edge). Already dead: a corpse being worn. Severing frees
nobody and it says nothing. His anchor is **a snare, sprung and empty.**

### Pockets — 3

| | Kind | Grants | Door sits on |
|---|---|---|---|
| **Bill Hook** | knowledge | the tool + **1 max Resolve** | a Held screen, after a Logger |
| **Mechanical traps** | knowledge | the whole family + **1 max Resolve** | a Held screen, after a Hunter |
| **The Lost Trapper** | boss | the memory that repairs **the bridge**, and **the chapter 1 refill** | his trap line |

**Ch1 pockets must be traversable without Float** — you do not have it until this chapter's boss falls.
Chapter 1 is the flat chapter, and that is a free variety curve rather than a limitation.

### Blights — 2

| Behind | Puzzle should use |
|---|---|
| an optional fight | pure observation — this is the tutorial blight |
| **the Bill Hook** | the tool. First time the player learns a blight can be tool-gated |

### Everything else that needs a spot

| | |
|---|---|
| **Camp** | **his cottage** — empty, her bench where she left it. Menu-reachable from anywhere |
| **Major repair** | **the bridge**, woodland → village. Commons + the boss memory + **a jigsaw** |
| **Materials introduced** | Bitterroot, Tallow, Heartwood, Pitch — seed thick, a common must never refuse a craft |
| **Cairnstone** | **1 cairn spot** — finite by construction. Nothing in ch1 spends it; it exists so the noun is met before the village needs it |
| **Graveflower** | none placed. It grows on mended ground: **2**, from the two blights |
| **Starting kit** | the Splitting Axe, Clearmind Salve knowledge |
| **Freed NPCs** | **9** — one named speaker at most, the rest a line and a nod |
| **Field station** | at least one (the trapper's stump or a mortar), so single-track crafting exists away from camp |

---

## 3. Chapter 2 — the village

**The arsenal chapter.** Three of the four damage types arrive here, both fused components, both extra
weapons families, and four of the eight knowledge pockets.

### Creatures

| Was | Vulnerable | Resistant | Deals | Sightings | Placement |
|---|---|---|---|---|---|
| **Stable Master** | Edge | Caustic | Blunt | 3 | 2 path, 1 optional — **Warden** (`BlocksPath`) |
| **Apothecary** | Blunt | Caustic | Edge | 3 | 2 path, 1 optional — **two of the three hand over a recipe** |
| **Innkeeper** | **Caustic** | Fire | Fire | 3 | 2 path, 1 optional — **never before an apothecary** |

**The chain is the tightest ordering constraint in the game and it must survive the map:**

> arrive with Edge → **Stable Master** → the **Bell Maul** pocket → **Apothecary** → **Corrosive
> Draught** → **Innkeeper** → **the Blacksmith**

Break that order anywhere and a creature stands on the path with no answer available.

**Boss — the Blacksmith** (Caustic / Fire / Blunt). Alive, tethered, and **the first person you kill.**
He begs, he settles his affairs — *"There's a key under the anvil." "Tell her I—"* — and his last words
are a sigh and *"Thank you…"*. His anchor is an object of his trade, never person-shaped.

### Pockets — 5

| | Kind | Grants |
|---|---|---|
| **Pry Bar** | knowledge | the tool + **1 max Resolve.** The Merchant is behind this door |
| **Bell Maul** | knowledge | Blunt + **1 max Resolve** |
| **Armour family** | knowledge | Tanner's Coat, Padded Jack, Limewashed Smock + **1 max Resolve** |
| **Fused traps** | knowledge | Oil Slick, Vitriol Flask + **1 max Resolve** |
| **The Blacksmith** | boss | the memory that repairs **the mine cart**, **Flare**, and **the chapter 1 + 2 refill** |

**Ch2 pockets may be vertical** — Float arrives from the chapter 1 boss.

### Blights — 4

| Behind | Puzzle should use |
|---|---|
| an optional fight ×3 | observation, and order |
| **the Pry Bar** | the tool |

### Everything else that needs a spot

| | |
|---|---|
| **Camp** | **the first house you liberate.** Three painted states — sparse / filling / full — driven by the count of rescue flags |
| **NPCs in the camp** | **3–4 physically present, with lines.** Everyone else is bedrolls, lamps, a second fire, voices off-screen. Count the bedrolls and you have counted the rescues |
| **Major repair** | **the mine cart**, village → mine. Commons + the boss memory + **a jigsaw** |
| **Materials introduced** | Scrap Iron, Cured Hide |
| **Cairnstone** | **3 spots** — with ch1's one, that is 4 stones against the chapter's 3 sinks (Bell Maul, Limewashed Smock, Deadfall). **Place all three before the Apothecary**, since the Maul is how you answer her |
| **Graveflower** | **4**, from the four blights |
| **Recipe handovers** | **2 freed apothecaries** — one gives Burning Oil, one gives Corrosive Draught. Both need real scenes |
| **The Merchant** | behind the Pry Bar |
| **Freed NPCs** | **9** |
| **Field stations** | the anvil at minimum. **Fusion happens only at the camp bench** |

---

## 4. Chapter 3 — the mine

**Rising action, not falling.** Twelve fights against nine, no new commons, no refill, and **the boss
behind a gate you have to go three ways to open.** The chapter is where you spend.

*(Two corrections to that line, both 2026-08-01: it said "everything abundant" — it is **Bitterroot and
Tallow** abundant, since the other commons and Cairnstone have no sinks left here — and it said "only two
pockets", which is now two **knowledge** pockets out of six. `items.md` §1 and §8.)*

### Creatures

| Was | Vulnerable | Resistant | Deals | Sightings | Placement |
|---|---|---|---|---|---|
| **Soldier** | Blunt | Fire | Edge | 3 | 2 path, 1 optional — **Brute** |
| **Miner** | **Fire** | Blunt | Blunt | 3 | 2 path, 1 optional |
| **The Beast** | **Caustic** | Edge | Caustic | **6** | **3 path, 3 optional** |

**The Beast's three variants — one damage pair, three sprites, three jobs:**

| Variant | Path? | Where | Reads as |
|---|---|---|---|
| **the crosser** | path | the main line, baseline numbers | it got through |
| **the settled one** | optional | side passages, tougher and slower | it has been here a while, and it has been feeding |
| **the breach-thing** | **guards the engine's pocket** | at the portal | there is something worse on the other side |

**The three path Beasts carry the escalation; the three optional ones are the chapter's Tallow valve** —
this is the only chapter with no refill, so the binder a short player needs is standing in a passage they
chose to skip. *(Narrowed from "supply valve" 2026-08-01: the carcass drops Tallow only, because Cured
Hide has no sink left in chapter 3 — `items.md` §7.)*

**Boss — the Demon / Iris.** Damage types deliberately unfixed. The finale is about which route you
took, not which tool you brought.

### Pockets — 6

| | Kind | Grants |
|---|---|---|
| **Mine Pick** | knowledge | the tool + **1 max Resolve** |
| **The exorcism engine** | knowledge, **optional, off the critical path** | the blueprint + **1 max Resolve.** Guarded by the **breach-thing** |
| **The lit knot** | **hunted** — examines **Flare** | passage **+ one narrative beat**. No upgrade, no material, no object |
| **The high knot** | **hunted** — examines **Float** | passage + one narrative beat |
| **The far knot** | **hunted** — examines **Suppress** | passage + one narrative beat |
| **The Demon** | boss | the finale |

**The three knot pockets are new as of 2026-08-01** — `combat.md` §23. They double chapter 3's Dream
authoring and add nothing to any other budget: no fight, no creature, no material, no craft family, no
max-Resolve upgrade. **Their doors sit on three of the seven critical-path fight screens**, so they need
no screens of their own. ~~Two is the documented fallback if the 3D bill hurts — drop the far knot.~~
**All three are committed** (Jeremy, 2026-08-01: *"Want them to beat all 3"*) — the three cuts are a
three-act beat structure that only works at three. **If the authoring runs long the lever is size, not
count:** a knot pocket is seen once and asks one question, so shrink it rather than dropping one.

### The Snarl — the chapter's gate

**One authored Waking screen: the passage to the Demon, knotted shut.** Three **cords** run out of it
into the rock in three directions; follow each to where it enters the stone and that is a knot pocket's
door. Cut all three and the Snarl falls open.

**This is the chapter's spine and chapter 3 did not have one** — chapters 1 and 2 gate their bosses
behind the bridge and the mine cart, and the mine had nothing. **It is cut, not mended:** no jigsaw, no
fourth major repair, fifteen mends stays fifteen. Placement rules: the three doors are **Held or clear,
never Fouled** (rule 5), and the order the player takes them in is free.

**Each cut pays one narrative beat and no object** (`combat.md` §23, `items.md` §9) — the work, then the
hand, then where else he has seen it. **Because the order is free, the beat hangs off the *ordinal*, not
off the pocket**: write beat 1 / 2 / 3 and fire whichever is next. Three flags exist already (a cut knot
is world state); the beat reads their **count**.

> **The verb is fixed to the pocket, the beat is not** — enter the high knot first and it is Float +
> beat 1, enter it last and it is Float + beat 3. **So the beat text may not mention the room:** no
> darkness, no height, no distance. It can only describe **the knot**, which is the same craft in all
> three. Three atmospheric paragraphs that each describe their own pocket is the natural thing to write
> and the wrong thing to write.

### Blights — 6

| Behind | |
|---|---|
| an optional fight ×5 | the five optional fights each guard one |
| **the Mine Pick** | the last tool-gated blight |

### Everything else that needs a spot

| | |
|---|---|
| **Camp** | **a camp in the mine.** No other people — *"it is your task you do alone"* |
| **The Blacksmith's spirit** | in the camp. The only occupant, and the reason the finale has a third option |
| **Major repair** | **the exorcism engine** — 8 Graveflower + commons + its pocket + one of her notes + **the final jigsaw**. The only recipe needing both tracks |
| **Materials** | no new nouns. **Bitterroot and Tallow abundant** — a hard requirement, they are what the chapter spends. The four dormant commons need only enough for the Mine Pick (`items.md` §1) |
| **Cairnstone** | **2 spots** — one for the Mine Pick, one spare. Not abundant; there is nothing here to spend it on |
| **Graveflower** | **6**, from the six blights |
| **Beast carcasses** | **6 harvest hotspots**, one per Beast, gated on that fight's flag. Drop **Tallow** only |
| **Her notes** | Deepmend, and the engine's physick half |
| **Armour** | nothing new — all three coats are chapter 2 (`items.md` §4). *(An earlier line here said the Limewashed Smock becomes makeable in the mine; that was the ch3 typo, corrected 2026-08-01.)* |
| **Freed NPCs** | **6** — Soldiers and Miners only. The Beast frees nobody |

---

## 5. The rules a placement has to satisfy

Read this as a checklist against a drawn map. Each one is ratified elsewhere and cited.

| | Rule | Source |
|---|---|---|
| **1** | A creature is only on the critical path if the player **can already make the type it is vulnerable to** when they reach it. Otherwise it guards something optional | `items.md` §7 |
| **2** | **One creature per screen**, and never the same creature on two adjacent screens | `combat.md` §21 |
| **3** | Nothing respawns on a cleared screen. A body stays where it fell | `combat.md` §21 |
| **4** | **Creatures, interactables and pocket doors share screens.** Screens should not feel empty. **Sharpened 2026-08-02** — some screens exist only to satisfy rule 2 *(ch2 row 9, ch3 row 21)*, and **a spacer screen must still carry something to pick up.** Jeremy: *"I'd rather have screens you grab an item or two than an empty screen just to fulfil not giving the same enemy twice."* **Material nodes count** — the bar is *not bare*, not *not-only-nodes*, which makes it cheap to clear and non-negotiable to skip | `combat.md` §22 |
| **5** | A **pocket door sits on a Held screen**, appearing once the creature is beaten. **Never on a Fouled screen** — that would put material spend on the critical path | `combat.md` §22 |
| **6** | Each of the **8 knowledge pockets** grants its craft family **and** one max-Resolve increase. **Boss and knot pockets grant neither** — eight pockets, eight upgrades, and nothing else may add to that count | `combat.md` §19, §23 |
| **7** | **Ch1 pockets are flat.** Float arrives with the ch1 boss, Flare with the ch2 boss | `combat.md` §19 |
| **8** | **The final fight must be winnable on path materials alone.** The engine may require optional content | `items.md` §7 |
| **9** | A **common must never be the reason a craft is refused.** If the player is ever short of Heartwood, Heartwood was misclassified | `items.md` §1 |
| **10** | **Every screen can be cleansed**, free, once nothing is Held / Blocked / Fouled on it | `combat.md` §22 |
| **11** | ~~Every optional sighting points at the ending — beat the fight you did not have to, reach a blight, mend it, get a flower.~~ **Restated 2026-08-02, and it points the other way: every blight sits behind an optional fight or behind that chapter's tool.** Beat the fight you did not have to, reach a blight, mend it, get a flower — *but no blight is ever simply lying on the path.* **This form is exact in all three chapters, 12 of 12, with no exception.** The old wording asked every optional *fight* to produce a blight, which chapter 1 cannot do — it has 5 optional fights and only 2 blights, because the bill's 2 / 4 / 6 blight escalation and the Messenger's forced-optional placement are two independent constraints that simply do not divide. **Nothing was violating the rule; the rule was counting from the wrong end.** The obligation the old form was reaching for survives as **rule 17** | `combat.md` §21 |
| **12** | **Battles are 1v1, always.** No encounter holds two bodies | `combat.md` §18 |
| **13** | **Nothing optional may compete with a critical-path item for a scarce material.** Graveflower and Cairnstone are the only two scarce nouns and both are finite by construction (12 and **6**). Spend either on something optional and the player can trade away a thing they need *blind* — the good ending before they have heard of the engine, or a tool before they know it is the way forward. Either the optional thing is cheap in scarce material, or it uses none. **Ratified 2026-08-01** — Jeremy: *"I agree 100%"*. ~~**See §8 — this rule is currently violated, and the violation is a softlock.**~~ **The violation is fixed — §8 is closed** |
| **14** | **All fifteen mends are required and unfailable.** No pay-to-skip on a blight, no purchase route on a major repair (`combat.md` §22, ratified 2026-08-01). The player who does not want mend puzzles opts out one level up by not doing optional fights, and finishes by severing |
| **15** | **Nothing crafted is ever consumed except a salve.** Traps, weapons, armour and tools are one-time purchases owned for the rest of the game (`items.md` §3, `combat.md` §19, ratified 2026-08-01). **Placement consequence: a scarce material's world total only has to cover the number of distinct objects that need it, never a rate.** Added 2026-08-01 as the general form of what §8 caught |
| **16** | **Every fight in the game happens exactly once.** The world holds **33 encounters** and a player who does everything fights **33 times** — nine creature *types*, each seen three times *(the Beast six)*, but **each sighting is a different individual and none of them ever comes back.** Stated directly 2026-08-02 at Jeremy's request — *"I only want to fight each enemy once (obviously all 3 of each type once)"* — because it was previously only derivable from rules 2, 3 and 12 plus §21's *"every instance frees a different named person"*, and a rule you have to assemble from four places is a rule that gets broken. **It is also the hard boundary on `items.md` §7's supply levers:** lever 4 would let *materials* regrow in a cleansed chapter, and even if it is taken, **creatures never do.** A body stays where it fell |
| **17** | **Every optional fight must be worth having taken.** The reward may be a blight *(9 of the 13 are)*, but a Dream pocket, a field station, one of her scattered notes or a material cache all count. **Added 2026-08-02** as the half of the old rule 11 that was actually load-bearing — it is rule 4's *"screens should not feel empty"* applied to a fight rather than a screen, and it is a stronger bar, because a fight costs the player something and a screen does not. Freeing a person does **not** count on its own: all 30 regular fights free someone, so it cannot distinguish an optional fight worth taking from one that is not | `combat.md` §21, `placement.md` §5 rule 4 |

---

## 6. Two arithmetic corrections this sheet turned up

Building the totals exposed two numbers repeated in the design docs that no longer follow from the
counts underneath them. Both are stated here rather than quietly fixed, because the wrong figures appear
in more than one place.

**1. It is 24 people freed, not 27.** The 27 dates from when every creature was seen three times: nine
creatures × three sightings. The Beast now appears **six** times and frees nobody, so the human total is
`(8 human creatures × 3) = 24`, and the fight total is `24 + 6 = 30`. This lowers the writing bill by
three scenes and does not affect the art brief, which was already budgeting 9–12 freed designs across
however many rescues.

**2. Chapter 2's optional fights (3) do not cover chapter 2's blights (4).** *"The optional sighting
guards a repair site"* reads like a one-to-one rule and cannot be one — the two counts are set by
different constraints and only coincide in chapter 3. The resolution is already in the design and is an
improvement: **one blight per chapter is gated on that chapter's tool instead of on a fight.** Three
tool-gated blights, nine fight-gated, twelve in total, and it gives the three tools a use beyond opening
the way forward.

---

## 7. Open items — small, and none of them block drawing the map

| | The question | Suggested answer, if one is wanted now |
|---|---|---|
| ~~**The Limewashed Smock**~~ | ~~one ch2 pocket buys all three coats, but §7 lists the Smock as ch3~~ | **CLOSED 2026-08-01 — the §7 line was simply wrong and is deleted.** The Smock is forgeable in ch2 and first useful in ch3, which needs no gate at all. `items.md` §4 |
| ~~**The mechanical traps**~~ | ~~the family is one ch1 pocket, but §7 lists three of the four as ch2~~ | **CLOSED 2026-08-01 — the ch1 pocket teaches all four, and three need Scrap Iron, a village material.** Both tables true, nothing invented. `items.md` §3 |
| **Her scattered notes** | Two are required (Deepmend, the engine's physick half). How many total? | **Four** — two that grant, two that only read. The two silent ones are the foreshadowing `items.md` §9 depends on |
| **Field stations** | Count and kind are unspecified | At least one per chapter, plus the camp bench. Fusion is camp-bench-only |
| **Iris's anchor** | Her work, or her body? | Left open deliberately — `combat.md` §21. The trade-object rule holds either way |
| **The Demon's damage pair** | Unfixed | Fill in when the encounter is authored |
| **Jigsaw forms** | Three forms across fifteen instances, but which fifteen | Jeremy's call — this is authoring, not budget |

---

## 8. ~~One thing that is not small — the Cairnstone budget can softlock the game~~ CLOSED

> **CLOSED 2026-08-01, same day it was found, and by none of the three options below.** Jeremy proposed
> a fourth that the table missed: **traps are crafted once and kept forever** (`items.md` §3). That
> deletes the repeatable column outright — the Deadfall's stone becomes a one-time purchase like the
> other three — so nothing can be overspent and there is nothing to guard.
>
> **The Cairnstone budget was cut with it: 3 / 5 / 8 → 1 / 3 / 2, sixteen stones down to six**, against
> a whole-game demand of four. `items.md` §1 carries the arithmetic; §1 and §§2–4 of this sheet carry
> the new spot counts. Jeremy's target: *"rare mats should be for the equipment, one time, and the
> amounts should line up or be over a little."*
>
> **Rule 13 is no longer violated** — with every scarce sink one-time and the budget over demand in both
> gate windows, the rule is now automatically true rather than enforced.
>
> **The section is kept rather than deleted, for the diagnosis.** The walkthrough below is a worked
> example of the failure mode rule 13 exists to catch, and the recommendation — my option A, a
> one-line recipe change — was worse than Jeremy's answer in a way worth remembering: **it fixed the
> arithmetic and left the mechanic that produced it in place.**

**Found 2026-08-01 while costing the two items above. This is a balance problem, not a bookkeeping one,
and it needs a decision before chapter 2 is authored.** Flagged rather than fixed because every option
changes the numbers in §1.

**Cairnstone is finite by construction** — 16 in the world, from 16 one-time authored cairns (`items.md`
§1). Four things spend it:

| Spender | Cost | Repeatable? | On the critical path? |
|---|---|---|---|
| **Bell Maul** | 1 | no | **yes** — the ch2 chain needs Blunt before the Apothecary |
| **Mine Pick** | 1 | no | **yes** |
| **Limewashed Smock** | 1 | no | no |
| **Deadfall** | 1 | ~~**yes — it is a consumable trap**~~ **no, as of 2026-08-01** | no |

**The Deadfall is the problem, and `items.md` §2 already contains the argument against it.** That section
took scarce material out of every salve for one reason: *"all four are consumables and a fixed world
budget cannot supply a consumable."* A trap is consumed on every use (§3, *"all six are consumed,
always"*), so the Deadfall is the same mistake in a different table — it just was not spotted because it
sits in the trap list rather than the salve list.

**The concrete softlock.** Chapter 2 grants 5 Cairnstone. A player who reaches the village, learns the
Deadfall, and rigs five of them across the Stable Master fights arrives at the Bell Maul pocket with zero
Cairnstone and cannot forge the Bell Maul. The Bell Maul is how you beat the Apothecary. **The chapter's
refill does not arrive until the chapter 2 boss falls, which is after the Apothecary** — so there is no
rescue, and the save is dead. That is invariant 12.

**Three ways out, and they are not equivalent:**

| | Fix | Cost |
|---|---|---|
| **A** | **Take Cairnstone off the Deadfall** — price it in commons, heavier than the Spiked Pit rather than scarcer (e.g. 3 Heartwood + 1 Scrap Iron) | Cairnstone loses its only repeating sink, so 16 is then **~4× more than the game can spend.** The budget and the 16 cairn spots in §1 both want cutting to about 5 |
| **B** | **Take Cairnstone off the two critical-path items** — Bell Maul and Mine Pick go commons-only | Cairnstone becomes a purely optional currency and cannot softlock anything. But `items.md` §1 defines it as *"every forged weapon head and the best traps"*, so this guts what it is for |
| **C** | Leave it and guarantee the budget per gate | Requires the player to be unable to overspend, which means either a warning prompt or a reserved stock. **Both are the kind of hand-holding this design has refused everywhere else** |

~~**Recommended: A, and then cut the Cairnstone budget.**~~ It is the fix §2 already made for the identical
problem, it keeps Cairnstone meaning *"the permanent things you forge"* — which is the more evocative job
anyway — and it makes rule 13 automatically true instead of needing to be enforced. The honest cost is
that six numbers in §1's budget table were sized for a sink that is about to disappear, and the placement
sheet's 16 cairn spots go with them.

**Not recommended: C.** A softlock you avoid by warning the player is still a softlock with a sign on it.

> **What the option table got wrong, kept because it is a repeatable mistake.** All three options treat
> *"how much scarce material does the Deadfall cost"* as the question. **The real question was whether a
> permanent kit should have a per-use price at all** — and the answer had been drifting toward *no* for
> weeks without anyone writing it down: carry limits were gone, the loadout moved to the rig screen,
> camps became a menu. Option A would have made the arithmetic safe and left the last inventory mechanic
> standing. **Symptom-level fixes pass their own test, which is exactly why they are hard to catch.**
