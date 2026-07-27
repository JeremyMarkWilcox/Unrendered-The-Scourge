using Godot;

namespace Unrendered.Data;

/// <summary>
/// One Dream heist: a self-contained stealth setpiece entered by touching a relic in the
/// Waking world. You are not there to fight. You are there to steal the thing holding a
/// noble's world together, while it hunts you for it.
///
/// Scoped to a zone lieutenant rather than to individual creatures — cleansing the
/// lieutenant drops the whole region to Form 3, so a heist is always a setpiece and never
/// a chore repeated per rat.
/// </summary>
[GlobalClass]
public partial class DreamHeistData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    /// <summary>Instance id of the entity that becomes Severed when the anchor is taken.</summary>
    [Export] public string TargetEntityId { get; set; } = "";

    [ExportGroup("Geography")]
    /// <summary>Waking room the player is returned to on ejection, however it ends.</summary>
    [Export] public string ReturnRoomId { get; set; } = "";

    /// <summary>Dream room the player arrives in.</summary>
    [Export] public string StartRoomId { get; set; } = "";

    /// <summary>Dream room where the pursuer begins. Should not be the start room.</summary>
    [Export] public string PursuerStartRoomId { get; set; } = "";

    [ExportGroup("The pursuer")]
    [Export] public string PursuerName { get; set; } = "The Mind-Stalker";

    /// <summary>Seconds between pursuer moves. Lower is more frightening and less fair.</summary>
    [Export(PropertyHint.Range, "0.5,10,0.25")]
    public float PursuerMoveSeconds { get; set; } = 3.0f;

    /// <summary>
    /// Chance per move that it wanders instead of closing. Pure pursuit is oppressive and
    /// unreadable; a little drift lets the player believe they can outthink it.
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float PursuerWanderChance { get; set; } = 0.25f;

    [ExportGroup("Resolve cost")]
    /// <summary>Resolve burned per second simply for being in the Dream. This is the clock.</summary>
    [Export(PropertyHint.Range, "0,20,0.25")]
    public float DrainPerSecond { get; set; } = 3.0f;

    /// <summary>Extra drain per second when the pursuer is in an adjacent room.</summary>
    [Export(PropertyHint.Range, "0,20,0.25")]
    public float NearDrainPerSecond { get; set; } = 2.5f;

    /// <summary>
    /// Fraction of the bar restored on returning to the Waking world, however the run ended.
    /// 1.0 means every crossing starts fresh. Lower it to make a botched run leave a mark.
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")]
    public float ResolveRestoredOnReturn { get; set; } = 1.0f;

    [ExportGroup("Text")]
    [Export(PropertyHint.MultilineText)] public string EntryText { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string SuccessText { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string CaptureText { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string TimeoutText { get; set; } = "";
}
