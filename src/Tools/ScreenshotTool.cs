using System.Linq;
using System.Threading.Tasks;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;
using Unrendered.UI;
using Unrendered.World;

namespace Unrendered.Tools;

/// <summary>
/// Walks the slice and captures a frame at each beat, so the look of a change can be
/// checked without sitting through it. Needs a real window — the headless dummy renderer
/// has nothing to capture.
///
/// Run with: <c>godot --path . -- --shots</c>
/// </summary>
public static class ScreenshotTool
{
    public const string OutputDir = "res://.shots";

    /// <summary>
    /// The screen the first three beats are taken on, and the body standing in it.
    ///
    /// <para><b>Chosen for two authored numbers, not for the scenery.</b> The Hunter is
    /// <c>VulnerableTo = Edge</c> and <c>ResistantTo = Fire</c>, so bare hands — which are
    /// <see cref="DamageType.Blunt"/> at <see cref="GameConfig.BareHandedPotency"/> — land at full
    /// weight: 3 into 30 is ten blows, and ten of its 8 back is 80 of a 100 bar. That is the same
    /// arithmetic the retired rat gave this walk, which is why the beats either side of the fight
    /// survived the retirement unchanged. **The Logger would not do**, and neither of the two Logger
    /// screens in chapter 1 can be used here: it resists Blunt, so a bare-handed walk cannot finish
    /// it and beat 03 would shoot a corpse-less screen after 40 wasted blows.</para>
    ///
    /// <para><c>ch1_deadfall</c> also has <b>no blight on it</b>, which is load-bearing rather than
    /// incidental: a screen cleanses on two reasons (`combat.md` §22), so a blighted room would still
    /// be grey after the body was finished and beat 03 would be misnamed.</para>
    /// </summary>
    private const string FightRoom = "ch1_deadfall";
    private const string Quarry = "ch1.hunter_01";

    public static async Task CaptureSlice(Node host, Hud hud)
    {
        DirAccess.MakeDirRecursiveAbsolute(OutputDir);

        // Beats 01-07 were the waystation prototype until unit 3b deleted it. Four of them were the
        // heist and had nowhere to go — unit 6 builds the first 3D pocket and can shoot its own — but
        // `01_waking_corrupted` and `07_purified` were always **one screen either side of a fight**, and
        // that comparison is better on an authored chapter-1 screen than it ever was on the prototype.
        // So they are re-pointed rather than deleted, and the numbering is **closed up rather than left
        // gapped**: the old argument for gaps was that an old .shots directory should line up with a new
        // one beat for beat, and six beats changing at once ends that whether the numbers move or not.
        ClearCaption();
        NavigationManager.Instance.GoToRoom(FightRoom);

        await Settle(host, 20);
        await Shot(host, "01_waking_corrupted");

        // -- The fight screen, which unit 3a shipped and this tool never looked at --
        // It costs nothing: it sits between two beats that already existed, and the click that opens it
        // is the click beat 03 needed anyway. Worth having because the screen is drawn in code rather
        // than authored, so it is the one panel with no `.tscn` for anyone to open and eyeball — and it
        // is where the first played fight showed the room straight through a themeless container.
        ClearCaption();
        InteractionService.ActivateEntity(Quarry);
        await Settle(host, 30);
        await Shot(host, "02_fight_screen");

        if (EncounterManager.Instance?.InFight != true)
            GD.PushWarning($"[ScreenshotTool] clicking {Quarry} opened no fight — beat 02 is the room");

        // Finish the body; the screen cleanses with it.
        //
        // This was one click until unit 3a. `ActivateEntity` on a Severed body purified it outright, and
        // §18 replaced that shortcut with the baseline attack — so the same call now *opens a fight*, and
        // the beat named `purified` spent one run showing a fight in progress on grey, uncleansed
        // ground. Striking until the body is finished is what "several clicks instead of one" means, and
        // it is the walk's job to say so out loud: **this beat depended on a behaviour instead of stating
        // an intent**, which is the third time an unwritten dependency has bitten this tool.
        //
        // The cap is a hang guard, not a rule; see `FightRoom` for the ten-blows arithmetic and why the
        // room is the one it is. If a retune ever pushes the exchange over, the collapse navigates to a
        // camp and this beat would quietly shoot the wrong room, so all three outcomes are checked and
        // printed. A tool with no suite over it has to carry its own assertions.
        var blows = 0;
        while (EncounterManager.Instance is { InFight: true } fight && blows++ < 40) fight.Strike();

        if (EncounterManager.Instance?.InFight == true)
            GD.PushWarning($"[ScreenshotTool] {Quarry} outlasted {blows} bare-handed blows — beat 03 is a "
                + "fight in progress, not a purified screen");
        else if (NavigationManager.Instance.CurrentRoomId != FightRoom)
            GD.PushWarning("[ScreenshotTool] finishing the body cost the fight — beat 03 is shooting "
                + $"'{NavigationManager.Instance.CurrentRoomId}', which is wherever the respawn landed");
        else if (EntityStateManager.Instance?.GetState(Quarry) != EntityState.Purified)
            GD.PushWarning($"[ScreenshotTool] the fight ended but {Quarry} is "
                + $"{EntityStateManager.Instance?.GetState(Quarry)} — beat 03 is misnamed");

        await Settle(host, 60);
        await Shot(host, "03_purified");

        // -- The satchel, promised in three documents as the cheapest available improvement --
        // Six rounds went into one label on this panel and **every one of them cost a full play session**,
        // because there was no beat here to look at. That is the whole reason it exists.
        //
        // The spread is manufactured with `AddItem` and that is deliberate: what the beat has to show is
        // the *grid* — six category tabs, a multi-digit count, and the two display names that were cut on
        // screen before the caption fix, "Rendered Tallow" and "Corrosive Draught". The honest walk owns
        // one axe and whatever it just picked up, which is a two-tile shot of a panel that is about
        // arrangement. Harvesting first anyway, so the beat is not *entirely* a fixture.
        ClearCaption();
        Activate("ch1_deadfall_pitch");

        var inv = InventoryCraftingManager.Instance;
        inv?.AddItem("mat_cairnstone", 12);
        inv?.AddItem("mat_tallow", 3);
        inv?.AddItem("herb_bitterroot", 7);
        inv?.AddItem("brew_corrosive_draught", 2);
        inv?.AddItem("salve_deepmend");
        inv?.AddItem("catalyst_ashincense");
        inv?.AddItem("tool_pry_bar");
        inv?.AddItem("trap_snare", 2);
        inv?.AddItem("armour_tanners_coat");

        if (hud?.Inventory == null) GD.PushWarning("[ScreenshotTool] no satchel to open — beat 04 is the room");
        else
        {
            hud.Inventory.Open();
            await Settle(host, 10);
            hud.Inventory.DebugSelect("mat_tallow");
        }

        await Settle(host, 30);
        await Shot(host, "04_satchel");

        hud?.Inventory?.Close();
        await Settle(host, 10);

        // -- The one thing no gate can check: the Mend cue against cleansed ground --
        // Added 2026-08-03 with unit 2. `#4ade80` is the hue `PlaceholderCues` warns about spending, and
        // what it has to stay legible against is the purified palette's own green: `PurifiedMid`
        // (`#6f8f5e`) in the treeline, `PurifiedGround` (`#4a5c3b`) under it. A green cue on green dirt.
        // Two frames of one screen, taken either side of the mend, is the whole comparison, and a
        // mended blight stays on screen as its maker's mark (§22) so the cue is in both of them.
        //
        // This was the first beat authored off the prototype — no waystation screen had a blight, and it
        // was worth more on the authored tutorial one than on a synthetic room nobody will ever walk
        // into. Every beat above it is authored ground now too, so the note is history rather than a
        // caveat; what it left behind is the rule that a look-beat belongs on the screen a player sees.
        ClearCaption();
        NavigationManager.Instance.GoToRoom("ch1_fern_gully");
        await Settle(host, 20);
        await Shot(host, "05_blight_on_corrupted_ground");

        ClearCaption();
        var blight = NavigationManager.Instance.CurrentRoom?.Hotspots
            .FirstOrDefault(h => h is { Kind: HotspotKind.Mend });

        if (blight == null) GD.PushWarning("[ScreenshotTool] no blight on ch1_fern_gully");
        else MendManager.Mend(blight);

        // Cleansing is per screen and this one also holds a creature, so mending alone leaves it grey.
        // Purified rather than severed: a severed body still holds the screen.
        var room = NavigationManager.Instance.CurrentRoom;
        foreach (var placement in room?.Entities ?? new Godot.Collections.Array<EntityPlacement>())
            if (!string.IsNullOrEmpty(placement?.InstanceId))
                EntityStateManager.Instance.Purify(placement.InstanceId);

        MendManager.Reevaluate(room);
        await Settle(host, 60);
        await Shot(host, "06_blight_on_cleansed_ground");

        // -- The same thing again for the Station cue, added 2026-08-03 with unit 4a --
        // Bronze `#c98a4b`, and writing this comment honestly cost it both of its original claims.
        //
        // The cue bronze would really be confused with is **`Workbench` gold `#ffd24a`** — same verb
        // ("Craft"), neighbouring hue — and **no screen in the project carries both.** `ch2_smithy` holds
        // the anvil alone; a camp has been a menu since 2026-08-01, so the bench and a field station may
        // never share a frame at all. That is the finding rather than a shortcoming of the beat: the
        // confusable pair cannot be judged side by side because the game never puts it side by side. If
        // that ever stops being true, the screen that breaks it is the one to shoot here.
        //
        // Nor is there a cleansed *sky* to check it against. `PlaceholderStyle = 1` is
        // `BackdropStyle.Interior`, and `DrawInterior` never calls `DrawSkyGradient` — "no sky indoors" —
        // so `GameConfig.PurifiedSky` (`#7fa8c9`) is *structurally absent* from any interior screen and
        // cannot be spent on legibility here. Bronze lands on the purified wall (`#6f8f5e`) and straddles
        // the floor line onto `#4a5c3b`. Beat 09 is the wilderness screen where the sky does get drawn,
        // so the two colour beats cover the purified palette between them rather than either alone.
        //
        // What the pair still earns: bronze read against corrupted fog and against cleansed green, which
        // is the axis every cue has to survive, and the one a gate cannot look at.
        ClearCaption();
        NavigationManager.Instance.GoToRoom("ch2_smithy");
        await Settle(host, 20);
        await Shot(host, "07_station_on_corrupted_ground");

        // Cleansed needs both reasons cleared, same as any screen: purify the bodies, mend the blights.
        ClearCaption();
        var smithy = NavigationManager.Instance.CurrentRoom;
        foreach (var placement in smithy?.Entities ?? new Godot.Collections.Array<EntityPlacement>())
            if (!string.IsNullOrEmpty(placement?.InstanceId))
                EntityStateManager.Instance.Purify(placement.InstanceId);

        foreach (var h in smithy?.Hotspots ?? new Godot.Collections.Array<HotspotData>())
            if (h is { Kind: HotspotKind.Mend })
                MendManager.Mend(h);

        MendManager.Reevaluate(smithy);
        await Settle(host, 60);
        await Shot(host, "08_station_on_cleansed_ground");

        GD.Print("[ScreenshotTool] done");
        host.GetTree().Quit();
    }

    // ---- Helpers ---------------------------------------------------------

    private static HotspotData Find(string hotspotId) =>
        NavigationManager.Instance.CurrentRoom?.Hotspots
            .FirstOrDefault(h => h != null && h.Id == hotspotId);

    private static void Activate(string hotspotId)
    {
        var data = Find(hotspotId);
        if (data == null) GD.PushWarning($"[ScreenshotTool] no hotspot '{hotspotId}'");
        else InteractionService.ActivateHotspot(data);
    }

    private static void Walk(string targetRoomId)
    {
        var door = NavigationManager.Instance.CurrentRoom?.Hotspots.FirstOrDefault(
            h => h != null && h.Kind == HotspotKind.Navigate && h.TargetRoomId == targetRoomId);

        if (door == null) GD.PushWarning($"[ScreenshotTool] no door to '{targetRoomId}'");
        else InteractionService.ActivateHotspot(door);
    }

    private static async Task Settle(Node host, int frames)
    {
        for (var i = 0; i < frames; i++)
            await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>
    /// Wipe the held caption so a beat can only show a line its own actions produced.
    /// </summary>
    /// <remarks>
    /// <b>Not a fix to a bug — a correction to this tool.</b> The caption is a notification with a
    /// five-second hold (<c>Hud.NotifyHoldSeconds</c>), which is right in play and wrong here: the whole
    /// walk takes about a second, so every beat inherited the previous beat's line. Beat 01 shot
    /// `ch1_deadfall` captioned *"A bench, and room to work. You will come back here."* — the cottage
    /// bench, from booting — and a reference frame that describes a different room is worse than one with
    /// no caption at all, because the wrong one gets believed.
    /// <para>
    /// Called at the top of each beat rather than just before each <c>Shot</c>, so a line the beat
    /// genuinely earns still lands. An empty notify rather than a new <c>Hud</c> method: the tool has no
    /// business growing the HUD's API to tidy its own frames.
    /// </para>
    /// </remarks>
    private static void ClearCaption() => EventBus.Instance?.EmitNotify("");

    private static async Task Shot(Node host, string name)
    {
        await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var image = host.GetViewport().GetTexture().GetImage();
        var path = $"{OutputDir}/{name}.png";

        var err = image.SavePng(path);
        GD.Print(err == Error.Ok
            ? $"[ScreenshotTool] {path}"
            : $"[ScreenshotTool] FAILED {path}: {err}");
    }
}
