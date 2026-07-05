using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

public sealed class LevelConfig
{
    public const string LevelSelectPath = "res://data/level_select.json";

    public string Id { get; private set; } = "demo_001";
    public string Name { get; private set; } = "Train Defense Demo";
    public float Duration { get; private set; } = 120.0f;
    public float BossStartTime { get; private set; } = 82.0f;
    public List<float> Lanes { get; } = new() { 175.0f, 365.0f };
    public float HeroLineY { get; private set; } = 735.0f;
    public float TrainLineY { get; private set; } = 845.0f;
    public float PlayerMinX { get; private set; } = 52.0f;
    public float PlayerMaxX { get; private set; } = 488.0f;
    public float PlayerStartX { get; private set; } = 270.0f;
    public float HeroBlockRadius { get; private set; } = 54.0f;
    public Dictionary<string, int> TrackLaneIndexes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public PlayerConfig Player { get; } = new();
    public ObjectConfig Objects { get; } = new();
    public RewardConfig Rewards { get; } = new();
    public BossConfig Boss { get; } = new();
    public PickupPairConfig PickupPairs { get; } = new();
    public Dictionary<string, EnemyTypeConfig> EnemyTypes { get; } = new();
    public Dictionary<string, PickupConfig> Pickups { get; } = new();
    public List<TimelineEventConfig> Timeline { get; } = new();

    public static LevelConfig LoadSelected()
    {
        string levelPath = ReadSelectedLevelPath();
        return LoadFromPath(levelPath);
    }

    public static LevelConfig LoadForStation(int station)
    {
        string levelPath = ReadLevelPathForStation(station);
        return LoadFromPath(levelPath);
    }

    public float GetLaneX(int laneIndex)
    {
        if (Lanes.Count == 0)
        {
            return 270.0f;
        }

        int index = laneIndex % Lanes.Count;
        if (index < 0)
        {
            index += Lanes.Count;
        }

        return Lanes[index];
    }

    public int ResolveLaneIndex(string trackId, int fallbackLane)
    {
        string normalized = NormalizeTrackId(trackId);
        if (!string.IsNullOrWhiteSpace(normalized) && TrackLaneIndexes.TryGetValue(normalized, out int laneIndex))
        {
            return laneIndex;
        }

        return fallbackLane;
    }

    public EnemyTypeConfig GetEnemyType(string enemyId)
    {
        if (EnemyTypes.TryGetValue(enemyId, out EnemyTypeConfig config))
        {
            return config;
        }

        foreach (EnemyTypeConfig fallback in EnemyTypes.Values)
        {
            return fallback;
        }

        return EnemyTypeConfig.CreateDefault("grunt");
    }

    public PickupConfig GetPickup(string pickupId)
    {
        if (Pickups.TryGetValue(pickupId, out PickupConfig config))
        {
            return config;
        }

        foreach (PickupConfig fallback in Pickups.Values)
        {
            return fallback;
        }

        return PickupConfig.CreateDefault("attack_add");
    }

    private static string ReadSelectedLevelPath()
    {
        return ReadLevelPathForStation(1);
    }

    private static string ReadLevelPathForStation(int station)
    {
        const string fallbackPath = "res://data/levels/demo_001.json";
        if (!FileAccess.FileExists(LevelSelectPath))
        {
            GD.PushWarning($"Missing level select file: {LevelSelectPath}. Falling back to {fallbackPath}.");
            return fallbackPath;
        }

        string json = FileAccess.GetFileAsString(LevelSelectPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            GD.PushWarning($"Empty level select file: {LevelSelectPath}. Falling back to {fallbackPath}.");
            return fallbackPath;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        string defaultLevel = ReadString(root, "default_level", "demo_001");
        int requestedStation = Math.Max(1, station);

        if (TryGet(root, "levels", out JsonElement levels) && levels.ValueKind == JsonValueKind.Array)
        {
            string firstPath = fallbackPath;
            string defaultPath = fallbackPath;
            bool hasFirstPath = false;
            bool hasDefaultPath = false;
            int levelIndex = 0;
            foreach (JsonElement level in levels.EnumerateArray())
            {
                string id = ReadString(level, "id", "");
                string path = ReadString(level, "path", "");
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (!hasFirstPath)
                {
                    firstPath = path;
                    hasFirstPath = true;
                }

                if (id == defaultLevel)
                {
                    defaultPath = path;
                    hasDefaultPath = true;
                }

                int stationNumber = ReadInt(level, "station", levelIndex + 1);
                if (stationNumber == requestedStation)
                {
                    return path;
                }

                levelIndex += 1;
            }

            return hasDefaultPath ? defaultPath : firstPath;
        }

        return fallbackPath;
    }

    private static LevelConfig LoadFromPath(string path)
    {
        LevelConfig config = new();
        AddDefaultEntries(config);

        if (!FileAccess.FileExists(path))
        {
            GD.PushWarning($"Missing level config: {path}. Using built-in defaults.");
            return config;
        }

        string json = FileAccess.GetFileAsString(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            GD.PushWarning($"Empty level config: {path}. Using built-in defaults.");
            return config;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        config.Id = ReadString(root, "id", config.Id);
        config.Name = ReadString(root, "name", config.Name);
        config.Duration = ReadFloat(root, "duration", config.Duration);
        config.BossStartTime = ReadFloat(root, "boss_start_time", Math.Min(config.Duration, config.BossStartTime));
        config.HeroLineY = ReadFloat(root, "hero_line_y", config.HeroLineY);
        config.TrainLineY = ReadFloat(root, "train_line_y", config.TrainLineY);
        config.PlayerMinX = ReadFloat(root, "player_min_x", config.PlayerMinX);
        config.PlayerMaxX = ReadFloat(root, "player_max_x", config.PlayerMaxX);
        config.PlayerStartX = ReadFloat(root, "player_start_x", config.PlayerStartX);
        config.HeroBlockRadius = ReadFloat(root, "hero_block_radius", config.HeroBlockRadius);

        config.ResetDefaultTrackAliases();

        if (TryGet(root, "lanes", out JsonElement lanes) && lanes.ValueKind == JsonValueKind.Array)
        {
            config.Lanes.Clear();
            foreach (JsonElement lane in lanes.EnumerateArray())
            {
                if (lane.TryGetSingle(out float laneX))
                {
                    config.Lanes.Add(laneX);
                }
            }

            config.ResetDefaultTrackAliases();
        }

        if (TryGet(root, "tracks", out JsonElement tracks) && tracks.ValueKind == JsonValueKind.Object)
        {
            config.Lanes.Clear();
            config.TrackLaneIndexes.Clear();
            int trackIndex = 0;
            foreach (JsonProperty track in tracks.EnumerateObject())
            {
                if (track.Value.TryGetSingle(out float trackX))
                {
                    config.Lanes.Add(trackX);
                    config.RegisterTrackAlias(track.Name, trackIndex);
                    trackIndex += 1;
                }
            }
        }

        if (TryGet(root, "player", out JsonElement player))
        {
            config.Player.Read(player);
        }

        if (TryGet(root, "objects", out JsonElement objects))
        {
            config.Objects.Read(objects);
        }

        if (TryGet(root, "rewards", out JsonElement rewards))
        {
            config.Rewards.Read(rewards);
        }

        if (TryGet(root, "enemy_types", out JsonElement enemyTypes))
        {
            config.EnemyTypes.Clear();
            foreach (JsonProperty property in enemyTypes.EnumerateObject())
            {
                EnemyTypeConfig enemy = EnemyTypeConfig.CreateDefault(property.Name);
                enemy.Read(property.Value);
                config.EnemyTypes[property.Name] = enemy;
            }
        }

        if (TryGet(root, "pickups", out JsonElement pickups))
        {
            config.Pickups.Clear();
            foreach (JsonProperty property in pickups.EnumerateObject())
            {
                PickupConfig pickup = PickupConfig.CreateDefault(property.Name);
                pickup.Read(property.Value);
                config.Pickups[property.Name] = pickup;
            }
        }

        if (TryGet(root, "pickup_pairs", out JsonElement pickupPairs))
        {
            config.PickupPairs.Read(pickupPairs);
        }

        if (TryGet(root, "timeline", out JsonElement timeline) && timeline.ValueKind == JsonValueKind.Array)
        {
            config.Timeline.Clear();
            foreach (JsonElement item in timeline.EnumerateArray())
            {
                config.Timeline.Add(TimelineEventConfig.FromJson(item));
            }
            config.Timeline.Sort((left, right) => left.Time.CompareTo(right.Time));
        }

        if (TryGet(root, "boss", out JsonElement boss))
        {
            config.Boss.Read(boss);
        }

        return config;
    }

    private static void AddDefaultEntries(LevelConfig config)
    {
        config.ResetDefaultTrackAliases();
        config.EnemyTypes["grunt"] = EnemyTypeConfig.CreateDefault("grunt");
        config.Pickups["attack_add"] = PickupConfig.CreateDefault("attack_add");
        config.Timeline.Add(new TimelineEventConfig
        {
            Time = 2.0f,
            Type = "enemy",
            Enemy = "grunt",
            Lane = 0
        });
    }

    private void ResetDefaultTrackAliases()
    {
        TrackLaneIndexes.Clear();
        RegisterTrackAlias("left", 0);
        RegisterTrackAlias("lefttrack", 0);
        RegisterTrackAlias("left_track", 0);
        RegisterTrackAlias("right", 1);
        RegisterTrackAlias("righttrack", 1);
        RegisterTrackAlias("right_track", 1);
    }

    private void RegisterTrackAlias(string trackId, int laneIndex)
    {
        string normalized = NormalizeTrackId(trackId);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            TrackLaneIndexes[normalized] = laneIndex;
        }
    }

    private static string NormalizeTrackId(string trackId)
    {
        return trackId.Trim().Replace("_", "").Replace("-", "").ToLowerInvariant();
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static string ReadString(JsonElement element, string name, string fallback)
    {
        return TryGet(element, name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
    }

    private static int ReadInt(JsonElement element, string name, int fallback)
    {
        return TryGet(element, name, out JsonElement value) && value.TryGetInt32(out int result)
            ? result
            : fallback;
    }

    private static float ReadFloat(JsonElement element, string name, float fallback)
    {
        return TryGet(element, name, out JsonElement value) && value.TryGetSingle(out float result)
            ? result
            : fallback;
    }

    private static Color ReadColor(JsonElement element, string name, Color fallback)
    {
        string hex = ReadString(element, name, "");
        return string.IsNullOrWhiteSpace(hex) ? fallback : ColorFromHex(hex, fallback);
    }

    private static Color ColorFromHex(string hex, Color fallback)
    {
        string clean = hex.Trim().TrimStart('#');
        if (clean.Length != 6 && clean.Length != 8)
        {
            return fallback;
        }

        try
        {
            float r = int.Parse(clean.Substring(0, 2), NumberStyles.HexNumber) / 255.0f;
            float g = int.Parse(clean.Substring(2, 2), NumberStyles.HexNumber) / 255.0f;
            float b = int.Parse(clean.Substring(4, 2), NumberStyles.HexNumber) / 255.0f;
            float a = clean.Length == 8
                ? int.Parse(clean.Substring(6, 2), NumberStyles.HexNumber) / 255.0f
                : 1.0f;
            return new Color(r, g, b, a);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    public sealed class PlayerConfig
    {
        public int HeroDamage { get; private set; } = 8;
        public int TurretDamage { get; private set; } = 5;
        public float HeroFireRate { get; private set; } = 1.0f;
        public int BulletCount { get; private set; } = 1;
        public float MinFireInterval { get; private set; } = 0.16f;
        public float ExplosiveRadius { get; private set; } = 72.0f;

        public void Read(JsonElement element)
        {
            HeroDamage = ReadInt(element, "hero_damage", HeroDamage);
            TurretDamage = ReadInt(element, "turret_damage", TurretDamage);
            HeroFireRate = ReadFloat(element, "hero_fire_rate", HeroFireRate);
            BulletCount = ReadInt(element, "bullet_count", BulletCount);
            MinFireInterval = ReadFloat(element, "min_fire_interval", MinFireInterval);
            ExplosiveRadius = ReadFloat(element, "explosive_radius", ExplosiveRadius);
        }
    }

    public sealed class ObjectConfig
    {
        public float HeroMuzzleY { get; private set; } = 724.0f;
        public float HeroMuzzleOffsetX { get; private set; } = -11.0f;
        public float TurretMuzzleY { get; private set; } = 758.0f;
        public float TurretMuzzleOffsetX { get; private set; } = -5.0f;
        public float TurretBulletSpacing { get; private set; } = 18.0f;
        public float EnemySpawnY { get; private set; } = -56.0f;
        public float EnemySize { get; private set; } = 44.0f;
        public float PickupSpawnX { get; private set; } = 270.0f;
        public float PickupSpawnY { get; private set; } = -64.0f;
        public float PickupSize { get; private set; } = 50.0f;
        public float PickupMissY { get; private set; } = 870.0f;
        public float BulletSpeed { get; private set; } = 620.0f;
        public float BulletWidth { get; private set; } = 10.0f;
        public float BulletHeight { get; private set; } = 20.0f;
        public float BulletDestroyDistanceAboveTop { get; private set; } = 48.0f;
        public float SoliderFollowDistance { get; private set; } = 18.0f;

        public void Read(JsonElement element)
        {
            HeroMuzzleY = ReadFloat(element, "hero_muzzle_y", HeroMuzzleY);
            HeroMuzzleOffsetX = ReadFloat(element, "hero_muzzle_offset_x", HeroMuzzleOffsetX);
            TurretMuzzleY = ReadFloat(element, "turret_muzzle_y", TurretMuzzleY);
            TurretMuzzleOffsetX = ReadFloat(element, "turret_muzzle_offset_x", TurretMuzzleOffsetX);
            TurretBulletSpacing = ReadFloat(element, "turret_bullet_spacing", TurretBulletSpacing);
            EnemySpawnY = ReadFloat(element, "enemy_spawn_y", EnemySpawnY);
            EnemySize = ReadFloat(element, "enemy_size", EnemySize);
            PickupSpawnX = ReadFloat(element, "pickup_spawn_x", PickupSpawnX);
            PickupSpawnY = ReadFloat(element, "pickup_spawn_y", PickupSpawnY);
            PickupSize = ReadFloat(element, "pickup_size", PickupSize);
            PickupMissY = ReadFloat(element, "pickup_miss_y", PickupMissY);
            BulletSpeed = ReadFloat(element, "bullet_speed", BulletSpeed);
            BulletWidth = ReadFloat(element, "bullet_width", BulletWidth);
            BulletHeight = ReadFloat(element, "bullet_height", BulletHeight);
            BulletDestroyDistanceAboveTop = ReadFloat(element, "bullet_destroy_distance_above_top", BulletDestroyDistanceAboveTop);
            SoliderFollowDistance = ReadFloat(element, "solider_follow_distance", SoliderFollowDistance);
        }
    }

    public sealed class RewardConfig
    {
        public int ScrapBase { get; private set; } = 60;
        public int ScrapPerKill { get; private set; }
        public int Fuel { get; private set; } = 10;
        public int Food { get; private set; } = 8;
        public int Parts { get; private set; } = 1;

        public void Read(JsonElement element)
        {
            ScrapBase = ReadInt(element, "scrap_base", ScrapBase);
            ScrapPerKill = ReadInt(element, "scrap_per_kill", ScrapPerKill);
            Fuel = ReadInt(element, "fuel", Fuel);
            Food = ReadInt(element, "food", Food);
            Parts = ReadInt(element, "parts", Parts);
        }
    }

    public sealed class EnemyTypeConfig
    {
        public string Id { get; private set; } = "grunt";
        public string Name { get; private set; } = "Grunt";
        public int Hp { get; private set; } = 38;
        public float Speed { get; private set; } = 92.0f;
        public int Damage { get; private set; } = 12;
        public int TrainDamage { get; private set; } = 6;
        public int Reward { get; private set; } = 2;
        public int SpawnCount { get; private set; } = 1;
        public float GroupSpacing { get; private set; } = 22.0f;
        public float GroupSpread { get; private set; } = 14.0f;
        public string TexturePath { get; private set; } = "";
        public Color Color { get; private set; } = new(0.84f, 0.29f, 0.24f, 1.0f);

        public static EnemyTypeConfig CreateDefault(string id)
        {
            return new EnemyTypeConfig { Id = id };
        }

        public void Read(JsonElement element)
        {
            Name = ReadString(element, "name", Name);
            Hp = ReadInt(element, "hp", Hp);
            Speed = ReadFloat(element, "speed", Speed);
            Damage = ReadInt(element, "damage", Damage);
            TrainDamage = ReadInt(element, "train_damage", TrainDamage);
            Reward = ReadInt(element, "reward", Reward);
            SpawnCount = ReadInt(element, "spawn_count", SpawnCount);
            GroupSpacing = ReadFloat(element, "group_spacing", GroupSpacing);
            GroupSpread = ReadFloat(element, "group_spread", GroupSpread);
            TexturePath = ReadString(element, "texture", ReadString(element, "image", TexturePath));
            Color = ReadColor(element, "color", Color);
        }
    }

    public sealed class PickupConfig
    {
        public string Id { get; private set; } = "attack_add";
        public string Name { get; private set; } = "ATK +5";
        public string Kind { get; private set; } = "attack_add";
        public float Value { get; private set; } = 5.0f;
        public int Hp { get; private set; } = 60;
        public float Speed { get; private set; } = 145.0f;
        public Color Color { get; private set; } = new(1.0f, 0.37f, 0.37f, 1.0f);

        public static PickupConfig CreateDefault(string id)
        {
            return new PickupConfig { Id = id };
        }

        public void Read(JsonElement element)
        {
            Name = ReadString(element, "name", Name);
            Kind = ReadString(element, "kind", Kind);
            Value = ReadFloat(element, "value", Value);
            Hp = ReadInt(element, "hp", Hp);
            Speed = ReadFloat(element, "speed", Speed);
            Color = ReadColor(element, "color", Color);
        }
    }

    public sealed class PickupPairConfig
    {
        public float StartTime { get; private set; } = 6.0f;
        public float Interval { get; private set; } = 10.0f;
        public float EndTime { get; private set; } = 76.0f;
        public int Hp { get; private set; } = 130;
        public float Speed { get; private set; } = 112.0f;
        public Color Color { get; private set; } = new(0.48f, 0.84f, 1.0f, 0.56f);
        public List<PickupPairOptionConfig> Options { get; } = new()
        {
            new("attack_add", "fire_rate"),
            new("coins", "heal"),
            new("bullet_add", "attack_mult"),
            new("turret_mult", "explosive")
        };

        public void Read(JsonElement element)
        {
            StartTime = ReadFloat(element, "start_time", StartTime);
            Interval = ReadFloat(element, "interval", Interval);
            EndTime = ReadFloat(element, "end_time", EndTime);
            Hp = ReadInt(element, "hp", Hp);
            Speed = ReadFloat(element, "speed", Speed);
            Color = ReadColor(element, "color", Color);

            if (TryGet(element, "options", out JsonElement options) && options.ValueKind == JsonValueKind.Array)
            {
                Options.Clear();
                foreach (JsonElement option in options.EnumerateArray())
                {
                    if (PickupPairOptionConfig.TryFromJson(option, out PickupPairOptionConfig pair))
                    {
                        Options.Add(pair);
                    }
                }
            }

            if (Options.Count == 0)
            {
                Options.Add(new PickupPairOptionConfig("attack_add", "fire_rate"));
            }
        }

        public PickupPairOptionConfig GetOption(int index)
        {
            int optionIndex = index % Options.Count;
            if (optionIndex < 0)
            {
                optionIndex += Options.Count;
            }

            return Options[optionIndex];
        }
    }

    public sealed class PickupPairOptionConfig
    {
        public PickupPairOptionConfig(string left, string right)
        {
            Left = left;
            Right = right;
        }

        public string Left { get; }
        public string Right { get; }

        public static bool TryFromJson(JsonElement element, out PickupPairOptionConfig pair)
        {
            pair = new PickupPairOptionConfig("", "");
            if (element.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            List<string> pickupIds = new();
            foreach (JsonElement item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string pickupId = item.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(pickupId))
                    {
                        pickupIds.Add(pickupId);
                    }
                }
            }

            if (pickupIds.Count < 2)
            {
                return false;
            }

            pair = new PickupPairOptionConfig(pickupIds[0], pickupIds[1]);
            return true;
        }
    }

    public sealed class TimelineEventConfig
    {
        public float Time { get; init; }
        public string Type { get; init; } = "enemy";
        public string Track { get; init; } = "";
        public string Enemy { get; init; } = "grunt";
        public string Pickup { get; init; } = "attack_add";
        public int Lane { get; init; }
        public int Count { get; init; }

        public static TimelineEventConfig FromJson(JsonElement element)
        {
            return new TimelineEventConfig
            {
                Time = ReadFloat(element, "time", 0.0f),
                Type = ReadString(element, "type", "enemy"),
                Track = ReadString(element, "track", ""),
                Enemy = ReadString(element, "enemy", "grunt"),
                Pickup = ReadString(element, "pickup", "attack_add"),
                Lane = ReadInt(element, "lane", 0),
                Count = ReadInt(element, "count", 0)
            };
        }
    }

    public sealed class BossConfig
    {
        public string Name { get; private set; } = "Armored Locomotive";
        public List<int> Bars { get; } = new() { 160, 180, 220 };
        public int DamagePerHit { get; private set; } = 5;
        public float SpawnX { get; private set; } = 140.0f;
        public float SpawnY { get; private set; } = 122.0f;
        public float Width { get; private set; } = 260.0f;
        public float Height { get; private set; } = 92.0f;
        public float AttackInterval { get; private set; } = 4.0f;
        public float InitialAttackDelay { get; private set; } = 1.6f;
        public float WarningDelay { get; private set; } = 1.0f;
        public float WarningTopY { get; private set; } = 188.0f;
        public float WarningBottomY { get; private set; } = 820.0f;
        public float WarningWidth { get; private set; } = 110.0f;
        public float DangerRadius { get; private set; } = 58.0f;
        public int AttackDamage { get; private set; } = 18;
        public float PatternStartDelay { get; private set; } = 1.2f;
        public float BetweenAttackDelay { get; private set; } = 1.3f;
        public float ShockwaveSpawnY { get; private set; } = -72.0f;
        public float ShockwaveImpactY { get; private set; } = 845.0f;
        public int StaggeredCount { get; private set; } = 6;
        public float StaggeredSpawnInterval { get; private set; } = 0.55f;
        public int StaggeredHp { get; private set; } = 42;
        public float StaggeredSpeed { get; private set; } = 145.0f;
        public int StaggeredDamage { get; private set; } = 10;
        public float StaggeredWidth { get; private set; } = 96.0f;
        public float StaggeredHeight { get; private set; } = 38.0f;
        public int WideHp { get; private set; } = 180;
        public float WideSpeed { get; private set; } = 190.0f;
        public int WideDamage { get; private set; } = 24;
        public float WideWidth { get; private set; } = 330.0f;
        public float WideHeight { get; private set; } = 56.0f;

        public int MaxHp
        {
            get
            {
                int total = 0;
                foreach (int bar in Bars)
                {
                    total += bar;
                }

                return Math.Max(1, total);
            }
        }

        public void Read(JsonElement element)
        {
            Name = ReadString(element, "name", Name);
            DamagePerHit = ReadInt(element, "boss_damage_per_hit", DamagePerHit);
            SpawnX = ReadFloat(element, "spawn_x", SpawnX);
            SpawnY = ReadFloat(element, "spawn_y", SpawnY);
            Width = ReadFloat(element, "width", Width);
            Height = ReadFloat(element, "height", Height);
            AttackInterval = ReadFloat(element, "attack_interval", AttackInterval);
            InitialAttackDelay = ReadFloat(element, "initial_attack_delay", InitialAttackDelay);
            WarningDelay = ReadFloat(element, "warning_delay", WarningDelay);
            WarningTopY = ReadFloat(element, "warning_top_y", WarningTopY);
            WarningBottomY = ReadFloat(element, "warning_bottom_y", WarningBottomY);
            WarningWidth = ReadFloat(element, "warning_width", WarningWidth);
            DangerRadius = ReadFloat(element, "danger_radius", DangerRadius);
            AttackDamage = ReadInt(element, "attack_damage", AttackDamage);
            PatternStartDelay = ReadFloat(element, "pattern_start_delay", PatternStartDelay);
            BetweenAttackDelay = ReadFloat(element, "between_attack_delay", BetweenAttackDelay);
            ShockwaveSpawnY = ReadFloat(element, "shockwave_spawn_y", ShockwaveSpawnY);
            ShockwaveImpactY = ReadFloat(element, "shockwave_impact_y", ShockwaveImpactY);
            StaggeredCount = ReadInt(element, "staggered_count", StaggeredCount);
            StaggeredSpawnInterval = ReadFloat(element, "staggered_spawn_interval", StaggeredSpawnInterval);
            StaggeredHp = ReadInt(element, "staggered_hp", StaggeredHp);
            StaggeredSpeed = ReadFloat(element, "staggered_speed", StaggeredSpeed);
            StaggeredDamage = ReadInt(element, "staggered_damage", StaggeredDamage);
            StaggeredWidth = ReadFloat(element, "staggered_width", StaggeredWidth);
            StaggeredHeight = ReadFloat(element, "staggered_height", StaggeredHeight);
            WideHp = ReadInt(element, "wide_hp", WideHp);
            WideSpeed = ReadFloat(element, "wide_speed", WideSpeed);
            WideDamage = ReadInt(element, "wide_damage", WideDamage);
            WideWidth = ReadFloat(element, "wide_width", WideWidth);
            WideHeight = ReadFloat(element, "wide_height", WideHeight);

            if (TryGet(element, "bars", out JsonElement bars) && bars.ValueKind == JsonValueKind.Array)
            {
                Bars.Clear();
                foreach (JsonElement bar in bars.EnumerateArray())
                {
                    if (bar.TryGetInt32(out int hp) && hp > 0)
                    {
                        Bars.Add(hp);
                    }
                }
            }
        }
    }
}
