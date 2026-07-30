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
/// There is deliberately **no station or camp field yet**. Crafting is meant to happen at camps
/// (`combat.md` §17) and camps do not exist, so a `RequiresCamp` flag would be a field nothing
/// reads — which is the exact mistake `ItemData.VitalityRestore` spent two milestones being. When
/// camps land, the gate goes in <c>InventoryCraftingManager.CanCraft</c> beside the blueprint
/// check, and every existing recipe keeps working.
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

    [ExportGroup("Result")]
    [Export] public string OutputItemId { get; set; } = "";
    [Export] public int OutputAmount { get; set; } = 1;

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
