# Unrendered: The Scourge

Godot 4.7.1 (mono) / C#. A dual-realm survival-horror RPG: a cold high-res **Waking** world
and a pixelated **Dream** realm, where severing a spirit's anchor in the Dream deforms its
physical host in reality.

**Both realms are first-person, and that is the only thing they share.** The Waking world is
first-person in a **2D** world — Myst-style, painted screen nodes, click to move. The Dream is
first-person in a **3D** world, walked rather than clicked. Settled 2026-07-30; what is still open is
whether the creatures in the Dream are 2D billboards or 3D models. The Waking half is built and
playable; the Dream's 3D half is **walkable but not wired in** (`scenes/dream/DreamProbe.tscn`). The
graph of 2D Dream rooms still in `data/rooms/` is the old prototype and is going away.

## Running

```sh
GODOT="C:/Users/Owner/Desktop/Game Dev/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"

dotnet build                                    # C# assembly
"$GODOT" --path .                               # play
"$GODOT" --headless --path . -- --verify-slice  # assert every milestone gate (exit 0 = pass)
"$GODOT" --path . -- --shots                    # capture the slice to .shots/
"$GODOT" --headless --path . -- --seed-content  # regenerate data/*.tres from ContentSeeder

"$GODOT" --path . scenes/dream/DreamProbe.tscn                  # the 3D Dream probe (or F6 in the editor)
"$GODOT" --path . scenes/dream/DreamProbe.tscn -- --probe-shot  # one labelled frame to .shots/, then exit
```

`--seed-content` **overwrites** everything under `data/`. It exists to bootstrap the starting
content correctly; once you begin editing rooms in the inspector, stop running it.

## Controls

| Key | |
|---|---|
| `Tab` | Toggle spectral vision (Waking ⇄ Pixel Realm) |
| Left click | Interact with a hotspot, or select a slot in the satchel |
| `I` | Satchel — held stacks, what a thing is, and using it |
| `Esc` | Close the open panel |
| `F3` | Debug panel (pools, hazards in room, entities, inventory, flags) + hotspot outlines |
| `F1` | Cycle every entity in the room through its states |
| `F2` | Take a debug wound (15 Vitality) |
| `F5` / `F9` | Save / load |

The **Dream probe** is a separate scene with its own keys: `WASD` walks, click captures the mouse and
`Esc` releases it; `[ ]` greys, `; '` white clamp, `- =` dither, `, .` light radius; `5 6` creature
line thickness, `7 8` line density, `9 0` crawl rate, `K` whether creatures reach white; `O P` light
flicker and `N M` its speed; `F4` hides the readout and `F12` saves a frame named after the values
that produced it. Holding a key scrubs. **[`docs/dream-probe.md`](docs/dream-probe.md) is the guide to
driving it**, including where the lighting and material settings live in the editor.

## Architecture

**Content is data, not scenes.** One `RoomView.tscn` renders every screen in the game; each
room is a `RoomData` `.tres`. Adding a location means dropping in a resource file — no new
scene, no new code.

```
src/
  Core/       Enums, EventBus, GameConfig, EntityPresentation, Boot, Main
  Managers/   10 autoloads (see below)
  Data/       [GlobalClass] resources authored as .tres
  World/      RoomView, Hotspot, EntityOverlay, PlaceholderBackdrop, InteractionService
  UI/         Hud, ResolveMeter, VitalityMeter, DebugPanel, InventoryPanel
  Dream/      the 3D probe's walker and look tuner   <- wired into nothing; see the handoff
  Save/       JSON round-trip of the manager state
  Tools/      ContentSeeder, SliceVerifier, ScreenshotTool
data/         rooms/ entities/ items/ recipes/ zones/ heists/   <- the game's content
scenes/       Boot, Main, RoomView
scenes/ui/    InventoryPanel + the shared PanelTheme.tres
scenes/dream/ DreamProbe.tscn + the look shader and its material
```

**Panels are scenes; the HUD is not.** `Hud.cs` is built in code because its layout never changes.
Grids and slots are layout you tune by eye, so they are authored as `.tscn` against a published
node-name contract (see the handoff): the scene owns layout, the script owns behaviour, and the
script only knows `%UniqueName`s — so a panel can be restyled freely without touching code.

### Autoloads

`EventBus` · `WorldStateManager` · `EntityStateManager` · `RealmTransitionManager` ·
`InventoryCraftingManager` · `ResolveManager` · `VitalityManager` · `NavigationManager` ·
`DreamRunManager` · `CampManager`

`CampManager` is registered last because it asks the others questions rather than answering them.

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

That `SubViewport` is also the seam the 3D Dream drops into, which is why going 3D costs the
architecture nothing: `RealmTransitionManager`, `EntityStateManager` and the save format do not care
what the Dream looks like.

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

M0–M3 complete, plus the Dream heist, M4's crafting layer with its inventory panel, camps, and
combat's wound ledger — 328 automated checks, `-- --verify-slice`.

The Waking world is a point-and-click adventure; the Pixel Realm is a stealth heist. Combat is
**designed and half-built**: `docs/combat.md` §18 is the ratified fight design, `CombatManager`
resolves damage and tracks wounds, and what is missing is the encounter state machine — so no fight
can be started yet. Progression is designed but unbuilt. `docs/combat.md` is the source of truth for
anything to do with fighting, Resolve, traps or proficiencies.

**Two pools, and they are not interchangeable.** Resolve is nerve: it drains on a clock in the
Dream, and an empty bar removes you from the confrontation. Vitality is the body: it has no
clock at all and moves only when something wounds you or you treat a wound, and it must never
appear in the Dream. Vitality is spent by **hazards** — a `VitalityCost` on any hotspot, charged
only on a successful click, and refused outright in the Dream — and restored by salves. Emptying it
collapses you: **death destroys what you can remake and keeps what you cannot.** Reagents and
catalysts are gone, salves, lore and relics survive, the cache is untouched, and you wake at the
last camp.

**Hazards are not traps.** Traps are the player's own crafted tools and are blocked on M4; a hazard
is the world costing you the body.

The HUD keeps whichever pool can currently move on top and fades the other — Vitality is uppermost
out here, Resolve inside a heist. That keys off a live Dream run rather than the current realm,
because spectral vision flips the realm constantly without Resolve moving at all, and peeking is a
look rather than a place: your body is still out here and can still be hurt. Resolve is never
faded, and Vitality dims rather than disappearing, because a readable bar is how the HUD states
that nothing in the Dream can kill you.

**Camps are the safe ground, and a room is a camp precisely when it holds a `Workbench` hotspot.**
There is no camp flag to keep in sync — the camp is the room you put the bench in. Arriving refills
both pools and silently becomes your return point; there is nothing to click, because a player would
never decline a rest. Crafting and the shared cache only work at a bench, so **placing that first
`Workbench` hotspot is what turns crafting back on in play** — until then every attempt is refused
with `CraftBlock.NotAtCamp` / `StashBlock.NotAtCamp`, which is a reason the crafting panel can print
rather than a silent failure.

**The 3D Dream is walkable, in a scene wired into nothing.** `scenes/dream/DreamProbe.tscn` is the
look shader, the 480×270 3D viewport, a first-person controller and the lighting model — the last
architectural risk and the first thing worth showing anyone, which turned out to be the same build.
It touches no autoload, no save format and not `RoomView`, so the suite is unchanged at 328.

**Next: the greybox pocket** — one quiet knowledge pocket with a distinctive landmark, and a capsule
beside a billboard so the creature question can be answered by looking rather than argued. Then the
seam into `Main` with its gates, then the encounter state machine, the crafting panel and the journal.
`docs/handoff.md`, "The Dream in 3D", has the order and what unit 1 already taught.
