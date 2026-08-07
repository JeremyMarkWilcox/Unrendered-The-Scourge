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

        // **No `HasScene` test here, and that was a considered reversal.** A scene-less pocket is enterable:
        // `PocketHost.OnPocketEntered` treats a blank `ScenePath` as a supported state rather than an error,
        // and every fixture in `GatePockets` relies on it — being *inside a pocket* is what those guards probe,
        // and geometry is a separate concern by construction. So the stand-in's guard lives on the other path
        // only (`InspectTakeFromWaking`), where the dangerous direction is. See `PocketBlock.MustBeWalked`.
        return PocketBlock.None;
    }

    /// <summary>Whether the way in is open. See <see cref="InspectEnter"/>.</summary>
    public bool CanEnter(string pocketId) => InspectEnter(pocketId) == PocketBlock.None;

    /// <summary>
    /// Why <see cref="TakeFromWaking"/> would refuse. Shares every refusal with <see cref="InspectEnter"/> and
    /// adds exactly one: <b>a pocket that has a scene must be walked</b>
    /// (<see cref="PocketBlock.MustBeWalked"/>).
    /// </summary>
    /// <remarks>
    /// <b>This is the whole of unit 10's guarantee that the stand-in cannot outlive the 3D pass.</b> The day a
    /// <c>PocketData</c> points at a finished scene, this method starts refusing it — so the reward for that
    /// pocket can only be had by walking it, whatever any caller asks for. Nothing has to be remembered,
    /// removed or unset.
    /// <para>
    /// The <c>InFight</c> refusal is inherited rather than dropped, and it matters more here than on
    /// <c>Enter</c>: a boss pocket's grant <i>severs a tether</i>, and severing mid-fight is exactly what
    /// `combat.md` rules out by reading the tether when a fight <b>ends</b>. A stand-in that granted mid-fight
    /// would be a behavioural difference from the real path in the one place a fight could notice.
    /// </para>
    /// </remarks>
    public PocketBlock InspectTakeFromWaking(string pocketId)
    {
        if (InPocket) return PocketBlock.AlreadyInside;
        if (EncounterManager.Instance?.InFight == true) return PocketBlock.InFight;

        var pocket = GetPocket(pocketId);
        if (pocket == null) return PocketBlock.NoSuchPocket;

        if (WorldStateManager.Instance?.GetFlag(Flags.PocketCleared(pocket.Id)) == true)
            return PocketBlock.AlreadyTaken;

        // The one refusal this path adds, and the only guard the stand-in needs. See the remark above.
        if (pocket.HasScene) return PocketBlock.MustBeWalked;

        return PocketBlock.None;
    }

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
    /// <b>The grant and the flag are one act, and the once-only guard now lives in <see cref="GrantOnce"/></b>
    /// — moved there 2026-08-05 when <see cref="TakeFromWaking"/> became its second caller, so the rule has one
    /// home rather than a copy per route. It used to be inline here, and the reasoning is on that method.
    /// <para>
    /// <b>What is still this method's own is the return</b>, which is why it is not simply
    /// <c>TakeFromWaking</c> with a navigation bolted on: this one is called <i>from inside a pocket</i> by the
    /// 3D scene and hands you back to the screen you left. The stand-in is called from that screen and never
    /// goes anywhere.
    /// </para>
    /// </remarks>
    public bool Complete()
    {
        if (!InPocket) return false;

        var pocket = Current;
        GrantOnce(pocket);
        Return(pocket, completed: true);
        return true;
    }

    /// <summary>
    /// <b>Take what the pocket holds without going in</b> — unit 10. For a pocket whose 3D scene does not
    /// exist yet, this is what clicking its entrance does: the same grant, the same flag, no crossing.
    /// Returns whether anything was granted.
    /// </summary>
    /// <remarks>
    /// <b>It shares <see cref="GrantOnce"/> with <see cref="Complete"/>, and that sharing is the entire
    /// safety argument.</b> There is one copy of *what a pocket gives you* and one copy of the
    /// once-only guard, so the day Jeremy points a pocket at a finished scene the reward does not change —
    /// the route to it does. If this method reimplemented the grant, the two would drift silently, and they
    /// would drift in the worst possible direction: the stand-in is what gets played for months, so the
    /// *scene* path would be the untested one when it finally arrived.
    /// <para>
    /// <b>Why the whole thing is legitimate rather than a hack.</b> Jeremy, 2026-08-05: *"I have a lot to do
    /// in the dream world for each and I will be building each one separately and they don't have any impact
    /// on the 2D for when we build, so decoupling them but also documenting where they hook up and make it an
    /// easy translation later seems like a better option than our constant deferring it."* The measurement
    /// behind that: <c>PocketData</c> is nine fields of which **one** is 3D — <c>ScenePath</c>. Everything
    /// else is what the 2D world receives, so it was always 2D content waiting on a 3D excuse. Fourteen
    /// entrances, eight max-Resolve grants, two verb grants and every blueprint in the game were parked
    /// behind scenes that only exist for the walking.
    /// </para>
    /// <para>
    /// <b>What it does not do, stated because a stand-in that reads as finished is how a project lies to
    /// itself.</b> No verbs, no <c>PocketGate</c>, no <c>DreamCreature</c>, no anchor, no <see cref="Leave"/>,
    /// no <see cref="Eject"/>. Those are the Dream half and they stay unexercised — <c>F11</c> still has to
    /// hand out verbs for testing. And **all eight points of max Resolve now arrive for free**, so nothing
    /// about Resolve *balance* can be concluded from play until real pockets exist. `--verify-content` counts
    /// the scene-less pockets so this cannot quietly read as done.
    /// </para>
    /// <para>
    /// <b>It knowingly breaks invariant 4</b> — *you cannot reach the anchor from the Waking world* — because
    /// a boss pocket's grant severs a tether, and here that happens from a Waking screen. Ruled acceptable by
    /// Jeremy the same day: *"the anchor rewards are always more resolve or abilities that we will test
    /// separate in the 3D world, so we should be good. This is until we are ready to connect them."* The
    /// invariant is not amended, and it comes back intact the moment a boss pocket has a scene — which is why
    /// the switch had to be derived from the scene rather than from a flag someone could forget.
    /// </para>
    /// </remarks>
    public bool TakeFromWaking(string pocketId)
    {
        switch (InspectTakeFromWaking(pocketId))
        {
            case PocketBlock.InFight:
                // The one refusal that is not silent, and it is the same words `Enter` uses, because it is the
                // same refusal — you do not turn your back on a body. It also matters more here than there:
                // a boss pocket's grant severs, and severing mid-fight is precisely what `combat.md` rules
                // out by reading the tether when a fight *ends*.
                EventBus.Instance?.EmitNotify("Not with this in front of you.");
                return false;
            case PocketBlock.None:
                break;
            default:
                // Silent for the same reasons `Enter` is: a mistyped id is authored data and belongs to the
                // sweep, a cleared pocket already has its remnant on the screen saying so, and a pocket with
                // a scene is not this method's business.
                return false;
        }

        var pocket = GetPocket(pocketId);
        GrantOnce(pocket);

        // The only thing this path has that `Complete` does not: something has to say what happened, because
        // there was no 3D scene to show it. Jeremy asked for the pocket named and the grant named — *"maybe
        // just write that dream world they are and what they grant"* — so the line is assembled from the
        // authored data rather than written.
        //
        // **This sentence used to end "and `Describe` is what the entrances' labels use too", which was never
        // true** — an entrance's `Label` is authored per hotspot in its room file, and this call is the only
        // one in the codebase. Corrected 2026-08-06 by the read-back that gated `Describe`; a claim about a
        // second caller is worth checking before writing, because it is what makes a reader think the string is
        // watched from somewhere else.
        EventBus.Instance?.EmitNotify($"[Dream, unbuilt] {pocket.DisplayName} — {Describe(pocket)}");
        return true;
    }

    /// <summary>
    /// Grant a pocket's contents exactly once. The shared half of <see cref="Complete"/> and
    /// <see cref="TakeFromWaking"/>.
    /// </summary>
    /// <remarks>
    /// <b>The guard is before the grant, never after, and the flag is the guard.</b> That is what makes it
    /// atomic in the only sense that matters here: a second call — from a re-entry, a doubled signal, a scene
    /// that calls <c>Complete</c> twice, or an entrance clicked again before its remnant swaps in — grants
    /// nothing. Without it a knowledge pocket would be a max-Resolve farm, and eight is the entire budget
    /// (`build-plan.md` §6 rule 6).
    /// <para>
    /// Extracted 2026-08-05 for unit 10. It was inline in <c>Complete</c>, and copying it into the second
    /// caller would have put the once-only rule in two places — the shape that <c>GrantPage</c>'s remark
    /// already calls the belt to a brace. One home, two callers.
    /// </para>
    /// </remarks>
    private static void GrantOnce(PocketData pocket)
    {
        if (pocket == null) return;

        var world = WorldStateManager.Instance;
        var flag = Flags.PocketCleared(pocket.Id);
        if (world?.GetFlag(flag) == true) return;

        Grant(pocket);
        world?.SetFlag(flag);
    }

    /// <summary>
    /// What this pocket gives you, in words, from the authored fields. Used by the stand-in's notification.
    /// </summary>
    /// <remarks>
    /// <b>Derived from the data rather than authored as a string</b>, on decision 11's reasoning: a
    /// hand-written "what it grants" line beside the fields that grant it is a second copy that can disagree,
    /// and this one would disagree in the direction that hides a mis-authored pocket. A pocket granting
    /// nothing says so, which is how a half-filled <c>.tres</c> announces itself in play.
    /// </remarks>
    public static string Describe(PocketData pocket)
    {
        if (pocket == null) return "nothing";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(pocket.GrantsBlueprintId))
        {
            var page = InventoryCraftingManager.Instance?.GetItem(pocket.GrantsBlueprintId);
            parts.Add(page != null ? page.DisplayName : pocket.GrantsBlueprintId);
        }

        if (pocket.GrantsMaxResolve > 0) parts.Add($"+{pocket.GrantsMaxResolve} max Resolve");
        if (pocket.GrantsAbility != DreamVerb.None) parts.Add($"he can {Words(pocket.GrantsAbility)}");
        if (pocket.Kind == PocketKind.Knot) parts.Add("a knot cut");
        if (!string.IsNullOrEmpty(pocket.TargetEntityId)) parts.Add($"{pocket.TargetEntityId} severed");

        return parts.Count == 0 ? "nothing" : string.Join(", ", parts);
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
                {
                    InventoryCraftingManager.Instance?.Discover(pocket.GrantsBlueprintId);
                    GrantPage(pocket.GrantsBlueprintId);
                }

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

    /// <summary>
    /// Hand over the page for a blueprint just learned, so what he knows is also something he holds.
    /// </summary>
    /// <remarks>
    /// <b>Decision 11, 2026-08-04: a page is a view of the knowledge rather than a second copy of it</b>, and
    /// what makes that safe is that the page id and the blueprint id are <i>the same string</i>. So this takes
    /// no argument of its own — there is nothing to pass that could disagree with what
    /// <see cref="InventoryCraftingManager.Discover"/> was just given, and a second field naming "the page for
    /// this family" is precisely the drift the decision was made to avoid. The fiction is Jeremy's: Daniel
    /// writes the page himself once the Dream gives the craft back, which is why the pocket produces one at all.
    /// <para>
    /// The other direction was already built — <see cref="InventoryCraftingManager.AddItem"/> discovers on
    /// picking up anything <see cref="ItemCategory.Lore"/> — so <c>Discover</c> above is strictly redundant
    /// whenever the page exists. It is called anyway, and deliberately: <b>the knowledge is the grant and the
    /// page is the receipt.</b> A family whose <c>.tres</c> has not been authored yet must still open its
    /// recipes, or an unwritten page would silently cost the player a whole craft family — which is the
    /// failure mode that the guard below turns into a warning instead.
    /// </para>
    /// <para>
    /// <b>Not a silent no-op on a missing file.</b> <c>AddItem</c> accepts any id and only consults the
    /// catalogue to decide whether to <c>Discover</c>, so handing over an unauthored page would put a nameless
    /// stack in the satchel that the grid draws as a blank swatch. Better to say so: the seven
    /// <c>lore_craft_*</c> pages are authored as of 2026-08-05, so the day this warning fires it means a
    /// pocket names a family nobody wrote a page for.
    /// </para>
    /// </remarks>
    private static void GrantPage(string blueprintId)
    {
        var inv = InventoryCraftingManager.Instance;
        if (inv == null) return;

        if (inv.GetItem(blueprintId) == null)
        {
            GD.PushWarning($"[Pocket] Learned '{blueprintId}' with no page authored for it; nothing to hand over.");
            return;
        }

        // Idempotent by the same reasoning as `GrantStartingKit`: the satchel is the only authority that
        // cannot drift, and a re-entered pocket must not print a second copy. `Complete` already refuses a
        // cleared pocket, so this is the belt to that braces.
        if (!inv.Has(blueprintId)) inv.AddItem(blueprintId);
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
