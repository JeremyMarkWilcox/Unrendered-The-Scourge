// Creatures. Line noise rather than a value, because at two greys value has run out.
//
// THE ARGUMENT, because it overturns a written plan. The ramp reserved the *bottom* for creatures and
// the plan of record was a rim light for finding them in the dark. Both assumed the world had spare
// values. At `levels = 2` it has exactly two — {0, clamp} — so a dark creature is not "the darkest
// thing", it is one of the only two things, and rim light has no gradient to sit on. Jeremy's fix
// routes around the dead end instead of tuning it: the world's texture is *ordered* (regular Bayer
// crosshatch), so a creature of *random* lines separates by pattern regularity at any value, against
// any background. Value is saturated; pattern is free.
//
// Three things that buys, in order of how much they matter:
//
//   1. Background independence. A value-based creature has to be authored against its surroundings —
//      every room would need a lit surface behind every creature. This one does not care. That is a
//      production saving of the same kind as the player light making a pocket cost zero lighting work.
//   2. It deletes an exception. Rim light was recorded as "the one exception to the white-clamp".
//   3. A creature that fails to render, in a game called Unrendered. The dither trick again: an
//      artifact of the technique doing the narrative work.
//
// UNSHADED, DELIBERATELY. Lighting must not touch a creature, or background independence is a lie —
// it would go dark past the player's radius, which is the exact failure this replaces.
//
// HOW IT REACHES WHITE, AND WHY THAT IS NOT A HACK. The look pass promotes any pixel carrying chroma
// to pure white; ordinary surfaces are greyscale and cannot. So a lit line is *tinted*, not bright,
// and the pass whitens it. The rule the two shaders share is "chroma means notable", and what splits
// notable into take-this and flee-this is **pattern, not value**: items are solid white, creatures are
// lined. That matters more than it sounds — the mindless creatures are specified as anchored or
// aimless, so a stationary one must still be tellable from an item, which motion alone would not do.
shader_type spatial;
render_mode unshaded;

// Tint of a lit line. Any colour works; only the *presence* of chroma is read. Never appears on
// screen — the look pass replaces it with white.
uniform vec3 signal_tint : source_color = vec3(1.0, 0.15, 0.15);

// false: lines are locked to the body, so the pattern travels with the creature — for the mindless
// ones you walk around at leisure.
// true: lines are horizontal in screen space, so the creature reads as a hole through which static
// shows and does not obey the space — for bosses, the same argument as their being billboards.
uniform bool screen_space_lines = false;

// Bigger is chunkier in both modes: viewport pixels per line in screen space, and inversely
// proportional to lines-per-metre on the body.
uniform float line_thickness : hint_range(1.0, 16.0) = 3.0;

// Fraction of lines that are lit. 0.5 is even; low is a mostly-absent thing, high a mostly-solid one.
uniform float line_density : hint_range(0.0, 1.0) = 0.5;

// HOW MUCH THE LINES REFUSE TO LINE UP. At 0 a line is lit or unlit along its whole width, which reads
// as a barcode and reveals the trick. Raise it and lines break into independently-rolled cells, so the
// body tears rather than stripes — stripes become static. This is the difference between a *pattern*
// on a creature and a creature that is *failing to resolve*.
uniform float line_break : hint_range(0.0, 1.0) = 0.0;
uniform float break_size : hint_range(1.0, 64.0) = 10.0;

// Re-rolls per second. 0 is static and reads as a texture; high strobes and fights the Bayer pattern.
// A slow crawl is the interesting middle — unstable enough to feel alive, slow enough to read.
uniform float crawl_hz : hint_range(0.0, 30.0) = 7.0;

// When false, lit lines are grey at `capped_value` and go through the ramp like scenery instead of
// being promoted to white. This is the alternative to spending the white reserve on creatures; kept
// because it is one toggle and the comparison is worth having rather than arguing about.
uniform bool reach_white = true;
uniform float capped_value : hint_range(0.0, 1.0) = 0.4;

// BODY MODE ONLY. 0 keeps lines fixed in the model, so a band's *pixel* size grows as you approach
// and shrinks — eventually below one pixel — as you retreat. 1 keeps a band's pixel size constant
// instead, which means bands are added as you close in: the creature **resolves as you approach**,
// and can never shimmer at range. Two different answers to "should they change with distance", and
// they are opposites, so look rather than reason.
uniform float distance_compensation : hint_range(0.0, 1.0) = 0.0;
uniform float reference_distance : hint_range(0.5, 20.0) = 3.0;

// A camera-facing quad instead of geometry — the cheap half of the billboard question. Answers
// whether *not obeying the space* reads as otherworldly, which needs no art; it cannot answer
// whether a *drawn* creature out-strikes a model, which needs a drawing.
uniform bool billboard = false;

// Procedural stand-in silhouette for the billboard, so the test is a creature-shaped thing rather
// than a floating rectangle. Delete the moment real art exists.
uniform bool silhouette_from_uv = false;

varying vec3 body_position;

// Per-instance, derived from where the thing stands. Without it every creature sharing a material rolls
// the *same* pattern, so two of them side by side have bands at identical heights — which reads as one
// effect applied to the screen rather than as two separate broken things. Derived rather than authored
// so a creature dropped into a room is never accidentally in phase with its neighbour.
varying float instance_seed;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void vertex() {
	body_position = VERTEX;
	instance_seed = fract(dot(MODEL_MATRIX[3].xyz, vec3(0.7311, 0.4523, 0.9137)) * 13.71) * 97.0;

	if (billboard) {
		// Rebuild the basis from the inverse view matrix so the quad always faces the camera, keeping
		// only the model's translation. This is the defect the billboard is being tested *for*: it
		// does not obey the space.
		MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			INV_VIEW_MATRIX[0],
			INV_VIEW_MATRIX[1],
			INV_VIEW_MATRIX[2],
			MODEL_MATRIX[3]);
	}
}

void fragment() {
	if (silhouette_from_uv) {
		// A vertical capsule in UV space, so the billboard is a creature-shaped hole rather than a
		// rectangle and the comparison against the 3D capsule is about facing, not about shape.
		vec2 p = (UV - 0.5) * 2.0;
		if (length(vec2(p.x, max(abs(p.y) - 0.45, 0.0))) > 0.55) {
			discard;
		}
	}

	float line;
	float across;
	if (screen_space_lines) {
		// FRAGCOORD here is in the SubViewport's own 480x270 pixels, not the upscaled window, so
		// screen space means the real grid and needs no correction — unlike DreamLook's canvas pass.
		line = floor(FRAGCOORD.y / line_thickness);
		across = FRAGCOORD.x;
	} else {
		// VERTEX is view-space in fragment, so its length is the distance to the camera.
		float lines_per_metre = 30.0 / line_thickness;
		lines_per_metre *= mix(
			1.0,
			reference_distance / max(length(VERTEX), 0.5),
			distance_compensation);
		line = floor(body_position.y * lines_per_metre);
		across = body_position.x * lines_per_metre;
	}

	float step_index = floor(TIME * crawl_hz);
	float cell = floor(across / break_size);

	// Two rolls: one shared by the whole line, one owned by this cell. Which a cell obeys is itself a
	// stable per-cell draw, so `line_break` reads as *how torn* the creature is rather than as a
	// crossfade between two patterns — and the tearing pattern holds still while the lighting of it
	// crawls.
	float shared = hash(vec2(line + instance_seed, step_index));
	float own = hash(vec2((line * 131.0) + cell + instance_seed, step_index + 3.7));
	float torn = hash(vec2((line * 17.0) + cell, instance_seed + 5.3));

	float roll = torn < line_break ? own : shared;
	float lit = roll < line_density ? 1.0 : 0.0;

	ALBEDO = mix(vec3(0.0), reach_white ? signal_tint : vec3(capped_value), lit);
}
