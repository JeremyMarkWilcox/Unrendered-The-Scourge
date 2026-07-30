using Godot;

namespace Unrendered.Data;

/// <summary>
/// One line of a recipe: how much of what goes in. Its own file because Godot resolves a C#
/// script resource by filename, so a nested class here would make the <c>.tres</c> unloadable.
/// </summary>
[GlobalClass]
public partial class RecipeIngredient : Resource
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public int Amount { get; set; } = 1;
}
