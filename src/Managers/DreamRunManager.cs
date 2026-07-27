using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// Owns an active Dream heist.
///
/// You are not in the Pixel Realm to fight. You are there to take the relic that holds a
/// noble's world together, before your Resolve runs out or the thing hunting you catches
/// up. Every ending is an ejection — nothing here can kill you. Returning to the Waking
/// world refills Resolve, so a botched run costs you the trip and whatever you burned to
/// open the way, not a lasting wound.
///
/// The maze reuses the ordinary room system. Dream rooms are just <see cref="RoomData"/>
/// with Dream-only hotspots, and the adjacency graph is derived from their Navigate
/// hotspots — so building a new heist is authoring rooms, exactly like everywhere else.
/// </summary>
public partial class DreamRunManager : Node
{
    public static DreamRunManager Instance { get; private set; }

    public DreamHeistData ActiveHeist { get; private set; }
    public bool IsRunning => ActiveHeist != null;

    /// <summary>Which dream room the pursuer currently occupies.</summary>
    public string PursuerRoomId { get; private set; } = "";

    /// <summary>Shortest path in rooms from the pursuer to the player. -1 if unknown.</summary>
    public int PursuerDistance { get; private set; } = -1;

    private readonly Dictionary<string, DreamHeistData> _heists = new();
    private float _moveTimer;
    private bool _ending;

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus != null) bus.RoomEntered -= OnRoomEntered;

        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        LoadHeistRegistry();

        var bus = EventBus.Instance;
        if (bus != null) bus.RoomEntered += OnRoomEntered;
    }

    private void OnRoomEntered(string roomId) => NotifyPlayerMoved(roomId);

    // ---- Registry --------------------------------------------------------

    private void LoadHeistRegistry()
    {
        _heists.Clear();

        using var dir = DirAccess.Open(GameConfig.HeistDataDir);
        if (dir == null) return;

        dir.ListDirBegin();
        for (var file = dir.GetNext(); file != ""; file = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;

            var name = file.EndsWith(".remap") ? file[..^6] : file;
            if (!name.EndsWith(".tres")) continue;

            var heist = GD.Load<DreamHeistData>($"{GameConfig.HeistDataDir}/{name}");
            if (heist == null || string.IsNullOrEmpty(heist.Id)) continue;

            _heists[heist.Id] = heist;
        }
        dir.ListDirEnd();

        GD.Print($"[DreamRun] Registered {_heists.Count} heist(s).");
    }

    public DreamHeistData GetHeist(string heistId)
    {
        if (string.IsNullOrEmpty(heistId)) return null;
        return _heists.TryGetValue(heistId, out var h) ? h : null;
    }

    // ---- Lifecycle -------------------------------------------------------

    public bool Begin(string heistId)
    {
        if (IsRunning) return false;

        var heist = GetHeist(heistId);
        if (heist == null)
        {
            GD.PushError($"[DreamRun] No heist registered with id '{heistId}'.");
            return false;
        }

        // Crossing with nothing left in the tank would eject you on the first frame.
        if (SanityManager.Instance is { Current: <= 1f })
        {
            EventBus.Instance?.EmitNotify("You have nothing left to cross with. Not tonight.");
            return false;
        }

        ActiveHeist = heist;
        _ending = false;
        _moveTimer = 0f;
        PursuerRoomId = heist.PursuerStartRoomId;

        // The Dream is not a view you toggle during a heist. It is where you are.
        RealmTransitionManager.Instance?.SetRealm(GameRealm.Dream, force: true);
        if (RealmTransitionManager.Instance != null)
            RealmTransitionManager.Instance.TransitionsLocked = true;

        NavigationManager.Instance?.GoToRoom(heist.StartRoomId);

        EventBus.Instance?.EmitHeistStarted(heist.Id);
        if (!string.IsNullOrEmpty(heist.EntryText))
            EventBus.Instance?.EmitNotify(heist.EntryText);

        RecomputeProximity();
        return true;
    }

    /// <summary>Take the anchor. The only ending that is worth anything.</summary>
    public void TakeAnchor()
    {
        if (!IsRunning) return;

        var heist = ActiveHeist;
        EntityStateManager.Instance?.Sever(heist.TargetEntityId);

        End(HeistOutcome.Success, string.IsNullOrEmpty(heist.SuccessText)
            ? "The relic comes away warm. Somewhere behind you a great deal of porcelain stops being calm."
            : heist.SuccessText);
    }

    /// <summary>Called by <see cref="SanityManager"/> the moment Resolve bottoms out.</summary>
    public void NotifyResolveExhausted()
    {
        if (!IsRunning || _ending) return;

        End(HeistOutcome.Exhausted, string.IsNullOrEmpty(ActiveHeist.TimeoutText)
            ? "Your nerve goes all at once. The Pixel Realm lets go of you like something it was bored of holding."
            : ActiveHeist.TimeoutText);
    }

    private void Capture()
    {
        if (!IsRunning || _ending) return;

        var heist = ActiveHeist;

        End(HeistOutcome.Captured, string.IsNullOrEmpty(heist.CaptureText)
            ? $"{heist.PursuerName} puts something through you that is not a hand. The world tears, and you are in the cold again."
            : heist.CaptureText);
    }

    private void End(HeistOutcome outcome, string message)
    {
        if (_ending) return;
        _ending = true;

        var heist = ActiveHeist;
        ActiveHeist = null;
        PursuerRoomId = "";
        PursuerDistance = -1;

        if (RealmTransitionManager.Instance != null)
        {
            RealmTransitionManager.Instance.TransitionsLocked = false;
            RealmTransitionManager.Instance.SetRealm(GameRealm.Waking, force: true);
        }

        NavigationManager.Instance?.GoToRoom(heist.ReturnRoomId);

        // The Waking world does not press on your nerve. Coming back is a breath.
        SanityManager.Instance?.Replenish(heist.ResolveRestoredOnReturn);

        EventBus.Instance?.EmitPursuerProximityChanged(-1);
        EventBus.Instance?.EmitHeistEnded(heist.Id, outcome);
        EventBus.Instance?.EmitNotify(message);

        _ending = false;
    }

    // ---- The hunt --------------------------------------------------------

    public override void _Process(double delta)
    {
        if (!IsRunning) return;

        _moveTimer += (float)delta;
        if (_moveTimer < ActiveHeist.PursuerMoveSeconds) return;

        _moveTimer = 0f;
        StepPursuer();
    }

    /// <summary>
    /// One move toward the player through the room graph, with a chance to drift instead.
    /// Perfect pursuit is unreadable and feels arbitrary; a little wandering is what lets a
    /// player believe they can outthink it, which is the feeling worth having.
    /// </summary>
    private void StepPursuer()
    {
        var playerRoom = NavigationManager.Instance?.CurrentRoomId;
        if (string.IsNullOrEmpty(playerRoom) || string.IsNullOrEmpty(PursuerRoomId)) return;

        var neighbours = Neighbours(PursuerRoomId);
        if (neighbours.Count == 0) return;

        string next;
        if (GD.Randf() < ActiveHeist.PursuerWanderChance)
        {
            next = neighbours[(int)(GD.Randi() % (uint)neighbours.Count)];
        }
        else
        {
            next = StepTowards(PursuerRoomId, playerRoom)
                   ?? neighbours[(int)(GD.Randi() % (uint)neighbours.Count)];
        }

        PursuerRoomId = next;

        if (PursuerRoomId == playerRoom)
        {
            Capture();
            return;
        }

        RecomputeProximity();
    }

    /// <summary>Player walked through a door — it may have been the wrong one.</summary>
    public void NotifyPlayerMoved(string roomId)
    {
        if (!IsRunning) return;

        if (roomId == PursuerRoomId)
        {
            Capture();
            return;
        }

        RecomputeProximity();
    }

    private void RecomputeProximity()
    {
        var playerRoom = NavigationManager.Instance?.CurrentRoomId;
        var distance = Distance(PursuerRoomId, playerRoom);

        if (distance == PursuerDistance) return;

        PursuerDistance = distance;
        EventBus.Instance?.EmitPursuerProximityChanged(distance);
    }

    /// <summary>Drain is the clock, and it runs faster when the thing is close.</summary>
    public float CurrentDrainPerSecond()
    {
        if (!IsRunning) return 0f;

        var rate = ActiveHeist.DrainPerSecond;
        if (PursuerDistance is >= 0 and <= 1) rate += ActiveHeist.NearDrainPerSecond;
        return rate;
    }

    // ---- Room graph ------------------------------------------------------
    // Derived from Navigate hotspots, so the maze is authored as rooms and doors like any
    // other part of the game. Realm gating is ignored here on purpose: the pursuer moves
    // through the Dream's own topology regardless of what the player can currently see.

    private static List<string> Neighbours(string roomId)
    {
        var result = new List<string>();
        var room = NavigationManager.Instance?.GetRoom(roomId);
        if (room == null) return result;

        foreach (var hotspot in room.Hotspots)
        {
            if (hotspot == null || hotspot.Kind != HotspotKind.Navigate) continue;
            if (string.IsNullOrEmpty(hotspot.TargetRoomId)) continue;
            if (!result.Contains(hotspot.TargetRoomId)) result.Add(hotspot.TargetRoomId);
        }

        return result;
    }

    /// <summary>Breadth-first hop count. -1 when there is no path.</summary>
    private static int Distance(string fromRoomId, string toRoomId)
    {
        if (string.IsNullOrEmpty(fromRoomId) || string.IsNullOrEmpty(toRoomId)) return -1;
        if (fromRoomId == toRoomId) return 0;

        var seen = new HashSet<string> { fromRoomId };
        var frontier = new Queue<(string Room, int Depth)>();
        frontier.Enqueue((fromRoomId, 0));

        while (frontier.Count > 0)
        {
            var (room, depth) = frontier.Dequeue();

            foreach (var neighbour in Neighbours(room))
            {
                if (!seen.Add(neighbour)) continue;
                if (neighbour == toRoomId) return depth + 1;
                frontier.Enqueue((neighbour, depth + 1));
            }
        }

        return -1;
    }

    /// <summary>First hop along a shortest path, or null if there is no route.</summary>
    private static string StepTowards(string fromRoomId, string toRoomId)
    {
        string best = null;
        var bestDistance = int.MaxValue;

        foreach (var neighbour in Neighbours(fromRoomId))
        {
            if (neighbour == toRoomId) return neighbour;

            var distance = Distance(neighbour, toRoomId);
            if (distance < 0 || distance >= bestDistance) continue;

            bestDistance = distance;
            best = neighbour;
        }

        return best;
    }

    /// <summary>
    /// Move the pursuer by fiat. For the verifier and for debugging a maze layout — no
    /// gameplay path calls this.
    /// </summary>
    public void DebugSetPursuerRoom(string roomId)
    {
        if (!IsRunning) return;

        PursuerRoomId = roomId;
        _moveTimer = 0f;
        RecomputeProximity();
    }

    /// <summary>Abandon any run without ceremony. Used by save/load and the verifier.</summary>
    public void ForceClear()
    {
        ActiveHeist = null;
        PursuerRoomId = "";
        PursuerDistance = -1;
        _ending = false;

        if (RealmTransitionManager.Instance != null)
            RealmTransitionManager.Instance.TransitionsLocked = false;
    }
}
