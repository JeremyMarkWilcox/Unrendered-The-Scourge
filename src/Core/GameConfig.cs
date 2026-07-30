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

    // ---- Vitality --------------------------------------------------------

    /// <summary>
    /// The body's pool. There is deliberately no rate beside it: Vitality has no clock and
    /// never ticks — it moves only when something wounds you or you treat a wound
    /// (docs/combat.md §6). Max rises through rare items, never levels, so this is the
    /// starting ceiling rather than a permanent one.
    /// </summary>
    public const float VitalityMax = 100f;

    // ---- Combat ----------------------------------------------------------

    /// <summary>
    /// How many traps you may rig before a fight (docs/combat.md §18). **Two**, and the number is
    /// here rather than on the creature because it is a tuning dial, not authored content.
    ///
    /// Two rather than one because one slot makes every fight a single pick with nothing interacting,
    /// and the oil-slick-plus-ignition combination §4 is built around needs two to exist at all.
    /// Jeremy's framing: "give two options so that players can mix and match."
    ///
    /// The sites hang off the creature, not the room — which is why no <c>RoomData</c> or
    /// <c>HotspotData</c> field corresponds to this, and why Waking-world rooms could be authored
    /// before the encounter loop existed.
    /// </summary>
    public const int RigSlotsPerFight = 2;

    /// <summary>
    /// Multiplier when you hit what a creature is vulnerable to, and when you hit what it resists.
    /// Ratified 2026-07-30 (§18): the pair replaced Defence as a number entirely.
    ///
    /// **These are not a violation of §11's no-damage-multipliers invariant**, and the distinction is
    /// worth keeping straight: §11 prohibits multipliers that *grow* — a weapon tier, a strength stat,
    /// anything that makes the same action better later. These are fixed, two-sided, and authored on
    /// the *target*, so they change which resource you spend rather than how much power you have.
    /// </summary>
    public const float VulnerableMultiplier = 2f;

    /// <summary>Never zero. §7: an absolute reads as a lock and sends players hunting a key.</summary>
    public const float ResistantMultiplier = 0.5f;

    /// <summary>
    /// Where §5's condition ladder changes word, as a fraction of durability **remaining**. Read as:
    /// above <see cref="BleedingBelow"/> is `Unmarked`, and at or below <see cref="CollapsingBelow"/>
    /// is `Collapsing`.
    ///
    /// Tuning numbers, deliberately not gated — a gate on these would go red the first time Jeremy
    /// wanted a fight to *read* differently, which is exactly the kind of gate that teaches people to
    /// ignore red. What is gated is the ladder's *shape*: monotonic, total, and never leaving a
    /// creature without a word.
    /// </summary>
    public const float BleedingBelow = 0.75f;

    public const float FalteringBelow = 0.4f;
    public const float CollapsingBelow = 0.15f;

    // ---- Content paths ---------------------------------------------------

    public const string RoomDataDir = "res://data/rooms";
    public const string EntityDataDir = "res://data/entities";
    public const string ItemDataDir = "res://data/items";
    public const string RecipeDataDir = "res://data/recipes";
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
