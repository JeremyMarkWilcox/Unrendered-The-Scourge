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
        // The journal (unit 7a). Reached here rather than through `Hud` because closing a panel is not a
        // policy decision — the two above are here for the same reason, and only *opening* has to go
        // through the HUD's which-panels-may-coexist rule.
        else if (@event.IsActionPressed("cancel") && _hud?.Crafting?.IsOpen == true)
        {
            _hud.Crafting.Close();
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
        // `debug_craft_here` (F7) stood here and went with unit 7a — the branch and
        // `CraftEverythingPossibleHere` both, on the demolition its own doc-comment scheduled: *"delete with
        // the key binding when the journal lands."* Every one of its jobs is now a thing you can see. It
        // crafted what was possible; a row you can press does that, one at a time and on purpose. It printed
        // the per-recipe `CraftBlock`; the row carries it in words. And it existed because
        // `EventBus.CraftingRequested` fired with nothing subscribed, which stopped being true the moment
        // `Hud.OnCraftingRequested` landed.
        //
        // The *action* stays in `project.godot` and F6 stays bound: nothing in the game grants a blueprint
        // yet, so `debug_learn_all` is still the only way to see his side of the book with anything on it.
        // The hint line said both keys would come off together and that was wrong about which one the panel
        // replaces — a journal shows you what you know, it does not teach you.
        // `debug_rig_here` (F8) stood here and went on 2026-08-05, on the demolition its own doc-comment
        // scheduled: *"delete this with its key binding when the satchel grows a Set button."* `%SetButton`
        // is that button, a third sibling beside Use and Equip, and it is the shape the key's comment agreed
        // to two days earlier — no target picker, because `NavigationManager.SoleCreatureHere` is a room
        // query with one answer.
        //
        // **The key was the more capable of the two and the button is still the better verb.** It rigged
        // every trap it could in one press and printed a `RigBlock` per refusal, which the button cannot do
        // and should not: *set this one, against that body* is the decision §18 charges a turn for inside a
        // fight, and a key that made all of them at once was a convenience that skipped the choice. The
        // refusals it printed are now the greying — one trap, one button, one reason it is dead.
        //
        // The *action* stays in `project.godot` on F7's precedent: an unbound action costs nothing, and a
        // deleted one is a merge conflict for Jeremy the next time he opens the input map.
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
}
