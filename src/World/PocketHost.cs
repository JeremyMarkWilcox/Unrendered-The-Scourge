using Godot;
using Unrendered.Core;
using Unrendered.Dream;
using Unrendered.Managers;

namespace Unrendered.World;

/// <summary>
/// The seam between the 2D Waking world and a 3D Dream pocket. Unit 6.
///
/// <para><b>It is the whole of the 2D→3D→2D handoff, and it is this small because of how the probe was
/// built.</b> <c>scenes/dream/DreamProbe.tscn</c> is a <c>Control</c> whose child is a full-rect
/// <c>SubViewportContainer</c> with the monochrome shader on the <i>container</i> — so a pocket is already a
/// Control that can be added beside <see cref="RoomView"/>, the greyscale applies to the 3D output only, and
/// the HUD drawn above it stays in colour. That is <c>dream-realm-monochrome-aesthetic</c> satisfied by the
/// arrangement rather than by effort.</para>
///
/// <para><b>No scene change, deliberately.</b> <c>change_scene_to_file</c> would tear down <c>Main</c> and
/// take <see cref="RoomView"/>, the HUD and every panel's state with it, then rebuild them on the way back.
/// Adding and freeing a child keeps one scene tree for the whole game, and the autoloads never notice.</para>
///
/// <para><b>It decides nothing.</b> <see cref="PocketManager"/> owns whether you are in a pocket; this class
/// only reacts to being told. That split is invariant 10, and it is what lets the gates enter a pocket in a
/// headless run where no geometry exists at all — the five Waking-side guards that read
/// <c>PocketManager.InsideTheDream</c> stay falsifiable because this file is not on their path.</para>
/// </summary>
public partial class PocketHost : Control
{
    private Node _live;
    private string _liveScenePath = "";
    private DreamVerbHold _verbs;

    /// <summary>The room view to hide while a pocket is up. Set by <c>Main</c>.</summary>
    private RoomView _roomView;

    /// <summary>Whether a pocket scene is instantiated right now. For the gates.</summary>
    public bool SceneLoaded => _live != null;

    /// <summary>Which scene is up, or "". For the gates.</summary>
    public string LoadedScenePath => _liveScenePath;

    /// <summary>Whether the verb poller is attached right now. For the gates.</summary>
    public bool VerbInputLive => _verbs != null;

    public void Bind(RoomView roomView) => _roomView = roomView;

    public override void _Ready()
    {
        Name = "PocketHost";
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Visible = false;

        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.PocketEntered += OnPocketEntered;
        bus.PocketLeft += OnPocketLeft;
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;

        bus.PocketEntered -= OnPocketEntered;
        bus.PocketLeft -= OnPocketLeft;
    }

    // ---- Loading and freeing ---------------------------------------------

    private void OnPocketEntered(string pocketId)
    {
        // Invariant 13: trust the manager, not the payload. An earlier subscriber could have left the pocket
        // from inside this very emit, which is the ejection post-mortem's exact shape.
        var manager = PocketManager.Instance;
        if (manager == null || manager.CurrentPocketId != pocketId) return;

        var pocket = manager.GetPocket(pocketId);
        if (pocket == null) return;

        // Before the scene branches, so a pocket with no geometry still has verbs — and so that no authored
        // scene has to remember anything. See `AttachVerbInput`.
        AttachVerbInput();

        if (string.IsNullOrEmpty(pocket.ScenePath))
        {
            // A pocket with no scene is still a pocket as far as every rule is concerned — you are inside
            // it, the bench is shut, the camp verb refuses. This is the state a headless gate run lives in
            // permanently, and it is deliberately not an error: the fixture that keeps those guards honest
            // has no geometry by design.
            ShowPocket();
            return;
        }

        if (!ResourceLoader.Exists(pocket.ScenePath))
        {
            // Loud, because this one is a typo in authored data and the symptom is otherwise a black
            // screen you can still walk out of. The gates cross every ScenePath against this same call.
            GD.PushError($"[PocketHost] Pocket '{pocketId}' names a scene that is not there: {pocket.ScenePath}");
            ShowPocket();
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(pocket.ScenePath);
        if (scene == null)
        {
            GD.PushError($"[PocketHost] Pocket '{pocketId}' scene failed to load: {pocket.ScenePath}");
            ShowPocket();
            return;
        }

        _live = scene.Instantiate();
        _liveScenePath = pocket.ScenePath;
        AddChild(_live);

        SetViewportsRendering(true);
        ShowPocket();
    }

    private void OnPocketLeft(string pocketId, bool completed)
    {
        // Rendering stopped before the node goes, so the last frame of a freed viewport cannot be drawn.
        SetViewportsRendering(false);
        DetachVerbInput();

        if (_live != null)
        {
            // Removed then freed, not queued alone: `QueueFree` defers to the end of the frame, and a
            // pocket re-entered in the same frame — which a gate does — would find the old geometry still
            // parented here. `CampPanel.Rebuild` learned the same thing about rows.
            RemoveChild(_live);
            _live.QueueFree();
            _live = null;
        }

        _liveScenePath = "";

        Visible = false;
        if (_roomView != null) _roomView.Visible = true;
    }

    // ---- The verb poller -------------------------------------------------

    /// <summary>
    /// Give the pocket its <see cref="DreamVerbHold"/>, so the three verb keys work.
    /// </summary>
    /// <remarks>
    /// <b>Here rather than in the pocket scene, and that is a correction.</b> The node shipped as something to
    /// drop into each scene, and the first pocket played had no verbs at all because nobody had dropped one in
    /// — the symptom being that <c>F11</c> reported two verbs learned and no key did anything, since the grant
    /// lives in <c>Main</c> and the polling did not exist. **A thing every pocket must have, that takes no
    /// configuration and has no position, is not authoring — it is wiring, and wiring that can be forgotten
    /// fourteen times will be.** Same argument that made `PocketAnchor` and `PocketExit` nodes in the first
    /// place, followed one step further: those two need placing, this one does not.
    /// <para>
    /// A child of the host rather than of the loaded scene, so it exists on the **no-geometry** path too — the
    /// state every headless gate run lives in, and the one where a scene-side node could not exist at all.
    /// </para>
    /// </remarks>
    private void AttachVerbInput()
    {
        if (_verbs != null) return;

        _verbs = new DreamVerbHold();
        AddChild(_verbs);
    }

    /// <summary>Take it away again. Its <c>_ExitTree</c> drops any hold still open.</summary>
    private void DetachVerbInput()
    {
        if (_verbs == null) return;

        // Removed then freed rather than queued, for the reason the scene above is: a pocket re-entered in
        // the same frame would otherwise find the old poller still parented here and keep two.
        RemoveChild(_verbs);
        _verbs.QueueFree();
        _verbs = null;
    }

    private void ShowPocket()
    {
        Visible = true;

        // Hidden rather than left underneath. A `SubViewportContainer` is opaque where it draws, so this is
        // not about visibility — it is about a Waking screen not reacting to anything while you are
        // elsewhere. Hotspots are Controls: hiding the parent takes their input with them.
        if (_roomView != null) _roomView.Visible = false;
    }

    /// <summary>
    /// Turn the pocket's <c>SubViewport</c>s on and off.
    /// </summary>
    /// <remarks>
    /// <b>A hidden <c>SubViewport</c> keeps rendering.</b> Visibility governs whether its
    /// <i>container</i> draws, not whether the viewport redraws its target — so without this the game would
    /// pay for a 3D world, its lights and its post-process on every frame of every 2D screen, and the only
    /// symptom would be a framerate nobody could account for. Set to <c>WhenVisible</c> rather than
    /// <c>Always</c> on the way in, so the engine still gets to skip a frame it does not need.
    /// </remarks>
    private void SetViewportsRendering(bool on)
    {
        if (_live == null) return;

        foreach (var viewport in FindViewports(_live))
            viewport.RenderTargetUpdateMode = on
                ? SubViewport.UpdateMode.WhenVisible
                : SubViewport.UpdateMode.Disabled;
    }

    /// <summary>
    /// Every <c>SubViewport</c> in a pocket, at any depth. Walked rather than assumed to be the probe's one
    /// child, because a pocket Jeremy authors may nest more than one and a missed viewport is an invisible
    /// cost rather than a visible fault.
    /// </summary>
    private static System.Collections.Generic.List<SubViewport> FindViewports(Node root)
    {
        var found = new System.Collections.Generic.List<SubViewport>();
        if (root is SubViewport viewport) found.Add(viewport);

        foreach (var child in root.GetChildren())
            found.AddRange(FindViewports(child));

        return found;
    }
}
