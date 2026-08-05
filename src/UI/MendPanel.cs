using System.Collections.Generic;
using Godot;
using Unrendered.Core;
using Unrendered.Data;
using Unrendered.Managers;

namespace Unrendered.UI;

/// <summary>
/// The jigsaw, on screen (<c>combat.md</c> §22, unit 2). Opened by <c>Hud</c> off
/// <c>EventBus.MendRequested</c>, and it calls <see cref="MendManager.Mend"/> once the board is solved
/// — which is the whole of its authority. It decides nothing about the world.
///
/// <para>
/// Layout lives in <c>scenes/ui/MendPanel.tscn</c>, on the <see cref="InventoryPanel"/> precedent: this
/// file knows the node <i>names</i> in the published contract and nothing about their arrangement.
/// </para>
///
/// <para>
/// <b>One board serves all three of §22's forms</b>, because all three are "click the right thing(s)":
/// a one-step <c>Solution</c> is <i>spot the wrong part</i>, a longer one is <i>do these in the right
/// order</i>, and the tool form is the same board reached through a door you need the tool to open — so
/// it needs no code here at all. That last one is worth stating plainly: the tool gate is
/// <b>geography</b>, not a field on the puzzle and not a branch in this panel.
/// </para>
///
/// <para>
/// <b>There is no fail state and no timer</b> (Jeremy: <i>"you cannot fail a slider puzzle"</i>). A
/// wrong click on an ordered board resets the sequence and costs nothing — no Resolve, no Vitality, no
/// material. §22 makes the mend an <i>attention</i> gate rather than a skill gate on purpose, because
/// the good ending is priced in Graveflower and gating that on dexterity is what the deleted
/// pay-to-skip was invented to protect against.
/// </para>
/// </summary>
public partial class MendPanel : Control
{
    private Label _title;
    private Label _prompt;
    private Container _partContainer;
    private Button _partTemplate;
    private Label _status;
    private Button _closeButton;

    private HotspotData _blight;
    private readonly List<Button> _parts = new();

    /// <summary>How many steps of <see cref="MendPuzzleData.Solution"/> are correctly in.</summary>
    private int _step;

    /// <summary>A part already seated. Bright, against <see cref="LooseAlpha"/> for the rest.</summary>
    private const float LooseAlpha = 0.72f;

    // ---- For the gates ---------------------------------------------------
    // Read off what is on screen rather than recomputing, for the same reason InventoryPanel's
    // properties do: a property that recomputes only proves it agrees with itself.

    /// <summary>Whether the board is up. Reads <c>Visible</c> rather than a bool of its own.</summary>
    public bool IsOpen => Visible;

    /// <summary>Which blight is on the board, or "" when none.</summary>
    public string BlightId => _blight?.Id ?? "";

    /// <summary>How many part buttons are drawn — template excluded.</summary>
    public int PartCount => _parts.Count;

    /// <summary>Correct clicks so far. Reaching the solution's length is what solves it.</summary>
    public int Step => _step;

    /// <summary>The line under the board, as drawn.</summary>
    public string StatusText => _status?.Text ?? "";

    public override void _Ready()
    {
        _title = GetNode<Label>("%Title");
        _prompt = GetNode<Label>("%Prompt");
        _partContainer = GetNode<Container>("%PartContainer");
        _partTemplate = GetNode<Button>("%PartTemplate");
        _status = GetNode<Label>("%Status");
        _closeButton = GetNode<Button>("%CloseButton");

        _partTemplate.Visible = false;
        _closeButton.Pressed += Close;
    }

    public override void _ExitTree()
    {
        if (_closeButton != null) _closeButton.Pressed -= Close;
    }

    // ---- Opening and closing ---------------------------------------------

    /// <summary>
    /// Put a blight's board up. Refuses rather than showing an empty frame when there is nothing to
    /// click, which is the one authoring mistake in <see cref="MendPuzzleData"/> that is invisible in
    /// the inspector — an out-of-range <c>Solution</c> index names a part that is never drawn, so the
    /// mend simply could not be finished. Returns whether the board opened.
    /// </summary>
    /// <remarks>
    /// The refusal is loud on purpose. <c>--verify-slice</c> fails on an unsolvable board so this
    /// should be unreachable in a checked build; if it is ever reached anyway, a pushed error and a
    /// line the player can read beats a frame with no buttons in it.
    /// </remarks>
    public bool Open(HotspotData blight)
    {
        // Not a bare kind test: a major repair is a Station with a board on it, and MendManager owns
        // which hotspots those are (`MendManager.CarriesABoard`, 2026-08-05).
        if (!MendManager.CarriesABoard(blight)) return false;

        var puzzle = blight.MendPuzzle;
        if (puzzle == null || !puzzle.IsSolvable())
        {
            GD.PushError($"[MendPanel] '{blight.Id}' has no solvable puzzle authored; refusing to open.");
            EventBus.Instance?.EmitNotify("You can see what is wrong with it and no way to put it right. [unauthored puzzle]");
            return false;
        }

        _blight = blight;
        _step = 0;

        _title.Text = string.IsNullOrEmpty(blight.Label) ? "MENDING" : blight.Label.ToUpperInvariant();
        _prompt.Text = puzzle.Prompt;

        Rebuild();
        Visible = true;
        return true;
    }

    public void Close()
    {
        Visible = false;
        _blight = null;
        _step = 0;
    }

    // ---- The board -------------------------------------------------------

    private void Rebuild()
    {
        // Removed before freeing so the grid re-flows this frame. The per-button lambdas need no
        // unsubscribing — they are connected to the button itself, so they die with it.
        foreach (var part in _parts)
        {
            _partContainer.RemoveChild(part);
            part.QueueFree();
        }
        _parts.Clear();

        var puzzle = _blight?.MendPuzzle;
        if (puzzle?.Parts == null) return;

        for (var i = 0; i < puzzle.Parts.Length; i++)
        {
            var button = (Button)_partTemplate.Duplicate();
            button.Name = $"Part_{i}";
            button.Visible = true;
            button.Text = puzzle.Parts[i];

            var index = i;
            button.Pressed += () => OnPartPressed(index);

            _partContainer.AddChild(button);
            _parts.Add(button);
        }

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        var puzzle = _blight?.MendPuzzle;
        if (puzzle?.Solution == null) return;

        _status.Text = puzzle.IsOrdered
            ? $"{_step} of {puzzle.Solution.Length} set."
            : "";

        // Parts already seated stay lit, so an ordered board shows its own progress without a
        // separate readout. On a single-step board nothing is ever seated, so nothing dims.
        for (var i = 0; i < _parts.Count; i++)
        {
            var seated = false;
            for (var s = 0; s < _step; s++)
                if (puzzle.Solution[s] == i) seated = true;

            _parts[i].Modulate = new Color(1f, 1f, 1f, seated ? 1f : LooseAlpha);
        }
    }

    // ---- Clicking --------------------------------------------------------

    private void OnPartPressed(int index)
    {
        var puzzle = _blight?.MendPuzzle;
        if (puzzle?.Solution == null || _step >= puzzle.Solution.Length) return;

        if (puzzle.Solution[_step] != index)
        {
            // Wrong. The sequence resets and nothing else happens — see the class summary.
            var wasStarted = _step > 0;
            _step = 0;
            RefreshStatus();

            _status.Text = wasStarted
                ? "That is not what comes next. Begin again."
                : "That one is sound.";
            return;
        }

        _step++;

        if (_step < puzzle.Solution.Length)
        {
            RefreshStatus();
            return;
        }

        // Solved. The seam is one call wide (§22): everything that happens next — the flag, the
        // Graveflower, the screen's verdict — happens in the manager, so the gates reach all of it
        // without a panel.
        var blight = _blight;
        Close();
        MendManager.Mend(blight);
    }

    /// <summary>
    /// Click a part without a mouse. For <c>--verify-slice</c>, on the
    /// <c>InventoryPanel.DebugSelect</c> precedent — the panel is the one layer that is live headlessly
    /// and untested by everything else, so it has to be drivable.
    /// </summary>
    public void DebugClickPart(int index)
    {
        if (index >= 0 && index < _parts.Count) OnPartPressed(index);
    }

    /// <summary>
    /// Solve the board the honest way: click the authored solution in order, through the same handler
    /// a mouse goes through. Returns whether it closed, which for a solvable board means it mended.
    /// </summary>
    public bool DebugSolve()
    {
        var puzzle = _blight?.MendPuzzle;
        if (puzzle?.Solution == null) return false;

        // Copied first: the last click closes the panel and clears `_blight`, so iterating the live
        // reference would be reading through a field this loop nulls out from underneath itself.
        var solution = (int[])puzzle.Solution.Clone();
        foreach (var step in solution) DebugClickPart(step);

        return !Visible;
    }
}
