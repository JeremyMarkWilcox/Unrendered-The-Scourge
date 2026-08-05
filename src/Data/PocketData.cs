using Godot;
using Unrendered.Core;

namespace Unrendered.Data;

/// <summary>
/// What a Dream pocket <i>grants</i>. It deliberately does not describe how you get in, because all
/// fourteen enter identically — ruled 2026-08-04, when the boss pockets turned out to want the same
/// entrance as the knowledge ones (*"an item related to the boss that you find a few screens away"*).
/// </summary>
/// <remarks>
/// <b>The kind is the only thing that branches, and it branches in exactly one method</b>
/// (<c>PocketManager.Complete</c>). That is the property worth protecting: a boss special case in the
/// *entrance* would have meant two hotspot kinds, two flag patterns and two authoring conventions for
/// what is one act — walking into a broken thing.
/// </remarks>
public enum PocketKind
{
    /// <summary>
    /// One of the eight in `items.md` §8: a craft family remembered, and a permanent point of Resolve.
    /// **The only kind that may raise max Resolve, and nothing outside a pocket may raise it at all.**
    /// </summary>
    Knowledge = 0,

    /// <summary>
    /// One of the three behind the Snarl. Writes <c>Flags.KnotCut</c> and nothing else — the door it opens
    /// was built and gated in unit 1 (<c>HotspotData.RequiredFlags</c>), so a knot pocket owes this unit a
    /// single flag.
    /// </summary>
    Knot = 1,

    /// <summary>
    /// A boss's tether. Severs <see cref="PocketData.TargetEntityId"/>, which is the only way it can be
    /// severed — invariant 4: you cannot reach the anchor from the Waking world.
    /// </summary>
    Boss = 2,
}

/// <summary>
/// One Dream pocket: an id, what it grants, and the scene to walk around in.
///
/// <para><b>It is not a graph of rooms, and that is the whole difference from the <c>DreamHeistData</c> it
/// replaced (deleted in 6b).</b> That model reused <c>RoomData</c> and derived everything —
/// the maze, the pursuer's pathfinding, whether a room was "in the Dream" — by walking Navigate hotspots. A
/// pocket is a 3D scene, so none of those derivations have anything to derive, and a pocket id never
/// appears in <c>NavigationManager</c> at all. Jeremy, 2026-08-04: *"keep building the two worlds separated
/// besides travel to the 3D level from the 2D and then the 3D back to the 2D."*</para>
///
/// <para><b>Quiet by construction.</b> There is no pursuer field, no drain and no clock. The pursuer is
/// boss-pocket-only and belongs to the 3D pass; Resolve is a fuel tank spent by verbs, never a per-second
/// cost (<c>spirit-light-and-resolve</c>), so <c>DrainPerSecond</c> had no successor rather than a new
/// home.</para>
/// </summary>
[GlobalClass]
public partial class PocketData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";

    /// <summary>
    /// Shown where the room name goes while you are inside. A pocket is not a room, so the HUD has nothing
    /// else to put there.
    /// </summary>
    [Export] public string DisplayName { get; set; } = "";

    [Export] public PocketKind Kind { get; set; } = PocketKind.Knowledge;

    [ExportGroup("The scene")]
    /// <summary>
    /// Path to the pocket's own scene — a <c>Control</c> whose child is a full-rect
    /// <c>SubViewportContainer</c>, which is the shape <c>scenes/dream/DreamProbe.tscn</c> already has.
    /// </summary>
    /// <remarks>
    /// <b>A string rather than an <c>[Export] PackedScene</c>, and the reason is load timing.</b> An
    /// exported scene reference is a dependency of this resource, so it loads when the resource loads —
    /// which would put all fourteen pockets' geometry, materials and lights in memory the moment the
    /// registry is scanned at boot, and quietly delete the instantiate-on-entry/free-on-exit property the
    /// seam is built around. A string is loaded by <c>PocketManager</c> when you actually go in.
    /// <para>
    /// The cost of a string is that a typo is invisible until someone walks into it, which is why the gates
    /// cross this field against <c>ResourceLoader.Exists</c> — one authored collection naming something in
    /// another is the crossing that never gets checked on its own.
    /// </para>
    /// </remarks>
    [Export] public string ScenePath { get; set; } = "";

    [ExportGroup("What it grants")]
    /// <summary>
    /// The blueprint remembered here. <see cref="PocketKind.Knowledge"/> only, and this is the mechanism
    /// `items.md` §8 pays a pocket for: nothing in <c>data/</c> grants a blueprint today, which is why
    /// <c>F6</c> is currently the only thing that opens any gated recipe.
    /// </summary>
    [Export] public string GrantsBlueprintId { get; set; } = "";

    /// <summary>
    /// Points of permanent Resolve. <b>One per knowledge pocket and nothing else in the game may grant a
    /// point</b> (`build-plan.md` §6 rule 6, and eight is the whole budget).
    /// </summary>
    /// <remarks>
    /// Max <i>Vitality</i> never rises, ever — that asymmetry is deliberate and load-bearing
    /// (<c>spirit-light-and-resolve</c>): the body is a fixed thing you spend and mend, and only the spirit
    /// grows. So there is no Vitality twin of this field and there must never be one.
    /// </remarks>
    [Export(PropertyHint.Range, "0,10,1")]
    public int GrantsMaxResolve { get; set; }

    /// <summary>
    /// Instance id of the body whose tether is cut here. <see cref="PocketKind.Boss"/> only.
    /// </summary>
    [Export] public string TargetEntityId { get; set; } = "";

    /// <summary>
    /// The Dream verb pulling this anchor teaches. <see cref="PocketKind.Boss"/> only, and **only the first
    /// two boss pockets set it** — <see cref="DreamVerb.Float"/> then <see cref="DreamVerb.Flare"/>.
    /// </summary>
    /// <remarks>
    /// <b>Ratified 2026-08-04, and the *where* was the question.</b> `combat.md` §19 says Float arrives
    /// *"after boss 1"*, which could have meant the boss pocket or the Waking-world fight that follows it.
    /// Jeremy: *"you get the dream abilities after pulling the anchor for boss 1 and 2. Boss 3 ends the game
    /// so you don't."* So it hangs here, on the pocket, and it is granted by <c>Complete()</c> alongside the
    /// sever — one act, two consequences.
    /// <para>
    /// <b>Two grants, not three.</b> <see cref="DreamVerb.Suppress"/> is held from the first frame so nothing
    /// grants it, and boss pocket 3 grants nothing because it is the finale. That makes the budget *exactly
    /// two*, which is tighter than the one-per-pocket shape max Resolve uses and is billed as such by
    /// <c>--verify-content</c>.
    /// </para>
    /// <para>
    /// <b>The rule this creates cannot be checked and so is written down instead:</b> every boss pocket must
    /// be completable with the verbs you already hold when you reach it. Boss pocket 1 is solvable with
    /// Suppress alone, because Float is what is inside it; boss pocket 2 without Flare. A boss pocket whose
    /// anchor needs the verb it grants is unwinnable, and nothing in code could catch it — it is 3D geometry,
    /// the same category as `placement.md` rule 7's *"ch1 pockets are flat"*, which this generalises.
    /// </para>
    /// </remarks>
    [Export] public DreamVerb GrantsAbility { get; set; } = DreamVerb.None;

    // There is deliberately no `ReturnRoomId`, which `DreamHeistData` had. You come back to the screen you
    // left, and that is a runtime fact rather than an authored one — the entrance is a hotspot on a
    // specific screen, so authoring the return would be storing a second copy of where the door is.
    // Two facts that must always agree, which is the `IsSanctuary` mistake.
}
