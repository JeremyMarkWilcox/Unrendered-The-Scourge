using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Managers;

/// <summary>
/// Owns being inside a Dream pocket: which one, what it grants, and where you come back to. Unit 6, built
/// to `docs/combat.md` §15 and the discussion of 2026-08-04.
///
/// <para><b>It replaced <c>DreamRunManager</c> (deleted in 6b), and it is much smaller because a pocket
/// is a 3D scene rather than a graph of rooms.</b> That class derived a maze, a pursuer's path and
/// "is this room in the Dream" by walking <c>Navigate</c> hotspots; none of those have anything to derive
/// now. A pocket id never appears in <see cref="NavigationManager"/> at all, which is the point — Jeremy,
/// 2026-08-04: <i>"keep building the two worlds separated besides travel to the 3D level from the 2D and
/// then the 3D back to the 2D."</i></para>
///
/// <para><b>No <c>_Process</c>, and there must never be one.</b> The old model's clock was Resolve draining
/// per second; Resolve is a fuel tank spent by verbs (<c>spirit-light-and-resolve</c>), so a pocket has no
/// clock to tick and adding one would reintroduce a currency the design removed.</para>
///
/// <para><b>It loads no scenes.</b> Entering sets state and announces it; <c>PocketHost</c> hears the
/// announcement and does the loading. That split is invariant 10 — game logic in managers — and it is what
/// makes <see cref="Enter"/> work in a headless run with no geometry anywhere, which is what kept the five
/// Waking-side guards falsifiable when the heist was deleted.</para>
/// </summary>
public partial class PocketManager : Node
{
    public static PocketManager Instance { get; private set; }

    private readonly Dictionary<string, PocketData> _pockets = new();

    /// <summary>The pocket you are inside, or null.</summary>
    public PocketData Current { get; private set; }

    /// <summary>Whether the player is inside a pocket right now.</summary>
    public bool InPocket => Current != null;

    /// <summary>Id of the pocket you are inside, or "".</summary>
    public string CurrentPocketId => Current?.Id ?? "";

    /// <summary>
    /// The Waking screen to put the player back on. Remembered on entry rather than authored, because the
    /// entrance is a hotspot on a specific screen and authoring the return would store where the door is
    /// twice — the <c>IsSanctuary</c> mistake, which this project has made once and does not repeat.
    /// </summary>
    public string ReturnRoomId { get; private set; } = "";

    /// <summary>
    /// <b>The one question seven guards ask, in one place.</b> True while the player is somewhere the
    /// Waking world's rules do not apply.
    /// </summary>
    /// <remarks>
    /// <b>It existed to be edited once instead of twice, and it paid.</b> It read
    /// <c>InPocket || DreamRunManager.IsRunning</c> for the length of unit 6a — <c>CampManager</c> asks it in
    /// four places, and <c>InventoryCraftingManager</c>, <c>Hud</c> and <c>SaveService</c> once each — so
    /// retiring the heist deleted **one clause here** and touched none of those seven files. Pointing them
    /// all at the pocket directly would have meant editing every one of them twice.
    /// <para>
    /// With one term it now looks like a wrapper around <see cref="InPocket"/>, and it is kept anyway: the
    /// boss aura and the Dream's abilities both want *"are the Waking rules suspended"* as a question with
    /// one answer, and collapsing it into seven direct reads is how the next mechanism ends up asking six of
    /// them and missing one.
    /// </para>
    /// <para>
    /// Static and null-tolerant so a guard can ask before the autoloads have finished registering, which
    /// <c>SaveService</c> does during a load.
    /// </para>
    /// </remarks>
    public static bool InsideTheDream =>
        Instance is { InPocket: true };

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready() => LoadPocketRegistry();

    // ---- Registry --------------------------------------------------------

    private void LoadPocketRegistry()
    {
        _pockets.Clear();

        using var dir = DirAccess.Open(GameConfig.PocketDataDir);
        if (dir == null)
        {
            // Not an error: `data/pockets/` is empty until Jeremy's 3D pass authors one, and a pushed error
            // on every boot would train people to ignore the log. The gates count the directory against the
            // registry, so a directory that exists and fails to load is caught there instead.
            GD.Print($"[Pockets] No pocket directory at {GameConfig.PocketDataDir}");
            return;
        }

        dir.ListDirBegin();
        for (var file = dir.GetNext(); file != ""; file = dir.GetNext())
        {
            if (dir.CurrentIsDir()) continue;

            var name = file.EndsWith(".remap") ? file[..^6] : file;
            if (!name.EndsWith(".tres")) continue;

            var pocket = GD.Load<PocketData>($"{GameConfig.PocketDataDir}/{name}");
            if (pocket == null)
            {
                GD.PushWarning($"[Pockets] Failed to load pocket resource: {name}");
                continue;
            }

            if (string.IsNullOrEmpty(pocket.Id))
            {
                GD.PushWarning($"[Pockets] Pocket resource {name} has a blank Id; skipping.");
                continue;
            }

            if (_pockets.ContainsKey(pocket.Id))
            {
                GD.PushWarning($"[Pockets] Duplicate pocket Id '{pocket.Id}' in {name}.");
                continue;
            }

            _pockets[pocket.Id] = pocket;
        }
        dir.ListDirEnd();

        GD.Print($"[Pockets] Registered {_pockets.Count} pocket(s): {string.Join(", ", _pockets.Keys)}");
    }

    public PocketData GetPocket(string pocketId)
    {
        if (string.IsNullOrEmpty(pocketId)) return null;
        return _pockets.TryGetValue(pocketId, out var p) ? p : null;
    }

    public IReadOnlyDictionary<string, PocketData> Pockets => _pockets;

    /// <summary>
    /// Register a pocket built in code, on <see cref="NavigationManager.DebugRegisterRoom"/>'s precedent
    /// and for the same reason: <c>data/pockets/</c> is empty until the 3D pass, and the mechanism has to be
    /// provable before there is content. Not for gameplay.
    /// </summary>
    public void DebugRegisterPocket(PocketData pocket)
    {
        if (pocket == null || string.IsNullOrEmpty(pocket.Id)) return;
        _pockets[pocket.Id] = pocket;
    }

    // ---- Whether you may go in -------------------------------------------

    /// <summary>
    /// Why <see cref="Enter"/> would refuse. Same inspect/act split as everywhere else in this codebase —
    /// the hotspot's availability and the act must ask one question.
    /// </summary>
    /// <remarks>
    /// <b>The fight refusal is not tidiness, it is a correctness requirement.</b> A pocket is not a room, so
    /// entering one does <i>not</i> navigate — which means <c>EncounterManager.OnRoomEntered</c>, the
    /// backstop that stops a fight outliving its screen, never fires. Without this guard you could walk into
    /// a pocket mid-fight, wander a 3D scene, and come back still committed to a creature: unit 3a's defect
    /// exactly, in a new costume. It also stops a pocket being a free escape, since flight costs a parting
    /// blow and this would not.
    /// </remarks>
    public PocketBlock InspectEnter(string pocketId)
    {
        if (InPocket) return PocketBlock.AlreadyInside;
        if (EncounterManager.Instance?.InFight == true) return PocketBlock.InFight;

        var pocket = GetPocket(pocketId);
        if (pocket == null) return PocketBlock.NoSuchPocket;

        if (WorldStateManager.Instance?.GetFlag(Flags.PocketCleared(pocket.Id)) == true)
            return PocketBlock.AlreadyTaken;

        return PocketBlock.None;
    }

    /// <summary>Whether the way in is open. See <see cref="InspectEnter"/>.</summary>
    public bool CanEnter(string pocketId) => InspectEnter(pocketId) == PocketBlock.None;

    // ---- Going in and coming out -----------------------------------------

    /// <summary>
    /// Cross into a pocket. Returns whether you went.
    /// </summary>
    /// <remarks>
    /// The realm flips and transitions lock, exactly as the deleted <c>DreamRunManager.Begin</c> did — you are
    /// not looking at the Dream, you are in it (invariant 4, and the epitaph of spectral vision).
    /// <b>Nothing is loaded here</b>: the signal is the last thing to fire, and <c>PocketHost</c> is what
    /// turns it into geometry.
    /// </remarks>
    public bool Enter(string pocketId)
    {
        switch (InspectEnter(pocketId))
        {
            case PocketBlock.NoSuchPocket:
                // Silent, like the two below, and that is a deliberate change of mind. This pushed an error
                // until the gates exercised it and printed a stack trace into an otherwise green run — the
                // shape `FightPanel.Bind`'s `optional` parameter exists to stop, because a red line under a
                // pass costs someone a detour to rule out. A mistyped id is *authored* data, so it belongs to
                // a sweep rather than to a click: the gates cross every `CrossToDream` hotspot against the
                // registry, which catches it before anyone can walk into it.
                return false;
            case PocketBlock.AlreadyInside:
                return false;
            case PocketBlock.AlreadyTaken:
                // Unreachable from a hotspot, which hides itself on the flag — so this arm is for a
                // reload, a debug key or a second door authored onto one pocket. Silent: the broken
                // remnant on the screen already says what happened.
                return false;
            case PocketBlock.InFight:
                EventBus.Instance?.EmitNotify("Not with this in front of you.");
                return false;
        }

        Current = GetPocket(pocketId);
        ReturnRoomId = NavigationManager.Instance?.CurrentRoomId ?? "";

        var realm = RealmTransitionManager.Instance;
        if (realm != null)
        {
            realm.SetRealm(GameRealm.Dream, force: true);
            realm.TransitionsLocked = true;
        }

        EventBus.Instance?.EmitPocketEntered(Current.Id);
        return true;
    }

    /// <summary>
    /// <b>The pocket was finished.</b> One of the two calls a pocket scene makes — Jeremy, 2026-08-04:
    /// <i>"going back will be written in the 3D world."</i> Grants, writes the flag, and returns you.
    ///
    /// <para>Returns whether anything happened, so the scene can tell a completion from a double call.</para>
    /// </summary>
    /// <remarks>
    /// <b>The grant and the flag are one act, and the flag is checked first.</b> That is what makes it
    /// atomic in the only sense that matters here: a second <c>Complete</c> — from a re-entry, a doubled
    /// signal, or a scene that calls it twice — grants nothing. Without it a knowledge pocket would be a
    /// max-Resolve farm, and eight is the entire budget (`build-plan.md` §6 rule 6).
    /// </remarks>
    public bool Complete()
    {
        if (!InPocket) return false;

        var pocket = Current;
        var world = WorldStateManager.Instance;
        var flag = Flags.PocketCleared(pocket.Id);

        // Guard before granting, never after. See the remark above.
        if (world?.GetFlag(flag) != true)
        {
            Grant(pocket);
            world?.SetFlag(flag);
        }

        Return(pocket, completed: true);
        return true;
    }

    /// <summary>
    /// <b>The player left without finishing.</b> The other call the scene makes, and the one the whole
    /// softlock argument rests on: <b>it writes nothing.</b>
    /// </summary>
    /// <remarks>
    /// Nothing in the Dream can kill you (invariant 2), so leaving early is the only failure a pocket has —
    /// and the entrance must survive it. If an abandoned pocket broke its own door, a survivable mistake
    /// would permanently delete a craft family and a point of max Resolve, which is a dead save reached by
    /// playing badly. Ruled 2026-08-04 and gated as its own check.
    /// </remarks>
    public bool Leave()
    {
        if (!InPocket) return false;

        Return(Current, completed: false);
        return true;
    }

    /// <summary>
    /// Whether the player holds a Dream verb. <see cref="DreamVerb.Suppress"/> is always true — it is held
    /// from the first frame and deliberately carries no flag.
    /// </summary>
    /// <remarks>
    /// Static and on this class rather than on a Dream-side node, because the 3D scenes are the *readers*: a
    /// <c>PocketGate</c> hiding a Float-only ledge asks this, and fourteen cloned scenes asking it must all
    /// get the same answer from the same place. Same reason <c>InsideTheDream</c> lives here.
    /// </remarks>
    public static bool Holds(DreamVerb verb) => verb switch
    {
        DreamVerb.None => true,
        DreamVerb.Suppress => true,
        _ => WorldStateManager.Instance?.GetFlag(Flags.VerbLearned(verb)) == true,
    };

    /// <summary>
    /// Plain words for a verb, for the line the grant prints. Prose rather than the enum member, on
    /// <c>EncounterManager.Words</c>'s precedent: the enum is a programmer's vocabulary.
    /// </summary>
    public static string Words(DreamVerb verb) => verb switch
    {
        DreamVerb.Suppress => "put the light out",
        DreamVerb.Float => "leave the ground",
        DreamVerb.Flare => "push the light out",
        _ => "do nothing new",
    };

    /// <summary>What the pocket pays out. The only place <see cref="PocketKind"/> branches.</summary>
    private static void Grant(PocketData pocket)
    {
        switch (pocket.Kind)
        {
            case PocketKind.Knowledge:
                if (!string.IsNullOrEmpty(pocket.GrantsBlueprintId))
                    InventoryCraftingManager.Instance?.Discover(pocket.GrantsBlueprintId);

                if (pocket.GrantsMaxResolve > 0)
                    ResolveManager.Instance?.RaiseMax(pocket.GrantsMaxResolve);
                break;

            case PocketKind.Knot:
                // The knot's whole mechanical job. The door it opens — three flags on the Snarl — was built
                // and gated in unit 1, so this is one flag and no more. The beat *text* is story, and story
                // is last (ruled 2026-08-04).
                WorldStateManager.Instance?.SetFlag(Flags.KnotCut(pocket.Id));
                break;

            case PocketKind.Boss:
                // The only way a tether is ever cut. Invariant 4: the anchor is unreachable from out there,
                // which is why this cannot be a Waking-world verb.
                if (!string.IsNullOrEmpty(pocket.TargetEntityId))
                    EntityStateManager.Instance?.Sever(pocket.TargetEntityId);

                // And the verb, from the same act. Ratified 2026-08-04: *"you get the dream abilities after
                // pulling the anchor for boss 1 and 2. Boss 3 ends the game so you don't."* So the grant is
                // here rather than on the Waking-world fight that follows, and the third boss pocket simply
                // leaves the field `None` — no special case, just nothing left to spend a verb in.
                if (pocket.GrantsAbility != DreamVerb.None)
                {
                    WorldStateManager.Instance?.SetFlag(Flags.VerbLearned(pocket.GrantsAbility));
                    EventBus.Instance?.EmitNotify(
                        $"Something of his own comes back to him. He knows how to {Words(pocket.GrantsAbility)}.");
                }
                break;
        }
    }

    /// <summary>Put the player back on the screen they left. Shared by both exits so they cannot drift.</summary>
    private void Return(PocketData pocket, bool completed, bool navigate = true)
    {
        var destination = navigate ? ReturnRoomId : "";

        Current = null;
        ReturnRoomId = "";

        // Every hold dropped, on every exit route. A verb still held after an ejection would keep draining in
        // the Waking world, because `DreamVerbs.RatePerSecond` does not ask where you are — and §6 rules out
        // ambient drain permanently. One call here rather than one per exit is what makes that airtight.
        DreamVerbs.ReleaseAll();

        var realm = RealmTransitionManager.Instance;
        if (realm != null)
        {
            realm.TransitionsLocked = false;
            realm.SetRealm(GameRealm.Waking, force: true);
        }

        // Cleared *before* navigating, and before the signal, because both have subscribers that ask
        // `InsideTheDream` — `CampManager.OnRoomEntered` refuses to recognise a camp inside the Dream, so
        // arriving while this still read "in a pocket" would silently skip the refill on a camp you walked
        // out of. Invariant 13's lesson generalised: set the state, then announce it.
        if (!string.IsNullOrEmpty(destination)) NavigationManager.Instance?.GoToRoom(destination);

        // No Resolve refill on the way out, deliberately. The old model restored the bar because the run
        // *was* a drain on it; under the fuel-tank model a free top-up on exit would make every ability
        // free. Camps are the refill point (§17), and that is the one place it should be.
        EventBus.Instance?.EmitPocketLeft(pocket.Id, completed);
    }

    /// <summary>
    /// **Thrown out.** Tears the pocket down, writes nothing, and <b>navigates nowhere</b> — the caller owns
    /// where you land. Returns whether you were in a pocket to be thrown out of.
    /// </summary>
    /// <remarks>
    /// <b>A third exit rather than a flag on <see cref="Leave"/>, and the difference is the destination.</b>
    /// `Leave` puts you back on the screen the entrance was on, because you chose to walk out. An ejection
    /// hands the destination to <c>CampManager.RespawnAtCamp</c>, because your nerve is empty and only a camp
    /// refills it — returning you to the entrance would leave you standing next to a pocket you cannot use.
    /// Same reasoning that makes the Vitality collapse *convenience rather than punishment*.
    /// <para>
    /// <b>It writes nothing, which is the whole softlock rule.</b> No flag, no grant, so the entrance survives
    /// and an ejection costs only the trip. That is what makes running out of nerve and being banished by a
    /// boss the same call — ruled 2026-08-04, and one path is the point.
    /// </para>
    /// <para>
    /// It still emits <c>PocketLeft</c> so <c>PocketHost</c> frees the geometry. Skipping that would leave a
    /// 3D scene parented and rendering behind a 2D screen, which is the cost `PocketHost` disables its
    /// viewports to avoid.
    /// </para>
    /// </remarks>
    public bool Eject()
    {
        if (!InPocket) return false;

        Return(Current, completed: false, navigate: false);
        return true;
    }

    /// <summary>
    /// Abandon a pocket without ceremony, granting nothing and navigating nowhere. For save/load and the
    /// gates, on the deleted <c>DreamRunManager.ForceClear</c>'s precedent.
    /// </summary>
    /// <remarks>
    /// A load calls this: pocket state is deliberately not persisted, so a save taken inside one restores
    /// you to the Waking room the file names, with the entrance intact and nothing granted. It keeps 3D
    /// state out of the save format entirely, and it is the same ruling the heist already had
    /// (*"a heist does not survive a load"*).
    /// </remarks>
    public void ForceClear()
    {
        Current = null;
        ReturnRoomId = "";

        if (RealmTransitionManager.Instance != null)
            RealmTransitionManager.Instance.TransitionsLocked = false;
    }

    /// <summary>For the gates and for starting a new game.</summary>
    public void ResetAll() => ForceClear();
}
