using Godot;

namespace Unrendered.Data;

/// <summary>
/// The jigsaw behind one blight (<c>combat.md</c> §22). Its own file for the same reason
/// <see cref="RecipeIngredient"/> is: Godot resolves a C# script resource by filename, so a nested
/// class here would make the <c>.tres</c> unloadable.
/// </summary>
/// <remarks>
/// <b>It is a click puzzle, not a piece-drag, and §22's own words are why.</b> The section calls the
/// interaction a jigsaw but names its three forms as <i>spot the wrong part among many</i>, <i>do these
/// in the right order</i> and <i>use the right tool on the right thing</i> — all three are "click the
/// right thing(s)", which is one panel with three shapes of data rather than three puzzles. Fifteen
/// instances then cost fifteen data blocks and no art.
/// <para>
/// <b>There is no fail state and there is no timer</b>, per Jeremy's <i>"you cannot fail a slider
/// puzzle."</i> A wrong click resets the sequence and costs nothing. That is not a softness to be fixed
/// later: §22 spent a whole subsection establishing that the mend is an <i>attention</i> gate rather
/// than a skill gate, because gating the good ending on puzzle skill is what the deleted pay-to-skip
/// existed to protect against.
/// </para>
/// <para>
/// <b>The form is derived from the data, never stored.</b> A <c>Form</c> enum beside
/// <see cref="Solution"/> would be the same fact twice and able to disagree with itself — the reason
/// <c>RoomData.IsCamp</c> does not exist beside a <see cref="Core.HotspotKind.Workbench"/> hotspot. See
/// <see cref="IsOrdered"/>.
/// </para>
/// </remarks>
[GlobalClass]
public partial class MendPuzzleData : Resource
{
    /// <summary>What the player is being asked to put right. Pass-3 prose.</summary>
    [Export(PropertyHint.MultilineText)] public string Prompt { get; set; } = "";

    /// <summary>
    /// The parts on the board, in the order they are laid out. These are the labels you click.
    /// </summary>
    [Export] public string[] Parts { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Indices into <see cref="Parts"/>, <b>in the order they must be clicked</b>. One entry is
    /// "spot the wrong part"; several is "do these in the right order".
    /// </summary>
    /// <remarks>
    /// Indices rather than strings so that renaming a part in the inspector cannot silently unsolve the
    /// puzzle — and an index that falls outside <see cref="Parts"/> is the one authoring mistake here
    /// that is invisible in the inspector <i>and</i> silent in play (the part it names is never drawn,
    /// so the mend simply cannot be completed). <c>--verify-slice</c> asserts range and distinctness
    /// for exactly that reason.
    /// </remarks>
    [Export] public int[] Solution { get; set; } = System.Array.Empty<int>();

    /// <summary>
    /// Whether order matters — the <i>do these in the right order</i> form. Derived, so there is
    /// nothing to keep in sync.
    /// </summary>
    public bool IsOrdered => Solution != null && Solution.Length > 1;

    /// <summary>
    /// Whether this puzzle can be finished at all: something to click, and every step naming a part
    /// that exists, no step twice.
    /// </summary>
    /// <remarks>
    /// Lives here rather than in the gate so the panel and the verifier answer the question the same
    /// way. A panel that quietly renders an unsolvable board is worse than one that refuses to open.
    /// </remarks>
    public bool IsSolvable()
    {
        if (Parts == null || Parts.Length == 0) return false;
        if (Solution == null || Solution.Length == 0) return false;

        var seen = new System.Collections.Generic.HashSet<int>();
        foreach (var step in Solution)
        {
            if (step < 0 || step >= Parts.Length) return false;
            if (!seen.Add(step)) return false;
        }

        return true;
    }
}
