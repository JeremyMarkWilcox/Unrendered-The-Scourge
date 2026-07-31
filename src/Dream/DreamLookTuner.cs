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
/// step goes wrong.</b> The handoff calls the ramp "a screenshot decision, not an argument", and
/// separately notes that a creeping level count is nearly always a symptom of light placement rather
/// than of the ramp. Neither observation is actionable unless the parameters move while you are
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

    private float FlickerScale => _flickers.Count == 0 ? 0.0f : _flickers[0].Scale;

    private float FlickerSpeed => _flickers.Count == 0 ? 0.0f : _flickers[0].SpeedScale;

    private int Levels => _look.GetShaderParameter(ParamLevels).As<int>();

    private float Clamp => _look.GetShaderParameter(ParamClamp).As<float>();

    private float Dither => _look.GetShaderParameter(ParamDither).As<float>();

    private float LightRange => _playerLight.OmniRange;

    private float Thickness => Creature(ParamThickness);

    private float Density => Creature(ParamDensity);

    private float Crawl => Creature(ParamCrawl);

    private float Compensation => Creature(ParamCompensation);

    private float Break => Creature(ParamBreak);

    private bool ReachWhite =>
        _creatures.Count > 0 && _creatures[0].GetShaderParameter(ParamReachWhite).As<bool>();

    public override void _Ready()
    {
        _look = GetNode<SubViewportContainer>("%DreamContainer").Material as ShaderMaterial;
        _playerLight = GetNode<OmniLight3D>("%PlayerLight");
        _walker = GetNode<DreamProbeWalker>("%Player");
        _readout = GetNode<Label>("%Readout");

        foreach (var name in new[] { "%CreatureMindless", "%CreatureBoss", "%CreatureBillboard" })
        {
            if (GetNode<MeshInstance3D>(name).GetActiveMaterial(0) is ShaderMaterial creature)
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
                _walker.Look(motion.Relative);
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
                _readout.Visible = !_readout.Visible;
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

    private void NudgeLight(float step) =>
        _playerLight.OmniRange = Mathf.Clamp(LightRange + step, 0.5f, 30.0f);

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
    private void Refresh()
    {
        _readout.Text =
            $"greys      {Levels,5}      [ ]\n"
            + $"clamp      {Clamp,5:0.00}      ; '\n"
            + $"dither     {Dither,5:0.00}      - =\n"
            + $"light      {LightRange,5:0.00}      , .\n"
            + "\n"
            + $"thickness  {Thickness,5:0.0}      5 6\n"
            + $"density    {Density,5:0.00}      7 8\n"
            + $"crawl Hz   {Crawl,5:0.0}      9 0\n"
            + $"white      {(ReachWhite ? "  yes" : "   no"),5}      K\n"
            + $"dist comp  {Compensation,5:0.0}      J L\n"
            + "\n"
            + $"flicker    {FlickerScale,5:0.0}      O P\n"
            + $"fl speed   {FlickerSpeed,5:0.0}      N M"
            + "\n\nWASD walk · click to look · Esc release · F4 hide · F12 shot";

        GD.Print(
            $"[probe] levels={Levels} clamp={Clamp:0.00} dither={Dither:0.00} light={LightRange:0.00}"
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

        var showing = _readout.Visible;
        _readout.Visible = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var image = GetViewport().GetTexture().GetImage();
        _readout.Visible = showing;

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
