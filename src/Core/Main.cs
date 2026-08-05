using System.Collections.Generic;
using System.Linq;
using Godot;
using Unrendered.Data;
using Unrendered.Managers;
using Unrendered.Save;
using Unrendered.Tools;
using Unrendered.UI;
using Unrendered.World;

namespace Unrendered.Core;

/// <summary>
/// Root of the running game. Owns the room view and the HUD, kicks off the first
/// navigation once the autoloads have registered themselves, and hosts the debug keys
/// that make the milestone gates checkable by hand.
/// </summary>
public partial class Main : Node
{
    private RoomView _roomView;
    private Hud _hud;
    private PocketHost _pockets;

    /// <summary>The 2D→3D seam. Exposed so the gates can ask what it has loaded.</summary>
    public PocketHost Pockets => _pockets;

    public override void _Ready()
    {
        _roomView = GetNode<RoomView>("RoomView");
        _hud = GetNode<Hud>("Hud");

        // Unit 6's seam, built here rather than authored into `Main.tscn` for the reason the HUD is built in
        // code: it has no layout worth looking at. It goes **between** the room view and the HUD — over the
        // Waking world, which it replaces, and under the HUD, which stays in colour on top of the Dream's
        // greyscale. Added before `StartGame` so a pocket entered on the first frame has somewhere to land.
        _pockets = new PocketHost();
        AddChild(_pockets);
        MoveChild(_pockets, _hud.GetIndex());
        _pockets.Bind(_roomView);

        // Deferred so every autoload's _Ready (notably the room registry scan) has run.
        CallDeferred(nameof(StartGame));
    }

    private void StartGame()
    {
        if (NavigationManager.Instance?.GoToRoom(GameConfig.StartingRoomId) != true)
        {
            GD.PushError($"[Main] Could not enter starting room '{GameConfig.StartingRoomId}'.");

            // Quit rather than return when the gates are the reason we booted. Returning leaves a
            // headless run sitting in an empty scene tree forever, which reads as a hung machine
            // instead of a broken build — found 2026-08-02 break-testing an unparseable starting
            // room, which produced no output at all rather than a red check.
            if (Boot.VerifySlice) GetTree().Quit(1);
            return;
        }

        if (ResolveManager.Instance != null) ResolveManager.Instance.Active = true;

        // What he already owns, before the gates get a chance to empty the satchel on their way past.
        // Safe ahead of a load: ImportStacks clears first, so the file wins either way.
        InventoryCraftingManager.Instance?.GrantStartingKit();

        if (Boot.VerifySlice)
        {
            GetTree().Quit(SliceVerifier.Run(_roomView, _hud, _pockets));
            return;
        }

        // Before `--shots` and after the gates, which is only a reading order — it shares nothing with
        // either. It reads authored content and exits 0 unconditionally: a bill is a to-do list while a
        // chapter is being built, so it must never be able to fail a build (`build-plan.md` §6).
        if (Boot.VerifyContent)
        {
            GetTree().Quit(ContentReport.Run());
            return;
        }

        if (Boot.CaptureShots)
        {
            _ = ScreenshotTool.CaptureSlice(this, _hud);
            return;
        }

        EventBus.Instance?.EmitNotify("Cold. The track keeps going.");
    }

    // ---- Debug -----------------------------------------------------------

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsEcho() || !@event.IsPressed()) return;

        // Both go through `Hud` rather than reaching into the panel, because which panels may be up at once
        // is the HUD's policy and both of these draw a dim that stacks with the other's.
        if (@event.IsActionPressed("inventory"))
        {
            _hud?.ToggleSatchel();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("camp"))
        {
            // Unit 5's return-to-camp verb. A real key rather than a debug one — it is the amendment in
            // §17, not scaffolding — so it is absent from the F6–F8 demolition list and stays when the
            // journal lands. It refuses with a line when there is no leaving from here, which is why this
            // branch has no guard of its own.
            _hud?.ToggleCampMenu();
            GetViewport().SetInputAsHandled();
        }
        // Only swallowed when there is something open to close, so cancel stays available to
        // whatever wants it next.
        else if (@event.IsActionPressed("cancel") && _hud?.Inventory?.IsOpen == true)
        {
            _hud.Inventory.Close();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("cancel") && _hud?.Camp?.IsOpen == true)
        {
            _hud.Camp.Close();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_panel"))
        {
            _hud.ToggleDebugPanel(_roomView);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_cycle_entity"))
        {
            CycleEntityStates();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_pocket"))
        {
            TogglePocket();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_wound"))
        {
            // Until a hazard is authored into a room, this is the only way to watch the body
            // move at all. Deliberately a *wound* rather than a set-to-value: it exercises the
            // real path, including the collapse announcement at zero.
            VitalityManager.Instance?.Damage(15f);
            EventBus.Instance?.EmitNotify("[debug] took a wound");
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_learn_all"))
        {
            LearnEveryCraftFamily();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_learn_verbs"))
        {
            LearnGrantableDreamVerbs();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_craft_here"))
        {
            CraftEverythingPossibleHere();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_rig_here"))
        {
            RigEverythingPossibleHere();
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_save"))
        {
            if (SaveService.Save()) EventBus.Instance?.EmitNotify("Saved.");
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_load"))
        {
            if (SaveService.Load()) EventBus.Instance?.EmitNotify("Loaded.");
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Cross into the first registered Dream pocket, or come back out. Unit 6's seam, made reachable.
    /// </summary>
    /// <remarks>
    /// <b>Bound to F10 rather than F4</b>, because the probe scene binds F4 to its own readout toggle — and
    /// the probe is the scene this key loads, so the two would fight the moment it worked. Found by reading
    /// the README's own list of probe keys rather than by pressing anything.
    /// <para>
    /// <b>The fourth debug key in this family, and it exists because the mechanism has no authored
    /// entrance.</b> A pocket is entered from a <c>CrossToDream</c> hotspot, and placing those is pass-2
    /// content on rooms Jeremy owns — so without a key the whole seam would be reachable only from
    /// <c>--verify-slice</c>, which is the state <c>EncounterManager.Rig</c> shipped in and the reason F8
    /// exists. Retires when the first real entrance is authored.
    /// <para>
    /// <b>It toggles enter/leave and deliberately cannot <c>Complete</c> a pocket.</b> Completing writes
    /// <c>pocket.&lt;id&gt;.cleared</c>, which breaks the entrance permanently — so a key that completed would
    /// be *destructive* scaffolding: one press and the pocket cannot be entered again for the rest of that
    /// save, which is the opposite of what a test key is for. <c>Leave</c> grants nothing and writes nothing,
    /// so this is repeatable as many times as you like. The completion path is driven by the gates, and by a
    /// real pocket scene calling <c>PocketManager.Complete()</c> when the player reaches its objective —
    /// which is where that call belongs anyway (*"going back will be written in the 3D world"*).
    /// </para>
    /// </para>
    /// </remarks>
    private static void TogglePocket()
    {
        var pockets = PocketManager.Instance;
        if (pockets == null) return;

        if (pockets.InPocket)
        {
            var left = pockets.CurrentPocketId;
            pockets.Leave();
            EventBus.Instance?.EmitNotify($"[debug] left {left} — nothing taken, the way in is still there");
            return;
        }

        // First registered rather than a hardcoded id, on `LearnEveryCraftFamily`'s reasoning: a name in
        // code goes quietly out of date the day the content is renamed, and the symptom is a key that does
        // nothing, which looks exactly like a broken seam.
        foreach (var id in pockets.Pockets.Keys)
        {
            var verdict = pockets.InspectEnter(id);
            GD.Print($"[debug] pocket {id}: {verdict}");

            if (verdict != PocketBlock.None) continue;
            if (pockets.Enter(id)) return;
        }

        EventBus.Instance?.EmitNotify(pockets.Pockets.Count == 0
            ? "[debug] no pockets are registered"
            : "[debug] nothing to cross into — every pocket is taken, or you are mid-fight");
    }

    /// <summary>
    /// Steps every entity in the current room one place along the state enum. This is the
    /// M2 gate: press it and watch both realms update without a room reload.
    /// </summary>
    private static void CycleEntityStates()
    {
        var room = NavigationManager.Instance?.CurrentRoom;
        var entities = EntityStateManager.Instance;
        if (room == null || entities == null) return;

        foreach (var placement in room.Entities)
        {
            if (placement?.Entity == null) continue;

            var next = entities.GetState(placement.InstanceId) switch
            {
                EntityState.Corrupted => EntityState.Severed,
                EntityState.Severed => EntityState.Purified,
                _ => EntityState.Corrupted,
            };
            entities.SetState(placement.InstanceId, next);
        }

        EventBus.Instance?.EmitNotify("[debug] cycled entity states");
    }

    /// <summary>
    /// Learns every blueprint any recipe asks for, so chapters 2 and 3 are walkable before the Dream
    /// knowledge pockets exist.
    /// </summary>
    /// <remarks>
    /// <b>This is scaffolding with a scheduled demolition</b>, and it was chosen over the alternative on
    /// purpose. `items.md` §8 pays for each craft family with a Dream knowledge pocket, and pockets are
    /// unit 6 — so the honest state after unit 4a is *the recipe exists, the flag works, and you cannot
    /// learn it yet*, which leaves the three tool doors shut and half the map unreachable for playtest.
    /// The obvious fix is three placeholder <c>Lore</c> pages dropped in early rooms; the trouble is that
    /// unit 6 would then have to *un-author* them, and content that has to be removed later is the kind
    /// that gets left in. A debug key deletes itself.
    /// <para>
    /// <b>Read off the registry rather than a list of names</b>, which is the difference between an aid
    /// that keeps working and one that silently stops. Unit 4b adds roughly fourteen recipes; a hardcoded
    /// roster would go quietly out of date the first time one of them named a page not on it, and the
    /// symptom — one recipe still unknown after pressing the key — is indistinguishable from the recipe
    /// being authored wrong. There is a gate over this for the same reason.
    /// </para>
    /// </remarks>
    private static void LearnEveryCraftFamily()
    {
        var inv = InventoryCraftingManager.Instance;
        if (inv == null) return;

        var learned = 0;
        foreach (var recipe in inv.Recipes.Values)
        {
            if (recipe == null || !recipe.NeedsBlueprint) continue;
            if (inv.Discover(recipe.RequiredBlueprintId)) learned++;
        }

        EventBus.Instance?.EmitNotify(learned == 0
            ? "[debug] every craft family was already known"
            : $"[debug] learned {learned} craft " + (learned == 1 ? "family" : "families"));
    }

    /// <summary>
    /// Grants every Dream verb that a pocket is allowed to teach, so <c>PocketGate</c>s can be walked through
    /// before any boss pocket exists.
    /// </summary>
    /// <remarks>
    /// <b>The fifth key in the F6–F10 family, and it exists for the reason the other four do: a mechanism with
    /// no authored source.</b> <c>DreamVerbs.BeginHold</c> refuses a verb you have not been taught, and the only
    /// thing that teaches one is a boss pocket's <c>Complete()</c> — of which there are zero. So without this,
    /// a <c>PocketGate</c> with <c>Requires = Float</c> reads as permanently shut, which is indistinguishable
    /// from a broken gate. Retires with the second boss pocket.
    /// <para>
    /// <b>It reads the enum rather than naming Float and Flare</b>, on <see cref="LearnEveryCraftFamily"/>'s
    /// reasoning: a hardcoded pair goes quietly out of date the day a fourth verb is added, and the symptom is
    /// a key that half works. <b>Suppress is deliberately included and deliberately harmless</b> — it carries no
    /// flag at all, so setting one changes nothing, and skipping it here would encode the exception in a second
    /// place that has to agree with <c>PocketManager.Holds</c>.
    /// </para>
    /// </remarks>
    private static void LearnGrantableDreamVerbs()
    {
        var world = WorldStateManager.Instance;
        if (world == null) return;

        var learned = 0;
        foreach (DreamVerb verb in System.Enum.GetValues<DreamVerb>())
        {
            if (verb == DreamVerb.None) continue;
            if (PocketManager.Holds(verb)) continue;

            world.SetFlag(Flags.VerbLearned(verb), true);
            learned++;
        }

        EventBus.Instance?.EmitNotify(learned == 0
            ? "[debug] every Dream verb was already held"
            : $"[debug] learned {learned} Dream " + (learned == 1 ? "verb" : "verbs"));
    }

    /// <summary>
    /// Crafts everything that can be crafted where you are standing, and says why for everything that
    /// cannot. The second half is the point.
    /// </summary>
    /// <remarks>
    /// <b>Added 2026-08-03, for the same reason as the key above and one more.</b> Unit 4a built the whole
    /// crafting mechanism and no way to reach it: <c>EventBus.CraftingRequested</c> fires and nothing
    /// subscribes, and the panel is deliberately not in this stream — <c>build-plan.md</c> §4 keeps the
    /// crafting, combat and journal panels in one editor pass, and decision 9 (2026-08-02) makes the
    /// crafting screen *be* the journal. So a standalone panel now would be a screen that a ratified
    /// decision has already deleted. A debug key is the only unblock that does not pre-empt it.
    /// <para>
    /// <b>It reports the block reasons rather than just succeeding or failing</b>, because
    /// <c>Inspect</c>'s ordering means a single "cannot craft" is four different situations wearing one
    /// face — and one of them (<c>NotAtStation</c>) is about where you are rather than what you own, which
    /// is exactly the distinction a player standing at a bench with a full satchel cannot see. Printing
    /// the per-recipe verdict is also the cheapest possible check on the camp-satisfies-every-station
    /// ruling: stand at the cottage bench and nothing should ever read <c>NotAtStation</c>.
    /// </para>
    /// Delete with the key binding when the journal lands.
    /// </remarks>
    private static void CraftEverythingPossibleHere()
    {
        var inv = InventoryCraftingManager.Instance;
        if (inv == null) return;

        var made = new List<string>();
        var blocked = new Dictionary<CraftBlock, int>();

        // Snapshot the ids first: crafting sets flags and consumes materials, so the verdict on a later
        // recipe legitimately changes mid-sweep, and iterating the live dictionary while that happens is
        // the kind of bug that only shows up once there are twenty recipes.
        foreach (var recipeId in inv.Recipes.Keys.ToList())
        {
            var verdict = inv.Inspect(recipeId);
            GD.Print($"[debug] {recipeId}: {verdict}");

            if (verdict != CraftBlock.None)
            {
                blocked[verdict] = blocked.GetValueOrDefault(verdict) + 1;
                continue;
            }

            var recipe = inv.Recipes[recipeId];
            if (inv.Craft(recipeId))
                made.Add(recipe?.RepairsWorld == true
                    ? recipe.SetsFlag
                    : inv.DisplayNameOf(recipe?.OutputItemId));
        }

        if (made.Count > 0)
        {
            EventBus.Instance?.EmitNotify($"[debug] made {string.Join(", ", made)}");
            return;
        }

        // Nothing was made, which is the case worth explaining. `AlreadyMade` is deliberately named
        // rather than folded into "nothing to do": for a tool or a trap it is the *success* state, and
        // reading it as a failure would send you looking for materials you do not need.
        var why = blocked.Count == 0
            ? "no recipes are registered"
            : string.Join(", ", blocked.OrderBy(pair => pair.Key).Select(pair => $"{pair.Value} {pair.Key}"));

        EventBus.Instance?.EmitNotify($"[debug] nothing craftable here — {why}");
    }

    /// <summary>
    /// Sets every trap in the satchel against the creature in this room, and says why for each one it
    /// cannot. The second half is the point, exactly as with <see cref="CraftEverythingPossibleHere"/>.
    /// </summary>
    /// <remarks>
    /// <b>Added 2026-08-03, and it is the third key in a row that exists because a built mechanism had no
    /// reachable verb.</b> <c>EncounterManager.Rig</c> was called from nowhere but <c>SliceVerifier</c>:
    /// the fight screen <i>displays</i> what is set and never sets anything, and it could not — rigging is
    /// free before the fight and costs a turn inside it (§18), so the verb belongs on the room, not on the
    /// fight. Ratified 2026-08-03: <b>a key now, a satchel button later</b>, on the standing pattern of
    /// building the debug key, testing the mechanism through it, and deleting it when the UI lands.
    /// <para>
    /// <b>Delete this with its key binding when the satchel grows a Set button</b> — the shape agreed for
    /// that is a third sibling beside <c>%UseButton</c> and <c>%EquipButton</c>, targeting the room's only
    /// creature. There is no target picker in either version and there does not need to be: every one of
    /// the thirty rooms that holds a creature holds exactly one, so <c>Rig</c> has never had an ambiguous
    /// target and the verb can be a button rather than a mode.
    /// </para>
    /// <para>
    /// It will usually report nothing to set, and that is not a fault in the key: all six trap recipes
    /// need a blueprint and nothing in the data grants one yet, so <c>debug_learn_all</c> and then
    /// <c>debug_craft_here</c> at a camp is the sequence that gives this key something to work with.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b><c>internal</c> rather than <c>private</c> so the suite can press it.</b> Its siblings are private
    /// and gated only by <c>InputMap.HasAction</c>, which is enough for them because a player can see
    /// whether they worked. This one cannot be seen to work: no trap is craftable until unit 6 grants a
    /// blueprint, so on a real playthrough it reports *"nothing to set"* whether it is correct or broken —
    /// the manufactured-probe case in <c>verify-slice-blind-spots</c>. The gate hands it a satchel full of
    /// traps and a room with a creature and checks the sites afterwards.
    /// </remarks>
    internal static void RigEverythingPossibleHere()
    {
        var inv = InventoryCraftingManager.Instance;
        var encounters = EncounterManager.Instance;
        var room = NavigationManager.Instance?.CurrentRoom;
        if (inv == null || encounters == null || room == null) return;

        // The room's creature, whatever state it is in. Purified bodies are included on purpose: `Rig`
        // refuses nothing on account of state, and finding that out by pressing the key is better than
        // finding it out by reading this method.
        var target = room.Entities?.FirstOrDefault(p => p?.Entity != null);
        if (target == null)
        {
            EventBus.Instance?.EmitNotify("[debug] nothing here to rig against");
            return;
        }

        var set = new List<string>();
        var blocked = new Dictionary<RigBlock, int>();

        // Traps only, so the per-item verdict is worth reading. Sweeping the whole satchel would report
        // `NotATrap` for every material and herb in it and bury the two verdicts that mean something.
        foreach (var itemId in inv.Stacks.Keys.ToList())
        {
            if (inv.Stacks[itemId] <= 0) continue;
            if (inv.GetItem(itemId)?.Category != ItemCategory.Trap) continue;

            var verdict = encounters.InspectRig(target.InstanceId, itemId);
            GD.Print($"[debug] {itemId} vs {target.InstanceId}: {verdict}");

            if (verdict != RigBlock.None)
            {
                blocked[verdict] = blocked.GetValueOrDefault(verdict) + 1;
                continue;
            }

            if (encounters.Rig(target.InstanceId, itemId)) set.Add(inv.DisplayNameOf(itemId));
        }

        if (set.Count > 0)
        {
            EventBus.Instance?.EmitNotify(
                $"[debug] set {string.Join(", ", set)} against {target.InstanceId}");
            return;
        }

        // `SlotsFull` and `AlreadyRigged` are both successes wearing a refusal's face — the sites are
        // loaded, which is what you pressed the key to achieve — so they are named rather than folded
        // into one "cannot". Same argument as `AlreadyMade` above.
        var why = blocked.Count == 0
            ? "no traps in the satchel — all six recipes need a blueprint"
            : string.Join(", ", blocked.OrderBy(pair => pair.Key).Select(pair => $"{pair.Value} {pair.Key}"));

        EventBus.Instance?.EmitNotify($"[debug] nothing newly set — {why}");
    }
}
