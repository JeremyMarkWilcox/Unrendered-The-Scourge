using Godot;

namespace Unrendered.Core;

/// <summary>
/// Everything a view needs to draw one entity right now — the fully resolved answer to
/// "what does this thing look like, given its state and the realm I'm standing in".
/// Views never branch on state themselves; they ask for one of these and render it.
/// </summary>
public sealed class EntityPresentation
{
    /// <summary>False means the entity is simply not perceivable from here.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Null falls back to the procedural placeholder silhouette.</summary>
    public Texture2D Texture { get; init; }

    /// <summary>Name shown on hover, appropriate to the form being looked at.</summary>
    public string Label { get; init; } = "";

    public Color Modulate { get; init; } = Colors.White;

    /// <summary>Whether this thing is currently barring a path.</summary>
    public bool BlocksPath { get; init; }

    /// <summary>Whether clicking it does anything meaningful.</summary>
    public bool Interactable { get; init; }

    /// <summary>Hover verb: "Sever", "Observe", "Harvest".</summary>
    public string Verb { get; init; } = "";

    /// <summary>What happens, or why nothing does, when clicked.</summary>
    public string InteractText { get; init; } = "";

    // ---- Placeholder silhouette (used while Texture is null) -------------

    public Color PlaceholderColor { get; init; } = Colors.Magenta;
    public Vector2 PlaceholderSize { get; init; } = new(260, 200);

    /// <summary>Ragged for nightmares, clean for porcelain nobles.</summary>
    public bool PlaceholderJagged { get; init; }

    /// <summary>Dark royalty only. A purified field rat does not get a crown.</summary>
    public bool PlaceholderCrowned { get; init; }
}
