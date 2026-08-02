using Godot;
using Unrendered.Core;

namespace Unrendered.Data;

/// <summary>
/// A creature or spirit archetype. Carries one texture per state — three, all optional.
/// Anything left null falls back to the procedural placeholder so the loop is playable
/// before art exists.
/// </summary>
[GlobalClass]
public partial class EntityData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";

    /// <summary>Form 1 — the mutated host. "Cursed Giant Rat".</summary>
    [Export] public string CorruptedName { get; set; } = "";

    /// <summary>
    /// Form 2 — the pristine dark noble in the Dream. "The Gnawing Baron".
    ///
    /// <b>Deliberately kept with no reader as of 2026-08-02.</b> The great swap deleted every
    /// Waking-side path that could show Form 2, so nothing in code reads this today. It survives
    /// where <c>ZoneData</c> did not, for the opposite reason: that class was an empty shape held
    /// against a future need, and this is <i>authored writing</i> already sitting in the .tres files.
    /// The 3D pocket that shows Form 2 is unit 6's job, and it will want the name that was written
    /// for it.
    /// </summary>
    [Export] public string SpiritName { get; set; } = "";

    /// <summary>Form 3 — what it was all along. "Field Rat".</summary>
    [Export] public string PurifiedName { get; set; } = "";

    [ExportGroup("Textures")]
    [Export] public Texture2D CorruptedTexture { get; set; }
    [Export] public Texture2D SeveredTexture { get; set; }
    [Export] public Texture2D PurifiedTexture { get; set; }

    // `SpiritTexture` / `ShatteredTexture` / `EchoTexture` — the Dream half of the six-cell texture
    // table. Deleted 2026-08-02: there is no 2D Dream layer to draw them into. Unlike SpiritName
    // above, no .tres ever filled these in, so nothing authored was lost.

    [ExportGroup("Behaviour")]
    [Export] public EntityState InitialState { get; set; } = EntityState.Corrupted;

    /// <summary>Whether this thing bars passage while Corrupted.</summary>
    [Export] public bool BlocksPath { get; set; } = true;

    /// <summary>
    /// Strength of the visible aura an anchored body projects. Per docs/combat.md §2 this is
    /// what drives regeneration once Waking combat exists — and per §6 it is also what drains
    /// Resolve during a boss fight, at a rate sized to empty a full bar in about six turns.
    ///
    /// It does **not** drive damage reduction, and there is nothing to overload it with any more:
    /// §18 deleted Defence as a number on 2026-07-30. What survived is the *type* half, which lives
    /// on <see cref="VulnerableTo"/> and <see cref="ResistantTo"/> below — and note those are
    /// unaffected by severing, where the aura's two effects both stop.
    ///
    /// That drain is **conditional on the tether**: it applies while the entity is
    /// <see cref="EntityState.Corrupted"/> and stops entirely once it is
    /// <see cref="EntityState.Severed"/>, so severing buys a fight that costs no Resolve.
    /// Read the state, do not add a flag. And it is never ambient — only the engagement
    /// costs, never standing in the room.
    /// </summary>
    [Export] public float CorruptionAura { get; set; } = 0.15f;

    // ---- Combat (docs/combat.md §18, ratified 2026-07-30) --------------------

    /// <summary>
    /// How much harm the body absorbs before it collapses. Never shown — §5's described conditions
    /// (<i>unmarked → bleeding → faltering → collapsing</i>) are derived from this as fractions and
    /// are the only readout.
    ///
    /// <b>Keep these roughly flat across creatures.</b> Difficulty is meant to come from
    /// <see cref="VulnerableTo"/> matching, not from a bigger number, because more durability makes a
    /// fight *longer* and §5 caps a fight at 3–6 turns — "expensive, never long". A creature that is
    /// hard because you brought the wrong tool costs you materials; one that is hard because it has
    /// three times the durability costs you the pacing the whole section is built on.
    /// </summary>
    [ExportGroup("Combat")]
    [Export] public int Durability { get; set; } = 30;

    /// <summary>
    /// The type that does <b>double</b> against it. Set this on every creature, not just the armoured
    /// ones: if only some had a real weakness then "the armoured one" would mean *the fight where the
    /// mechanic is switched on*, and players would correctly learn to ignore damage types everywhere
    /// else. Universal is what makes "what does this want?" a question worth asking each time.
    /// </summary>
    [Export] public DamageType VulnerableTo { get; set; } = DamageType.None;

    /// <summary>
    /// The type that does <b>half</b> against it. Never zero — absolutes read as locks and invite
    /// key-hunting rather than the intended question, <i>is this fight worth what it will cost me?</i>
    /// (§7). Together with <see cref="VulnerableTo"/> this replaced the Defence number entirely; do
    /// not add one back.
    /// </summary>
    [Export] public DamageType ResistantTo { get; set; } = DamageType.None;

    /// <summary>
    /// What it deals to you, which is the other half of what the confirm dialog has to tell the player
    /// before they commit (§7's legibility promise). Without it the armour choice is a coin flip
    /// rather than a decision.
    /// </summary>
    [Export] public DamageType Deals { get; set; } = DamageType.None;

    /// <summary>How hard it hits, per turn. Never displayed.</summary>
    [Export] public int Damage { get; set; } = 8;

    /// <summary>
    /// §18's per-creature override for the condition ladder's *words* — not its bands. Leave it empty
    /// and the creature uses the shared vocabulary; author **exactly four** entries and they replace
    /// it, in <see cref="Condition"/> order: unmarked, bleeding, faltering, collapsing.
    ///
    /// <para>Only the words are overridable, and that asymmetry is the design. The bands are computed
    /// from <see cref="Durability"/> so that no creature can be authored into a wound state with no
    /// description; letting the bands be authored too would put that hole straight back. A spirit that
    /// should read "the crown is cracking" rather than "it is bleeding" needs different prose, not a
    /// different threshold.</para>
    ///
    /// <para>Wrong-length arrays are a gate failure rather than a silent fallback: four is the only
    /// count that can possibly be right, and quietly ignoring a three-entry array would hide a typo in
    /// the one part of a fight the player actually reads.</para>
    /// </summary>
    [Export] public Godot.Collections.Array<string> ConditionWords { get; set; } = new();

    /// <summary>What it becomes farmable for once exorcised. Blank = nothing.</summary>
    [ExportGroup("Purified yield")]
    [Export] public string HarvestItemId { get; set; } = "";

    /// <summary>Best-effort display name for the given state.</summary>
    public string NameFor(EntityState state) => state switch
    {
        EntityState.Purified => string.IsNullOrEmpty(PurifiedName) ? CorruptedName : PurifiedName,
        _ => CorruptedName,
    };
}
