# Chapter 2 ledger — the village

**Drafted 2026-08-02 by Claude from [`placement.md`](placement.md) §3, in the format settled by
[`ledger-ch1.md`](ledger-ch1.md).** One row per screen; filling it in **is** drawing the map.

> **Claude draws, Jeremy reacts** — settled 2026-08-02. The *Held / Blocked / Fouled / door* columns are
> forced by the bill. The graph is my choice; overrule any edge freely.

**Nineteen screens.** With chapter 1's seventeen that is 36, leaving ~22 for the mine against the ~58
total — which is the right shape, since chapter 3 carries twelve fights to this chapter's nine.

**This is the arsenal chapter and the tightest ordering constraint in the game.** Three damage types
arrive, four of the eight knowledge pockets are here, and `placement.md` §3 warns that breaking the chain
anywhere strands a creature with no answer. **The chain survives this map — the walk-through is below.**

---

## The rows

**Held** = a creature stands on it · **Blocked** = a tool opens it · **Fouled** = a blight is on it ·
**▸** = pocket door.

| # | id | name ± | exits | Held by | Blocked | Fouled | pocket ▸ | nodes ± | NPC / other |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `ch2_bridgehead` | The Bridgehead | ←ch1, 2 | — | — | — | — | **Cairnstone ×1** | arrival. The mended bridge behind you |
| 2 | `ch2_mill_lane` | Mill Lane | 1, 3 | **Stable Master #1** *(path,* **Warden***)* | — | — | ▸ **Bell Maul** | Scrap Iron, **Cairnstone ×1** | freed: the ostler |
| 3 | `ch2_first_house` | The First House | 2, 4 | — | — | — | — | Bitterroot ×2 | **CAMP.** Three painted states |
| 4 | `ch2_green` | The Village Green | 3, 5, **13** | — | — | — | — | **Cairnstone ×1**, Bitterroot | the hub, and the chapter's only fork |
| 5 | `ch2_apothecary` | The Apothecary's Shop | 4, 6 | **Apothecary #1** *(path)* | — | — | — | Bitterroot ×2 | freed → **Burning Oil.** Real scene |
| 6 | `ch2_shambles` | The Shambles | 5, 7 | **Stable Master #2** *(path,* **Warden***)* | — | — | ▸ **Armour family** | **Cured Hide ×3** | freed: the drover |
| 7 | `ch2_still_room` | The Still Room | 6, 8 | **Apothecary #2** *(path)* | — | — | ▸ **Fused traps** | Bitterroot, Tallow | freed: **Iris's mentor** → **Corrosive Draught.** Real scene |
| 8 | `ch2_taproom` | The Taproom | 7, 9 | **Innkeeper #1** *(path)* | — | — | — | Tallow ×2 | freed: the pot-boy |
| 9 | `ch2_inn_yard` | The Inn Yard | 8, 10 | — | — | — | — | Scrap Iron ×2, Cured Hide | the stables. **One of her scattered notes** *(silent — it reads, it grants nothing)* |
| 10 | `ch2_forge_row` | Forge Row | 9, 11 | **Innkeeper #2** *(path)* | — | — | ▸ **Pry Bar** | Scrap Iron ×2 | **the anvil** — field station, and **on disk here since 2026-08-05** as `ch2_forge_row_anvil`; it was in row 11 from 2026-08-02, against this row. Freed: the farrier |
| 11 | `ch2_smithy` | The Smithy | 10, 12 | **the Blacksmith** *(boss)* | — | — | ▸ **the Blacksmith** *(boss)* | — | his forge. The door is his place. **Body placed 2026-08-05** as `ch2.blacksmith` — the pocket door is still Jeremy's. **No station here**: the anvil belongs to row 10 and was moved out the same day — Jeremy, *"I don't want the smithy to be holding it"* — so the tools are forged before you meet him, not on his ground |
| 12 | `ch2_cart_road` | The Cart Road | 11, → ch3 | — | *(the repair)* | — | — | — | **MAJOR REPAIR 2** + jigsaw. ~~Gated on the boss memory~~ gated on the boss **flag** *(decision 10)*. Authored 2026-08-05 as `ch2_cart_road_cart` + `recipe_repair_mine_cart`; ~~the `RequiredFlag` goes on once the Blacksmith is placed~~ **the boss was placed on row 11 the same day and the `RequiredFlag` is on**: `defeated.ch2.blacksmith` — `defeated.`, not `freed.`, per Jeremy's ruling that the body dies with the demon *(see `Flags.Defeated`)*. **No creature stands on this screen**; he is one screen back at the forge |
| 13 | `ch2_back_lane` | Back Lane | 4, 14, 16, 19 | — | — | — | — | Cured Hide | the optional branch hub |
| 14 | `ch2_tannery` | The Tannery | 13, 15 | **Stable Master #3** *(optional,* **Warden***)* | — | **blight 1** | — | Cured Hide ×2 | |
| 15 | `ch2_drying_yard` | The Drying Yard | 14 | — | — | — | — | Scrap Iron ×2 | behind a Warden, so it is a real choice |
| 16 | `ch2_chapel` | The Chapel | 13, 17 | **Apothecary #3** *(optional)* | — | **blight 2** | — | Bitterroot ×2 | |
| 17 | `ch2_vestry` | The Vestry | 16, 18 | — | **Pry Bar** | **blight 4** | — | — | Blocked **and** Fouled — the tool-gated blight |
| 18 | `ch2_cellar` | The Cellar | 17 | — | — | — | — | **materials cache** | **the Barricaded Merchant.** Cured Hide + Scrap Iron in bulk |
| 19 | `ch2_barn` | The Barn | 13 | **Innkeeper #3** *(optional)* | — | **blight 3** | — | Tallow ×2 | |

**Counts against `placement.md` §3:** 9 fights *(6 path, 3 optional)* ✅ · 1 boss ✅ · 5 pockets *(4
knowledge + boss)* ✅ · 4 blights ✅ · 5 jigsaws *(4 blights + the cart)* ✅ · 3 Cairnstone ✅ · 1 camp ✅ ·
9 freed ✅ · 4 max-Resolve grants ✅ · the Merchant behind the Pry Bar ✅ · the anvil ✅.

---

## The graph

```
 ←ch1
   |
   1 bridgehead                    [Cairnstone]
   |
   2 mill lane ....... Stable Master #1  ▸Bell Maul   [Cairnstone]
   |                        (Warden — you cannot walk past)
   3 THE FIRST HOUSE — camp
   |
   4 VILLAGE GREEN ───────────────────────────────┐   [Cairnstone]
   |                                              |
   5 apothecary's shop .. Apothecary #1           13 back lane ──┬── 14 tannery .. Stable Master #3  ~blight 1
   |                        → Burning Oil                        |        |
   6 shambles ......... Stable Master #2 ▸Armour                 |        15 drying yard
   |                        (Warden)                             |
   7 still room ....... Apothecary #2   ▸Fused traps             ├── 16 chapel .. Apothecary #3  ~blight 2
   |                        → Corrosive Draught                  |        |
   8 taproom .......... Innkeeper #1                             |        17 vestry  [Pry Bar] ~blight 4
   |                                                             |            |
   9 inn yard ......... the stables · her note                                                |        18 cellar — THE MERCHANT
   |                                                             |
   10 forge row ....... Innkeeper #2    ▸Pry Bar                 └── 19 barn .. Innkeeper #3  ~blight 3
   |                      ▪ the anvil
   11 smithy  ▸THE BLACKSMITH (boss)
   |
   12 cart road — MAJOR REPAIR 2 → ch3
```

**One fork again, at the green** — same shape as chapter 1's crossroads and for the same reason: skipping
the optional third should be one decision, not five missed turnings. It is a village green rather than a
signpost, so the "there is more that way" reading comes from the painted scene instead of an examine.

> **Confirmed by Jeremy, 2026-08-02.** On the single fork: *"The green (4) is the only fork. Spine down the
> left, everything optional to the right. Same shape as chapter 1's crossroads — I think this setup is
> clean and makes sense for a village setting vs a wooded one."* **That last clause is the useful part**:
> the two chapters reuse one topology and read as different places anyway, which means the shape is doing
> structural work and the *painting* is carrying the variety. That is the division the whole three-pass
> plan assumes, and this is the first place it has been checked against a second chapter.
>
> **Row 13's four-way split stays.** I flagged it as the edge I most expected to move — four branches off
> one hub, of lengths 2 / 3 / 1, is the least regular thing in the chapter. Jeremy: *"I actually like the
> design and that they are different lengths."* **Kept as drawn, and the irregularity is the point** — a
> back lane where every turning runs a different distance reads like a place that grew, which a
> three-equal-spurs hub would not. Do not tidy it later on symmetry grounds.

---

## The chain survives — walked through, fight by fight

`placement.md` §3: *"Break that order anywhere and a creature stands on the path with no answer
available."* So it is worth checking rather than asserting.

| Screen | Fight | Vulnerable | Do you have it? |
|---|---|---|---|
| 2 | Stable Master #1 | Edge | **the Splitting Axe**, carried from ch1 ✅ |
| 5 | Apothecary #1 | Blunt | **Bell Maul** ← pocket on row 2, and its Cairnstone came from rows 1 and 2 ✅ |
| 6 | Stable Master #2 | Edge | ✅ |
| 7 | Apothecary #2 | Blunt | ✅ |
| 8 | Innkeeper #1 | **Caustic** | **Vitriol Flask** ← Corrosive Draught from freeing row 7, Cured Hide from row 6, recipe from row 7's pocket ✅ |
| 10 | Innkeeper #2 | Caustic | the Flask is **permanent** (rule 15) ✅ |
| 11 | **the Blacksmith** | Caustic | ✅ — *the chapter teaches the answer to its own boss* |

**Every path fight is answered by the screen before it, and nothing is answered by the optional branch.**

> **Row 7 → row 8 is where the camp rework starts doing real work, and it is the first time.** The Vitriol
> Flask is a **Fused** trap, and fusion happens **only at the camp bench** (`combat.md` §17) — not at the
> anvil, which is not reached until row 10. So beating Apothecary #2 sends you to the camp and **you walk
> back out**: 3 → 4 → 5 → 6 → 7 → 8, five screens through ground you have cleared, carrying the thing that
> unlocks the rest of the chapter.
>
> That is exactly the shape Jeremy asked for when camps became menus — *"it will add more of a cost of
> having to retravel than a pure overlay inventory."* **It is load-bearing here rather than decorative**,
> because it is the one moment in the game where the critical path routes through the camp.
>
> **And once is the right number — confirmed 2026-08-02.** Jeremy: *"at least you only have to rewalk
> once. I feel like a little rewalking will help add a little bit of down time and not make a constant
> grind forward as well."* **So the walk-out is pacing, not friction, and it must not be tuned away.** It
> reframes what the camp rework was for: `combat.md` §17 argued the menu removes a *chore*, and the honest
> reading is narrower — it removes the **repeated** walk and keeps the **single** one, which is the part
> that was doing work. Chapter 3 has no refill and one camp; **if its layout produces no walk-out at all,
> that is a flatness worth fixing rather than an efficiency to bank.**

**Rule 2 holds on every edge.** Path: SM1–(1,3) empty · A1–(4) empty, A1–SM2 · SM2–A2 · A2–Inn1 · Inn1–(9)
empty · Inn2–(9,11) empty. Branch: SM3, A3 and Inn3 each touch only the empty back lane, so no two of them
are neighbours. **The inn yard at row 9 exists to keep the two Innkeepers apart** — it is the one screen
here placed for a rule rather than for content, and it earns its keep as the stables.

> **Row 9 gets a hand-placed find — asked for and taken, 2026-08-02.** Jeremy, on the graph: *"can we add
> an item to pick up so there is something there?"* The ledger already gave it Scrap Iron ×2 and Cured
> Hide; my rendered graph labelled it *empty*, which was shorthand for *not Held, Blocked or Fouled* and
> read as *bare*.
>
> **Jeremy's ruling, and it corrects mine.** I said material nodes are wallpaper and cannot be the reason
> a screen exists. He: *"I'd rather have screens you grab an item or two than an empty screen just to
> fulfil not giving the same enemy twice."* **So nodes do count as content — a rule-2 spacer earns its
> place by being worth stopping on, and a handful of pickups clears that bar.** The thing to avoid is not
> *"a screen whose only content is nodes"*, it is *"a screen with nothing at all"* — a much easier bar and
> a much cheaper fix. It generalises to all three chapters: **every spacer screen must carry something to
> pick up, and none may be left bare on the grounds that it exists for a rule.** Row 9 takes a note on top
> of its nodes anyway, not because the nodes were insufficient but because the note had nowhere else to go.
>
> **What goes there: one of her scattered notes.** `placement.md` §8 settles that there are **four** —
> two that grant a recipe *(Deepmend and the engine's physick half, both chapter 3)* and **two that only
> read**, existing to foreshadow. Those two silent notes are **ratified content with no chapter assigned
> anywhere in the bill** — a real gap this question found. Row 9 takes the first of them; the second wants
> a similar home in chapter 1, and chapter 3 should not take either, because their whole job is to be
> read *before* the notes that matter.
>
> **The stables are the right room for it.** Rule 3's demon *"dropped them carelessly, out of order,
> worthless to it"* — a note in the straw of a coaching yard is exactly that, and the Stable Masters are
> this chapter's Warden, so the yard already belongs to them thematically. **It also costs the bill
> nothing:** a silent note is not a recipe, not a material and not a fight, so no count in §3 moves.

**Rule 5 holds:** all four knowledge pocket doors sit on Held, un-Fouled screens (2, 6, 7, 10). Row 17 is
Blocked **and** Fouled, which is allowed — the rule forbids a *pocket door* on a Fouled screen, not a
blight behind a tool.

**Cairnstone: all three before the Apothecary**, as §3 requires — rows 1, 2 and 4, against row 5. ch2's
three plus ch1's one is four, against three ch2 sinks (Bell Maul, Limewashed Smock, Deadfall).

---

## Three things this chapter exposed

**1. Rule 11 fits chapters 2 and 3 exactly, and fails only in chapter 1 — which makes it fixable.**
`ledger-ch1.md` finding 1 called rule 11 *"a statement about the game, not about every fight"* and
suggested softening it. **Laying out chapter 2 shows that is the wrong fix**, because the rule is not
approximately true here — it is exactly true:

| | optional fights | blights behind an optional fight | match? |
|---|---|---|---|
| **ch1** | 5 | 1 | ✗ — four spare |
| **ch2** | 3 | 3 | ✅ exact |
| **ch3** | 5 | 5 | ✅ exact |

*(Each chapter also has one further blight behind that chapter's tool, which is a separate pattern and is
consistent in all three.)*

~~**So rule 11 is a real rule with exactly one exception, and the exception has a name: the Messenger.**~~

> **Struck 2026-08-02 — the table above is right and the conclusion under it was wrong.** Prompted by one
> question from Jeremy: *"the Messenger breaks a rule?"* **It does not.** Counting the other way — *every
> blight sits behind an optional fight or behind that chapter's tool* — the rule is exact in **all three**
> chapters, 12 blights out of 12, and needs no exception at all. The table's ch1 column was also off by
> one: blight 2 is double-gated, sitting behind Messenger #1 *and* the Bill Hook, so two of ch1's five
> optional fights gate a blight rather than one.
>
> **The old wording could never have held in ch1** regardless of which creature was optional, because the
> optional-fight count comes from rule 1 and creature vulnerability while the blight count comes from the
> bill's deliberate 2 / 4 / 6 escalation. Two constraints, no common factor. `placement.md` rule 11 is
> restated, **rule 17** now carries *every optional fight must be worth having taken*, and full working
> is in [`ledger-ch1.md`](ledger-ch1.md) finding 1.
>
> **What the wrong version nearly cost:** naming a creature inside a rule would have hard-coded one
> chapter's arithmetic into the bill checker, where it would have read as law forever.

**2. `placement.md` §8 still says there are 16 Cairnstones in the world; there are 6.** Rule 13 says
*"finite by construction (12 and **6**)"*, and the per-chapter tables say 1 + 3 + 2 = **6**. The 16 is a
survivor from before the Deadfall was made permanent — that section is marked closed, and the stale
figure inside it was not swept. Left flagged rather than edited, per that section's own convention. The
sinks after the fix are four (Bell Maul, Mine Pick, Limewashed Smock, Deadfall), so **6 spots is 4 sinks
plus 2 slack** and the design is sound; only the sentence is wrong.

**3. The Warden is a placement tool and this chapter is where it pays.** `BlocksPath` is on all three
Stable Masters. Two of them sit on the spine at rows 2 and 6, so **the chapter's first fight is
unskippable** — right for the chapter that teaches three new damage types. The third sits at row 14 and
turns the drying yard's Scrap Iron into a genuine choice rather than a pickup. **No other creature in the
game has this**, so nowhere else can a fight be made mandatory by geometry; worth remembering when
chapter 3 wants the same effect and has to use the Snarl instead.

---

## What is still missing before this can be generated

| | |
|---|---|
| **Prose** | every `ExamineText`, nine freed lines — **two of them real scenes** (rows 5 and 7), the Merchant, the Blacksmith's death |
| **The camp's three states** | row 3 needs three painted backgrounds, switched on the rescue-flag count |
| **Hotspot regions** | authored in the editor over art |
| **Jigsaw forms** | which form each of the five ch2 jigsaws uses |
| **Node counts** | lever 1 in `items.md` §7 — deliberately generous, deliberately provisional |
| **Unit 1** | `ChapterId`, and the flag vocabulary this table is written against |
| **The second silent note** | row 9 takes one of the two; **the other still has no home.** Chapter 1 is where it wants to go — see the row 9 callout |
