# Chapter 3 ledger — the mine

Derived from `docs/placement.md` §4 and §5, `docs/items.md`, `docs/combat.md` §21–§23. Same format as
`ledger-ch1.md` and `ledger-ch2.md`: one row per Waking screen, and every row is a `.tres` the skeleton
pass can generate once unit 1 lands.

**23 screens** — exactly the §1 bill's `~23`. Chapter 1 came in at 17 against `~16` and chapter 2 at 19
against `~19`, so the three ledgers total **59 screens against the bill's ~58**. One over, in a row the
bill itself marks as the only guess.

**The lesson of the chapter:** you are spending, not acquiring. Twelve fights, one new tool, no refill,
no new material nouns, and a boss behind a gate that takes three separate trips to open.

---

## The rows

Ids follow `ch3_<place>`. **Held** = a creature stands on it · **Blocked** = something opens it ·
**Fouled** = a blight is on it · **▸** = pocket door.

| # | id | name ± | exits | Held by | Blocked | Fouled | pocket ▸ | nodes ± | NPC / other |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `ch3_adit` | The Adit | ←ch2, 2 | — | — | — | — | **Cairnstone ×1**, Heartwood, Bitterroot ×2 | arrival. The mine mouth, and a cairn for its dead |
| 2 | `ch3_head_frame` | The Head Frame | 1, 3 | **Soldier #1** *(path)* | — | — | — | Scrap Iron ×2, Tallow | freed: the sergeant |
| 3 | `ch3_dry_store` | The Dry Store | 2, 4 | — | — | — | — | Bitterroot ×2, Tallow ×2 | **CAMP.** The Blacksmith's spirit — the only occupant |
| 4 | `ch3_main_drift` | The Main Drift | 3, 5, **14** | — | — | — | — | Bitterroot ×2, Heartwood | **fork A.** The old workings run up and off |
| 5 | `ch3_pump_house` | The Pump House | 4, 6 | **Miner #1** *(path)* | — | — | ▸ **Mine Pick** | Scrap Iron, Tallow | freed: the pumpman. **+1 max Resolve** |
| 6 | `ch3_crosscut` | The Crosscut | 5, 7 | **Beast — the crosser #1** *(path)* | — | — | ▸ **the lit knot** *(Flare)* | Bitterroot ×2 | **carcass ×1.** A black cord runs into the rock |
| 7 | `ch3_stope` | The Stope | 6, 8 | **Soldier #2** *(path)* | — | — | ▸ **the high knot** *(Float)* | Tallow ×2 | freed: the powder-monkey. A second cord |
| 8 | `ch3_sump` | The Sump | 7, 9, **18** | — | — | — | — | **Cairnstone ×1**, Bitterroot ×2, Tallow | **fork B.** The wet level drops away here |
| 9 | `ch3_long_drift` | The Long Drift | 8, 10 | **Beast — the crosser #2** *(path)* | — | — | — | Bitterroot ×2 | **carcass ×1** |
| 10 | `ch3_winze` | The Winze | 9, 11 | **Miner #2** *(path)* | — | — | ▸ **the far knot** *(Suppress)* | Tallow ×2 | freed: the winze-man. The third cord |
| 11 | `ch3_deep_gallery` | The Deep Gallery | 10, 12 | **Beast — the crosser #3** *(path)* | — | — | — | Bitterroot ×2, Tallow ×2 | **carcass ×1** |
| 12 | `ch3_snarl` | **The Snarl** | 11, 13 | — | **three cuts** | — | — | — | the passage knotted shut. Three cords leave it |
| 13 | `ch3_tear` | The Tear | 12 | — | — | — | ▸ **the Demon** *(boss)* | — | the finale. Iris is on the other side |
| 14 | `ch3_old_workings` | The Old Workings | 4, 15, 17 | — | — | — | — | Bitterroot ×2, Heartwood | fork A's hub |
| 15 | `ch3_rise` | The Rise | 14, 16 | **Soldier #3** *(optional)* | — | **blight 1** | — | Tallow ×2 | freed: the runner |
| 16 | `ch3_powder_store` | The Powder Store | 15 | **Miner #3** *(optional)* | — | **blight 2** | — | Tallow ×2, Pitch | freed: the shot-firer. **Her note — Deepmend** |
| 17 | `ch3_sealed_drift` | The Sealed Drift | 14 | — | **Mine Pick** | **blight 6** | — | Bitterroot ×2, Scrap Iron | Blocked **and** Fouled — the last tool-gated blight |
| 18 | `ch3_wet_level` | The Wet Level | 8, 19, 20 | — | — | — | — | Bitterroot ×2, Tallow | fork B's hub |
| 19 | `ch3_settled_chamber` | The Settled Chamber | 18 | **Beast — the settled one #1** *(optional)* | — | **blight 3** | — | Bitterroot ×2 | **carcass ×1.** It has been here a while |
| 20 | `ch3_drowned_stall` | The Drowned Stall | 18, 21 | **Beast — the settled one #2** *(optional)* | — | **blight 4** | — | Bitterroot ×2 | **carcass ×1** |
| 21 | `ch3_bone_pile` | The Bone Pile | 20, 22 | — | — | — | — | **Tallow ×3**, Bitterroot ×2, Pitch | what the settled ones left. The Tallow valve |
| 22 | `ch3_breach` | The Breach | 21, 23 | **Beast — the breach-thing** *(optional)* | — | — | ▸ **the exorcism engine** | — | **carcass ×1. MAJOR REPAIR 3** + the final jigsaw. **Her note — the engine's physick half.** **+1 max Resolve** |
| 23 | `ch3_wound` | The Wound | 22 | — | — | **blight 5** | — | Tallow ×2 | the worst of them, on the far side of the tear |

**Counts against `placement.md` §4:** 12 regular fights *(7 path, 5 optional)* ✅ · 1 boss ✅ · 6 pockets
*(2 knowledge + 3 knot + boss)* ✅ · 6 blights ✅ · 7 jigsaws *(6 blights + the engine)* ✅ · 2 Cairnstone
✅ · 1 camp ✅ · **6 freed** ✅ · 2 max-Resolve grants ✅ · 6 carcass hotspots ✅ · 2 of her notes ✅ ·
the Snarl ✅.

---

## The graph

```
 ←ch2
   |
   1 adit                     [Cairnstone]
   |
   2 head frame ....... Soldier #1
   |
   3 THE DRY STORE — camp · the Blacksmith's spirit
   |
   4 MAIN DRIFT ─────────────────────────┐  fork A
   |                                     |
   5 pump house ....... Miner #1         14 old workings ──┬── 15 rise .. Soldier #3   ~blight 1
   |                     ▸ Mine Pick                       |        |
   6 crosscut ......... Beast crosser #1                   |        16 powder store .. Miner #3  ~blight 2
   |                     ▸ the lit knot  ·cord             |             (her note — Deepmend)
   7 stope ............ Soldier #2                         |
   |                     ▸ the high knot ·cord             └── 17 sealed drift  [Mine Pick] ~blight 6
   |
   8 SUMP ───────────────────────────────┐  fork B          [Cairnstone]
   |                                     |
   9 long drift ....... Beast crosser #2 18 wet level ──┬── 19 settled chamber .. Beast  ~blight 3
   |                                                    |
   10 winze ........... Miner #2                        └── 20 drowned stall .... Beast  ~blight 4
   |                     ▸ the far knot  ·cord                    |
   |                                                              21 bone pile — Tallow valve
   11 deep gallery .... Beast crosser #3                          |
   |                                                              22 THE BREACH .. the breach-thing
   12 THE SNARL  ◄── the three cords run back to 6, 7 and 10        ▸ the exorcism engine
   |                                                                 MAJOR REPAIR 3 · her note
   13 THE TEAR   ▸ THE DEMON  (boss)                                 |
                                                                     23 the wound  ~blight 5
```

**Two forks, and this is the one place I departed from the shape you confirmed twice.** Chapters 1 and 2
each have a single fork — the crossroads and the green — and you liked both. **Chapter 3 has two: the
Main Drift at row 4 and the Sump at row 8.** The reason is load, not variety: chapter 3 carries **5
optional fights, 6 blights, the engine and the major repair**, roughly double chapter 2's optional half.
Hung off one hub that is a ten-screen lobe against a thirteen-screen spine, and *"do I want the optional
content"* collapses into one yes/no answered in the chapter's first two minutes.

**Two forks make it two decisions, taken eight screens apart, and they are not the same decision.** Fork A
is dry, above, and short — three screens, two people, the tool-gated blight. Fork B is wet, below, and
long — six screens, no people at all, and the ending at the end of it. **A player can take one and skip
the other**, which is a choice neither earlier chapter offers. Overrule it and everything below rows 14
and 18 hangs off a single hub instead; nothing else in the ledger moves.

---

## The Snarl, and why the cords are on those three screens

`placement.md` §4: the three knot doors *"sit on three of the seven critical-path fight screens"*, and
the Snarl's three cords are how you find them. **Rows 6, 7 and 10** — the crosscut, the stope and the
winze. Three different creature types hold them, which is incidental but reads well.

| | door | verb | why that screen |
|---|---|---|---|
| **the lit knot** | row 6 | **Flare** | the first Beast. The cord is the first one you see |
| **the high knot** | row 7 | **Float** | a stope is a tall chamber — the door is above you |
| **the far knot** | row 10 | **Suppress** | a winze goes down. It is the deepest of the three |

**The cords are visible from the first pass and do nothing.** On rows 6, 7 and 10 a black cord runs out
of the wall into the rock from the moment the screen is first entered — examinable, inert, unexplained.
**Reaching the Snarl at row 12 is what makes them mean something**, and the player should recognise all
three immediately rather than be told where to go. This is the one piece of ch3 authoring that has to be
in the art before it can be in the code.

**The backtrack is one sweep, and it is the chapter's shape.** From row 12 you walk up to 10, 7 and 6,
cutting as you go, then back down: about twelve screen transitions in a chapter with no refill. That is
deliberate — it is the *"three ways to open"* §4 asks for, and it is the biggest walk in the game.

> **This answers the flatness risk `ledger-ch2.md` flagged.** Chapter 2's single camp round-trip prompted
> the note that *"chapter 3 has no refill and one camp; if its layout produces no walk-out at all, that is
> a flatness worth fixing."* **It produces two.** The Snarl sweep is one. The other is the engine: you
> cannot build it until you hold **8 Graveflower**, the last ch3 blight is at row 23 on the far side of
> the breach, and the bench you build it on is at row 22 — so the last flower is always fetched *past*
> the place you go back to. **The good ending is literally a walk back**, which is the phrasing
> `tethered-dies-untethered-survives` already uses for it.

---

## Rule check against `placement.md` §5

**Rule 1 — a path creature must be beatable with what you can already make.** All three ch3 types are
covered by chapter 2 and none of the coverage is new:

| path fight | first at | vulnerable to | you hold it because |
|---|---|---|---|
| **Soldier** | row 2 | **Blunt** | the Bell Maul, ch2 row 2 — the chapter's first pocket |
| **Miner** | row 5 | **Fire** | Burning Oil, ch2 row 5 — Apothecary #1, a path fight |
| **Beast** | row 6 | **Caustic** | the Corrosive Draught, ch2 row 7 — Apothecary #2, a path fight |

**Both Fire and Caustic are trap-only** (`items.md` §3), so every Beast and every Miner in this chapter is
a trap fight rather than a weapon fight. That is the chapter reading as *spend* rather than *swing*, and
it falls out of the vocabulary rather than being imposed on it.

**Rule 2 — no two of the same creature adjacent.** Spine: 5 Miner–6 Beast–7 Soldier, then 9 Beast–10
Miner–11 Beast. Fork A: 15 Soldier–16 Miner. Fork B: 19 and 20 are both Beasts and **both hang off the
empty hub at 18**, so they are not adjacent; 20 Beast–**21 empty**–22 Beast is why the bone pile exists.

> **Row 21 is this chapter's row 9** — a screen placed to keep two Beasts apart. Per your ruling on the
> inn yard it is not left bare: **Tallow ×3, Bitterroot ×2 and Pitch**, and it is the chapter's stated
> Tallow valve, so it is the single most worthwhile spacer in the game.

**Rule 4 — screens should not feel empty.** Six screens hold no fight, no pocket and no blight: rows 1, 4,
8, 14, 18 and 21. Every one carries nodes; rows 1 and 8 carry the Cairnstone, and row 21 the Tallow valve.

**Rule 5 — pocket doors on Held screens, never Fouled.** Mine Pick (5, Held) · lit knot (6, Held) · high
knot (7, Held) · far knot (10, Held) · the engine (22, Held) — **none Fouled** ✅. §4's stricter form for
the knots specifically — *"Held or clear, never Fouled"* — holds: all three are Held. The Demon's door at
13 sits on a clear screen, matching the Lost Trapper at ch1 row 9 and the Blacksmith at ch2 row 11.

**Rule 6 — eight knowledge pockets, eight max-Resolve grants.** ch3 contributes 2, at rows 5 and 22. The
three knots and the Demon grant none ✅. Total across the game: 2 + 4 + 2 = 8 ✅.

**Rule 8 — the final fight must be winnable on path materials alone.** The Snarl needs the three knot
cuts and nothing else; all three doors are on the path. The engine, the notes and every blight are
optional. **You can walk from row 1 to row 13 touching neither fork** ✅.

**Rule 11 — every blight sits behind an optional fight or behind that chapter's tool.** All six: blights
1–5 behind the five optional fights *(15, 16, 19, 20, 23)*, blight 6 behind the Mine Pick at 17. **No
blight in this chapter is lying on the path** ✅. *(This is the restated form — see finding 1.)*

**Rule 17 — every optional fight must be worth having taken.** All five gate a blight, and the
breach-thing at row 22 also gates the exorcism engine, her second note and the major repair ✅.

**Rule 13 — nothing optional competes for a scarce material.** Graveflower's only sink is the engine, and
the engine is itself optional. Cairnstone's only ch3 sink is the Mine Pick, on the path, paid for by the
cairn at row 1 ✅.

**Rule 16 — every fight once.** 12 + 1, each a different individual, none returning ✅.

---

## Four things this chapter exposed

**1. Rule 11 is now confirmed exact in two chapters out of three, and chapter 1 is the only miss.**
`ledger-ch1.md` finding 1 proposed softening the rule; `ledger-ch2.md` finding 1 argued that was the wrong
fix because ch2 lands it exactly. **Chapter 3 lands it exactly too — 5 optional fights, 5 blights.**

| | optional fights | blights behind an optional fight | match? |
|---|---|---|---|
| **ch1** | 5 | 1 | ✗ — four spare |
| **ch2** | 3 | 3 | ✅ exact |
| **ch3** | 5 | 5 | ✅ exact |

> **Superseded the same day, and the exception was never needed.** I proposed restating rule 11 as *"a
> real rule with one named exception, the Messenger."* Jeremy: *"the Messenger breaks a rule?"* **Checking
> that found the actual answer: the rule was counting from the wrong end.** Inverted — *every blight sits
> behind an optional fight or behind that chapter's tool* — it is exact in all three chapters, **12 of 12,
> no exception**. The old form asked every optional *fight* to yield a blight, and the count of optional
> fights is set by rule 1 and creature vulnerability while the count of blights is set by the bill's
> 2 / 4 / 6 escalation; **two independent constraints that do not divide.** `placement.md` rule 11 is
> restated and **rule 17** carries the obligation the old wording was protecting. Full working in
> [`ledger-ch1.md`](ledger-ch1.md) finding 1.
>
> **The lesson is worth more than the fix.** A rule that fails in one chapter looks like it needs an
> exception, and *naming a creature in it* would have written a coincidence of chapter 1 into a law about
> the whole game — permanently, and in the bill checker. **The question that dissolved it took one line to
> ask.**

**2. The second Cairnstone spot has no sink, and this is the first place that is visible rather than
stated.** §4 says ch3 gets *"2 spots — one for the Mine Pick, one spare. Not abundant; there is nothing
here to spend it on."* Laid out, that is a harvestable object at row 8 that no recipe in the game can
consume. `items.md` §1 lists Cairnstone's total sinks as **four permanent things** — Bell Maul, Limewashed
Smock, Mine Pick, Deadfall — against **6 world spots** (1 + 3 + 2).

**Two spare, and rule 15 says spares can never become useful later**, because nothing crafted is
consumed. **Three options, none urgent:** cut the row 8 spot and let ch3 have one; leave it as slack
against a mis-sequenced player *(the honest reading — a player who spends Cairnstone in the wrong order
is not softlocked)*; or find it a fourth sink.

> **Ruled 2026-08-02: both spots stay.** Jeremy: *"more is better than less imo and maybe we will write a
> use for the 2 down the line but it's a low priority for now."* **So the spare is deliberate slack, not
> an oversight** — and the reason it is safe to leave is rule 15: nothing crafted is consumed, so a spare
> can never become a shortage. **The direction to keep in mind is that a fifth Cairnstone sink can be
> added later without touching any map**, because the material is already in the ground; only a recipe
> would change. **Do not cut a spot to tidy the count** — that is the move that would foreclose it.
>
> *(Separate from the §8 count discrepancy already flagged: §8 still says 16 Cairnstones exist, rule 13
> and the per-chapter tables say 6.)*

**3. The camp is eight screens from the deepest fight, and there is no refill to make the trip worth
it.** Row 3 is the only bench in the chapter, fusion happens only at a camp bench, and row 11 is eight
screens down. **This is not a problem for supply** — ch3 grants no refill by design, so nobody walks up
for Vitality — but it does mean **any fusing a player wants to do mid-chapter is a menu-in plus a
full-length walk-out.**

That is the pacing beat you asked for, at its maximum. The fix, if one were ever wanted, is cheap and
reversible: moving the camp to row 8 halves the worst walk and costs the Dry Store its *"arrive, make
base, then choose"* placement right after the first fight.

> **Ruled 2026-08-02: it stays at row 3.** Jeremy: *"It seems like it is in a good spot."* **Consistent
> with the ch2 ruling that the walk-out is pacing rather than friction**, and this is the same call one
> chapter later and at four times the distance. **Row 3 is now settled, not provisional** — the flag left
> here is only that the Snarl sweep at row 12 does not touch the camp at all, so the chapter's real
> traversal load and its longest camp walk are two separate things and should be judged separately in
> play.

**4. Every Beast fight in the chapter is a trap fight, and the trap is the same trap six times.** The
Beast is Caustic-vulnerable, Caustic is trap-only, and the Corrosive Draught is the only Caustic source in
the game. **Six of the twelve fights therefore open the same way** — and three of those are on the path,
so a player who ignores both forks still does it three times.

**This is a variety risk rather than a rule violation**, and the levers are already in the vocabulary:
the fused traps from ch2 row 7, the three Beast *variants* differing in numbers rather than type, and the
Bell Maul still working at half damage since **the wrong type halves and never zeroes**. **What it argues
for is that the three crossers should escalate hard in numbers**, so the sixth Beast is not the first
fight replayed — which is exactly what §4's *"the three path Beasts carry the escalation"* already says.
Flagging it here because the ledger makes the repetition countable for the first time.

> **Ruled 2026-08-02, and the answer reframes the whole chapter.** Jeremy: *"This is okay because beasts
> will feel like something new and exciting at the end of the game."* **The Beast is the only non-human
> enemy in the game** — every other creature is a person on a tether, and the Beast frees nobody. It
> arrives in the last chapter, after nine humans, and it is the *only* thing the player has not seen
> before. **Novelty absorbs the repetition**, and six sightings is the right count for a thing you want to
> register as a new category rather than a one-off.
>
> **It also explains a placement that otherwise looks arbitrary:** the Beast takes six of the twelve
> slots — half the chapter — where every other creature in the game gets three. That is not a balance
> figure, it is *"the mine has something else in it"*, and the six-way repetition is how the player learns
> that. **So the escalation across the three crossers is still the thing to author carefully**, but it is
> polish on a beat that already works rather than a fix for a defect.

---

## What is still missing before this can be generated

| | |
|---|---|
| **Prose** | every `ExamineText`, six freed lines, six blight descriptions, the Blacksmith's spirit, the Demon / Iris finale |
| **The three knot beats** | beat 1 / 2 / 3, fired on the **count** of cut knots, and **none may mention its own room** (`placement.md` §4) |
| **The cord art** | rows 6, 7 and 10 need the cord in the painting before the door can exist |
| **Hotspot regions** | authored in the editor over art |
| **Jigsaw forms** | which form each of the seven ch3 jigsaws uses — the engine's is the final one |
| **Node counts** | Bitterroot and Tallow are a **hard** abundance requirement here, not a preference. Lever 1 in `items.md` §7 |
| **The Demon's damage types** | deliberately unfixed (`placement.md` §4) |
| **The Dream creature type** | rows 6, 7, 10 and 22 open pockets that hold 3D pursuers, which are **not `EntityData`** — unit 6 authors its own resource |
| **Unit 1** | `ChapterId`, the flag vocabulary, and **the multi-flag gate** the Snarl at row 12 needs |
