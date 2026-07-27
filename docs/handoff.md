# Handoff — where we left off

Last updated: **2026-07-27.** Read this first, then `docs/combat.md`.

That session was mostly design, plus three code changes — including the fix for what was §6.1's
one open bug, so **nothing is currently known to be broken**. **`docs/combat.md` gained three new
sections (15–17) and one deliberate revision to Section 6.** If you only read one thing before
starting work, read those.

---

## 1. Run it in one minute

```sh
GODOT="C:/Users/Owner/Desktop/Game Dev Tools/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"

dotnet build                                    # must be clean, 0 warnings
"$GODOT" --headless --path . -- --verify-slice   # 99 checks, exit 0 = nothing broke
"$GODOT" --path .                                # play it
"$GODOT" --path . -- --shots                     # capture the whole loop to .shots/
```

**Run `--verify-slice` before and after any change.** It drives the entire game headlessly and is
the fastest way to find out whether something regressed.

> **But green gates do not mean playable.** See §5. Two separate classes of bug have walked
> straight through a fully green run: one hiding in a **state the verifier normalises** (it
> switches off the Resolve clock and clears any Dream run), and one hiding in a **layer the
> verifier never questioned** (it asserted manager state and never asked the view what it was
> drawing). Both bit on 2026-07-27; both are written up in §6.1 and §3.

---

## 2. What actually works right now

The full slice, end to end, with no art:

```
[WAKING]  search the cairn  →  the relic is exposed  →  touch it
[DREAM]   six-room maze, Resolve draining, the Mind-Stalker hunting you
          →  reach the Baron's solar, take the anchor
[WAKING]  ejected, Resolve refilled. the rat is Severed — grey, slumped, road clear
          →  finish it  →  zone cleanses, sun comes out, bitterroot appears to harvest
```

Plus: spectral vision toggle, room navigation, flag-gated hotspots, harvest-once nodes,
zero-respawn persistence, JSON save/load, a debug panel (`F3`) and procedural placeholder art for
every room and creature.

**Content is 2 Waking rooms, 6 Dream rooms, 1 entity, 1 heist, 5 items, 1 zone** — all `.tres`
under `data/`, all editable in the Godot inspector.

---

## 3. What changed on 2026-07-27

### Code

- **`Sanity` is now `Resolve` everywhere.** `SanityManager` → `ResolveManager`, `SanityMeter` →
  `ResolveMeter`, `GameConfig.SanityMax` → `ResolveMax`, the `EventBus.SanityChanged` signal and
  its emitter → `ResolveChanged` / `EmitResolveChanged`, `ItemData.SanityRestore` →
  `ResolveRestore`, `SaveGame.Sanity` → `Resolve`. Only one `.tres` referenced the renamed
  property, so it was hand-edited rather than reseeded — no `--seed-content` was run and no
  inspector edits were at risk. The save JSON key changed, so pre-rename saves fall back to the
  100f default.
- **Fixed a save/load soft lock.** `DreamRunManager.ForceClear()`'s doc comment claimed it was
  "used by save/load"; it never was — `src/Save/` had zero references to `DreamRunManager`. So a
  save taken mid-heist restored the Dream room and realm with no run, and because **no Dream room
  has a door back to the Waking world**, that state was unrecoverable. `SaveService.Load` now
  clears any stale run and, if the restored room belongs to a heist maze, lands you at that
  heist's `ReturnRoomId` in the Waking realm instead. New: `DreamRunManager.HeistOwningRoom()` /
  `IsDreamRoom()`, derived by walking the Navigate graph out from each registered start room — no
  new data field, same rule the pursuer already follows.
- **8 new gates** (84 → 92) covering that soft lock. They were verified to fail without the fix,
  not merely to pass with it.
- **§6.1's ejection failure is found and fixed** — it was a stale signal payload in the *view*,
  not an exception in `End()`. Full account in §6.1 below. `RoomView.OnRoomEntered` and
  `Hud.OnRoomEntered` now ignore a `roomId` that is no longer `NavigationManager.CurrentRoomId`;
  both expose what they are displaying (`DisplayedRoomId` / `DisplayedRoomName`) and
  `SliceVerifier.Run` takes the live view and HUD so gates can assert what is *on screen*.
  **5 new gates** (92 → 97), two of which were verified to fail before the fix, plus 2 harness
  checks asserting those views actually reach the verifier (97 → 99). Written up as invariant 13
  and in the README, because the trap is general and M4 adds three more bus subscribers.

### Design — see `combat.md` §15–17 for the real text

- The Dream's purpose is no longer only "weaken three bosses." **The demon scrambled your own
  craft knowledge, and you go in to reclaim it.** Mandatory for progression; severing bosses stays
  optional.
- **Only knowledge crosses over.** Traversal shortcuts and supply-harvesting were both considered
  and rejected, with reasons recorded in §11 so they are not re-derived.
- The Dream is **closed-off pockets**, each with its own Waking entrance.
- **Chases only in boss pockets.** Elsewhere, mindless creatures whose contact costs Resolve.
- **Observation puzzles**, in both realms. Spatial-reconfiguration puzzles rejected as a new system.
- **Monochrome Dream**, authored greyscale and dithered in the shader.
- **2.5D is the plan** — the "is being hunted frightening?" gate has been played and cleared.
- **Camps** replace a central workshop, and absorb §6's old "sanctuary."
- **Crafting is Zelda-style tools**, not power tiers, so invariant 5 survives intact.

---

## 4. Start here next session

There are no known open bugs. Go straight to M4.

### M4 — inventory and crafting

Still the right next step, because everything else is blocked on it: traps need materials and a
build UI, catalysts need an inventory, her cache needs somewhere to live, blueprints need to be
collectible.

What exists already: `InventoryCraftingManager` holds stacks and an item catalogue and emits
signals. `ItemData` has categories, icons, stack sizes, `ResolveRestore`. Nothing has a UI.

What M4 needs:

- `RecipeData` resource (inputs → output), currently not written at all
- Crafting resolution in `InventoryCraftingManager`
- An inventory grid and a crafting panel
- Her cache as a real place with finite named salves in it
- Using an item (salves restore Vitality; catalysts are a Dream loadout)

**Two decisions already made that affect how you build it:**

1. **Author the new panels as `.tscn` scenes**, not in code. `Hud.cs` stays as it is — it works and
   its layout never changes. But grids, slots and hover states are layout you need to see while
   tuning, and there are three panels coming. Rules: scenes own layout, scripts own behaviour;
   reference nodes with `%UniqueName` or an `[Export] NodePath`, never `GetNode("A/B/C")` string
   paths; make the shared theme `.tres` on the *first* panel, not the third; and keep panels
   rendering from `ItemData.PlaceholderColor` so nothing depends on icon art existing.
2. **Keep the logic in the managers.** `--verify-slice` works because it drives
   `InteractionService` and the managers directly, and asserts against *them*. Crafting
   resolution belongs in `InventoryCraftingManager`, with the panel a thin view over it. Put
   recipe logic in a panel script and it becomes untestable headlessly.

   Note the correction: the verifier does *not* skip the UI — `Main` builds `RoomView` and `Hud`
   before calling it, and both are live and reacting to every signal in a headless run. What it
   lacked was any gate that asked them what they were showing, which is how §6.1 hid. So when you
   add the M4 panels, hand them to `SliceVerifier.Run` the same way and assert the state they
   display, not only the manager state behind them.

**Build the discovery gate from day one.** Recipes should be gated on discovered blueprints
(`ItemCategory.Lore`) even if every blueprint is granted at boot for testing. Retrofitting that
later means reopening both the crafting resolution and its UI; building it now makes the journal
purely additive.

### Also unbuilt and easy to forget: Vitality

Designed in full in `combat.md` §6, zero lines of code. Camps, her salves, protected items and the
0-Vitality consequence all route through it. Worth knowing plainly: **with only Resolve
implemented, the two routes in §1 currently cost the same thing**, so nothing about the two-route
economy has actually been exercised yet.

---

## 5. Placeholders and traps for the unwary

Things that *work* but are not the real implementation, or that will mislead you:

| Where | What |
|---|---|
| `SliceVerifier.Run(view, hud)` | Sets `ResolveManager.Active = false` so drain cannot race the assertions. **Nothing timing- or drain-related is tested.** Both arguments are null-tolerant so the gates stay runnable from anywhere — but `GateHarness` asserts they arrived, so a wiring mistake in `Main` fails the run instead of silently skipping the five view checks. |
| `GateHeistMechanics` | Calls `run.ForceClear()` before `GatePersistence`, so save/load is only tested from a Waking room with no run. This is exactly where the soft lock hid. |
| headless runs | `_Process` never ticks meaningfully, so the pursuer only moves when a test calls `DebugSetPursuerRoom`. **Pursuit timing is untested.** |
| the view, in gates | `RoomView` and `Hud` *are* live in a headless run and now have gates asserting the room they display — but only the room. Backdrops, overlays, tweens and hotspot rects are still unasserted, and `--shots` is the only thing that looks at a pixel. |
| `ContentSeeder.cs:219` | The entry puzzle is a plain `Interact` hotspot ("search the cairn"). The minigame does not exist. |
| `InteractionService.cs:136` | Finishing a `Severed` body is **one click**. This is where the turn-based item fight goes. |
| Everywhere | **Vitality does not exist.** |
| `ItemData.ResolveRestore` | **Misnamed, unread, and contradicts `combat.md` §6.** §6 restores Resolve only by leaving the Dream or resting at a camp — never from an item — and salves restore **Vitality**. The field is a mechanical leftover from the `SanityRestore` rename; `salve_clearmind` stores 35 and nothing reads it. **M4 must settle this before wiring item use:** rename to `VitalityRestore`, or decide some items really do restore Resolve. |
| Everywhere | **No audio at all.** Deliberately last — but see the note in `combat.md` §14. |
| Everywhere | Craft / Physick / Attunement are designed, unbuilt. |
| Everywhere | **Defence does not exist as a field.** Decided 2026-07-27 to be fixed per unit and unaffected by severing (`combat.md` §2). `EntityData` has no such property yet — do not reach for `CorruptionAura`, which carries regen and the Resolve drain instead. |
| Everywhere | Traps are designed in detail, unbuilt. Blocked on M4. |
| `RoomData.BaseCorruption` | **Not vestigial — an earlier entry here said it was, and that was wrong.** It no longer drives any drain (Waking rooms do not drain, Dream rates are per-heist), but `RoomView.cs:187` reads it as the placeholder backdrop's **fog density**: `fog: 0.35f + BaseCorruption * 0.65f`. Deleting it would visibly change every Waking room. Treat it as a look knob, not dead weight. |
| `RoomData.IsSanctuary` | Unused, but **do not delete** — camps (`combat.md` §17) will likely repurpose it. |
| `PlaceholderBackdrop`, `EntityOverlay` | All art is procedural. Every texture field is nullable and unset. |

**When a bug reproduces in play but the gates are green, there are two things to suspect, in this
order.** First, a **state the verifier normalises** — an active run, a live clock, a mid-sequence
save. Second, a **layer it never questions**: everything downstream of the manager state the gates
assert. Do not assume the logic is fine because the assertions are; check that the assertions are
pointed at the thing that is actually broken. One bug of each class has shipped and been fixed
(§3), and in both cases the gates were green and correct about what they measured.

---

## 6. One closed post-mortem, then the open questions

Nothing in **§6.1 is open** — it is kept, at its original number, because `RoomView.cs` and
`SliceVerifier.cs` both cite "handoff.md 6.1" in comments, and because the way it hid is more
useful than the bug. Everything genuinely unresolved is in **§6.2**.

### 6.1 The ejection failure — **CLOSED 2026-07-27** (was: "unexplained, cause unknown")

Reported in play: reached the tether, Resolve stopped draining, and could not leave the Dream.

**It was never in `End()`.** The leading hypothesis — an exception between `ActiveHeist = null`
and `GoToRoom` — was wrong. Every manager was correct the whole time; the *view* was showing a
room the player had already been pulled out of.

The pursuer starts in the solar, so walking into it is the most likely thing to happen on a fast
run. Doing so calls `DreamRunManager.NotifyPlayerMoved` — which is subscribed to `RoomEntered` —
and that captures you and ejects you **from inside that emit**. So:

1. `GoToRoom("dream_way_solar")` sets `CurrentRoomId`, emits `RoomEntered("dream_way_solar")`.
2. `DreamRunManager` is first in the subscriber list (autoload), sees the pursuer, calls `End()`.
3. `End()` navigates to `waystation_yard` — a **nested** `GoToRoom`, which emits and completes
   its own `RoomEntered`. `RoomView` and `Hud` handle it and correctly show the yard.
4. The outer emit resumes, and *now* reaches `RoomView` with the stale `"dream_way_solar"`.
   The view repaints the Dream room over the top.

Result: no drain (the run really is over), the Dream room still on screen, and — because the
realm is now Waking while the room is a Dream room whose hotspots are all `DreamOnly` — **not one
clickable thing anywhere.** The HUD confirmed it by still naming the Baron's Solar.

The fix is a stale-payload guard in both subscribers — `RoomView.OnRoomEntered` and
`Hud.OnRoomEntered` return early when the payload is no longer `NavigationManager.CurrentRoomId`.
Stated as invariant 13 in §7 and in the README under "Signal payloads can arrive stale," because
the trap belongs to the bus, not to these two files.

**Why nothing caught it:** `GateHeistMechanics` already walked into the pursuer and asserted
`CurrentRoomId == "waystation_yard"` — and that assertion was *true*. The gates had never asked
the view what it was drawing. They do now, in five places: `RoomView.DisplayedRoomId` is asserted
after an ordinary door, after the capture ejection, after the success ejection and after the
mid-heist load, and `Hud.DisplayedRoomName` is asserted after the capture ejection.

The ordinary-door one is the control, and it matters: without it, "the view never shows the wrong
room" would also be satisfied by a view that draws *nothing*. It passed before the fix and after.
The other two new gates — the capture ejection and the HUD — were watched to fail on the unfixed
build (`2 FAILURE(S) of 95 checks`) and pass on the fixed one. The remaining two, the success
ejection and the load, are not nested and so passed either way; they are regression cover, not
evidence.

> The general lesson, worth more than the bug: when play and the gates disagree, suspect the
> boundary the gates do not cross. Last time that was a state the verifier normalises (§5); this
> time it was a layer the verifier never questioned.

### 6.2 Still open, in the order they'll bite

1. **Trap placement** — authored sites per room, or free-form on a grid? Authored is far less work
   and probably reads better on painted backgrounds. Nothing blocks on it.
2. **Is the pure-BFS pursuer fair?** Never tested against a real player. Much smaller problem now
   that it only runs inside boss pockets.
3. **The entry puzzle** is a placeholder, and is now the first real instance of Waking-world puzzle
   content — so it sets the grammar for every puzzle after it.
4. **The journal** must land before the first real observation puzzle, though it comes after
   crafting. See the callout at the end of `combat.md` §15 for why.
5. **The item, trap and equipment-type vocabulary** — to be discussed before crafting is built.
   `combat.md` §4's trap table and §7's type sketch are both explicitly **unratified**: they were
   written in a design pass, not decided, and one row (the salt line) was struck on 2026-07-27
   for contradicting the tether rule. Nothing in them blocks M4's crafting *plumbing*, which is
   content-agnostic — but do not implement that table as though it were a spec.

---

## 7. Invariants — break these and the design collapses

Learned the hard way over several design passes. Each one is load-bearing:

1. **The two routes must cost different currencies.** Sever costs Resolve, time and a catalyst;
   overkill costs finite supplies. If they ever cost the same thing, the choice becomes a
   difficulty slider. This is the reason for most of the rules below.
2. **Nothing in the Dream can kill you.** Every ending is an ejection; only the price differs.
   Mindless creatures cost Resolve, bosses eject — and **Vitality must never appear in the Dream**,
   or this collapses along with the two-pool split.
3. **The anchor→body loop runs one way only.** Severing weakens the body. Wounding the body does
   *not* weaken the spirit. (Proposed and explicitly rejected.)
4. **Spectral vision is a look, not a place.** `Tab` shows Form 2 and the aura; you cannot sever
   through it. Otherwise the relic and the whole heist are pointless.
5. **No stat multiplies damage.** All progression gates *access*. Out-statting the horror is what
   kills horror. Crafting grants variety and access — tools, not tiers.
6. **Fights run 3–6 turns.** Overkill must be expensive, never long.
7. **Her named salves are finite and uncraftable.** One cache, forever. Crude substitutes only —
   but they must be *sufficient* to win, or failure quietly deletes the overkill route. Salves
   survive death; the only way to lose them is to spend them.
8. **Views never branch on entity state.** They ask `EntityStateManager.Resolve`, which is the
   single place that knows the Form 1/2/3 mapping.
9. **Nothing material leaves the Dream.** Knowledge, perception, Attunement and state only.
10. **Keep game logic in managers, not in UI.** It is what makes `--verify-slice` able to test the
    whole game headlessly, and that harness is the project's best asset.
11. **Severing is the only thing that removes regeneration or the Resolve drain.** No trap, item,
    ward or consumable does either, in whole or in part. A cheap Waking-world substitute for the
    anchor's effects — a "poor man's sever" — turns the Dream from *the* way you weaken a boss
    into a convenience, and takes invariant 1 with it. `combat.md` §4 records the salt line being
    struck for exactly this.
12. **Defence is fixed per unit, and moderate.** Severing does not change it; the right equipment
    type is meant to be clearly *better*, never the only thing that functions. Absolutes read as
    locks and invite key-hunting, and they turn a missing answer into a dead save. `combat.md` §7.
13. **Never trust a signal payload as current state.** `EventBus` emission is synchronous and in
    subscription order, so a subscriber ahead of you can act on the signal — including navigating
    again — before you are reached, which delivers the newer event first and leaves the stale one
    to land last. Anything caching per-signal state must re-check the manager
    (`CurrentRoomId != roomId → return`). This is a code rule, not a design one, but it earns its
    place here: breaking it is what produced §6.1, and M4 adds three more bus subscribers.

> **Revised 2026-07-27:** the old rule *"Resolve never moves in the Waking world"* is gone.
> Resolve now drains in boss fights, but **only while the boss's tether is unbroken** — sever it
> and the same fight costs none (`Corrupted` drains, `Severed` does not). The bar stays unambiguous because empty
> means the same thing in both realms — *you are removed from the confrontation.* See `combat.md`
> §6.

---

## 8. Where things live

| | |
|---|---|
| `README.md` | architecture, commands, the state × realm table, and the EventBus stale-payload contract (invariant 13) |
| `docs/combat.md` | **source of truth** for combat, Resolve, the Dream, presentation and camps. Supersedes the GDD in several places. §15–17 are the newest. |
| `docs/handoff.md` | this file |
| `src/Tools/SliceVerifier.cs` | what "working" means, as executable assertions — and see §5 for what it does *not* cover |
| `src/Tools/ContentSeeder.cs` | regenerates `data/*.tres`. **Overwrites** — stop running it once you edit rooms in the inspector. |

### Superseded parts of the GDD

Do not re-derive these from the design doc:

- The **Snap duel** is deleted. Resolve-empty and being-touched are the same ejection.
- **Sanity** is gone as both a concept and a name; it is **Resolve**, and Vitality is separate.
- The Pixel Realm is **stealth and theft**, not a tactical duel.
- **"1st-Person"** means Myst-style perspective in the Waking world. The Dream is going 2.5D.
