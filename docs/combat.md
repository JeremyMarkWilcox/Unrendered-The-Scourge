# Combat & Progression — design

Status: the **Dream heist (section 9) is built and verified.** Everything about Waking-world
combat, traps and progression is **designed, not built** — numbers there are starting points
for tuning, not gospel.

Supersedes the "Sanity as universal health pool" idea from the original build plan, and the
Snap duel from the GDD.

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

> Already half-implemented: `EntityData.CorruptionAura` exists and `SanityManager` only
> counts it while the entity is `Corrupted`, so severing already reduces room drain today.
> Missing: the visual aura, the regen, and Resilience.

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
| **Resolve** | the Dream, and only the Dream | returning to the Waking world |

**Resolve does not move in the Waking world.** No ambient drain, no corrupted-ground
pressure, no bleed for standing near something anchored. It is a heist clock and nothing
else, and coming back refills it. One resource, one meaning, one place.

**At 0 Vitality:** you wake in a sanctuary having **lost everything you were carrying.** Not
a reload — the run cost you the inheritance. Crueller and more interesting than lost time.

**At 0 Resolve:** the Dream throws you out. There is no Snap duel; Resolve empty and being
touched are the same ending. Because the bar cannot move outside a heist, an empty bar has
exactly one meaning.

> **Consequence, unresolved.** With a full refill on return, *failing a heist now costs
> nothing but time* — and the two-route economy in Section 1 depends on the Dream route
> having a price. Cheapest fix that fits everything else: **crossing consumes a catalyst.**
> You burn incense to open the way; botch the run and you have to go make more. That moves
> the cost into the finite-supply economy where it already belongs, keeps Resolve a pure
> timer, and is exactly the GDD's *"real-world crafting items catalyze abilities in the
> Pixel Realm."* Needs M4 inventory first. Alternatively drop
> `DreamHeistData.ResolveRestoredOnReturn` below 1.0 so a botched run leaves a mark.

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

---

## 11. Complexity budget

Two pools · three proficiencies · one turn-based resolver with two skins · five Dream verbs ·
a handful of traps.

No attributes. No XP. No skill trees. No second inventory. No health bars.

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

- **Does body damage persist between engagements once severed?** Section 3 assumes yes. It is
  the only item here that touches the save format, so it wants deciding before M5.
- Trap placement: free-form on a grid, or fixed authored sites per room? Authored sites are
  far less work and probably read better on painted backgrounds.
- Can traps be recovered after a fight, or are they consumed?
- What the Dream fight actually *is*, turn to turn — Section 9 lists the verbs but not the
  shape of the encounter. **Biggest remaining gap — see below.**

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

### Not built, deliberately

The 2.5D version. A first-person pixel-3D Dream would be more immersive, and the architecture
would absorb it — the Dream already renders through a `SubViewport` that could host a 3D
subscene, and nothing in `RealmTransitionManager`, `EntityStateManager` or the save system
cares what the Dream looks like.

But it is months of work, most of it in *feel*, which cannot be shortcut. Node stealth answers
the only question that matters first: **is being hunted in this game frightening?** If it is at
this fidelity, 2.5D is worth the months. If it is not, higher fidelity would not have rescued
it.

### Still open

- Audio. Stealth without sound cues is thin, and this currently has none.
- Does the pursuer's pure-BFS chase need softening? Untested against a real player.
- The entry puzzle is a placeholder `Interact` hotspot, not a minigame.
- Traps (Section 4) are designed but unbuilt; they need M4 inventory first.
