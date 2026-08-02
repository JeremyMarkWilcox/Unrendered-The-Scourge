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

    // ---- Material refill (items.md §7) -------------------------------------

    /// <summary>
    /// Re-arms every material node that is marked to come back, in the chapters named. Returns how
    /// many nodes were restored, so a caller can stay silent when nothing changed.
    /// </summary>
    /// <remarks>
    /// The one legal supply change in the game, and it fires on a <b>story milestone</b> — never a
    /// clock, never a step counter (items.md §7, combat.md §19). It takes its rooms as an argument
    /// rather than asking <c>NavigationManager</c> for them, which is what lets the gates drive it
    /// against synthetic content with no scene tree.
    /// <para>
    /// Idempotent: refilling twice restores nothing the second time, because clearing a flag that is
    /// already clear is not a restore. That matters because the trigger is a boss death, and a boss
    /// death that gets replayed through a save reload must not double the world's supply.
    /// </para>
    /// </remarks>
    public int RefillMaterialNodes(IEnumerable<Data.RoomData> rooms, params string[] chapterIds)
    {
        if (rooms == null || chapterIds == null || chapterIds.Length == 0) return 0;

        var wanted = new HashSet<string>(chapterIds);
        var restored = 0;

        foreach (var room in rooms)
        {
            if (room?.Hotspots == null || !wanted.Contains(room.ChapterId)) continue;

            foreach (var h in room.Hotspots)
            {
                if (h == null || !h.RefillsAtMilestone) continue;
                if (h.Kind != HotspotKind.Harvest) continue;

                var flag = h.ResolvedHarvestedFlag;
                if (!GetFlag(flag)) continue;

                SetFlag(flag, false);
                restored++;
            }
        }

        return restored;
    }

    // ---- Knots (combat.md §23) ---------------------------------------------

    /// <summary>How many of the given knot pockets have been cut.</summary>
    public int CutKnotCount(IEnumerable<string> knotPocketIds) =>
        knotPocketIds?.Count(id => !string.IsNullOrEmpty(id) && GetFlag(Flags.KnotCut(id))) ?? 0;

    /// <summary>
    /// Which beat the <b>next</b> cut plays: 1-based, and driven by the count of cuts already made
    /// rather than by which pocket the player walked into.
    /// </summary>
    /// <remarks>
    /// Returns 0 once every knot is cut, meaning there is no beat left to play. Callers should treat
    /// 0 as "say nothing" rather than clamping to the last beat and repeating it.
    /// <para>
    /// <b>The authoring rule this shape produces, recorded here because the text has no home yet:</b>
    /// a beat may not mention the pocket it fires in. No darkness, no height, no distance — beat 2
    /// plays wherever the player happens to be, so it can only talk about the knot itself, which is
    /// the same craft in all three (combat.md §23). Three atmospheric paragraphs that each describe
    /// their own room is the easy way to break this, and it breaks silently: it reads fine in the
    /// editor and wrong in play for two thirds of players. The three strings land in <b>unit 6</b>,
    /// with the rest of the pocket's grant-and-report contract.
    /// </para>
    /// </remarks>
    public int NextKnotBeatOrdinal(IEnumerable<string> knotPocketIds)
    {
        var ids = knotPocketIds?.Where(id => !string.IsNullOrEmpty(id)).ToList();
        if (ids == null || ids.Count == 0) return 0;

        var cut = CutKnotCount(ids);
        return cut >= ids.Count ? 0 : cut + 1;
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
