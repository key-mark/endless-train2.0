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
    public PlayerConfig Player { get; } = new();
    public ObjectConfig Objects { get; } = new();
    public RewardConfig Rewards { get; } = new();
    public BossConfig Boss { get; } = new();
    public Dictionary<string, EnemyTypeConfig> EnemyTypes { get; } = new();
    public Dictionary<string, PickupConfig> Pickups { get; } = new();
    public List<TimelineEventConfig> Timeline { get; } = new();

    public static LevelConfig LoadSelected()
    {
        string levelPath = ReadSelectedLevelPath();
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

        if (TryGet(root, "levels", out JsonElement levels) && levels.ValueKind == JsonValueKind.Array)
        {
            string firstPath = fallbackPath;
            foreach (JsonElement level in levels.EnumerateArray())
            {
                string id = ReadString(level, "id", "");
                string path = ReadString(level, "path", "");
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (firstPath == fallbackPath)
                {
                    firstPath = path;
                }

                if (id == defaultLevel)
                {
                    return path;
                }
            }

            return firstPath;
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
        public float TurretMuzzleY { get; private set; } = 758.0f;
        public float EnemySpawnY { get; private set; } = -56.0f;
        public float EnemySize { get; private set; } = 44.0f;
        public float PickupSpawnY { get; private set; } = -64.0f;
        public float PickupSize { get; private set; } = 50.0f;
        public float PickupMissY { get; private set; } = 870.0f;
        public float BulletSpeed { get; private set; } = 620.0f;
        public float BulletWidth { get; private set; } = 10.0f;
        public float BulletHeight { get; private set; } = 20.0f;

        public void Read(JsonElement element)
        {
            HeroMuzzleY = ReadFloat(element, "hero_muzzle_y", HeroMuzzleY);
            TurretMuzzleY = ReadFloat(element, "turret_muzzle_y", TurretMuzzleY);
            EnemySpawnY = ReadFloat(element, "enemy_spawn_y", EnemySpawnY);
            EnemySize = ReadFloat(element, "enemy_size", EnemySize);
            PickupSpawnY = ReadFloat(element, "pickup_spawn_y", PickupSpawnY);
            PickupSize = ReadFloat(element, "pickup_size", PickupSize);
            PickupMissY = ReadFloat(element, "pickup_miss_y", PickupMissY);
            BulletSpeed = ReadFloat(element, "bullet_speed", BulletSpeed);
            BulletWidth = ReadFloat(element, "bullet_width", BulletWidth);
            BulletHeight = ReadFloat(element, "bullet_height", BulletHeight);
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

    public sealed class TimelineEventConfig
    {
        public float Time { get; init; }
        public string Type { get; init; } = "enemy";
        public string Enemy { get; init; } = "grunt";
        public string Pickup { get; init; } = "attack_add";
        public int Lane { get; init; }

        public static TimelineEventConfig FromJson(JsonElement element)
        {
            return new TimelineEventConfig
            {
                Time = ReadFloat(element, "time", 0.0f),
                Type = ReadString(element, "type", "enemy"),
                Enemy = ReadString(element, "enemy", "grunt"),
                Pickup = ReadString(element, "pickup", "attack_add"),
                Lane = ReadInt(element, "lane", 0)
            };
        }
    }

    public sealed class BossConfig
    {
        public string Name { get; private set; } = "Armored Locomotive";
        public List<int> Bars { get; } = new() { 160, 180, 220 };
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
