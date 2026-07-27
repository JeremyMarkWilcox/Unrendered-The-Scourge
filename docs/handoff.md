# Handoff — where we left off

Last updated: **2026-07-26.** Read this first, then `docs/combat.md`.

---

## 1. Run it in one minute

```sh
GODOT="C:/Users/Owner/Desktop/Game Dev Tools/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"

dotnet build                                    # must be clean, 0 warnings
"$GODOT" --headless --path . -- --verify-slice  # 84 checks, exit 0 = nothing broke
"$GODOT" --path .                               # play it
"$GODOT" --path . -- --shots                    # capture the whole loop to .shots/
```

**Run `--verify-slice` before and after any change.** It drives the entire game headlessly
and is the fastest way to find out whether something regressed. `--shots` exists so the
*look* of a change can be checked without playing through — both were worth more than they
cost.

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
zero-respawn persistence, JSON save/load, a debug panel (`F3`) and procedural placeholder art
for every room and creature.

**Content is 2 Waking rooms, 6 Dream rooms, 1 entity, 1 heist, 5 items, 1 zone** — all
`.tres` under `data/`, all editable in the Godot inspector.

---

## 3. Start here next session

### M4 — inventory and crafting

It is the only sensible next step, because **everything else is blocked on it**: traps need
materials and a build UI, catalysts need an inventory, her cache needs somewhere to live,
blueprints need to be collectible.

What exists already: `InventoryCraftingManager` holds stacks and an item catalogue and emits
signals. `ItemData` has categories, icons, stack sizes, `SanityRestore`. Nothing has a UI.

What M4 needs:

- `RecipeData` resource (inputs → output), currently not written at all
- Crafting resolution in `InventoryCraftingManager`
- An inventory grid and a crafting panel
- Her cache as a real place with finite named salves in it
- Using an item (salves restore Resolve; catalysts are a Dream loadout)

**Note the HUD is built in code, not as a scene** (`src/UI/Hud.cs` → `BuildUi()`). That was
fine for placeholder furniture; M4 is the point to decide whether to keep going that way or
author real scenes.

---

## 4. Placeholders that are lying to you

Things that *work* but are not the real implementation. Do not mistake these for finished:

| Where | What |
|---|---|
| `ContentSeeder.cs:219` | The entry puzzle is a plain `Interact` hotspot ("search the cairn"). The minigame does not exist. |
| `InteractionService.cs:136` | Finishing a `Severed` body is **one click**. This is where the turn-based item fight goes. |
| Everywhere | **Vitality does not exist.** Two pools are designed; only Resolve is built. |
| Everywhere | **No audio at all.** Stealth without sound cues is thin — this is the biggest single gap in the heist. |
| Everywhere | Craft / Physick / Attunement proficiencies are designed, unbuilt. |
| Everywhere | Traps are designed in detail, unbuilt. Blocked on M4. |
| `PlaceholderBackdrop`, `EntityOverlay` | All art is procedural. Every texture field is nullable and unset. |

---

## 5. Invariants — break these and the design collapses

Learned the hard way over several design passes. Each one is load-bearing:

1. **The two routes must cost different currencies.** Sever costs Resolve and time; overkill
   costs finite supplies. If they ever cost the same thing, the choice becomes a difficulty
   slider. This is the reason for most of the rules below.
2. **Nothing in the Dream can kill you.** Every heist ending is an ejection; only the price
   differs. A hard fail state turns the Dream into a skill gate, which stops being comparable
   to spending supplies.
3. **The anchor→body loop runs one way only.** Severing weakens the body. Wounding the body
   does *not* weaken the spirit. (Proposed and explicitly rejected.)
4. **Spectral vision is a look, not a place.** `Tab` shows Form 2 and the aura; you cannot
   sever through it. Otherwise the relic and the whole heist are pointless.
5. **No stat multiplies damage.** All progression gates *access*. Out-statting the horror is
   what kills horror.
6. **Fights run 3–6 turns.** Overkill must be expensive, never long.
7. **Her named salves are finite.** One cache, forever. Crude substitutes only.
8. **Views never branch on entity state.** They ask `EntityStateManager.Resolve`, which is the
   single place that knows the Form 1/2/3 mapping.

---

## 6. Open questions, in the order they'll bite

1. **Failing a heist currently costs nothing** (Resolve refills on return). Undercuts
   invariant #1. Proposed fix: *crossing consumes a catalyst.* Needs M4. See the
   "Consequence, unresolved" callout in `docs/combat.md` §6.
2. **Audio.** Nothing exists. The heist needs it more than it needs anything else.
3. **Is the pure-BFS pursuer fair?** Never tested against a real player. It may need
   softening, or a longer move interval.
4. **Ambient corruption has no mechanical home** now that Resolve doesn't drain outside the
   Dream. The GDD's "exposure to corrupted zones drains Sanity" is deliberately unimplemented.
   Fine if it stays atmosphere — worth a decision.
5. **Does body damage persist between engagements once severed?** Only open question that
   touches the save format. Decide before building Waking combat.
6. **2.5D Dream** — deferred, not rejected. Node stealth exists to answer *"is being hunted
   frightening?"* cheaply first. If yes at this fidelity, the months are worth spending; the
   `SubViewport` the Dream already renders through could host a 3D subscene.

---

## 7. Where things live

| | |
|---|---|
| `README.md` | architecture, commands, the state × realm table |
| `docs/combat.md` | **source of truth** for combat, Resolve, traps, progression. Supersedes the GDD in several places. |
| `docs/handoff.md` | this file |
| `src/Tools/SliceVerifier.cs` | what "working" means, as executable assertions |
| `src/Tools/ContentSeeder.cs` | regenerates `data/*.tres`. **Overwrites** — stop running it once you edit rooms in the inspector. |

### Superseded parts of the GDD

Do not re-derive these from the design doc:

- The **Snap duel** is deleted. Resolve-empty and being-touched are the same ejection.
- **Sanity as a universal health pool** was proposed and retracted; it is Resolve, a heist
  clock, and Vitality will be separate.
- The Pixel Realm is a **stealth heist**, not a tactical duel.
- **"1st-Person"** means Myst-style perspective, not a 3D camera.
