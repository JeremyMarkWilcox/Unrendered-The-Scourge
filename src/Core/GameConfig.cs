using Godot;

namespace Unrendered.Core;

/// <summary>Tuning constants. Anything a designer would want to twiddle lives here.</summary>
public static class GameConfig
{
    // ---- Presentation ----------------------------------------------------

    /// <summary>Design resolution of the Waking world. Matches the project viewport.</summary>
    public static readonly Vector2I WakingResolution = new(1920, 1080);

    /// <summary>
    /// Internal resolution of the Pixel Realm. Everything drawn in the Dream is rendered
    /// here and then nearest-neighbour upscaled 4x to fill the screen, so pixels are real
    /// pixels rather than a post-process imitation. All Dream art authors to this grid.
    /// </summary>
    public static readonly Vector2I DreamResolution = new(480, 270);

    /// <summary>Seconds for the spectral-vision crossfade. Logical state flips instantly.</summary>
    public const float RealmCrossfadeSeconds = 0.35f;

    /// <summary>Seconds for the fade-to-black when walking between rooms.</summary>
    public const float RoomTransitionSeconds = 0.25f;

    // ---- Resolve ---------------------------------------------------------

    public const float ResolveMax = 100f;

    // Resolve only moves inside a Dream heist, and each heist owns its own rates.
    // See DreamHeistData.DrainPerSecond / NearDrainPerSecond.

    // ---- Content paths ---------------------------------------------------

    public const string RoomDataDir = "res://data/rooms";
    public const string EntityDataDir = "res://data/entities";
    public const string ItemDataDir = "res://data/items";
    public const string ZoneDataDir = "res://data/zones";
    public const string HeistDataDir = "res://data/heists";

    public const string SaveFilePath = "user://savegame.json";

    /// <summary>Where a new game begins.</summary>
    public const string StartingRoomId = "waystation_yard";

    // ---- Placeholder palette --------------------------------------------
    // Used by PlaceholderBackdrop until real art lands. Cold and desaturated for the
    // Waking world; high-contrast and uncanny for the Dream.

    public static readonly Color WakingSky = new("#2b3138");
    public static readonly Color WakingMid = new("#3a4149");
    public static readonly Color WakingGround = new("#22262b");
    public static readonly Color WakingFog = new("#8d98a3");

    public static readonly Color DreamSky = new("#160d1f");
    public static readonly Color DreamMid = new("#3d1f4d");
    public static readonly Color DreamGround = new("#0b0710");
    public static readonly Color DreamAccent = new("#e8dcc8");

    public static readonly Color PurifiedSky = new("#7fa8c9");
    public static readonly Color PurifiedMid = new("#6f8f5e");
    public static readonly Color PurifiedGround = new("#4a5c3b");
}
