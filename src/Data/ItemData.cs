using Godot;

namespace Unrendered.Data;

/// <summary>What an item is for. Drives inventory filtering and which duel slot it fits.</summary>
public enum ItemCategory
{
    /// <summary>Raw material. Herbs, fungus, bone, ore.</summary>
    Reagent = 0,

    /// <summary>Apothecary output. Restores <b>Vitality</b> per combat.md §6 — see ResolveRestore.</summary>
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

    /// <summary>
    /// Amount restored on use. Salves only.
    ///
    /// **The name is a leftover and contradicts the design — do not wire it up as-is.** It was
    /// `SanityRestore`, renamed mechanically with everything else. But docs/combat.md §6 restores
    /// Resolve *only* by leaving the Dream or resting at a camp, never from an item, and salves
    /// restore **Vitality**. Nothing reads this field today (`salve_clearmind` just stores 35),
    /// so nothing is broken yet — but M4 adds item use, and whoever does it must settle whether
    /// this becomes `VitalityRestore` or whether some items legitimately restore Resolve.
    /// Vitality does not exist yet either. See handoff.md §5.
    /// </summary>
    [Export] public float ResolveRestore { get; set; }

    /// <summary>Placeholder swatch used by the inventory grid until icons exist.</summary>
    [Export] public Color PlaceholderColor { get; set; } = new("#6b7f5e");
}
