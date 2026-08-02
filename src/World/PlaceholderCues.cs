using System.Collections.Generic;
using Godot;
using Unrendered.Core;

namespace Unrendered.World;

/// <summary>
/// The placeholder readability layer's palette and shared drawing helpers (unit 1.5,
/// <c>build-plan.md</c>). Over ~59 unpainted screens the question that actually costs time is
/// <i>which room am I standing in and what is clickable</i>, and neither is answerable from a grey
/// box. This makes both readable without authoring a single asset.
///
/// <para>
/// <b>It is placeholder-only, and it switches itself off.</b> Cues draw when a screen has no art,
/// not when a flag is set — paint the screen and they vanish for it. The shipping affordance is a
/// hover tint on painted art and already exists in <see cref="Hotspot"/>; kind colours are
/// deliberately *not* promoted into it (build-plan.md — promoting later is one line, demoting is not).
/// </para>
///
/// <para>
/// <b>Why these hexes.</b> The backdrop decided it, not taste. <see cref="PlaceholderBackdrop"/>
/// draws two palettes — uncleansed cold slate and cleansed sky-blue over sage — both desaturated, so
/// saturated colour pops on either. The two hues needing care are mid-value green (vanishes on
/// purified ground) and mid-value blue (vanishes on purified sky), which is why
/// <see cref="HotspotKind.Harvest"/> is a pushed lime rather than the obvious herb green.
/// </para>
///
/// <para>
/// <b>The label carries the meaning; the colour is redundant coding.</b> Nobody has to learn that
/// lime means harvest, because the <c>Label</c> is drawn in place. That is also what makes this
/// colour-blind-safe without contortion: the riskiest pair under deuteranopia is crimson/lime, and
/// they are separated by value as well as by the text on top. <b>If a kind ever loses its label it
/// loses its safety net</b> — that is the constraint to keep, not the specific hexes.
/// </para>
/// </summary>
public static class PlaceholderCues
{
    /// <summary>
    /// One colour per <see cref="HotspotKind"/>.
    ///
    /// <para>
    /// A dictionary rather than a <c>switch</c> on purpose. A switch needs a <c>_ =&gt;</c> arm to
    /// compile, and that arm silently absorbs every kind added afterwards — the gate asserting
    /// totality could then never fail, which is the "unfalsifiable guard" trap recorded in
    /// <c>handoff.md</c>. Missing keys are visible here. <b>Unit 4 adds <c>Station</c>, so this
    /// fires for real within two units.</b>
    /// </para>
    /// </summary>
    public static readonly IReadOnlyDictionary<HotspotKind, Color> KindColors =
        new Dictionary<HotspotKind, Color>
        {
            // Continuity with the old single blue outline, and exits are what you scan a new screen for.
            [HotspotKind.Navigate] = new("#38d6ff"),

            // "Take this." High-value on purpose, to separate it from purified ground.
            [HotspotKind.Harvest] = new("#a3e635"),

            // The fight verb. Nothing in either backdrop palette is red.
            [HotspotKind.Entity] = new("#ff4d4d"),

            // Something happens, but it is not taking or moving.
            [HotspotKind.Interact] = new("#ffa83c"),

            // Deliberately the quietest — the verb where nothing happens should not compete.
            [HotspotKind.Examine] = new("#c8ccd2"),

            // The camp; the one hotspot you are glad to see.
            [HotspotKind.Workbench] = new("#ffd24a"),

            // Otherworldly, and absent from both backdrop palettes.
            // Provisional: unit 6 retires the heist system that owns this kind.
            [HotspotKind.CrossToDream] = new("#b07cff"),

            // The objective. Provisional, same reason.
            [HotspotKind.TakeAnchor] = new("#ffffff"),
        };

    /// <summary>
    /// Falls back to magenta rather than to something plausible. An unmapped kind is a bug, and a
    /// bug that looks like a design choice on screen is one nobody reports.
    /// </summary>
    public static Color ColorFor(HotspotKind kind) =>
        KindColors.TryGetValue(kind, out var c) ? c : Colors.Magenta;

    /// <summary>
    /// A hotspot that exists but is shut. Drawn instead of being omitted, so a flag gate can be
    /// tested by walking into the room rather than by opening the inspector — this is the ledger's
    /// <b>Blocked</b> column made visible.
    /// </summary>
    public static Color Locked(HotspotKind kind)
    {
        var c = ColorFor(kind);
        // Desaturated toward the backdrop and dropped in value: still identifiably its own kind,
        // clearly not available. Alpha is applied by the caller per element.
        return c.Lerp(new Color("#6b7280"), 0.62f);
    }

    /// <summary>
    /// The font every cue draws with. <c>ThemeDB</c>'s fallback rather than a theme lookup, because
    /// these are drawn from a <see cref="Node2D"/> as well as from Controls, and none of it should
    /// depend on a theme that only exists for the shipped UI.
    /// </summary>
    public static Font Font => ThemeDB.Singleton.FallbackFont;

    /// <summary>
    /// Text with a hard dark backing, drawn at <paramref name="baseline"/>.
    ///
    /// <para>
    /// The backing is not decoration. These cues sit on top of a procedural backdrop whose local
    /// value is unknown and changes when a zone cleanses — light text on the sage ground and dark
    /// text on the sky both disappear. A slab under the glyphs makes the label readable against
    /// anything underneath it, which is the one property this layer cannot do without.
    /// </para>
    /// </summary>
    public static void DrawLabel(CanvasItem where, Vector2 baseline, string text, Color colour,
                                 int fontSize = 22, float alpha = 1f)
    {
        if (string.IsNullOrEmpty(text)) return;

        var font = Font;
        if (font == null) return;

        var extent = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var pad = new Vector2(6f, 4f);

        where.DrawRect(
            new Rect2(baseline - new Vector2(pad.X, extent.Y * 0.80f + pad.Y),
                      extent + pad * 2f),
            new Color(0.04f, 0.05f, 0.06f, 0.72f * alpha));

        where.DrawString(font, baseline, text, HorizontalAlignment.Left, -1, fontSize,
            new Color(colour.R, colour.G, colour.B, alpha));
    }

    /// <summary>Width of a label's backing slab, for laying two of them out side by side.</summary>
    public static float LabelWidth(string text, int fontSize = 22)
    {
        var font = Font;
        if (font == null || string.IsNullOrEmpty(text)) return 0f;
        return font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X + 12f;
    }
}
