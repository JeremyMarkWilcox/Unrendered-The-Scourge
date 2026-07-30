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

    /// <summary>
    /// Vitality taken when this hotspot is successfully used. Zero — the default, and almost
    /// everything — is harmless.
    ///
    /// Deliberately a cost on the existing kinds rather than a <c>HotspotKind.Hazard</c> of its
    /// own, because it then composes: a <see cref="HotspotKind.Navigate"/> with a cost is a route
    /// you may take but will pay for, a <see cref="HotspotKind.Harvest"/> with a cost is thorns,
    /// an <see cref="HotspotKind.Interact"/> with a cost is reaching into something you should
    /// not have. No new enum, no new verb, no new rendering.
    ///
    /// **A cost only fires when the player chooses to click, which is what keeps it legal.**
    /// `combat.md` §6 rejected ambient attrition for Resolve on the grounds that a drain the
    /// player cannot act on teaches nothing; a per-click cost is discrete and avoidable by
    /// construction. Authoring is what makes it *fair* — the label has to warn you.
    ///
    /// Not a **trap**. Traps are the player's own crafted tools (`combat.md` §4) and are blocked
    /// on M4. This is a hazard: the world costing you something.
    /// </summary>
    [ExportGroup("Hazard")]
    [Export] public float VitalityCost { get; set; }

    /// <summary>Said when the cost lands. Blank falls back to a generic line.</summary>
    [Export(PropertyHint.MultilineText)] public string HazardText { get; set; } = "";

    /// <summary>True when using this costs the body something.</summary>
    public bool IsHazard => VitalityCost > 0f;

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
