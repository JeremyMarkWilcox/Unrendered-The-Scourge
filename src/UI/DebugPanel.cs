using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Unrendered.Core;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// Live dump of world state. The fastest way to tell whether a signal actually fired, and
/// the thing that makes the milestone gates checkable without a debugger.
/// </summary>
public partial class DebugPanel : PanelContainer
{
    private Label _body;
    private float _tick;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.TopLeft);
        Position = new Vector2(40, 140);
        CustomMinimumSize = new Vector2(560, 0);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.03f, 0.04f, 0.06f, 0.86f),
            BorderColor = new Color(0.35f, 0.85f, 1f, 0.45f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        };
        style.SetBorderWidthAll(1);
        AddThemeStyleboxOverride("panel", style);

        _body = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _body.AddThemeFontSizeOverride("font_size", 17);
        _body.AddThemeColorOverride("font_color", new Color("#b9e6f5"));
        AddChild(_body);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        // Four refreshes a second is plenty, and keeps the text readable.
        _tick += (float)delta;
        if (_tick < 0.25f) return;
        _tick = 0f;

        _body.Text = BuildReport();
    }

    private static string BuildReport()
    {
        var sb = new StringBuilder();
        var nav = NavigationManager.Instance;
        var world = WorldStateManager.Instance;
        var entities = EntityStateManager.Instance;
        var inv = InventoryCraftingManager.Instance;
        var resolve = ResolveManager.Instance;
        var vitality = VitalityManager.Instance;
        var camp = CampManager.Instance;

        sb.AppendLine($"room    {nav?.CurrentRoomId}   ch {nav?.CurrentRoom?.ChapterId}");
        sb.AppendLine($"realm   {RealmTransitionManager.Instance?.CurrentRealm}");

        if (resolve != null)
            sb.AppendLine($"resolve {resolve.Current:F1}/{resolve.Max:F0}   rate {resolve.CurrentRatePerSecond():+0.00;-0.00;0.00}/s");

        // No rate beside this one, and there never will be: the body has no clock (combat.md §6).
        if (vitality != null)
            sb.AppendLine($"vitality{vitality.Current,7:F1}/{vitality.Max:F0}   {(vitality.IsEmpty ? "DOWN" : "")}");

        // This is the block the deleted hazard readout asked for (combat.md §22, unit 2): what *this*
        // screen has authored on it, so an inspector edit can be confirmed without clicking into it —
        // a value you typed but did not save shows up here as an absence. It stays in the top block
        // rather than joining the lists below, because the flag list can grow past the bottom of the
        // screen and an authoring aid you cannot see is no aid.
        //
        // `screen` prints the flag beside the recomputed verdict on purpose. They must always agree —
        // the flag is a cache of the predicate (see MendManager.Reevaluate) — so the one arrangement of
        // characters that can appear here and should not is `true (held)` or `false (clear)`.
        var room = nav?.CurrentRoom;
        if (room != null)
        {
            var reasons = new List<string>();
            if (MendManager.IsHeld(room)) reasons.Add("held");
            if (MendManager.IsFouled(room)) reasons.Add("fouled");

            var flag = world?.GetFlag(Flags.Cleansed(room.Id)) ?? false;
            sb.AppendLine($"screen  cleansed {flag}"
                + $"   ({(reasons.Count == 0 ? "clear" : string.Join(" + ", reasons))})");

            var blights = room.Hotspots?
                .Where(h => h != null && h.Kind == HotspotKind.Mend)
                .ToList();

            if (blights is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine("blights");
                foreach (var b in blights)
                {
                    var puzzle = b.MendPuzzle;
                    var board = puzzle == null
                        ? "NO PUZZLE"
                        : $"{puzzle.Parts?.Length ?? 0}p/{puzzle.Solution?.Length ?? 0}s "
                          + $"{(puzzle.IsOrdered ? "ordered" : "single")}"
                          + $"{(puzzle.IsSolvable() ? "" : " UNSOLVABLE")}";

                    sb.AppendLine($"  {b.Id,-26} {(MendManager.IsMended(b) ? "mended" : "open  ")}"
                        + $"  {board}   -> {b.ItemId} x{b.ItemAmount}");
                }
            }

            // Which venues this screen is, on the same authoring-aid principle as the block above: a
            // `StationId` typed into the inspector and not saved shows up here as an absence. Both kinds
            // are listed because a camp bench declaring itself a mortar is the arrangement most worth
            // being able to see — it is a station whose kind does not say so.
            var stations = room.Hotspots?
                .Where(h => h != null && !string.IsNullOrEmpty(h.StationId))
                .ToList();

            if (stations is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine($"stations{(camp?.AtCamp == true ? "   (at camp: all satisfied)" : "")}");
                foreach (var s in stations)
                    sb.AppendLine($"  {s.StationId,-26} {s.Kind}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("entities");
        if (entities != null && entities.All.Count > 0)
            foreach (var (id, state) in entities.All) sb.AppendLine($"  {id,-28} {state}");
        else
            sb.AppendLine("  (none met)");

        sb.AppendLine();
        sb.AppendLine("inventory");
        if (inv != null && inv.Stacks.Count > 0)
            foreach (var (id, n) in inv.Stacks) sb.AppendLine($"  {id,-28} x{n}");
        else
            sb.AppendLine("  (empty)");

        var flags = world?.ExportFlags();
        sb.AppendLine();
        sb.AppendLine("flags");
        if (flags is { Count: > 0 })
            foreach (var (flag, value) in flags) sb.AppendLine($"  {flag,-28} {value}");
        else
            sb.AppendLine("  (none)");

        sb.Append("\n[F1] cycle state of every entity in room   [F2] take a wound (15)");
        return sb.ToString();
    }
}
