using System.Collections.Generic;
using System.Text;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.World;

/// <summary>
/// A corner plate naming the screen you are standing in, drawn only while the screen is unpainted
/// (unit 1.5, <c>build-plan.md</c>).
///
/// <para>
/// <b>Why this is the first of the five pieces, not the last.</b> Over ~59 grey screens,
/// <i>which one am I standing in</i> is a worse problem than <i>what is this hotspot</i> — the
/// hotspots at least have shapes and positions to tell them apart, and the rooms do not. A room id
/// on screen also turns "the door went somewhere wrong" from an inspector session into a glance.
/// </para>
///
/// <para>
/// <b>Drawn, never baked.</b> Every string here is read from the live <see cref="RoomData"/> each
/// frame it changes, so it cannot go stale the way a label baked into a placeholder PNG does when
/// the room is renamed in the inspector. A drawn label reads the resource, so it cannot lie.
/// </para>
/// </summary>
public partial class RoomCard : Control
{
    /// <summary>
    /// How far above the bottom edge the plate sits, clearing the HUD's debug-key hint strip.
    /// </summary>
    /// <remarks>
    /// The hint is anchored <c>BottomLeft</c> at <c>y = -44</c> with a 16px font, so it occupies roughly the
    /// last 60 pixels. 64 clears it with a hair to spare and no overlap as the plate grows a line.
    /// </remarks>
    private const float BottomInset = 64f;

    /// <summary>
    /// Where the plate's top-left corner goes, for a viewport of this height and a plate of this height.
    /// <b>Static and pure so a gate can ask it with no layout pass</b> — a headless run leaves <c>Size</c> at
    /// zero, which is exactly why the collision this fixes was invisible to 1095 green checks.
    /// </summary>
    /// <remarks>
    /// <b>The card moved from the top-left to the bottom-left on 2026-08-05, and the reason is ownership
    /// rather than tidiness.</b> Jeremy, from a screenshot: *"I don't like how the room display lines up with
    /// the debug one."* Both were anchored to the same corner — the HUD's <c>TopLeft</c> VBox at <c>(40, 32)</c>
    /// and this plate drawn at a flat <c>(24, 24)</c> — so the HUD's 30px room name sat across this card's id,
    /// and this card's second line sat across "WAKING WORLD". Neither was readable.
    /// <para>
    /// <b>The top-left belongs to the HUD because the HUD ships and this does not.</b> Unit 1.5 settled that
    /// this whole layer is debug-only — it draws when there is no art rather than when a flag is set — so when
    /// scaffolding and chrome want the same space, the scaffolding is what moves. The bottom-left is the right
    /// destination specifically because the debug-key hint strip already lives there: **the plate now sits
    /// directly above the F-key line, so all of the scaffolding shares one corner** and the shipping chrome has
    /// the other three to itself.
    /// </para>
    /// <para>
    /// Derived from the plate's measured height rather than a chosen y, so adding or removing a line cannot
    /// walk it back into something. The x is 24 so that the text inset of 16 puts the strings at 40 — the same
    /// left margin the hint strip and the HUD both use.
    /// </para>
    /// </remarks>
    public static Vector2 PlateOrigin(float viewportHeight, float plateHeight) =>
        new(24f, viewportHeight - BottomInset - plateHeight);

    private string _id = "";
    private string _chapter = "";
    private readonly List<string> _exits = new();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Hidden entirely on a painted screen — the card is scaffolding, not chrome.</summary>
    public void Show(RoomData room, bool visible)
    {
        Visible = visible && room != null;
        if (!Visible) return;

        _id = room.Id ?? "";
        _chapter = room.ChapterId ?? "";

        // The exit list is the room's own doors, available or not: a door held shut by a flag is
        // still a fact about the map, and the map is what this card is for.
        _exits.Clear();
        foreach (var h in room.Hotspots)
        {
            if (h is not { Kind: HotspotKind.Navigate }) continue;
            if (string.IsNullOrEmpty(h.TargetRoomId)) continue;
            if (!_exits.Contains(h.TargetRoomId)) _exits.Add(h.TargetRoomId);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var font = PlaceholderCues.Font;
        if (font == null) return;

        // **`DisplayName` is deliberately not here, removed 2026-08-05.** The HUD prints it at 30px in the
        // top-left on every screen, painted or not, so a second copy was the duplication half of the overlap
        // Jeremy reported — and it was the *larger* half, because two labels reading "His Cottage" in different
        // fonts looks like a bug even once they stop touching. What this card is for is the three things the
        // shipping HUD deliberately does **not** say: the **id**, the chapter, and where the doors go. Losing
        // the name costs nothing, since it is on screen either way.
        var lines = new List<(string Text, int Size, Color Colour)>
        {
            (_id, 26, new Color("#ffd24a")),
        };

        if (!string.IsNullOrEmpty(_chapter))
            lines.Add(($"chapter: {_chapter}", 16, new Color("#9aa3ad")));

        lines.Add((ExitLine(), 16, PlaceholderCues.ColorFor(HotspotKind.Navigate)));

        // Measure first so the plate fits the longest line rather than a guessed width.
        var width = 0f;
        var height = 14f;
        foreach (var (text, size, _) in lines)
        {
            width = Mathf.Max(width, font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X);
            height += size + 9f;
        }

        var origin = PlateOrigin(Size.Y, height);
        DrawRect(new Rect2(origin, new Vector2(width + 32f, height)), new Color(0.04f, 0.05f, 0.06f, 0.74f));
        DrawRect(new Rect2(origin, new Vector2(width + 32f, height)),
            new Color(1f, 1f, 1f, 0.10f), filled: false, width: 2f);

        var y = origin.Y + 12f;
        foreach (var (text, size, colour) in lines)
        {
            y += size;
            DrawString(font, new Vector2(origin.X + 16f, y), text, HorizontalAlignment.Left, -1, size, colour);
            y += 9f;
        }
    }

    private string ExitLine()
    {
        if (_exits.Count == 0) return "exits: none";

        var sb = new StringBuilder("exits: ");
        for (var i = 0; i < _exits.Count; i++)
        {
            if (i > 0) sb.Append("  ·  ");
            sb.Append(_exits[i]);
        }
        return sb.ToString();
    }
}
