using Godot;

namespace Unrendered.Core;

/// <summary>Tuning constants. Anything a designer would want to twiddle lives here.</summary>
public static class GameConfig
{
    // ---- Presentation ----------------------------------------------------

    /// <summary>Design resolution of the Waking world. Matches the project viewport.</summary>
    public static readonly Vector2I WakingResolution = new(1920, 1080);

    // `DreamResolution` (480x270) and `RealmCrossfadeSeconds` (0.35) were deleted 2026-08-02 with
    // spectral vision. The first was the SubViewport the Dream layer rendered into and the grid all
    // Dream art authored to; the second timed the fade between the two stacked layers. Neither
    // survives the move to a 3D Dream — if that render target wants a pixel grid, it will pick its
    // own, in three dimensions, and it should not inherit a number chosen for a 2D overlay.

    /// <summary>Seconds for the fade-to-black when walking between rooms.</summary>
    public const float RoomTransitionSeconds = 0.25f;

    // ---- Resolve ---------------------------------------------------------

    public const float ResolveMax = 100f;

    // The starting ceiling only: `ResolveManager.Max` rises one point per Dream knowledge pocket
    // (unit 6a, `build-plan.md` §6 rule 6). It was `=> GameConfig.ResolveMax` until then, which is why
    // the eight grants had no mechanism to land in.
    //
    // **Two things drain Resolve as of 2026-08-05, and neither is a clock.** The Dream verbs spend it per
    // second while held (below), and a boss's aura spends it per player turn while its tether is unbroken
    // (`combat.md` §6). The per-second clock the deleted heist ran on is not one of them, and the boss drain
    // deliberately did not inherit its shape: a fight is turn-based, so a wall clock there would charge the
    // player for thinking. There is still no ambient drain and there never was one.

    /// <summary>
    /// What each Dream verb costs per second while **held** (`combat.md` §19). One meter, three verbs, and
    /// they are **additive** — §19 is explicit that *"floating spends the same meter suppression does, so the
    /// climb is paid for out of the hiding."*
    /// </summary>
    /// <remarks>
    /// <b>Numbers, not rules, and deliberately not gated.</b> A check on these would go red the first time
    /// Jeremy wanted the Dream to feel different, which is the fourth blind-spot class. What *is* gated is the
    /// shape: that the rate is a sum, that it is zero when nothing is held, and that nothing drains outside a
    /// pocket.
    /// <para>
    /// Suppress is cheapest because it is the verb you hold longest — it is the one you start with and the
    /// only way past a minor creature, so pricing it like the other two would make hiding the expensive
    /// option. Flare is dearest: it reveals *and* attracts, and §19 wants that price real.
    /// </para>
    /// </remarks>
    public const float SuppressDrainPerSecond = 2.0f;

    public const float FloatDrainPerSecond = 4.0f;

    public const float FlareDrainPerSecond = 6.0f;

    /// <summary>
    /// Seconds a Dream creature must wait before it can cost you again — the default a
    /// <c>DreamCreatureData</c> may override.
    /// </summary>
    /// <remarks>
    /// <b>The load-bearing number in the whole creature model.</b> Contact costs a chunk of Resolve rather
    /// than health, and an <c>Area3D</c> reports contact every frame — so with no cooldown, standing inside a
    /// creature empties the meter in well under a second and the player is ejected before they know what
    /// touched them. It is the kind of fault that is invisible in code and obvious in play, which is why it
    /// has a gate rather than a comment.
    /// </remarks>
    public const float CreatureTouchCooldownSeconds = 1.5f;

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
    /// How many healing salves you may take into a fight (`items.md` §2). **Two, and it is a per-fight
    /// slot cap rather than a carry limit** — the distinction is the whole reason this constant exists
    /// beside <see cref="RigSlotsPerFight"/> instead of in the inventory.
    /// </summary>
    /// <remarks>
    /// <b>There is deliberately no carry cap anywhere in the game</b>, and it would be easy to add one
    /// here by accident. `items.md` §1 is explicit: *"There is no cap, so you are never maxed"* — written
    /// against Jeremy's peeve, *"I do hate finding things you are maxed on in games."* A satchel limit
    /// makes a pickup a wasted pickup; a slot limit makes a loadout a choice. Same number, opposite
    /// feeling, and only one of them is wanted.
    /// <para>
    /// The two caps do not arbitrate against each other and must never be merged into one pool (§2:
    /// *"salve slots are two healing items, trap slots are two traps, and nothing arbitrates"*). This is
    /// also why offensive brews — Burning Oil, Corrosive Draught — are authored as
    /// <see cref="Data.ItemCategory.Component"/> rather than Salve: they are bench inputs, never carried
    /// into a fight, so the question of which cap they count against is void rather than answered.
    /// </para>
    /// <para>
    /// Nothing reads this yet — the loadout screen is unit 3, the same milestone that first reads
    /// <see cref="RigSlotsPerFight"/>. It lands with the shapes for the reason §18 gives: a number the
    /// arsenal is authored against is cheaper now than after twenty items exist.
    /// </para>
    /// </remarks>
    public const int SalveSlotsPerFight = 2;

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
    /// How much a matching coat takes off an incoming blow — <b>the target-side mirror of
    /// <see cref="ResistantMultiplier"/></b>, and the same number for the same reason. Added by unit 3a,
    /// which is the first thing that could be hit.
    /// </summary>
    /// <remarks>
    /// A separate constant rather than reusing <see cref="ResistantMultiplier"/>, though they are equal
    /// today: one is authored on a creature and read when you attack, the other is authored on an item
    /// and read when you are attacked. Sharing the field would make tuning one of them silently tune the
    /// other, and §18 is explicit that the coat is a *trade* — so it is the half most likely to move.
    /// <para>
    /// <b>Halved, never negated.</b> §18: an immunity reads as a lock, which is the argument
    /// <see cref="ResistantMultiplier"/>'s own summary makes from the other side.
    /// </para>
    /// </remarks>
    public const float WardMultiplier = 0.5f;

    /// <summary>
    /// How much of its own durability an anchored body closes back up on each of its turns, as a fraction.
    /// <c>combat.md</c> §2's *"regenerates every turn"*, finally with a number under it. Reinstated
    /// 2026-08-05 on Jeremy's ruling — *"regen hp on enemy's turn and resolve drain on the players turn
    /// makes sense in my head. Double whammy"* — over an earlier deferral that had declined the per-turn
    /// heal outright.
    /// </summary>
    /// <remarks>
    /// <b>The number has one hard constraint and it is not about difficulty.</b> It must sit strictly below
    /// what a *wrong-type* weapon does per turn, because a heal that outpaces the worst weapon in the game
    /// makes that weapon useless rather than slow — and "an immunity reads as a lock" is the exact argument
    /// <see cref="ResistantMultiplier"/> and <see cref="WardMultiplier"/> both refuse to break. At 0.05 a
    /// resisted axe still nets progress every turn; it just will not finish inside the six turns of nerve
    /// the aura also costs you, which is the fight telling you to bring the other tool or cut the tether.
    /// <para>
    /// <b>Sized apart from <c>EntityData.CorruptionAura</c> on purpose, though the aura is what switches it
    /// on.</b> The aura's magnitude is pinned by §6 to a bar emptying in six turns; if regen read the same
    /// number it would be pinned to that too, and at 0.167 of durability per turn a resisted weapon is
    /// exactly the lock above. One authored float cannot carry two effects whose right sizes disagree, so
    /// the field says <i>whether</i> and this dial says <i>how much</i>.
    /// </para>
    /// <para>
    /// Not gated as a value, for <see cref="SuppressDrainPerSecond"/>'s reason — a check on the number goes
    /// red the day Jeremy tunes it. The *constraint* above is gated, against the weakest weapon in the
    /// arsenal, and so is the shape: that it stops at zero wounds, that severing switches it off, and that
    /// a creature with no aura never heals.
    /// </para>
    /// </remarks>
    public const float AnchoredRegenFraction = 0.05f;

    /// <summary>
    /// What a strike does with nothing equipped. §18 keeps a weak baseline attack so that <b>a player out
    /// of items has a fight with verbs rather than a fight with none</b> — this is the number that makes
    /// that true, and it is the floor of the whole damage economy.
    /// </summary>
    /// <remarks>
    /// 3 against the roster's default <c>Durability = 30</c> is ten turns unmodified — deliberately
    /// outside §5's 3–6 band, because bare hands are meant to be the answer to *"I have nothing"* rather
    /// than a viable plan. Guaranteed flight (§3) is what makes that safe to be genuinely bad.
    /// <para>
    /// It is <see cref="BareHandedType"/>, not <c>None</c>: <c>DamageAgainst</c> returns 0 for a typeless
    /// hit, so a typeless baseline would be no baseline at all. Fists are weight, so a creature may
    /// resist them and may even be vulnerable to them.
    /// </para>
    /// </remarks>
    public const int BareHandedPotency = 3;

    /// <summary>
    /// The type a strike carries with nothing equipped. See <see cref="BareHandedPotency"/> for why it
    /// is a real type rather than <c>None</c>.
    /// </summary>
    /// <remarks>
    /// <b>A constant because it is read in three places that must agree</b>, added 2026-08-03. It was
    /// hardcoded at <c>EncounterManager.Strike</c> and described in two doc comments, and then the fight
    /// screen needed it too — to tell you *"Strike — bare hands (weight)"*, which is the sentence that
    /// lets you connect the verb you are pressing to the reading above it saying the thing barely
    /// notices weight. A fourth copy in the gate over that would have made the gate agree with itself
    /// rather than with the game.
    /// </remarks>
    public const DamageType BareHandedType = DamageType.Blunt;

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
    // `ZoneDataDir` and the `ZoneData`/`ChapterData` resource it pointed at were deleted 2026-08-02.
    // The class had no reader and never had one: the constant was declared and never used, and
    // everything chapter-shaped runs off `RoomData.ChapterId` strings. Jeremy: "no point in having
    // more overhead than needed." A chapter has no data of its own until something needs to *show*
    // a chapter — a title card, a save slot label — and that is the moment to bring the class back,
    // with a loader, rather than keeping an empty one warm against the possibility.
    // `HeistDataDir` and `data/heists/` were deleted 2026-08-04 with unit 6b, along with `DreamHeistData`
    // and `DreamRunManager`. The directory had been empty since unit 3b retired the prototype heist, so the
    // constant had been pointing at nothing for a day — which is the honest reason it is a deletion rather
    // than a migration: nothing was moved because nothing was there.

    /// <summary>Dream pockets (unit 6). The replacement for the deleted heist registry.</summary>
    public const string PocketDataDir = "res://data/pockets";

    /// <summary>
    /// Dream creature types. <b>There is no registry behind this and there should not be one</b> — a
    /// <c>DreamCreature</c> node points at its <c>DreamCreatureData</c> through an <c>[Export]</c>, so the
    /// files are reached by the scenes that use them and nothing has to load them all.
    /// </summary>
    /// <remarks>
    /// <b>The constant exists because the verifier reads it, and for no other reason.</b> <c>ZoneDataDir</c>
    /// was declared, never used, and deleted on 2026-08-02 along with the class it pointed at; a path with no
    /// reader is a claim that a directory means something. This one's reader is the roster sweep in
    /// <c>SliceVerifier</c>, which is what makes the fifth blind-spot class — a file on disk that never became
    /// content — closable for creatures the way it already is for rooms, items, recipes and pockets.
    /// </remarks>
    public const string DreamCreatureDataDir = "res://data/creatures";

    public const string SaveFilePath = "user://savegame.json";

    /// <summary>
    /// Where a new game begins. Moved off <c>waystation_yard</c> on 2026-08-02, the day the 59
    /// skeleton screens landed — the second half of the great swap deletes the prototype, but
    /// re-pointing the boot is independent of that and is what makes the new map reachable at all.
    ///
    /// <para>
    /// The waystation and the six <c>dream_way_*</c> rooms outlived the move by a day: they still
    /// loaded, nothing walked to them, and they stayed until the gates using them as fixtures were
    /// re-anchored onto synthetic <c>verify_</c> rooms. **Unit 3b did that and deleted all eight.**
    /// Splitting it in two was the right call and worth keeping as a rule — deleting a fixture out from
    /// under a gate and authoring new content are different kinds of change, and doing both at once
    /// makes it impossible to tell which half broke something.
    /// </para>
    ///
    /// <para>
    /// <b>This room is a camp</b> (<c>ch1_cottage_bench</c>), which is required rather than incidental:
    /// with <c>waystation_shed</c> gone it is the only bench in the game, and crafting, the shared
    /// cache and the respawn destination are all gated on standing at one. It also means arriving here
    /// refills both pools — see the fourth blind-spot class in <c>handoff.md</c> §3 before writing any
    /// gate that carries a part-drained bar through the starting room.
    /// </para>
    /// </summary>
    public const string StartingRoomId = "ch1_cottage";

    /// <summary>
    /// What he already owns. Granted once when a new game begins, and the satchel is otherwise empty.
    /// </summary>
    /// <remarks>
    /// <b>This exists because unit 4b authored an item nothing in the world could give you.</b>
    /// `items.md` §4 lists the Splitting Axe as *"starting — his own tool"* with no inputs and no recipe,
    /// and until today "starting" had exactly one working meaning in this project: a recipe with a blank
    /// <see cref="Data.RecipeData.RequiredBlueprintId"/> is known from the first frame. There was no
    /// mechanism at all for a starting *object* — the satchel began empty and stayed that way — so the
    /// axe would have been catalogued, gated, displayed in the journal, and unobtainable.
    /// <para>
    /// <b>The gate that matters is the general one</b>, not a check that this list is non-empty: every
    /// authored item must be a harvest yield, a recipe output, or in here. A one-item starting kit is easy
    /// to eyeball; an unobtainable coat two content passes from now is not.
    /// </para>
    /// <para>
    /// Not a "loadout" and not equipping. The armour slot starts empty on purpose (`items.md` §4: *"the
    /// first coat should be a moment rather than a default"*), and what it means to *wear* the axe is
    /// unit 3's question. This puts it in the satchel and stops there.
    /// </para>
    /// </remarks>
    public static readonly string[] StartingItems = { "weapon_splitting_axe" };

    // ---- Placeholder palette --------------------------------------------
    // Used by PlaceholderBackdrop until real art lands. Cold and desaturated for corrupted ground,
    // warm and open once a chapter is cleansed.

    public static readonly Color WakingSky = new("#2b3138");
    public static readonly Color WakingMid = new("#3a4149");
    public static readonly Color WakingGround = new("#22262b");
    public static readonly Color WakingFog = new("#8d98a3");

    // The four Dream colours — #160d1f sky, #3d1f4d mid, #0b0710 ground, #e8dcc8 accent — went with
    // DrawDream on 2026-08-02. The Dream's real palette is settled and different anyway: black,
    // white and grey, authored into a shader (dream-realm-monochrome-aesthetic).

    public static readonly Color PurifiedSky = new("#7fa8c9");
    public static readonly Color PurifiedMid = new("#6f8f5e");
    public static readonly Color PurifiedGround = new("#4a5c3b");
}
