# Handoff — where we left off

Last updated: **2026-07-27.** Read this first, then `docs/combat.md`.

That session was almost entirely design, plus two code changes. **`docs/combat.md` gained three new
sections (15–17) and one deliberate revision to Section 6.** If you only read one thing before
starting work, read those.

---

## 1. Run it in one minute

```sh
GODOT="C:/Users/Owner/Desktop/Game Dev Tools/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"

dotnet build                                    # must be clean, 0 warnings
"$GODOT" --headless --path . -- --verify-slice   # 92 checks, exit 0 = nothing broke
"$GODOT" --path .                                # play it
"$GODOT" --path . -- --shots                     # capture the whole loop to .shots/
```

**Run `--verify-slice` before and after any change.** It drives the entire game headlessly and is
the fastest way to find out whether something regressed.

> **But green gates do not mean playable.** See §5. The verifier deliberately switches off the
> Resolve clock and clears any Dream run, so a whole class of bug passes straight through it. This
> bit us this session.

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

## 3. What changed last session

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

### First: reproduce the open bug (§6.1). It is five minutes and it is the only unknown.

### Then: M4 — inventory and crafting

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
2. **Keep the logic in the managers.** `--verify-slice` works because it drives `InteractionService`
   and the managers directly and never instantiates UI. Crafting resolution belongs in
   `InventoryCraftingManager`, with the panel a thin view over it. Put recipe logic in a panel
   script and it becomes untestable headlessly.

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
| `SliceVerifier.Run()` | Sets `ResolveManager.Active = false` so drain cannot race the assertions. **Nothing timing- or drain-related is tested.** |
| `GateHeistMechanics` | Calls `run.ForceClear()` before `GatePersistence`, so save/load is only tested from a Waking room with no run. This is exactly where the soft lock hid. |
| headless runs | `_Process` never ticks meaningfully, so the pursuer only moves when a test calls `DebugSetPursuerRoom`. **Pursuit timing is untested.** |
| `ContentSeeder.cs:219` | The entry puzzle is a plain `Interact` hotspot ("search the cairn"). The minigame does not exist. |
| `InteractionService.cs:136` | Finishing a `Severed` body is **one click**. This is where the turn-based item fight goes. |
| Everywhere | **Vitality does not exist.** |
| Everywhere | **No audio at all.** Deliberately last — but see the note in `combat.md` §14. |
| Everywhere | Craft / Physick / Attunement are designed, unbuilt. |
| Everywhere | Traps are designed in detail, unbuilt. Blocked on M4. |
| `RoomData.BaseCorruption` | Vestigial. Nothing uses it: Waking rooms do not drain and Dream rates are per-heist. |
| `RoomData.IsSanctuary` | Unused, but **do not delete** — camps (`combat.md` §17) will likely repurpose it. |
| `PlaceholderBackdrop`, `EntityOverlay` | All art is procedural. Every texture field is nullable and unset. |

**When a bug reproduces in play but the gates are green, suspect a state the verifier normalises**
— an active run, a live clock, a mid-sequence save — rather than assuming the logic is fine.

---

## 6. Open questions, in the order they'll bite

### 6.1 The unexplained ejection failure — **live bug, cause unknown**

Reported in play: reached the tether, Resolve stopped draining, and could not leave the Dream.

The save/load soft lock fixed in §3 produces *exactly* those symptoms, but it is **not the cause
here** — no save or load was used in that session. So there is a second route into "in a Dream room
with no live run" that has not been found.

Leading hypothesis, unverified: an exception thrown inside `DreamRunManager.End()` *after*
`ActiveHeist = null` but before `GoToRoom`, most likely in `SetRealm`. That would leave the run
dead (hence no drain), the player still in the Dream room, and the realm never switched. Anything
throwing *before* `End()` would leave the run live and still draining, so the window is narrow.

**To diagnose:** reproduce, then press **F3**. The debug panel prints room, realm and
`resolve X/Y rate Z`.

- `rate 0.00` → the run is dead; it is the `End()` window. Check the Godot console for the
  exception.
- `rate` non-zero → the run is live and this is a *display* problem in `ResolveMeter` instead,
  which the headless gates would never catch.

### 6.2 The rest

1. **Trap placement** — authored sites per room, or free-form on a grid? Authored is far less work
   and probably reads better on painted backgrounds. Nothing blocks on it.
2. **Is the pure-BFS pursuer fair?** Never tested against a real player. Much smaller problem now
   that it only runs inside boss pockets.
3. **The entry puzzle** is a placeholder, and is now the first real instance of Waking-world puzzle
   content — so it sets the grammar for every puzzle after it.
4. **The journal** must land before the first real observation puzzle, though it comes after
   crafting. See the callout at the end of `combat.md` §15 for why.

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

> **Revised this session:** the old rule *"Resolve never moves in the Waking world"* is gone.
> Resolve now drains in boss fights against an intact aura. The bar stays unambiguous because empty
> means the same thing in both realms — *you are removed from the confrontation.* See `combat.md`
> §6.

---

## 8. Where things live

| | |
|---|---|
| `README.md` | architecture, commands, the state × realm table |
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
