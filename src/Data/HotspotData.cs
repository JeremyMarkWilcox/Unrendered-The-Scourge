using Godot;
using Unrendered.Core;

namespace Unrendered.Data;

/// <summary>
/// One clickable region on a screen. Regions are authored in Waking-world design space
/// (1920x1080) regardless of which realm they belong to, so a doorway and the spirit
/// anchored in front of it line up when you toggle vision.
/// </summary>
[GlobalClass]
public partial class HotspotData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";

    /// <summary>Shown on hover, e.g. "Waystation Shed" or "Cursed Giant Rat".</summary>
    [Export] public string Label { get; set; } = "";

    [Export] public HotspotKind Kind { get; set; } = HotspotKind.Examine;

    /// <summary>Click region in 1920x1080 space.</summary>
    [ExportGroup("Placement")]
    [Export] public Rect2 Region { get; set; } = new(760, 440, 400, 200);

    /// <summary>Optional polygon for a tighter hit shape. Overrides <see cref="Region"/> when non-empty.</summary>
    [Export] public Vector2[] Polygon { get; set; } = System.Array.Empty<Vector2>();

    [Export] public RealmVisibility Visibility { get; set; } = RealmVisibility.Both;

    [ExportGroup("Navigate")]
    [Export] public string TargetRoomId { get; set; } = "";

    /// <summary>Which placed entity this hotspot targets.</summary>
    [ExportGroup("Entity")]
    [Export] public string EntityId { get; set; } = "";

    /// <summary>Which <see cref="DreamHeistData"/> a CrossToDream relic starts.</summary>
    [ExportGroup("Dream heist")]
    [Export] public string HeistId { get; set; } = "";

    [ExportGroup("Harvest")]
    [Export] public string ItemId { get; set; } = "";
    [Export] public int ItemAmount { get; set; } = 1;

    /// <summary>Set once taken, so a node cannot be farmed twice. Auto-derived if blank.</summary>
    [Export] public string HarvestedFlag { get; set; } = "";

    [ExportGroup("Text")]
    [Export(PropertyHint.MultilineText)] public string ExamineText { get; set; } = "";

    /// <summary>Shown instead of acting when the gate below refuses.</summary>
    [Export(PropertyHint.MultilineText)] public string BlockedText { get; set; } = "";

    /// <summary>Hotspot only appears when this world flag matches <see cref="RequiredFlagValue"/>. Blank = always.</summary>
    [ExportGroup("Gating")]
    [Export] public string RequiredFlag { get; set; } = "";
    [Export] public bool RequiredFlagValue { get; set; } = true;

    /// <summary>Hotspot is visible but refuses to act while this entity is still Corrupted.</summary>
    [Export] public string BlockedByEntityId { get; set; } = "";

    /// <summary>Sets this world flag to true when successfully activated. Blank = none.</summary>
    [Export] public string SetsFlag { get; set; } = "";

    /// <summary>Fallback flag name used to remember a consumed harvest node.</summary>
    public string ResolvedHarvestedFlag =>
        string.IsNullOrEmpty(HarvestedFlag) ? $"harvested.{Id}" : HarvestedFlag;
}
