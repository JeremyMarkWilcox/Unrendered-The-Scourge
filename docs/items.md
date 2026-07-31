# The item roster — drafted 2026-07-31

**What this is.** Every material, salve, trap, weapon, coat and tool in the Waking world, named and
costed in one pass. It exists because `combat.md` **§19** set the *shape* of the item economy and left
the *nouns* open, and the material list was the one genuinely blocking item in M4 — no station can be
authored until it exists, because every station is balanced against it.

**Read `combat.md` §19 first.** This file is the roster; §19 is the grammar, and where they disagree
§19 wins.

> **This is a document, not content.** Nothing here is a `.tres` yet, deliberately: the shapes it needs
> (`HotspotKind.Station`, `ItemCategory.Component`, `RecipeData.RequiresStation` /
> `RequiresBothTracks`) are unbuilt, so authoring files now would mean reopening every one of them at
> the editor pass. Roster now, files then.

> **What is locked and what is sketch, because they are not the same.**
>
> | | |
> |---|---|
> | **Names** | free forever. Display strings in a `.tres` — rename in the inspector whenever a better word turns up. |
> | **Quantities** | nearly free. One number per recipe, no knock-ons. |
> | **How many materials there are, and which track each belongs to** | **locked.** Every recipe is balanced against the list, so an eighth becoming a ninth is the thing that ripples. Jeremy, 2026-07-31: *"the amount is unchangeable unless we run into a roadblock or something is really worth adding later."* |
>
> Same distinction §4 makes about its trap roster: the rows are sketch, the grammar they are written in
> is decided.

---

## 1. Materials — eight, six common and two scarce

| Material | Track | First found | Role |
|---|---|---|---|
| **Bitterroot** | Physick | ch1 woodland | the base of every salve |
| **Tallow** | Physick | ch1 woodland | the binder |
| **Graveflower** | Physick · **SCARCE** | ch1, sparse throughout | **her work, and nothing else** |
| **Heartwood** | Craft | ch1 woodland | shafts, frames, trap bodies |
| **Pitch** | Craft | ch1 woodland | binder, and the fuel in fire traps |
| **Scrap Iron** | Craft | ch2 village | edges, heads, springs, mechanisms |
| **Cured Hide** | Craft | ch2 village | straps, grips, armour |
| **Cairnstone** | Craft · **SCARCE** | ch1 sparse → ch3 abundant | every forged weapon head and the best traps |

**Bitterroot and Tallow already exist as authored `.tres` items**, and `salve_clearmind` is already
`2 Bitterroot + 1 Tallow`. The existing content validates the vocabulary rather than needing rework,
which is the strongest argument available that eight is the right size.

**A common must never be the reason a craft is refused** (§19). If the player is ever short of
Heartwood, Heartwood was misclassified. The two scarce materials are the *only* things a chapter budget
contains, and they are the only things that make a craft a decision.

**Chapter 3 introduces no new common, on purpose.** The mine is where Cairnstone becomes abundant,
which gives the chapter an identity without widening the vocabulary — and it makes the mine the
**arsenal chapter**, which is the falling-action shape §19 asks for.

### Where the two scarce materials come from, and why it matters

**Cairnstone comes from breaking old cairns.** Finite by construction — a cairn is a one-time authored
spot — and `yard_cairn` already exists in `waystation_yard`, so the noun has a home in the world today.

**Graveflower comes from *restoring* the world, not from destroying anything.** Jeremy, 2026-07-31:
*"I want to fix every screen individually so you will be able to hoard this resource until you can use
it and have a large supply and be like 'ooh that's what that does' — and we are drawing a parallel to
the wife, Iris."*

That is the better version of an earlier draft where it grew wherever the Scourge had killed. Making it
a reward for **repair** rather than for death ties the material to her directly: you fix what she used
to tend, and the thing you find there turns out to be hers.

**Two consequences, and the first one changed a recipe.** Graveflower must be **single-purpose** or the
hoard gets spent before the revelation — see §2 below, where Deepmend moves into her cache for exactly
this reason. And **surplus Graveflower is never dead**, because everything it makes is consumable. That
matters because Jeremy has named the opposite as a pet peeve: *"I do hate finding things you are maxed
on in games."* A capacity upgrade can be wasted; a flower cannot.

### Starting per-chapter scarce budget

Guesses, to be tuned in play. These six numbers **are** the material economy.

| | ch1 woodland | ch2 village | ch3 mine |
|---|---|---|---|
| **Graveflower** | 2 | 4 | 6 |
| **Cairnstone** | 3 | 5 | 8 |

Gated by **story milestone**, never by a clock or a step counter (§19). Chapters 1 and 2 both refill
when the chapter 2 boss falls; chapter 3 is terminal.

---

## 2. Salves — four, and three of them are hers

| Salve | Track | Inputs | Learned |
|---|---|---|---|
| **Clearmind Salve** | Physick | 2 Bitterroot + 1 Tallow | **starting knowledge** — already authored |
| **Deepmend Salve** | Physick | 2 Bitterroot + 1 Tallow + 1 Graveflower | **her cache** → learned ch3 |
| **Burning Oil** | **fused** | 2 Pitch + 1 Tallow + 1 Graveflower | **her cache** → learned ch3 |
| **Corrosive Draught** | **fused** | 2 Bitterroot + 1 Cairnstone + 1 Graveflower | **her cache** → learned ch3 |

**For most of the game you can make exactly one salve.** That is the point, not a gap.

### Why Deepmend moved into her cache

An earlier draft had the greater healing salve learned in chapter 2. **It was moved 2026-07-31 because
it would have spent the Graveflower hoard before the payoff arrived** — the mystery resource has to stay
unspendable for the beat to land.

Moving it does three things at once. Graveflower becomes unmistakably **hers**, with a single purpose.
**Three** of her recipes now unlock together, so the hoard pays out three ways in one moment. And
chapter 2 loses nothing it will miss — its content is the Bell Maul, two coats and the Pry Bar.

**Two healing tiers work and three would not** (§19): the lesser one would normally be dead the moment
the greater exists, since a two-salve cap means you always bring two of the better — except the chapter
budget makes Graveflower genuinely finite, so Clearmind is what you make when it is spent. That is the
floor, the same shape as the baseline attack.

### Burning Oil and Corrosive Draught are *ingredients*, not consumables

They restore no Vitality, are never carried into a fight, and are consumed at the bench making traps.
**In code they are `Component`, not `ItemCategory.Salve`** — both static predicates key off the
category, and both land correctly, since Components survive death and hers are irreplaceable until the
recipes come back.

This is also why the question *"which per-fight cap does an offensive salve count against?"* is void:
salve slots are two healing items, trap slots are two traps, and nothing arbitrates.

---

## 3. Traps — two families, six items

| Trap | Type | Fires | Inputs | Family |
|---|---|---|---|---|
| **Snare** | — (it loses its next turn) | itself | Heartwood + Cured Hide | Mechanical |
| **Spiked Pit** | Edge | itself | Heartwood + Scrap Iron | Mechanical |
| **Deadfall** | Blunt, one-shot, large | itself | Heartwood + 1 Cairnstone | Mechanical |
| **Chime** | — (lures; reveals its condition) | itself | Scrap Iron + Heartwood | Mechanical |
| **Oil Slick** | **Fire**, over several turns | **your turn** | Burning Oil + Pitch | **Fused** |
| **Vitriol Flask** | **Caustic** | **your turn** | Corrosive Draught + Cured Hide | **Fused** |

**Two families, not six — so two knowledge pockets.** The Mechanical variants are Waking-world
refinements gated on holding the base, per §15's family rule, and that is the largest workload saving
in this document.

The "fires" column is not a property to memorise; it is readable from the object (§18). Oil waits for a
brand; a snare closes on its own.

**All six are consumed, always** — win, flee or driven off. Since §19 stopped destroying equipment on
death, a rigged trap is **the only thing a fight can take from you**, which is what keeps §1's overkill
column honest.

---

## 4. Weapons and armour — Fire and Caustic are trap-only

**Ratified 2026-07-31.** A permanent weapon cannot sensibly contain a consumable, and the payoff is
that **a creature vulnerable to Fire or Caustic can only be answered by preparation** — §4's *"you do
not out-fight it, you out-build it"* stated as a mechanic instead of a slogan. Jeremy: *"I want them as
traps, and it makes traps more useful beyond a lot of damage with no meaning."*

| Item | Slot | Effect | Inputs | Source |
|---|---|---|---|---|
| **Splitting Axe** | Weapon | Edge | — | **starting** — his own tool |
| **Bell Maul** | Weapon | Blunt | Heartwood + Scrap Iron + 1 Cairnstone | forged, ch2 |
| **Tanner's Coat** | Armour | wards Edge | 3 Cured Hide | forged, ch2 |
| **Padded Jack** | Armour | wards Blunt | Cured Hide + Tallow | forged, ch2 |
| **Limewashed Smock** | Armour | wards Caustic | Cured Hide + 1 Cairnstone | forged, ch3 |

**No armour wards Fire.** Jeremy: *"So fire is the only hazard you have to stomach."* Kept deliberately
— one unanswerable damage type gives the bestiary somewhere to escalate that no loadout can defuse.

**Armour is one family with three variants, so one pocket.** The armour slot starts **empty**: the first
coat should be a moment rather than a default.

Two weapons is not thin. With no damage numbers anywhere (§11) and vulnerabilities belonging to the
creature, Edge-or-Blunt is a genuine read of what you are fighting rather than a tier choice.

---

## 5. Tools

**Starting: the Craftsman's Kit.** What lets him use any station at all. **Never an item, never in a
slot, impossible to forget** (§19) — a tool you can leave behind is a trip back to camp for nothing,
which is §17's "if the player would never decline, do not ask" wearing travel boots.

**Forged — pure Zelda access, and the three tools are the three chapters:**

| Tool | Opens | Inputs |
|---|---|---|
| **Bill Hook** | bramble and thicket | Heartwood + Scrap Iron |
| **Pry Bar** | boarded doors, floorboards | 2 Scrap Iron |
| **Mine Pick** | weak stone, ore seams | Heartwood + Scrap Iron + 1 Cairnstone |

Each gates **access and nothing else**, which is invariant 5. None makes anything stronger.

---

## 6. Repair — two scales, and they are different systems

Jeremy described both *"fix every screen individually"* and *"3 is probably enough"* in one message.
Both are right, at different scales, and conflating them would have produced either dozens of key items
or three lonely set pieces.

| | Cost | Yields | Count |
|---|---|---|---|
| **Major repair** — the spine | a **reclaimed memory** + key items | passage, or the finale | **3** |
| **Minor restoration** — per screen | **commons only**, no memory, no key item | **Graveflower**, and the screen visibly changes | **many** |

**Commons-only is what makes the minor ones affordable to author.** Dozens of key items would be a
content mountain; a hotspot, a Heartwood cost, a flag and a changed background is an afternoon each. It
also gives commons a second sink so they are not purely trap-and-armour feed, and each one is a small
version of *"zone cleanses, sun comes out"* — already in the playable loop today.

**The three major repairs:**

| | Gates | Where |
|---|---|---|
| **The bridge** | woodland → village | end of ch1 |
| **The mine cart** | village → mine | end of ch2 |
| **The exorcism engine** | **nothing** — it unlocks the good ending | the finale |

Taught on a bridge, practised on a cart, and its third use is the climax. Same system all three times,
and the escalation costs nothing. See `combat.md` §20 for what the third one actually is.

---

## 7. Chapter by chapter — what is available when

| | **ch1 woodland** | **ch2 village** | **ch3 mine** |
|---|---|---|---|
| **Materials added** | Bitterroot, Tallow, Heartwood, Pitch, *(Graveflower and Cairnstone, sparse)* | Scrap Iron, Cured Hide | none — Cairnstone becomes abundant |
| **Salves craftable** | Clearmind | Clearmind | **+ Deepmend, Burning Oil, Corrosive** |
| **Traps** | Snare | + Spiked Pit, Deadfall, Chime | + Oil Slick, Vitriol Flask |
| **Weapon** | Splitting Axe *(starting)* | + Bell Maul | — |
| **Armour** | none | Tanner's Coat, Padded Jack | + Limewashed Smock |
| **Tool** | Bill Hook | Pry Bar | Mine Pick |
| **Major repair** | the bridge | the mine cart | the exorcism engine |

**Chapter 3 is where everything resolves at once** — her three recipes, both fused damage types, and an
abundant scarce material. That is the arsenal chapter and it is the deliberate falling action.

### Fire- and Caustic-vulnerable creatures must appear before chapter 3

Otherwise two of four damage types exist for one chapter. But the placement rule is **ratified
2026-07-31 and not optional** — and no gate can check it, because "is this creature on the critical
path" is a judgement about level design rather than a property of a `.tres`:

> **In chapters 1–2, a Fire- or Caustic-vulnerable creature guards something optional. Never the
> critical path.**

Then it is not a tax, it is a **locked door you can see through**: fight it expensively if you like
(the wrong type halves damage, it never zeroes it, and flight is guaranteed), or note it and move on.
That reads as anticipation. On the critical path it would read as friction — Jeremy's own word for the
risk was *"annoying"*, and annoying is a smell rather than a difficulty setting.

**This also fixes the last loose end in the refill schedule.** Backtracking to chapter 1 for materials
was a walk through cleared ground with nothing in it; now the creatures you could not kill are still
standing there and you can. **The restock trip and the come-back-later trip are the same trip**, and
neither was designed to make the other work.

---

## 8. The authoring bill this produces

Since a Craft family costs one knowledge pocket (§15):

| | Pockets |
|---|---|
| 3 access tools | 3 |
| Weapons — the Bell Maul; the Axe is starting | 1 |
| Armour — one family, three variants | 1 |
| Traps — Mechanical + Fused | 2 |
| **Knowledge pockets** | **7** |
| **Boss pockets** | **3** |
| **Total 3D pockets** | **10** |

Roughly three per chapter. **Accepted 2026-07-31** — Jeremy: *"10 is enough variation but not too many
runs as well. I can focus on 10 really polished levels."*

**Physick recipes cost no pocket.** They are found out here, in her notes, consistent with §15 and with
the journal's two sides.

---

## 9. Two lore notes that constrain authoring

Not decoration — both change how content gets placed.

### Her notes were scattered, his craft was stolen

Jeremy, 2026-07-31: her notes are strewn around the world *"from when the demon took her and dropped
them carelessly."*

**It earns its keep mechanically:** it explains why her notes are found in no sensible order, in
unrelated places — which is exactly how they will actually be authored. Lore covering for authoring
convenience is the cheapest art direction there is.

It also splits the two knowledge tracks into different *kinds* of violation, which they did not have
before:

| | What happened to it | Where it lives |
|---|---|---|
| **His Craft** | **stolen and scrambled** | locked inside the Dream |
| **Her Physick** | **discarded as worthless** | scattered across the world |

The demon wanted his knowledge and had no use for hers. That is a nastier characterisation than either
track carried alone — and it sets up the ending, where both trades together are what undoes it.

### She is not recovered until the end

Jeremy: *"You never actually get her back until the end, with the good ending."* So the Graveflower
stockpile is carried through the whole game unspent and unexplained, and her recipes returning **is**
her returning, mechanically, one chapter before she returns literally.

**And the stockpile turns out to be the foreshadowing** — see `combat.md` §20. In the final Dream pocket
**Iris is the anchor**, and severing her is the outcome where you did what she asked and lost her anyway.
That is only fair if the game hinted, and three things in this document hint without having been designed
to: **Graveflower you cannot spend on anything**, **her notes turning up in places the demon discarded
them**, and **three locked recipes in a cache you can hold but not reproduce.** All three say *her work
can still do something.*

So the roster carries the ending's fairness argument. Worth knowing before any of it is retuned: cutting
the unspendable stretch short — by giving Graveflower an early sink, or teaching her recipes sooner —
would not just soften an economy, it would delete the setup for §20.
