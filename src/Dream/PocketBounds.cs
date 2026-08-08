using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Unrendered.Dream;

/// <summary>
/// The invisible box a pocket is played inside. Set a size, get four walls and a ceiling built out of
/// collision and nothing else — no meshes, no material, nothing to render.
///
/// <para><b>The same three jobs in all fourteen pockets, from one number.</b> Every pocket needs the player
/// kept in, and <see cref="DreamVerb.Float"/> in particular needs a lid: rising has no natural limit but the
/// Resolve it costs, and a full bar buys enough height to leave any room. Building that by hand fourteen
/// times is fourteen chances to leave one side open, and an open side is a bug you only find by walking into
/// it.</para>
/// </summary>
/// <remarks>
/// <b>Put it at floor level.</b> The clear volume runs from this node's origin *up* by <see cref="Size"/>.Y,
/// and is centred on it in X and Z — so dropping it at the middle of the floor and typing the room's
/// dimensions is the whole of the authoring.
///
/// <para><b>Collision by default and a drawn shell on request</b> — see <see cref="BorderShell"/>. Containing
/// and showing stay separate so an unstyled pocket is still inescapable, but they are on the same node now
/// rather than split across a node and an environment, because the environment turned out to be unable to
/// show a border at all: depth fog measures distance from the camera, so it follows the player and knows
/// nothing about where a wall is.</para>
///
/// <para><b>Not a <c>[Tool]</c> script, and that is a deliberate omission rather than an oversight.</b>
/// Building the shapes in the editor would show them as gizmos while you author, which is genuinely useful —
/// but it would be this project's first editor-running script, and a fault in one of those takes the editor
/// with it. It is a one-attribute change if the feedback turns out to be worth that, and it should be its own
/// decision rather than a side effect of this one.</para>
///
/// <para><b>What it deliberately does not do yet: catch you when you fall through a seam.</b> That is
/// `whats-left.md` C5, deferred by Jeremy on 2026-08-07 until there is a real pocket to fall out of, because
/// its one interesting question — what the player sees when it fires — is answered by play. When it lands it
/// belongs here, as one more shape built from the same <see cref="Size"/>.</para>
/// </remarks>
[GlobalClass]
public partial class PocketBounds : Node3D
{
    /// <summary>
    /// The clear volume in metres: width, height, depth. The walls stand **outside** this, so the number you
    /// type is the space you get rather than the space minus two walls.
    /// </summary>
    [Export] public Vector3 Size { get; set; } = new(40.0f, 12.0f, 40.0f);

    /// <summary>
    /// How thick each slab is. **Not a tunnelling guard** — <c>CharacterBody3D</c> sweeps its motion, so at
    /// 2.2 m/s nothing is passing through a wall of any thickness. It is thick enough to be unambiguous and
    /// no thicker; a box costs the same twelve triangles at any scale, and these have no triangles at all.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,10,0.1")]
    public float Thickness { get; set; } = 2.0f;

    /// <summary>The lid that stops Float. On by default, because a pocket without one is escapable.</summary>
    [Export] public bool BuildCap { get; set; } = true;

    /// <summary>
    /// A floor slab under everything. **Off by default**, because a pocket has a real floor you can see and a
    /// second invisible one underneath it would hide exactly the seam you want to find.
    /// </summary>
    [Export] public bool BuildFloor { get; set; }

    /// <summary>
    /// Draw the walls as unshaded grey, so the boundary can be seen as well as felt.
    /// </summary>
    /// <remarks>
    /// <b>This is what depth fog could not do, and the difference is the whole lesson.</b> Fog measures
    /// distance from the camera, so it travels with the player — always clear for the first ten metres in
    /// every direction and always thick past it, wherever you are standing. It cannot mark a border because
    /// nothing about it knows where the border is. Geometry at the boundary does not move, because it is at
    /// the boundary. `FogVolume`, which would have placed fog in a box, is Forward+ only and this project runs
    /// `gl_compatibility`.
    /// <para>
    /// <b>Unshaded on purpose.</b> A lit wall out past the lamp's radius is black, which is what the boundary
    /// looks like today. Unshaded means it reads at <see cref="BorderValue"/> whatever the lighting does,
    /// which is the one thing a boundary has to do.
    /// </para>
    /// </remarks>
    [Export] public bool BorderShell { get; set; }

    /// <summary>
    /// How bright the shell reads, 0 to 1.
    /// </summary>
    /// <remarks>
    /// <b>Scrub it with <c>G</c> / <c>H</c>; it is not a number anybody can pick.</b> What the look shader
    /// reads is the *rendered luminance* of an unshaded surface, not the albedo typed here, and the two are
    /// far enough apart that the first default — 0.55, chosen because the posterise threshold sits near 0.5 —
    /// came out as a flat slab filling the horizon.
    /// <para>
    /// <b>What you are hunting is the dither band, not a brightness.</b> The ordered dither shifts the
    /// threshold per pixel, so there is a range where only *some* pixels cross it and the wall reads as
    /// crosshatch — a haze you can see through. Above that band every pixel crosses and it is an opaque
    /// panel; below it, none do and there is no border at all. Two of those three are wrong and the good one
    /// is narrow.
    /// </para>
    /// </remarks>
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BorderValue { get; set; } = 0.30f;

    private StaticBody3D _body;

    /// <summary>The body the shapes hang off, or null before <c>_Ready</c>. For the gates.</summary>
    public StaticBody3D Body => _body;

    /// <summary>How many slabs are standing. For the gates.</summary>
    public int ShapeCount => _body?.GetChildCount() ?? 0;

    public override void _Ready() => Rebuild();

    /// <summary>
    /// Throw the old box away and build a new one from the current fields.
    /// </summary>
    /// <remarks>
    /// <b>It clears first, and that is the bug this method is shaped around.</b> A rebuild that only added
    /// would double the walls every call — invisible, since the second set is exactly on top of the first, and
    /// harmless right up until something reads <see cref="ShapeCount"/> or a physics query returns two hits
    /// where the author expected one. Public so the gates can call it twice and check that it did not.
    /// </remarks>
    public void Rebuild()
    {
        if (_body == null)
        {
            _body = new StaticBody3D { Name = "Bounds" };
            AddChild(_body);
        }

        foreach (var child in _body.GetChildren())
        {
            _body.RemoveChild(child);
            child.QueueFree();
        }

        var half = Size * 0.5f;
        var t = Mathf.Max(Thickness, 0.01f);
        var mid = half.Y;

        // Walls run the full height plus the slab, so a corner cannot be squeezed through where a wall meets
        // the cap — the one gap this arrangement can have, and it costs nothing to close.
        var wallHeight = Size.Y + t * 2.0f;

        Slab("WallEast", new Vector3(half.X + t * 0.5f, mid, 0.0f), new Vector3(t, wallHeight, Size.Z + t * 2.0f));
        Slab("WallWest", new Vector3(-half.X - t * 0.5f, mid, 0.0f), new Vector3(t, wallHeight, Size.Z + t * 2.0f));
        Slab("WallNorth", new Vector3(0.0f, mid, -half.Z - t * 0.5f), new Vector3(Size.X + t * 2.0f, wallHeight, t));
        Slab("WallSouth", new Vector3(0.0f, mid, half.Z + t * 0.5f), new Vector3(Size.X + t * 2.0f, wallHeight, t));

        if (BuildCap)
            Slab("Cap", new Vector3(0.0f, Size.Y + t * 0.5f, 0.0f),
                new Vector3(Size.X + t * 2.0f, t, Size.Z + t * 2.0f));

        if (BuildFloor)
            Slab("Floor", new Vector3(0.0f, -t * 0.5f, 0.0f),
                new Vector3(Size.X + t * 2.0f, t, Size.Z + t * 2.0f));
    }

    /// <summary>Whether a point in this node's local space is inside the clear volume. For the gates.</summary>
    public bool Contains(Vector3 local) =>
        Mathf.Abs(local.X) <= Size.X * 0.5f
        && Mathf.Abs(local.Z) <= Size.Z * 0.5f
        && local.Y >= 0.0f && local.Y <= Size.Y;

    /// <summary>Whether a point in this node's local space is inside one of the slabs. For the gates.</summary>
    /// <remarks>
    /// Written out rather than asked of the physics server, because a headless run has no physics step and a
    /// query before one would report an empty world — a check that passes because nothing is there.
    /// </remarks>
    public bool Blocks(Vector3 local)
    {
        foreach (var slab in Slabs())
        {
            var d = local - slab.Position;
            var h = ((BoxShape3D)slab.Shape).Size * 0.5f;
            if (Mathf.Abs(d.X) <= h.X && Mathf.Abs(d.Y) <= h.Y && Mathf.Abs(d.Z) <= h.Z) return true;
        }

        return false;
    }

    /// <summary>The drawn faces of the border, if it is drawn at all. For the gates.</summary>
    public IEnumerable<MeshInstance3D> ShellFaces =>
        Slabs().Select(s => s.GetNodeOrNull<MeshInstance3D>("Shell")).Where(m => m != null);

    /// <summary>The slabs, for the gates.</summary>
    public IEnumerable<CollisionShape3D> Slabs()
    {
        if (_body == null) yield break;

        foreach (var child in _body.GetChildren())
            if (child is CollisionShape3D { Shape: BoxShape3D } shape)
                yield return shape;
    }

    private void Slab(string name, Vector3 at, Vector3 size)
    {
        var shape = new CollisionShape3D
        {
            Name = name,
            Position = at,
            Shape = new BoxShape3D { Size = size },
        };

        _body.AddChild(shape);

        if (BorderShell) shape.AddChild(Shell(size));
    }

    /// <summary>A face of the shell: the slab drawn, at the slab's own size and place.</summary>
    /// <remarks>
    /// A child of the shape rather than a sibling, so it inherits the transform that was already worked out
    /// and cannot drift from the wall it is drawing. <c>Rebuild</c> clears the shapes, which takes these with
    /// them — one lifetime, not two.
    /// <para>
    /// <b>Culling is disabled rather than flipped.</b> A boundary is only ever seen from inside, so the
    /// correct-looking answer is to cull the front — but the shell is also the thing you look at from the
    /// editor's outside view while placing it, and a wall that vanishes when you orbit is a wall you cannot
    /// author. Two-sided costs nothing at this triangle count.
    /// </para>
    /// </remarks>
    private MeshInstance3D Shell(Vector3 size)
    {
        var value = Mathf.Clamp(BorderValue, 0.0f, 1.0f);

        return new MeshInstance3D
        {
            Name = "Shell",
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                AlbedoColor = new Color(value, value, value),
            },
        };
    }
}
