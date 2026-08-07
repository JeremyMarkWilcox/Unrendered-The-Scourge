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

## Now — the two lanes run in parallel

Jeremy authors in the editor and plays; Claude writes managers, gates and derivable content. They touch
different files, so nothing below is blocked on the other lane unless it says so.

### J1. Play chapter 2 end to end — **unblocked, do this first**
The villagers wake up now, so this path has never been walked with the pieces working. What to watch:
her Burning Oil and Corrosive Draught notes coming off the Apothecary and the Still Room; the Oil Slick
and Vitriol Flask becoming buildable for the first time; the cart affordable with the full kit built.
**Done when:** you have crossed into chapter 3 with the kit and no missing materials.

### J2. Pocket 1 in 3D, then play it — **the largest thing left, and it unblocks the most**
Zero of fourteen scenes exist. The checklist is in `handoff.md` (*"What a pocket needs"*) and it needs no
scripting — `PocketHost` attaches the verb poller, `PocketAnchor` finishes, `PocketExit` leaves.
**Build one, play it, then clone.** Cloning first is how fourteen scenes drift identically.
**Done when:** one `PocketData` names a real scene and you can walk in, reach the anchor and come out.
**Unblocks:** the whole Dream half, and it retires the stand-in path for that pocket alone.

> **Carries a hidden second job: `DreamCreatureData` — and there are currently zero of them.** The system
> is built (`DreamCreature.cs`, `DreamCreatureData.cs`, contact cost, pursuit-while-lit, `Banishes`) and
> **`data/` has no creature file for it at all**, so a pocket with creature nodes in it has nothing to
> point them at. There is no directory yet either. The **cooldown** is the field that decides everything —
> without it one touch empties the meter in a few frames. Minor creatures cost Resolve on contact and
> pursue only while your light is on; a boss's `Banishes` throws you out.

### C1. The ending — **nothing in the game can end it, and this is the biggest hole**
There is no ending system of any kind: no `EndingManager`, nothing that detects the good ending, no
finale sequence, no win or lose screen. Iris can be beaten and the exorcism engine can be built and the
game simply continues. `combat.md` §20 has the three endings ratified in full.
**Done when:** reaching the finale resolves to one of §20's endings and says so on screen.
**Blocked on:** nothing. This is the next thing Claude should build.

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

### J3. The remaining thirteen pocket scenes
Clone from pocket 1 once it plays. **Blocked on:** J2.

### J4. Her fourth note
One `.tres` in `data/items/`, a message from Iris to Daniel. Its hotspot already stands in `ch3_breach`.
The id must **not** begin `lore_`, or the decision-11 crossing will accuse it of teaching a family that
does not exist. It teaches nothing — settled 2026-08-05. **This is the last item on the content bill**
(`items` reads 37 / 38), and it is pure prose, so it belongs with the story pass below.

---

## Last, and ruled to be last

Story, narration and tutorial content sit behind everything above, beside audio (ruled 2026-08-04).
An `[Export]` with a default does not invalidate existing `.tres` files, so **none of these need
building early** — the field can arrive with the thing that reads it.

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
the villagers nobody could free, the cart nobody could afford. Four sessions, five bugs, all of them
sitting inside a green suite.

So the rule that belongs at the bottom of this file: **play the unit in the sitting that builds it.**
Chapter 3 has never been played at all.
