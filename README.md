# Unrendered: The Scourge

Godot 4.7.1 (mono) / C#. A dual-realm survival-horror RPG: a cold high-res **Waking** world
and a pixelated **Dream** realm, where severing a spirit's anchor in the Dream deforms its
physical host in reality.

**Both realms are first-person, and that is the only thing they share.** The Waking world is
first-person in a **2D** world — Myst-style, painted screen nodes, click to move. The Dream is
first-person in a **3D** world, walked rather than clicked. Settled 2026-07-30, and so is what used to be
the open half of it: the creatures in the Dream are **3D models, with no billboards** — Jeremy's axis was
the coherence of the space, not per-creature image quality. The Waking half is built and playable; the
Dream's 3D half is **wired in as of unit 6, and empty**: a `CrossToDream` hotspot crosses you into a pocket
scene and back, `data/pockets/` holds one reference pocket pointing at `scenes/dream/DreamProbe.tscn`, and the
pockets themselves are Jeremy's to build. The 2D Dream rooms that used to sit in `data/rooms/` went with the
prototype on 2026-08-03 and the heist that drove them went in unit 6b, so there is exactly one Dream model
now rather than two.

## Running

```sh
GODOT="C:/Users/Owner/Desktop/Game Dev/Godot/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe"

dotnet build                                    # C# assembly
"$GODOT" --path .                               # play
"$GODOT" --headless --path . -- --verify-slice   # assert every milestone gate (exit 0 = pass)
"$GODOT" --headless --path . -- --verify-content # the content bill: what is authored vs. what is owed
"$GODOT" --path . -- --shots                    # capture the slice to .shots/

"$GODOT" --path . scenes/dream/DreamProbe.tscn                  # the 3D Dream probe (or F6 in the editor)
"$GODOT" --path . scenes/dream/DreamProbe.tscn -- --probe-shot  # one labelled frame to .shots/, then exit
```

**`--verify-content` never fails, and that is deliberate.** It prints the bill from `placement.md` §1 against
what is on disk and always exits 0 — a bill is a to-do list while a chapter is being built and an invariant
only once it is sealed, so folding it into `--verify-slice` would leave the suite red for months, and a red
result that is nobody's fault is how a suite gets ignored (`docs/build-plan.md` §6). It also names the rules
it *cannot* check and the ones already gated elsewhere, so nothing reads as unchecked by omission.

**`data/` is authored, and nothing overwrites it.** `--seed-content` and `ContentSeeder` wrote the
starting content and were deleted on 2026-08-01, at the start of the content pass — a tool that
rewrites every `.tres` in one command is a bootstrap beside five rooms and a hazard beside fifty-eight
(`docs/build-plan.md` §6). Anything that generates content from here on **writes only files that do
not exist**.

Pass 2 and unit 2 both needed the weaker form of that — appending to files that already existed — so the
rule has a second half. A generator that touches an authored file **lives in `/tmp`, never in the tree**,
names the files it will touch instead of globbing, **refuses any file that already holds the thing it is
adding**, and builds every file in memory before writing any of them, so a failure on the twelfth leaves
the first eleven untouched. The same discipline applies to break-tests, which snapshot first and restore
byte-exactly.

## Controls

| Key | |
|---|---|
| Left click | Interact with a hotspot, or select a slot in the satchel |
| `I` | Satchel — held stacks, what a thing is, and using it |
| `C` | Return to camp — the camps you have stood in, one row each; pressing a row travels. Refused inside a fight and inside a Dream pocket |
| `Esc` | Close the open panel |
| `F3` | Debug panel (pools, entities, inventory, flags, what this screen still has unfinished) + hotspot outlines |
| `F10` | Cross into the reference Dream pocket, or leave it — the 2D→3D seam, since no pocket entrance is authored yet. It deliberately cannot *complete* one: completing breaks the entrance for that save, so a key that did it would be destructive scaffolding. **`F10` and not `F4`, because the probe scene binds `F4` to its own readout toggle** and hosting it in-game would double-bind the key |
| `F1` | Cycle every entity in the room through its states |
| `F2` | Take a debug wound (15 Vitality) |
| `F5` / `F9` | Save / load |
| `F6` | Learn every craft family — nothing in `data/` grants a blueprint yet, so this is how any of the 16 gated recipes open |
| `F7` | Craft everything craftable where you stand, and name the block reason for everything else |
| `F8` | Set every trap you are carrying against the creature on this screen |

`F6`–`F8` and `F10` are **scaffolding for things not yet reachable in play**, listed because a debug key
nobody knows about is not an unblock. They come off separately: F6 and F7 retire with the journal (which
*is* the crafting screen), F8 the day the satchel grows a **Set** button beside Use and Equip, and F10 the
day a `CrossToDream` hotspot is authored onto a real screen.

**`C` is not one of them** — it is `docs/combat.md` §17's return-to-camp verb and it ships. The panel behind
it has no authored scene yet, so `CampPanel.BuildFallback` puts the published node contract up in code; when
`scenes/ui/CampPanel.tscn` lands against the same node names it is a drop-in, and what gets deleted is the
builder rather than a branch. Same arrangement as the fight screen, for the same reason.

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
  Managers/   12 autoloads (see below) + MendManager, which is static and deliberately not one
  Data/       [GlobalClass] resources authored as .tres
  World/      RoomView, Hotspot, EntityOverlay, PlaceholderBackdrop, InteractionService,
              PlaceholderCues, RoomCard, PocketHost   <- PocketHost is the 2D->3D seam
  UI/         Hud, ResolveMeter, VitalityMeter, DebugPanel, InventoryPanel, MendPanel,
              FightPanel, CampPanel   <- the last two have no .tscn yet and build their own
  Dream/      the probe's walker and look tuner, plus PocketAnchor / PocketExit / PocketGate
              <- the three nodes a cloned pocket scene is built from; no scripting needed
  Save/       JSON round-trip of the manager state
  Tools/      SliceVerifier, ContentReport, ScreenshotTool
data/         rooms/ entities/ items/ recipes/ pockets/   <- the game's content
scenes/       Boot, Main, RoomView
scenes/ui/    InventoryPanel, MendPanel + the shared PanelTheme.tres
scenes/dream/ DreamProbe.tscn + the look shader and its material
```

**Panels are scenes; the HUD is not.** `Hud.cs` is built in code because its layout never changes.
Grids and slots are layout you tune by eye, so they are authored as `.tscn` against a published
node-name contract (see the handoff): the scene owns layout, the script owns behaviour, and the
script only knows `%UniqueName`s — so a panel can be restyled freely without touching code.

### Autoloads

`EventBus` · `WorldStateManager` · `EntityStateManager` · `RealmTransitionManager` ·
`InventoryCraftingManager` · `ResolveManager` · `VitalityManager` · `NavigationManager` ·
`PocketManager` · `CampManager` · `CombatManager` · `EncounterManager`

**Order is load-bearing at both ends.** `EncounterManager` is last because it asks every other manager
questions and answers none of theirs, and `CampManager` sits after `PocketManager` for the same reason it
always sat late — it asks rather than answers. `DreamRunManager` sat between them until unit 6b deleted it;
`PocketManager` is its replacement, and the two overlapped for exactly one unit so that the gates probing a
Waking-side guard from inside a live run could move across before the class went.

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

M0–M3 complete, plus the Dream heist, M4's crafting layer with its inventory panel, camps, the blight
and mend layer, the station and material layer, the arsenal, and combat's wound ledger — 687 automated
checks, `-- --verify-slice`.

**The prototype is gone as of 2026-08-03.** The waystation, its shed, the six 2D Dream rooms, the rat and
`herb_gravecap` are deleted; every check that stood on them now names a synthetic `verify_` room registered
in code, or was deleted along with the mechanism it covered. So all 59 rooms, 11 entities and 27 items in
`data/` are content the game actually uses.

**The whole map is walkable, and the game boots into it.** All 59 screens of the three chapter ledgers
exist as skeletons — ids, the navigation graph, an examine stub and the harvest nodes — and a new game
now starts in `ch1_cottage`, so you can walk from the cottage to the Wound and read every screen on the
way. Exits read spatially: left, right, centre-up is away from you, centre-down is back toward you.

**The bodies are on it too, and so is the damage.** All 30 regular fights are placed, with the 24 people
they free and the 6 Beast carcasses they leave, plus the three tool-gated doors, the Snarl's three-flag
gate, the three doors a Warden bars, and the three camps. **All 12 blights** are on it as well, each a
broken made-thing you put back together for a Graveflower. What a screen is for is still missing in three
places, each waiting on a system rather than a decision: **pocket doors** and the **boss fights**
(unit 6), and the **3 major repairs** — the bridge, the mine cart and the exorcism engine — which need a
reclaimed memory and a jigsaw before they can be recipes, so they wait on a boss drop. **Field stations
are done at two**, the anvil and the mortar: every other recipe belongs at the bench, and a camp is a menu
reachable from anywhere, so a third station would save a walk rather than open anything.

**The three tool doors now open.** A recipe can set a world flag, so the Bill Hook, Pry Bar and Mine Pick
are craftable things that unlock geography rather than names in a ledger — and the three blights behind
them (`ch1_stonefield`, `ch2_vestry`, `ch3_sealed_drift`) are reachable for the first time. You cannot
*learn* them from anything authored yet: each needs a Dream knowledge pocket, which is unit 6, so **F6
learns every craft family** in the meantime. There is no crafting panel yet either — clicking the anvil in
`ch2_smithy` fires its signal and nothing is listening — so **F7 crafts everything craftable where you
stand** and names the block reason for the rest. Both keys are scaffolding with a scheduled demolition:
they come out when the journal lands, which is the crafting screen (decision 9).

**And there is an arsenal to spend materials on — 27 items and 17 recipes.** Two weapons (Edge and Blunt),
three coats (warding Edge, Blunt and Caustic), six traps, two salves and the two brews of hers that the
Fire and Caustic traps are fused from. He starts holding the **Splitting Axe**, the one item in the game
with no recipe.

**And there is now a fight to spend it in.** Rig up to two traps on a creature — free, and you can walk
away and come back to find them still there — then click to opt in. Turns alternate, every act costs one,
and fleeing always works but the thing gets a parting blow on the way out. The right weapon does double and
the wrong one half; a coat halves one incoming damage type and nothing else. Winning against a possessed
villager frees them if you cut the tether first and **kills them** if you simply overkill the body, and the
screen cleanses either way — the difference is counted in the ending, not painted on the ground. Dying is
survivable: you wake at the camp still wearing your kit, still holding your traps, having lost only raw
material. Equipping is a satchel verb, so a coat can be swapped anywhere.

Six verifier sections hold the shape. `graph`: every door names a room that exists, every door can be
walked back through, the three chapters are one connected world, and no two harvest nodes share the flag
that marks them taken. `fights`: no two creatures share an instance id, no creature stands on two
adjoining screens, every creature that bars the path bars a door, and — driven live on `ch1_low_track` —
the first creature in the game is actually on screen with the person it frees drawn and shut behind them.
`mending`: exactly 12 blights split 2 / 4 / 6 by chapter, each paying exactly one Graveflower and no
other hotspot in the game paying any, every board solvable with in-range distinct steps, exactly three of
them unreachable until a tool is learned — and, driven live on `ch1_fern_gully`, the panel opens off the
cue, solves, pays, and leaves the screen uncleansed because a creature is still standing on it.
`stations`: **every item a hotspot or a recipe names actually exists** — the check that found 33 harvest
nodes paying out items that had never been written — every item and recipe file on disk registered one,
only the eight ratified materials are yielded anywhere, a recipe is refused where its station is not and
allowed at the camp bench, and, driven live, crafting the Bill Hook sets its flag and opens the Bramble
Choke that was shut a moment earlier. `roster`: **no weapon deals Fire or Caustic and no armour wards
Fire** — the deliberate hole the traps exist to fill — every coat wards exactly one type and every weapon
deals exactly one, the four armed traps cover four distinct types while the Snare and the Chime deal
nothing on purpose, a trap refuses a second craft where a spare coat is allowed, Clearmind still heals more
per material unit than Deepmend, and — the one that earns its keep — **everything in the catalogue can be
obtained**, checked by seeding from the harvest nodes and the starting axe and closing over the recipe set
until nothing new can be made. That last one found one hole and names it: `catalyst_ashincense` has no
recipe, and the gate fails in **both** directions, so writing one is also the notice to delete the
exemption. `encounters`, the newest and by far the largest at 154 checks: the third rigged trap is refused
and nothing is quietly swapped out for it, rigging is free outside a fight and costs a turn inside, a
self-triggering trap fires on engage and the others wait for a turn spent on them, a 2× hit and a 0.5× hit
differ **fourfold** driven through `Strike` rather than measured beside it, a coat halves the type it wards
and does nothing at all against the others, flight always works and always costs one parting blow, **a door
is a way of breaking off and is priced like one while no other way of changing rooms costs anything**, the
three win routes write the three flags off `PurifiedName`, the placed roster counts **24 people and 6
beasts**, wounds persist across a severed engagement and close across an anchored one, two salves is two
salves and the trap sites were never in that trade, and — driven live through the panel — engaging names
both halves of what you are facing, a named verb advances the turn, and striking finishes the body in three.
It ends by killing the player on purpose: you wake at a camp, still equipped, still holding every trap, and
the loose reagents are gone. Three more came out of the **first playtest** (2026-08-03): the fight screen
draws on something opaque, the room behind it is tinted out, and **the tint is behind the verbs rather than
over them** — a scrim in front of the strip greys out the very buttons the tint was meant to make legible,
and passes every other check. `inventory panel` gained sixteen alongside them: every tile carries its item's
display name rather than its id, the stack count is dark enough to read against a pale swatch, a category tab
narrows the grid and says which kind in the player's word, a tab arrives with the first of its kind and
leaves with the last, the filter it was holding falls back to All rather than stranding you on an empty grid,
**Lore gets no tab because nothing authored is Lore yet** — that one is *meant* to go red the day a Dream
page lands — and the code's colour override steps aside for a colour the scene sets, which is checked against
a probe label because on screen the guarded and unguarded versions look identical. **The second playtest, the
same day, added three more, and all three are about the caption the first one asked for**: it was truncating
every name past about eight characters, so the band is now checked to be at least as wide as the swatch it
labels, the stack count to land *on* that swatch instead of three pixels past its corner, and the two to be
clear of one another. **Four more looks at the same tile added three checks and buried two others, all inside one
day** — the only checks in the suite so far to be born and buried the same afternoon, and each one asserted the
arrangement whoever wrote it had just built rather than a property that has to hold. The subject never changed:
the stack count wants the swatch's bottom-right corner, and the caption band crosses that corner. Asked *"why
can't it be square and have the number where it is?"* I held the count on identical pixels and made the swatch
90×90 — every check green, nothing authored touched, and against a swatch now reaching 40px lower those pixels
read as the middle of the icon, because **holding the pixels is not holding the position**. Told the placement
was right but the swatch still wasn't square, I found that the impossibility I had documented three times rested
on **a width I had fixed at 90 without deciding to**: the caption fixes the bottom and the scene fixes the top,
so the square is 61×61, and squaring the swatch means *narrowing it to its height*. **When a constraint reads as
mutually exclusive, look for the number you never chose.** What survives in the suite measures the *gap* from the
count to the swatch's two near edges — the only one of six that tells the accepted tile from the rejected one —
and the squareness, which is a check for something asked for twice rather than for something built once. All six
run against a synthetic tile rather than a drawn one, which is what keeps them green the day Jeremy authors a
real caption node: the numbers they check are computed in code from the template's, so they can only ever fail
on code.

**Retiring the prototype rearranged the sections more than it shrank them (2026-08-03, 680 → 670).** The
`M1` section is gone — it asserted that two rooms register and a door connects them, which `graph` already
sweeps across all 59 authored rooms at real scale, so a synthetic two-room copy was a weaker version of a
gate that exists; its one unique claim, *"a new game starts in the Waking world,"* moved to `harness`.
`M2` and the surviving half of `M3` now stand on `verify_loop`, and the heist section kept only the checks
that were using a live run as a **probe** for code that outlives it. Three checks went in that were not
planned. `content` gained a **deletion gate** — nothing registers with `ChapterId = "waystation"`, no item
id starts `herb_gravecap`, and `data/heists/` holds zero files — which is what would notice a `.tres`
surviving in somebody's working tree. `graph` gained *"and every authored room is on it, so the `ch*` sweeps
skip nothing,"* so an off-spine room now goes red **by name** instead of being skipped silently by five
sweeps and covered by none. And `camps` gained a pair on `verify_station`, a mortar in a room with no bench
under it: **a room can be a venue without being a camp**, that station is shut inside a live run, and it
opens again the moment the run ends — same room, same satchel. That last pair exists because break-testing
found the crafting venue guard had **no probe at all**: every crafting check stood on the camp arm, which
refuses inside a run on its own, so the guard could have been deleted and the suite would have stayed green.

**Then three findings from reading unit 3a's own gates back (2026-08-03, 670 → 687).** The strike verb now
names the damage type in your hand — *"Strike — Bell Maul (weight)"*, *"Strike — bare hands (weight)"* —
because the confirm line already told you what the creature *barely notices* and the verb never said what you
were about to hit it with; the tutorial fight was closable with a label rather than with a tuning decision.
`encounters` gained five for it, including the one that matters: **the strip repaints on `InventoryChanged`,
not only on the log**, counted by how many times the handler ran rather than by asking the signal who is
listening — `signal += handler` marshals into a *custom* callable in C#, so `GetSignalConnectionList` hands
back a null `Target` and `IsConnected` with a freshly-made `Callable` does not match either. Three ways of
introspecting it all went red against correct code before the fourth stopped introspecting. `fights` gained
**eight, and none of them is a number**: the nearest fight to the boot room is walked to rather than named,
and asserted to fall to the starting axe *before it falls you*, to be the pairing that axe answers, and to be
unwinnable bare-handed — three claims that compare two derived turn counts, so §5's 3–6 band stays tuning and
no gate goes red the day Jeremy wants a fight to feel different. `F8` reaches `EncounterManager.Rig`, which
shipped in 3a with **no verb anywhere**, and it is *pressed* by the suite rather than merely bound: it answers
*"no traps in the satchel"* on a real playthrough until a blueprint has a source, so correct and broken look
identical on screen.

**A screen is unfinished for two reasons, and both are things you did on it.** A creature that is not
purified **holds** it; an unmended blight **fouls** it; clear both and `screen.<id>.cleansed` goes up and
the colour comes back. A tool-gated door is deliberately *not* a third reason — it would resolve for
every screen in the world the instant you learned the tool, and cleansing should be caused by something
you did here. There is no zone state any more: `ZonePhase` and everything that read it came out on
2026-08-03, and `SaveGame` lost a field for the first time in its life.

~~The waystation, the shed, the six 2D Dream rooms and the rat still load; nothing walks to them.~~
**They came out on 2026-08-03, and the re-anchoring was the job.** (This line used to say **154** checks
stood on them; nobody had measured it. Counted per section, the four standing *wholly* on the prototype
totalled **76**.) About 15 of those were using a live heist as a **probe** for four files that survive
unit 6 — the HUD's pool order, a bench inside a run not being a camp, the crafting venue guard and
*"a mid-Dream save does not strand you"* — so those kept a synthetic fixture and the rest went with the
mechanism. `DreamRunManager` and `DreamHeistData` are still here, now held up only by a fixture built in
code; they retire in unit 6, with the pocket format.

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

**Next: the return-to-camp verb** — one verb, reachable from any Waking screen with no run live and refused
inside a fight or a pocket, which is the vertical slice's last missing beat. Then the Dream pocket format
(unit 6), the crafting panel and the journal. *Retiring the prototype was the previous entry here and landed
2026-08-03.*
`docs/build-plan.md` has the unit order; `docs/handoff.md` has the current state and the invariants.

**Expect the check count to fall during the content pass.** It is not regression — the suite grew up
around a five-room prototype, and every gate that stood on the waystation, the rat or the heist was a
gate whose fixture was being deleted out from under it. The swap took 347 → 338 on its own; unit 1.5 put
it back to 359, the skeleton rooms brought their own gates to 367, moving the boot made it 369, the
pass-2 bodies took it to 385, and unit 2's mend layer to 439. **The retirement itself was 680 → 670**,
smaller than the 76 standing on the prototype because most of those moved rather than died — and it *added*
three, one of which caught a guard that had never had a probe at all. Unit 2 is the other counter-example
worth remembering: **it deleted a whole system and the count did not move for it**, because every invariant
`ZonePhase` held moved onto the flag that replaced it.
