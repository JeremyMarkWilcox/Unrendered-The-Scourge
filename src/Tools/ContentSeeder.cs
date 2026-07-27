using Godot;
using Unrendered.Core;
using Unrendered.Data;

namespace Unrendered.Tools;

/// <summary>
/// Writes the vertical-slice content as .tres resources.
///
/// Run with: <c>godot --headless --path . -- --seed-content</c>
///
/// The output is ordinary Godot resources — open and edit them in the inspector like any
/// other. This exists because hand-authoring typed-array resource files is fiddly and
/// fails silently; letting the engine serialise its own objects cannot get it wrong.
/// Re-running overwrites, so treat this as the source of truth for the starting content
/// and stop running it once you have started editing rooms in the editor.
/// </summary>
public static class ContentSeeder
{
    public static void SeedAll()
    {
        EnsureDir(GameConfig.ItemDataDir);
        EnsureDir(GameConfig.EntityDataDir);
        EnsureDir(GameConfig.RoomDataDir);
        EnsureDir(GameConfig.ZoneDataDir);
        EnsureDir(GameConfig.HeistDataDir);

        SeedItems();
        var rat = SeedEntities();
        SeedZones();
        SeedRooms(rat);
        SeedDreamMaze();
        SeedHeists();

        GD.Print("[ContentSeeder] Done.");
    }

    private static void EnsureDir(string path)
    {
        if (DirAccess.DirExistsAbsolute(path)) return;

        var err = DirAccess.MakeDirRecursiveAbsolute(path);
        if (err != Error.Ok) GD.PushError($"[ContentSeeder] Could not create {path}: {err}");
    }

    private static void Save(Resource resource, string path)
    {
        var err = ResourceSaver.Save(resource, path);
        if (err != Error.Ok)
        {
            GD.PushError($"[ContentSeeder] Failed to write {path}: {err}");
            return;
        }

        // Claim the path so that anything referencing this object afterwards serialises it
        // as an ext_resource link rather than embedding a private copy. Without this, each
        // room would bake in its own duplicate of the rat.
        resource.TakeOverPath(path);
        GD.Print($"[ContentSeeder] wrote {path}");
    }

    // ---- Items -----------------------------------------------------------

    private static void SeedItems()
    {
        Save(new ItemData
        {
            Id = "herb_bitterroot",
            DisplayName = "Bitterroot",
            Description = "Grows where the ground has been frightened. Chewing it clears the head and ruins the mouth.",
            Category = ItemCategory.Reagent,
            PlaceholderColor = new Color("#6f8a4f"),
        }, $"{GameConfig.ItemDataDir}/herb_bitterroot.tres");

        Save(new ItemData
        {
            Id = "herb_gravecap",
            DisplayName = "Gravecap Fungus",
            Description = "Pale, cold to the touch, and faintly warm underneath. She says not to eat it.",
            Category = ItemCategory.Reagent,
            PlaceholderColor = new Color("#b9b0a2"),
        }, $"{GameConfig.ItemDataDir}/herb_gravecap.tres");

        Save(new ItemData
        {
            Id = "rat_tallow",
            DisplayName = "Rendered Tallow",
            Description = "Ordinary fat from an ordinary animal. It has been a long time since either of you had that.",
            Category = ItemCategory.Reagent,
            PlaceholderColor = new Color("#d8c79a"),
        }, $"{GameConfig.ItemDataDir}/rat_tallow.tres");

        Save(new ItemData
        {
            Id = "salve_clearmind",
            DisplayName = "Clearmind Salve",
            Description = "Bitterroot and tallow, worked cold. Rub it under the eyes and the second world dims for a while.",
            Category = ItemCategory.Salve,
            ResolveRestore = 35f,
            PlaceholderColor = new Color("#8fb8a8"),
        }, $"{GameConfig.ItemDataDir}/salve_clearmind.tres");

        Save(new ItemData
        {
            Id = "catalyst_ashincense",
            DisplayName = "Ash Incense",
            Description = "Burns in this world. Cuts in the other one.",
            Category = ItemCategory.Catalyst,
            PlaceholderColor = new Color("#c9a2e0"),
        }, $"{GameConfig.ItemDataDir}/catalyst_ashincense.tres");
    }

    // ---- Entities --------------------------------------------------------

    private static EntityData SeedEntities()
    {
        var rat = new EntityData
        {
            Id = "beast_giant_rat",
            CorruptedName = "Cursed Giant Rat",
            SpiritName = "The Gnawing Baron",
            PurifiedName = "Field Rat",
            InitialState = EntityState.Corrupted,
            BlocksPath = true,
            CorruptionAura = 0.30f,
            HarvestItemId = "rat_tallow",
        };
        Save(rat, $"{GameConfig.EntityDataDir}/beast_giant_rat.tres");

        return rat;
    }

    // ---- Zones -----------------------------------------------------------

    private static void SeedZones()
    {
        Save(new ZoneData
        {
            Id = "waystation",
            DisplayName = "The Forgotten Waystation",
            LieutenantEntityId = "waystation.rat_01",
            CorruptionScalar = 1.0f,
            PurificationText = "The distortion breaks. Air moves. Somewhere, birds.",
        }, $"{GameConfig.ZoneDataDir}/waystation.tres");
    }

    // ---- Rooms -----------------------------------------------------------

    private static void SeedRooms(EntityData rat)
    {
        // -- The Forgotten Waystation Yard -- the whole micro-loop lives here.
        var ratPlacement = new EntityPlacement
        {
            InstanceId = "waystation.rat_01",
            Entity = rat,
            WakingPosition = new Vector2(940, 880),
            DreamPosition = new Vector2(238, 205),
            WakingScale = 1.0f,
            DreamScale = 1.0f,
            // Stands in for the regional lieutenant so the slice can demonstrate a full
            // zone purification. M6 gives the Waystation a proper boss and demotes this.
            IsZoneLieutenant = true,
        };

        var yard = new RoomData
        {
            Id = "waystation_yard",
            DisplayName = "The Forgotten Waystation Yard",
            ZoneId = "waystation",
            BaseCorruption = 0.45f,
        };
        yard.Entities.Add(ratPlacement);

        yard.Hotspots.Add(new HotspotData
        {
            Id = "yard_to_shed",
            Label = "The Shed",
            Kind = HotspotKind.Navigate,
            Region = new Rect2(1420, 470, 330, 400),
            TargetRoomId = "waystation_shed",
            BlockedByEntityId = "waystation.rat_01",
            BlockedText = "It lifts its head before you have finished deciding. Not while that thing holds the road.",
        });

        yard.Hotspots.Add(new HotspotData
        {
            Id = "yard_sign",
            Label = "Waystation Marker",
            Kind = HotspotKind.Examine,
            Region = new Rect2(250, 520, 210, 330),
            Visibility = RealmVisibility.WakingOnly,
            ExamineText = "Six names carved into the post, and a seventh scratched out so hard the wood split.",
        });

        yard.Hotspots.Add(new HotspotData
        {
            Id = "yard_dream_door",
            Label = "A Door That Is Not There",
            Kind = HotspotKind.Examine,
            Region = new Rect2(1560, 300, 240, 520),
            Visibility = RealmVisibility.DreamOnly,
            ExamineText = "A doorway stands in the colonnade with no wall around it. In the other world this is empty air.",
        });

        // Only reachable once the field is broken — the GDD's Step 3 payoff.
        yard.Hotspots.Add(new HotspotData
        {
            Id = "yard_bitterroot",
            Label = "Bitterroot",
            Kind = HotspotKind.Harvest,
            Region = new Rect2(560, 890, 260, 150),
            Visibility = RealmVisibility.WakingOnly,
            ItemId = "herb_bitterroot",
            ItemAmount = 2,
            RequiredFlag = "zone.waystation.purified",
            RequiredFlagValue = true,
        });

        // TODO(puzzle): a placeholder for the entry minigame. Solving it exposes the relic.
        yard.Hotspots.Add(new HotspotData
        {
            Id = "yard_cairn",
            Label = "Disturbed Cairn",
            Kind = HotspotKind.Interact,
            Region = new Rect2(300, 880, 300, 170),
            Visibility = RealmVisibility.WakingOnly,
            ExamineText = "The stones have been re-stacked by something with the wrong number of "
                        + "fingers. Underneath, a hollow, and in the hollow something that hums.",
            SetsFlag = "waystation.relic_found",
        });

        yard.Hotspots.Add(new HotspotData
        {
            Id = "yard_relic",
            Label = "The Humming Relic",
            Kind = HotspotKind.CrossToDream,
            Region = new Rect2(330, 930, 220, 130),
            Visibility = RealmVisibility.WakingOnly,
            RequiredFlag = "waystation.relic_found",
            RequiredFlagValue = true,
            HeistId = "waystation_heist",
        });

        Save(yard, $"{GameConfig.RoomDataDir}/waystation_yard.tres");

        // -- The Shed -- second screen, proves navigation and per-room state.
        var shed = new RoomData
        {
            Id = "waystation_shed",
            DisplayName = "Waystation Shed",
            ZoneId = "waystation",
            BaseCorruption = 0.25f,
            PlaceholderStyle = BackdropStyle.Interior,
        };

        shed.Hotspots.Add(new HotspotData
        {
            Id = "shed_to_yard",
            Label = "Back to the Yard",
            Kind = HotspotKind.Navigate,
            Region = new Rect2(70, 430, 300, 520),
            TargetRoomId = "waystation_yard",
        });

        shed.Hotspots.Add(new HotspotData
        {
            Id = "shed_bench",
            Label = "Craftsman's Bench",
            Kind = HotspotKind.Interact,
            Region = new Rect2(760, 560, 420, 260),
            ExamineText = "Your tools, where you left them, under a year of dust. The vice still turns.",
            SetsFlag = "waystation.bench_found",
        });

        shed.Hotspots.Add(new HotspotData
        {
            Id = "shed_gravecap",
            Label = "Gravecap Fungus",
            Kind = HotspotKind.Harvest,
            Region = new Rect2(1350, 800, 260, 180),
            ItemId = "herb_gravecap",
            ItemAmount = 1,
        });

        Save(shed, $"{GameConfig.RoomDataDir}/waystation_shed.tres");
    }

    // ---- The Dream maze --------------------------------------------------

    /// <summary>
    /// The Waystation heist. Six rooms with two loops in them, because a maze you can only
    /// run *down* is a corridor — the loops are what make evading something possible, and
    /// therefore what make it a decision rather than a dice roll.
    ///
    ///     threshold - gallery - stair --- solar
    ///         |          |                 |
    ///      cistern - antechamber ----------+
    /// </summary>
    private static void SeedDreamMaze()
    {
        DreamRoom("dream_way_threshold", "The Threshold",
            "You arrive standing, which is not how you left.",
            ("dream_way_gallery", "Colonnade"), ("dream_way_cistern", "Stairs Down"));

        DreamRoom("dream_way_gallery", "The Long Gallery",
            "Portraits, all of them facing away.",
            ("dream_way_threshold", "Back to the Threshold"), ("dream_way_stair", "The Stair"),
            ("dream_way_antechamber", "A Low Door"));

        DreamRoom("dream_way_cistern", "The Dry Cistern",
            "Something enormous was kept here, and the marks go up the walls.",
            ("dream_way_threshold", "Stairs Up"), ("dream_way_antechamber", "Crawlspace"));

        DreamRoom("dream_way_antechamber", "The Antechamber",
            "Chairs arranged for a conversation that has not finished.",
            ("dream_way_gallery", "The Low Door"), ("dream_way_cistern", "Crawlspace"),
            ("dream_way_solar", "Gilded Doors"));

        DreamRoom("dream_way_stair", "The Turning Stair",
            "It turns the wrong way, and keeps turning.",
            ("dream_way_gallery", "The Gallery"), ("dream_way_solar", "Landing"));

        var solar = DreamRoom("dream_way_solar", "The Baron's Solar",
            "Porcelain, and a great deal of light with nothing making it.",
            ("dream_way_stair", "The Landing"), ("dream_way_antechamber", "Gilded Doors"));

        solar.Hotspots.Add(new HotspotData
        {
            Id = "dream_way_anchor",
            Label = "The Gnawing Baron's Anchor",
            Kind = HotspotKind.TakeAnchor,
            Region = new Rect2(810, 380, 300, 400),
            ExamineText = "",
        });

        Save(solar, $"{GameConfig.RoomDataDir}/dream_way_solar.tres");
    }

    /// <summary>
    /// Builds one maze node and its doors. Saved immediately unless the caller needs to add
    /// more to it first — the solar does, so it is returned unsaved-over.
    /// </summary>
    private static RoomData DreamRoom(string id, string name, string flavour,
                                      params (string Target, string Label)[] doors)
    {
        var room = new RoomData
        {
            Id = id,
            DisplayName = name,
            ZoneId = "waystation_dream",
            BaseCorruption = 1.0f,
            PlaceholderStyle = BackdropStyle.Dream,
        };

        // Doors are laid out across the screen so a three-exit room is readable at a glance.
        var slots = new[] { 180f, 810f, 1440f };
        for (var i = 0; i < doors.Length; i++)
        {
            room.Hotspots.Add(new HotspotData
            {
                Id = $"{id}__to__{doors[i].Target}",
                Label = doors[i].Label,
                Kind = HotspotKind.Navigate,
                Region = new Rect2(slots[i % slots.Length], 420, 300, 480),
                TargetRoomId = doors[i].Target,
            });
        }

        if (!string.IsNullOrEmpty(flavour))
        {
            room.Hotspots.Add(new HotspotData
            {
                Id = $"{id}__look",
                Label = "Look",
                Kind = HotspotKind.Examine,
                Region = new Rect2(760, 120, 400, 200),
                ExamineText = flavour,
            });
        }

        Save(room, $"{GameConfig.RoomDataDir}/{id}.tres");
        return room;
    }

    // ---- Heists ----------------------------------------------------------

    private static void SeedHeists()
    {
        Save(new DreamHeistData
        {
            Id = "waystation_heist",
            DisplayName = "The Gnawing Baron's Solar",
            TargetEntityId = "waystation.rat_01",
            ReturnRoomId = "waystation_yard",
            StartRoomId = "dream_way_threshold",

            // It begins on top of the prize. You have to draw it off, not outrun it.
            PursuerStartRoomId = "dream_way_solar",
            PursuerName = "The Mind-Stalker",
            PursuerMoveSeconds = 3.5f,
            PursuerWanderChance = 0.3f,

            DrainPerSecond = 2.5f,
            NearDrainPerSecond = 3.0f,
            ResolveRestoredOnReturn = 1.0f,

            EntryText = "The hum stops. You are standing somewhere with edges, and the edges are square.",
            SuccessText = "The relic comes away warm. Somewhere behind you, a great deal of porcelain "
                        + "stops being calm.",
            CaptureText = "The Mind-Stalker puts something through you that is not a hand. The world "
                        + "tears along a straight line and you are in the cold again, on your knees.",
            TimeoutText = "Your nerve goes all at once. The Pixel Realm lets go of you the way you would "
                        + "put down something you had stopped finding interesting.",
        }, $"{GameConfig.HeistDataDir}/waystation_heist.tres");
    }
}
