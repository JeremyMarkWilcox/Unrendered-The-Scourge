using System.Linq;
using Godot;

namespace Unrendered.Core;

/// <summary>
/// First scene loaded. Exists so there is somewhere for the main menu, save-slot picker
/// and studio logo to live later without Main having to know about them. For now it steps
/// straight into the game.
/// </summary>
public partial class Boot : Node
{
    private const string MainScene = "res://scenes/Main.tscn";

    /// <summary>Set from the command line; Main runs the automated slice pass and exits.</summary>
    public static bool VerifySlice { get; private set; }

    /// <summary>Set from the command line; Main walks the slice capturing frames, then exits.</summary>
    public static bool CaptureShots { get; private set; }

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();

        // `--seed-content` and ContentSeeder were deleted 2026-08-01, at the start of the content
        // pass. It wrote the whole of data/ unconditionally, which was a bootstrap while there were
        // five rooms and a loaded gun beside ~58 (docs/build-plan.md §6). Its payload was superseded
        // content anyway — the giant rat, the 2D dream maze, the heist. The one part worth keeping is
        // the writer idiom (ResourceSaver.Save then TakeOverPath, so a room links its entity instead
        // of embedding a copy); that returns in the ledger generator, write-if-absent. The original
        // is in git at bb665ad:src/Tools/ContentSeeder.cs.

        // godot --headless --path . -- --verify-slice
        VerifySlice = args.Contains("--verify-slice");

        // godot --path . -- --shots   (needs a real window; headless renders nothing)
        CaptureShots = args.Contains("--shots");

        CallDeferred(nameof(EnterMain));
    }

    private void EnterMain()
    {
        var error = GetTree().ChangeSceneToFile(MainScene);
        if (error != Error.Ok)
            GD.PushError($"[Boot] Could not load {MainScene}: {error}");
    }
}
