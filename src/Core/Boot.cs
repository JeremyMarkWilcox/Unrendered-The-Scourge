using System.Linq;
using Godot;
using Unrendered.Tools;

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

        // godot --headless --path . -- --seed-content
        if (args.Contains("--seed-content"))
        {
            ContentSeeder.SeedAll();
            GetTree().Quit();
            return;
        }

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
