using System.Linq;
using System.Threading.Tasks;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;
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

    private const string Rat = "waystation.rat_01";

    public static async Task CaptureSlice(Node host)
    {
        DirAccess.MakeDirRecursiveAbsolute(OutputDir);

        await Settle(host, 20);
        await Shot(host, "01_waking_corrupted");

        RealmTransitionManager.Instance.SetRealm(GameRealm.Dream);
        await Settle(host, 45);
        await Shot(host, "02_spectral_vision");

        RealmTransitionManager.Instance.SetRealm(GameRealm.Waking);
        await Settle(host, 45);

        // Find the relic, then cross.
        Activate("yard_cairn");
        await Settle(host, 10);
        Activate("yard_relic");
        await Settle(host, 40);
        await Shot(host, "03_heist_threshold");

        // Park the hunter a room away so the dread cue is in shot.
        DreamRunManager.Instance.DebugSetPursuerRoom("dream_way_cistern");
        Walk("dream_way_gallery");
        await Settle(host, 30);
        await Shot(host, "04_heist_gallery");

        DreamRunManager.Instance.DebugSetPursuerRoom("dream_way_antechamber");
        Walk("dream_way_stair");
        Walk("dream_way_solar");
        await Settle(host, 30);
        await Shot(host, "05_heist_solar");

        Activate("dream_way_anchor");
        await Settle(host, 45);
        await Shot(host, "06_ejected_severed");

        // Finish the anchorless body; the zone breaks with it.
        InteractionService.ActivateEntity(Rat);
        await Settle(host, 60);
        await Shot(host, "07_purified");

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
