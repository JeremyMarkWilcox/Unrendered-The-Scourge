// The Dream's look pass — greyscale, posterise, ordered dither, and the white reservation.
//
// Lives on the `SubViewportContainer`, so it sees the finished 480x270 image and nothing in the
// 3D scene has to know it exists. Authored greyscale, dithered here: greyscale posterises down to
// 1-bit trivially and 1-bit can never be recovered upward, so doing it in this order keeps the
// choice reversible forever (`handoff.md`, "The Dream in 3D", step 1).
//
// THE RAMP IS SEMANTIC, and this shader is where that is enforced rather than asked for. Ordinary
// surfaces physically cannot reach white; a mistake becomes impossible instead of merely
// discouraged — the same move as camp-ness derived from the bench.
//
//   the world      two-or-more grey bands, capped at `white_clamp` — never white
//   notable things promoted to pure white, and the only things that can be
//   take vs flee   split by PATTERN, not value: items are solid, creatures are line-noise
//
// REVISED 2026-07-30, AND THE TIER COUNT CHANGED. It used to be three *value* tiers, with creatures
// reserved the bottom. At `levels = 2` the world has exactly two values and there is no third for a
// creature to be, so creatures moved off value entirely and onto pattern — see
// `DreamCreature.gdshader`. What is left here is two value tiers plus a promotion rule.
//
// NOTABLE IS DETECTED BY CHROMA, NOT BRIGHTNESS — changed 2026-07-30 after the first tuning pass.
// It used to be a luminance threshold, and that leaks: a wall close to a bright light exceeds any
// threshold you pick and flashes pure white, which is the environment defeating the reserve from the
// inside. Raising the threshold to suppress it switches the white tier off altogether, which is what
// happened in play. Chroma cannot leak — the world is authored greyscale, so no amount of light gives
// it saturation, and a material is notable exactly when someone tinted it. It also restores what the
// art direction said first: *in a colourless realm, one coloured object screams.* Side benefit worth
// keeping: points of interest are visibly coloured in the editor and white in the game, so the
// authoring affordance is "paint it any colour and it becomes a point of interest".
//
// THREE PARAMETERS TO SCRUB — `levels`, `white_clamp`, `dither_strength`. `signal_chroma` is a
// noise floor, not a look knob, and is deliberately not on a key. There is no
// dither-rising-with-distance: falloff owns distance now, and a canvas_item pass over a SubViewport
// has no depth buffer to have done it with anyway.
shader_type canvas_item;
render_mode unshaded;

// How many greys the world gets. Two bands are spent on the reserved ends, so the floor is around
// six — at four, the world gets two, and two cannot separate an object from the wall behind it.
uniform int levels : hint_range(2, 16) = 6;

// The ceiling on ordinary surfaces. Below 1.0 by definition; this is the reservation.
uniform float white_clamp : hint_range(0.4, 1.0) = 0.82;

// Saturation above which a pixel counts as notable. A noise floor, not a look knob: the world is
// authored greyscale so its chroma is exactly zero, and anything tinted at all is far above this.
uniform float signal_chroma : hint_range(0.0, 0.5) = 0.04;

// Ordered-dither amplitude. Trades against `levels` — a heavily dithered six can read richer than
// a clean twelve, because dithering between adjacent bands simulates the values in between. With
// both ends of the ramp reserved the world has a compressed value band, so the pattern rather than
// the value does most of the texture work. Tune the two together, never in sequence.
uniform float dither_strength : hint_range(0.0, 1.0) = 0.7;

// 8x8 Bayer, unnormalised so the table reads like the matrix it is.
const float BAYER[64] = {
	0.0,  32.0,  8.0, 40.0,  2.0, 34.0, 10.0, 42.0,
	48.0, 16.0, 56.0, 24.0, 50.0, 18.0, 58.0, 26.0,
	12.0, 44.0,  4.0, 36.0, 14.0, 46.0,  6.0, 38.0,
	60.0, 28.0, 52.0, 20.0, 62.0, 30.0, 54.0, 22.0,
	3.0,  35.0, 11.0, 43.0,  1.0, 33.0,  9.0, 41.0,
	51.0, 19.0, 59.0, 27.0, 49.0, 17.0, 57.0, 25.0,
	15.0, 47.0,  7.0, 39.0, 13.0, 45.0,  5.0, 37.0,
	63.0, 31.0, 55.0, 23.0, 61.0, 29.0, 53.0, 21.0
};

void fragment() {
	// Read COLOR rather than sampling TEXTURE: on entry it is already the texture times the
	// modulate chain, so `src.a` carries the Dream layer's fade-in and this pass keeps working
	// unchanged when `RoomView` cross-fades the realms. The modulate only touches alpha during a
	// fade, so luminance is unaffected by it. (There is no `MODULATE` built-in to multiply by
	// afterwards — that was the first attempt and it does not compile.)
	vec4 src = COLOR;
	float chroma = max(max(src.r, src.g), src.b) - min(min(src.r, src.g), src.b);

	if (chroma >= signal_chroma) {
		// Notable. Snapped clean to the top of the ramp and deliberately never dithered: this is the
		// one signal in the game that has to survive being four pixels wide.
		COLOR = vec4(vec3(1.0), src.a);
	} else {
		float lum = dot(src.rgb, vec3(0.2126, 0.7152, 0.0722));

		// The dither must sit on the 480x270 grid, not the 1920x1080 one, or the pattern is
		// four times too fine and reads as noise instead of as texture. FRAGCOORD is in final
		// screen pixels here, so derive the viewport pixel from UV and the source size instead.
		vec2 px = floor(UV * vec2(textureSize(TEXTURE, 0)));
		ivec2 cell = ivec2(int(mod(px.x, 8.0)), int(mod(px.y, 8.0)));
		float d = (BAYER[cell.y * 8 + cell.x] / 64.0) - 0.5;

		// The world's input range is the whole of [0, 1] now. It used to be divided by the old
		// luminance threshold to normalise against it, which is one of the things that made a bright
		// wall able to reach the top of the ramp in the first place.
		float steps = float(levels - 1);
		float v = clamp(lum, 0.0, 1.0);
		float q = clamp(floor(v * steps + d * dither_strength + 0.5) / steps, 0.0, 1.0);

		COLOR = vec4(vec3(q * white_clamp), src.a);
	}
}
