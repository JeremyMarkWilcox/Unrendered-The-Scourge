# Combat & Progression — design

Status: the **Dream heist (section 9) is built and verified.** Everything about Waking-world
combat, traps and progression is **designed, not built** — numbers there are starting points
for tuning, not gospel.

Supersedes the "Sanity as universal health pool" idea from the original build plan, and the
Snap duel from the GDD. The resource is called **Resolve** everywhere, in the docs and now in the
code — there is no "Sanity" left in the project.

Sections **15–17** are newer than the rest and were written in one pass; where they disagree with
an earlier section, they win, and the earlier text carries a note saying so. The one deliberate
revision is in Section 6: Resolve now moves in Waking-world boss fights, which the original
version of that section forbade.

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
- carries high **Resilience** (flat damage reduction)
- projects a visible **aura**, which is also the room's ambient Resolve drain

Sever the anchor and all three collapse at once. The aura snaps off, the body visibly
deflates, regeneration stops, Resilience drops to a token value.

This teaches itself. A player watches a wound close, and understands. Then watches the
shimmer die, and understands they have won something. No tutorial copy required.

> Partly implemented: `EntityData.CorruptionAura` exists and gates on state. The old
> Waking-world *ambient* drain it used to feed has been **removed** — see the note in
> `EntityData.cs` — so the field is now free to carry what Section 6 asks of it.
> Missing: the visual aura, the regen, Resilience, and the boss-fight Resolve drain.

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
decisions happen *before* the first turn.

Menu-driven item selection is inherently repetitive, and more menu options do not fix it.
The fix is to make the room the weapon.

> You are a craftsman. You do not out-fight the thing. You out-build it.

### Traps

Discover **blueprints** in the world, build traps from materials, place them in the scene,
*then* trigger the engagement. Every room is a different rigging puzzle, so preparation
cannot go stale the way a combat menu does.

| Trap | Effect |
|---|---|
| **Snare** | it loses its next turn |
| **Pit / spikes** | heavy opening damage on engagement |
| **Oil slick + ignition** | damage over several turns, bypasses Resilience |
| **Salt line / ward circle** | **suppresses the aura for N turns — no regeneration** |
| **Chime** | lures it onto a chosen tile; reveals its condition |
| **Collapsing beam** | one-shot, large, consumes the site |

The **salt line is the load-bearing one.** It is a poor man's sever: a regen-free window
without ever entering the Dream. That is what turns overkill from *"can you afford it"* into
*"can you outsmart it"* — a far better reading of impractical-but-possible.

Traps are Waking-world only, which differentiates the two routes further: the real world is
where you scheme, the Dream is where you are exposed.

### Fits what is already built

Trap sites are another `HotspotKind`. Blueprints are `ItemCategory.Lore`. Both already exist.

---

## 5. Narrative presentation — states, not numbers

"No stats" and "count your oils" are both true: the numbers exist, they are simply never
shown. Instead of a health bar, an entity has **described conditions**:

> unmarked → bleeding → faltering → collapsing

The player reads prose and the aura shimmer. Never a digit.

> *"The oil takes hold. The hide splits, and this time it does not close."*

That single line reports that the salt line is holding, without any UI. This is less work
than a stat readout and much better horror.

### Length is the constraint that matters

**Fights should run 3–6 turns.** Fifteen turns of clicking oil is tedious no matter how good
the prose is.

Overkill must be **expensive, not long.** Traps front-load the damage, the salt line opens
the window, everything burns in five brutal turns. A setpiece, not a chore.

---

## 6. Two pools

| Pool | Spent by | Restored by |
|---|---|---|
| **Vitality** | Waking-world combat | her salves (finite), crude substitutes (poor) |
| **Resolve** | the Dream, and boss fights against an intact aura | leaving the Dream; resting at a camp |

**Resolve is your nerve, and an empty bar means one thing everywhere: you are removed from
the confrontation.** In the Dream that is ejection from the realm. In a boss fight it is
forced flight. One meaning, two venues.

It moves in exactly those two places. **There is still no ambient drain** — no
corrupted-ground pressure, no bleed for standing near something anchored, nothing outside an
actual confrontation. Normal enemies never touch it: they have no aura and cannot be severed,
so a drain there would teach the player nothing.

> **Revised.** This supersedes the earlier rule that Resolve *never* moves in the Waking
> world. That version kept the bar unambiguous by confining it to one place; the "removed
> from the confrontation" reading keeps it unambiguous while letting the aura actually bite,
> which is what makes fighting an un-severed boss feel like a decision. The incentive to
> sever was already carried by regeneration and Resilience (Section 2) — this adds pressure,
> not the reason.

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

Max Vitality rises through **rare items**, never levels.

---

## 7. The craftsman has no combat stats

Deliberate. If damage came from a strength stat, "over-preparing" would mean grinding levels
and the resource choice evaporates.

**Your damage is entirely what you brought and what you rigged.** A plain weapon cannot
exceed a Corrupted body's Resilience at all — it does literally zero.

The reason overkill is ruinous: **the thing keeping you alive is the same finite thing that
lets you win.** Her salves are both your healing and your ammunition.

### Legibility

The player must be able to estimate the bill *before* committing, or the choice is a trap.
Examining a Corrupted body with enough Craft proficiency should report something like
*"Resilience 12. Nothing you carry will scratch it unbuffed."*

---

## 8. Progression — three tracks, no attributes

No STR/DEX/INT. Nothing that multiplies damage; that is what kills dread. Everything gates
**access**.

| Track | Governs | Grows by |
|---|---|---|
| **Craft** | what you can build — gear, oils, wards, **traps** | making things |
| **Physick** | what you can brew | studying her notes — slowly, badly |
| **Attunement** | max Resolve, catalyst slots, what you perceive in the Dream | surviving the Dream |

**Physick starts near zero and grows poorly, on purpose.** You are a craftsman, not an
apothecary. Her finished stock outclasses your best work for most of the game, which keeps
the inheritance precious — and makes learning her craft mean something, given what actually
happened to her.

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
in the Dream · a handful of traps · observation puzzles in both realms · camps.

No attributes. No XP. No skill trees. No second inventory. No health bars. **No damage
multipliers** — crafting grants variety and access, Zelda-style tools rather than weapon tiers,
which is what keeps invariant 5 intact while letting the crafting side run deep.

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
- `EntityData.CorruptionAura` already exists and already gates on state
- `SaveService` persists plain dictionaries, so per-entity combat state grows without a save
  format break
- `EntityStateManager.Resolve` stays the one place that knows how a state looks
- Trap sites and blueprints reuse `HotspotKind` and `ItemCategory`

The one thing that must widen: **`EntityPresentation.Interactable` + `Verb` is a
single-action model.** Combat needs a *set* of available actions. Contained; not worth doing
until M5 starts.

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
- ~~Can traps be recovered after a fight, or are they consumed?~~ **Consumed, always** — win,
  flee or driven off. Which is why they must be cheap and rebuildable from common materials;
  if failure destroyed precious components, players would reload rather than retry.
- ~~What the Dream fight actually *is*, turn to turn.~~ There is no Dream fight. See Section 14
  and Section 15.

**Still open:**

- Trap placement: free-form on a grid, or fixed authored sites per room? Authored sites are
  far less work and probably read better on painted backgrounds. Nothing else blocks on this.

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

### 2.5D — the gate is cleared, and it is the plan

Node stealth existed to answer one question before spending months: **is being hunted in this
game frightening?** It has now been played, and it is — you feel rushed and chased with nothing
but text proximity cues and no audio at all, which makes the result stronger rather than weaker.

The cost estimate that justified deferring also turns out to have been wrong, because it assumed
3D creature art. The actual plan is **pixel-textured geometry with the hostile spirits as 2D
sprites** — no rigs, no animation blending, no 3D character work. At 480×270 with nearest
filtering, low-poly reads as style rather than as cheapness.

The architecture absorbs it: the Dream already renders through a `SubViewport` that can host a
3D subscene, and nothing in `RealmTransitionManager`, `EntityStateManager` or the save system
cares what the Dream looks like.

**The real risk is mechanical, not artistic.** Real-time movement adds a *reflex* requirement
even though ejection-on-touch keeps invariant 2 intact — and a player who cannot dodge would be
walled out of the efficient route and forced into overkill, which is exactly the difficulty
slider Section 1 exists to prevent. So the 2.5D Dream should be deliberate rather than twitchy:
slow, generous grab windows, readable telegraphs. Slower is also more frightening, so this is
probably not a trade at all.

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

Near-free to build: a camp is a `RoomData` with a flag, or another `HotspotKind`.

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
