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
| **Overkill** | Waking world | traps, consumables, her supplies — finite | stubbornness, and it hurts |

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

**At 0 Resolve in a boss fight:** you are driven off to camp and **lose everything you
rigged** — traps spent, the site consumed, rebuild before you try again. Fleeing voluntarily
costs the same rigging; the difference is that you still have your unspent consumables,
because you stopped before pouring them all in. Same loss, very different bill.

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
between visits (Section 3), so overkill has to land in one sitting; her salves are finite; and
a failed engagement costs you the rigging. Put those together and a player who fails twice
could otherwise lose the overkill route *permanently*. So the crude versions have to be able
to close a fight — worse, slower, needing more of them and better rigging — or failure
silently deletes half of Section 1. Not a soft lock, since severing always remains, but the
second route would quietly stop existing.

**Her named salves stay uncraftable.** Physick approaching their quality late (Section 8 says
her stock outclasses your work *"for most of the game"*) means brewing your own near-equals
under your own names. You learn her craft; you never duplicate her hand.

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

**Still open:**

- **Whether Fire belongs to Craft or to Physick.** §18 builds against three Craft types and Caustic
  alone on her side, because the asymmetry matches §8's "few of her notes, far apart". Moving Fire
  across would make it two and two and is a one-line change; it is recorded here so the choice is not
  mistaken for an accident.
- **Whether the encounter needs a defensive verb at all.** See the shield note above. Unanswerable
  until three fights have been played, and cheap either way.

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
| Catalyst on crossing | **no** | **yes**, as Section 6 decided |
| What a failed run costs | time, and nothing else | a catalyst, so a gathering trip |
| Trigger item | consumed on success only | consumed on success only |

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

The through-line: with healing free, a lost engagement costs you **your rigging and your
consumables, not your HP bar** — which is Section 4's thesis stated in the economy. HP was never
meant to be the scarce resource here.

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
| **Swarm** | several weak bodies | traps that catch more than one; single-target answers are inefficient |

**None of these needs a field.** Brute and Warden are the existing numbers arranged differently, and
Swarm is an encounter with more than one body in it — so archetype is a description of *how to
author*, not something to store. Same principle as camp-ness being derived from the workbench (§17):
do not store what falls out. If behaviour ever diverges enough to need one, an `Archetype` enum is
additive.

A fourth, **Afflicter** — barely hurts you in the fight but leaves something on you that keeps
costing Vitality afterwards — was raised and **cut.** It was the only one of the four that needed a
mechanic which does not exist (a persistent condition), and the other three earn their difference
for free.

**Consequence for the build:** the encounter holds a **list** of bodies from day one even though the
first authored fights will have one each. Swarm is nearly free if the list is there from the start
and expensive to retrofit, and a list of one behaves exactly like a single enemy.

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
