using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// Owns the persistent condition of every entity instance in the game, and resolves the
/// state x realm presentation table.
///
/// The design doc lists an enum {Corrupted, Severed, Purified} and separately describes
/// Form 1 / Form 2 / Form 3. Those are two different axes. Form 2 — the pristine porcelain
/// noble — is what a <i>Corrupted</i> entity looks like when perceived from the Dream. So
/// what you see is a function of both:
///
///                | Waking                      | Dream
///   -------------|-----------------------------|------------------------------
///   Corrupted    | Form 1: nightmare, blocks   | Form 2: spirit anchor, severable
///   Severed      | Form 1 deformed, passable   | shattered anchor, fleeing
///   Purified     | Form 3: mundane, farmable   | benign echo, nearly gone
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
    /// Resolve what a given entity looks like and offers, from a given realm.
    /// This is the single place in the codebase that knows the Form 1/2/3 mapping.
    /// </summary>
    public EntityPresentation Resolve(EntityData data, string instanceId, GameRealm realm)
    {
        if (data == null) return new EntityPresentation { Visible = false };

        var state = GetState(instanceId);

        return realm == GameRealm.Dream
            ? ResolveDream(data, state)
            : ResolveWaking(data, state);
    }

    private static EntityPresentation ResolveWaking(EntityData data, EntityState state) => state switch
    {
        // Form 1 — the mutated host, squatting in the road.
        EntityState.Corrupted => new EntityPresentation
        {
            Texture = data.CorruptedTexture,
            Label = data.NameFor(EntityState.Corrupted, GameRealm.Waking),
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
            Label = data.NameFor(EntityState.Severed, GameRealm.Waking),
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
            Label = data.NameFor(EntityState.Purified, GameRealm.Waking),
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

    private static EntityPresentation ResolveDream(EntityData data, EntityState state) => state switch
    {
        // Form 2 — flawless, porcelain, unbothered. This is the thing you can actually kill.
        EntityState.Corrupted => new EntityPresentation
        {
            Texture = data.SpiritTexture,
            Label = data.NameFor(EntityState.Corrupted, GameRealm.Dream),
            Modulate = Colors.White,
            BlocksPath = false,
            Interactable = true,
            Verb = "Sever",
            InteractText = "",
            PlaceholderColor = new Color("#efe6d6"),
            PlaceholderSize = new Vector2(70, 96),
            PlaceholderJagged = false,
            PlaceholderCrowned = true,
        },

        // The anchor is broken. What is left is receding.
        EntityState.Severed => new EntityPresentation
        {
            Texture = data.ShatteredTexture,
            Label = data.NameFor(EntityState.Severed, GameRealm.Dream),
            Modulate = new Color(1f, 1f, 1f, 0.55f),
            BlocksPath = false,
            Interactable = true,
            Verb = "Banish",
            InteractText = "",
            PlaceholderColor = new Color("#6f4a86"),
            PlaceholderSize = new Vector2(64, 74),
            PlaceholderJagged = true,
            PlaceholderCrowned = true,
        },

        // A warm nothing where a noble stood.
        _ => new EntityPresentation
        {
            Texture = data.EchoTexture,
            Label = "",
            Modulate = new Color(1f, 1f, 1f, 0.22f),
            BlocksPath = false,
            Interactable = false,
            Verb = "",
            InteractText = "",
            PlaceholderColor = new Color("#9fd4e8"),
            PlaceholderSize = new Vector2(40, 40),
            PlaceholderJagged = false,
        },
    };

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
