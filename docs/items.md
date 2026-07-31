# The item and creature roster — drafted 2026-07-31

**What this is.** Every material, salve, trap, weapon, coat and tool in the Waking world, named and
costed in one pass — and, since 2026-07-31, **every creature too** (§10). It exists because `combat.md`
**§19** set the *shape* of the item economy and left the *nouns* open, and the material list was the one
genuinely blocking item in M4 — no station can be authored until it exists, because every station is
balanced against it.

**Read `combat.md` §19 and §21 first.** This file is the roster; §19 is the item grammar and §21 is the
creature grammar, and where they disagree the grammar wins.

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
| **Graveflower** | Physick · **SCARCE** | ch1, sparse throughout | **the exorcism engine, and nothing else** |
| **Heartwood** | Craft | ch1 woodland | shafts, frames, trap bodies |
| **Pitch** | Craft | ch1 woodland | binder, and the fuel in fire traps |
| **Scrap Iron** | Craft | ch2 village | edges, heads, springs, mechanisms |
| **Cured Hide** | Craft | ch2 village | straps, grips, armour |
| **Cairnstone** | Craft · **SCARCE** | ch1 sparse → ch3 abundant | every forged weapon head and the best traps |

**Bitterroot and Tallow already exist as authored `.tres` items**, and `salve_clearmind` is already
`2 Bitterroot + 1 Tallow`. The existing content validates the vocabulary rather than needing rework,
which is the strongest argument available that eight is the right size.

> **Revised 2026-07-31 — every material now comes from the world.** Animals were cut from the bestiary
> entirely (§10), which removed the two creature-sourced materials at a stroke: **Tallow and Cured Hide
> are scavenged goods, not harvests.** Tallow is candles and a rendering pot; Cured Hide is the tannery's
> stock. Both are more natural where they now are.
>
> The consequence is a channel closing, and the result is *simpler* than what it replaced. Four sources,
> each producing exactly one kind of thing, with nothing crossing:
>
> | Source | Produces |
> |---|---|
> | **Dream pockets** | Craft knowledge — **always, and nowhere else** |
> | **Her notes, found out here** | Physick knowledge — always, and nowhere else |
> | **The world** | all eight materials |
> | **Freed NPCs** | objects — tools, key items, gifts |
> | **Fights** | key items only, never bulk |
>
> The two knowledge rows are the one place a player could reasonably get confused, and they do not,
> because the split is the journal's two sides: **his craft is locked in the Dream, her notes are strewn
> out here** (§9). Two kinds of knowledge, two homes, no guessing.
>
> `EntityData.HarvestItemId` goes unused as a result. See `combat.md` §21.

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

**Graveflower must be single-purpose or the hoard gets spent before the revelation.** That constraint
survived every revision; what changed is *which* single purpose.

> **Revised 2026-07-31 — Graveflower's purpose is the exorcism engine.** See `combat.md` §20 for the full
> argument. In short, an earlier draft spent it on three consumable salves against a total world budget
> of **twelve units**, which cannot supply anything you burn every fight. That was an arithmetic bug, not
> a balance question.
>
> | | |
> |---|---|
> | **The engine costs** | **8** |
> | **The world holds** | **12** (2 / 4 / 6) |
>
> So the material gathered by restoring the world, one screen at a time, is the material that saves your
> wife — and **the falling-action activity turns out to be the good ending's currency.** The slack of 4
> is deliberate: you may miss a third of the repairs and still get there.

**One earlier claim in this section is now false and is retained as a correction.** It read: *"surplus
Graveflower is never dead, because everything it makes is consumable"* — written against Jeremy's peeve,
*"I do hate finding things you are maxed on in games."* With a single one-off sink, surplus **is** inert.

Two things make that acceptable, and they should be checked rather than assumed:

- **There is no cap, so you are never *maxed*** — which is the specific thing the peeve is about. You are
  carrying extra, not being told a pickup was wasted.
- **The engine is built at the finale**, so the window in which surplus is knowably dead is minutes long.
  Before that, every flower is buffer against a repair you skipped.

If it still reads badly in play, the fix is to lower the engine's cost rather than to add a second sink —
a second sink would put the hoard's whole meaning back at risk.

### Starting per-chapter scarce budget

Guesses, to be tuned in play. These six numbers **are** the material economy.

| | ch1 woodland | ch2 village | ch3 mine |
|---|---|---|---|
| **Graveflower** | 2 | 4 | 6 |
| **Cairnstone** | 3 | 5 | 8 |

Gated by **story milestone**, never by a clock or a step counter (§19). Chapters 1 and 2 both refill
when the chapter 2 boss falls; chapter 3 is terminal.

---

## 2. Salves — four: two are hers, two are the village's

**Revised 2026-07-31. No salve uses a scarce material any more** — see §1. All four run on commons,
because all four are consumables and a fixed world budget cannot supply a consumable.

| Salve | Category | Inputs | Learned |
|---|---|---|---|
| **Clearmind Salve** | Salve | 2 Bitterroot + 1 Tallow | **starting knowledge** — already authored |
| **Deepmend Salve** | Salve | 3 Bitterroot + 2 Tallow | **her notes**, found ch3 |
| **Burning Oil** | **Component** | 2 Pitch + 1 Tallow | **a freed apothecary**, ch2 |
| **Corrosive Draught** | **Component** | 2 Bitterroot + 1 Scrap Iron | **a second freed apothecary**, ch2 |

### Two apothecaries, one recipe each

Jeremy, 2026-07-31: *"maybe there are two apothecaries that each give you one of the recipes? This makes
more sense in a city structure too, that people are more involved with things."*

**This is the repetition mechanic paying rent rather than being tolerated.** The Apothecary is one of the
nine creature types (§10), so she appears three times in the village and each defeat frees a different
person. Two of those three know herbs well enough to give you something.

Three things it buys:

- **Staggered rather than dumped.** You learn Fire, then later Caustic, instead of receiving both in one
  handout — and clearing *more* of the village is what completes your combat vocabulary.
- **One of them is Iris's mentor**, which is the scene that already existed looking for a home.
- **It resolves the objection cleanly.** An earlier draft blocked her from giving recipes in chapter 2 at
  all. That objection was **wrongly stated as timing** — Jeremy: *"I don't know why chapter 2 of 3 is
  early for a recipe from winning a battle."* He was right. The real constraint was never *when*, it was
  **never Graveflower**, and now that nothing consumable touches Graveflower the constraint is gone.

### Chapter 2 completes the arsenal, and chapter 3 is about the engine

The knock-on is a pacing change worth stating plainly, because it inverts the earlier plan. With both
damage components arriving in chapter 2, **the mine introduces no new combat capability at all** — its
content is the Mine Pick, the Limewashed Smock, abundant Cairnstone, and the exorcism engine.

That is better than saving new toys for last. **You enter the final chapter fully armed, and the
remaining work is construction** — which is exactly the falling-action shape §19 asked for, and it means
the last chapter's difficulty comes from what you are being asked to *do* rather than from a fresh tier
of things to learn.

### Two healing tiers, and why the lesser one is not dead

**Deepmend does not obsolete Clearmind, but the reason changed.** It used to be that Deepmend needed
scarce Graveflower. Now both run on commons, so the protection has to come from cost: **5 material units
against 3.** With a per-chapter material budget, an all-Deepmend loadout costs about two-thirds more, so
Clearmind is what you carry into ordinary fights and Deepmend is what you make when you expect trouble.

That is a weaker guard than the old one and it should be watched in play. If Deepmend turns out to be
strictly correct, widen the gap in units rather than reintroducing a scarce ingredient.

### Burning Oil and Corrosive Draught are *ingredients*, not consumables

They restore no Vitality, are never carried into a fight, and are consumed at the bench making traps.
**In code they are `Component`, not `ItemCategory.Salve`** — both static predicates key off the
category, and both land correctly: Components survive death, and dying with a batch of Burning Oil in
the pack should cost you the materials to rebrew, not the ability to fight Fire-vulnerable creatures.

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

| | Gates | Cost | Where |
|---|---|---|---|
| **The bridge** | woodland → village | commons + a memory from **the ch1 boss pocket** | end of ch1 |
| **The mine cart** | village → mine | commons + a memory from **the ch2 boss pocket** | end of ch2 |
| **The exorcism engine** | **nothing** — it unlocks the good ending | **8 Graveflower** + commons + **its own pocket** + **one of her notes** | the finale |

**The first two cost no extra pocket; the engine does.** Beat the boss, get the memory, fix the thing,
cross into the next chapter — that is why the bridge and the cart are free. The engine cannot borrow its
chapter's boss pocket, because you build it *before* that fight rather than after, so it needs one of its
own. It is also **the only recipe requiring both tracks** — his Craft from a pocket, her Physick from a
scattered note. See §8, where this was undercounted in the first draft.

Taught on a bridge, practised on a cart, and its third use is the climax. Same system all three times,
and the escalation costs nothing. See `combat.md` §20 for what the third one actually is.

**The engine costs 8 of the world's 12 Graveflower** — the number is set in §1 and repeated here because
this is the only recipe that spends it. Two things follow that a reader of §6 alone would miss:

- **The slack is 4, and it is deliberate.** Enough that a careless player is not punished for missing a
  bloom or two; not so much that the hoard feels like loose change.
- **The recipe is the warning.** It appears in the journal when you learn it, listing *8 Graveflower*,
  before you can build it. Nothing in the game says out loud *save these* — the recipe saying what it
  needs is the fair-play notice, and there is not meant to be a second one.

Chapter 3 alone yields 6, so **a player who arrives with nothing can still reach 8 without backtracking
much**, and backtracking to chapters 1–2 for the rest is always open. Invariant 12 forbids a save that
cannot finish; this is what makes it true rather than assumed.

---

## 7. Chapter by chapter — what is available when

**Revised 2026-07-31**, following the move of Burning Oil and Corrosive Draught out of her cache and
into the two freed apothecaries.

| | **ch1 woodland** | **ch2 village** | **ch3 mine** |
|---|---|---|---|
| **Materials added** | Bitterroot, Tallow, Heartwood, Pitch, *(Graveflower and Cairnstone, sparse)* | Scrap Iron, Cured Hide | none new — **but everything gets abundant**, see below |
| **Salves craftable** | Clearmind | Clearmind | **+ Deepmend** |
| **Components** | none | **+ Burning Oil, Corrosive Draught** | — |
| **Traps** | Snare | + Spiked Pit, Deadfall, Chime, **Oil Slick, Vitriol Flask** | — |
| **Weapon** | Splitting Axe *(starting)* | + Bell Maul | — |
| **Armour** | none | Tanner's Coat, Padded Jack | + Limewashed Smock |
| **Tool** | Bill Hook | Pry Bar | Mine Pick |
| **Damage types available** | Edge | Edge, Blunt, **Fire, Caustic** | all four |
| **Major repair** | the bridge | the mine cart | **the exorcism engine** |

**Chapter 2 is now the arsenal chapter and chapter 3 is the construction chapter.** That is a real change
of shape from the previous draft, which loaded everything into the mine, and it is better: the middle
chapter is where a player should be learning, and the last one is where they should be *using* what they
have while the story closes. Chapter 3's only new capability is the Limewashed Smock and the Mine Pick;
everything else it offers is material and the engine.

### Chapter 3 is rising action, not falling — and its supply has to match

**An earlier line here called chapter 3 "the deliberate falling action". That was wrong and is
withdrawn.** Jeremy: *"less pockets in level 3 and more beasts to fight will help with the rising action,
rather than being stuck in the dream world thinking about progressing in the awake 2D world."* The Dream
is where you go to *think*, and the last chapter should not be thinking. Chapter 3 is short on Craft and
long on pressure: **roughly twelve regular fights against nine in each earlier chapter**, because the
Beast appears five or six times (§10, `combat.md` §21).

**That creates one real risk and it has a concrete fix.** Twelve fights against a finite material budget
means a player runs dry and backtracks — in the final chapter, which is exactly the momentum the change
was made to build. Two conditions, both authoring rules:

1. **Chapter 3 is abundant in every common, not only Cairnstone.** The scarce-material line in the table
   above used to be the whole promise; it is not enough.
2. **The Beast supplies what it costs.** A slain Beast leaves a carcass yielding **Tallow and Cured
   Hide**, so the fights that spend chapter 3's materials are the fights that stock them. Bitterroot and
   Pitch have to be seeded by hand — a Beast cannot plausibly drop either.

That second one is not an economy patch bolted on. It is the same hotspot-and-flag machinery that places
a freed person, pointed at a corpse instead — see `combat.md` §21, *"you can only harvest what was never
human."*

**Chapter 1 is deliberately thin — one damage type and one trap.** With Blunt withheld until the Bell
Maul, the woodland is where the player learns that a wrong-typed fight is *expensive rather than
impossible*, which is the lesson every later chapter builds on.

### A creature vulnerable to a type you cannot yet make guards something optional

**Ratified 2026-07-31, restated the same day.** The rule used to name chapters — *"in chapters 1–2, no
Fire- or Caustic-vulnerable creature on the critical path"* — and that stopped being right the moment
both types became learnable mid-chapter-2. What it was always protecting is the player's ability to
answer the fight, so the rule now says that directly:

> **A creature is only placed on the critical path if the player can already make the type it is
> vulnerable to by the time they reach it.** Otherwise it guards something optional.

Off the path it is not a tax, it is a **locked door you can see through**: fight it expensively if you
like (the wrong type halves damage, it never zeroes it, and flight is guaranteed), or note it and come
back. That reads as anticipation. On the path it would read as friction — Jeremy's own word for the risk
was *"annoying"*, and annoying is a smell rather than a difficulty setting.

**No gate can check this.** "Is this creature on the critical path" is a judgement about level design,
not a property of a `.tres`, so it stays an authoring rule enforced by reading §10's placement column.

**The Blacksmith is the case that looks like a violation and is not.** He is Caustic-vulnerable and he is
the chapter 2 boss, squarely on the path — but the apothecary who teaches Corrosive Draught is also on
the path and comes first. The chapter teaches the answer to its own boss. That is the intended shape for
every chapter, not an exception carved out for one fight.

**And the rule fixes the last loose end in the refill schedule.** Backtracking to chapter 1 for materials
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
| **The exorcism engine** — added 2026-07-31, see below | **1** |
| **Knowledge pockets** | **8** |
| **Boss pockets** | **3** |
| **Total 3D pockets** | **11** |

**Accepted at ten 2026-07-31** — Jeremy: *"10 is enough variation but not too many runs as well. I can
focus on 10 really polished levels."* **It is eleven**, and the eleventh was an undercount rather than an
addition.

### The engine needs a pocket, and I had priced it at zero

§6 lists the exorcism engine as a major repair, and major repairs cost *a reclaimed memory* — which is a
Dream pocket. For the bridge and the mine cart that memory comes out of the chapter's **boss** pocket,
which is why they cost nothing extra: beat the boss, fix the thing, cross into the next chapter.

**The engine cannot work that way, because you build it before the final fight, not after.** So its
knowledge has to come from somewhere else, and there is only one place it can come from.

**It is also the one recipe that needs both tracks.** `RecipeData.RequiresBothTracks` has been a named
shape in this document's header since it was written, with nothing to use it; §9 says *"both trades
together are what undoes it."* The engine is where that finally cashes out:

> **The exorcism engine costs a Dream pocket for his Craft, one of her scattered notes for her Physick,
> and 8 Graveflower.** It is the only recipe in the game that requires all three.

That is the two-track design paying off on the single object the whole story points at, and the first
draft of this table charged nothing for it.

**Beyond that one, the wall is the item list, not imagination.** A twelfth pocket means a twelfth *craft
family* — a fourth tool, a third weapon, a third trap family — and §1 locked the material and item counts
precisely so that this could not drift. Jeremy: *"like I say, with falling action being a little light is
probably good."*

**Physick recipes cost no pocket.** They are found out here, in her notes, consistent with §15 and with
the journal's two sides. The two damage components cost no pocket either — they come from freed people,
which is the third source in §1's table.

### Where the ten fall, now that the chapters are fixed

The material and creature placements above determine this; it is not a free choice.

| | Knowledge pockets | Boss pocket | Total |
|---|---|---|---|
| **ch1 woodland** | Bill Hook, Mechanical traps | the Lost Trapper | **3** |
| **ch2 village** | Pry Bar, Bell Maul, armour, Fused traps | the Blacksmith | **5** |
| **ch3 mine** | Mine Pick, **the engine** | the Demon | **3** |

**Chapter 2 is heavier and it should stay that way.** It is the arsenal chapter, so most of the Craft
lives there. Chapter 3 is deliberately light on Dream time — **not because there was nothing to put in
it, but because the last chapter should be pressure rather than contemplation.** Being pulled under to
solve a logic puzzle at the climax is a brake on the one stretch of the game that needs momentum.

**So the earlier note here — "if chapter 3 plays thin, split the armour family out" — is withdrawn.**
Chapter 3 is not thin, it is *fast*, and the escalation is in the Waking world: the Beast appears five or
six times instead of three, and the mine fills up with things that crossed. See §10 and `combat.md` §21.
Jeremy: *"less pockets in level 3 and more beasts to fight will help with the rising action, rather than
being stuck in the dream world thinking about progressing in the awake 2D world."*

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
stockpile is carried through the whole game unspent and unexplained, and Deepmend — her one late recipe —
returning **is** her returning, mechanically, a chapter before she returns literally.

**Rewritten 2026-07-31.** The previous version of this note claimed the foreshadowing rested on *three*
locked recipes in her cache. Two of those recipes now come from the village apothecaries instead (§2), so
that leg of the argument is gone and the note would otherwise be quietly wrong. What is left is
narrower and, on inspection, stronger:

- **Graveflower you cannot spend on anything, for two and a half chapters.** It is the only item in the
  game with no sink until the finale, and the journal never explains why you are picking it.
- **Her notes turning up in places the demon discarded them** — carelessly, out of order, worthless to it.
- **Deepmend arriving in chapter 3 from her own hand**, the first thing of hers you get back.

All three say the same thing: *her work can still do something.* And that is the whole setup, because
**the exorcism engine is built out of Graveflower** — the resource you have been hoarding without a
reason turns out to be the reason. See `combat.md` §20 and §21.

The ending it sets up also changed on 2026-07-31 and the direction reversed: **severing Iris now kills
her**, and the engine is the only route where she lives. That makes the hoard load-bearing rather than
thematic — a player who spent Graveflower freely would have been *mechanically* locked out of the good
ending, which is exactly why §1 moved every consumable off it and §6 sized the engine at 8 of 12 with
slack to spare.

So the roster carries the ending's fairness argument. Worth knowing before any of it is retuned: giving
Graveflower an early sink would not just soften an economy, it would put back a way to lose the good
ending by accident, hours before the game tells you the ending exists.

---

## 10. The bestiary — nine creatures and three bosses, drafted 2026-07-31

**Read `combat.md` §21 first.** That section decides *who survives, why the corrupted are all human, how
often each one appears, and what it costs in art and writing.* This section is the roster: the twelve
entities, their damage types, and where each one stands. Where they disagree, §21 wins.

**Why twelve and not more.** Four damage types give exactly **12 unique (vulnerable, resistant) pairs**,
so twelve is where the matrix runs out — a thirteenth creature would have to repeat a pair, and two
creatures that answer to the same tool are one creature wearing two costumes. The softer rule that agrees
with it: **creatures per chapter ≈ fights per chapter ÷ 2**, and at nine fights a chapter that lands on
three. Jeremy set the target himself — *"I like the sound of 13"* — and the matrix put it at 12.

### The nine — the trade is the stat block

Every regular creature is a person a demon turned into their own trade. **That is the whole design
method**, and it does three jobs at once: it generates nine silhouettes that are legible at 80 pixels, it
explains the damage type without a word of exposition, and it is the reason §21's repetition reads as an
ecology instead of as padding. A Logger answers to an axe because he *is* an axe.

| # | Was | Ch | Vulnerable | Resistant | Deals | Placement |
|---|---|---|---|---|---|---|
| 1 | **Logger** | 1 | Edge | Blunt | Edge | path |
| 2 | **Hunter** | 1 | Edge | Fire | Blunt | path — **Swarm** (a hunting party, several bodies in one encounter) |
| 3 | **Messenger** | 1 | **Fire** | Edge | Fire | **optional, all three sightings** — see below |
| 4 | **Stable Master** | 2 | Edge | Caustic | Blunt | path — **Warden** (`BlocksPath`) |
| 5 | **Apothecary** | 2 | Blunt | Caustic | Edge | path — **two of her three give a recipe** (§2) |
| 6 | **Innkeeper** | 2 | **Caustic** | Fire | Fire | path, but only after the apothecary |
| 7 | **Soldier** | 3 | Blunt | Fire | Edge | path — **Brute** |
| 8 | **Miner** | 3 | **Fire** | Blunt | Blunt | path |
| 9 | **The Beast** | 3 | **Caustic** | Edge | Caustic | path — **the only non-human, and the only one seen more than three times** |

**Checks this table passes, and they are the reason to keep it in this shape:**

- **All nine (vulnerable, resistant) pairs are distinct**, and no creature is vulnerable to what it
  resists.
- **Every damage type is a vulnerability at least twice** — Edge ×3, Fire ×2, Blunt ×2, Caustic ×2 — so
  no tool is a one-fight novelty.
- **Every creature is answerable when you meet it**, under §7's placement rule. Worked through below.

**Archetypes are derived, not authored** (§18 — there is no enum). Swarm is *several entries in one
encounter list*; Warden is `BlocksPath = true`; Brute is a higher `Damage` with the same `Durability`.
Keep durability roughly flat across all nine: difficulty comes from the type match, because a bigger
number makes a fight longer and §5 caps a fight at 3–6 turns.

### The Beast breaks two rules on purpose

**It is seen five or six times, not three**, increasing as the mine goes deeper. Everything else in the
table is fixed at three. That is chapter 3's plot rather than its difficulty curve — the portal is open
and the world is filling with things that crossed — and it is the only escalation in the game that costs
no art and no writing, because there is nobody to free. Full argument in `combat.md` §21.

**And it is slain, not freed.** Jeremy: *"the beasts are just slain, not knocked sense into them."*

> The survival rule is about **the possessed**. The Beast was never a person, so nothing is freed and
> nothing is lost.

No exposition is needed, because it is the only thing on this table that does not look human — the player
never stands waiting for someone to get up. It is also therefore **the only creature you can harvest**
(Tallow and Cured Hide, §7), which is what funds chapter 3's fight count.

Its extra sightings should mostly be `BlocksPath`. Fights are opt-in and flight is guaranteed, so an
optional Beast is one a cautious player never meets and the escalation quietly does not happen — but
Jeremy's note stands: *"when we place, we will see if any should be optional."* Default, not law.

### The three bosses

| Boss | Ch | Vulnerable | Resistant | Deals | Who it was |
|---|---|---|---|---|---|
| **The Lost Trapper** | 1 | Edge | Blunt | Edge | **already dead** — a corpse being worn. Severing frees nobody, and it says nothing |
| **The Blacksmith** | 2 | **Caustic** | Fire | Blunt | alive, tethered; he **begs you**, thanks you, and dies. The first person you kill |
| **The Demon / Iris** | 3 | — | — | — | the finale. See `combat.md` §20 |

**Each boss's anchor is an object of their trade, never person-shaped** — the Trapper's is **a snare,
sprung and empty**. Ratified 2026-07-31; the reasoning and the one part still open (Iris's object) are in
`combat.md` §21.

**Each boss's pair deliberately echoes a regular creature in its own chapter.** The Trapper takes the
Logger's pair, so chapter 1's first lesson is also the answer to its last fight. The Blacksmith takes the
Innkeeper's, so the optional creature you could ignore turns out to have been the rehearsal. That is free
teaching — no tutorial, no hint, just a player noticing that the axe worked here before.

**The Demon's types are deliberately unfixed.** The final fight is about which route you took, not which
tool you brought, and pinning its pair now would be inventing a constraint on a scene whose shape
`combat.md` §20 already settles. Fill it in when the encounter is authored.

### The chapter chains — each chapter teaches the answer to its own boss

**Chapter 1 — woodland.** You start with the Splitting Axe and nothing else, so Edge is the whole
vocabulary. Logger and Hunter both answer to it, and the Lost Trapper does too. **The chapter's lesson is
that the right tool is cheap and the wrong tool is expensive**, learned against creatures that resist
Blunt and Fire when you have neither.

**The Messenger is the exception to §21's cadence, and it is the only one.** It is Fire-vulnerable in a
chapter with no Fire, so §7 puts it off the critical path — and unlike every other creature that means
**all three of its sightings are optional**, not two-path-and-one-optional. The chapter's fight count is
unchanged at nine; only the split moves, to **four on the path and five optional.** It is also the reason
backtracking to chapter 1 has something in it: three fights you could not win, standing exactly where you
left them, each guarding a repair site.

**Chapter 2 — village.** A strict chain, and it is the tightest sequence in the game:

> arrive with Edge → **Stable Master** (V:Edge) → forge the **Bell Maul** → **Apothecary** (V:Blunt) →
> learn **Corrosive Draught** → **Innkeeper** (V:Caustic) → **the Blacksmith** (V:Caustic)

Each fight hands you the tool for the next one, the coats you forge midway ward exactly what the two path
creatures deal, and the boss is answered by a component the chapter itself taught. Nothing in that chain
is coincidence and none of it should be reordered casually.

**Chapter 3 — mine.** You arrive with all four types, so every creature is answerable on sight and the
chapter's difficulty is not about acquisition. Soldier, Miner and the Beast, then the finale.

### What freed people hand over

§1 fixes the rule: **freed NPCs give objects and recipes, never Craft knowledge** — that is the Dream's
job and it stays the Dream's job. The meaningful handovers are few on purpose (§21's writing note: real
scenes for these, one line or a nod for everyone else).

| Chapter | Who | Gives | Locked? |
|---|---|---|---|
| 2 | **Apothecary, 1st freed** | the **Burning Oil** recipe | **locked** — §2 |
| 2 | **Apothecary, 2nd freed** — Iris's mentor | the **Corrosive Draught** recipe | **locked** — §2 |
| 2 | **the Barricaded Merchant** | a **materials cache** — the chapter's Cured Hide and Scrap Iron in bulk | sketch |
| 1–3 | everyone else | commons, one of her scattered notes, or nothing | sketch |

**The Merchant is behind the Pry Bar, not before it.** Jeremy: *"I was thinking you need the pry bar to
get to him."* So the Pry Bar stays a chapter 2 knowledge pocket and the Merchant is what it opens — which
is the correct direction, because a tool that unlocks a person reads better than a person who hands you a
tool.

### What is locked here and what is sketch

Same distinction as the header, applied to this section:

| | |
|---|---|
| **The count — 9 + 3** | **locked.** It is the damage-type matrix, and changing it changes every placement below it. |
| **The (vulnerable, resistant) pairs** | **locked.** Distinctness is the whole argument; swap one and check the whole column. |
| **Chapter assignment and the chapter 2 chain** | **locked.** The chain is what makes each chapter teach its own boss. |
| **Trades and names** | **free.** *Stable Master* could be *Carter*; the constraint is that the trade must read as a silhouette and imply the damage type. |
| **`Deals`, and `Durability` / `Damage` numbers** | **sketch.** Tune in play. |

**Only the Beast deals Caustic, so the Limewashed Smock answers exactly one creature — and that is now
fine.** It was flagged as the roster's thinnest column; Jeremy: *"I'm actually okay with this since he is
by far the most different."* The Beast then went from three sightings to five or six, which settles it
without changing a single damage type: **the coat is not for one fight, it is for six**, and it is the
last thing you forge before the chapter that needs it most. Leave the Miner on Blunt.

### The slice that exists today is the old model

`data/entities/beast_giant_rat.tres` is an animal with both a Dream pocket and a purified harvest yield —
a combination that is now boss-only, since the nine are untethered and nothing is harvested from a body.
Jeremy, 2026-07-31: *"the rat was just a test, no real loss here."* Retarget the waystation pocket to the
Lost Trapper when the chapter 1 content pass happens.
