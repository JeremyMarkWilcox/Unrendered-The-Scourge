using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// Owns the persistent condition of every entity instance in the game, and resolves what
/// each state looks like.
///
/// <para>
/// This was a two-axis table until 2026-08-02 — state x realm, six cells, because Form 2 (the
/// pristine porcelain noble) was what a <i>Corrupted</i> entity looked like when you pressed Tab
/// and perceived it from the Dream. The great swap deleted the realm axis. Three cells remain:
/// </para>
///
///   Corrupted  | Form 1: nightmare, blocks the road
///   Severed    | Form 1 deformed, passable
///   Purified   | Form 3: mundane, farmable
///
/// <para>
/// Form 2 has not been cancelled — it moved. It belongs to the 3D pocket you travel into, which
/// is unit 6's, and it will be resolved by whatever draws that, not by this. What was lost here
/// was the claim that you could <i>see</i> Form 2 from a Waking screen.
/// </para>
///
/// State is stored per <i>instance</i> and never reset on room re-entry. That is the
/// zero-respawn rule: purify the rat, walk away, come back, it is still a rat.
/// </summary>
public partial class EntityStateManager : Node
{
    public static EntityStateManager Instance { get; private set; }

    private readonly Dictionary<string, EntityState> _states = new();

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ---- State -----------------------------------------------------------

    /// <summary>
    /// Seeds an instance's state the first time it is ever encountered. Subsequent calls
    /// are deliberately ignored — a room reload must not undo progress.
    /// </summary>
    public void Register(EntityPlacement placement)
    {
        if (placement?.Entity == null || string.IsNullOrEmpty(placement.InstanceId)) return;
        if (_states.ContainsKey(placement.InstanceId)) return;

        _states[placement.InstanceId] = placement.Entity.InitialState;
    }

    public EntityState GetState(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return EntityState.Corrupted;
        return _states.TryGetValue(instanceId, out var s) ? s : EntityState.Corrupted;
    }

    public bool IsKnown(string instanceId) =>
        !string.IsNullOrEmpty(instanceId) && _states.ContainsKey(instanceId);

    public void SetState(string instanceId, EntityState state)
    {
        if (string.IsNullOrEmpty(instanceId)) return;
        if (_states.TryGetValue(instanceId, out var existing) && existing == state) return;

        _states[instanceId] = state;
        EventBus.Instance?.EmitEntityStateChanged(instanceId, state);
    }

    /// <summary>Cut the anchor. The host deforms but is not yet free.</summary>
    public void Sever(string instanceId) => SetState(instanceId, EntityState.Severed);

    /// <summary>Complete the exorcism. The host reverts to what it was before the vault opened.</summary>
    public void Purify(string instanceId) => SetState(instanceId, EntityState.Purified);

    /// <summary>Every instance the player has met, for the debug panel and the save file.</summary>
    public IReadOnlyDictionary<string, EntityState> All => _states;

    // ---- The table -------------------------------------------------------

    /// <summary>
    /// Resolve what a given entity looks like and offers.
    /// This is the single place in the codebase that knows the Form 1/3 mapping.
    /// </summary>
    public EntityPresentation Resolve(EntityData data, string instanceId)
    {
        if (data == null) return new EntityPresentation { Visible = false };

        return ResolveWaking(data, GetState(instanceId));
    }

    private static EntityPresentation ResolveWaking(EntityData data, EntityState state) => state switch
    {
        // Form 1 — the mutated host, squatting in the road.
        EntityState.Corrupted => new EntityPresentation
        {
            Texture = data.CorruptedTexture,
            Label = data.NameFor(EntityState.Corrupted),
            Modulate = Colors.White,
            BlocksPath = data.BlocksPath,
            Interactable = true,
            Verb = "Approach",
            InteractText = "It will not die by hands. Whatever holds it is not here.",
            PlaceholderColor = new Color("#7a3b2e"),
            PlaceholderSize = new Vector2(320, 240),
            PlaceholderJagged = true,
        },

        // Form 1, deformed. The anchor is cut; the meat has not caught up.
        EntityState.Severed => new EntityPresentation
        {
            Texture = data.SeveredTexture ?? data.CorruptedTexture,
            Label = data.NameFor(EntityState.Severed),
            Modulate = new Color(0.62f, 0.60f, 0.66f),
            BlocksPath = false,
            Interactable = true,
            Verb = "Observe",
            InteractText = "It drags itself in circles, hollowed out. Something left in a hurry.",
            PlaceholderColor = new Color("#4e4048"),
            PlaceholderSize = new Vector2(280, 190),
            PlaceholderJagged = true,
        },

        // Form 3 — what it was before the vault opened.
        _ => new EntityPresentation
        {
            Texture = data.PurifiedTexture,
            Label = data.NameFor(EntityState.Purified),
            Modulate = Colors.White,
            BlocksPath = false,
            Interactable = !string.IsNullOrEmpty(data.HarvestItemId),
            Verb = string.IsNullOrEmpty(data.HarvestItemId) ? "Observe" : "Harvest",
            InteractText = "It regards you without fear, and goes back to foraging.",
            PlaceholderColor = new Color("#8a6b45"),
            PlaceholderSize = new Vector2(150, 110),
            PlaceholderJagged = false,
        },
    };

    // `ResolveDream` stood here: the three Dream cells — the crowned porcelain noble offering
    // "Sever", the half-transparent shattered anchor offering "Banish", and the 22%-alpha echo that
    // could not be touched. Deleted 2026-08-02. Those three cells were the only writers of
    // `EntityPresentation.PlaceholderCrowned`, so the crown went with them.

    // ---- Persistence -----------------------------------------------------

    public Dictionary<string, int> ExportStates() =>
        _states.ToDictionary(kv => kv.Key, kv => (int)kv.Value);

    public void ImportStates(Dictionary<string, int> states)
    {
        _states.Clear();
        if (states == null) return;

        foreach (var kv in states)
        {
            _states[kv.Key] = (EntityState)kv.Value;
            EventBus.Instance?.EmitEntityStateChanged(kv.Key, (EntityState)kv.Value);
        }
    }

    public void ResetAll() => _states.Clear();
}
