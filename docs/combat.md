# Combat & Progression — design

Status: the **Dream heist (section 9) is built and verified.** Everything about Waking-world
combat, traps and progression is **designed, not built** — numbers there are starting points
for tuning, not gospel. As of 2026-07-30 the design is **ratified rather than sketched**: see
**Section 18**, which is the authority on how a fight works and closes the item, trap and
equipment vocabulary that the rest of this document repeatedly defers.

Supersedes the "Sanity as universal health pool" idea from the original build plan, and the
Snap duel from the GDD. The resource is called **Resolve** everywhere, in the docs and now in the
code — there is no "Sanity" left in the project.

Sections **15–18** are newer than the rest; where they disagree with an earlier section, they win,
and the earlier text carries a note saying so. Two deliberate revisions: Section 6 has Resolve
moving in Waking-world boss fights, which the original version of that section forbade, and
**Section 18 deletes Defence as a number** in favour of a vulnerability and a resistance, which
Sections 2 and 7 were written around.

---

## 1. The principle

Two ways to end an entity, costing **different currencies**, so the choice is real rather
than a difficulty setting.

| Route | Where | Costs | Feels like |
|---|---|---|---|
| **Sever** | Pixel Realm | Resolve — renewable | the efficient path |
| **Overkill** | Waking world | **her salves — finite and uncraftable until ch3 — and turns** | stubbornness, and it hurts |

> **The overkill column was revised 2026-08-01 and the old wording is worth seeing.** It used to read
> *"traps, consumables, her supplies — finite."* Traps are now crafted once and kept forever (`items.md`
> §3), so they left the column — **but they had already stopped belonging in it.** §19's rule that a
> common must never be the reason a craft is refused made five of the six traps effectively infinite;
> remaking one costs a gathering walk, not a supply. The column had been hollow for a while and the
> permanent-trap decision only made it visible. **Invariant 1 survives intact** — the two routes still
> cost genuinely different currencies, a Dream run against her irreplaceable salves — and the finite half
> is now concentrated in the one place the story wants it.

Neither is correct. Most players will sever. The overkill route exists because being *able*
to refuse the Dream is worth more than the efficiency you give up for it.

Killing the Form 1 body ends the entity outright — spirit included.

---

## 2. The anchor is a regeneration source

The single rule everything else hangs off.

While a spirit's anchor is intact in the Pixel Realm, its Form 1 body in reality:

- **regenerates** every turn
- projects a visible **aura**, which **drains your Resolve for as long as you are fighting it**

Sever the anchor and both collapse at once. The aura snaps off, the body visibly deflates,
regeneration stops — **and the fight stops costing Resolve.**

**Severing is the only thing that does this.** No trap, item, ward or consumable removes
regeneration or the drain, in whole or in part — see §4, where a salt line that claimed to was
struck for exactly this reason. The aura is the anchor's presence in reality, so only the anchor
can answer it.

**The drain is conditional on the tether, not on the fight** (decided 2026-07-27). A boss
whose tether you have already broken costs you no Resolve to finish, however long it takes.
This is the cleanest possible statement of the rule, because it is already the state machine
the code has: **`Corrupted` drains, `Severed` does not.** No new flag, no boss-specific
special case — it falls straight out of `EntityStateManager`.

It is also not ambient. Standing in the room costs nothing; only the engagement does. See
Section 6, which is the authority on where Resolve moves.

This teaches itself. A player watches a wound close, and understands. Then watches the
shimmer die, and understands they have won something. No tutorial copy required.

> **Not implemented at all**, despite appearances. `EntityData.CorruptionAura` exists as an
> authored field and is set on the rat, but **nothing reads it** — the old Waking-world
> *ambient* drain it used to feed was removed, and nothing replaced the consumer. So the field
> is free to carry what Section 6 asks of it, but do not mistake "the field exists" for "the
> mechanic is half-wired." Missing: the visual aura, the regen, and the boss-fight Resolve drain.
> (This note used to list "the Defence field" as missing too. **Section 18 deleted it** — there is
> no Defence number to add.)

### Defence is fixed, and equipment is the answer to it

> **Superseded in part by §18, 2026-07-30. The *type* half of this section is exactly right and is
> now built on; the *number* is gone.** Defence is not a fixed scalar you overcome — each creature
> has one **vulnerability** and one **resistance**, and the right damage type does double while the
> wrong one does half. Everything below about types being access gates rather than tiers still
> holds, and holds more cleanly, because there is no longer a second number beside the type. Read
> "Defence" below as "how hard this is to hurt with the wrong thing."

**Decided 2026-07-27.** Once the aura carries the Resolve drain, it does not also need to
carry a variable damage-reduction stat. So **Defence is a fixed property of each unit.** It
does not rise, does not fall, and — unlike regeneration and the drain — **is not affected by
severing.** You overcome it by bringing the right *kind* of equipment, not by grinding a
number or by weakening the thing first.

Why this is the better shape:

- It is invariant 5 stated as a mechanic rather than a prohibition. Defence you beat with a
  **type** is an access gate; Defence you beat with a bigger number is a tier. This is the
  same Zelda-tools logic the crafting side already runs on — the tool is the key.
- It stops the anchor from being a bundle of three loosely-related buffs. The anchor now means
  two legible things: *it heals* and *it presses on your nerve*. Both visible, both stop when
  you sever.
- Severing is still worth doing without being a prerequisite. It removes regen and the drain;
  it does **not** hand you a weapon that works. That keeps the two routes genuinely different
  rather than making sever the strictly-dominant opener.

**Values stay moderate — see §7, which is the authority on how high Defence goes.** Fixed does
not mean insurmountable: the right tool is meant to be clearly better, not the only thing that
works at all.

That setting is what keeps the answer rule from becoming a trap. Every Defence value should
still have at least one answer the player can obtain, craftable or findable rather than a unique
(invariant 7) — but because values are moderate, a player who has *not* found the intended
answer is inefficient rather than stuck. So this is a **design-quality rule, not a
softlock-prevention rule**, which is a weaker and much safer thing to depend on. Compare the
catalyst rule, which really is load-bearing: an uncraftable catalyst has no fallback at all,
whereas a wrong-tool fight always has the slow, expensive option.

~~**Still to decide:** the vocabulary of equipment types, and how many there are.~~ **Decided
2026-07-30 — four damage types (Edge, Blunt, Fire, Caustic) and two equipment slots (one weapon,
one armour). See §18.** Still unbuilt.

### The loop runs one way only

Severing the anchor weakens the body. **Wounding the body does *not* weaken the spirit.**
Overkill is all-or-nothing: kill it in this engagement or you have spent everything for
nothing.

That asymmetry is what makes the regeneration bite, and it is what makes the two routes feel
genuinely different rather than like two damage types.

---

## 3. Fleeing, and why chip-and-run needs no special rule

Players may flee any fight. The creature gets a **parting strike**; that is the only cost.

No "you cannot escape" rule is needed, because the regeneration already polices the tactic:

- **Anchored** — damage decays between visits. Leave it at 40 HP, return, it is at 100.
  Chipping converts supplies into nothing. You must burst it down in one engagement.
- **Severed** — no regen, damage sticks. Chipping across several visits is now perfectly
  sensible, and is a legitimate way to finish a husk cheaply.

The system teaches this far better than a greyed-out button would. One wasted attempt and
the player understands the rule forever.

---

## 4. Preparation is the real combat

**Turn-based, one action each, strict alternation.** Simple on purpose — the interesting
decisions happen *before* the first turn. §18 states the rule that makes that literal:
*anything you did before the fight is free, anything you do during it costs your turn.*

Menu-driven item selection is inherently repetitive, and more menu options do not fix it.
The fix is to make the room the weapon.

> You are a craftsman. You do not out-fight the thing. You out-build it.

### Traps

Discover **blueprints** in the world, build traps from materials, place them in the scene,
*then* trigger the engagement. ~~Every room is a different rigging puzzle~~ — **every *creature* is
one** (revised by §18: rig sites hang off the entity placement, not the room, so no room needs
authored trap data). Preparation still cannot go stale the way a combat menu does; the variety comes
from what you are fighting rather than where.

> **This table is still a sketch of *content*, but it is no longer a sketch of the *rules*.**
> §18 ratified the grammar — four damage types, and placement free while acting costs a turn — so
> the two right-hand columns below are now real, and only the roster is provisional. Jeremy flagged
> on 2026-07-27 that the salt line in particular was never something he proposed; it stays struck.
> Treat the rows as illustrations of the *shape* of trap content, and note that they now illustrate
> a shape that exists.

| Trap | Effect | Type | Set off by |
|---|---|---|---|
| **Snare** | it loses its next turn | — | itself |
| **Pit / spikes** | heavy opening damage on engagement | Edge | itself |
| **Oil slick + ignition** | damage over several turns | Fire | **your turn** |
| **Chime** | lures it onto a chosen tile; reveals its condition | — | itself |
| **Collapsing beam** | one-shot, large, consumes the site | Blunt | itself |
| ~~**Salt line / ward circle**~~ | ~~suppresses the aura for N turns — no regeneration~~ | — | **struck, see below** |

Note what the last column is doing: it is not a property the player memorises per trap, it is
readable from the object. Oil waits for a brand; a snare closes on its own. See §18.

> **Amended by §19 (2026-07-31) — the *inputs* column is now decided, and it splits the roster in
> two.** Edge and Blunt traps are pure Craft, made from common materials. **Fire and Caustic traps are
> fused**: they need her burning oil or her corrosive salve as an ingredient, so they cannot be built
> without both knowledge tracks, and they are built at the camp bench rather than at a field station.
> The oil slick row above is therefore a fused trap, not a Craft one. ~~Traps are still **consumed
> always** — §19 leans on that harder than this section did, because after equipment stopped being
> destroyed on death, a rigged trap became the only thing a fight can take from you.~~
>
> ~~Permanent traps were requested and **rejected** in the same conversation: they would collapse
> overkill's cost to turns and salves while severing still cost a Dream run, which inverts invariant 1.
> See §19, "Equipment survives death".~~
>
> **Superseded 2026-08-01: traps are crafted once and kept forever.** The full argument, including why
> the rejection above was right when written, is in §19, "Traps are permanent". Short version: the
> abundance rule had already made five of the six traps effectively unlimited, so "consumed" was
> bookkeeping rather than cost, and the finite half of invariant 1 is now **her salves**.

#### Only severing touches the aura

**The salt line is struck** (2026-07-27), and the rule it broke is worth stating positively,
because it is load-bearing:

> **Cutting the tether is the only way to remove regeneration or the Resolve drain.** No trap,
> item, ward or consumable does either, in whole or in part.

The salt line was described here as "a poor man's sever: a regen-free window without ever
entering the Dream." That is precisely the thing that must not exist. If a cheap Waking-world
trap can buy the anchor's effects off, the Dream route stops being the way you weaken a boss and
becomes a convenience — which hollows out invariant 1, the two routes costing different
currencies, and takes most of Section 2's teaching with it. The aura is the anchor's presence in
reality; only the anchor can answer it.

This also settles what would otherwise have been an open question. Once the aura carries the
Resolve drain as well as regeneration (§6), any trap that "suppresses the aura" would have had to
either pause the nerve clock — making it mandatory in every boss fight — or suppress an aura that
still drains, which reads as a contradiction. Striking it removes the question rather than
answering it awkwardly.

Traps are Waking-world only, which differentiates the two routes further: the real world is
where you scheme, the Dream is where you are exposed.

### Fits what is already built

Blueprints are `ItemCategory.Lore`, which already exists. ~~Trap sites are another
`HotspotKind`.~~ **Revised by §18: they are not.** Sites come with the entity placement and their
count is one number in `GameConfig`, so there is no site hotspot to author and no room data to add —
which is what makes it safe to lay out rooms before combat is built.

---

## 5. Narrative presentation — states, not numbers

"No stats" and "count your oils" are both true: the numbers exist, they are simply never
shown. Instead of a health bar, an entity has **described conditions**:

> unmarked → bleeding → faltering → collapsing

The player reads prose and the aura shimmer. Never a digit.

**The bands are derived, not authored** (§18): they are fractions of the creature's durability, with
a per-entity override for the one that needs its own words. That way a creature cannot be authored
into a state that has no description.

> *"The oil takes hold. The hide splits, and this time it does not close."*

That single line reports that the tether is cut — the wound is staying open because nothing is
animating the body any more — without any UI, and without naming a mechanic. This is less work
than a stat readout and much better horror.

### Length is the constraint that matters

**Fights should run 3–6 turns.** Fifteen turns of clicking oil is tedious no matter how good
the prose is.

Overkill must be **expensive, not long.** Traps front-load the damage, everything burns in five
brutal turns, and the nerve clock in §6 is what guarantees it cannot become a grind. A setpiece,
not a chore.

---

## 6. Two pools

| Pool | Spent by | Restored by |
|---|---|---|
| **Vitality** | Waking-world combat | her salves (finite), crude substitutes (poor) |
| **Resolve** | the Dream, and boss fights **while the boss's tether is unbroken** | leaving the Dream; resting at a camp |

**Resolve is your nerve, and an empty bar means one thing everywhere: you are removed from
the confrontation.** In the Dream that is ejection from the realm. In a boss fight it is
forced flight. One meaning, two venues.

It moves in exactly those two places. **There is still no ambient drain** — no
corrupted-ground pressure, no bleed for standing near something anchored, nothing outside an
actual confrontation. Normal enemies never touch it: they have no aura and cannot be severed,
so a drain there would teach the player nothing.

> **Sharpened by §19 (2026-07-31) — inside the Dream, suppressing your spirit light is now the *only*
> drain.** There is no base drain either, so the bar moves only while you are actively hiding. This is
> not a reversal of the paragraph above but its logical end: a drain the player cannot act on teaches
> nothing, and a drain that exists only while you hold a verb down is the most actionable version
> there is. It also makes the clock appear only in boss pockets and not in knowledge pockets, with no
> flag on any region — see §19, "Resolve: suppression is the only drain".
>
> The Vitality row's "her salves (finite), crude substitutes (poor)" is reshaped by §19 into a
> four-recipe apothecary: two healing tiers you craft, and two of hers that are ingredients rather
> than consumables.

**And within a boss fight it is conditional, not automatic** (decided 2026-07-27): only an
**unbroken tether** drains. Fight the boss with its anchor intact and the aura works on your
nerve the whole time; come back after severing and the same fight costs you none. In code that
is exactly `Corrupted` drains / `Severed` does not — see Section 2.

### How fast it drains

**Size the rate so an unbroken tether empties a full bar in about six turns** (decided
2026-07-27). That is the top of the 3–6 turn window in §5, which makes the drain the thing
that *enforces* that window rather than an extra clock running beside it: fight efficiently
and the bar is a pressure you feel but survive; stall, over-rig mid-fight or chip away at
something you brought the wrong tools for, and it banishes you at roughly the moment the
fight had become a chore anyway.

Consequences worth stating, because they are easy to get wrong later:

- **Per turn, not per second.** Resolve in the Dream drains on a wall clock because the Dream
  is real-time traversal. A boss fight is turn-based, so the drain must tick on turns or the
  player is punished for thinking. Same bar, same meaning, different unit — `ResolveManager`
  will need a discrete `SpendForTurn`-style path alongside `_Process`.
- **Six turns is the budget for the whole engagement, not per phase.** If a boss ever gets
  multiple phases, the bar does not reset between them.
- The rate is defined against a **full bar**, which is what makes camps load-bearing: see §17.
  **Starting a boss fight part-drained is allowed** — you are responsible for your own resource
  management (decided 2026-07-27). No refusal and no confirmation prompt; the bar is always on
  screen, so it is an informed choice. Arriving with three turns of nerve left and losing is the
  system working.

> **Revised.** This supersedes the earlier rule that Resolve *never* moves in the Waking
> world. That version kept the bar unambiguous by confining it to one place; the "removed
> from the confrontation" reading keeps it unambiguous while letting the aura actually bite,
> which is what makes fighting an un-severed boss feel like a decision.
>
> **Amended 2026-07-27** with the tether condition above. Note what it changes about the
> sentence this note used to end on. The old wording said the drain "adds pressure, not the
> reason" to sever, because regeneration already carried the incentive (the old wording also
> credited Resilience, which is no longer state-dependent — see §2). Once
> the drain is conditional, severing *also* switches it off, so it is a third reward for
> severing rather than pressure applied evenly to both routes. That is the point — it pushes
> on the overkill route specifically, which is the one that is supposed to be expensive.
>
> It does not collapse invariant 1. The two routes still cost different things: overkill
> spends **finite supplies** against a regenerating body under a nerve timer, severing spends
> a **catalyst and Dream time** and then buys a fight with neither. Resolve appears on both
> sides, but not as a fungible currency — it refills at camps and on leaving the Dream, so in
> the boss fight it acts as a *fight-length limit*, which is invariant 6 ("overkill must be
> expensive, never long") doing its job rather than a second difficulty slider.

**At 0 Vitality:** you wake **at a camp**, having lost what you were carrying. Not a reload —
but **her named salves and other protected items survive.** The only way to lose those is to
spend them, which is the point: burning her work must always be a choice, never bad luck.

**At 0 Resolve in the Dream:** it throws you out. There is no Snap duel; Resolve empty and
being touched are the same ending.

**At 0 Resolve in a boss fight:** you are driven off to camp and **lose the rig** — ~~traps spent,~~ the
site consumed, set it up again before you try again. Fleeing voluntarily costs the same rigging; the
difference is that you still have your unspent salves, because you stopped before pouring them all in.
Same loss, very different bill.

> **Amended 2026-08-01.** Traps are permanent now (§19), so being driven off costs the *placement* and
> the turns, not the traps themselves. That is a smaller bill than this paragraph was written to
> describe — deliberately: the finite half of invariant 1 moved onto her salves, which are the thing you
> actually run out of in a boss fight.

> **Consequence — decided.** Resolve refilling on return would mean *failing a heist costs
> nothing but time*, and the two-route economy in Section 1 depends on the Dream route having
> a price. **Crossing consumes a catalyst.** You burn incense to open the way; botch the run
> and you go make more. That puts the cost in the finite-supply economy where it belongs,
> keeps Resolve a pure clock, and is exactly the GDD's *"real-world crafting items catalyze
> abilities in the Pixel Realm."* Needs M4 inventory first.
>
> **Catalysts are craftable from common materials, not rare finds.** This is what keeps the
> Dream punishing without making it undoable: rare catalysts risk a genuine soft lock where
> you cannot afford to cross and cannot progress without crossing, whereas craftable ones make
> a botched run cost a gathering trip. Annoying and recoverable, which is the target feel.
>
> This matters more than it first did, because Resolve now bounds *every* Dream visit and not
> just three heists — see Section 15. Thirty free retries would turn an unsettling place into
> a grind.
>
> **Revisit 2026-07-30 — the last paragraph is retired, the rest stands.** Extending the catalyst to
> *every* Dream visit was the overreach. **The catalyst is charged on crossing into a boss pocket
> only**, because that is where invariant 1 bites: the heist is the sever route, and the sever route
> is the thing that must stay priced. A knowledge pocket is the crafting track, not one of the two
> routes to killing a boss, so taxing it buys no balance and only taxes the observation loop that
> Section 15 is built to encourage. Re-entry to a knowledge pocket costs time and nothing else, and
> its trigger item is consumed on success only. See Section 15's re-entry subsection.
>
> **Reversed 2026-07-31 by §19 — the catalyst is no longer a price at all. It is what severing an
> anchor gives you**, and it grants abilities in the Dream. Entry costs finding a pocket's object of
> interest and nothing more. The objection this whole block was built on — *failing a heist costs
> nothing but time* — is answered rather than ignored: a material cost on retry does not prevent a
> grind, it adds a gathering trip to one. Time was always the real currency. What replaces the toll is
> a gradient rather than a gate, so nobody can be priced out, and pocket **length** becomes the
> balance parameter instead. Full reasoning, plus the rule that killing a boss must not delete its
> pocket, in §19, "Catalysts flip from price to reward".
>
> Also note the "thirty free retries" worry above was aimed at the wrong target: retries are a grind
> when the pocket is a *task*, and the ordinary Dream's pockets are **observation loops**, where
> re-entering with what you learned is the intended progression rather than a leak.

Max Vitality rises through **rare items**, never levels.

---

## 7. The craftsman has no combat stats

Deliberate. If damage came from a strength stat, "over-preparing" would mean grinding levels
and the resource choice evaporates.

**Your damage is entirely what you brought and what you rigged.**

> **§18 restates this as a pair of multipliers, 2026-07-30.** "Moderate, never absolute" is now a
> concrete number rather than a tuning instruction: the right damage type does **double**, the wrong
> one does **half**, and neither is ever zero. Everything below is the argument that produced those
> two figures, and it is worth keeping because it is what stops anyone raising them later.

**Defence values stay moderate** (decided 2026-07-27). The earlier design leaned on
absolutes — a plain weapon doing *literally zero* against a boss's Resilience — and that is
explicitly not the target any more. Fixed Defence is meant to make the right tool clearly
**better**, not to make every other approach impossible. A wrong-tool fight should feel
wasteful and slow, which the six-turn nerve clock (§6) already punishes on its own; it does
not also need a wall.

Two reasons this is the safer setting:

- **It is its own softlock protection.** If Defence is never absolute, a player who has not
  found the intended answer is inefficient rather than stuck, and the craftable-answer rule in
  §2 stops being the only thing standing between them and a dead save.
- **Absolutes make Defence read as a lock, and locks invite key-hunting.** Moderate values keep
  the interesting question — *is this fight worth what it will cost me?* — which is the question
  Section 1's two routes exist to ask.

Reserve near-immunity for the rare, deliberate case where "you need something else entirely" is
the *point* of an encounter. It should be an authored exception, never the default shape of a
boss.

### Crafting is the answer key

Defence is only the first instance of a bigger idea: **crafting is how you answer what an enemy
*does*, not just what it resists.** Regeneration, a Defence a plain weapon works badly against,
and eventually whatever behaviours enemies acquire — the intended response to each is something
you built, chose or discovered, rather than a number you grew. That is what makes crafting
load-bearing rather than a side system, and it is invariant 5 ("progression gates access") paying
off in the place the player actually feels it.

~~**Open: the equipment-type vocabulary.**~~ **Closed 2026-07-30 — see §18.** The list is four
damage types, **Edge, Blunt, Fire and Caustic**, with Caustic taught only by the Physick side of the
journal; and two equipment slots, one weapon and one armour, where the weapon carries a type and the
armour halves one. Small enough to hold in the head and to read off a description, which is what this
paragraph was asking for.

Worth recording *why* it closed when it did, since this section spent two revisions arguing it should
wait. Deferring was right for the reason given — settling a vocabulary against systems that do not
exist means re-settling it — and what changed is that combat stopped being one of the systems that
did not exist. Once the turn structure was ratified, "what does this trap do" became a question with
a unit to answer in. **The vocabulary was never blocked by the content pass; it was blocked by the
grammar,** which is also why it turned out to be the right thing to settle before room authoring
rather than during it.

The reason overkill is ruinous: **the thing keeping you alive is the same finite thing that
lets you win.** Her salves are both your healing and your ammunition.

### Legibility

The player must be able to estimate the bill *before* committing, or the choice is a trap.
Examining a body with enough Craft proficiency should report something like
*"Hide like slate. Nothing you carry will scratch it — it wants burning, not cutting."*

Note that Defence being fixed makes this easier to write, not harder: the reading is a
permanent fact about the creature rather than a number that changes underneath the player, so
it can go in the bestiary once and stay true. It also has to name the *answer*, not just the
problem — a reading that says "nothing works" without hinting at what would is the trap this
section exists to prevent.

---

## 8. Progression — three tracks, no attributes

No STR/DEX/INT. Nothing that multiplies damage; that is what kills dread. Everything gates
**access**.

| Track | Governs | Grows by | Kind |
|---|---|---|---|
| **Craft** | what you can build — gear, oils, wards, **traps** | making things | a **set** of known recipes |
| **Physick** | what you can brew, and **Caustic damage** | studying her notes — slowly, badly | a **set** of known recipes |
| **Attunement** | max Resolve, catalyst slots, what you perceive in the Dream | surviving the Dream | a **number** |

**Two of the three tracks are not numbers** (decided 2026-07-30, when recipe discovery was settled as
*found only* — trial-and-error brewing was rejected). Craft and Physick are simply *which recipes do
you know*, so they grow by discovery rather than by accumulating a score. Only **Attunement** stays
numeric. That is invariant 5 paying off harder than this section originally claimed: nothing
multiplies, and two thirds of progression cannot even be expressed as a multiplier.

**Physick starts near zero and grows poorly, on purpose.** You are a craftsman, not an
apothecary. Her finished stock outclasses your best work for most of the game, which keeps
the inheritance precious — and makes learning her craft mean something, given what actually
happened to her.

With the tracks as sets, that sentence stops being a curve to tune and becomes an **authoring
statement**: there are few of her notes and they are far apart. §18's damage-type split is the same
fact in the combat vocabulary — three of the four types are yours and one is hers, so her side of the
journal is small, late, and opens something nothing else can.

---

## 9. The Pixel Realm is a heist, not a fight  — **BUILT**

You do not fight dark royalty. You rob them.

Touch a relic in the Waking world and you are put into a small maze of Dream rooms with a
draining clock and something already looking for you. Reach the anchor, take it, and you are
ejected — with the target now `Severed`. Get caught or run out of Resolve and you are ejected
anyway, having spent the run for nothing.

**Resolve is the clock, and only the clock.** It drains while you are in there, faster when
the pursuer is close, and refills the moment you are back out. Attunement raising max Resolve
reads simply as *"I can stay in longer."* The refill fraction is per-heist
(`ResolveRestoredOnReturn`), so a specific setpiece can be made to leave a mark.

**Nothing in the Dream can kill you.** Every ending is an ejection; only the price differs.
That is what keeps the two routes denominated in the same currency — if the Dream were a
skill gate with a fail state, it would stop being comparable to spending supplies.

### Implementation

`DreamRunManager` (autoload) owns an active run. The maze reuses the ordinary room system —
Dream rooms are plain `RoomData` and the adjacency graph is derived from their Navigate
hotspots, so authoring a new heist is authoring rooms. `DreamHeistData` holds the tuning.

The pursuer BFS-steps toward the player on a timer with a wander chance. **The loops in the
maze are the mechanic**: a maze you can only run down is a corridor, and the loops are what
make evasion a decision rather than a dice roll.

The player's only readout is distance rendered as language — *"footsteps, not far"* — never a
number and never a minimap. Not knowing which direction it is coming from is doing the work.

### Spectral vision is still a look, not a place

`Tab` remains the diagnostic: you see Form 2 anchored to the beast, and you see the aura. You
cannot sever from there. That preserves the GDD's dual-perception idea while making the heist
the only real way through.

## 10. Her supplies

Strictly **finite** for the named salves — one cache, that is all there ever is. Crude
craftable substitutes exist and are visibly worse.

Spending a real one should always sting slightly. Burning her work to avoid being frightened
is characterisation, not just economy.

**Her salves are protected from death** (Section 6). Losing them to a bad fight is noise, and
noise characterises nothing; losing them because you chose to spend them is the whole point.

**Crude substitutes must be *sufficient*, not merely present.** Anchored bosses regenerate
between visits (Section 3), so overkill has to land in one sitting; her salves are finite; ~~and
a failed engagement costs you the rigging.~~ Put those together and a player who fails twice
could otherwise lose the overkill route *permanently*. So the crude versions have to be able
to close a fight — worse, slower, needing more of them and better rigging — or failure
silently deletes half of Section 1. Not a soft lock, since severing always remains, but the
second route would quietly stop existing.

> **Weakened 2026-08-01, and that is good news for this rule rather than bad.** The struck clause was
> one of three legs holding up "a player who fails twice loses the route" — and it is gone, because
> traps are permanent (§19) and a failed engagement now costs only turns. **The requirement stands
> anyway on the remaining two legs**, since her salves are still finite and bosses still regenerate.
> But the failure spiral it was guarding against is now much shallower: failing twice costs you salves
> you chose to spend, not a kit you have to rebuild.

**Her named salves stay uncraftable.** Physick approaching their quality late (Section 8 says
her stock outclasses your work *"for most of the game"*) means brewing your own near-equals
under your own names. You learn her craft; you never duplicate her hand.

> **Reversed 2026-07-31 by §19, and the reversal is the payoff rather than a loss.** Her two salves are
> **found first and learned last**: a finite authored cache in your hands from the first hour, crafted
> only late, once enough Craft is reclaimed and enough of her notes are found. Section 8's *"for most
> of the game"* is preserved exactly — it is true for most of the game, and the moment it stops being
> true is the emotional payoff of the entire crafting system. Learning her recipe reads as resurrection
> rather than a tier unlock.
>
> It also fixes a problem this section could not see: a late-game recipe normally means a late-game
> *item*, barely used and barely felt. Inverted, the item is present throughout and only the *making*
> of it is late.
>
> Two details from §19 that belong here. Her salves are **ingredients, not consumables** — burning oil
> and corrosive go inside his traps, so they are `Component` in code despite being called salves, and
> they never take a combat slot. And the hoarding problem this section's "spending one should sting"
> invites is authored around rather than ruled around: **hand the player one in the first tutorial
> fight** and make the use unmistakable, so they learn what spending her work feels like before they
> start saving it forever.

---

## 11. Complexity budget

Two pools · three proficiencies · one turn-based resolver for Waking combat · node-or-3D stealth
in the Dream · a handful of traps · observation puzzles in both realms · camps · **four damage types
and two equipment slots** (§18).

No attributes. No XP. No skill trees. No second inventory. No health bars. **No damage
multipliers** — crafting grants variety and access, Zelda-style tools rather than weapon tiers,
which is what keeps invariant 5 intact while letting the crafting side run deep.

> **§18's 2× / 0.5× is not a violation of that, and the distinction is worth stating before someone
> reads it as one.** The prohibition is on multipliers that *grow* — a number you raise until the
> fight gets easier. A vulnerability multiplier is fixed, belongs to the creature rather than to the
> player, and cannot be improved by anything the player does; it is the *type* being an access gate,
> expressed arithmetically. Nothing the player owns has a damage number that ever goes up.

Two things were considered and **rejected** for this budget, recorded so they are not
re-derived: using the Dream as a **traversal shortcut** past monsters (a third route paid in a
freely-refilling resource, which collapses Section 1 — crafted bridges are the same fantasy in
the correct currency), and **harvesting crafting supplies** in the Dream (it inflates the
finite-supply economy, and carrying physical goods out of a place that is not real makes no
sense). Also rejected: puzzles built on spatial reconfiguration, which would have been a new
system rather than new content.

Small enough to finish, deep enough to have opinions about.

> **Revised 2026-07-31 by §19, and the net is a reduction.** Added: **stations** (one `HotspotKind`),
> and **one logic puzzle per knowledge track**, played once per recipe rather than once per craft.
> Removed: the material economy this budget assumed. Gone are stackable raw reagents as inventory
> (materials are a tally that takes no slots), the carry-slot economy and its upgrades (§18's rig cap
> already made that decision, better and later), the shared cache's purpose, and the catalyst as a
> crafted entry toll.
>
> **The line to keep is the reason for the trade:** crafting depth is paid for in content forever,
> while a puzzle is paid once. That is what makes twenty-five hand-authored puzzles cheaper over the
> life of the project than a material economy with ten nouns in it. The vocabulary ceiling — **eight
> materials, two of them scarce** — is the version of this budget that applies to items.
>
> One addition worth naming against the "no damage multipliers" rule above: **fused recipes** (§19)
> require both knowledge tracks and produce Fire and Caustic. They are not stronger, they are the only
> route to those types, so they are access rather than power and the rule holds.

---

## 12. What this means for existing code

Mostly additive:

- `EntityState` already has the `Severed` middle value the model pivots on
- `EntityData.CorruptionAura` already exists as an authored field — though **nothing reads it
  yet**; it is set by `ContentSeeder` and stored in the `.tres`, and that is all
- `SaveService` persists plain dictionaries, so per-entity combat state grows without a save
  format break
- `EntityStateManager.Resolve` stays the one place that knows how a state looks
- Blueprints reuse `ItemCategory.Lore`. ~~Trap sites reuse `HotspotKind`.~~ **They do not — §18 puts
  them on the entity placement instead, so no new `HotspotKind` is needed either.**

The one thing that must widen: **`EntityPresentation.Interactable` + `Verb` is a
single-action model.** Combat needs a *set* of available actions. Contained; not worth doing
until M5 starts.

### Added by §18

Two enums and a handful of `[Export]`s, listed in full at the end of §18. The reason to add them
*before* the content pass rather than alongside it: a system written later reads fields that already
exist, but a field added later means reopening every `.tres` that was authored without it. Nothing
here is a system, and nothing here goes on `RoomData` or `HotspotData` — which is exactly why room
authoring does not have to wait for the encounter loop.

One decision that belongs to the code rather than the design: **the encounter holds a list of
bodies**, not one. A list of one behaves identically to a single enemy, and it is the only way Swarm
stays cheap.

---

## 13. Still open

**Answered:**

- ~~Does body damage persist between engagements once severed?~~ **Yes — and it does not
  persist while anchored.** That asymmetry *is* the reward for severing: without it, severing
  would only remove regen *within* a fight, and Section 3's lesson would have no contrast to
  teach from. A husk has nothing animating it, so nothing closes the wound. Cheap to build:
  `SaveService` persists plain dictionaries, so this rides alongside the per-instance entity
  state that already never resets on room re-entry — persistent wounds are the zero-respawn
  rule one level deeper. The number exists for the resolver and is never shown; Section 5's
  condition bands stay the only readout.

  **Built 2026-07-30** as `CombatManager` (autoload #11), and the build made the save question
  smaller than it looked: only the severed half can ever reach disk, because the anchored half is
  already gone by the time an engagement ends. `SaveGame.Wounds` is therefore additive with no
  version bump. One thing the design did not specify and the code had to settle — **the state is
  read when the engagement ends, not when the hit lands.** Severing mid-fight keeps the damage that
  got you there; the other reading would throw away the work as a reward for finishing it.
- ~~Can traps be recovered after a fight, or are they consumed?~~ **Consumed, always** — win,
  flee or driven off. Which is why they must be cheap and rebuildable from common materials;
  if failure destroyed precious components, players would reload rather than retry.
- ~~What the Dream fight actually *is*, turn to turn.~~ There is no Dream fight. See Section 14
  and Section 15.
- ~~Does anything other than severing remove regeneration or the Resolve drain?~~ **No. Cutting
  the tether is the only way, and the salt line that claimed otherwise is struck** — see §4.
  Answered 2026-07-27.
- ~~Can a boss fight be started on a part-empty bar?~~ **Yes. You are responsible for your own
  resource management** (2026-07-27). No refusal, no confirmation prompt, no special warning: the
  Resolve bar is on screen at all times, so walking into a boss half-drained is an informed
  choice and losing that fight is the correct outcome. This is consistent with how the Dream
  already behaves — `DreamRunManager.Begin` refuses only a bar at literally ≤1, which exists to
  stop an ejection on the first frame, not to protect the player from a bad plan.
- ~~How high should Defence go?~~ **Moderate, not absolute** — see §7. Answered 2026-07-27, and
  then made concrete by §18: 2× for the right damage type, 0.5× for the wrong one, and **there is no
  Defence number at all.**
- ~~Trap placement: free-form on a grid, or fixed authored sites per room?~~ **Neither — the sites
  come with the creature** (§18). Two of them, counted in `GameConfig`. This is the answer that
  unblocked room authoring, because it means a room needs no trap data whatsoever.
- ~~The whole item, trap and equipment-type vocabulary.~~ **Settled 2026-07-30 — see §18.** Four
  damage types split three-to-one across the couple, two equipment slots, three enemy archetypes that
  need no field, nothing that degrades. §4's trap roster is still provisional *content*, but the
  grammar it is written in is now decided.
- ~~Should auto-firing and turn-costing traps be a per-trap property?~~ **No — it is a consequence of
  *when*.** Placement is free, acting during the fight costs a turn. Some traps still need a spark,
  which is legible from the object rather than memorised (§18).
- ~~Does equipment degrade?~~ **No.** Death already destroys what you can remake, so decay would be a
  worse second copy of the same pressure — and degradation taxes *use* where death-loss taxes
  *failure*. Answered 2026-07-30, Jeremy's call.
- ~~Is there a shield?~~ **Not at launch, and the deferral is safe** — slots are an enum and an item
  declares its own, so a third re-authors nothing. The version worth building is *brace*, a verb
  costing a turn, and whether the turn economy needs it can only be judged after the loop exists.

- ~~**Whether Fire belongs to Craft or to Physick.**~~ **Answered 2026-07-31 by §19: Fire is fused, so
  the split is two and two.** A Fire trap needs her burning oil, and the oil is chemistry while the
  ignition is mechanism — so neither track owns Fire and it cannot be built without both. This was not
  chosen for symmetry; it fell out of deciding that her combat salves are trap *ingredients* rather
  than thrown consumables. The recorded worry that three-and-one matched §8's "few of her notes, far
  apart" is answered by volume instead: she has four recipes against the Craft side's many, so her
  contribution stays sparse while being load-bearing.
- ~~Can traps be recovered after a fight?~~ ~~Still **consumed, always** — the answer above stands, and
  §19 depends on it more heavily than §13 did. Once equipment stopped being destroyed on death, a
  rigged trap became the only thing a fight can take from you.~~ **Answered again, the other way,
  2026-08-01: traps are never consumed.** Craft each one once and own it for the rest of the game.
  §19, "Traps are permanent" — a question that gets re-asked in three separate sessions is usually
  the design telling you the answer is wrong.

**Still open:**

- **Whether the encounter needs a defensive verb at all.** See the shield note above. Unanswerable
  until three fights have been played, and cheap either way.
- **The eight material names**, and especially the scarce pair — §19 sets the ceiling but not the
  nouns, and a station cannot be authored until they exist.
- **What Dream abilities a catalyst grants** (§19). Deliberately left open: they will be discovered as
  the 3D pockets get built. What is settled is that they are lateral rather than compounding, and that
  invariant 11 forbids any that touch the Resolve drain or a boss's regeneration.

---

## 14. Resolved: the Dream is stealth  — **BUILT**

The fork between turn-based Dream verbs and a minigame was settled in favour of neither:
the Dream became an **evasion and theft** sequence. Being unable to fight is the strongest
lever horror has, and it makes the two realms genuinely different activities rather than the
same menu in two palettes.

An unplanned benefit: the scarier the Dream is, the more meaningful the overkill route
becomes. *"I would rather spend everything she left me than go back in there"* is a better
reason to burn supplies than arithmetic ever was.

### Built (option A — node stealth)

Reuses the existing room system. Six-room Waystation maze with two loops, a BFS pursuer, the
Resolve clock, both failure endings, and language-only proximity cues. Covered by the
automated gates in `SliceVerifier`.

### The Dream goes 3D — decided 2026-07-30

Node stealth existed to answer one question before spending months: **is being hunted in this
game frightening?** It has now been played, and it is — you feel rushed and chased with nothing
but text proximity cues and no audio at all, which makes the result stronger rather than weaker.

The cost estimate that justified deferring also turns out to have been wrong, because it assumed
3D creature art. At 480×270 with nearest filtering, low-poly reads as style rather than as cheapness.

**Settled: the Dream is first-person in a 3D world**, against a Waking world that is first-person in
a 2D one. Both realms are first-person and that word was carrying two meanings; it is worth saying
which each time.

**Still open: whether the hostile spirits are 2D billboards or 3D models.** This section used to call
that "2.5D versus full 3D", which was a misleading name — both readings always had 3D geometry, and
the only difference between them was the creatures. Named correctly it blocks creature art and
nothing else. It is answered by building both and looking at them; `docs/handoff.md`, "The Dream in
3D", is the startup order and step 5 is that comparison.

The architecture absorbs all of it: the Dream already renders through a `SubViewport` that can host a
3D subscene, and nothing in `RealmTransitionManager`, `EntityStateManager` or the save system
cares what the Dream looks like.

**The real risk is mechanical, not artistic.** Real-time movement adds a *reflex* requirement
even though ejection-on-touch keeps invariant 2 intact — and a player who cannot dodge would be
walled out of the efficient route and forced into overkill, which is exactly the difficulty
slider Section 1 exists to prevent. So the 3D Dream should be deliberate rather than twitchy:
slow, generous grab windows, readable telegraphs. Slower is also more frightening, so this is
probably not a trade at all.

**Mostly answered 2026-07-30, and by a design change rather than by tuning.** Creatures — bosses and
mindless wanderers alike — are to be warded off **using puzzles and the environment**, which makes the
skill the Dream tests *what you understood about the room* rather than how fast you can sidestep. A
knowledge test is the shape invariant 5 asks for, so the fairness argument stops resting on movement
speed. It is a downgrade from a design problem to a tuning one, not a disappearance: you still have to
execute while something is coming.

Two consequences recorded so they are not rediscovered. This **spends the Section 11 complexity
budget deliberately** — 3D physical manipulation is a new system and there is now a physics handler
because the puzzles need one. And it **overturns the earlier rejection of "the puzzle is the
evasion"**, which was a complexity argument; the complexity is being bought anyway, so the rejection
is spent rather than still standing. Note the word: a *ward* here keeps a creature at a distance
inside the Dream, and removes neither regeneration nor the Resolve drain, so invariant 11 is
untouched despite using its vocabulary.

**Fully answered later the same day, and the remaining piece is the important one.** The paragraph
above left the reflex requirement as a tuning problem. Jeremy closed it: *"if you are really good at
running and dodging you can get away from enemies or bosses… But I will create trap/puzzle
environments for people who aren't as good."* **Reflex becomes one route past a creature rather than
the route** — so failing at it points you at the environment instead of walling you out of the sever
route and into overkill. That is the whole substance of the invariant 1 objection, and it is answered
structurally rather than by making things slower.

Two routes past one obstacle, paying different currencies, is also the same shape as this document's
central choice, which is a reason to trust it. **The thing to watch is that the Resolve clock can
convert skill into currency:** if dodging is instant and the environmental route costs time, and time
costs Resolve, the better player pays less for the same outcome and the two routes collapse back into
a difficulty slider over enough visits. Keep them roughly Resolve-comparable. The opposite failure —
reliable traps making dodging pointless — is milder, but which one you would rather have is a real
question for play.

### Still open

- Audio. Deliberately last, once the loop exists — but note that Section 15 removes the pursuer
  from most of the Dream, so the ordinary Dream's tension now rests on the clock and on dread.
  Until audio exists, any hazard the player is *meant to hear coming* must be readable by sight
  alone, or it will playtest as unfair and the wrong conclusion will be drawn.
- Does the pursuer's pure-BFS chase need softening? Untested against a real player — though it
  now only runs inside boss pockets, which is a far smaller tuning problem than every room.
- The entry puzzle is a placeholder `Interact` hotspot, not a minigame. It is now the first real
  instance of Waking-world puzzle content, so it sets the grammar for everything after it.
- Traps (Section 4) are designed but unbuilt; they need M4 inventory first.

---

## 15. The ordinary Dream — why you keep going back

Section 9 gave the Dream one purpose, and one purpose that fires three times is a side trip, not
a realm. This is what makes it load-bearing enough to justify building.

### The premise

The demon that half-possessed you — the same event that opened the Dream to you — **scrambled
your knowledge of your own craft.** So you go in to reclaim your own stolen memory, and what you
recover is what lets you rebuild the bridge and go on. The gift and the reason you need it are
the same wound.

That inverts the fantasy in a useful way: you are not robbing spirits for an advantage, you are
stealing back from the thing that robbed you.

It also gives Section 8's three tracks a source, which they previously lacked:

| Track | Whose | Where it comes from |
|---|---|---|
| **Craft** | yours, stolen | recovered in the Dream — which is why it "grows by making things": you are re-remembering |
| **Physick** | hers, never yours | her notes, in the Waking world — which is why it starts near zero and grows badly |
| **Attunement** | the demon's gift | surviving the Dream |

**Recovering knowledge is mandatory. Weakening bosses stays optional.** That is the right way
round: it makes the Dream necessary for progression while leaving the two-route choice in
Section 1 genuinely a choice.

### Only knowledge crosses over

Nothing material leaves the Dream. Four things can, and all four already exist in the design or
the code:

1. **Knowledge** — blueprints (already `ItemCategory.Lore`) and recipes. You learn *how* to build
   a trap; you never pick one up. This wires the Dream straight into the trap economy without a
   single object crossing.
2. **Attunement** — already "grows by surviving the Dream."
3. **Perception** — you come back able to *see* a Waking hotspot that was always there. Nothing
   crossed; your eyes changed. Flag-gated hotspots already do this.
4. **State** — severing, generalised. A door sealed in reality whose anchor is a Dream object
   opens content rather than bypassing a monster.

### Pockets, not a world

The Dream is a set of **closed-off pockets**, each entered from its own Waking-world object. Enter
at one point to explore a knowledge area; the boss guarding its tether is in a different pocket
reached from a different entrance. They never connect.

This costs nothing to build — `DreamRunManager` already enters a run from a relic and derives
adjacency from Navigate hotspots, so a pocket is just a set of rooms. It buys three things: each
pocket can have its own rules, small self-contained mazes are far easier to author and tune than
one large realm, and it **hard-separates the knowledge trips from the hunted ones**, so severing a
boss can never be incidentally cheap just because you happened to already be inside.

#### The two kinds, specified 2026-07-30 — and the split is on creature AI, not on counterplay

|  | **Knowledge pocket** | **Boss pocket** |
|---|---|---|
| **How you get in** | find an item tied to crafting (or another trigger) in the Waking world | find the item that is that boss's **conduit** |
| **Objective** | solve the puzzle and reach the **memory fragment** | reach the **anchor** and take it |
| **Fail condition** | Resolve runs out before you get there | Resolve runs out, or the boss reaches you |
| **What is in there with you** | creatures with **no tracking — blind and mindless**, either anchored in place or running aimlessly | the boss, which **chases**, plus elements that let a less skilled player still win |

**This settles the question of whether the two-versions split survives, and it settles it on a
different axis than the one that was in doubt.** The open worry was that a knowledge pocket where
creatures can be warded off sits too close to a boss pocket. It does not, because the line was never
counterplay — **it is whether anything is hunting you.** Blind versus pursuing is a hard distinction
that holds no matter how much environmental counterplay exists on either side. Record the axis
correctly: *AI, not affordance.*

Two consequences fall straight out. **The BFS-fairness question disappears from most of the Dream** —
blind creatures do not pathfind at all, so there is no pursuit to tune outside the three boss pockets;
the wanderers are hazards, not hunters. And **entry becomes an item gate in both cases**, which is
invariant 5's shape exactly (progression gates *access*) and puts a Waking-world discovery step in
front of every boss.

#### Re-entry — decided 2026-07-30: the trigger item is consumed on success, never on failure

Jeremy: *"the item isn't consumed until you are successful, then it is. Wasted time is still a
negative to the player, I think more than enough."*

**Consume-on-success is clearly right, and for a reason this document already argues elsewhere.**
Consuming on entry would mean failing a puzzle you do not yet understand costs you *access to the
puzzle*, which is the dead-save shape invariant 12 exists to prevent — a missing answer becoming a
lock. It is also the intuitive semantics: the item is a key, and a key is spent when the door opens,
not when you fail to open it. Nothing needs explaining to the player.

**"Wasted time is enough" is also right, but the argument for it is stronger than the one given.**
Time as a *deterrent* is weak — a determined player will spend it. The real case is structural: the
ordinary Dream is already an **observation loop**. Nothing material crosses over, puzzles are about
*noticing*, and a clock bounds each trip. In that structure, knowledge retained across free retries
is not a leak in the economy — **it is the progression.** A player who learns a pocket over two trips
and then walks straight to the fragment has played correctly. The clock's job is to make a single
trip tense and force prioritisation *within* it, not to ration how much of the pocket a player may
ever see.

**Two consequences worth building around, because they follow rather than being optional.**

- **A pocket is tense exactly once.** Run one is exploration under pressure; run two is a known
  errand. That is fine and intended, but it means the tension budget is spent on the first visit, so
  **size a pocket such that a good first run can finish it.** A pocket that cannot be cleared in one
  Resolve bar by construction guarantees its fragment is collected on the boring run.
- ~~The mirror risk is lockout-on-*partial* success.~~ **Closed 2026-07-30, and it was never open —
  the content already answered it.** Jeremy: *"Crafting run → you are moving towards one item that
  will recover your building knowledge. You will be able to craft **one** new thing. Boss anchor run
  → you go in to sever the anchor. Once you do these there is no reason to go back."* **One pocket
  holds exactly one thing**, so consuming the key on the way out can never strand anything. See
  below.

#### One pocket, one thing — ratified 2026-07-30

A knowledge pocket yields **exactly one recipe**. A boss pocket yields **exactly one severed anchor**.
Nothing else is in there and there is no reason to return. This makes consume-on-success airtight,
and it buys four things beyond that:

- **It removes completion anxiety, which the Resolve clock would otherwise weaponise.** A draining
  clock plus uncertainty about whether you have found everything produces frantic exhaustive
  searching — the worst possible version of this realm. Knowing there is exactly one thing means the
  clock pressures *route-finding* instead of *sweeping*, which is the observation-puzzle behaviour
  Section 15 wants.
- **The fail state stays binary.** You got the thing or you did not. No partial credit, no
  per-fragment bookkeeping — the save needs one flag per pocket, not a set.
- **Pocket count becomes countable, which makes it schedulable.** One pocket per reclaimed recipe,
  one per boss. The production plan falls out of the content plan instead of being guessed.
- **The journal's Craft side becomes an index of the Dream.** Every Craft entry is one pocket
  cleared, which is a free diegetic record of progress and needs no separate tracking.

#### What a pocket is pinned to — corrected 2026-07-30: recipe *families*, not recipes

The first statement of this rule said pocket count equals Craft-recipe count. **That is wrong, and
the correction is the largest workload saving available.** Jeremy: *"You will start with some basic
crafting so that cuts the numbers down some. I would say upgrades to recipes can be found by
interacting with memory items in the world when you have the first recipe, and will cut down the
amount of 3D zones a lot."*

| Where a recipe comes from | Costs a pocket? |
|---|---|
| **Starting recipes** — known before the first Dream trip | no |
| **Base recipe of a family** — reclaimed from the Dream | **yes, one pocket each** |
| **Upgrades to a family** — Waking-world memory items, gated on holding the base recipe | no |
| **Apothecary side** — the wife's notes, Waking world | no |

So **one pocket per recipe *family***, and the lever is family count. A family of three costs one
pocket instead of three.

**This is better than a cost saving, and three things make it so.**

- **It sharpens the premise rather than diluting it.** What the demon *took* was the craft itself, so
  the Dream returning a base capability is exactly right — while refinement is something a craftsman
  worked out for himself, which is precisely what a Waking-world *memory* is. Base from the Dream,
  refinement from the world, is a better articulation of the premise than "all Craft knowledge is in
  the Dream."
- **It makes reclaimed knowledge retroactively activate content already in the world**, which is the
  "only knowledge crosses over" rule finally doing mechanical work. Memory items can be authored inert
  and become meaningful later, so a Waking room gains content on a revisit without being re-authored.
  Cheap, and it is the Metroidvania shape that suits a small game.
- **It needs no new machinery.** `RecipeData.RequiredBlueprintId` and flag-gated hotspots already do
  exactly this: an upgrade is a `Lore` pickup whose requirement is the base recipe.

**The gate that must not move:** a *new craftable thing* always comes from the Dream. Only refinement
is out here. Keep that line and the Craft track stays Dream-locked in the way the premise needs.

> **Guard the word "upgrade" — it is the one that reopens a settled decision.** The crafting model is
> **Zelda tools, not Dark Cloud tiers**: variety and access, never bigger numbers (invariant 5, and
> the Dark Cloud comparison was explicitly withdrawn on 2026-07-27). "Upgrade" is the standard name
> for the rejected shape. So define it as:
>
> - the recipe now makes a **different** thing — notably a **different damage type**, which with one
>   weapon slot and 2×/0.5× matching is pure access and opens creatures you could not handle; or
> - the recipe now **costs less** — an economy change, not a stat, and meaningful because materials
>   are finite;
> - **never** the recipe now makes a *better* thing. Same discipline already applied to Defence.
>
> **Two authoring risks that come with it.** Memory items must not become a **collectible sweep** —
> the Waking world would inherit the exhaustive-search problem one-thing-per-pocket just removed from
> the Dream. Keep them few, and place each where its memory actually happened; that is free
> characterisation and it prevents scatter. And **the pocket must pay off immediately and completely**
> — if the base recipe is weak until upgraded, the Dream trip becomes the chore that earns the boring
> version, which inverts the weight the whole realm depends on. You should be able to craft and use
> the thing the moment you are out.

Note the starting recipes are not only a saving: they are **why the first pocket lands.** Reclaiming
craft only means something to a player who already knows what crafting feels like, so knowing a few
recipes before the first trip is the setup for the payoff.

> **Implementation rule that falls straight out of this: consume the trigger and grant the knowledge
> in one atomic operation, never as two steps.** If they can be separated, a crash or a bad save
> between them consumes the key without granting the recipe, and the content is then permanently
> unreachable — the exact lockout consume-on-success was chosen to avoid, arriving through the back
> door. Same class of failure as the stale-payload bug in `handoff.md`: correct in the happy path,
> silently destructive in the interleaving nobody pictured.

**How this sits with the catalyst rule in Section 6, resolved along the pocket split.** Section 6
decided that crossing consumes a catalyst, because *"failing a heist costs nothing but time"* would
leave the sever route unpriced and collapse invariant 1. It then extended that to **every** Dream
visit. **The extension is what is now retired; the original is untouched.**

| | **Knowledge pocket** | **Boss pocket / heist** |
|---|---|---|
| Why you are here | reclaim craft knowledge — the progression track, not the combat economy | **sever an anchor** — this *is* the sever route |
| Catalyst on crossing | **no** | ~~**yes**, as Section 6 decided~~ — **no, see below** |
| What a failed run costs | time, and nothing else | ~~a catalyst, so a gathering trip~~ — **time, and nothing else** |
| Trigger item | consumed on success only | consumed on success only |

> **Revised 2026-07-31 by §19 — the two columns now agree on cost, and the split survives anyway.**
> The catalyst is no longer charged on crossing *anywhere*; it is what severing an anchor **gives** you.
> So a failed boss-pocket run costs time and nothing else, exactly like a knowledge pocket.
>
> **What still separates them is the row above it — and that was always the real distinction.** §15's own
> argument is that the axis is *whether anything is hunting you*, which no change to the economy touches.
> Two things replace the catalyst as the boss pocket's price: **the run itself is dangerous** (a pursuer
> that chases on sight, and Resolve burning the whole time because suppression is mandatory there — §19),
> and **pocket length is now the balance parameter**, since time is the entire cost of failure.
>
> One thing this table should gain: **a boss pocket you enter after killing its boss is empty.** No
> pursuer, no chase, walk to the fragment and collect the catalyst you would have earned. That closes the
> lockout where overkilling a boss would delete its pocket forever, and it makes the reward identical on
> both routes while the costs stay different — invariant 1 rather than an exception to it. See §19.

The reason the split is clean rather than a compromise: **invariant 1 is an argument about the two
routes to killing a boss, and a knowledge pocket is not one of them.** Pricing entry to a puzzle that
teaches you a recipe does nothing for the sever-versus-overkill balance; it just taxes the
observation loop that Section 15 exists to encourage. The heist keeps its price because the heist is
the route being priced.

### What is in there

**Chases happen only in boss pockets** — roughly three times in the game. A monster you meet
constantly becomes a traffic obstacle; three encounters make each one an event, and it lets the
rest of the Dream run on dread rather than threat, which is far more sustainable. It also shrinks
the BFS-tuning problem to a bounded setpiece.

Everywhere else there are **mindless creatures**: stationary, or wandering aimlessly, harmful on
contact, functioning as hazards to route around inside the puzzles. They are cheaper than the
pursuer — a random walk, no pathfinding — and they frighten differently, unpredictable rather
than relentless.

**Contact costs Resolve. Only a boss ejects you outright.** One currency, two magnitudes, no new
pool — and **Vitality must never appear in the Dream**, or the two-pool split in Section 6
collapses and invariant 2 goes with it. Getting touched costs you exactly the thing you need to
finish the puzzle, which is a better punishment than a health bar.

That also produces a spiral worth building on purpose: get touched → lose Resolve → the render
degrades (Section 16) → you can see less → you get touched more. Escalating, diegetic, and it
still never kills you.

> **Balance warning.** Unpredictable moving hazards in a low-resolution monochrome space with
> degrading visibility can read as *unfair* rather than tense. Keep them slow, telegraphed, and
> mostly-stationary with a few wanderers — and see the audio note in Section 14.

### Puzzles are observation, not manipulation

Dream puzzles are about **noticing** something, and the answer is spent in the Waking world. That
serves the knowledge rule above, turns Resolve into *how long do I dare stay and study this*, and
makes Dream trips research expeditions rather than errands. It needs no new systems.

Waking-world puzzles are the other half and are nearly free — the game is already a Myst-style
point-and-click, so hotspots, `HotspotKind` and flag gates are the native machinery.

> **The journal is a dependency, not a nicety.** A player who notices something in the Dream may
> spend it an hour later. With no journal that is a memory test, and puzzles get designed around
> answers small enough to hold in your head — a constraint imposed by missing UI rather than
> chosen. The journal comes *after* crafting (it is a view over discovered-knowledge state that
> crafting must read anyway), but it must land **before the first real observation puzzle.**

---

## 16. Presentation — colour against monochrome

The Waking world is a **beautifully drawn colour 2D** world. The Dream is **monochrome** — black,
white and grey — containing nothing but hostile spirits and puzzles.

Three payoffs, only one of them aesthetic:

- **Production cost.** Palette discipline is the hardest part of pixel art at scale and monochrome
  deletes it. Better, it can be enforced in the *pipeline*: a desaturate/posterise shader on the
  Dream `SubViewport` puts anything dropped in on-style automatically, so even untextured greybox
  geometry reads as intentional. One shader instead of permanent per-asset vigilance — and it
  means Dream levels can be built long before their art exists, which is the same principle
  `PlaceholderBackdrop` already runs on.
- **Signposting.** In a colourless realm one coloured object screams. The anchor is the obvious
  candidate.
- **Economy.** Section 14 wants the Dream to be somewhere you would rather not go, because *"I
  would rather spend everything she left me than go back in there"* is what prices the overkill
  route. A beautiful Waking world against an ugly Dream does that with art instead of rules.

### Greyscale and 1-bit, not one or the other

**Author every asset in greyscale and dither in the shader.** Greyscale posterises down to 1-bit
trivially; 1-bit can never be recovered up to greyscale. That pipeline order makes both looks
available everywhere, forever, tunable per room by one parameter — and keeps the choice
reversible for the life of the project. Two things it unlocks:

- **Dither as depth cue.** Density rising with distance is a fog, which solves the separation
  problem below while looking distinctive rather than compromised.
- **Dither as the Resolve readout.** Because it is a shader parameter it can be animated, so the
  realm visibly degrades as your nerve drains. A timer with no UI, exactly in the spirit of
  Section 5's *never a digit*.

### The craft problem this creates

Monochrome, 480×270 and 3D depth all fight legibility at once, and 3D normally leans on colour to
separate near from far. Everything has to be carried by **value**: depth fog, silhouette-first
shapes, rim light, and the creatures as the highest-contrast thing on screen — which is also
scarier, so that one is free.

Degrade *distance* and environment as Resolve falls while holding creatures and interactables at
full contrast. Otherwise legibility collapses exactly when the player most needs to read the
space. Thematically it is right too: as your resolve fails, your world shrinks to arm's length.

Uniform monochrome also means the pockets read as one realm for free — no shared-landmark work
needed for cohesion. But it inverts the risk into **samey and disorienting**. With no minimap and
one palette, players need to know whether they have been down this corridor before, and *random*
scenery produces noise rather than landmarks. Section 9 says the loops in the maze are the
mechanic — and a loop only reads as a loop if you recognise you have come back around. So: one
deliberately placed distinctive silhouette per **junction**, not per room.

---

## 17. Camps

There is no central workshop. Each area has one or more **safe camps**, and that is where crafting
happens. This is structural rather than cosmetic:

- **Backtracking to a hub is dead time**, and it taxes the exact playstyle the game is built on.
  Section 4 says preparation *is* the combat, so the bench has to be near the fight.
- It gives the Waking world **safe/unsafe geography**, which it currently has none of — all the
  tension lives in the Dream. A defined safe pocket makes everything outside it feel unsafe by
  contrast. Same trick as colour-against-monochrome, applied to space.
- It is the **Resolve restore point** the boss-fight drain in Section 6 needs.
- It gives **her cache** a home.
- It is the game's only source of relief, which it needs, since everything else is dread.

Near-free to build, and it was: **a camp is a room holding a `HotspotKind.Workbench` hotspot.** No
flag — see the note at the end of this section.

**Per-area, not per-room, and no fast travel.** If you can craft in the room you are about to
fight in, the logistics vanish and Section 4's "can you rig this site in time" goes with them.
Per-area camps preserve the real decision — *what do I carry from camp to the site* — without
bringing hub backtracking back.

The feeling to aim for is **commitment, not desperation**: you left camp with a plan and you find
out in the room whether the plan was right. Tune it sparse enough that you pack deliberately,
close enough that failure is not a slog.

Camps are also the single failure destination for both pools (Section 6). Being sent there after a
lost fight is convenience rather than punishment — you have to rebuild there anyway, so it skips
dead walking, and it does not weaken the packing decision, because you still walked out with a
plan the first time.

### Ratified 2026-07-30 — the whole mechanic, decided

Jeremy's calls. **Everything happens automatically on arrival. There is no rest button.**

- **Reaching a camp instantly refills both pools** — Resolve *and* Vitality. Not a rest action.
  Rationale, in his words: a rest click is annoying when the player forgets it, and declining to heal
  is never advantageous, so making it optional is bug-like behaviour rather than a decision. The
  general rule this establishes: **if the player would never decline, do not ask.**
- **Arriving also makes that camp the return point.** For the same reason — a picker whose answer is
  the newest camp ~95% of the time is a confirmation step, not a choice. Camps are fully
  interchangeable (below), so rebinding to the last one you stood in is never *worse*, only
  occasionally farther. No claim interaction, no menu, no per-camp state to track.
- **One shared cache — a Souls box.** Readable from any camp; the same contents everywhere. Per-camp
  storage is deliberately rejected for now: it would make camps non-interchangeable, break the
  "latest is fine" default, and reinvent hub backtracking as *walking back to fetch*. The real
  decision this section protects is **what do I carry from camp to the site**, which is made at the
  moment of departure. If chest-to-chest logistics ever look desirable, that is a deliberate later
  change, not something to acquire by accident.
- **Crafting only at a camp.** Not gated on anything further — being there is enough.
- ~~**`RoomData.IsSanctuary` is renamed `IsCamp`.**~~ **Built with no flag at all — `IsSanctuary` was
  *deleted*.** A room is a camp precisely when it holds a `HotspotKind.Workbench` hotspot, which
  Jeremy's own phrasing decided: the camp is "the room you put the workbench in". A flag that must
  always agree with the bench is two copies of one fact, and it makes both a benchless camp and a
  camp-less bench authorable mistakes. Deriving it makes them unrepresentable. (The old field's doc
  comment described the ambient Resolve drain that no longer exists; drain is boss-only now.)

**Free healing does not devalue her salves — it protects them.** Jeremy's objection, and it is
correct: salves matter *inside* a battle, so their scarcity is "how many can I carry into this
fight," never "how do I get back to full afterwards." Section 10 wants her finite, uncraftable salves
spent on choices that sting. Without free camp healing, players top off with them between fights —
burning the irreplaceable thing on the boring use. So this decision **strengthens** invariant 7.

**And it does not open a chip-and-run exploit, even for a camp adjacent to a dangerous room** —
Section 3 already polices that from the other side. Anchored enemies regenerate to full between
visits, so chipping converts supplies into nothing; severed ones do not regenerate and chipping them
across visits is explicitly legitimate. Camps need no rule about it.

The through-line: with healing free, a lost engagement costs you **~~your rigging and~~ your
consumables, not your HP bar** — which is Section 4's thesis stated in the economy. HP was never
meant to be the scarce resource here. *(Traps left this sentence 2026-08-01 — §19, "Traps are
permanent". The through-line survives on the half that mattered: what a lost fight takes is **her
salves**, and only the ones you chose to pour in.)*

> **Two revisions from §19 (2026-07-31). The camp is untouched as a mechanism; two of the bullets above
> change.**
>
> **"Crafting only at a camp" widens to "crafting only at the right station."** Field stations — the
> mortar, the anvil, the trapper's stump — make single-track things wherever Jeremy authors them, and
> `CraftBlock.NotAtCamp` becomes `NotAtStation`. **`HotspotKind.Workbench` remains the only thing that
> makes a room a camp**, so the derivation this section fought for survives intact and `CampManager`
> needs no change: a station is not a camp, or every herb patch would refill both pools. In exchange
> the bench gains an exclusive job — **fusion happens at the camp bench and nowhere else** — which is
> thematically better than the storage monopoly it is losing, since fusion is the two of them working
> together and the camp is their home.
>
> **The shared cache is retired.** §18 gave it its purpose (banking a spare coat against a bad night)
> and §19 removed that purpose by letting equipment survive death. The paragraph above is still right
> about *why* per-camp storage was rejected, and the "what do I carry from camp to the site" decision
> it protects has moved to §18's rig cap, where it is made in front of the creature instead of blind at
> camp. **The code and its gates stay green and unused until the content pass**, then come out in one
> clean sweep if nothing has claimed them.
>
> Note that "free healing protects her salves" gets *stronger* under §19, not weaker: her salves are no
> longer even healing items, so there is nothing left to burn on the boring use.

### A camp is a menu, not a place — ratified 2026-08-01

**This overturns the two paragraphs at the top of this section about geography and per-area spacing.**
Jeremy: *"Since inventory isn't a factor any longer, can't we just make fast travel to camp but really
have it be a menu? … The hotspot is a crutch that very weak story RPGs use to make it seem more
impactful."*

> **There is one camp per chapter, and you reach it from anywhere with a menu action.** No walking, no
> remembering where it was, no per-area spacing. Arriving still refills both pools automatically, and
> everything else in the ratified list above is untouched.

**The three camps are the emotional spine of the game, which is the real reason to have one per chapter
rather than several per area.** Each is a painted room you return to, and the sequence says something the
plot never states out loud:

| | Camp | What the room says |
|---|---|---|
| **ch1** | **his cottage** | empty. She is gone, and the bench is where she left it |
| **ch2** | **the first house you liberate** | filling. Every person you free is somewhere in it |
| **ch3** | **a camp in the mine** | one dead man, and nobody else. This part is yours alone |

**What this costs, stated honestly: Waking-world hazards lose their teeth.** If you can leave for a full
heal at any moment, no attrition mechanic outside a fight can matter. Jeremy accepted the trade
directly — *"the gain from instant camp is more than the hazards bring"* — and §22 does not try to save
attrition. It changes what a hazard *is* instead, which is the only move free healing cannot defeat.

**Three pressures survive untouched, and they are the three the game actually runs on:**

| | Pressure | Why the menu cannot reach it |
|---|---|---|
| **Inside a fight** | **her salves**, ~~rigging,~~ turns, Vitality | there is no menu out of a fight |
| **Inside a Dream pocket** | Resolve, and the pursuer | there is no menu out of a pocket |
| **Between fights** | none, **by design** | this was already true — §17 always said the Waking world's job is relief |

**And it removes a genuine tax that had nothing to do with difficulty.** Jeremy: *"remembering where
camps are is cumbersome — this fixes a lot and relieves unneeded stress on memory."* Camps were already
interchangeable and already rebound automatically, so the walk to one was never a decision. It was
bookkeeping wearing a decision's clothes. The design already refused a carry limit and already lets
equipment survive death; the walk was the last piece of the same discarded idea.

#### Showing chapter 2 filling up, without twenty-seven bodies

§21 wants the liberated house to visibly fill as you rescue people, and §21's own art budget is nine to
twelve freed sprites against twenty-seven rescues. Both hold, because occupancy is not the same as
occupants:

- **Three or four named people are physically present** — the ones with lines. That is the cast.
- **Everyone else is signs of habitation.** Bedrolls on the floor, lamps lit, a second fire, tools
  racked, voices from the next room. **Count the bedrolls and you have counted the rescues** — a tally
  with no UI, no number, and one reused sprite.
- **Production-wise it is three painted states of one room** — sparse, filling, full — not twenty-seven
  additions. The people who are present reuse the freed portraits §21 already pays for.

#### The chapter 3 camp has one occupant, and he is dead

Jeremy: *"I don't think this should have other people, since it is your task you do alone. But if we had
the spirit of the Blacksmith to talk to it could give you more context."*

> **The Blacksmith's spirit is in the mine camp.** He is the only person there, he is the man you killed,
> and he is the reason the finale has a third option.

What he does is split deliberately, and the split is what keeps §20 fair — see that section for the full
argument. In short: **he gives mechanism freely and morality only as his own hedged opinion**, he does
not know the exorcism engine exists as a buildable thing, and Daniel never agrees with him out loud.

Jeremy's framing, which belongs in the writing brief verbatim because it is what protects the player who
severed: *"the Blacksmith didn't get a dirty deal, he got the greatest mercy at the time, and if he could
have seen and understood everything he would still have asked for it. Children accept characters having
godlike powers and saving everyone on a whim, and life isn't like that."*

---

## 18. The fight, ratified — 2026-07-30

Everything here is decided. It came out of a three-turn design pass and it is the **authority on
how a Waking-world fight works**; where it disagrees with Sections 4, 5, 7 or 8, it wins, and those
sections carry a note saying so.

Two things to hold on to before the details. First, **combat is Waking-world only and so are traps**
(§4), so nothing in this section waits on the Dream going 3D or on the billboard-vs-model question
under it — which is most of why it was settled first, and is now paying out in the other direction
as the Dream takes the front of the queue. Second, and more usefully: **this unblocks room authoring**, for the
reason in the next paragraph.

### Staging — rig, then commit

**Rig sites hang off the creature, not off the room.** Jeremy: *"maybe there will always be a node
below them to select traps?"* The rigging slots come with the entity placement, so **no room needs
authored trap data at all** — which closes §13's "free-form on a grid, or fixed authored sites per
room?" by removing the question rather than picking a side.

That is the single most valuable thing this pass produced, because it is what was standing between
the design and the editor. Rooms can now be laid out, decorated and connected without knowing
anything about combat.

What it costs, stated honestly: §4's pitch drops from *every room is a different rigging puzzle* to
*every creature is one*. For a short game that is the right trade, and it is forward-compatible —
a room that deserves an extra authored site can gain one later as an additive field, with nothing
re-authored.

**Two slots**, held as a count in `GameConfig` so it is a number to turn rather than authored
content. One slot makes every fight a single pick with nothing interacting; two gives combination
without combinatorics, and §4's oil-slick-plus-ignition needs two to exist at all.

**Fights are opt-in.** You rig, then click the creature and confirm; the battle UI overlays the room
rather than replacing it. Two consequences worth keeping:

- **The rigging phase has no timer.** You can leave, walk back to camp, return. That preserves §17's
  camp-to-site packing decision instead of competing with it, and it keeps hazards as the only thing
  that hurts you unprompted.
- **The confirm step is where §7's legibility promise gets paid.** §7 insists the player must be able
  to estimate the bill *before* committing or the choice is a trap. The confirmation moment is
  literally that, so the creature's reading belongs there — and it has to name **both** halves now:
  what the thing is weak to, and what it deals. The second half is what makes the armour choice
  below a decision rather than a coin flip.

### One currency inside a fight: the turn

Turns alternate, one action each, as §4 already said. The rule that governs what an action *is*:

> **Anything you did before the fight is free. Anything you do during the fight costs your turn.**

That was ratified in preference to a per-trap auto-versus-manual property, and it is one sentence
rather than a table the player has to memorise. But it does not make every trap fire by itself, and
the distinction matters:

| | Placing it | Setting it off |
|---|---|---|
| Snare, pit, spikes | free, before the fight | itself, when the thing moves |
| Oil, pitch, powder | free, before the fight | **your turn** |

Both are placed for free. Some traps simply need a spark, and that is legible from what the object
*is* — oil does not light itself, a snare closes on its own — so the player is reading the fiction
rather than recalling a flag. It is one authored bool (`TriggersItself`), and its value is never
surprising.

Consequences that fall out without needing rules of their own:

- **Re-rigging mid-fight needs no rule.** It is an action, so it costs a turn. At 3–6 turns that is
  usually a bad trade and occasionally the right desperate move, which is a decision the player
  makes rather than a rule they memorise. Prefer that shape wherever there is a choice.
- **Fleeing costs a turn and is guaranteed** — no roll, no "you cannot escape". §3's parting strike
  is the cost. Guaranteed flight is the softlock protection for the whole overkill route, which is
  what lets Defence be sharp without being dangerous.
- **Keep a weak baseline attack**, even though guaranteed flight makes it optional, or a player out
  of items has a fight with no verbs. It has an obvious home: the baseline attack is what the
  existing one-click *"finish the Severed body"* in `InteractionService` becomes.

### Defence stops being a number

**Revises §2 and §7.** Both were written around Defence as a fixed scalar you overcome with the
right kind of equipment. The type half survives; **the number does not.** Instead:

> **Every creature has one vulnerability and one resistance.** The right damage type does double;
> the wrong one does half.

Three reasons this is better than the scalar it replaces:

- **It is universal, not an archetype switch.** If only *some* creatures had a meaningful weakness,
  "the armoured one" would mean *the fight where the mechanic is switched on*, and players would
  correctly learn to ignore damage types everywhere else. Giving every creature both fields costs
  two fields and makes *"what does this want?"* a question you ask every time.
- **It replaces length with matching.** More durability makes a fight *longer*, which runs straight
  into §5's "3–6 turns, expensive never long". A type match changes **which** resource you spend
  instead of **how many turns** you spend, so difficulty rises without the length rule bending.
- **It deletes a stat.** There is no Defence field to author, tune or explain — the multiplier is
  the whole mechanic, and §2's "the anchor means two legible things" gets cleaner still.

So keep **durability roughly flat** across creatures and let *harder* mean *needs the right answer*.
Never zero, either way: 2× and 0.5× over a short fight is already a fourfold spread in materials
burned, and §7's rule that absolutes read as locks and invite key-hunting is untouched by this
change.

### The four damage types, split across the couple

| Type | From | Made of |
|---|---|---|
| **Edge** | Craft | blades, spikes, snares |
| **Blunt** | Craft | weights, falls, hammers |
| **Fire** | Craft | oil, pitch, brands |
| **Caustic** | **Physick** | her chemistry |

Three physical plus one alchemical, and **Caustic is taught only by the Physick side of the
journal.** That gives her notes a combat expression instead of leaving them purely medicinal, and it
means finding her work makes you *able to hurt something new* — the same beat as reclaiming Craft in
§15, in a smaller key.

The 3-to-1 split is asymmetric on purpose and the asymmetry is the point: §8 says Physick starts
near zero and grows badly, so there is *less of her* in the vocabulary, and what there is arrives
late and matters. If Fire ever wants to move to her side that is a one-line change; the 3-to-1
reading is what the code is being built against.

### Three enemy archetypes — and no enum for them

| | What it does | What answers it |
|---|---|---|
| **Brute** | hits hard and slowly, telegraphed | armour, and out-damaging it |
| **Warden** | high durability, narrow vulnerability | the *matching* type; ruinous if you brought the wrong one |
| ~~**Swarm**~~ | ~~several weak bodies~~ | **cut 2026-08-01 — see below** |

**Neither of these needs a field.** Brute and Warden are the existing numbers arranged differently — so
archetype is a description of *how to author*, not something to store. Same principle as camp-ness being
derived from the workbench (§17): do not store what falls out. If behaviour ever diverges enough to need
one, an `Archetype` enum is additive.

A third, **Afflicter** — barely hurts you in the fight but leaves something on you that keeps
costing Vitality afterwards — was raised and **cut.** It was the only one that needed a
mechanic which does not exist (a persistent condition), and the others earn their difference
for free.

#### Swarm is cut too — 2026-08-01

Jeremy: *"as of now and probably always, battles are 1 v 1."* That is a decision about the whole game,
not about one creature, and it removes Swarm's entire content: several weak bodies in one encounter is
the only thing Swarm ever was.

**It is cut for exactly the reason Afflicter was**, which is the useful part of recording it: an
archetype is only free if it is the existing machinery arranged differently. Brute and Warden are.
Afflicter needed a persistent condition. Swarm needs multi-body turn order, target selection, and a trap
that resolves against more than one creature — three mechanics that do not exist and would each need
their own gates. Two archetypes that cost nothing beat three where the third is a subsystem.

**The encounter's body *list* stays.** It costs nothing, a list of one behaves exactly like a single
enemy, and it is expensive to retrofit if 1v1 is ever revisited. But it is now **an unexercised code path
with no content behind it**, which is precisely the `--verify-slice` blind spot the project has a memory
about: a gate over a list that is always length one can never fail. Document it as insurance, do not
gate it, and do not let a future session read the list as evidence that multi-body fights were designed.

### Equipment — one weapon, one armour

Two slots, and scope is the reason for exactly two.

- **The weapon carries a damage type.** That is its whole mechanical identity; there are no weapon
  tiers, per §11's no-damage-multipliers rule.
- **Armour turns aside one incoming type, and halves rather than negates it.** Same argument as
  creature resistance — an immunity reads as a lock and sends the player hunting a key instead of
  weighing a trade.
- **One worn item, not a set.** One coat at a time is a real choice, because you cannot be protected
  against everything. Four armour slots would be four times the swap ritual before every fight and
  no additional decision.

**A shield is deferred, not rejected.** It is safe to defer precisely because slots are an enum and
an item declares which slot it fills, so adding a third re-authors nothing. And the version worth
building is a **verb, not a number**: *brace* — spend your turn to take much less this turn — which
uses the turn currency instead of adding a second passive stat. Whether the turn economy has a hole
that wants filling is a question only playing three fights can answer, so it waits for the loop.

**Nothing degrades.** Jeremy's call, and his reasoning is the strongest available: losing items on
death already forces recrafting, *"making this trade more impactful than one and done"* — so
durability decay would be a second, worse copy of pressure the game already has. The sharper version
of the same point: **degradation taxes use, and death-loss taxes failure.** Only one of those is a
thing you want to discourage. It also needs a repair economy and UI to be legible at all.

**Equipment is destroyed on death**, therefore. That follows the rule already in the code without
adding an exception — `InventoryCraftingManager.SurvivesDeath` keeps what you cannot remake and
destroys what you can, and equipment is craftable. It is also exactly the recraft pressure the
paragraph above is asking for. Three notes on it:

- **It is not a softlock.** The baseline attack always exists and materials are harvestable, so a
  death that takes your weapon is a setback and a gathering trip, never a dead save.
- **It gives the shared cache its first real purpose.** §17 established one Souls box and then had
  little for the player to put in it; banking a spare coat against a bad night is a genuine reason
  to use it, and a genuinely interesting one, because storing it means not wearing it.
- **Death is entirely avoidable**, because flight is guaranteed. Making the avoidable failure
  expensive is fair in a way that making an unavoidable one expensive would not be.

> **Reversed 2026-07-31 by §19: equipment survives death.** Jeremy's call — *"I think this reduces
> scope and is better for the player"* — so nothing degrades **and** nothing is lost. Equipment becomes
> a Zelda tool in full: crafted once from a reclaimed memory plus materials, owned for good.
>
> **What that costs, stated here rather than discovered later: death is now cheap.** It takes ~~the traps
> you had rigged and~~ the walk back from camp, and nothing else. That is defensible on this section's own
> logic — flight is guaranteed, so death is avoidable, and a cheap price on an avoidable failure is a
> different bargain from a cheap price on an unavoidable one. But it is thinner than what this section
> designed, and it should be re-checked in play rather than assumed.
>
> Two knock-ons. The **cache loses the purpose the second bullet above gave it** (see §17's revision
> note), and ~~**traps become the only thing a fight can take from you**, which is what keeps §1's
> overkill column honest — the route paid for in finite supplies is still paid for in finite supplies.
> Permanent traps were requested in the same conversation and rejected for exactly that reason.~~
> **a fight can now take nothing from you at all** — traps went permanent 2026-08-01 (§19, "Traps are
> permanent"), so §1's overkill column is paid in **her salves** instead. That is the honest version:
> traps had been abundant-by-rule for a while, and salves are the resource that actually runs out.

### The condition ladder is derived

§5's *unmarked → bleeding → faltering → collapsing* is computed as **fractions of durability**, with
a per-entity override for the creature that needs its own words. The number exists for the resolver
and is never shown; the bands stay the only readout. Deriving it rather than authoring it per
creature means a creature cannot be authored into a state where it has no description, which is the
kind of hole a content pass finds at the worst moment.

### What must exist before content is authored

The whole point of settling this early. Nothing below is a system — it is the **shape** authored
content will be poured into, and a field added after the pass means reopening every `.tres`.

| | |
|---|---|
| `DamageType` | `None, Edge, Blunt, Fire, Caustic` — `None` first, so a reagent does not default to a weapon type |
| `EquipSlot` | `None, Weapon, Armour` — `Shield` additive later |
| `KnowledgeTrack` | `Craft, Physick` — from the journal's two sides, so the panel can draw two halves |
| `ItemCategory` | gains **`Trap`** and **`Equipment`** |
| `EntityData` | durability, vulnerable-to, resistant-to, the type it deals, its damage |
| `ItemData` | damage type, potency, slot, wards-against, triggers-itself |
| `GameConfig` | the rig slot count (2) |

`RoomData` and `HotspotData` gain **nothing**, which is the whole reason room authoring is free to
start.

> **§19 adds a second shapes table (2026-07-31), and it does touch `HotspotData`** — stations are a new
> `HotspotKind`. Room *layout* is still safe, but a room that wants a station needs that member to exist
> first, so the two tables should be built together before the content pass. See §19, "What must exist in
> code before any station is authored".

---

## 19. Stations, and crafting as knowledge — ratified 2026-07-31

**This section supersedes the material economy assumed by §4, §6, §10, §11 and §17, and reverses two
things §18 ratified.** Where it disagrees with an earlier section, it wins; every place that happens
is marked in the earlier section rather than left to be discovered.

Jeremy's reason for opening it, and it is the one that should be read first: *"I realized that advanced
crafting could kill my project with scope creep and honestly not be as interesting."* That instinct is
correct, and the reason is sharper than the amount of work involved:

> **Crafting depth is paid for in content forever. Puzzle-crafting is paid once.** A material economy
> needs every new recipe balanced against every existing one, every drop rate retuned, every new
> area's nodes priced. A puzzle is authored once and is then finished permanently. This section is not
> cutting a feature — it is swapping an ongoing cost for a fixed one.

### The loop

```
[FIND A RECIPE]  →  [SOLVE IT ONCE, AT A STATION]  →  [GATHER]  →  [CRAFT, INSTANTLY, FOREVER]
```

Four verbs, and the third is the only one that repeats.

### The puzzle *is* the recipe — solved once, never again

**A logic puzzle has one answer, so it can only be played rarely.** Play-until-you-succeed is the
right interaction and per-craft is the wrong frequency: the fortieth mashing of the same herbs is the
first thing in this game that would feel like padding.

> **The puzzle is solved once per recipe, not once per craft. Solving it *is* learning the recipe.**

The first time you work out a Clearmind Salve you are at the mortar solving it, and that act writes
the recipe into `DiscoveredBlueprints`. Every craft after that is instant. Four things follow, and all
four are why this is the shape rather than a compromise:

- **The authoring budget is finite and small.** Twenty-five hand-made puzzles played once each is
  Myst-sized. The same twenty-five played three hundred times is the failure mode.
- **It deletes the quality scalar, which protects the no-ladder rule.** A mini-game that produced a
  *better* salve on a good run would have reintroduced a power ladder through the back door —
  precisely what §11 and §18 spend their length preventing. A binary outcome cannot.
- **It is headless-safe for free.** There is no score to pass across the seam. `Craft(recipeId)` gates
  on `DiscoveredBlueprints` exactly as it does today, so `--verify-slice` never touches a puzzle and
  no game rule ends up in the one layer the gates cannot reach.
- **Learning becomes an event with a place attached.** Today picking up an `ItemCategory.Lore` item
  silently teaches a recipe. Now the found page or the Dream fragment gives you the *problem*, and you
  walk to the forge to understand it. **The puzzle happens at the station, not on pickup** — finding
  the page and understanding it are different acts, and the walk between them is the game.

**One mini-game per knowledge track, never per recipe.** `KnowledgeTrack { Craft, Physick }` already
exists: Physick gets one interaction, Craft gets one, and a recipe varies the *parameters* — more
steps, tighter logic — never the interaction itself.

**The Dream gets no mini-games.** Its verb is observation, and a timing or logic interaction in there
would collide with both the Resolve economy and §15's "nothing material crosses over".

### Stations, and what a camp is for now

A **station** is a new `HotspotKind`: the mortar, the anvil, the trapper's stump, the herb bank.
Authored anywhere, in any number.

**`HotspotKind.Workbench` remains the only thing that makes a room a camp.** §17's derivation is
untouched and `CampManager` needs no change — a station is not a camp, or every herb patch would
refill both pools and the body economy would be gone in one move. `CraftBlock.NotAtCamp` widens to
**`NotAtStation`**, and `RecipeData` gains a `RequiresStation` field so **each recipe declares where
it is made**. That converts a hard-coded rule into content Jeremy authors, which is the direction
this project keeps going in — camp-ness from the bench, `SurvivesDeath` from the category, and now
venue from the recipe.

**Camps keep resting, respawn, and one exclusive job: fusion happens at the bench and nowhere else**
(below). Field stations make single-track things. That is a mechanical monopoly the camp had lost when
the cache was retired, and this one is thematically earned — their home, their shared work.

**Craftsman's tools are always available and are never inventory.** A tool you can leave behind is a
trip back to camp for nothing, which is §17's "if the player would never decline, do not ask" wearing
travel boots.

**The world itself can be a station.** A collapsed bridge is a station whose output is a *flag* rather
than an item: same hotspot, same recipe check, one extra field for what it sets. **World repair costs
key items and a reclaimed memory, never materials** — a bridge is a one-time authored event, so a
unique found object makes it a puzzle where a timber cost would make it a budget line. It also lands
squarely on invariant 5: a Dream memory becomes a road you can walk down, which is the first time the
Dream's payoff is visible out here.

*Rejected, and Jeremy's reason is the right one:* finding broken things in the world to **learn** from.
It contradicts §15's gate that a new craftable thing always comes from the Dream and only refinement is
found out here.

### The material vocabulary — eight nouns, and only two of them matter

The thing that creeps is not the quantity beside an item, it is the **vocabulary**: ten herbs crossed
with six ores crossed with forty recipes, where every addition has to be balanced against everything
already there. Quantity is a number; vocabulary is a multiplication. So the ceiling goes on the part
with the balance cost:

> **Cap the scarce list, not the total. Eight materials in the whole game, six common and two scarce
> — one scarce per track. A common must never be the reason a craft is refused.**

That last clause has teeth: **a common that can block you is a scarce material in disguise**, and that
is exactly where grind comes from. If the player is ever short of timber, timber was misclassified.

| | Count | Contents |
|---|---|---|
| **Physick** | 3 | a herb, a binder, and one scarce |
| **Craft** | 5 | timber, metal, stone, leather, and one scarce |

**Commons may be pool-separated by track for flavour; the two scarce materials must be shared across
recipes.** That is not tidiness — it is where the only real decision in the material economy lives. If
timber only made traps and metal only made armour there would be nothing to weigh; the scarce ones
being contested is what makes *spend it on the trap or the coat* a question.

**Start below the ceiling — two or three — but treat the ceiling as hard.** "Start small and keep
adding" is precisely how a vocabulary creeps, one reasonable material at a time. A ninth material
wanting to exist is a signal that a *recipe* wants redesigning.

**The line that makes a small list feel large:** *knowledge is the gate, materials are the pacing.*
You cannot make the coat because you do not know how; once you know how, materials decide *when*.
Eight nouns carry twenty-five recipes without the game feeling thin, because the variety was never in
the nouns.

### Materials are a tally, not an inventory

They occupy **no slots**, are never sorted, dropped or arbitrated, and are not what the satchel is for.
Five or eight counters in a strip — arrows and rupees, not a bag. In code that is a static
**`CountsAgainstSlots(ItemCategory)`**, the exact sibling of `SurvivesDeath`, keyed on the category so
a new material cannot forget to declare itself.

`ItemCategory.Reagent` is redefined and renamed to **`Component`**: a named station input rather than a
generic raw material. `RecipeIngredient` survives, and quantities survive with it — the earlier claim
that a recipe input could always be a singular unique object does not survive contact with a
regenerating herb bank, and was wrong.

**Key items are one category**, covering story objects, gate keys and world-repair components. They
survive death, never take a slot, and are the low-stress discovery layer: searching a drawer and
finding *the tanner's awl* is point-and-click gameplay, where finding `3x bitterroot` is arithmetic.
Author as many as flavour wants; none of them creates an economy.

### The chapter resource arc, and the two refills

**Scarce materials are gated by story milestone, never by a clock or a step counter.** A real-time
regeneration would need a clock the project has deliberately refused twice (`VitalityManager` and
`CombatManager` both carry "no `_Process`, and there must never be one") and would reward idling.

> **Each chapter has its own resource arc.** You arrive flush, spend down through the chapter, and
> fight its boss on whatever your exploration earned you. **Chapters 1 and 2 both refill when the
> chapter 2 boss falls. Chapter 3 is terminal.** Two refills in the whole game, and no game+.

Thoroughness in chapter 1 is paid out in chapter 1's boss fight rather than banked forever, and a
chapter never has to be balanced against any other chapter. That is the whole reason to do it this
way.

> **Where the refills come from — added 2026-08-01.** They hang on **the chapter 1 and chapter 2 boss
> pockets**, which are already spoken for and cost nothing extra. Beat the boss, reclaim the memory, fix
> the bridge or the cart — **and the world behind you restocks.** Framed diegetically as a cleansing that
> runs back through everything you already cleared, which is the same fiction as *"zone cleanses, sun
> comes out"* at chapter scale. All eleven pockets in [`items.md`](items.md) §8 have jobs, so there was
> never a spare one to hang this on; making it a consequence of the boss is better than inventing a
> twelfth pocket to be the vending machine.
>
> **Chapter 3 gets no refill, and that is now covered by the Beast rather than by a third milestone.**
> Roughly half of chapter 3's Beast encounters are optional, so a player who runs short has material
> standing in a side passage they chose to walk past. Jeremy: *"if we clearly make half optional then you
> have the ability to but not needed."* A player-controlled valve beats a scheduled refill, because the
> design never has to guess how much they needed.

**With no carry limit, the milestone schedule is the only throttle on the entire consumable economy**
— it is a balance-critical number rather than a flavour detail, and it is the lever that gives Jeremy
the exact control he asked for.

**The backtrack is an option, not a lap.** Restocking chapter 1 after chapter 2 means walking back
through cleared ground, which is a chore — but only a player who burned through their supply ever needs
to, so it is a recovery path rather than required content. It also has its own answer already built:
*"zone cleanses, sun comes out"* is in the playable loop today, so a cleansed chapter 1 is visibly a
different place. **Author something into a cleansed zone that was not there before**, even if it is
only light and one line of text.

**Save-scumming to re-farm a depleted node is not defended against.** Single-player, their game, and
any measure against it costs more than the exploit.

#### Why depleting a chapter is not a dead end

This is the biggest risk in the schedule and it has a structural answer rather than a tuned one:

> **The sever route costs only time. It can never be priced out.**

Deplete the chapter, burn every salve, fail the overkill, flee (guaranteed), walk into the Dream, take
the anchor — and come back to a boss that no longer regenerates and no longer drains Resolve. Nothing
on that path requires a material. §10's "crude substitutes must be *sufficient*" is the same guarantee
one layer down, and it still holds.

**One correction to the tempting reading of this**, because it matters to how `SettleEngagement` is
understood: the damage from the failed overkill is **not** banked. The boss was *anchored* during that
fight, so its wounds closed. What carries forward is not progress on the body but the fact that the
second fight is against something that cannot heal or drain you. The failed attempt costs supplies and
teaches you the creature; the sever is what changes the arithmetic.

**Extended from §13: no fight may require a consumable to win.** The baseline attack always exists, so
a player who spends everything before the boss faces a harder fight, never an impossible one.

### No carry limit — and why the rig cap is the better copy of that choice

**Deleted 2026-07-31, and it was redundant rather than merely expensive.** An earlier draft of this
section had a tight satchel with slot upgrades and the retired cache as overflow. §18 already ratified
`GameConfig.RigSlotsPerFight`, so **the loadout choice already existed at the fight** — a carry limit
was a second copy of the same decision, made one layer earlier and made worse:

> A carry limit forces the choice **blind**, at camp, before you know what you are facing. The rig cap
> forces it **informed**, in front of the creature, with §7's readout naming what it fears and what it
> deals. Two limits enforcing one decision, and the redundant copy is the one that removes information
> from the player.

So the satchel is unlimited, and the caps live at the fight: **two traps and two salves.** Note how
that multiplies with §5's 3–6 turn length rather than overlapping it — using a salve costs a turn, so
carrying two means up to two of your turns are spent not killing. The cap and the clock price each
other.

**Slot upgrades go with it.** Permanent Vitality and Resolve maxima remain the ratified thoroughness
rewards, and two sinks do not need a currency.

**The shared cache is retired, not deleted.** §17 built it and §18 gave it its purpose — banking a
spare coat against a bad night — and equipment surviving death removes that purpose. The code and its
gates stay green and unused until the content pass; if nothing has claimed it by then, it and its gates
come out in one clean pass. Ripping out working gated code to make a design point buys an afternoon of
red for no gameplay.

### The four salves, and her cache inverted

**Four recipes, total.** Two healing, two of hers, and that is the whole apothecary side.

| | Track | Inputs | Role |
|---|---|---|---|
| **Lesser healing salve** | Physick | commons only | the floor — always makeable |
| **Greater healing salve** | Physick | commons + scarce | the good one, chapter-budgeted |
| **Burning oil salve** | **fused** | commons + scarce | **an ingredient**, not a consumable |
| **Corrosive salve** | **fused** | commons + scarce | **an ingredient**, not a consumable |

**Two healing tiers work and three would not.** The lesser one would normally be dead content the
moment the greater exists — with a two-salve cap you always bring two of the better — except the
chapter budget makes the scarce material genuinely finite, so the lesser salve is what you make when it
is spent. That is the floor, the same shape as the baseline attack. A third tier adds a rung that is
dominated by one of its neighbours in every situation the game can produce.

#### Her salves are found first and learned last — the inversion, and it is the best thing here

**Jeremy's call, and it repairs a problem nobody had named.** The fused salves are the ones **she left
you**: a finite authored cache, unrepeatable, in your hands from the first hour. You craft the weak
ones for most of the game, and late — with enough Craft reclaimed and enough of her notes found — you
learn to make hers.

> A late-game recipe normally means a late-game *item*: barely used, barely felt. Inverted, **the item
> is present for the whole game and only the making of it is late.** Learning her recipe reads as
> resurrection rather than a tier unlock.

It also kills the ladder objection outright — the fused salve was never an upgrade you grow into, it is
a thing you were rationing and finally earn the right to reproduce. And it is the authored cache §10
describes and M4 still lists as missing content, extended into an arc.

**This reverses §10's "her named salves stay uncraftable."** The sentence it was protecting —
*you learn her craft, you never duplicate her hand* — is not deleted so much as given a scene: for most
of the game it is exactly true, and the moment it stops being true is the payoff.

**The Elixir problem, and it is authored around rather than ruled around.** Players hoard finite
best-items and finish games having never spent one. The reluctance is intended here (§10: *burning her
work must always be a choice*), so do not fix it with a mechanic — fix it with placement. **Hand the
player one burning oil salve in the first tutorial fight and make the use unmistakable**, so they learn
what spending her work feels like. Better still, have them *craft the trap with it* rather than handing
over a finished one: her ingredient, his mechanism, a typed creature, all taught in one beat. After
that, hoarding is their choice and it is the right one to have offered.

### Fusion — and it was already in the vocabulary

Jeremy: *"You need to fuse your crafting and her herblore for the most powerful resource in the game.
It is a symbol for their love."*

**The symbol costs nothing to build, because §18 already implies it.** Traps are Craft objects —
vessels, mechanisms, things you build. Caustic is hers. So:

> **A trap carrying her chemistry cannot be made by either track alone.** Her oil or her acid inside
> his mechanism. `KnowledgeTrack` needs no third member; a recipe simply needs to be able to require
> **both**, and fusion happens at the bench.

| | |
|---|---|
| **Edge / Blunt traps** | pure Craft — commons only |
| **Fire / Caustic traps** | **fused** — need burning oil or corrosive salve |

**This closes §13's last open question: Fire is fused, not purely his.** The oil is chemistry and the
ignition is mechanism, so Fire lands two-and-two across the couple rather than three-and-one. Half the
damage coverage in the game runs through her, on four recipes — load-bearing rather than decorative,
while every trap is still his object.

**A fused item must never be simply stronger.** It is the only route to a damage type some creature is
vulnerable to, which is **access, not power**, and keeps §11's no-multipliers rule intact. Keep the
fused list at the four recipes above: a symbol used six times is a mechanic.

**Bookkeeping that is easy to get wrong:** burning oil and corrosive are *called* salves and are
mechanically **`Component`**, not `ItemCategory.Salve` — they restore no Vitality and are crafting
inputs. Both static predicates key off the category, and both land correctly: Components survive death,
which is what her irreplaceable ones need.

**Which cap does an offensive salve count against? Neither — the question is void.** Her combat salves
are never carried into a fight at all; they are consumed at the bench making traps. **Salve slots are
two healing items. Trap slots are two traps.** No overlap and nothing to arbitrate.

Note the ramp this produces on its own: early on the scarce Physick material has exactly one sink
(greater healing), and gains two more the moment her recipes come back. The material becomes contested
precisely when her work returns.

### Equipment survives death — reversing §18

**§18 ratified that equipment is destroyed on death**, argued as the recraft pressure that replaces
durability. That is reversed 2026-07-31, on Jeremy's call: *"I think this reduces scope and is better
for the player."*

So **nothing degrades and nothing is lost.** Equipment is a Zelda tool — crafted once from a reclaimed
memory plus materials, owned for good. It is also what retires the cache's stated purpose (above).

**State the consequence plainly rather than discovering it later: death is now free.** ~~It costs the
traps you had rigged and~~ It costs the walk back from camp and nothing else — salves are only "lost" by
being spent, which is consumption rather than a penalty. That is defensible, because §18's flight
guarantee makes death entirely avoidable and making an avoidable failure cheap is a different bargain
from making an unavoidable one cheap. It is thinner than what §18 designed, and it is worth re-checking
in play rather than assuming.

> **Amended 2026-08-01: "cheap" became "free" when traps went permanent** (below). The last item on the
> bill was the rigged traps, and it is gone. Everything the paragraph argues still holds — it is simply
> arguing it about a smaller number now, and the "worth re-checking in play" line is worth more than it
> was. **If it needs bite, spend-for-the-fight is the lever**, not death penalties: see the fallback at
> the end of the trap reversal below.

### Traps are permanent — reversing §13, and reversing this section's own answer

~~**Traps remain consumed, always** — §13's answer stands and this section leans on it. They are now the
only thing a fight can take from you, which is what keeps §1's overkill column honest: the route paid
for in finite supplies is still paid for in finite supplies. Permanent traps were considered and
**rejected**: they would have collapsed overkill's cost to turns and salves while severing still cost a
Dream run, inverting invariant 1. The fantasy behind the request — *I own the fire tool forever* — is
served by equipment, which is permanent and grants damage types already.~~

> **REVERSED 2026-08-01. Traps are crafted once and kept forever** (`items.md` §3). This is the passage
> that rejected the idea, kept visible because the rejection was *correct when it was written* and the
> reason it stopped being correct is the useful part.
>
> **What the rejection assumed: that traps were a finite supply.** They are not, and had not been for a
> while. §19 requires that a common never be the reason a craft is refused, and five of the six traps are
> made entirely from commons — so remaking one costs a gathering walk, not a supply. The sixth, the
> Deadfall, was the only genuinely scarce one, and its Cairnstone cost turned out to be a **softlock**
> rather than a tension (`placement.md` §8). **The overkill column had already been hollowed out by the
> abundance rule; making traps permanent only stopped the document from claiming otherwise.**
>
> **Invariant 1 is not inverted, because the finite half moved rather than vanished.** Overkill now costs
> **her salves** — uncraftable until chapter 3, rationed all game, and the one thing in the economy that
> genuinely runs out. Jeremy: *"it makes salves the biggest finite resource and it should be. There are
> ways to win without using salves, so to me that is why it works well."* A cost you can choose to avoid
> is a better cost than one you pay automatically.
>
> **Three reasons beyond the softlock, and the second is the one that decided it:**
>
> - **The trap mechanics lagged the rest of the design.** §19 deleted carry limits, §18 moved the loadout
>   decision to the rig screen in front of the creature, and camps became menus. Counting how many
>   Deadfalls you own was the last surviving inventory-bookkeeping mechanic in a game that had removed
>   every other one. Jeremy: *"we have shifted the style of the game a lot from the beginning and the trap
>   mechanics lagged behind."*
> - **"Should I save this trap for something worse?" is a blind trade**, and this design has deleted blind
>   trades everywhere else it found them. It is the same argument that made §18's rig cap better than a
>   carry limit — *informed, in front of the creature, instead of blind at camp.* Permanent traps finish
>   that job: you own all six and the only question left is which two answer the thing you are looking at.
> - **You are gridlocked to two per fight regardless.** The rig cap already supplies the scarcity that
>   consumption was supposed to provide, and it supplies it in the better place.
>
> **The honest cost, and it is real: a failed fight now takes nothing but time.** Flight is guaranteed
> (§18), camps refill both pools free (§17), equipment survives death (§19), and traps now do too. What
> keeps that from becoming trial-and-error is invariant 7 — **a creature's vulnerability is readable off
> the creature**, so a wrong rig is a misread rather than a lottery ticket, and the design never wanted
> the player brute-forcing combinations. If it needs more bite in play, the cheap fix is that **a trap
> that fires is spent for the remainder of that fight** and returns afterwards, which keeps the Deadfall's
> *one-shot, large* honest without reintroducing an inventory.

### Catalysts flip from price to reward

**§6 made a catalyst the price of crossing.** Reversed 2026-07-31: **a catalyst is what severing an
anchor gives you**, and it grants abilities in the Dream. Entry to a pocket costs finding its object of
interest and nothing more.

That restores the GDD line the system was named for — *real-world crafting items catalyse abilities in
the Pixel Realm* — which §6 had narrowed into an entry toll. **An entry fee is a tax; an ability is a
loadout decision at the same price.** It also produces a rhyme worth having: rig two traps for a Waking
fight, spend a catalyst on a Dream boss.

**What §6 gave as its reason has to be answered rather than ignored.** It objected that without a
catalyst, *failing a heist costs nothing but time*, and that thirty free retries turn an unsettling
place into a grind. Jeremy's rebuttal is accepted and is the stronger argument: a material cost on
retry does not prevent the grind, it **adds a gathering trip to** the grind. Time was always the real
currency and the catalyst only taxed it. What replaces the toll:

> **A gradient, not a gate.** The first anchor is severed with no abilities at all, and each one taken
> makes the next pocket easier. Fail a run and you lose time; skip severing and you do every remaining
> pocket the hard way.

That is a soft gate, so nobody can be locked out — which suits invariant 12 better than the toll did —
and the incentive to sever becomes positive rather than punitive. **Keep the abilities lateral**, not
compounding: new perception, new ward, a new way through, never *stronger*, or the last pocket is
trivial and the first is brutal. Invariant 11 still binds — no ability that slows the Resolve drain or
touches a boss's regeneration.

**Because time is now the entire cost of failure, pocket length becomes a balance parameter rather than
a pacing preference.** Failing at minute two of a three-minute pocket is a shrug; failing at minute
eleven of twelve is the worst feeling the game can produce. Ratified ramp: **early pockets 2–5 minutes,
late ones up to 15 — and the long ones must be the ones where failure is least likely.**

#### Killing a boss must not delete its pocket

The lockout Jeremy spotted: overkill the boss, its pocket vanishes, its catalyst is gone forever —
missable content, and it punishes the route that was already the expensive one.

> **Find the object of interest after the boss is dead and you get the catalyst free, with no trial.
> The pocket still opens; it is simply empty.** No pursuer, no chase — walk to the fragment and take
> it.

This makes the **reward identical on both routes and the cost different**, which is invariant 1 rather
than an exception to it: severing pays a Dream run and buys an easier fight, overkill pays a much harder
fight and collects afterwards. Severing does not become pointless, because what it buys is the *fight*.

Three things it gets for free. The lore justifies the mechanic — the trial is gone because the mind
holding it up is gone. It costs one flag check on whether to spawn the boss, reusing a pocket already
built. And it is a **falling-action room by construction**: a dead thing's dream with nothing left in
it but what you came for.

### Resolve: suppression is the only drain

**Ratified 2026-07-31, and it deletes a feature rather than adding one.** An intermediate design gave
regions a no-drain flag so a pocket could have a calm tail. Jeremy: *"Suppressing spirit light is the
only drain, so when you get past enemies you can stop using it and solve the puzzles at your pace."*

> **There is no base drain in the Dream. Resolve moves only while you are suppressing your spirit
> light.** No regional flag, no bool to author, no chance of putting a safe room in the wrong place.

The clock stops being a timer and becomes **resource management with a verb attached** — you choose,
continuously, between being seen and being hidden, and only one of those costs.

**And it makes the clock appear exactly where the design already wanted it, with no authoring at all:**

| | |
|---|---|
| **Knowledge pockets** | blind creatures drawn to light. Show your light and they drift toward you; hide and the bar burns. You may stand and think for as long as you are willing to be seen. **Paced observation, no clock.** |
| **Boss pockets** | the boss is *always* drawn to the light, chases on sight, and needs line of sight broken for ten seconds. Showing your light means being hunted permanently, so suppression is effectively mandatory. **The bar becomes a hard clock.** |

That is §15's two-versions split arriving as a *consequence* of one rule rather than as a flag on every
region. It also leaves the two ratified readings intact: Resolve capacity still means *how long you can
stay hidden*, and §5's "never a digit" still holds, because **the world is the readout** — suppressed
means the near field goes dark, and that teaches faster than any warning. Sound and UI nags are
deliberately **not** built; revisit only if a played pocket shows a player getting caught out. Jeremy:
*"I find those warnings super annoying."*

Tuning note: keep the suppression drain slow enough that cautious over-hiding is a waste rather than a
death. The *visible* cost — being half-blind — is what teaches the player to come back up.

### Three verbs, one meter — and eight upgrades that make it a fuel tank

**Ratified 2026-08-01, and it changes what Resolve *is*.** The section above is still correct that
suppression is the only *passive* drain; what it lacked was anything else to spend on. Jeremy: *"instead
of ladders, which make no sense in the spirit realm — after boss 1 you gain the ability to float, costing
resolve. It would help pathfinding around enemies be much more meaningful."*

| Verb | Gained | Cost | What it is for |
|---|---|---|---|
| **Suppress** | from the start | drains while held | hide. The near field goes dark |
| **Float** | after **boss 1** | drains while held | slow rise and slight lateral drift. Vertical traversal without ladders |
| **Flare** | after **boss 2** | drains while held | push the light *out*. Hidden geometry, the anchor and the tether lines become visible — and everything in the pocket can see you |

**Flare is the exact mirror of the first verb and that is why it is the right third one.** It is a puzzle
tool with a price, never an escape button: it reveals and it attracts, and it arrives in chapter 3 where
the pockets are hunted, so the price is real. Jeremy is open to more verbs later; three is the set that
exists.

> **Each of the eight knowledge pockets raises maximum Resolve.**

**This is what makes the verbs worth having, and it needed no new rule.** A permanent Resolve maximum was
already established as the mechanical form of Attunement, and *"only knowledge, perception, Attunement
and state cross over"* already permits it. You get better at being in the Dream by being in the Dream.

**Resolve stops being a second health bar and becomes a fuel tank**, which is a far better reason to want
a bigger one than "breathing room" was. It is also why the upgrades can be frequent without inflating
anything: eight increments buy traversal and stealth, not survivability.

**Maximum Vitality never increases, and this is a rule rather than an omission.** It is not Attunement,
so it must not cross a boundary that explicitly passes nothing material — and §18 spent a page deleting
Defence as a number on the grounds that difficulty comes from the type match rather than from bigger
numbers. A growing health bar is the same scalar creep seen from the player's side. **Waking-world
progression is recipes, tools and coats.** Jeremy: *"not increasing Vit makes the game much easier to
balance too."*

**Tuning note, and it is the one thing to know before balancing:** the boss aura drain and the three
verbs draw on the same meter, so every upgrade lengthens boss fights *and* deepens pockets at the same
time. Those are coupled, not independent. An earlier draft here proposed making the boss drain
proportional to maximum so that fights stayed six turns; that is **withdrawn**. Jeremy: *"the 6-turn rule
was just an arbitrary number — increasing Resolve should give you the option for longer fights. I don't
think there is an issue until it is off balanced."* So **per-boss drain is an authored value, not a
formula.** The invariant that must hold is that a boss fight ends decisively, not that it ends on a
particular turn.

### Two principles worth quoting, because they settle future arguments

**"More memorable than challenging."** Jeremy, 2026-07-31, about pocket design. Where a decision could
go either way, this is the tie-breaker.

**Falling action after the challenge, never before it.** *"The worst games I've played you go through a
challenging maze and then at the end something even worse is there. Instead challenge the player and
have a falling action activity that feels productive and they can revel in their previous victory."*
Two things it decides immediately: **the memory fragment goes after the hard part, not behind it** —
chase or hazard early, calm traverse to the fragment after, so taking it is the exhale. And keep the
tail **short**, a minute rather than five, since a long exhale turns back into pressure.

Boss counterplay follows the same rule: *"We will make solutions for the bosses to help so it is not
really difficult but is memorable"* — which is §15's environmental route past a creature, already
ratified, pointed at difficulty rather than at fairness.

### What must exist in code before any station is authored

The §18 discipline applied again: shapes first, so a field added later does not reopen every `.tres`.

| | |
|---|---|
| `HotspotKind` | gains **`Station`**. `Workbench` keeps its camp meaning and gains fusion. |
| `RecipeData` | `RequiresStation`, `RequiresBothTracks`, and an optional `SetsFlag` for world repair |
| `CraftBlock` | `NotAtCamp` → **`NotAtStation`** |
| `ItemCategory` | `Reagent` → **`Component`**; key items unified into one member |
| `InventoryCraftingManager` | **`CountsAgainstSlots(ItemCategory)`**, static, sibling of `SurvivesDeath`; `SurvivesDeath` gains `Equipment` |
| `GameConfig` | a salve cap beside `RigSlotsPerFight`, and the material milestone schedule |

**And one thing to name before any station exists, because everything downstream is balanced against
it: the eight materials.** ~~Two or three at first, but named~~ — **done 2026-07-31, and the whole
roster went with it: [`items.md`](items.md).** Every material, salve, trap, weapon, coat and tool,
named and costed, with the per-chapter availability table and the scarce budget. Two of the eight
turned out to already exist as authored `.tres` items, which is the strongest evidence available that
eight is the right size.

---

## 20. The last boss — two ways to lose her, one to save her

**Jeremy's reveal, 2026-07-31.** In the final Dream pocket, **the anchor is Iris.** She asks you to
destroy her, and severing her weakens the boss exactly as any sever does.

| Route | The fight | Iris | Ending |
|---|---|---|---|
| **Sever her** | weakened, no aura drain | **dies** | the loss — you did what she asked |
| **Overkill, without the engine** | full strength, anchor intact, Resolve draining throughout | **dies** | the loss — you refused, and it cost her anyway |
| **The exorcism engine** | weakened — the engine *releases* the anchor instead of destroying it | **lives** | **the only good ending** |

> **Revised twice on 2026-07-31, and this is the settled version. Read the second note; the first is
> kept only so the reasoning is traceable.**
>
> **First revision** (superseded): Jeremy moved the good ending onto the overkill route as well —
> *"option 2 killing the boss gives you the good ending as well, because it would be a hard battle"* —
> making the engine an accessibility ramp on a hard fight rather than a distinct outcome.
>
> **Second revision (current):** he reversed it the same day, after the untethering rules were settled in
> §21. *"Untethering your wife or killing her without the exorcism will kill her. The trap/exorcism is
> the only good ending path and I like it better this way."*
>
> **The reversal is a consequence, not a change of mind.** §21 established that a tethered person dies
> when the tether goes, whichever end it is cut from. Iris is tethered. So the only survivable outcome is
> the one that does not cut the tether at all — and there is exactly one of those. The ending stopped
> being a menu and became an implication of a rule the game teaches for three chapters.

**What this buys, and it is the largest structural gain in the document: the entire crafting system now
converges on one object.** With two good paths the engine was optional flavour. With one, every station,
every reclaimed memory and every restored screen is pointed at building the thing that saves your wife.
§6's three major repairs stop being a progression device and become a rising sequence — **taught on a
bridge, practised on a cart, and the third one is your marriage.**

**Overkill becomes the unprepared route, not the heroic one**, and that is correct rather than a
demotion. Invariant 12 says there are no dead saves: a player who reaches the finale with nothing built
can still finish the game. They just finish it badly. The three routes are better read as *prepared*,
*unprepared and merciful*, and *unprepared and stubborn* — and the difference between the last two is
only how the player feels about what they did, which is the right place for a difference to live.

### The tether has two ends — which is what the engine actually cuts

**This is the detail the earlier draft got wrong, and it matters to the fiction as much as the rules.**
If the engine merely "severed the tether", it would destroy the anchor — and the anchor is Iris, so the
engine would kill her exactly as the Dream route does.

> **Severing in the Dream destroys the anchor. The engine releases it.** Same tether, cut from the other
> end.

So the engine weakens the boss *and* she lives, which is why it is the **only** good outcome, and
invariant 11 is satisfied more literally than before rather than less: cutting the tether is still the
only way to remove regeneration, and it turns out there are two ends to cut.

**And this is exactly why the other two routes kill her.** Severing in the Dream destroys the anchor.
Overkill leaves the tether intact and kills the thing on the far end of it, which takes her with it.
Only the engine takes the tether off her without breaking either end. One rule, three outcomes, no
special-casing at the finale.

### Graveflower is the engine's material — 8 of the 12 in the world

**Revised 2026-07-31. This replaces an earlier passage that treated the Graveflower hoard purely as a
hint, and it fixes an arithmetic bug the earlier roster shipped with.**

[`items.md`](items.md) §2 originally spent Graveflower on three *consumable* salves against a total
world budget of twelve units (2/4/6). Twelve units cannot supply a consumable you burn every fight. The
material was mis-assigned, and the fix is the one the ending was asking for anyway:

> **Graveflower goes into the exorcism engine and nothing else. The engine costs 8. The world holds 12.**

Three things fall out, and none of them had to be designed:

- **Fixing the world is literally how you save her.** Graveflower comes only from minor restorations
  (§6), so the falling-action activity turns out to be the good ending's currency. Every optional screen
  you repair is a payment.
- **The hoard is still the hint, and now a better one.** You accumulate an unspendable scarce material
  for two chapters, from restoring the things she used to tend. Alongside **her notes turning up where
  the demon discarded them** and **a cache you can hold but not read**, it says *her work can still do
  something* — without ever saying it.
- **There is slack, deliberately: 8 of 12.** You may miss a third of the repairs and still save her.

**The slack is not generosity, it is the difference between a choice and a trap.** Requiring all twelve
would mean one skipped repair in chapter 1 silently locking the good ending eight hours later, with no
signal at any point. Recoverability survives too: **chapter 3 alone yields 6**, so even a player who
ignored restoration entirely for two chapters can reach 8 by working the mine and taking one trip back.
Expensive, never impossible — the same bargain guaranteed flight makes.

**One line of UI does all the fairness work and it is not a warning.** Once you hold the engine's plan,
its recipe lists *8 Graveflower* exactly as every other recipe lists its inputs. The game never nags and
never hides, which is Jeremy's standard stated as a mechanic: *"it gives some responsibility to the
player — I find those warnings super annoying."*

### The exorcism engine does not violate invariant 11 — it satisfies it

This needs stating carefully, because the instinct is that it cannot be legal. §4 **struck the salt
line** to enforce invariant 11, and the rule reads:

> *Cutting the tether is the only way to remove regeneration or the Resolve drain. No trap, item, ward
> or consumable does either, in whole or in part.*

**It says cutting the tether is the only way. It does not say the Dream is the only way to cut the
tether.** So an engine that severs from the Waking side is not a trap buying the anchor's effects off —
**it is a severing, by a third route.** The invariant is untouched.

Two conditions keep it that way, and both are cheap:

- **It is a unique authored object, never `ItemCategory.Trap`.** One recipe, one encounter,
  unrepeatable. The moment it is a trap category it teaches the player that traps can do this, and the
  salt line is back through the front door.
- **It cuts the tether; it does not "suppress the aura."** Same effect, different claim, and §4 spent a
  page on precisely that difference.

### The choice-architecture problem, and why it went away

An earlier draft worried at length that if exorcism were mechanically equal to severing but morally
cheaper, nobody would ever sever — *"not three choices, one choice and two mistakes"* — and tried to
keep the three bills genuinely different by loading cost onto the engine route.

**That worry was solved by the roster, not by tuning.** The engine is strictly better *and that is fine*,
because **it is not available to most players at the moment they arrive.** It costs three chapters of
optional work — 8 Graveflower from restorations you were never required to do, plus both journal tracks.
A player who has it is a player who earned it over the whole game; a player who does not is choosing
between the only two options they actually hold. Nobody is weighing three equal buttons, which is what
made the original framing wrong.

So the honest description is not *three routes* at all. It is **one route you prepare for, and two you
fall back on** — and which of the fallbacks a player takes is a question about them, not about a
tradeoff the design has to balance.

### How the player learns the engine exists — settled 2026-08-01

Three steps, and each one is a separate act by the player. The chain matters more than any of its links,
because it is what stops a good ending being handed over in a conversation.

| | Step | Gives you |
|---|---|---|
| **1** | **the Blacksmith's spirit**, in the chapter 3 camp (§17) | *the principle* — that there might be another way |
| **2** | **an optional Dream pocket**, guarded by the **breach-thing** (§21) | *the blueprint* — the recipe itself |
| **3** | **mended blights** (§22) | *the material* — 8 Graveflower of the world's 12 |

**The ghost gives the principle, never the recipe, and the distinction is load-bearing.** He felt a
tether from the inside and he is a blacksmith, so he knows a tether is a **made thing**, and a made thing
can be unmade by a better-made one. He does not know it takes Graveflower, or eight of it, or that it is
built at a bench. Daniel supplies the craft. The Blacksmith supplies the insight.

**He also does not know the engine exists as a buildable object, which is what makes his comfort safe.**
He is arguing about the only two roads he ever saw: stay what he was, or end. So *"you did right by me"*
is not an endorsement of severing Iris — it is a dead man saying that of the two options he knew about,
the player picked the right one. **The same lines mean different things depending on what that player has
found.** Someone who has built the engine hears outdated advice from a man who died before there was a
third way, which is sad; someone who has not hears genuine comfort. It can never function as a hint, so
the no-warnings rule above survives contact with the only NPC positioned to break it.

Two smaller guards, both cheap and both writing rather than systems:

- **He hopes; he is not certain.** *"I think you did right by me."* A ghost who is sure is an oracle. A
  ghost who is hoping is a person — and he is not neutral, because he needs it to have been right.
- **Daniel never agrees out loud.** Silence or deflection, not a speech. That is the internal
  counterweight Jeremy asked for: *"we will need to counter this so the player doesn't feel robbed of
  making a poor choice."*

**The guard on the blueprint is the breach-thing, and it is chosen rather than convenient.** The pocket
needs a gate, the roster is locked at twelve by the damage-type matrix, and the breach-thing — the last
and largest Beast variant, at the portal — was already going to be there. It is also the only right
answer thematically: **the one gate on the good ending is the one fight that costs nothing.** Every other
door in chapter 3 is a person. This one never was.

Jeremy's constraint on timing is satisfied by placement alone — *"I don't think they should be able to
craft it until before the fight"* — because the pocket is deep in chapter 3, near the portal.

### "You happened to have it" becomes "you went back for it" — revised 2026-08-01

**Menu camps (§17) made backtracking nearly free, and that changes the shape of this section rather than
breaking it.** A player told about the engine in chapter 3 can now go back and mend what they skipped, at
almost no cost in time. The earlier framing — that the engine is unavailable to most players at the
moment they arrive, and that this is what keeps the three routes from being three equal buttons — is
still true on a first arrival. What is no longer true is that it stays unavailable.

> **The good ending is no longer "you happened to have it." It is "you went back for it."**

**That is the better version and it is deliberately not a downgrade.** Daniel walking back over every
place he already passed, mending the things he did not have to mend, to save her — that is the same
person the whole game has been about, and it is a stronger last act than a player discovering they
happened to be carrying enough.

**It also removes the one thing this structure could genuinely get wrong.** Jeremy, 2026-08-01: *"I don't
want to gate the player on the first time through, because it punishes them if they want to; and if they
were faithful to the game they should be rewarded and not have to backtrack — they should be able to
finish at the climax."* Both halves of that are now true at once:

| The player who… | gets |
|---|---|
| **mended as they went** | the engine already affordable. **No backtrack, they finish at the climax** — the reward for having been thorough is that nothing interrupts the ending |
| **skipped the optional work** | a real second chance, cheap in time, expensive in the only currency that matters — going back over the whole game with the reason now in front of them |

Neither is punished, and the two are told apart by what they already did rather than by a check at the
door. That is the fairest version of a locked ending this design has produced, and it exists because
camps became a menu — which is not a connection anyone planned.

**The fusion pays off here and nowhere else does it matter as much.** The engine needs **her physick to
purify and his craft to contain**, so it cannot be built from one track. Jeremy: *"ironic that both
trades will be its downfall."* The demon stole his knowledge and discarded hers as worthless
([`items.md`](items.md) §9) — and the two together are what undoes it. That is mechanically true, not
only thematically true.

### Two guarantees already in the design make this safe

Neither was designed for this and both hold, which is usually a sign the structure is right:

- **Flight is guaranteed (§18), so a player who reaches the finale without the engine can walk away and
  go build it.** The good ending is never irrecoverably lost — only not yet earned. That keeps
  invariant 12 intact at the one place a three-ending structure would most easily break it.
- **Chapters 1 and 2 refill and stay reachable** (§19), so going back to do the optional work is
  supplied rather than a scavenge. Jeremy, 2026-07-31, on the 8-of-12 slack: *"you can backtrack to
  reclaim them so it is still open."*

**Both guarantees are load-bearing now in a way they were not when there were two good endings.** With
one good path, "the door stays open until you land the final blow" is the only thing standing between
this design and a gotcha. Neither guarantee may be weakened without reopening the ending.

**A third joined them on 2026-08-01: camps are a menu (§17), so the return trip costs no walking.** That
is what converts "go back and do the optional work" from a punishment into an offer, and it is why the
revision above could be accepted rather than argued around. Add it to the list of things that may not be
weakened without reopening this section.

### Closed 2026-07-31

**Whether refusing to sever her leaves her alive-but-corrupted or takes her with the boss** — previously
left to the writing pass. §21's untethering rule closed it: **she dies.** Overkill leaves the tether
intact and kills what is on the other end of it. There is no longer a version of this scene where
refusing is enough on its own.

---

## 21. The corrupted — who survives, and why — ratified 2026-07-31

**This section exists because it was the largest hole in the story.** Jeremy: *"This might be the
biggest plot hole in my story/game — what are the factors that determine if a possessed person is saved
or not."* The answer turned out to be already built, in a rule adopted for pacing reasons and never read
as fiction.

> **Tethered dies. Untethered survives.**

Minor demons *wear* people. The hold is loose, and beating the host shakes it off — Jeremy's words,
*"the hold on them just needs them to get beat up."* The demon itself drove an anchor into exactly
three: the Lost Trapper, the Blacksmith, and Iris.

**That is why there are only three heists, and it is now also why there are only three deaths.** The
decision that non-bosses have no Dream pocket was made because ten heists would be a slog. It turns out
to encode the moral logic of the entire game, which is worth recording as a caution as much as a
success: the constraint arrived first and the meaning arrived second, and it would have been easy to
resolve the pacing problem some other way and never find this.

**It is legible without a line of exposition.** A player can see which kind they are facing, because one
kind has a pocket and the other does not. Nobody has to be told, and nobody will hunt for a fourth
anchor that does not exist — which was Jeremy's stated reason for wanting the rule uniform in the first
place: *"it is something that should be consistent and will stop players from second guessing
themselves."*

### Three appearances, three fates — the art is the warning

Jeremy's instinct, unprompted: *"Should possessed people just look possessed kind of, but then the Lost
Trapper looks like a zombie since he has been dead?"* Yes — and it generalises into the whole visual
grammar.

| | Looks like | Outcome |
|---|---|---|
| **The nine** | still recognisably a person, but the trade is eating them: the tool fused to the hand, the body overgrown toward the work | beaten → **freed, alive** |
| **The Lost Trapper** | a corpse being worn. Weeks dead | nobody in there. Nothing to save |
| **The Blacksmith** | alive, and further gone than anything yet seen | freeing him **kills him** |

**This is §7's legibility promise applied to art instead of to a readout.** The player can read the
outcome before committing, and it costs no UI, no dialogue and no warning — which is the requirement
Jeremy has restated more than any other: *"I find those warnings super annoying."*

It also does a job no timer could. **The Blacksmith reads as what the nine become if nobody reaches them
in time**, which is an argument for hurrying that never touches the clock — and §19 spent a page
establishing that this game does not get a clock.

### The escalation is four steps, not three

The three bosses teach three lessons, but the load-bearing step is the one that is not a boss at all.

| | | Teaches |
|---|---|---|
| **The Lost Trapper** | you cut a snare, and nothing answers | the **verb** |
| **The twenty-seven** | beaten, freed, they get up and walk away | the **rule** |
| **The Blacksmith** | begs, is freed, thanks you, and **dies** | the **cost** — by breaking the rule, and Daniel learns it here with the player |
| **Iris** | begs in the same words | the **choice**, and you built the third answer |

**Step two is why step three lands.** By the time the Blacksmith dies the player has freed roughly a
dozen people and watched every one survive. He is not escalating from one prior data point; he is
violating a dozen. A design that skipped straight from the Trapper to the Blacksmith would have taught
nothing, because two events do not establish a rule to break.

### What the Blacksmith knows, and what Daniel does not — settled 2026-07-31

**The knowledge is split between the two characters, and that split is the scene.**

| | |
|---|---|
| **The Blacksmith** | **suspects** it will kill him, and **does not care.** He is in agony and would rather end as himself than continue as this. He never says so. |
| **Daniel** | **has no idea**, until he surfaces into the Waking world and finds the body. |

**Corrected from the first draft, which gave Daniel the knowledge one boss too early.** That version had
him understand the cost going in — and it quietly broke the escalation table above, which claims the
Blacksmith *teaches the cost*. If Daniel already knows, the Blacksmith teaches Daniel nothing and the
lesson lands only on the player. Jeremy's version puts the knowledge where the structure needs it: **the
Blacksmith gets agency, Daniel gets the reveal, and Iris gets the informed choice.**

Note what is and is not committed here, because "we don't need to pick one" can turn into mush and this
does not. Three things are decided — **he suspects, he does not care, he does not say it.** The only
thing left open is whether his suspicion is *correct*, and nobody in that room could know that. It is an
ambiguity inside a character rather than inside the design, which is the writable kind.

**He has to settle his affairs, or the beat inverts.** In hindsight the player must be able to tell he
knew; otherwise Daniel reads as having carelessly killed an innocent man, which makes him stupid instead
of complicit. The fix is characterisation, not exposition — **a man who expects to live does not hand
over his business.** *"There's a key under the anvil."* *"Tell her I—"* First read it is a hurt man
rambling; second read it is a will.

**And the last thing he says is gratitude.** Jeremy, 2026-07-31: a sigh, and *"Thank you…"* as the spirit
goes.

**That line is doing structural work, not just tone.** Jeremy: *"This isn't on Daniel, it is on the
demons."* The player has just killed someone by doing exactly what the game taught them to do, and if
the scene reads as punishment for playing correctly they will distrust every choice after it — which is
fatal three hours before §20 asks them to make the biggest one. The thanks is what keeps the death a
**tragedy rather than a trap.** Grief, not guilt. Keep it if the scene is ever rewritten; it is the
cheapest possible guard on the ending's fairness.

### An anchor is the trade-object, never person-shaped — ratified 2026-07-31

Jeremy asked what the Trapper's anchor should actually look like, having not pictured one: *"just having
a voice when you interact with it and the choice to cut the anchor."*

> **The Trapper's anchor is a snare, sprung and empty.**

Four reasons stacked, and the fourth is the one that matters:

- **It is his trade**, which is the same rule the nine already run on (§10) — the trade *is* the design.
- **Sprung and empty is the whole character in one silhouette:** the thing that caught him, with nothing
  in it any more.
- **It is the cheapest model in the game.** A loop of wire and a bent sapling.
- **You cut a snare.** The verb this game spends three chapters teaching is *cut*, and the first thing
  you ever cut is a rope. The Blacksmith's anchor is then visibly something you cannot just cut, and the
  escalation lives in the object rather than in a difficulty number.

**The rule it generalises to is now binding on all three bosses:**

> **An anchor is an object of the person's trade. It is never person-shaped, and it never has a face.**

The voice comes *out of a thing*, which is more unsettling than a thing with a face, and it saves three
character models in a realm that cannot afford them.

**One question this pushes downstream, and it is a writing call rather than a systems one.** If the
grammar holds, **Iris's anchor is her work** — her kit, her notes — not her body. That is tighter
thematically, because §9 of `items.md` already establishes that her recipes returning *is* her returning,
so destroying her work destroying her closes the loop the item economy opened. It trades gut-punch for
horror. **Left open deliberately**; the rule above stands either way, and only Iris's specific object is
unfixed.

### Encounter cadence — nine creatures, three sightings each

| | |
|---|---|
| **Regular creatures** | **9** — three per chapter, chapter-locked, never reused across chapters |
| **Bosses** | **3** — one per chapter |
| **Sightings per creature** | **3** — two on the critical path, **one guarding something optional** |
| **Except the Beast** | **6** — the same cadence run twice, **3 on the path and 3 optional** (2026-08-01) |
| **Fights** | 9 in ch1, 9 in ch2, **12 in ch3** · **30 regular + 3 bosses** |

### The Beast is the exception, and the exception is the plot — added 2026-07-31

Jeremy's original chapter 3 pitch was *"an otherworldly beast that made its way through the portal ahead,
and if we don't close it then the world will be full of physical creatures that cross as well."* **So the
Beast appearing more often is not a difficulty curve, it is the thing the chapter is about.** Jeremy:
*"the many times will be impactful for the ending — that the creatures aren't just in the dream world any
more, and this is the future if you don't stop it."*

**It is also the only escalation in the game that is free.** Every other creature's repeats cost a
rescued character, an art asset and a scene. The Beast was never a person, so a repeat costs a room and
nothing else — which is exactly backwards from the usual situation where the thing you want more of is
the expensive thing.

> **The survival rule is about the possessed. The Beast was never a person, so nothing is freed and
> nothing is lost — it is simply slain.**

Jeremy: *"the beasts are just slain, not knocked sense into them."* That carve-out is legible without a
word of explanation, because **it is the only thing in the game that does not look human**, so the player
never stands waiting for someone to get up.

**Escalate the presentation, not just the count.** Six of the same encounter is padding; one, then a
pair, then something larger near the portal is a tide coming in. Vary the staging — the count is the
budget, not the design.

**And this is what makes the mine's economy work.** §7 of `items.md` needs chapter 3 to supply the
materials its fight count consumes, or the rising action stalls into a farming trip in the worst possible
chapter. A slain Beast leaves a carcass, and **the fights that spend chapter 3's materials are the fights
that supply them** — see "harvest is the mirror of rescue" below.

~~**Two placement notes carried from the discussion.** The extra Beasts should mostly sit on the critical
path…~~ **Superseded by the split below, 2026-08-01.**

#### Three on the path, three optional — and three variants — 2026-08-01

The earlier note wanted the extra Beasts on the critical path so the escalation would actually be paid
for; §19's chapter 3 supply wanted them optional so a short player has somewhere to go. **Both are
satisfied by splitting the two jobs rather than compromising between them.**

| | Count | Job |
|---|---|---|
| **On the path** (`BlocksPath`) | **3** | the escalation. Three forced sightings is enough to read as *they are everywhere now* |
| **Optional** | **3** | the supply valve, and the last one guards the engine's pocket (§20) |

**Which means the Beast does not need a special placement rule after all — it needs the ordinary one,
run twice.** §21's cadence is two on the path and one optional per creature; the Beast is that doubled.
That is a simplification, not a carve-out, and it removes the only creature that had its own placement
law.

**Three visual variants, and they cost nothing extra on the tactical side.** Jeremy: *"we can make one
stronger variant towards the end of the level and make two different visuals — one the main-path version
and the other a side path. For this impactful enemy I feel like it is worth it."* A variant shares the
(vulnerable, resistant) pair, so the locked twelve is untouched — it is the same tactical problem with a
different `Durability`, a different `Damage`, and a different sprite.

| Variant | Where | Reads as |
|---|---|---|
| **the crosser** | main path, baseline | it got through |
| **the settled one** | side paths, tougher and slower | it got through a while ago, and it has been feeding |
| **the breach-thing** | last, at the portal — guarding the engine's pocket | there is something worse on the other side |

Three sprites for six encounters, and the escalation is carried by what the player sees rather than by a
number going up — which is what *"escalate the presentation, not just the count"* above was asking for
without knowing how to spend it.

**Why three sightings and not two.** First you meet it and learn its type the hard way; second you come
prepared, which is the payoff and the floor for a vulnerability to become knowledge rather than a puzzle;
third is optional. Two is enough to teach and three is what makes the opt-in system mean something — a
thorough player fights nine a chapter, a cautious one fights six.

**Required outnumbers optional, deliberately.** Jeremy: *"I like needed being larger than optional. A
lot of games flip it and it feels like a chore."*

**The optional sighting is where two existing rules land, and they land on the same encounter.** §7's
placement rule puts the Fire- and Caustic-vulnerable creatures on the optional sighting in chapters 1–2,
so the thing you cannot kill yet is never in your way. And **the optional sighting guards a repair
site** — so beating the fight you did not have to leads to a screen you can restore, which yields
Graveflower, which builds the engine, which saves your wife. **Every piece of optional content in the
game points at the ending, and none of it needs a quest log to say so.**

### Repetition is not a compromise here — it is the theme

Repeating a creature is normally padding. It is not, in this genre and under these rules, and the reason
is worth stating because the instinct is to apologise for it.

- **A screen is a memorised place, not a corridor.** So the constraint is narrow: **one creature per
  screen, never the same one on two adjacent screens.** Adjacent duplicates read as wallpaper; the same
  trade in the deep woods and again at the ford reads as an ecology.
- **Nothing ever respawns on a cleared screen.** This is the real violation in a point-and-click, and the
  design is already immune: a defeated body stays where it fell, and the screen changes permanently.
- **Every instance frees a *different named person*.** So the corrupted form repeats and the recovered
  one does not.

That last point is where repetition stops being tolerated and starts being the point:

> **Corruption erases the person. Freeing them restores it.**

Three identical hunched silhouettes are not three copies of a monster — they are what the Scourge *does*,
which is make people interchangeable. Individuality comes back at the moment you save them. And the
village visibly fills with people you rescued as you clear it, which sets up the finale for free: the one
person you cannot save.

**That beat now has a home: the chapter 2 camp** (§17), which is the first house you liberate and fills
as you go. It is shown with bedrolls rather than bodies — three painted states of one room, a handful of
named people with lines, and a countable pile of bedding standing in for everyone else. And the chapter 3
camp is the answer to it: nobody there at all, except the one man you killed.

### What this costs in code and art

**Code — three consequences, none urgent, all cheap if taken before content:**

| | |
|---|---|
| `EntityData.SpiritTexture` / `ShatteredTexture` / `EchoTexture` | **dead.** Written when both realms were 2D; the Dream went 3D on 2026-07-30 and a `Texture2D` cannot be handed to a low-poly pursuer. Delete, and fix the class comment that still describes "six in total". |
| `EntityData.HarvestItemId` / `PurifiedTexture` | **unused, and still unused even though the Beast drops materials** — see below. Nothing is harvested via the *entity*. `EntityState.Purified` survives and now means *this person is freed*, which is the flag the rescued NPC spawns on. |
| **Bosses have two forms, not three** | blank `PurifiedName`, blank `HarvestItemId`. They are destroyed, never purified. |

**A freed human is an NPC, not an entity state.** The room places an NPC hotspot gated on the flag the
fight sets — existing hotspot-and-flag machinery, no new fields. Which means `PurifiedName` belongs only
to bodies that stay bodies, and after the animal cut there are none.

#### Harvest is the mirror of rescue, and it is the same machinery

Jeremy, 2026-07-31, on the extra Beasts: *"lots of beasts to drop stuff."* That looked like it revived
`EntityData.HarvestItemId`, which had just been declared dead. **It does not.** The drop is authored the
same way a rescue is:

| | The room places… | Gated on |
|---|---|---|
| **Beat a person** | an **NPC hotspot** — they get up and speak | the fight's flag |
| **Slay a Beast** | a **harvest hotspot** — a carcass you can work | the fight's flag |

One word different in the room data, no new code, and `InteractionService.Harvest` already exists and is
already flag-gated on `harvested.{instanceId}`. It is also **more** flexible than the field would have
been: two Beasts in different rooms can drop different things without two `EntityData` files.

> **You can only harvest what was never human.**

That is the rule, and it is not squeamishness for its own sake — harvesting a neighbour you have just
freed would be grotesque in a way that would recolour the whole rescue system, while dressing a kill that
came through a portal is just work. The one animal in the game is the one thing you can render down.

**What it drops: Tallow and Cured Hide.** `items.md` §1 reclassified both as scavenged goods *because*
the animals were cut, and a carcass is their obvious source — so chapter 3 gets them back from the only
place they ever made sense. Bitterroot and Pitch have to be seeded thick in the mine by hand instead; a
Beast cannot plausibly supply either.

**Art — the brief, because the failure mode is expensive:**

- **Roughly 9 corrupted + 9–12 freed ≈ 20 assets**, not 27. The corrupted nine must each be unique; the
  freed can reuse a design across two or three instances, and **varying the pose rather than the design**
  (kneeling, standing, being helped up) reads as different moments rather than pasted copies. At 80
  pixels tall, identity is silhouette and costume, not face.
- **Silhouette carries everything.** Nine enemies have to be tellable apart by shape alone with no detail
  legible. This is free here, because nine trades are nine shapes — but it is the note an artist will
  miss if it is not written down.
- **Commission one full character and a style sheet first, approve, then order the rest.**
- Low-res is not a style choice bolted on: **480×270 with 4× nearest is what the renderer already is**
  (§16).

**Writing, which is the cost that lands on Jeremy rather than on the budget.** Twenty-seven freed people
is twenty-seven scenes, and filler dialogue damages a short game more than a smaller cast would. Settled
2026-07-31: the apothecaries and anyone handing over something meaningful get real scenes; everyone else
gets **one line or a nod, and walks off.** Jeremy: *"people don't care too much about side stuff."*

---

## 22. The Waking world — screens, blights, and mending — ratified 2026-08-01

**This section exists because §17 made camps a menu, and that took the teeth out of every Waking-world
hazard in one move.** With a free heal available at any moment, no attrition mechanic outside a fight can
matter. Rather than try to save attrition, this changes what a hazard *is* — into the one thing free
healing cannot defeat.

### Three reasons a screen is not finished

This is the whole interactivity vocabulary of the Waking world, and everything else is examine text.

| | The screen is… | Answered by |
|---|---|---|
| **Held** | a creature stands on it | the fight |
| **Blocked** | a way through is shut | the chapter's tool — Bill Hook, Pry Bar, Mine Pick |
| **Fouled** | a made thing is broken | **mending it** — see below |

**A screen with none of these left is cleansed**, and cleansing is free and universal: the colour comes
back, the light changes, and nothing is charged for it. That is Jeremy's requirement — *"I do want every
screen to be able to lift the visual curse"* — and it holds even for a screen that only ever held two
items to pick up.

**Creatures, interactables and pocket doors share screens.** Jeremy: *"enemies and point-and-click
objects or portal items should be on the same screen; it will space out the content better… we don't want
screens to feel empty either."* The one thing that stays on its own screen is a Dream pocket's interior,
which is a different realm.

### Hazard is renamed Blight, and the verb is Mend

`Hazard` stopped being accurate the moment it stopped dealing damage.

> **Blight** — the thing. **Mend** — the verb. **A mended screen** — the state.

*Blight* is a land word rather than a person word, and it reads as what the Scourge leaves behind rather
than as a trap someone set.

**Code side this is more than a rename, and the difference matters — a blight is not the hazard that
exists today.** There is deliberately **no `HotspotKind.Hazard`**: a hazard is a `VitalityCost` plus a
`HazardText` on any existing hotspot kind, charged by `InteractionService` on a successful click. A
blight charges nothing and damages nobody. So:

| | Today's hazard | A blight |
|---|---|---|
| **Shape** | `VitalityCost` + `HazardText` on any hotspot | its own thing — gates cleansing, presents a puzzle, pays out |
| **Costs** | Vitality | **a jigsaw. Nothing else, and there is no skip** |
| **Yields** | nothing | **1 Graveflower**, and the screen cleanses |
| **Repeats** | by default, unless flag-gated | never — one flag, one mend |

**What becomes of `VitalityCost` is a decision for the code pass, not this section.** Waking-world damage
is no longer part of the design, so the field has no design customer left — but `yard_cairn` is authored
with `VitalityCost = 10`, that content is verified in play, and it is currently the only way to reach 0
Vitality outside a fight. Do not delete either the field or the cairn on the strength of this section:
**decide it deliberately when the code pass reaches §22, and check what the collapse-path gates are
actually exercising first.** This is exactly the class of thing the `--verify-slice` blind-spot note
warns about — a green gate over content that the design has quietly stopped wanting.

The one clean rename either way: **`HazardText` → `BlightText`** if the field survives.

### The twelve blights are the restoration sites, and they are the good ending's currency

**This is the load-bearing consequence, and it collapses four systems into one chain.**

> **A blight is what makes a screen one of the twelve restoration sites.** Mending it cleanses the screen
> and yields **1 Graveflower**.

hazard → restoration site → Graveflower → the exorcism engine → Iris lives. Every optional thing in the
Waking world now points at the ending, with no quest log saying so, and the hazard machinery that was
about to be deleted becomes the authoring surface for the twelve.

**Why the count is exactly twelve and not "many":** [`items.md`](items.md) §6 already fixed the minor
restorations at twelve because §1 fixes the world's Graveflower at twelve and §20 prices the engine at
eight. The blight count was never free; it was already spent.

### Mending costs a puzzle, not materials — and as of 2026-08-01 it costs *only* that

An earlier draft of this had a blight cost commons to put right. Jeremy: *"I think a mini-game to cleanse
the land is a better option than giving up resources."* He is right, and the argument is bigger than
protecting the economy:

> **Paying materials makes the good ending a grind. Solving makes it an attention gate.** The player who
> looked carefully gets the engine.

That is exactly the ending §20 wants, and it is the one gate the design had no mechanism for.

**Not a scored mini-game — a small point-and-click puzzle**, which is the same verb §15 already ratified
for Dream pockets. It needs no new system: hotspots, examine text, and order. Jeremy: *"I want the puzzles
to be fun and fulfilling and not too difficult."* A simple slider is explicitly in scope.

#### It is a jigsaw, it is required, and the skip is deleted — ratified 2026-08-01

**This reverses the pay-to-skip that the rest of this subsection was built around.** Two moves by Jeremy
collapsed it, and the second one is the argument:

> *"You cannot fail a slider puzzle."* — and — *"I should require it or just not do it. No game or event
> means you look at something and get a flower though lol."*

The first killed the framing. A puzzle with no fail state is not a difficulty gate, so "unskippable" never
meant *hard*, it only meant *thirty seconds*. Every condition written above was protecting against a wall
that could not exist. **The condition that the skip must still yield the Graveflower is therefore void** —
it was guarding against gating the ending on puzzle skill, and there is no puzzle skill to gate on.

The second is the real reason: **the puzzle is what earns the flower.** Remove it and a blight is a pickup
with extra steps. So it is all fifteen mends or none of them.

**And a skip could not have been balanced anyway.** Commons are abundant by design — §19 requires that a
common never be the reason a craft is refused — so a commons skip is either cheap, in which case everyone
takes it and the puzzle is dead content, or dear, in which case it breaks the commons rule from the other
side. There was no price that worked.

**The out the design was trying to provide already exists one level up.** Blights sit behind optional
fights (§21). A player who does not want them does not do the optional content, never meets a blight, and
finishes by severing — which §20 treats as a legitimate ending and which the Blacksmith's *"greatest mercy
at the time"* speech exists specifically to protect. The skip was a second out for players who already
had one, and redundant options are not free: they cost a price to balance, a second interaction path, UI
to explain, and nobody uses them.

| | Was | **Is** |
|---|---|---|
| **Mending a blight** | a puzzle, or commons to skip | **a jigsaw. That is the whole interaction** |
| **Skipping** | yields the flower, must cost more than solving | **does not exist** |
| **The 12 → 8 slack** | four blights you could not be bothered with | **four blights you did not find** |

**That last row is the gain.** The slack now measures exploration rather than tedium tolerance, which is a
far better thing for the good ending to hang on, and it is exactly what §20's *"you went back for it"*
wants. Miss up to four blights entirely and the engine is still affordable.

**A jigsaw specifically, and small.** Jeremy: *"a jigsaw is shorter and gives the same completionist
feeling — win-win."* Board sizes stay deliberately modest; invariant 6 applies (expensive never long) and
a mend is a payoff, not a test.

#### The three major repairs get one too — and the same rule

The bridge, the mine cart and the exorcism engine were costed as commons-plus-a-memory, which made the
three biggest moments in the Waking world the only flat ones. They get a jigsaw as well, on the same
terms: required, unfailable, short. **Fifteen mends in total, one interaction, no branching.**

The framing that settles it is Jeremy's — *"I thought they would look cool to complete the objects"*:
**the puzzle is not a gate, it is the animation.** It is what makes a repair an act rather than a purchase.
The engine is different only trivially, and it is a tautology rather than a rule: skipping its assembly
would mean not building the engine, so there is nothing to skip *to*.

**Twelve blights plus three majors is a real authoring bill.** Keep it tractable with roughly **three
forms, five instances each** — *spot the wrong part among many*, *do these in the right order*, *use the
right tool on the right thing* — with the third form gated on the chapter's tool, so a chapter 3 blight
cannot be solved in chapter 1 and the fifteen spread themselves across the game without a difficulty
curve to tune.

### What Daniel is actually doing — he repairs, he does not build

Jeremy raised the lore problem directly, and it is a real one: *"this is tripping me up lore-wise more
than mechanically — how should Daniel use his craftsman ability to ward off the curse? Building a statue
comes to mind but it feels like idolatry, which is counterintuitive to the plot."*

**The statue is not needed, and the instinct that it was wrong was correct.** An idol is made to be looked
at. A tool is made to be used. The answer was already in the premise:

> **A blight is a broken made-thing.** A well cover, a fence, a wheel, a hearth, a lintel, a boundary
> stone, a plough. The corruption spreads through the works of human hands left undone, and Daniel — a
> craftsman — puts them back in working order. The ground stops being sick because the works on it are
> whole again.

Five things fall out of that, and none of them had to be designed:

- **The idolatry problem disappears.** He is not making a holy object. He is doing his job.
- **It is the theme, exactly inverted.** The demon stole his craft. Every mend is that craft used the way
  craft is meant to be used.
- **It explains why it has to be him** — not because he is chosen, but because he is the one who knows how
  things go together. That is the un-godlike heroism Jeremy asked for two decisions earlier.
- **The puzzle and the fiction become the same act.** "Put this back together correctly" *is* the three
  forms above. The puzzle never feels bolted on, because it is not a puzzle, it is the work.
- **It finally explains Graveflower.** You do not manufacture it. **It grows where the land is right
  again.** So the material that saves Iris comes only from healed ground — twelve small repairs to one
  town, gathered into the thing that repairs a person.

**The maker's mark.** If a mended screen needs a visible sign — Jeremy's serpent instinct, which was
sound — the craftsman's version is not a statue but **his stamp on the work.** One sprite, a signature
rather than an object of worship, and a free visual index of what is done. It is also quietly grim by the
end: his mark on twelve broken things, and on the box he built for his wife.

### Where a Dream pocket's door sits

| Pocket | The door is | Why |
|---|---|---|
| **Boss pocket** (3) | the tethered person's place — the forge, the trap line, the cottage | you go where they were |
| **Knowledge pocket** (8) | **something of Daniel's own that the demon ruined** — a warped blade, a burnt page, his own bench | the stolen-craft premise made literal: you put your hand on your ruined thing and fall in after what was taken from it |

**A door appears on a Held screen once the creature is beaten**, which is Jeremy's *"the side pockets
would be in their own screen beyond an enemy."* Doors are never on Fouled screens.

**That last rule is not cosmetic — it keeps the critical path free of material spend.** Blights are the
optional twelve; if a pocket door sat behind one, progression would depend on solving a puzzle or paying
commons, and both are things this design has spent two sections making optional.

> **A third kind of door joins this table 2026-08-01 — the knot pockets.** See §23. Their doors follow
> the same rule (Held or clear, never Fouled) and their objects are different again: not a person's place
> and not Daniel's ruined thing, but **the demon's own work** — a cord of it, running into the rock.

---

## 23. The Snarl — chapter 3's gate — ratified 2026-08-01

**The problem this solves was structural and Jeremy found it before I did.** Chapters 1 and 2 each end
behind a **major repair**: the bridge, then the mine cart. You cannot reach the boss until you have
rebuilt the way to them, and that repair is the chapter's spine. **Chapter 3 had nothing.** You arrive
in the mine, fight twelve fights, and the Demon is simply *there* — the one chapter with no reason you
cannot walk straight at it.

His proposal: *"We could make a force field door that can only be broken by breaking 2–4 anchors that
are powering it. Would close off the path to the boss so the player has to run around and not go
straight there. Would be more dream world content and opportunities to use chapter 2 powers."*

**Ratified with two changes of vocabulary and no change of mechanism.**

### What it is

**The demon has knotted the way shut.** The last passage of the mine is closed by **the Snarl** — a mass
of the demon's binding grown across the rock, the same craft it uses on people turned on a place. It
cannot be cut, mended, forced or crafted around in the Waking world. Three **cords** run out of it and
disappear into the stone in three directions.

**Follow a cord to where it goes into the rock, and there is a pocket door.** Inside, in the Dream, is
the **knot** that cord is tied in. Cut the knot and the cord goes slack out here. Cut all three and the
Snarl falls open.

| | |
|---|---|
| **The barrier** | **the Snarl** — Waking, impassable, one authored screen |
| **The trace** | **3 cords**, followed across the mine to three doors |
| **The task** | **3 knot pockets** — hunted Dream pockets, one per cord |
| **The verb** | **cut**, which is the verb you already have |
| **The reward** | **passage, and nothing else** — no material, no upgrade, no recipe |

### Why "knot" and not "anchor"

**Jeremy's word was "anchor" and it is already taken.** An anchor is *the object a tethered person's soul
is bound to* (§21) — the whole trade at the finale turns on it, and Iris's anchor is the one thing §20
leaves deliberately open. A second meaning on that noun would be the worst possible collision in the
worst possible chapter. Jeremy: *"it will be easy to fit that into a narrative."*

**"Knot" is the better word anyway, and not only because it is free.** The demon binds a person with a
**tether** to an **anchor**; it binds a *way* with a **knot**. Same hand, same craft, different victim —
so the vocabulary tells you the mine's door and the woman in the finale were done by the same thing.
**And your answer is the same answer:** you sever a tether and you cut a knot. The gate teaches no new
verb. It is the verb the whole game is about, aimed at architecture for the first time.

"Force field" was the wrong register for the same reason — it is a science-fiction noun in a game whose
entire vocabulary is craft, rope, iron and rot.

### Why three, and what each one is for

**Three, because there are three Dream verbs and this is their exam.** §19 gives you Suppress from the
start, Float after the first boss and Flare after the second, all drawing on one Resolve meter. Chapter 3
is the only place all three are in hand, and until now nothing asked you to have understood them.

| Knot | The verb it examines | The bind |
|---|---|---|
| **The lit knot** | **Flare** | it is invisible in the dark and the pocket is dark. **The light that shows it to you shows you to the pursuer** — you cannot look and hide at the same time |
| **The high knot** | **Float** | it is tied out of reach. Floating spends the same meter suppression does, so the climb is paid for out of the hiding |
| **The far knot** | **Suppress** | it is past the pursuer, in the open. Moving unlit means **you cannot see it until you are on it** — you go from memory |

**One verb each, one meter, three different squeezes on it.** That is the whole design; the level
authoring is Jeremy's.

~~**Two is the fallback if the 3D bill hurts** — drop the far knot, since Suppress is the verb the player
has had longest and needs the least examining.~~ **Four is not on the table**: there is no fourth verb, so
a fourth knot would be a repeat rather than a question.

> **The two-knot fallback is withdrawn — 2026-08-01.** Jeremy: *"Want them to beat all 3."* **Three is
> now the committed number, not a target with an escape hatch under it.** See *"If the bill hurts, the
> lever is scale"* below for what replaces it.

### What it costs the rest of the design — checked, and the answer is very little

- **It does not touch "eight pockets, eight max-Resolve upgrades"** (§19). **A knot pocket grants no
  upgrade.** Its reward is passage — a Zelda door at chapter scale — and the eight knowledge pockets keep
  their one-to-one with the eight upgrades. This was the first thing checked and it is the constraint most
  at risk from adding pockets.
- **It does not add a major repair or a jigsaw.** Fifteen mends stays fifteen (§22). **A knot is cut, not
  mended** — it is the demon's work, and Daniel repairs human work only. The two ideas do not overlap even
  slightly, which is a good sign the noun is right.
- **It adds no fights and no creatures.** The three doors sit on three of chapter 3's **existing seven
  critical-path fight screens**, per the Held-screen rule above.
- **It costs no material.** Nothing is crafted, so §1's economy is untouched and rule 13 in
  `placement.md` is not engaged.
- **It adds three 3D pocket interiors** — 11 to 14 across the game — and **one Waking screen**, the Snarl
  itself. That is the real bill and it is worth stating plainly rather than burying: **chapter 3's Dream
  authoring roughly doubles.**

### The "fewer pockets in chapter 3" rule is amended, not reversed

`items.md` §7 records Jeremy's objection in his own words: *"less pockets in level 3 and more beasts to
fight will help with the rising action, rather than being stuck in the dream world thinking about
progressing in the awake 2D world."* Three new pockets in chapter 3 looks like a straight contradiction.
It is not, and the distinction is in his sentence: **what he objected to was being stuck in the Dream
*thinking*.**

> **The rule now reads: chapter 3 has fewer *knowledge* pockets and more *action* pockets.** Knowledge
> pockets are where you stand still and work something out. Knot pockets are chases. Chapter 3 keeps
> exactly **two** knowledge pockets — unchanged — and gains three pockets that are pure pressure.

Jeremy, on the same point: *"I think this will help with the falling action also rather than hurt.
Demons in the mine, more action-packed dream worlds, breaking the knots to do the final battle."* That
is the shape the chapter wanted: **the Dream stops being the room where you go to think and becomes the
place the chase happens.**

**And it retires a stale category.** §15 and the pockets note described exactly two kinds of pocket:
quiet knowledge pockets and **hunted boss pockets**. Knot pockets are hunted and have no boss at the end,
so the two kinds are properly **quiet and hunted** — "hunted" simply stops being a synonym for "boss".
Nothing about how a hunted pocket behaves changes.

### Rules the knot pockets inherit unchanged

Stated so no one re-derives them at authoring time:

- **The pursuer costs Resolve on contact, and cannot kill you** (§15). Vitality never appears in the
  Dream. **This is not an aura** — aura is boss-only and drains passively; the pursuer is a hazard.
- **0 Resolve ejects you to camp**, the camp menu refills for free (§17), and you walk back in. **A knot
  pocket cannot dead-end a save** — invariant 12 holds without a special case.
- **A cut knot stays cut.** It is world state, like a severed tether and a mended screen.
- **The order is the player's.** Three cords, three directions, no sequence.

### What a cut knot pays — ratified 2026-08-01

Jeremy: *"So yeah just more falling action / narrative I agree."*

**The reward slot was empty by construction, not for want of ideas, and that is worth recording before
anyone tries to fill it with a mechanic.** A gate like this normally pays out one of three things, and
three separate earlier decisions had already spent all three:

| The usual reward | Why it is unavailable here |
|---|---|
| **A shortcut back** | worthless — camps are a menu (§17), travel is already free |
| **A power** | spoken for — **eight knowledge pockets, eight max-Resolve upgrades** (§19), and that pairing is load-bearing |
| **A material** | banned outright (`items.md` §3) — a material reward drifts the premise to *the demon stole the supplies* |

**What is left is the fourth currency, and it is the one the Dream trades in anyway: knowledge.**
Everything you bring out of a pocket is something you now *know*. So the knots pay in knowledge, and it
costs no system to do it — no item, no flag, no field, no UI. A cut knot is already world state.

### What he learns: how to read the demon's work

**You have spent two chapters meeting tethered people from the outside** — corrupted, coming at you. You
have never once seen the binding itself. **The knot is the demon's joinery, and Daniel is the one
character who would stand there and read it**, the way any maker reads another maker's work. That is the
stolen-craft premise (§20) spending its last dividend: the demon took his craft, and the final use of
that craft is recognising the demon's.

| Cut | What he sees |
|---|---|
| **First** | **the work.** Not a monster's mess — a *joint*. Deliberate, tied by something that knew exactly what it was doing. A craftsman's recognition, and it is not a welcome one |
| **Second** | **the hand.** Same tie, same habits, the same small wrongness in the same place. One maker did the wood, the village and the mine |
| **Third** | **where else it is.** He has seen this tie before, and he knows where |

**The third beat is the one the chapter needs.** It hands the finale its question a chapter early —
which is precisely the pacing Jeremy is describing — and it does so without answering it.

### The two "threes" are different threes, and confusing them is the likely mistake

Jeremy, on reading the above: *"I thought we said to do 3 based on the three Spirit abilities you have
at that point?"* **Yes — and that is still the reason there are three knots.** The count has since
picked up a *second* justification and the two hang on different hooks, which is worth stating flatly
because the section now argues both and a reader will assume they are one argument.

| | Fixed to the pocket | Floats to the cut order |
|---|---|---|
| **The verb it examines** | ✔ lit = Flare, high = Float, far = Suppress | — |
| **The narrative beat** | — | ✔ first / second / third cut |

**Whichever cord the player follows, they get that pocket's verb and the next beat in the sequence.**
Enter the high knot first and it is Float + beat 1; enter it last and it is Float + beat 3. The verb
assignment never moves. Nothing about *"write the third so it only lands because the first two
happened"* touches which knot examines which verb — "third" there means the third **cut**.

> **Why the count is over-determined rather than double-counted:** three verbs says *why a fourth would
> be a repeat*; three acts says *why a second would be a mutilation*. They constrain from opposite
> sides, which is why the number is now hard in both directions.

### Three guards on this, all of them cheap to obey

- **It attaches to the ordinal, not to the pocket** — and therefore **the beat text may not mention the
  pocket it fires in.** No darkness, no height, no distance: beat 2 plays wherever the player happens to
  be, so it can only talk about **the knot itself**, which is the same craft in all three. This is the
  authoring rule the ordinal decision actually produces, and it is easy to violate by writing three
  atmospheric paragraphs that each describe their own room. Tie the "one maker" line to the high knot
  and half of all players hear it first, where it means nothing.
- **It does not close §21, and must not.** *What* Iris is tethered to is still deliberately open — her
  work or her body. The knots are **where the player finds out, not the decision about what they find
  out.** Write the third beat so it names the recognition and not the object, and either answer still
  lands on it.
- **It does not undercut the Blacksmith's ghost or the engine** (§20). The ghost gives you the
  *principle* and never the recipe; the engine is *how*. Reading a knot is neither — it is **what you
  aim the engine at.** Three different pieces of the same answer, arriving from three different places,
  none of them redundant.

**And it retro-justifies the Snarl itself.** A barrier that exists to slow you down is arbitrary level
design. A barrier that exists because the demon would rather you did not arrive **knowing** is a motive.
The knots are the thing it was hiding.

### If the bill hurts, the lever is scale — not count. Closed 2026-08-01

**The fallback used to be "build two knots instead of three." It is withdrawn.** Jeremy: *"Want them to
beat all 3."* Three is committed.

**And that is the right call for a reason beyond preference, now that the reward is narrative.** The
moment each cut carries a beat, the three stop being three instances of one room and become **a
three-act structure that only works at three** — the work, the hand, where else he has seen it. Cutting
one does not remove a third of the content, it removes the middle term of an argument. A two-knot
version would still *function* as a gate and would no longer *say* anything, which is the worst kind of
cut: it passes every check and quietly deletes the point.

> **What replaces the escape hatch, so there is still an answer if chapter 3's Dream authoring runs
> long: shrink the pockets, do not drop one.** Cost per pocket is the elastic quantity here and count is
> not. A knot pocket is **seen once**, has **one objective**, and its whole job is one squeeze on one
> verb — none of which needs size. §15's open note already asks what the *smallest* space that still
> reads as a place is; the built six-room maze is very likely more than a one-visit chase needs.

**Two consequences to hold onto:**

- **Three knots is now a floor, not a target.** It sits alongside "fifteen mends" and "eight pockets,
  eight upgrades" as a number the design leans on rather than one it happens to have landed on.
- **The three-act shape is the reason, so it is also the constraint on writing them.** If the three
  beats could be delivered in any order and any subset without loss, the argument above is false and the
  fallback quietly comes back. **Write them so the third one only lands because the first two happened.**
