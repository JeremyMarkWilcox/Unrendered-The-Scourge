using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Core;

namespace Unrendered.Managers;

/// <summary>
/// The authoritative record of what has happened: boolean world flags, and nothing else since
/// 2026-08-03. Everything in the game derives from this, and the save file is essentially a
/// serialisation of it.
/// </summary>
/// <remarks>
/// It used to hold a second dictionary of zone phases beside the flags. Collapsing that into flag names
/// (<see cref="Flags.Cleansed"/>) is what made the persistence half of this class disappear — one
/// vocabulary means one export, one import and one rebroadcast, and the <c>domain.subject.state</c>
/// shape keeps a class of thing countable by prefix, which a per-zone enum never was.
/// </remarks>
public partial class WorldStateManager : Node
{
    public static WorldStateManager Instance { get; private set; }

    private readonly Dictionary<string, bool> _flags = new();

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
    //
    // `_zonePhases`, `GetZonePhase`, `SetZonePhase`, `IsZoneCleansed`, `ZoneCorruptionScalar` and
    // `ExportZonePhases` all stood here until 2026-08-03 (unit 2). The state they held is now one flag
    // per screen — `Flags.Cleansed(roomId)`, written by `MendManager.Reevaluate` — which is why this
    // class got *shorter* rather than gaining a per-screen dictionary beside the flags it already has.
    //
    // Two of the six had a live invariant, and following `deleting-a-gated-mechanism` the probes moved
    // rather than being deleted with them: "the zone is cleansed" and "zone phase restored" became the
    // same assertions about a `screen.<id>.cleansed` flag in `--verify-slice`. Those probes then had to
    // move a *second* time — they landed on `screen.waystation_yard.cleansed`, and unit 3b deleted the
    // waystation, so they read a synthetic `verify_loop` screen now. Worth stating: a probe re-anchored
    // onto content is a probe that moves again whenever the content does.
    //
    // **`ZoneCorruptionScalar` is the one honest deletion with no surviving invariant.** Nothing read
    // it — Resolve drain has been boss-only aura since `aura-scoped-to-bosses` — so its probe ("the
    // zone reads as inert") went with it instead of moving. A check that still passes while measuring
    // nothing is worse than one check fewer.

    // ---- Persistence -----------------------------------------------------

    public Dictionary<string, bool> ExportFlags() => new(_flags);

    /// <summary>
    /// Replace the whole world with a saved one. Takes flags alone now: a cleansed screen is a flag, so
    /// the save format lost a field and nothing had to be added to carry it.
    /// </summary>
    public void ImportState(Dictionary<string, bool> flags)
    {
        _flags.Clear();

        if (flags != null)
            foreach (var kv in flags) _flags[kv.Key] = kv.Value;

        // Rebroadcast so every live view resyncs without a reload.
        foreach (var kv in _flags) EventBus.Instance?.EmitWorldFlagChanged(kv.Key, kv.Value);
    }

    public void ResetAll() => _flags.Clear();
}
