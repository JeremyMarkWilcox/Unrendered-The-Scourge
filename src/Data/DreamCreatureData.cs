using Godot;
using Unrendered.Core;

namespace Unrendered.Data;

/// <summary>
/// What a thing in the Dream does to you. Unit 8, and **deliberately not an <see cref="EntityData"/>** —
/// `build-plan.md` decision 8 ruled that out on 2026-08-02: *"enemies in the spirit realm will have completely
/// different functionality than the 2D battle types."*
///
/// <para><b>Nothing in here fights.</b> `EntityData` is built for the turn grammar — vulnerable and resistant
/// types, durability, tether state, what severing frees — and a Dream creature has none of it. Jeremy,
/// 2026-08-04: *"honestly none of the creatures fight. The minor ones are limited with their chasing AI, only
/// when you have light on, and they hurt resolve instead of banish; bosses have more aggressive AI and will
/// banish you."* So there is no shared schema and there should not be one. The only genuine overlap is a boss,
/// which exists in both realms — and that is a link between two files, not one file doing both jobs.</para>
///
/// <para><b>A resource so variants are authored rather than cloned.</b> Same argument as the three pocket
/// nodes: fourteen scenes with tweaked scripts is fourteen copies that drift, and cloning is how they drift.
/// A `.tres` per creature type keeps the rules in data and the geometry in scenes.</para>
/// </summary>
[GlobalClass]
public partial class DreamCreatureData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";

    /// <summary>For the log and for a readout. Never shown as a health bar; there is no health here.</summary>
    [Export] public string DisplayName { get; set; } = "";

    [ExportGroup("What contact costs")]
    /// <summary>
    /// Resolve taken per touch. Ignored when <see cref="Banishes"/> is set, because a banish does not take a
    /// chunk — it takes the whole visit.
    /// </summary>
    [Export(PropertyHint.Range, "0,100,1")]
    public int ResolveCost { get; set; } = 10;

    /// <summary>
    /// Seconds before this creature can cost you again. Zero falls back to
    /// <see cref="GameConfig.CreatureTouchCooldownSeconds"/>.
    /// </summary>
    /// <remarks>
    /// <b>The field the whole model turns on.</b> An <c>Area3D</c> reports contact every frame, so without a
    /// cooldown a single touch empties the meter faster than a player can react and the ejection reads as
    /// arbitrary. Authored per type rather than global because a slow drifting thing and something that lunges
    /// want different windows — but the fallback means a creature cannot be authored *without* one, which is
    /// the safe direction.
    /// </remarks>
    [Export(PropertyHint.Range, "0,10,0.25")]
    public float TouchCooldownSeconds { get; set; }

    /// <summary>
    /// Whether contact throws you out of the pocket outright instead of costing a chunk. **Bosses only.**
    /// </summary>
    /// <remarks>
    /// A banish and running out of nerve are the same call (<c>PocketManager.Eject</c>) — ruled 2026-08-04, and
    /// one path is the point: both write nothing, so the entrance survives and the cost is the trip. Nothing in
    /// the Dream can kill you (invariant 2), so this is the worst thing anything in there can do.
    /// </remarks>
    [Export] public bool Banishes { get; set; }

    [ExportGroup("How it behaves")]
    /// <summary>
    /// Whether it only pursues while the player's light is on — true for the minor creatures, and **the reason
    /// <see cref="DreamVerb.Suppress"/> has a job.**
    /// </summary>
    /// <remarks>
    /// The field is here rather than in each creature's script because it is a *rule* and not flavour: it is
    /// what makes the starting verb the answer to the whole minor-creature population, and therefore what makes
    /// boss pocket 1 solvable with Suppress alone. **How aggressively a thing chases is the 3D pass**; whether
    /// it chases at all is this.
    /// </remarks>
    [Export] public bool PursuesOnlyWhenLit { get; set; } = true;
}
