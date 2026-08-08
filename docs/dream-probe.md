# The Dream probe — how to drive it

A practical guide to `scenes/dream/DreamProbe.tscn`, written for a first go at Godot's 3D side.
The design reasoning is in `docs/combat.md` §14 (the Dream is stealth, and its creatures are models) and
§15 (the pockets); this file is only *which button*.

**The one thing to know first: you do not need the 3D editor to tune the look.** Every value worth
arguing about is on a key while the scene is running. The editor is for *moving things* — lights,
walls, the landmark. Those are two different sessions and it is worth keeping them separate.

---

## Two scenes, and which one to open

| | |
|---|---|
| **`scenes/dream/Sandbox.tscn`** | **the playtest pocket.** `data/pockets/dream_probe.tres` points at it, so **F10 in the running game** crosses you in — and that is the only way the verbs, the creatures and the exit are live at all, because they all refuse outside a pocket. Build here |
| **`scenes/dream/DreamProbe.tscn`** | the look bench. Three creature meshes side by side for scrubbing the creature shader, which the sandbox has no reason to carry. **F6** it when you are tuning the look and nothing else |

Both carry the tuner, so the look keys below work in either.

> **F6 on the sandbox is a real visit** — the verbs work, creatures cost, the exit fires. That is not free:
> everything in a pocket asks `PocketManager` whether you are inside one first, and running a scene directly
> never used to set it, so all of them refused and the pocket read as broken. The **`Standalone`** node in the
> scene is what tells the manager. It also fills the bar and hands over Float and Flare, so there is no F11 to
> remember.
>
> **Put one in every pocket scene you build.** It works out which pocket it belongs to from the scene's own
> path, so there is nothing to configure and a copied scene cannot carry a stale id. It stands down entirely
> when you cross in from the game, so it costs nothing there.
>
**Use F6 to build and F5 → F10 to believe it.** F6 is a two-second loop on one scene and covers everything
you author. What it cannot cover is the crossing itself — `PocketHost` loading the scene, freeing it, turning
the viewports off on the way out, and the HUD and panels surviving all of it. Nothing in the scene can be
wrong in a way F6 shows and F10 hides; plenty can be the other way round.

A reasonable rhythm: F6 while shaping, F5 → F10 at the end of a sitting and any time the scene's *structure*
changed rather than its contents.

---

## Running the look bench

1. In the **FileSystem** panel (bottom left), open `scenes/` → `dream/` → double-click
   `DreamProbe.tscn`.
2. Press **F6** — *Run Current Scene*.

> **Not F5.** F5 runs the project's main scene (`Boot.tscn`) and you will get the normal game.
> F6 runs whatever scene is open.

Then, in the running window:

| | |
|---|---|
| **Click once** | captures the mouse — nothing looks around until you do |
| `WASD` | walk |
| `Esc` | releases the mouse **but does not quit** |
| **F8** | stops the run (or the editor's stop button, or close the window) |

---

## Tuning the look — the keys

Hold a key to scrub it. The readout in the top-left shows every current value.

**The world:**

| Key | | What it does |
|---|---|---|
| `[` `]` | **greys** | how many bands the world is posterised into. **2 to 5 is the sanctioned band**; the choice inside it depends on how much detail the eventual models have, so it is not settled and does not need to be |
| `; '` | **clamp** | the ceiling on ordinary surfaces. This is what *reserves* white — geometry physically cannot reach the top of the ramp |
| `- =` | **dither** | the crosshatch strength. Trades against greys: a heavily dithered 2 can read richer than a clean 12, so move the two together |
| `G` `H` | **border** | how bright `PocketBounds.BorderShell` reads. **The value you are hunting is the dither band, not a brightness** — see below |
| `, .` | **light** | the player light's radius. This is also the Resolve degrade in its entirety — when the drain is built, the dark closing in *is* this number falling. **`Q` (Suppress) dims the same number**, and the two do not fight: the verb takes the radius over only while it is held, and hands it back to whatever you last tuned it to |

**Creatures** — both at once, so body-locked and screen-space stay comparable:

| Key | | What it does |
|---|---|---|
| `5` `6` | **thickness** | pixels per line in screen space; inversely, lines per metre on the body |
| `7` `8` | **density** | fraction of lines lit. Low is a barely-there thing, high a nearly-solid one |
| `9` `0` | **crawl Hz** | re-rolls per second. **0 is static** and reads as a texture; fast strobes and fights the dither |
| `K` | **white** | whether creatures are promoted to white, or capped at the clamp and left dark. Settled: **no** |
| `U` `I` | **line break** | how much the lines refuse to line up. 0 is continuous bands and reads as a barcode; raise it and the body tears into independently-lit cells — stripes become static |
| `J` `L` | **dist comp** | body-locked creatures only. 0 locks bands to the model, so they thin below one pixel at range and shimmer. 1 holds their pixel size constant instead, so bands are *added* as you approach. **Default 1** |

> Creatures seed their pattern from where they stand, so two of them are never in phase. That is
> derived rather than authored — a creature dropped into a room cannot accidentally sync with its
> neighbour. `break_size` (the width of a torn cell) is inspector-only, to keep this list survivable.
>
> **Thickness and density are coupled.** Thick lines mean few bands, and at low density only one or
> two are lit — with the crawl re-rolling, a creature intermittently vanishes outright. Fine lines
> have enough bands for the fraction to hold. If you go thick, raise density with it.

**Lights:**

| Key | | What it does |
|---|---|---|
| `O` `P` | **flicker** | scales every light's guttering at once. Each light's *character* is authored on its own `Flicker` child, so dialling this keeps the lamp subtle and the sconces desperate |
| `N` `M` | **fl speed** | scales how fast they gutter |

**Both:**

| Key | | What it does |
|---|---|---|
| `B` | **flood** | light the whole pocket at once and drop the monochrome, so you can see what you are building. `B` again puts both back |
| `F4` | | hide the readout, for a clean screenshot |
| `F12` | | save a frame to `.shots/`, named after the values that produced it |

**The verbs** — not tuning keys. These are the real abilities, they cost Resolve while held, and they are
listed here and on the readout because they are the other thing you press in a pocket:

| Key | | What it does |
|---|---|---|
| `Q` | **Suppress** | 2/s. Puts the lamp out. Hold it to hide |
| `Space` | **Float** | 4/s. Slow rise with damped steering. About 25 s of lift on a full bar |
| `E` | **Flare** | 6/s. Throws the lamp wide — **the light only**, see below |

Holding `Q` and `E` together costs both (8/s) and goes dark; Suppress wins. Running the bar out ends every
hold, so Float drops you.

> **`B` is two changes, not one, and the second is what makes it useful.** Raising the ambient alone hands a
> bright room to `DreamLook`, which posterises it into a few greys under a clamp — a flooded pocket comes out
> one flat value and you can see *less* than by lamplight. So the look material comes off the container at the
> same time and you get the raw lit room.
>
> **`PocketHost` attaches the light**, so a pocket you cross into from the game always has it and there is
> nothing to wire. **A pocket you run on its own with `F6` has no host**, so if you want `B` in that workflow —
> and that is the workflow — drop a plain `Node` with `DreamAuthorLight` on it at the root of the pocket scene.
> Having both is fine: the first one into the tree answers the key and the other stays quiet.
>
> Nothing about it is saved and nothing about it ships.

> There is no signal-threshold key any more. Points of interest used to be detected by *brightness*,
> which leaked — a wall near a bright light would flash white. They are detected by **chroma** now:
> the world is greyscale, so anything you tint is a point of interest and nothing else can ever be
> one. **Paint a material any colour and it becomes white in game.** The sphere is red in the editor
> and white when you play.

> **Standing rule, 2026-08-07: every debug key that works in a pocket goes on the readout**, including ones
> the tuner does not own. Asked for after `B` was rebound twice and went unfound: *"we should always add the
> debug buttons labels to the temp hud."* So the flood light and the three verbs are listed there beside the
> scrub keys even though nothing about them is scrubbed. The duplication is deliberate — a key you cannot
> discover is a key that does not exist, and there is one surface in a pocket where discovery happens.

**Nothing you scrub at runtime is saved.** That is why the readout prints to the Output panel and why
F12 puts the numbers in the filename — so a value you like can be found again. Send me the numbers, or
just the filenames, and I will write them into `DreamLook.tres` as the new defaults.

---

## The three verbs now do something

Until 2026-08-07 all three verbs were **priced but inert**: `DreamVerbs` refused the hold outside a pocket,
charged Resolve by the second while it was held, and then nothing happened. That is the worst state a feature
can sit in, because every check about it is green and the meter really moves, so it reads as *broken* rather
than as unbuilt. Jeremy found it the only way it can be found — *"Q works. I didn't see space do anything."*

### `Q` — Suppress, and `E` — Flare: one script on the lamp

Attach **`DreamSuppressibleLight`** to the player's `OmniLight3D` (right-click the node → *Extend Script*, or
swap its type). Nothing else. Holding `Q` puts the lamp out over `FadeSeconds`; holding `E` throws it wide;
letting go brings it back.

| field | | |
|---|---|---|
| `SuppressedFraction` | **0** | how much lamp is left while hiding. `0` is out entirely — invisible and blind. `0.1` leaves a stub you can read the floor by |
| `FlaredMultiple` | **3** | how far Flare throws the same radius. §19 calls Flare *"the exact mirror of Suppress"*, so once Suppress owned the radius the mirror was a multiplier, not a second mechanism |
| `FadeSeconds` | **0.2** | how fast it goes, both directions. Not instant, or it reads as a renderer fault rather than an act |

**Holding both is a contradiction, and Suppress wins.** You pay for both — the rates are additive, so `Q`+`E`
is 8 Resolve a second — and the lamp goes dark. Going dark is the safe resolution: the alternative hands
someone who fat-fingered a key a tripled lamp in a room full of things that hunt light.

> **It drives the *radius*, not the energy, and that is forced.** `DreamLightFlicker` writes `LightEnergy`
> every frame from a base it captured at `_Ready`, so anything else writing energy is overwritten before you
> ever see it — a feature that would look plainly broken with nothing in the log.
>
> **While you are lit it writes nothing**, so `,` and `.` still tune the lamp normally; the verb takes the
> channel over only while held and returns it to whatever you last set. That is gated in both directions.

> ⚠️ **Flare is the light, and only the light.** The verb is also supposed to *reveal hidden geometry* and to
> *let everything in the pocket see you*. Neither exists. The first wants a `PocketGate` variant that reads
> the **hold** (`IsHolding`) rather than the **grant** (`Holds`); the second is creature AI, which is
> unwritten. Worth knowing before concluding Flare is finished.

### `Space` — Float: two fields on the walker

`DreamProbeWalker` already carried the movement, so Float is two numbers in `_PhysicsProcess`.

| field | | |
|---|---|---|
| `FloatRiseSpeed` | **1.6** | metres per second of rise while held. A lift, not a jump — `Enums.cs` says *"slow rise and slight lateral drift"* |
| `FloatDrift` | **0.5** | the fraction of walking speed you keep in the air. Full control makes floating a better walk than walking; none at all makes every gated ledge sit directly above its launch point |

**Rise beats gravity outright** rather than fighting it — `velocity.Y = FloatRiseSpeed`, not a force added
against `Gravity`. Two numbers holding one decision is how you get a "float" that silently becomes a slow
fall the first time someone lowers the rise.

At 4 Resolve a second against a 100 bar, **a full meter buys about 25 seconds of rise** — roughly 40 m. That
pairing is what decides how high a pocket can gate something, so if you change either number, re-walk the
ledge. Running out drops you, which is the intended failure.

> **Float's movement has no gate, deliberately.** `_PhysicsProcess` and `MoveAndSlide` do not run in a
> headless verifier, so a check could only re-assert the arithmetic I just wrote. Play is the only proof:
> hold Space on the sandbox floor and you should reach the y = 7 `HighLedge` in about five seconds.

---

## What is in the sandbox

Built from scratch on 2026-08-07, to the contract `PocketHost` expects and with one of everything that
exists so far. It is **scaffolding, not content** — nothing in it ships, so change or delete anything.

| | |
|---|---|
| **`DreamContainer`** | `stretch_shrink = 4` and `texture_filter = Nearest`. **These two lines are the 480 × 270 look** — see the warning below before touching either |
| **Ground** | 60 × 60, one mesh with its own collision. Visible, because it is the thing you walk on |
| **`Bounds`** | `PocketBounds` at 60 × 20 × 60. Invisible walls and a lid. **This is the whole containment** |
| **Structures** | four blocks and a stub wall, as `CSGBox3D` with `use_collision` — things to walk around rather than a room to walk in, which is the open-floor reading |
| **`Ramp`** | 20°, walkable. `CharacterBody3D` **does not step up**, so this is the only way to gain height on foot |
| **`HighLedge`** | at y = 7, out of walking reach, carrying a `PocketGate` set to **Float** — so it is not there at all until you have the verb. Press **F11** to grant Float and Flare and watch it appear |
| **`Signal`** | the white sphere, on the ledge. Red in the editor, white in game — that is the chroma rule, not a bug |
| **`Landmark`** | a tall prism with its own light, far off, for navigating by silhouette |
| **Creatures** | one `dream_still` and one `dream_drifter`, both inert until you cross in as a pocket. Neither moves — **AI is the 3D pass and is not written** |
| **`Exit`** | a `PocketExit` at z = −24. Walk into it and you are back in the Waking world, having written nothing |
| **`Standalone`** | what makes **F6** a real visit. Enters the pocket, fills the bar, grants Float and Flare. Inert under F5 |
| **No `PocketAnchor`** | deliberately. An anchor calls `Complete()`, which writes `pocket.dream_probe.cleared` and **breaks the entrance for that save** — the same reason `F10` cannot complete |

> ### ⚠️ If the Dream looks tinted, it is the viewport resolution, not a colour
>
> **The look shader cannot emit colour.** Both of its exits are `vec3(x)`, so R, G and B come out equal by
> construction and no amount of lighting, fog or material changes that. **And you can still see green.**
>
> `stretch_shrink = 4` is what makes the pocket render at 480 × 270. Leave it off and `stretch` resizes the
> `SubViewport` to the full 1920 × 1080 — everything still draws, still looks monochrome, and the 8 × 8 Bayer
> cell lands on **8 screen pixels instead of 32**. A near-1-pixel alternating pattern lights alternating
> subpixels on an RGB-striped monitor, and green is the middle one. **Standing still you see grey; moving,
> the pattern crawls and it reads as colour** — strongest at the white/grey boundary, because that is where
> the dither is 50/50 and the pattern is finest.
>
> That is the whole of the "green fog" reported on 2026-08-07. It cost two rounds because it was diagnosed
> as pigment. **A rendering fault that is not in the render is one a screenshot cannot settle** — the frame
> was pure grey the entire time and measuring it would have confirmed nothing. When something is reported as
> *motion-dependent* and *sitting on a pattern edge*, suspect pixel scale first.
>
> Both lines are gated on **every** pocket scene now, so the other thirteen cannot repeat it.

---

## The boundary — two nodes, one shared file

**`PocketBounds`** contains the player; **`DreamEnvironment.tres`** is what the border looks like. They are
kept apart on purpose, so the border can be restyled without touching what stops you walking, and so an
unstyled pocket still cannot be escaped.

### Containing it

Drop a **`PocketBounds`** at floor level, in the middle of the room, and type the room's dimensions into
`Size`. You get four walls and a lid, built from collision and nothing else — no meshes, no material,
nothing to draw. The walls stand **outside** the size you typed, so the number is the space you get.

| field | | |
|---|---|---|
| `Size` | **40 × 12 × 40** | the clear volume, running *up* from the node. Centred in X and Z |
| `Thickness` | **2** | not a tunnelling guard — `CharacterBody3D` sweeps, so nothing passes a wall of any thickness. It is thick enough to be unambiguous |
| `BuildCap` | **on** | the lid that stops Float. Turn it off for a pocket with a real ceiling |
| `BuildFloor` | **off** | deliberately. A pocket has a floor you can see; a second invisible one under it would hide the seam you want to find |

> **It does not catch you when you fall through a seam** — that is the deferred C5 in `whats-left.md`, and
> when it lands it belongs on this node, built from the same `Size`.

### Showing it

Tick **`BorderShell`** on the same node. It draws each invisible wall as unshaded grey, at the wall, so the
boundary can be seen as well as felt.

**Then scrub `BorderValue` with `G` / `H` — do not try to pick it.** The first default was 0.55, reasoned
from the posterise threshold sitting near 0.5, and it came out as a flat slab filling the horizon. What the
shader reads is the *rendered luminance* of an unshaded surface, not the albedo you typed, and the two are
not close.

> **You are hunting the dither band, not a brightness.** The ordered dither shifts the threshold per pixel,
> so there is a range where only *some* pixels cross it and the wall reads as **crosshatch — a haze you can
> see through**. Above that band every pixel crosses and it is an opaque panel; below it, none do and there
> is no border at all. Two of those three are wrong and the good one is narrow. Starts at 0.30.
>
> **Unshaded is the feature, not a detail.** A *lit* wall out past the lamp's radius is black, which is
> precisely the invisible boundary this exists to fix.

### Why this is not fog — corrected 2026-08-07

**Depth fog was recommended for this job and cannot do it.** It measures distance from the *camera*, so it
travels with you: always clear for the first `fog_depth_begin` metres in every direction and always thick
past them, wherever you stand. It dims the far field; it can never mark a border, because nothing about it
knows where the border is. `FogVolume`, the node that *would* place fog in a box, is **Forward+ only** and
this project runs `gl_compatibility`.

So `DreamEnvironment.tres` ships with **`fog_enabled = false`**, values intact. Turn it back on if you want
the far field to fade as atmosphere — that it does well. Two things first:

- **It must be exactly grey.** Chroma at or above `signal_chroma` (0.04) is promoted to pure white, the one
  visual reserved for *go here*, so a tinted fog becomes a glowing wall the player reads as the objective.
  Gated; Godot's own default fog colour is bluish and trips it.
- **There are no gradients at `levels = 2`.** Fog can only push a pixel across one threshold, and the dither
  turns that into crosshatch. A dithered edge, not a soft fade.

> **If you ever see a *coloured* fog on screen, the fog is not the problem.** `DreamLook.gdshader` cannot
> output a colour at all — every path in it ends in grey or white. A tint means the look material is not on
> the container, and the usual reason is that the flood light (**`B`**) took it off.

---

## Seeing inside while authoring — use the editor's own switch

**There is nothing to build for this and nothing to remember to turn off.** In the 3D viewport, the
**Perspective** dropdown (top left) → **Display Unshaded** draws flat albedo with no lighting at all, so the
whole box is legible instantly. It is per-viewport and non-destructive.

**Do not raise `AmbientLightEnergy` in the inspector instead.** That value lives on the `Environment`
embedded in the pocket, so a `Ctrl+S` while it is up ships that pocket with the horror drained out of it, and
nothing anywhere would go red about it. `B` at runtime is safe for the same reason in reverse — runtime
changes are never written to disk.

---

## Seeing the look without running

The look pass lives on the container that draws the 3D image to the screen, **not inside the 3D
scene**. So the **3D** workspace tab shows you the raw, full-colour, un-posterised room — that is
normal and not a bug. Try the **2D** workspace tab with the scene open: it should show the posterised
result live, which makes it a decent way to nudge a light and see the effect without an F6 cycle.

If the 2D tab does not cooperate, ignore it — F6 is the ground truth and takes two seconds.

---

## Gotchas that cost real time

**Never set `own_world_3d` on the `DreamViewport`.** It was on at first, for tidiness — a private
`World3D` so the Dream's 3D never touched the main viewport's. The cost was not tidy at all: **nodes in
a private world do not appear in the editor's 3D view**, so the pocket could be run but never authored.
Sharing the parent viewport's world changes nothing at runtime — the main viewport has no `Camera3D`, so
it renders none of this — and makes every object selectable and draggable. Authorability beats
encapsulation here.

**`CharacterBody3D` does not step up.** A box even 0.25 m tall stops you dead; `move_and_slide` treats
the step face as a wall and slides along it. **Use slopes, not steps** — a `CSGBox3D` rotated to under
45° is walkable, and `Floor Max Angle` on the `Player` node raises that limit if you need steeper. If
kerbs and thresholds turn out to be something rooms genuinely want, step-up is a system that can be
built; it is deliberately not built yet, because the right time is after real authoring friction rather
than before.

**Do not put comments in a `.tscn`.** Godot rewrites the file whenever you save the scene in the editor
and strips them. Anything worth writing down goes in this file, in the shader, or in a `.cs` — all three
of which the editor leaves alone.

## Getting around the 3D editor

Click the **3D** tab at the top of the editor. You are looking at the room from outside.

| | |
|---|---|
| **Middle mouse + drag** | orbit |
| **Shift + middle mouse + drag** | pan |
| **Mouse wheel** | zoom |
| **Shift + F**, then `WASD` | freelook — fly the camera like a first-person game |
| **F** | frame the selected node (use this constantly — it is how you find things) |

For moving a selected node: `Q` select, `W` move, `E` rotate, `R` scale. In move mode you get three
coloured arrows — **red is X, green is Y (up), blue is Z**. Dragging an arrow constrains to that axis,
which is almost always what you want.

**Ctrl+S saves the scene.** Editor changes persist; runtime key changes do not.

---

## Where the knobs actually are

Select a node in the **Scene** panel (top left); its properties appear in the **Inspector** (right).

### The lights

| Node | What to try |
|---|---|
| `Player/PlayerLight` | **Light → Energy** (brightness), **Omni → Range** (radius, same as `,` `.`), **Shadow → Enabled**. This is the carried lamp |
| `FarLightLandmark` | the light that makes the landmark an island. Move it, and watch how much the landmark's readability depends on where it sits rather than on how bright it is |
| `FarLightSignal` | the same, for the white sphere's alcove |
| `WorldEnvironment` | click the **Environment** resource to expand it → **Ambient Light → Energy**. It is 0.04, almost nothing, on purpose — turn it up and you will see the horror drain out of the room immediately |

**Placing a light is an authoring decision, not a lighting chore.** Putting one somewhere says *this
matters, come here*. That is the whole reason a pocket costs no lighting work: the player carries the
near field, and you spend placed lights only where you want the eye to go.

### The greys

| Node | What to try |
|---|---|
| `Pocket/Floor` | **Material → Albedo → Color**. This material is shared by the floor, walls, pillars and ledge, so changing it here changes all of them at once — convenient, and worth knowing before it surprises you |
| `Pocket/Ceiling` | its own darker material. The ceiling is deliberately not the same value as the floor; a big flat plane at floor value eats the frame |
| `Signal` | the white sphere. Its material is **unshaded**, which is why it reads white even in the dark. Flip **Shading → Mode** to *Per-Pixel* if you would rather points of interest had to be approached to be seen — that is a real design choice and I have no opinion on it |
| `Creature` | albedo 0.03, near-black. The darkest thing in the room by rule |

> **Leave specular at 0 on materials and lights.** A highlight can push a wall past the signal
> threshold and the wall flashes white, which breaks the white reservation from the inside. It is the
> one setting here that can silently defeat the design rather than just look wrong.

### The shader defaults

Select `DreamContainer` → **Inspector → Material** → click the `ShaderMaterial` → **Shader
Parameters**. The same four values as the keys, with sliders. Changing them here and saving writes
`DreamLook.tres`, so this is where a settled value goes to live.

---

## What I actually need from you

The look is the one thing gates cannot check, so your eyes are the instrument. In rough order of how
much each answer unblocks:

1. **Is six greys enough, and does the dither read as texture or as noise?** Scrub `[` `]` and `- =`
   together. If things are hard to tell apart, suspect the *light placement* before the grey count —
   raising the count buys legibility at the price of the look, and it is nearly always the wrong fix.
2. **Does the white sphere read as "go here"?** It is the only white thing in the game by design. Too
   bright, too small, too much?
3. **Is the creature findable?** It reads well as a black hole against a lit wall and vanishes past
   the light radius. It has no rim light yet — tell me whether it needs one, because that decides
   whether creatures get a custom material.
4. **Does the landmark work from across the room?** With no minimap and no colour, silhouette is the
   only navigation there is.
5. **Is 2.2 m/s right?** Deliberate rather than twitchy is the intent, and slower is more frightening.
   It is `WalkSpeed` on the `Player` node if you want to try numbers.
6. **Is the room the right size?** Each pocket is visited once, so cost-per-pocket has to stay low —
   the useful question is how *small* a room can be and still read as a place.

F12 anything you like, and tell me the numbers. "Too dark" is a useful finding; "too dark at
greys 8, light 5.5" is one I can act on.
