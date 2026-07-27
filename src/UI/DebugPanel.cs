using System.Text;
using Godot;
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

        sb.AppendLine($"room    {nav?.CurrentRoomId}   zone {nav?.CurrentRoom?.ZoneId}");
        sb.AppendLine($"realm   {RealmTransitionManager.Instance?.CurrentRealm}");

        if (nav?.CurrentRoom != null && world != null)
            sb.AppendLine($"phase   {world.GetZonePhase(nav.CurrentRoom.ZoneId)}");

        if (resolve != null)
            sb.AppendLine($"resolve {resolve.Current:F1}/{resolve.Max:F0}   rate {resolve.CurrentRatePerSecond():+0.00;-0.00;0.00}/s");

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

        sb.Append("\n[F1] cycle state of every entity in room");
        return sb.ToString();
    }
}
