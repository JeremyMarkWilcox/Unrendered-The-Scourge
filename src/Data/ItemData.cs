using Godot;

namespace Unrendered.Data;

/// <summary>What an item is for. Drives inventory filtering and which duel slot it fits.</summary>
public enum ItemCategory
{
    /// <summary>Raw material. Herbs, fungus, bone, ore.</summary>
    Reagent = 0,

    /// <summary>Apothecary output. Restores Sanity.</summary>
    Salve = 1,

    /// <summary>Craftsman output. Spends in the Dream realm as an ability.</summary>
    Catalyst = 2,

    /// <summary>Story object. Not consumable.</summary>
    Relic = 3,

    /// <summary>Readable. Feeds the journal.</summary>
    Lore = 4,
}

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public ItemCategory Category { get; set; } = ItemCategory.Reagent;
    [Export] public Texture2D Icon { get; set; }
    [Export] public int MaxStack { get; set; } = 20;

    /// <summary>Sanity restored on use. Salves only.</summary>
    [Export] public float SanityRestore { get; set; }

    /// <summary>Placeholder swatch used by the inventory grid until icons exist.</summary>
    [Export] public Color PlaceholderColor { get; set; } = new("#6b7f5e");
}
