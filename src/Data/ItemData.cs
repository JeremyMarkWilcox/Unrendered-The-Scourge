using Godot;
using Unrendered.Core;

namespace Unrendered.Data;

/// <summary>What an item is for. Drives inventory filtering and which duel slot it fits.</summary>
public enum ItemCategory
{
    /// <summary>Raw material. Herbs, fungus, bone, ore.</summary>
    Reagent = 0,

    /// <summary>Apothecary output. Restores <b>Vitality</b> per combat.md §6.</summary>
    Salve = 1,

    /// <summary>Craftsman output. Spends in the Dream realm as an ability.</summary>
    Catalyst = 2,

    /// <summary>Story object. Not consumable.</summary>
    Relic = 3,

    /// <summary>Readable. Feeds the journal.</summary>
    Lore = 4,

    /// <summary>
    /// A crafted trap. Rigged into a site before a fight, never "used" from the satchel — placement
    /// is free and only acting during the fight costs a turn (docs/combat.md §18). Consumed always,
    /// win or lose, which is why traps must be cheap and rebuildable from common materials (§13).
    /// </summary>
    Trap = 5,

    /// <summary>
    /// Worn. One weapon carrying a <see cref="Unrendered.Core.DamageType"/> and one armour halving
    /// an incoming one — see <see cref="Unrendered.Core.EquipSlot"/>. Nothing degrades, deliberately:
    /// death already destroys what you can remake, so decay would be a worse second copy of the same
    /// pressure, and it taxes *use* where death-loss taxes *failure*.
    /// </summary>
    Equipment = 6,
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
    /// Vitality given back on use. Salves only; zero on everything else.
    ///
    /// **Settled 2026-07-29, and the name is the decision.** This was `SanityRestore`, then
    /// `ResolveRestore` after a mechanical rename, and in that form it contradicted
    /// docs/combat.md §6 — which restores Resolve *only* by leaving the Dream or resting at a
    /// camp, never from an item. No item may restore Resolve. Letting one do so puts a crack in
    /// invariant 11: it is a short step from "this steadies your nerve" to a consumable that
    /// substitutes for severing, and that takes invariant 1 with it.
    /// </summary>
    [Export] public float VitalityRestore { get; set; }

    /// <summary>Placeholder swatch used by the inventory grid until icons exist.</summary>
    [Export] public Color PlaceholderColor { get; set; } = new("#6b7f5e");

    // ---- Combat (docs/combat.md §18, ratified 2026-07-30) --------------------
    //
    // Authored ahead of the encounter loop on purpose. A system written later can read fields that
    // already exist, but a field added later means reopening every .tres authored without it — so
    // these land before the content pass, not alongside it.

    /// <summary>
    /// The harm this deals. Weapons and traps only; <see cref="DamageType.None"/> on everything else,
    /// which is why <c>None</c> is the zero value — a reagent must not default to a weapon type.
    ///
    /// Against a creature that is vulnerable to it this does double, and against one that resists it
    /// half. Those two multipliers are the entire difficulty surface, and they replaced the Defence
    /// number the design used to have.
    ///
    /// Named <c>Deals</c> rather than <c>DamageType</c> so it does not shadow its own type name, and
    /// because it pairs readably with <see cref="WardsAgainst"/> in the inspector.
    /// </summary>
    [ExportGroup("Combat")]
    [Export] public DamageType Deals { get; set; } = DamageType.None;

    /// <summary>
    /// How hard it hits, or for armour how much it turns aside. Never displayed — §5's described
    /// conditions are the only readout, and the number exists for the resolver alone.
    /// </summary>
    [Export] public int Potency { get; set; }

    /// <summary>
    /// Where it is worn, or <see cref="EquipSlot.None"/> for everything that is not equipment. One
    /// weapon and one armour at a time; there is no set, because being unable to protect yourself
    /// against everything is what makes the choice real.
    /// </summary>
    [Export] public EquipSlot Slot { get; set; } = EquipSlot.None;

    /// <summary>
    /// Armour only: the incoming type this halves. **Halves, never negates** — an immunity reads as a
    /// lock and sends the player hunting for a key instead of weighing a trade (§7).
    /// </summary>
    [Export] public DamageType WardsAgainst { get; set; } = DamageType.None;

    /// <summary>
    /// Traps only: whether it goes off by itself when the creature acts, or waits for you to spend a
    /// turn on it. Oil does not light itself; a snare closes on its own.
    ///
    /// This looks like a flag the player must memorise and deliberately is not one: the rule is
    /// *placement is free, acting during the fight costs your turn*, and this value is always
    /// guessable from what the object is. Do not author one that surprises — a snare that needed a
    /// spark would break the only thing making the single-sentence rule work.
    /// </summary>
    [Export] public bool TriggersItself { get; set; } = true;

    /// <summary>
    /// Which half of the journal teaches this, for the panel's two sides. Only meaningful on
    /// <see cref="ItemCategory.Lore"/> and on recipes; <see cref="KnowledgeTrack.Craft"/> is the
    /// default because three of the four damage types are his.
    /// </summary>
    [Export] public KnowledgeTrack Track { get; set; } = KnowledgeTrack.Craft;
}
