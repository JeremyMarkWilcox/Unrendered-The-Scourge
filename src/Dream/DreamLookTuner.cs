using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Unrendered.Dream;

/// <summary>
/// The probe's harness: live keys for the look parameters, the player light's radius, the readout
/// that makes a screenshot self-describing, and a capture key.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because the numbers are not decidable yet, and pretending otherwise is how this
/// step goes wrong.</b> The ramp is a screenshot decision rather than an argument, and a creeping
/// level count is nearly always a symptom of light placement rather than of the ramp — both are
/// written up in <c>docs/dream-probe.md</c>, under the grey keys and under the questions it asks for
/// answers to. Neither observation is actionable unless the parameters move while you are
/// standing in the room looking at them. So the deliverable of this unit is <i>knobs, not
/// values</i> — whatever the keys settle on gets written back into <c>DreamLook.tres</c>.
/// </para>
/// <para>
/// The player light's radius is on a key beside the shader parameters despite not being a shader
/// parameter, because it is the fourth thing in the same conversation: falloff owns distance, so the
/// light's range is what actually separates near from far. It is also the Resolve degrade in its
/// entirety — the drain is one number on this light, and the dark closing in is the whole effect.
/// Worth feeling by hand before anything drives it.
/// </para>
/// <para>
/// It owns all mouse input for the probe because it sits on the scene root, in the main tree, where
/// input arrives unconditionally — see the remarks on <see cref="DreamProbeWalker"/>.
/// </para>
/// </remarks>
public partial class DreamLookTuner : Control
{
    /// <summary>Shared with <c>ScreenshotTool</c> so probe frames land beside slice frames.</summary>
    public const string OutputDir = "res://.shots";

    private const string ParamLevels = "levels";
    private const string ParamClamp = "white_clamp";
    private const string ParamDither = "dither_strength";

    private const string ParamThickness = "line_thickness";
    private const string ParamDensity = "line_density";
    private const string ParamCrawl = "crawl_hz";
    private const string ParamReachWhite = "reach_white";
    private const string ParamCompensation = "distance_compensation";
    private const string ParamBreak = "line_break";

    private ShaderMaterial _look;
    private OmniLight3D _playerLight;
    private DreamProbeWalker _walker;
    private Label _readout;

    /// <summary>
    /// Both creatures, so the shared knobs move together. Their one differing parameter —
    /// <c>screen_space_lines</c> — is authored per material and deliberately not on a key: the point
    /// of having two is to see body-locked and screen-space side by side in one frame.
    /// </summary>
    private readonly System.Collections.Generic.List<ShaderMaterial> _creatures = [];

    /// <summary>
    /// Every flicker in the scene, collected by walking the tree rather than by unique name, so a
    /// light Jeremy drops in later is picked up without touching this script.
    /// </summary>
    private readonly System.Collections.Generic.List<DreamLightFlicker> _flickers = [];

    /// <summary>
    /// Every <see cref="PocketBounds"/> in the scene, so the border's brightness is a key like everything
    /// else here rather than a number to guess in the inspector and re-run for.
    /// </summary>
    /// <remarks>
    /// <b>It earned a key by being un-guessable.</b> The first value shipped at 0.55 on the reasoning that the
    /// posterise threshold sits near 0.5 — and on screen it saturated to a flat slab, because what the shader
    /// reads is the rendered luminance of an unshaded surface and not the albedo that was typed. The
    /// interesting band is *below* the top, where the ordered dither only flips some pixels and the wall reads
    /// as crosshatch instead of as a panel. That band cannot be worked out from the number; it has to be
    /// scrubbed.
    /// </remarks>
    private readonly System.Collections.Generic.List<PocketBounds> _bounds = [];

    private float BorderValue => _bounds.Count == 0 ? 0.0f : _bounds[0].BorderValue;

    private float FlickerScale => _flickers.Count == 0 ? 0.0f : _flickers[0].Scale;

    private float FlickerSpeed => _flickers.Count == 0 ? 0.0f : _flickers[0].SpeedScale;

    private int Levels => _look.GetShaderParameter(ParamLevels).As<int>();

    private float Clamp => _look.GetShaderParameter(ParamClamp).As<float>();

    private float Dither => _look.GetShaderParameter(ParamDither).As<float>();

    private float LightRange => _playerLight?.OmniRange ?? 0.0f;

    private float Thickness => Creature(ParamThickness);

    private float Density => Creature(ParamDensity);

    private float Crawl => Creature(ParamCrawl);

    private float Compensation => Creature(ParamCompensation);

    private float Break => Creature(ParamBreak);

    private bool ReachWhite =>
        _creatures.Count > 0 && _creatures[0].GetShaderParameter(ParamReachWhite).As<bool>();

    public override void _Ready()
    {
        // **Everything but the container is optional, and that is what makes this droppable into a pocket.**
        // It shipped asking `GetNode` for three creature meshes by unique name, which threw in any scene that
        // did not happen to be the probe — so the one harness worth having in every pocket was the one thing
        // that could only live in one. The probe still has all of them; a pocket with no creatures in it now
        // gets the world keys and quietly no creature keys, rather than a crash on load.
        _look = GetNodeOrNull<SubViewportContainer>("%DreamContainer")?.Material as ShaderMaterial;
        _playerLight = GetNodeOrNull<OmniLight3D>("%PlayerLight");
        _walker = GetNodeOrNull<DreamProbeWalker>("%Player");
        _readout = GetNodeOrNull<Label>("%Readout");

        foreach (var name in new[] { "%CreatureMindless", "%CreatureBoss", "%CreatureBillboard" })
        {
            if (GetNodeOrNull<MeshInstance3D>(name)?.GetActiveMaterial(0) is ShaderMaterial creature)
            {
                _creatures.Add(creature);
            }
        }

        Collect(this);

        if (_look == null)
        {
            GD.PushError("[probe] the container has no ShaderMaterial — the look pass is missing.");
            return;
        }

        Refresh();

        // godot --path . -- --probe-shot   (needs a real window; headless renders nothing)
        if (OS.GetCmdlineUserArgs().Contains("--probe-shot"))
        {
            _ = CaptureAndQuit();
        }

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } click
            && click.ButtonIndex == MouseButton.Left)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        if (@event is InputEventMouseMotion motion)
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                _walker?.Look(motion.Relative);
            }

            return;
        }

        if (@event is not InputEventKey { Pressed: true } key || _look == null)
        {
            return;
        }

        // Echoes are accepted, so holding a key scrubs. That is the point of the whole class.
        switch (key.PhysicalKeycode)
        {
            case Key.Escape:
                Input.MouseMode = Input.MouseModeEnum.Visible;
                return;
            case Key.F4:
                if (_readout != null) _readout.Visible = !_readout.Visible;
                return;
            case Key.F12:
                _ = Capture();
                return;

            case Key.Bracketleft: NudgeLevels(-1); break;
            case Key.Bracketright: NudgeLevels(+1); break;

            case Key.Minus: Nudge(ParamDither, -0.05f, 0.0f, 1.0f); break;
            case Key.Equal: Nudge(ParamDither, +0.05f, 0.0f, 1.0f); break;

            case Key.Semicolon: Nudge(ParamClamp, -0.02f, 0.4f, 1.0f); break;
            case Key.Apostrophe: Nudge(ParamClamp, +0.02f, 0.4f, 1.0f); break;

            case Key.Comma: NudgeLight(-0.25f); break;
            case Key.Period: NudgeLight(+0.25f); break;

            case Key.Key5: NudgeCreature(ParamThickness, -0.5f, 1.0f, 16.0f); break;
            case Key.Key6: NudgeCreature(ParamThickness, +0.5f, 1.0f, 16.0f); break;

            case Key.Key7: NudgeCreature(ParamDensity, -0.05f, 0.0f, 1.0f); break;
            case Key.Key8: NudgeCreature(ParamDensity, +0.05f, 0.0f, 1.0f); break;

            case Key.Key9: NudgeCreature(ParamCrawl, -1.0f, 0.0f, 30.0f); break;
            case Key.Key0: NudgeCreature(ParamCrawl, +1.0f, 0.0f, 30.0f); break;

            case Key.K: ToggleReachWhite(); break;

            case Key.U: NudgeCreature(ParamBreak, -0.1f, 0.0f, 1.0f); break;
            case Key.I: NudgeCreature(ParamBreak, +0.1f, 0.0f, 1.0f); break;

            case Key.J: NudgeCreature(ParamCompensation, -0.1f, 0.0f, 1.0f); break;
            case Key.L: NudgeCreature(ParamCompensation, +0.1f, 0.0f, 1.0f); break;

            case Key.G: NudgeBorder(-0.02f); break;
            case Key.H: NudgeBorder(+0.02f); break;

            case Key.O: NudgeFlicker(-0.1f, 0.0f); break;
            case Key.P: NudgeFlicker(+0.1f, 0.0f); break;

            case Key.N: NudgeFlicker(0.0f, -0.1f); break;
            case Key.M: NudgeFlicker(0.0f, +0.1f); break;

            default: return;
        }

        Refresh();
    }

    // ---- Parameters ------------------------------------------------------

    private void NudgeLevels(int step) =>
        _look.SetShaderParameter(ParamLevels, Mathf.Clamp(Levels + step, 2, 16));

    private void Nudge(string parameter, float step, float min, float max) =>
        _look.SetShaderParameter(
            parameter,
            Mathf.Clamp(_look.GetShaderParameter(parameter).As<float>() + step, min, max));

    private void NudgeLight(float step)
    {
        if (_playerLight == null) return;

        _playerLight.OmniRange = Mathf.Clamp(LightRange + step, 0.5f, 30.0f);
    }

    /// <summary>
    /// Move every border shell's brightness together, and rebuild so the change is on screen.
    /// </summary>
    /// <remarks>
    /// A rebuild rather than a reach into the material, because <c>PocketBounds</c> owns how the shell is
    /// made and duplicating that here is the second copy that drifts. It throws away five boxes and builds
    /// five boxes; at this size that is free, and it means the key exercises the same path the scene does.
    /// </remarks>
    private void NudgeBorder(float step)
    {
        foreach (var bounds in _bounds)
        {
            bounds.BorderValue = Mathf.Clamp(bounds.BorderValue + step, 0.0f, 1.0f);
            bounds.Rebuild();
        }
    }

    private float Creature(string parameter) =>
        _creatures.Count == 0 ? 0.0f : _creatures[0].GetShaderParameter(parameter).As<float>();

    private void NudgeCreature(string parameter, float step, float min, float max)
    {
        var value = Mathf.Clamp(Creature(parameter) + step, min, max);
        foreach (var creature in _creatures)
        {
            creature.SetShaderParameter(parameter, value);
        }
    }

    private void Collect(Node node)
    {
        if (node is DreamLightFlicker flicker)
        {
            _flickers.Add(flicker);
        }

        if (node is PocketBounds bounds)
        {
            _bounds.Add(bounds);
        }

        foreach (var child in node.GetChildren())
        {
            Collect(child);
        }
    }

    /// <summary>
    /// Scales every light's authored flicker at once. It scales rather than sets, so the per-light
    /// character — a subtle lamp against deeper guttering sconces — survives being dialled.
    /// </summary>
    private void NudgeFlicker(float amountStep, float speedStep)
    {
        var amount = Mathf.Clamp(FlickerScale + amountStep, 0.0f, 2.0f);
        var speed = Mathf.Clamp(FlickerSpeed + speedStep, 0.1f, 4.0f);
        foreach (var flicker in _flickers)
        {
            flicker.Scale = amount;
            flicker.SpeedScale = speed;
        }
    }

    private void ToggleReachWhite()
    {
        var value = !ReachWhite;
        foreach (var creature in _creatures)
        {
            creature.SetShaderParameter(ParamReachWhite, value);
        }
    }

    /// <summary>
    /// Draws the current values, and prints them, so a value settled by eye can be read back off
    /// either a screenshot or the console rather than remembered.
    /// </summary>
    /// <remarks>
    /// <b>Every debug key in a pocket belongs on this readout, including the ones this class does not own.</b>
    /// Standing rule, asked for on 2026-08-07 after <c>B</c> was rebound twice and went unfound: *"we should
    /// always add the debug buttons labels to the temp hud"*. So the block below lists the flood light
    /// (<c>DreamAuthorLight</c>) and the three verbs (<c>DreamVerbHold</c>) alongside the tuner's own keys —
    /// they are not scrubbed here and they are still the answer to *what can I press*.
    /// <para>
    /// The duplication is the point and it is worth naming, because the instinct is to keep each key's label
    /// next to its owner. A key the player cannot discover is a key that does not exist, and there is exactly
    /// one surface in a pocket where discovery happens.
    /// </para>
    /// </remarks>
    private void Refresh()
    {
        if (_readout == null) return;

        _readout.Text =
            $"greys      {Levels,5}      [ ]\n"
            + $"clamp      {Clamp,5:0.00}      ; '\n"
            + $"dither     {Dither,5:0.00}      - =\n"
            + $"light      {LightRange,5:0.00}      , .\n"
            + $"border     {BorderValue,5:0.00}      G H\n"
            + "\n"
            + $"thickness  {Thickness,5:0.0}      5 6\n"
            + $"density    {Density,5:0.00}      7 8\n"
            + $"crawl Hz   {Crawl,5:0.0}      9 0\n"
            + $"white      {(ReachWhite ? "  yes" : "   no"),5}      K\n"
            + $"dist comp  {Compensation,5:0.0}      J L\n"
            + "\n"
            + $"flicker    {FlickerScale,5:0.0}      O P\n"
            + $"fl speed   {FlickerSpeed,5:0.0}      N M"
            + "\n\nWASD walk · click to look · Esc release"
            + "\nB flood light · Q hide · Space float · E flare"
            + "\nF4 hide readout · F12 shot";

        GD.Print(
            $"[probe] levels={Levels} clamp={Clamp:0.00} dither={Dither:0.00} light={LightRange:0.00}"
            + $" border={BorderValue:0.00}"
            + $" | creature thickness={Thickness:0.0} density={Density:0.00} crawl={Crawl:0.0}Hz"
            + $" white={ReachWhite} comp={Compensation:0.0} break={Break:0.0} | flicker={FlickerScale:0.0}x speed={FlickerSpeed:0.0}x");
    }

    // ---- Capture ---------------------------------------------------------

    /// <summary>
    /// Saves the frame, named after the values that produced it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It must capture the window, not the <c>SubViewport</c>.</b> The look pass lives on the
    /// container, so it runs when the viewport's texture is drawn to the screen — a
    /// <c>SubViewport</c> capture would silently save the raw 3D image with no posterise, dither or
    /// clamp in it, which is the one thing a screenshot here exists to show. The 4x nearest upscale
    /// comes along with it, and that is correct: it is how the game presents the grid, and the
    /// pixels are still legible as 4x4 blocks.
    /// </para>
    /// <para>
    /// The filename carries the parameters because a scrubbing session otherwise produces a folder
    /// of images nobody can attribute — the same reason the readout prints as well as draws. The
    /// readout itself is hidden for the frame, so a shot is always sendable.
    /// </para>
    /// </remarks>
    private async Task Capture()
    {
        DirAccess.MakeDirRecursiveAbsolute(OutputDir);

        var name = ($"probe_g{Levels}_c{Clamp:0.00}_d{Dither:0.00}_l{LightRange:0.00}"
            + $"_t{Thickness:0.0}_n{Density:0.00}_hz{Crawl:0.0}{(ReachWhite ? "_w" : "")}")
            .Replace('.', '-');

        var showing = _readout?.Visible ?? false;
        if (_readout != null) _readout.Visible = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var image = GetViewport().GetTexture().GetImage();
        if (_readout != null) _readout.Visible = showing;

        var path = $"{OutputDir}/{name}.png";
        var error = image.SavePng(path);
        GD.Print(error == Error.Ok ? $"[probe] {path}" : $"[probe] FAILED {path}: {error}");
    }

    private async Task CaptureAndQuit()
    {
        // Let CSG build its meshes and the lights settle before the shutter opens.
        for (var i = 0; i < 30; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        await Capture();
        GetTree().Quit();
    }
}
