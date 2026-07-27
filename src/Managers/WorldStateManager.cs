using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// The authoritative record of what has happened. Boolean world flags plus a phase per
/// zone. Everything else in the game derives from this, and the save file is essentially
/// a serialisation of it.
/// </summary>
public partial class WorldStateManager : Node
{
    public static WorldStateManager Instance { get; private set; }

    private readonly Dictionary<string, bool> _flags = new();
    private readonly Dictionary<string, ZonePhase> _zonePhases = new();

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ---- Flags -----------------------------------------------------------

    public bool GetFlag(string flag, bool fallback = false)
    {
        if (string.IsNullOrEmpty(flag)) return fallback;
        return _flags.TryGetValue(flag, out var v) ? v : fallback;
    }

    public void SetFlag(string flag, bool value = true)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (_flags.TryGetValue(flag, out var existing) && existing == value) return;

        _flags[flag] = value;
        EventBus.Instance?.EmitWorldFlagChanged(flag, value);
    }

    // ---- Zones -----------------------------------------------------------

    public ZonePhase GetZonePhase(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId)) return ZonePhase.Corrupted;
        return _zonePhases.TryGetValue(zoneId, out var p) ? p : ZonePhase.Corrupted;
    }

    public void SetZonePhase(string zoneId, ZonePhase phase)
    {
        if (string.IsNullOrEmpty(zoneId)) return;
        if (GetZonePhase(zoneId) == phase) return;

        _zonePhases[zoneId] = phase;
        EventBus.Instance?.EmitZonePhaseChanged(zoneId, phase);
    }

    public bool IsZoneCleansed(string zoneId) => GetZonePhase(zoneId) == ZonePhase.Purified;

    /// <summary>
    /// Corruption multiplier a room in this zone should feel. Purified zones are inert;
    /// an active front is somewhat calmer than untouched ground.
    /// </summary>
    public float ZoneCorruptionScalar(string zoneId) => GetZonePhase(zoneId) switch
    {
        ZonePhase.Purified => 0f,
        ZonePhase.ActiveFront => 0.65f,
        _ => 1f,
    };

    // ---- Persistence -----------------------------------------------------

    public Dictionary<string, bool> ExportFlags() => new(_flags);

    public Dictionary<string, int> ExportZonePhases() =>
        _zonePhases.ToDictionary(kv => kv.Key, kv => (int)kv.Value);

    public void ImportState(Dictionary<string, bool> flags, Dictionary<string, int> zonePhases)
    {
        _flags.Clear();
        _zonePhases.Clear();

        if (flags != null)
            foreach (var kv in flags) _flags[kv.Key] = kv.Value;

        if (zonePhases != null)
            foreach (var kv in zonePhases) _zonePhases[kv.Key] = (ZonePhase)kv.Value;

        // Rebroadcast so every live view resyncs without a reload.
        foreach (var kv in _flags) EventBus.Instance?.EmitWorldFlagChanged(kv.Key, kv.Value);
        foreach (var kv in _zonePhases) EventBus.Instance?.EmitZonePhaseChanged(kv.Key, kv.Value);
    }

    public void ResetAll()
    {
        _flags.Clear();
        _zonePhases.Clear();
    }
}
