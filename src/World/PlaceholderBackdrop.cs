using System;
using Godot;
using Unrendered.Core;

namespace Unrendered.World;

/// <summary>
/// Draws a room procedurally when no painted background exists yet. This is not
/// programmer-art-as-apology: it is deliberately readable, so zone purification is legible
/// as a *change* long before a single asset is authored.
///
/// Everything is drawn in normalised proportions of <see cref="Control.Size"/>, so it does not
/// care what it is sized to.
/// </summary>
public partial class PlaceholderBackdrop : Control
{
    private Color _sky = Colors.DimGray;
    private Color _mid = Colors.Gray;
    private Color _ground = Colors.DarkGray;
    private Color _accent = Colors.White;
    private float _fog;
    private BackdropStyle _style = BackdropStyle.Wilderness;
    private uint _seed = 1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public void Configure(BackdropStyle style, Color sky, Color mid, Color ground, Color accent,
                          float fog, string seedSource)
    {
        _style = style;
        _sky = sky;
        _mid = mid;
        _ground = ground;
        _accent = accent;
        _fog = fog;
        _seed = HashSeed(seedSource);
        QueueRedraw();
    }

    // ---- Deterministic noise ---------------------------------------------
    // Same room always looks the same. xorshift32 rather than Random so there is no
    // allocation and no dependence on run order.

    private uint _rngState;

    private void ResetRng() => _rngState = _seed == 0 ? 1u : _seed;

    private float NextFloat()
    {
        _rngState ^= _rngState << 13;
        _rngState ^= _rngState >> 17;
        _rngState ^= _rngState << 5;
        return (_rngState & 0xFFFFFF) / (float)0x1000000;
    }

    private float Range(float lo, float hi) => lo + NextFloat() * (hi - lo);

    private static uint HashSeed(string s)
    {
        unchecked
        {
            uint h = 2166136261u;
            if (!string.IsNullOrEmpty(s))
                foreach (var c in s) h = (h ^ c) * 16777619u;
            return h == 0 ? 1u : h;
        }
    }

    // ---- Drawing ---------------------------------------------------------

    public override void _Draw()
    {
        ResetRng();

        var w = Size.X;
        var h = Size.Y;
        if (w <= 0 || h <= 0) return;

        switch (_style)
        {
            case BackdropStyle.Interior: DrawInterior(w, h); break;
            default: DrawWilderness(w, h); break;
        }
    }

    private void DrawWilderness(float w, float h)
    {
        var horizon = h * 0.55f;

        DrawSkyGradient(w, horizon);
        DrawRect(new Rect2(0, horizon, w, h - horizon), _ground);

        // Two treelines. The far one is washed toward the sky colour for depth.
        DrawTreeline(w, horizon, h * 0.16f, _mid.Lerp(_sky, 0.55f), horizon - h * 0.01f, 26);
        DrawTreeline(w, horizon, h * 0.26f, _mid.Darkened(0.35f), horizon + h * 0.03f, 15);

        DrawTrack(w, h, horizon);

        // Mist gathers along the ground and thins upward.
        DrawFogBands(w, h, horizon);
    }

    private void DrawInterior(float w, float h)
    {
        var floor = h * 0.68f;

        // No sky indoors. Side walls run off both edges of frame.
        DrawRect(new Rect2(0, 0, w, floor), _mid.Darkened(0.45f));
        DrawRect(new Rect2(0, floor, w, h - floor), _ground);

        // Back wall, catching what light there is, with a doorway punched through it.
        var wall = new Rect2(0, h * 0.06f, w, floor - h * 0.06f);
        DrawRect(wall, _mid);

        var doorW = w * 0.14f;
        var doorH = floor - h * 0.28f;
        var door = new Rect2(w * 0.5f - doorW * 0.5f, floor - doorH, doorW, doorH);
        DrawRect(door, _ground.Darkened(0.5f));
        DrawRect(door, _accent, filled: false, width: MathF.Max(1f, w * 0.0015f));

        // Stonework, so the wall is not a flat slab. Courses stop at the doorway.
        const int rows = 11;
        for (var r = 1; r < rows; r++)
        {
            var y = wall.Position.Y + wall.Size.Y * r / rows;
            var colour = _mid.Darkened(0.22f);
            var thickness = MathF.Max(1f, h * 0.002f);

            if (y > door.Position.Y)
            {
                DrawLine(new Vector2(0, y), new Vector2(door.Position.X, y), colour, thickness);
                DrawLine(new Vector2(door.End.X, y), new Vector2(w, y), colour, thickness);
            }
            else
            {
                DrawLine(new Vector2(0, y), new Vector2(w, y), colour, thickness);
            }
        }

        // Floor boards, converging slightly.
        for (var i = 1; i < 5; i++)
        {
            var t = i / 5f;
            var y = Mathf.Lerp(floor, h, t * t);
            DrawLine(new Vector2(0, y), new Vector2(w, y), _ground.Darkened(0.3f), MathF.Max(1f, h * 0.002f));
        }

        DrawFogBands(w, h, floor);
    }

    // `DrawDream` — a void sun, a five-deep colonnade and a converging tile floor, drawn flat with
    // no atmosphere. Deleted 2026-08-02: the Dream is 3D, so a fake 2D perspective of it is not a
    // placeholder for anything that will ever exist. Kept in git if the *composition* is ever wanted
    // as reference for the real pocket.

    // ---- Pieces ----------------------------------------------------------

    /// <summary>
    /// Faked with stacked bands rather than a gradient shader. Enough of them and the
    /// stepping stops reading as stepping.
    /// </summary>
    private void DrawSkyGradient(float w, float horizon)
    {
        const int bands = 32;
        var bandHeight = horizon / bands;

        for (var i = 0; i < bands; i++)
        {
            var t = i / (float)(bands - 1);
            DrawRect(new Rect2(0, bandHeight * i, w, bandHeight + 1f), _sky.Lerp(_mid, t * 0.7f));
        }
    }

    private void DrawTreeline(float w, float horizon, float maxHeight, Color colour, float baseY, int count)
    {
        var pts = new Vector2[count * 2 + 2];
        var idx = 0;
        pts[idx++] = new Vector2(0, baseY);

        for (var i = 0; i < count; i++)
        {
            var x0 = w * i / count;
            var x1 = w * (i + 0.5f) / count;
            pts[idx++] = new Vector2(x0, baseY - maxHeight * Range(0.35f, 1.0f));
            pts[idx++] = new Vector2(x1, baseY - maxHeight * Range(0.15f, 0.6f));
        }

        pts[idx] = new Vector2(w, baseY);
        DrawColoredPolygon(pts, colour);
    }

    private void DrawTrack(float w, float h, float horizon)
    {
        var track = new[]
        {
            new Vector2(w * 0.24f, h),
            new Vector2(w * 0.76f, h),
            new Vector2(w * 0.532f, horizon),
            new Vector2(w * 0.468f, horizon),
        };
        DrawColoredPolygon(track, _ground.Lightened(0.12f));
    }

    private void DrawFogBands(float w, float h, float horizon)
    {
        if (_fog <= 0f) return;

        // Many thin, heavily overlapping bands: the accumulated alpha smooths into haze,
        // where a handful of thick ones just look like stripes.
        const int bands = 26;
        var spread = h * 0.34f;
        var top = horizon - h * 0.12f;
        var bandHeight = spread / bands * 3.2f;

        for (var i = 0; i < bands; i++)
        {
            var t = i / (float)(bands - 1);
            var alpha = _fog * (1f - t) * (1f - t) * 0.085f;
            DrawRect(new Rect2(0, top + spread * t, w, bandHeight), new Color(_accent, alpha));
        }
    }
}
