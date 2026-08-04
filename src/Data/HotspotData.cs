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

    // `Visibility` (RealmVisibility) sat here until 2026-08-02. It is the field spectral vision was
    // built on: a hotspot could be authored to exist only in the Waking world or only in the Dream,
    // and the same screen showed two different sets of things. Deleted with the great swap — a
    // Waking screen has one set of things on it now, and Dream content lives in its own 3D pocket
    // rather than hiding behind a key press on a Waking screen.

    [ExportGroup("Navigate")]
    [Export] public string TargetRoomId { get; set; } = "";

    /// <summary>Which placed entity this hotspot targets.</summary>
    [ExportGroup("Entity")]
    [Export] public string EntityId { get; set; } = "";

    /// <summary>Which <see cref="DreamHeistData"/> a CrossToDream relic starts.</summary>
    [ExportGroup("Dream heist")]
    [Export] public string HeistId { get; set; } = "";

    /// <summary>
    /// What a <see cref="HotspotKind.Harvest"/> node yields — and what a
    /// <see cref="HotspotKind.Mend"/> pays out for solving its jigsaw.
    /// </summary>
    /// <remarks>
    /// <b>Shared rather than given a `MendYield` of its own, and that is the gateable choice.</b> Every
    /// blight pays 1 Graveflower (<c>combat.md</c> §22) and it would have been a line shorter to
    /// hard-code that in <c>MendManager</c> — but then the check "all twelve pay exactly one" would be
    /// asserting a constant against itself, which is the unfalsifiable-guard trap recorded in
    /// <c>handoff.md</c>. Authored, it is twelve facts a sweep can actually read.
    /// </remarks>
    [ExportGroup("Harvest / Mend yield")]
    [Export] public string ItemId { get; set; } = "";
    [Export] public int ItemAmount { get; set; } = 1;

    /// <summary>Set once taken, so a node cannot be farmed twice. Auto-derived if blank.</summary>
    [Export] public string HarvestedFlag { get; set; } = "";

    /// <summary>
    /// Whether this node comes back at the material-refill milestone (items.md §7).
    /// </summary>
    /// <remarks>
    /// <b>Defaults to false, and the direction matters.</b> Common materials are meant to re-arm once,
    /// so salves stay makeable; the scarce ones must not, because the world holding exactly 12
    /// Graveflower and 6 Cairnstone is the arithmetic the good ending is balanced against. A node the
    /// generator forgets to mark therefore fails toward a supply that is tighter than intended —
    /// visible in play and fixable in one field — rather than toward a scarce material that farms
    /// forever, which would quietly delete the economy and never show up as a bug.
    /// </remarks>
    [Export] public bool RefillsAtMilestone { get; set; }

    // `VitalityCost`, `HazardText` and `IsHazard` were deleted 2026-08-01, along with ApplyHazard,
    // the debug panel's hazard list and GateHazards. They were the Waking world costing you the
    // body, and `combat.md` §22 removed Waking-world damage from the design: a camp is one menu
    // action away (§17), so no attrition outside a fight can matter. §22 asked that the field not be
    // deleted on its own say-so without first checking what the collapse-path gates exercise — they
    // drive `VitalityManager.Damage` directly, so the collapse, `SurvivesDeath` and the respawn kept
    // every check they had. Jeremy: "vit cost was when the environment damaged you and since nothing
    // does now we don't really need it."
    //
    // What replaces it is not a cheaper hazard, it is a different verb: a **Blight** costs a jigsaw
    // and pays a Graveflower (§22). It shares no field with this one, which is why this is a
    // deletion rather than a rename.

    /// <summary>
    /// The jigsaw behind a <see cref="HotspotKind.Mend"/>. Ignored by every other kind.
    /// </summary>
    /// <remarks>
    /// A blight needs no gating field of its own: the flag it sets is derived
    /// (<see cref="ResolvedMendedFlag"/>), and the three tool-form puzzles are kept out of reach by
    /// <i>geography</i> — all three sit on screens you can only walk to through a tool-gated door, so a
    /// <see cref="RequiredFlag"/> on top would be the same fact stored twice, in the version that can
    /// disagree with itself. <c>--verify-slice</c> asserts the graph property rather than the field.
    /// </remarks>
    [ExportGroup("Mend")]
    [Export] public MendPuzzleData MendPuzzle { get; set; }

    /// <summary>
    /// Which station this is — <c>mortar</c>, <c>anvil</c>, <c>stump</c>. A
    /// <see cref="HotspotKind.Station"/> hotspot narrows the crafting panel to recipes whose
    /// <see cref="RecipeData.RequiresStation"/> matches, and a <see cref="HotspotKind.Workbench"/>
    /// carrying one is the camp's own bench declaring which station it *also* counts as.
    /// </summary>
    /// <remarks>
    /// A free string rather than an enum, on the same reasoning as <see cref="RoomData.ChapterId"/>: the
    /// vocabulary is content Jeremy authors and a station is added by placing a hotspot, not by editing
    /// code. What keeps that honest is the pair of gates over it — every station id a recipe names must
    /// exist somewhere in the world, and every station in the world must be named by some recipe, so a
    /// typo is a red check rather than a bench that silently makes nothing.
    /// </remarks>
    [ExportGroup("Station")]
    [Export] public string StationId { get; set; } = "";

    [ExportGroup("Text")]
    [Export(PropertyHint.MultilineText)] public string ExamineText { get; set; } = "";

    /// <summary>Shown instead of acting when the gate below refuses.</summary>
    [Export(PropertyHint.MultilineText)] public string BlockedText { get; set; } = "";

    /// <summary>Hotspot only appears when this world flag matches <see cref="RequiredFlagValue"/>. Blank = always.</summary>
    [ExportGroup("Gating")]
    [Export] public string RequiredFlag { get; set; } = "";
    [Export] public bool RequiredFlagValue { get; set; } = true;

    /// <summary>
    /// Every one of these flags must be <c>true</c> as well. Empty = no extra requirement.
    /// </summary>
    /// <remarks>
    /// Added 2026-08-02 for the Snarl (ledger-ch3 row 12), which opens only once all three knot
    /// pockets are cut and so cannot be expressed by the single flag above.
    /// <para>
    /// Deliberately all-true with no per-entry polarity: the only gate that ever needs a
    /// <c>false</c> is the hide-once-done shape (a relic that vanishes when taken), and that one
    /// is always a single flag, which <see cref="RequiredFlag"/> already covers. Keeping the
    /// multi case value-free means this is a plain string array rather than an array of
    /// sub-resources — cheap to author and cheap to read in the inspector.
    /// </para>
    /// </remarks>
    [Export] public string[] RequiredFlags { get; set; } = System.Array.Empty<string>();

    /// <summary>Hotspot is visible but refuses to act while this entity is still Corrupted.</summary>
    [Export] public string BlockedByEntityId { get; set; } = "";

    /// <summary>Sets this world flag to true when successfully activated. Blank = none.</summary>
    [Export] public string SetsFlag { get; set; } = "";

    /// <summary>Fallback flag name used to remember a consumed harvest node.</summary>
    public string ResolvedHarvestedFlag =>
        string.IsNullOrEmpty(HarvestedFlag) ? $"harvested.{Id}" : HarvestedFlag;

    /// <summary>
    /// The flag remembering that this blight has been mended, on the same shape as
    /// <see cref="ResolvedHarvestedFlag"/> — but with no authorable override beside it.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="Id"/> and nothing else, on purpose. <c>Flags</c> promises that a class of
    /// thing can be counted by prefix, and <c>blight.*.mended</c> has to count exactly twelve; an
    /// override field is a way for one of the twelve to hide under another name. <c>HarvestedFlag</c>
    /// earns its override because a node can legitimately share state with something else — a mend
    /// never can, because it is the screen's own act.
    /// </remarks>
    public string ResolvedMendedFlag => Flags.Mended(Id);
}
