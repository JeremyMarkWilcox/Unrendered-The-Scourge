# Unrendered: The Scourge

Godot 4.7.1 (mono) / C#. A dual-realm survival-horror RPG: a cold high-res **Waking** world
and a pixelated **Dream** realm, where severing a spirit's anchor in the Dream deforms its
physical host in reality.

Presentation is Myst-style — first-person perspective, 2D screen nodes, click to move.

## Running

```sh
GODOT="C:/Users/Owner/Desktop/Game Dev Tools/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"

dotnet build                                    # C# assembly
"$GODOT" --path .                               # play
"$GODOT" --headless --path . -- --verify-slice  # assert every milestone gate (exit 0 = pass)
"$GODOT" --path . -- --shots                    # capture the slice to .shots/
"$GODOT" --headless --path . -- --seed-content  # regenerate data/*.tres from ContentSeeder
```

`--seed-content` **overwrites** everything under `data/`. It exists to bootstrap the starting
content correctly; once you begin editing rooms in the inspector, stop running it.

## Controls

| Key | |
|---|---|
| `Tab` | Toggle spectral vision (Waking ⇄ Pixel Realm) |
| Left click | Interact with a hotspot |
| `F3` | Debug panel + hotspot outlines |
| `F1` | Cycle every entity in the room through its states |
| `F5` / `F9` | Save / load |

## Architecture

**Content is data, not scenes.** One `RoomView.tscn` renders every screen in the game; each
room is a `RoomData` `.tres`. Adding a location means dropping in a resource file — no new
scene, no new code.

```
src/
  Core/       Enums, EventBus, GameConfig, EntityPresentation, Boot, Main
  Managers/   8 autoloads (see below)
  Data/       [GlobalClass] resources authored as .tres
  World/      RoomView, Hotspot, EntityOverlay, PlaceholderBackdrop, InteractionService
  UI/         Hud, ResolveMeter, DebugPanel
  Save/       JSON round-trip of the manager state
  Tools/      ContentSeeder, SliceVerifier, ScreenshotTool
data/         rooms/ entities/ items/ zones/ heists/   <- the game's content
scenes/       Boot, Main, RoomView
```

### Autoloads

`EventBus` · `WorldStateManager` · `EntityStateManager` · `RealmTransitionManager` ·
`InventoryCraftingManager` · `ResolveManager` · `NavigationManager` · `DreamRunManager`

Managers publish to the `EventBus`; views subscribe. Nothing holds a hard reference to
anything else, so one state change ripples to every backdrop, overlay and HUD element.

**Signal payloads can arrive stale.** Emission is synchronous and in subscription order, so a
subscriber may act on the signal — including navigating again — before the later subscribers
are reached. Walking into the pursuer does exactly this: it ejects you from inside
`RoomEntered`, so the ejection's `RoomEntered` runs to completion first and the one that
carried you into the Dream room lands *last*. Anything that caches per-room state must check
the payload against the manager (`CurrentRoomId != roomId → return`) rather than trusting it.

### State × realm, not state alone

The GDD lists an enum `{Corrupted, Severed, Purified}` *and* describes Form 1 / 2 / 3.
These are different axes — Form 2 (the pristine porcelain noble) is what a **Corrupted**
entity looks like **from the Dream**. `EntityStateManager.Resolve` is the only place in the
codebase that knows the mapping:

| | Waking | Dream |
|---|---|---|
| **Corrupted** | Form 1 — nightmare, blocks the road | Form 2 — spirit anchor, severable |
| **Severed** | Form 1 deformed, passable | shattered anchor, fleeing |
| **Purified** | Form 3 — mundane, farmable | benign echo, nearly gone |

State is stored per *instance* and never reset on room re-entry. That is the zero-respawn
rule, mechanically.

### Realm rendering

Both realms are live in the same room. The Waking layer sits underneath at full opacity;
the Dream layer fades in over it. Dream content renders into a `SubViewport` at **480×270**
with nearest filtering and a 4× upscale — real pixels, not a post-process. All Dream art
authors to that grid.

### Playable before any art exists

Every texture field is nullable. When one is null, `PlaceholderBackdrop` / `EntityOverlay`
draw a procedural stand-in, so the whole loop is testable and legible with zero assets. Drop
in real art one `.tres` field at a time.

## Status

> **Picking this up after a break? Read [`docs/handoff.md`](docs/handoff.md) first.** It has
> the current state, what to build next, which parts are lying placeholders, and the design
> invariants that must not be broken.
>
> One thing to know before you open the editor: the Dream's design changed substantially —
> `docs/combat.md` gained sections **15–17** and one deliberate revision to section 6.

M0–M3 complete, plus the Dream heist — 99 automated checks, `-- --verify-slice`.

The Waking world is a point-and-click adventure; the Pixel Realm is a stealth heist. Combat
and progression are designed but unbuilt — see `docs/combat.md`, which is the source of truth
for anything to do with fighting, Resolve, traps or proficiencies.

Next: M4 inventory and crafting, which traps, catalysts and her cache all depend on.
