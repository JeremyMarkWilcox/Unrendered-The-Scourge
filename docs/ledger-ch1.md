# Chapter 1 ledger — the woodland

**Drafted 2026-08-01 by Claude from [`placement.md`](placement.md) §2, as the worked example of the
ledger format ([`build-plan.md`](build-plan.md) §5).** This is the artifact the skeleton `.tres` files
are generated from: one row per screen, and filling it in **is** drawing the map.

> **React to it, do not accept it.** Everything in the *Held / Blocked / Fouled / door* columns is
> forced by the bill and is not mine to move. Rewriting a row costs nothing today and costs a
> regeneration after pass 1.
>
> **Who draws the map — settled 2026-08-02: Claude does, Jeremy reacts.** I hedged this and it cost a
> turn. The graph is **mostly forced** — rule 2 fixes which creatures can be neighbours, the pocket doors
> must sit on Held screens after the right creature, the tool-gated blight must sit behind the tool, and
> the path fights must alternate. That is constraint satisfaction, which is the derivable half
> ([[division-of-labour]]). What is *not* derivable is whether it **feels like a wood** — and that is
> judged far faster by looking at a drawn map than by drawing one from a blank page. So the **±** marks
> stay, and they mean *"I chose this, overrule it freely"*, not *"waiting on you"*.

**Seventeen rows: ~16 Waking screens plus the cottage**, which is now a real room with doors
(decision 3). `placement.md` counts camps separately from the ~58, so the totals still agree.

---

## The rows

Ids follow `ch<n>_<place>`. Flags follow the §5 vocabulary. **Held** = a creature stands on it,
**Blocked** = a tool opens it, **Fouled** = a blight is on it.

| # | id | name ± | exits ± | Held by | Blocked | Fouled | pocket door | nodes ± | NPC / other |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `ch1_cottage` | His Cottage | 2 | — | — | — | — | — | **CAMP.** Her bench. `StartingRoomId` |
| 2 | `ch1_dooryard` | The Dooryard | 1, 3 | — | — | — | — | Bitterroot ×2, Tallow | the Splitting Axe is already his |
| 3 | `ch1_low_track` | The Low Track | 2, 4 | **Logger #1** *(path)* | — | — | — | Heartwood | freed: the woodcutter |
| 4 | `ch1_deadfall` | The Deadfall | 3, 5 | **Hunter #1** *(path)* | — | — | **Mechanical traps** | Pitch | freed: the snarer |
| 5 | `ch1_crossroads` | The Crossroads | 4, 6, **7**, 12 | — | — | — | — | Bitterroot | signpost. **The Bramble is visible from here** |
| 6 | `ch1_cut_stand` | The Cut Stand | 5, 8 | **Logger #2** *(path)* | — | — | **Bill Hook** | Heartwood ×2 | freed: the sawyer |
| 7 | `ch1_bramble` | The Bramble Choke | **5**, 16 | **Messenger #1** *(optional)* | **Bill Hook** | — | — | — | Held **and** Blocked. Seen one screen before the key |
| 8 | `ch1_snare_line` | The Snare Line | 6, 9 | **Hunter #2** *(path)* | — | — | — | Tallow, Pitch | freed: the trapper's boy |
| 9 | `ch1_trap_yard` | The Trap Yard | 8, 10 | **the Lost Trapper** *(boss)* | — | — | **the Lost Trapper** *(boss)* | — | his trap line. The door is his place. **Body placed 2026-08-05** as `ch1.lost_trapper` — the pocket door is still Jeremy's |
| 10 | `ch1_bridge` | The Broken Bridge | 9, → ch2 | — | *(the repair)* | — | — | — | **MAJOR REPAIR 1** + jigsaw. ~~Gated on the boss memory~~ gated on the boss **flag** *(decision 10)*. Authored 2026-08-05 as `ch1_bridge_span` + `recipe_repair_bridge`; ~~the `RequiredFlag` goes on once the Lost Trapper is placed~~ **the boss was placed on row 9 the same day and the `RequiredFlag` is on**: `defeated.ch1.lost_trapper`, and a gate asserts the span is drawn shut until it is set. **No creature stands on this screen** — he is one screen back, which is what makes the flag a gate rather than a formality |
| 11 | `ch1_fern_gully` | Fern Gully | 12 | **Logger #3** *(optional)* | — | **blight 1** | — | Heartwood | **the tutorial blight** — pure observation |
| 12 | `ch1_wet_hollow` | The Wet Hollow | 5, 11, 13, 14 | — | — | — | — | Bitterroot ×2 | the whole optional branch hangs off here |
| 13 | `ch1_burnt_stand` | The Burnt Stand | 12, 15 | **Hunter #3** *(optional)* | — | — | — | Pitch ×2 | **field station** — the trapper's stump |
| 14 | `ch1_old_road` | The Old Road | 12 | **Messenger #2** *(optional)* | — | — | — | **bulk cache** — Pitch ×3, Tallow ×3, Heartwood ×2 | the wrecked mail cart. Fire-vulnerable. Winnable, and it costs you |
| 15 | `ch1_coldhearth` | Coldhearth | 13 | **Messenger #3** *(optional)* | — | — | — | Bitterroot | a burnt-out croft. **One of her scattered notes** *(silent)* |
| 16 | `ch1_stonefield` | The Stonefield | 7, 17 | — | — | **blight 2** | — | Bitterroot | tool-gated blight, behind the Bramble |
| 17 | `ch1_boundary` | The Boundary Stone | 16 | — | — | — | — | **Cairnstone ×1**, Heartwood | the cairn is the Boundary Stone itself |

**Counts against `placement.md` §2:** 9 regular fights *(4 path, 5 optional)* ✅ · 1 boss ✅ · 3 pockets
✅ · 2 blights ✅ · 3 jigsaws *(2 blights + the bridge)* ✅ · 1 Cairnstone ✅ · 1 camp ✅ · **9 freed NPCs,
all nine reachable on a first pass** ✅ · 2 max-Resolve grants *(the two knowledge pockets)* ✅.

---

## The chain, and why the order is what it is

> `ch1_cottage` → **Logger #1** → **Hunter #1** *(traps pocket)* → **Logger #2** *(Bill Hook pocket)* →
> **Hunter #2** → **the Lost Trapper** → the bridge

Four path fights, alternating Logger and Hunter, which satisfies **rule 2** (never the same creature on
two adjacent screens) by construction rather than by checking afterwards. Both are Edge-vulnerable and
he starts with the Splitting Axe, so **rule 1** is satisfied trivially — chapter 1's lesson is *one
damage type, one trap*, and the only creature that breaks it is the Messenger, which is why all three
of its sightings hang off the optional branch.

**The Messenger is expensive, not impossible.** It resists Edge, so the axe deals half — a longer fight
that spends more Vitality and more salves, against a creature that deals Fire, which no armour wards
(`items.md` §4). That is the whole point of it: `items.md` §21 says chapter 1 is where the player learns
**a wrong-typed fight is expensive rather than impossible**, and the Messenger is the only creature in
the chapter that can teach it. **Every fight in chapter 1 is winnable on a first pass.** Coming back with
Fire later is a *better* way to do it, never the only way.

**The two knowledge pockets sit on the two path screens that are neither first nor last**, so both doors
appear on a Held screen after its creature falls (**rule 5**), and neither is on a Fouled screen (rows 4
and 6 carry no blight).

### The graph, drawn out

```
        1 cottage
        |
        2 dooryard
        |
        3 low track ......... Logger #1
        |
        4 deadfall .......... Hunter #1   ▸ pocket: Mechanical traps
        |
        5 CROSSROADS ------------------+----------------+
        |                              |                |
        6 cut stand ..... Logger #2    7 bramble        12 wet hollow
        |   ▸ pocket: Bill Hook          [Bill Hook]      |    |    |
        8 snare line .... Hunter #2     .. Messenger #1   |    |    |
        |                              |                 |    |    14 old road .. Messenger #2
        9 trap yard                    16 stonefield     |    13 burnt stand .. Hunter #3
        |   ▸ pocket: the Lost Trapper    ~ blight 2     |        |   [field station]
        10 BROKEN BRIDGE → ch2         |                 |        15 coldhearth .. Messenger #3
                                       17 boundary       11 fern gully .. Logger #3
                                         [Cairnstone]       ~ blight 1
```

> **Confirmed by Jeremy 2026-08-02** — *"I think this graph makes a ton of sense and gives the players
> options where to go but not too many either. I like it a lot… I can't see doing it another way."* **The
> exits column is no longer ±.** Chapter 2 and 3 get drawn the same way.

**The crossroads is the only fork in the chapter**, and everything hangs off it: the spine left, the
Bramble ahead, the whole optional branch right. That makes the signpost examine load-bearing instead of
decorative, and it means skipping the optional content is **one decision made once** rather than five
turnings missed. *(The risk of a single chokepoint is that one wrong turn hides a third of the chapter —
the mitigation is that the signpost is the only examine in ch1 that must be written well.)* **Jeremy is
painting the signpost into the row 5 background**, so the examine has a thing to be attached to rather
than being a floating hotspot on scenery — which is the right order, and the reason this row's art
should be drawn before its prose is written.

**The Bramble is seen from the crossroads and opened from row 6 — one screen apart, in that order.** You
reach the lock before the key, which is what makes it a *locked door you can see through* rather than a
door that opens the moment you find it. An earlier draft hung the Bramble off row 6 and lost the beat
entirely; this is the fix.

**Rule 2 holds on every edge.** Path: L1–H1, H1–(5), (5)–L2, L2–H2, H2–(9). Branch: M1's neighbours are
(5) and (16), both empty; L3 and H3 touch only the empty hollow; M3 hangs off H3 rather than off M2, and
**that placement is not cosmetic** — 14 and 15 adjacent would put two Messengers side by side, which is
the one arrangement three creatures in five optional slots makes easy to get wrong.

**Rows 11–17 are genuinely optional**: nothing on the spine passes through row 7 or row 12. The branch
holds three of the five optional fights, both blights, the Cairnstone and the field station.

---

## Seven things the derivation exposed

**1. Rule 11 does not fit chapter 1, and the bill already knew.** *"Every optional sighting points at
the ending"* — but chapter 1 has **5 optional fights and 2 blights**, and one of those blights is
tool-gated rather than fight-gated. So exactly **one** optional fight guards a blight; the other four
guard a field station, a Cairnstone, materials, and nothing. This is the same shape as `placement.md`
§6's second correction (ch2's 3 optional vs 4 blights) and it wants the same resolution: **rule 11 is a
statement about the game, not about every fight.** Worth restating in `placement.md` before it is
checked mechanically, or the bill checker will flag four false positives.

> **Sharpened 2026-08-02 by drawing chapter 2, and the softening above is the wrong fix.** Rule 11 is not
> approximately true — it is **exactly** true in the other two chapters: ch2 has 3 optional fights and 3
> fight-gated blights, ch3 has 5 and 5. Only chapter 1 misses, at 5 and 1. **So it is a real rule with one
> named exception — the Messenger** — whose all-optional placement gives ch1 five optional fights where
> the arithmetic wants three. Record it as an exception, not a weakening: a softened rule stops catching
> the thing it exists for, which is an optional fight that leads nowhere. See
> [`ledger-ch2.md`](ledger-ch2.md) finding 1.

> **CLOSED 2026-08-02, and both readings above were wrong.** Jeremy, on being told rule 11 needed an
> exception named after a creature: *"the Messenger breaks a rule?"* **It does not, and neither does
> anything else** — checking the question found that the rule was simply counting from the wrong end.
>
> **Invert it and it is exact everywhere, 12 blights out of 12:**
>
> | | blights | behind an optional fight | behind that chapter's tool | gated? |
> |---|---|---|---|---|
> | **ch1** | 2 | 1 *(Logger #3 → blight 1)* | 1 *(Bill Hook → blight 2)* | ✅ 2 / 2 |
> | **ch2** | 4 | 3 | 1 *(Pry Bar)* | ✅ 4 / 4 |
> | **ch3** | 6 | 5 | 1 *(Mine Pick)* | ✅ 6 / 6 |
>
> **No blight anywhere is simply lying on the path, and no exception is needed.** *(Chapter 1's blight 2
> is in fact double-gated: row 16 sits past row 7, which is Held by Messenger #1* **and** *Blocked by the
> Bill Hook. So two of ch1's five optional fights do gate a blight, not one — the old count was also off
> by one.)*
>
> **Why the old form could never hold:** it asked every optional *fight* to produce a blight, and the
> number of optional fights is set by rule 1 and creature vulnerability while the number of blights is set
> by the bill's deliberate 2 / 4 / 6 escalation. **Two independent constraints, and they do not divide.**
> That is arithmetic, not a design fault, and naming a creature in the rule would have written a
> coincidence of chapter 1 into a law about the whole game.
>
> **What survives is `placement.md` rule 17** — *every optional fight must be worth having taken* — which
> is what the old rule 11 was actually protecting. See finding 7 for the one screen in the game that
> currently fails it.

**2. Nothing in chapter 1 is withheld for the backtrack — corrected 2026-08-02.** An earlier draft of
this row claimed the three Messengers could not be won in chapter 1 and that six people were freed on a
first pass and three later. **That was wrong**, and the ratified text says so plainly: `items.md` §21 —
*"a wrong-typed fight is expensive rather than impossible"* — and §7 — *"the wrong type halves damage, it
never zeroes it, and flight is guaranteed."* Jeremy: *"I would rather give the player a chance to do it
all and have the chance to do it again if and when they want to."*

> **The rule this fixes in my own head:** *off the critical path* means **the player may choose to defer
> it**, not **the game defers it for them**. Those read the same in a placement table and are opposites in
> play. Anywhere the docs say a creature "guards something optional", the optionality is the **player's**.

So chapter 1 frees **nine on a first pass**, and the reason to return with Fire is that it is cheaper,
not that it is the only way. Nothing about the placement changes; only what the ledger claims about it.

**3. Row 7 is no longer the empty screen — Messenger #1 stands on it.** Jeremy: *"If we have 3 different
types of creatures couldn't we put another one in?"* — and that is the better fix than the examine I had
proposed, because it costs no new noun. It needed the finding above first — a creature that could not be
beaten would have made the Bramble a wall rather than a door. The screen is now **Held *and* Blocked**,
which is the first time the player sees two reasons at once, and the Cairnstone moved to row 17 so the
Boundary Stone *is* the cairn. **No screen in chapter 1 is empty now.**

Filling row 7 is also what forced the graph rewrite above: hanging the Bramble off row 6 put the lock
*after* the key. It now hangs off the crossroads instead. **A creature on a screen changed where the
screen belongs** — worth remembering when the other two chapters are laid out.

**4. What a second pass through a cured chapter contains — and it needed no new rule.** Jeremy asked
whether freed NPCs give their items again and whether pickups reappear, believing this was an assumption
neither of us had nailed down. **It is already ratified, in three places, and the answer is no:**

| | |
|---|---|
| `placement.md` rule 3 | *"Nothing respawns on a cleared screen. A body stays where it fell"* |
| `combat.md` §21 | *"Nothing **ever** respawns on a cleared screen. This is the real violation in a point-and-click, and the design is already immune"* |
| `combat.md` §21 | *"Every instance frees a **different named person**"* — so a freed person is a person, and people are not restocked |

Graveflower and Dream knowledge were right in the assumption too, and for two different reasons:
Graveflower is **12 authored restorations**, so it is finite because the *sites* are finite, not because
a rule forbids re-picking; Dream knowledge cannot repeat at all, since you either know the Bill Hook or
you do not. Cairnstone is finite by construction (`items.md` §1).

> **So the second pass is worth taking for what you *left*, never for what *regrew*.** Nodes you walked
> past, blights you did not mend, cairns you did not break, the branch off the crossroads you never took.
> That is the same player-controlled valve as chapter 3's optional Beasts — and it is better than
> respawning, because the game never has to guess how much you needed.

**One knock-on, and it is a real cost of finding 2.** `items.md` §7 justifies the backtrack like this:
*"Backtracking to chapter 1 for materials was a walk through cleared ground with nothing in it; now the
creatures you could not kill are still standing there and you can. The restock trip and the come-back-later
trip are the same trip."* **That justification dies with the Messenger correction** — there are no
creatures you could not kill. The backtrack is now purely *"the branch you skipped"*, which is thinner,
and `combat.md` §17's standing instruction is what has to carry it: **"author something into a cleansed
zone that was not there before."** Currently unspecified, and now load-bearing.

**5. Chapter 1 could not build its one trap — a live contradiction, found by placing the Pitch.** Working
out where the material nodes go meant asking what chapter 1 actually *spends*, and the answer came back
**nothing**: `items.md` §7 gives chapter 1 exactly one trap (the Snare), §3 priced it in **Cured Hide**,
and §1 and §7 both put Cured Hide in **chapter 2**. Three true statements, jointly impossible. The cost
was not only the missing trap — it made the Mechanical pocket, one of chapter 1's two knowledge pockets,
pay off a chapter after you open it, which is the specific thing the pocket rule forbids.

**Repriced to Heartwood + Pitch** (`items.md` §3), because §1 already calls Pitch *"binder, and the fuel
in fire traps"* and because importing Cured Hide would cost chapter 2 its shape. Nothing in this ledger
moves — the Pitch is already on rows 4, 8 and 13.

> **Why a ledger finds this and a read-through does not.** Each of the three statements lives in a
> different section and each is locally correct; the contradiction only exists when you ask *"what can
> the player make on this screen, today, with what they are standing next to."* **That question is what
> laying out a chapter is**, which is an argument for drawing chapters 2 and 3 before generating any of
> them rather than chapter by chapter.

**6. Her two silent notes had no home anywhere in the bill — and this ledger now houses one.** Drawing
chapter 2 turned up a question about a screen with nothing hand-placed on it, and answering it found the
gap: `placement.md` §8 settles that **four** of her notes exist — two that grant a recipe *(Deepmend, the
engine's physick half, both chapter 3)* and **two that only read**, whose whole job is to foreshadow. The
bill counts them and never places them.

**Chapter 1 takes the first, at row 15, Coldhearth** — a burnt-out croft at the deep end of the optional
branch, one screen past Hunter #3. Chapter 2 takes the second, in the inn yard stables. **Neither can go
in chapter 3**, because a note written to foreshadow has to be read before the notes it foreshadows.

> **Both landed on optional, off-spine screens, and that is deliberate.** A silent note grants nothing,
> so a player who never finds one loses no capability — which makes it the *only* class of content that
> can sit behind an optional fight without violating rule 11's spirit. It is also the cheapest possible
> answer to *"why walk down here"* on a dead-end screen: no count in §2 or §3 moves, no material economy
> shifts, and the screen stops being a spacer. **Expect to want more of these** than the bill's four
> once the remaining dead ends get their prose.

**7. Exactly one optional fight in the whole game rewarded nothing — row 14 — and it is fixed in place.**
Rule 17 *(new, 2026-08-02)* asks that every optional fight be worth having taken. Checked across all
thirty-three encounters, **thirteen optional fights, and twelve of them already pay:**

| | gates | ✅ |
|---|---|---|
| Logger #3 *(11)* | **blight 1** | ✅ |
| Messenger #1 *(7)* | **blight 2** at row 16, and the Cairnstone past it | ✅ |
| Hunter #3 *(13)* | **the field station**, and the way through to Coldhearth | ✅ |
| Messenger #3 *(15)* | **one of her silent notes** | ✅ |
| **Messenger #2 *(14)*** | **Tallow** | ✗ |
| ch2's three, ch3's five | a blight each | ✅ |

**Row 14 was the only hole, and it was the worst place to have one** — the Messenger is Fire-vulnerable in
a chapter with no Fire, so it is the most expensive optional fight in the game, and it paid the least.

**Fixed by giving it chapter 1's bulk cache: a wrecked mail cart — Pitch ×3, Tallow ×3, Heartwood ×2.**
It is the chapter-1 mirror of the Barricaded Merchant in ch2's cellar, which is also a bulk payout at the
end of a dead-end spur, and the proportion is right: hardest optional fight, biggest material payout. **It
also gives `items.md` §7's lever 1 somewhere obvious to push** if chapter 1 ever plays short.

> **The alternative was moving Messenger #2 onto the Boundary Stone at row 17**, which would have made it
> gate the Cairnstone and dropped chapter 1 to 16 screens — exactly the bill's figure. **Rejected on two
> counts.** It would put both Fire-vulnerable Messengers on one three-screen arm, making that whole branch
> a punishment; and **Jeremy confirmed this map on 2026-08-02** — *"I can't see doing it another way."*
> **Redrawing a confirmed map to satisfy a rule written afterwards is the wrong direction of travel**, and
> the in-place fix costs one table cell.

---

## What is still missing before this can be generated

| | |
|---|---|
| ~~**The map's shape**~~ | **drawn 2026-08-02** — see the graph above. Overrule any edge freely |
| **Prose** | every `ExamineText`, the nine freed lines, two blight descriptions |
| **Hotspot regions** | authored in the editor over art, never generated |
| **Jigsaw forms** | which of the three forms each of the three ch1 jigsaws uses |
| **The material node counts** | *"seed thick"* is a rule, not a number. Deliberately generous and deliberately provisional — they are **lever 1** in `items.md` §7, the first dial to turn if play runs short |
| **Unit 1** | `ChapterId`, and the flag vocabulary this table is written against |
