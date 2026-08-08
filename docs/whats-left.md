# What's left

**The bill for the rest of the game, in build order. This is the only place the remaining work is
listed** — `handoff.md` and `build-plan.md` §8 point here rather than keeping their own copies, because
three lists of the same work is how the last one went stale three times in two days.

**Two things are deliberately not in this file.** Counts of authored content — run
`godot --headless --path . -- --verify-content`, which prints the bill against what is actually on disk
and is never out of date. And *why* any of this is shaped the way it is — that lives in `combat.md`,
`items.md` and `build-plan.md` §7, which are the ratified rulings.

**Rule for keeping it honest: a finished line is deleted, not ticked.** A list of done things is a
changelog, and git already is one.

---

## Now — and as of 2026-08-07 it is Jeremy's lane alone

Jeremy authors in the editor and plays; Claude writes managers, gates and derivable content. They touch
different files, so nothing below is blocked on the other lane unless it says so.

> **Chapter 2 was played end to end on 2026-08-07 and the path holds** — the villagers wake, her notes
> come off the Apothecary and the Still Room, the fused traps build, the cart is affordable. One bug came
> back and is fixed: equipment could be forged more than once. That was the last item in this section that
> was not a scene.
>
> **The C lane has nothing in *Now*.** C1 moved to the story sweep on Jeremy's call, and C3 and C4 are both
> blocked on Dream and chapter-1 work that has to happen in the editor first. So the honest reading of this
> file today is that **the game is waiting on scenes, not on systems** — and the one C-lane job that is
> unblocked is the small fallback check offered under J-UI below.

### J2. Pocket 1 in 3D, then play it — **the largest thing left, it unblocks the most, and it is now the only thing in Now that is not optional**
Zero of fourteen scenes exist. The checklist is in `handoff.md` (*"What a pocket needs"*) and it needs no
scripting — `PocketHost` attaches the verb poller, `PocketAnchor` finishes, `PocketExit` leaves.
**Build one, play it, then clone.** Cloning first is how fourteen scenes drift identically.
**Done when:** one `PocketData` names a real scene and you can walk in, reach the anchor and come out.
**Unblocks:** the whole Dream half, and it retires the stand-in path for that pocket alone.

> **All three verbs have an act as of 2026-08-07**, so playing a pocket now tests something. `Q` puts the
> lamp out, `E` throws it wide (`DreamSuppressibleLight` on the player's `OmniLight3D`), `Space` lifts you
> (`DreamProbeWalker`). **Two thirds of Flare is still missing** — the hidden-geometry reveal and being seen
> while flared — so do not read "Flare works" off the light alone. Details in `dream-probe.md`.

> ~~**Carries a hidden second job: `DreamCreatureData` — and there are currently zero of them.**~~
> **Closed 2026-08-07 — three archetypes authored in `data/creatures/`, and the roster has a gate.**
> `dream_still` (anchored, 12 and a 2 s window), `dream_drifter` (wanders, 8 and 3 s — cheaper per touch
> because it can find you, longer window because it can pin you), and `dream_pursuer` (`Banishes`, so no
> chunk at all). **They are archetypes, not content**: the three real bosses get their own files with their
> own names when their pockets exist, and Iris cannot be written until the finale is.
>
> **A `DreamCreature` reaches its data through an `[Export]`, so nothing loads these and nothing has to.**
> That is also why they needed a sweep of their own — every other collection is counted by the manager that
> loads it. Six checks: the files parse, ids are present and distinct, a banisher authors **no** chunk
> (`Touch` ejects before it reads one, so a number there is tuned and never consulted), a non-banisher
> authors one (zero is not gentle, it is invisible), and a banisher still stops hunting when your light goes
> out — because `DreamVerb.Suppress` is documented as *the only verb boss pocket 1 can be solved with*, and
> that is only true while this holds.

### J-UI. The real UI scenes — **nothing here is urgent, and none of it blocks anything**
Every screen works today; three of them are trees built in C# at runtime rather than scenes you can lay
out by eye. **Full node contracts are published in `handoff.md` §2** — build against those names exactly.

| scene | state | what to author |
|---|---|---|
| `scenes/ui/InventoryPanel.tscn` | **exists** | missing three optional nodes the script stopgaps: `%EquipButton`, `%SetButton`, `%CategoryTabs` |
| `scenes/ui/MendPanel.tscn` | **exists** | nothing outstanding |
| `scenes/ui/FightPanel.tscn` | **does not exist** | whole scene, plus a `%Dim` `ColorRect` at 55% black |
| `scenes/ui/CampPanel.tscn` | **does not exist** | whole scene — `%CampList`, `%CampTemplate`, `%CloseButton`, optional `%Heading` / `%Dim` |
| `scenes/ui/CraftingPanel.tscn` | **does not exist** | whole scene — `%RecipeList`, `%RecipeTemplate`, `%CraftTab`, `%PhysickTab`, `%CloseButton`, optional `%Heading` / `%Dim` |

`Hud`, `RoomView`, `Boot`, `Main` and the Dream probe all have real scenes already.

> **The trap, and it is the reason this has an entry at all: a wrong node name fails *silently*.** Each
> script asks `GetNodeOrNull` for its contract node and builds a stand-in when the answer is nothing — so a
> typo means the fallback runs, your authored node sits unused, and **the game looks exactly as it does
> now.** No error, no refusal, nothing red.
>
> **C-lane, cheap, and worth doing before you open the editor on any of the three:** a check that reddens
> when a `.tscn` exists and the fallback ran anyway. Ask for it and it takes ten minutes.

### C2. The `Dim` and layout traps, for whoever authors those scenes
Recorded here because they each cost a unit already: `SetAnchorsPreset` does **not** mean *fill your
parent* — it rewrites offsets to preserve the node's current rect, which for a fresh `ColorRect` is 0×0.
Minimum-size propagation beats fixed offsets, which is how the journal's ✕ ended up pushed off the bottom
of a 480px box by 1014px of content. And `Size` means nothing in a headless run, so gates assert anchors
**and** offsets — a half-asserted rect is how a dim sat invisible for a whole unit.

---

## Next — after the above

### C3. Chapter 1 sealed
`--verify-content`'s ch1 block turns from a report into gates. **Blocked on:** chapter 1 being finished
and played, which is J1's chapter-1 equivalent plus at least one 3D pocket.

### C4. The pursuer, tuned
Pure BFS, never tested against a player, and it only runs inside boss pockets now. `combat.md` §15's
*"level mechanics that slow the pursuer down"* is a better answer than tuning the BFS.
**Blocked on:** J2, since there is nowhere to be pursued yet.

### C5. The catch volume — **specified 2026-08-07, deliberately not built**
An `Area3D` under a pocket that returns you to the entrance instead of letting you fall forever. Offered and
**deferred by Jeremy on the day it was offered**: *"document and we will use it later on when more testing
happens"* — which is the right call, because what it should do when it fires is a question play answers and
design does not. Written down now so the shape does not have to be rediscovered.

| | |
|---|---|
| **What it is** | one `Area3D` per pocket, sized to the whole playable volume plus a margin, `BodyEntered` → put the player back |
| **Where it goes back to** | the pocket's own spawn point, **not** a camp and **not** an ejection. Falling out is a seam in the geometry, not a failure of nerve — charging Resolve for it would price a bug |
| **What it must not do** | write anything. `Leave()` and `Eject()` both already write nothing; this must not become a third way out with different rules |
| **Why a volume and not thicker walls** | thickness stops tunnelling, which `CharacterBody3D` at 2.2 m/s does not do anyway. What actually leaks is *seams* — two boxes meeting on the same coordinate — and a volume catches the ones you have not found |
| **The one it also catches** | Float has no ceiling in an open-plan pocket. A cap is invisible collision (a `StaticBody3D` with a `WorldBoundaryShape3D`, no mesh), which is a different node and a different job — see the ceiling note below |

**Do not build it before there is something to fall out of.** It is ten minutes against a real pocket and a
guess against none, and its one interesting decision — *what does the player see when it fires* — cannot be
made from a headless run.

### J3. The remaining thirteen pocket scenes
Clone from pocket 1 once it plays. **Blocked on:** J2.

### J4. Her fourth note
One `.tres` in `data/items/`, a message from Iris to Daniel. Its hotspot already stands in `ch3_breach`.
The id must **not** begin `lore_`, or the decision-11 crossing will accuse it of teaching a family that
does not exist. It teaches nothing — settled 2026-08-05. **This is the last item on the content bill**
(`items` reads 37 / 38), and it is pure prose, so it belongs with the story pass below.

---

## Last — the story sweep, and it now owns the ending

Story, narration and tutorial content sit behind everything above, beside audio (ruled 2026-08-04).
An `[Export]` with a default does not invalidate existing `.tres` files, so **none of these need
building early** — the field can arrive with the thing that reads it.

### C1. The ending — **deferred into this pass on Jeremy's call, 2026-08-07**
It was the top of the C lane and it is not urgent: *"C1 and story should be deferred until we do the
story sweep."* The reasoning holds up — an ending is the one system whose shape is decided by its
writing, and building the machine first means building it against a guess at what the last scene says.

What is missing is everything: no `EndingManager`, nothing that detects the good ending, no finale
sequence, no win or lose screen. Iris can be beaten and the exorcism engine can be built and the game
simply continues. `combat.md` §20 has the three endings ratified in full, so the *rules* are settled and
only the prose and the machine are outstanding.

**Done when:** reaching the finale resolves to one of §20's endings and says so on screen.
**Do not let it slip past this pass** — it is the only item here that the game cannot ship without, and
the rest of this section is polish that a player would merely notice.

- **The words.** Every `[unwritten]` in `data/` — examine text, spirit names, the freed-NPC lines.
- **`RecipeData.CraftNarration`**, plus somewhere to draw it.
- **The equip tutorial** — the deliberately unequipped starting axe exists to be its subject.
- **Audio.** Constrains how Dream hazards read, which is why it is last rather than never.
- **Title screen, pause, options.** None exist. Not needed to finish the game; needed to ship it.

---

## Open decisions — each is one sentence from Jeremy, and each unblocks work

1. **Ash Incense.** A catalyst item with no source, no reader and, since severing became boss-only, no
   purpose. Cut it, or give it a job.
2. **`F6` learn-all.** Kept deliberately (*"a nice debug for testing"*), but it opens all sixteen gated
   recipes at once, so any session that touches it stops being evidence about pacing. No action needed —
   recorded so it is not mistaken for an oversight.

---

## The standing risk, which no amount of tooling has changed

**The gates prove structure. They have never proved playability.** Every bug that has mattered was found
by playing: the journal that would not close, the resurrected Trapper, the bench pointing at a dead key,
the villagers nobody could free, the cart nobody could afford, the coat you could forge twice. Four
sessions, six bugs, all of them sitting inside a green suite.

**The sixth is the sharpest example yet, because a gate was actively defending it.** `--verify-slice`
asserted *"a spare is allowed — the cache exists partly to bank one"*, and the ruling it cited had been
struck five days earlier. The suite was not silent about the coat; it was **wrong** about it, in writing,
in green. So the risk is not only that gates miss things — it is that a gate outlives the decision it
encodes, and nothing in the suite can notice that.

So the rule that belongs at the bottom of this file: **play the unit in the sitting that builds it.**
Chapter 3 has never been played at all.
