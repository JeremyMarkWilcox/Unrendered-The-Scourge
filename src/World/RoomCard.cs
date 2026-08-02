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
    private string _id = "";
    private string _display = "";
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
        _display = room.DisplayName ?? "";
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

        var lines = new List<(string Text, int Size, Color Colour)>
        {
            (_id, 26, new Color("#ffd24a")),
            (_display, 20, new Color("#e8eaee")),
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

        var origin = new Vector2(24f, 24f);
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
