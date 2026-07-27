using Godot;
using Unrendered.Core;

namespace Unrendered.Data;

/// <summary>
/// A creature or spirit archetype. Carries one texture per (state, realm) cell of the
/// presentation table — six in total, all optional. Anything left null falls back to the
/// procedural placeholder so the loop is playable before art exists.
/// </summary>
[GlobalClass]
public partial class EntityData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";

    /// <summary>Form 1 — the mutated host. "Cursed Giant Rat".</summary>
    [Export] public string CorruptedName { get; set; } = "";

    /// <summary>Form 2 — the pristine dark noble in the Pixel Realm. "The Gnawing Baron".</summary>
    [Export] public string SpiritName { get; set; } = "";

    /// <summary>Form 3 — what it was all along. "Field Rat".</summary>
    [Export] public string PurifiedName { get; set; } = "";

    [ExportGroup("Waking textures")]
    [Export] public Texture2D CorruptedTexture { get; set; }
    [Export] public Texture2D SeveredTexture { get; set; }
    [Export] public Texture2D PurifiedTexture { get; set; }

    /// <summary>Form 2, the attackable anchor.</summary>
    [ExportGroup("Dream textures")]
    [Export] public Texture2D SpiritTexture { get; set; }
    [Export] public Texture2D ShatteredTexture { get; set; }
    [Export] public Texture2D EchoTexture { get; set; }

    [ExportGroup("Behaviour")]
    [Export] public EntityState InitialState { get; set; } = EntityState.Corrupted;

    /// <summary>Whether this thing bars passage while Corrupted.</summary>
    [Export] public bool BlocksPath { get; set; } = true;

    /// <summary>
    /// Strength of the visible aura an anchored body projects. Per docs/combat.md §2 this is
    /// what drives regeneration once Waking combat exists — and per §6 it is also what drains
    /// Resolve during a boss fight, at a rate sized to empty a full bar in about six turns.
    ///
    /// It does **not** drive damage reduction. Defence is a separate fixed per-unit value that
    /// severing does not change (§2), and it is not modelled here yet — do not overload this
    /// field with it.
    ///
    /// That drain is **conditional on the tether**: it applies while the entity is
    /// <see cref="EntityState.Corrupted"/> and stops entirely once it is
    /// <see cref="EntityState.Severed"/>, so severing buys a fight that costs no Resolve.
    /// Read the state, do not add a flag. And it is never ambient — only the engagement
    /// costs, never standing in the room.
    /// </summary>
    [Export] public float CorruptionAura { get; set; } = 0.15f;

    /// <summary>What it becomes farmable for once exorcised. Blank = nothing.</summary>
    [ExportGroup("Purified yield")]
    [Export] public string HarvestItemId { get; set; } = "";

    /// <summary>Best-effort display name for the given state and realm.</summary>
    public string NameFor(EntityState state, GameRealm realm)
    {
        if (realm == GameRealm.Dream)
            return string.IsNullOrEmpty(SpiritName) ? CorruptedName : SpiritName;

        return state switch
        {
            EntityState.Purified => string.IsNullOrEmpty(PurifiedName) ? CorruptedName : PurifiedName,
            _ => CorruptedName,
        };
    }
}
