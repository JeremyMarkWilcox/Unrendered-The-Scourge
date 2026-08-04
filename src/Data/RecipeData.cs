using Godot;

namespace Unrendered.Data;

/// <summary>
/// A thing you know how to make. Authored as a <c>.tres</c> under <c>data/recipes/</c>, exactly
/// like rooms and items — adding a recipe is a resource file, not code.
///
/// **Recipes grant access, never power** (invariant 5). A recipe exists so that a material you
/// gathered can become a tool that opens something; it is never a numeric upgrade to a thing you
/// already have. If a recipe ever reads as "the better version of", it is the wrong recipe.
///
/// ~~There is deliberately **no station or camp field yet**.~~ **Landed 2026-08-03 (unit 4a),
/// and it went exactly where that note said it would** — the venue check sits in
/// <c>InventoryCraftingManager.Inspect</c> beside the blueprint check, and every existing recipe kept
/// working ~~because a blank <see cref="RequiresStation"/> means *anywhere*~~ **because all four of them
/// name a station, so nothing was relying on the blank case at all** (corrected in unit 4b — see the
/// field's own remarks; blank means the bench). Worth keeping the original reasoning visible: the field
/// waited until something read it, which is the mistake `ItemData.VitalityRestore` spent two milestones
/// being.
/// </summary>
[GlobalClass]
public partial class RecipeData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";

    /// <summary>Shown in the crafting panel. Falls back to the output item's name when blank.</summary>
    [Export] public string DisplayName { get; set; } = "";

    [ExportGroup("Cost")]
    [Export] public Godot.Collections.Array<RecipeIngredient> Inputs { get; set; } = new();

    /// <summary>
    /// Where it is made — the <see cref="HotspotData.StationId"/> of a mortar, an anvil, a stump.
    /// <b>Blank means the camp bench</b>, which is the unspecialised venue and not the open road.
    /// </summary>
    /// <remarks>
    /// <b>This summary said "blank means anywhere" until unit 4b, and the code never agreed with it.</b>
    /// <see cref="Managers.InventoryCraftingManager.AtVenueFor"/> returns <c>false</c> on a blank field
    /// once the camp check has missed — its own remark says so — so blank is the *most* restrictive
    /// setting available, not the least. The wrong reading was harmless only because all four recipes
    /// authored before today name a station; it would have become a real defect the first time somebody
    /// left the field empty meaning "craftable in a ditch". That is why the six trap recipes leave it
    /// blank *deliberately*: `items.md` §3 builds every trap at the bench, and a <c>"bench"</c> station id
    /// would be an alias for the same behaviour plus a hotspot nobody has authored.
    /// <para>
    /// <b>The camp bench satisfies every station; a field station satisfies only its own.</b> Ratified
    /// while building this (`combat.md` §19). It protects invariant 12 — no recipe can become
    /// unreachable — and it makes fusion the bench's exclusive job by *addition* rather than by
    /// exception. The consequence worth stating: because a camp is a menu reachable from anywhere
    /// (§17, 2026-08-01), <see cref="Core.CraftBlock.NotAtStation"/> can never dead-end anyone. It
    /// refuses crafting *where you stand*, and the answer is always "camp, or find the station" — so a
    /// field station is a saved round trip, not a gate.
    /// </para>
    /// </remarks>
    [ExportGroup("Venue")]
    [Export] public string RequiresStation { get; set; } = "";

    /// <summary>
    /// Needs both halves of the journal — his Craft and her Physick. **Exactly one recipe in the game
    /// is this**, the exorcism engine (`items.md` §6), and it is a bool rather than a
    /// <see cref="Core.KnowledgeTrack"/> pair because the interesting state is *both*, never *either*.
    /// </summary>
    [Export] public bool RequiresBothTracks { get; set; }

    [ExportGroup("Result")]
    [Export] public string OutputItemId { get; set; } = "";
    [Export] public int OutputAmount { get; set; } = 1;

    /// <summary>
    /// A world flag set on a successful craft. This is how a tool opens a door and how a bridge gets
    /// repaired: learn the Bill Hook → craft it → <c>learned.tool_bill_hook</c> → the bramble hotspot's
    /// <see cref="HotspotData.RequiredFlag"/> is met. Blank on everything ordinary.
    /// </summary>
    /// <remarks>
    /// <b>No new gating field was needed for tools</b> (`build-plan.md` §4), and that is the point of
    /// putting the flag here rather than on the item: the three door hotspots were authored in unit 1
    /// against <c>learned.tool_*</c> names that nothing set, and this is the system arriving at names
    /// already waiting for it. It also means a recipe can output *only* a flag — see
    /// <see cref="RepairsWorld"/>, which is the three major repairs of `items.md` §6.
    /// </remarks>
    [Export] public string SetsFlag { get; set; } = "";

    /// <summary>
    /// A recipe that changes the world instead of filling the satchel: a flag and no item. The
    /// collapsed bridge, the mine cart, the exorcism engine's installation.
    /// </summary>
    /// <remarks>
    /// Derived rather than authored, on the <see cref="NeedsBlueprint"/> precedent and for the same
    /// reason a form is derived from <c>MendPuzzleData.Solution.Length</c> — two fields that must agree
    /// are better stored as one. A recipe with neither an output item nor a flag is a content error and
    /// <c>CraftBlock.NoSuchRecipe</c> already catches it.
    /// </remarks>
    public bool RepairsWorld => string.IsNullOrEmpty(OutputItemId) && !string.IsNullOrEmpty(SetsFlag);

    /// <summary>
    /// Id of the <see cref="ItemCategory.Lore"/> blueprint that teaches this. Blank means you
    /// have always known how — the crude, obvious things.
    ///
    /// This gate exists from the first recipe on purpose. Retrofitting discovery later would mean
    /// reopening both the crafting resolution and its UI, whereas building it now makes the
    /// journal purely additive: the journal becomes a *view* of what is already tracked.
    /// </summary>
    [ExportGroup("Discovery")]
    [Export] public string RequiredBlueprintId { get; set; } = "";

    public bool NeedsBlueprint => !string.IsNullOrEmpty(RequiredBlueprintId);
}
