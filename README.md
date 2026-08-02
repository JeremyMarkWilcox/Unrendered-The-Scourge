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

"$GODOT" --path . scenes/dream/DreamProbe.tscn                  # the 3D Dream probe (or F6 in the editor)
"$GODOT" --path . scenes/dream/DreamProbe.tscn -- --probe-shot  # one labelled frame to .shots/, then exit
```

**`data/` is authored, and nothing overwrites it.** `--seed-content` and `ContentSeeder` wrote the
starting content and were deleted on 2026-08-01, at the start of the content pass — a tool that
rewrites every `.tres` in one command is a bootstrap beside five rooms and a hazard beside fifty-eight
(`docs/build-plan.md` §6). Anything that generates content from here on **writes only files that do
not exist**.

## Controls

| Key | |
|---|---|
| Left click | Interact with a hotspot, or select a slot in the satchel |
| `I` | Satchel — held stacks, what a thing is, and using it |
| `Esc` | Close the open panel |
| `F3` | Debug panel (pools, entities, inventory, flags) + hotspot outlines |
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
  Tools/      SliceVerifier, ScreenshotTool
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

### State, and the form it wears

The GDD lists an enum `{Corrupted, Severed, Purified}` *and* describes Form 1 / 2 / 3.
`EntityStateManager.Resolve` is the only place in the codebase that knows the mapping:

| | on a Waking screen |
|---|---|
| **Corrupted** | Form 1 — nightmare, blocks the road |
| **Severed** | Form 1 deformed, passable |
| **Purified** | Form 3 — mundane, farmable |

This was a two-axis table — state × realm, six cells — until the great swap on 2026-08-02, because
Form 2 (the pristine porcelain noble) was what a **Corrupted** entity looked like when you pressed
`Tab` and perceived it from the Dream. **Form 2 has not been cancelled; it moved.** It belongs to
the 3D pocket you travel *into*, and will be resolved by whatever draws that. What died with the
realm axis was the claim that you could see it from out here.

State is stored per *instance* and never reset on room re-entry. That is the zero-respawn
rule, mechanically.

### One realm on screen at a time

A Waking screen is one layer of one world: a backdrop, an optional photo, and the entity overlays.
There is no second live layer underneath it and nothing to crossfade between — spectral vision, the
`SubViewport` it drew the Pixel Realm into, and the whole Dream half of `RoomData` /
`HotspotData` / `EntityPlacement` went together on 2026-08-02 (`docs/build-plan.md` §7).

The Dream is still a place you go; it is just somewhere you *travel*, and when you get there it is
3D. `RealmTransitionManager` survives and still owns which realm you are in, because a heist — and,
later, a quiet pocket — needs that answer even with no way to peek.

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
combat's wound ledger — 385 automated checks, `-- --verify-slice`.

**The whole map is walkable, and the game boots into it.** All 59 screens of the three chapter ledgers
exist as skeletons — ids, the navigation graph, an examine stub and the harvest nodes — and a new game
now starts in `ch1_cottage`, so you can walk from the cottage to the Wound and read every screen on the
way. Exits read spatially: left, right, centre-up is away from you, centre-down is back toward you.

**The bodies are on it too.** All 30 regular fights are placed, with the 24 people they free and the 6
Beast carcasses they leave, plus the three tool-gated doors, the Snarl's three-flag gate, the three doors
a Warden bars, and the three camps. What a screen is for is still missing in four places, each waiting on
a system rather than a decision: **blights** and the **major repairs** (unit 2), **pocket doors** and the
**boss fights** (unit 6), and the **field stations** (unit 4).

Two verifier sections hold the shape. `graph`: every door names a room that exists, every door can be
walked back through, the three chapters are one connected world, and no two harvest nodes share the flag
that marks them taken. `fights`: no two creatures share an instance id, no creature stands on two
adjoining screens, every creature that bars the path bars a door, and — driven live on `ch1_low_track` —
the first creature in the game is actually on screen with the person it frees drawn and shut behind them.

The waystation, the shed, the six 2D Dream rooms and the rat still load; nothing walks to them. They
come out once the 154 checks standing on them are re-anchored, which is deliberately a separate job.

The Waking world is a point-and-click adventure; the Pixel Realm is a stealth heist. Combat is
**designed and half-built**: `docs/combat.md` §18 is the ratified fight design, `CombatManager`
resolves damage and tracks wounds, and what is missing is the encounter state machine — so no fight
can be started yet. Progression is designed but unbuilt. `docs/combat.md` is the source of truth for
anything to do with fighting, Resolve, traps or proficiencies.

**Two pools, and they are not interchangeable.** Resolve is nerve: it drains on a clock in the
Dream, and an empty bar removes you from the confrontation. Vitality is the body: it has no
clock at all and moves only when something wounds you or you treat a wound, and it must never
appear in the Dream. **Only a fight spends it**, and only a salve restores it — the `VitalityCost`
hotspot field that let the world charge you the body was cut on 2026-08-01 (`docs/combat.md` §22),
so nothing outside combat can take a wound. Emptying it collapses you: **death destroys what you can
remake and keeps what you cannot.** Reagents and catalysts are gone, salves, lore and relics survive,
the cache is untouched, and you wake at the last camp.

The HUD keeps whichever pool can currently move on top and fades the other — Vitality is uppermost
out here, Resolve inside a heist. **That keys off a live Dream run, never on the current realm** —
a quiet pocket is squarely in the Dream with no clock running, and reordering the bars there would
promise a threat that is not in the room. Resolve is never faded, and Vitality dims rather than
disappearing, because a readable bar is how the HUD states that nothing in the Dream can kill you.

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
It touches no autoload, no save format and not `RoomView`, so it left the suite untouched. **Unit 1
— the greybox pocket — is built and walkable**, and settled the creature question by looking rather
than arguing.

**Unpainted screens read themselves out loud.** A screen with no `WakingBackground` draws a corner card
naming the room, its chapter and its exits, and labels every hotspot in place — filled and outlined in a
colour per kind, with the destination room id written on a door and `⊘ {flag}` under anything a gate is
holding shut. **The condition is the art, not a setting**: paint a screen and its cues vanish for it, so
none of this can ship by accident. `ShowHotspotOutlines` is still the manual override for painted
screens, because a painting can hide a clickable region as well as a grey box can.

**Next: retiring the prototype** — re-anchoring the gates that stand on the waystation, the six 2D Dream
rooms and the rat onto synthetic `verify_` rooms, then deleting all of it — followed by the encounter
state machine, the crafting panel and the journal.
`docs/build-plan.md` has the unit order; `docs/handoff.md` has the current state and the invariants.

**Expect the check count to fall during the content pass.** It is not regression — the suite grew up
around a five-room prototype, and every gate that stands on the waystation, the rat or the heist is a
gate whose fixture is being deleted out from under it. The swap took 347 → 338 on its own; unit 1.5 put
it back to 359, the skeleton rooms brought their own gates to 367, and moving the boot made it 369.
