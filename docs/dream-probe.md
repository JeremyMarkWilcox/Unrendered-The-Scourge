# The Dream probe — how to drive it

A practical guide to `scenes/dream/DreamProbe.tscn`, written for a first go at Godot's 3D side.
The design reasoning is in `docs/combat.md` §14 (the Dream is stealth, and its creatures are models) and
§15 (the pockets); this file is only *which button*.

**The one thing to know first: you do not need the 3D editor to tune the look.** Every value worth
arguing about is on a key while the scene is running. The editor is for *moving things* — lights,
walls, the landmark. Those are two different sessions and it is worth keeping them separate.

---

## Running it

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
| `, .` | **light** | the player light's radius. This is also the Resolve degrade in its entirety — when the drain is built, the dark closing in *is* this number falling |

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
| `F4` | | hide the readout, for a clean screenshot |
| `F12` | | save a frame to `.shots/`, named after the values that produced it |

> There is no signal-threshold key any more. Points of interest used to be detected by *brightness*,
> which leaked — a wall near a bright light would flash white. They are detected by **chroma** now:
> the world is greyscale, so anything you tint is a point of interest and nothing else can ever be
> one. **Paint a material any colour and it becomes white in game.** The sphere is red in the editor
> and white when you play.

**Nothing you scrub at runtime is saved.** That is why the readout prints to the Output panel and why
F12 puts the numbers in the filename — so a value you like can be found again. Send me the numbers, or
just the filenames, and I will write them into `DreamLook.tres` as the new defaults.

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
