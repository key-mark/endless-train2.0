using Godot;
using System.Collections.Generic;

public partial class BattleScreen : Control
{
    private const string HeroScenePath = "res://scenes/objects/Hero.tscn";
    private const string TurretScenePath = "res://scenes/objects/Turret.tscn";
    private const string BulletScenePath = "res://scenes/objects/Bullet.tscn";
    private const string EnemyScenePath = "res://scenes/objects/Enemy.tscn";
    private const string PickupsScenePath = "res://scenes/objects/Pickups.tscn";
    private const string BossScenePath = "res://scenes/objects/Boss.tscn";
    private const string SoliderScenePath = "res://scenes/objects/Solider.tscn";
    private const string ShockwaveSmallScenePath = "res://scenes/objects/wave_small.tscn";
    private const string ShockwaveBigScenePath = "res://scenes/objects/wave_big.tscn";
    private const string TrainScreenScenePath = "res://scenes/TrainScreen.tscn";
    private const string EndMenuScenePath = "res://scenes/Endmenu.tscn";
    private static readonly string[] TurretLevelTexturePaths =
    {
        "res://resource/figure/gun/gun_lv1.png",
        "res://resource/figure/gun/gun_lv2.png",
        "res://resource/figure/gun/gun_lv3.png",
        "res://resource/figure/gun/gun_lv4.png"
    };

    private LevelConfig _level = null!;
    private Control _battleArea = null!;
    private Control _playerRig = null!;
    private ColorRect _trainBase = null!;
    private PackedScene _heroScene = null!;
    private PackedScene _turretScene = null!;
    private PackedScene _bulletScene = null!;
    private PackedScene _enemyScene = null!;
    private PackedScene _pickupsScene = null!;
    private PackedScene _bossScene = null!;
    private PackedScene _soliderScene = null!;
    private PackedScene _shockwaveSmallScene = null!;
    private PackedScene _shockwaveBigScene = null!;
    private readonly Texture2D[] _turretLevelTextures = new Texture2D[TurretLevelTexturePaths.Length];
    private Sprite2D _turretSprite = null!;
    private Label _weaponStatsLabel = null!;
    private Label _combatStatusLabel = null!;
    private Label _temporaryBuffLabel = null!;
    private Label _waveStatusLabel = null!;
    private bool _isDragging;
    private float _shootTimer;
    private float _battleTime;
    private int _killCount;
    private int _nextWaveEventIndex;
    private int _nextPickupPairIndex;
    private int _temporaryAttackAdd;
    private int _temporaryBulletAdd;
    private int _soliderCount;
    private int _turretVisualLevel = 1;
    private int _bonusScrapReward;
    private int _enemyScrapReward;
    private float _temporaryFireRateMultiplier = 1.0f;
    private float _temporaryAttackMultiplier = 1.0f;
    private float _temporaryTurretMultiplier = 1.0f;
    private float _temporaryExplosiveRadius;
    private bool _waveCompleted;
    private bool _bossSpawned;
    private bool _battleEnded;
    private bool _battleWon;
    private bool _bossDefeatHandled;
    private bool _bossAttackInProgress;
    private bool _rewardApplied;
    private int _activeBossShockwaves;
    private int _staggeredShockwavesRemaining;
    private int _nextStaggeredLaneIndex;
    private float _bossAttackCooldownTimer;
    private float _staggeredSpawnTimer;
    private float _nextPickupPairTime;
    private int _scrapReward;
    private int _fuelReward;
    private int _foodReward;
    private int _partsReward;
    private Boss _boss = null!;
    private ColorRect _bossWarningRect = null!;
    private ColorRect _resultPanel = null!;
    private Vector2 _trainBaseStartPosition;
    private Color _trainBaseStartColor;
    private float _trainHitFeedbackTimer;
    private readonly List<WaveSpawnEvent> _waveTimeline = new();
    private readonly List<TimedFeedback> _feedbacks = new();
    private readonly Dictionary<string, TrackLine> _trackLines = new();
    private static readonly Vector2[] SoliderFollowerSlots =
    {
        new(-1.0f, -0.11f),
        new(1.0f, -0.11f),
        new(-0.67f, -0.93f),
        new(0.67f, -0.93f),
        new(-0.67f, 0.82f),
        new(0.67f, 0.82f),
        new(-1.44f, 0.33f),
        new(1.44f, 0.33f)
    };

    private enum WaveSpawnKind
    {
        Enemy,
        Pickup
    }

    private enum BattlePhase
    {
        Wave,
        Boss,
        Ended
    }

    private enum BossAttackKind
    {
        StaggeredShockwaves,
        WideShockwave
    }

    private BattlePhase _phase = BattlePhase.Wave;
    private BossAttackKind _nextBossAttack = BossAttackKind.StaggeredShockwaves;
    private BossAttackKind _currentBossAttack = BossAttackKind.StaggeredShockwaves;

    private sealed class WaveSpawnEvent
    {
        public WaveSpawnEvent(float time, WaveSpawnKind kind, int laneIndex, string trackId, string enemyType, string pickupType, int count)
        {
            Time = time;
            Kind = kind;
            LaneIndex = laneIndex;
            TrackId = trackId;
            EnemyType = enemyType;
            PickupType = pickupType;
            Count = count;
        }

        public float Time { get; }
        public WaveSpawnKind Kind { get; }
        public int LaneIndex { get; }
        public string TrackId { get; }
        public string EnemyType { get; }
        public string PickupType { get; }
        public int Count { get; }
    }

    private sealed class TimedFeedback
    {
        public Control Node { get; init; } = null!;
        public float Duration { get; init; }
        public float Age { get; set; }
        public Vector2 StartPosition { get; init; }
        public Vector2 EndPosition { get; init; }
        public Vector2 StartScale { get; init; } = Vector2.One;
        public Vector2 EndScale { get; init; } = Vector2.One;
        public Color StartModulate { get; init; } = Colors.White;
    }

    private sealed class TrackLine
    {
        public TrackLine(Vector2 visualStart, Vector2 visualEnd)
        {
            VisualStart = visualStart;
            VisualEnd = visualEnd;
        }

        public Vector2 VisualStart { get; }
        public Vector2 VisualEnd { get; }
        public Vector2 Direction
        {
            get
            {
                Vector2 direction = VisualEnd - VisualStart;
                return direction.LengthSquared() > 0.01f ? direction.Normalized() : Vector2.Down;
            }
        }

        public Vector2 Normal => new(-Direction.Y, Direction.X);

        public Vector2 PointAtY(float y)
        {
            float deltaY = VisualEnd.Y - VisualStart.Y;
            if (Mathf.Abs(deltaY) <= 0.01f)
            {
                return new Vector2(VisualStart.X, y);
            }

            float ratio = (y - VisualStart.Y) / deltaY;
            return VisualStart.Lerp(VisualEnd, ratio);
        }
    }

    public override void _Ready()
    {
        _level = LevelConfig.LoadForStation(State.CurrentStation);
        _battleArea = GetNode<Control>("BattleArea540x960");
        _playerRig = GetNode<Control>("BattleArea540x960/PlayerRig");
        _trainBase = GetNode<ColorRect>("BattleArea540x960/TrainBasePlaceholder");
        _waveStatusLabel = GetNodeOrNull<Label>("BattleArea540x960/BattleAreaLabel") ?? CreateWaveStatusLabel();
        _weaponStatsLabel = GetNode<Label>("BattleArea540x960/WeaponStatsLabel");
        _combatStatusLabel = GetNode<Label>("BattleArea540x960/CombatStatusLabel");
        _temporaryBuffLabel = GetNode<Label>("BattleArea540x960/TemporaryBuffLabel");
        _trainBaseStartPosition = _trainBase.Position;
        _trainBaseStartColor = _trainBase.Color;

        LoadObjectPrefabs();
        BuildSceneTrackLines();
        SpawnPlayerPrefabs();
        ClearTemporaryBuffs();
        BuildWaveTimeline();
        SetPlayerX(_level.PlayerStartX);
        RefreshWeaponStats();
        RefreshCombatStatus();
        RefreshTemporaryBuffs();
        RefreshWaveStatus();
        _shootTimer = GetFireInterval();
    }

    private Label CreateWaveStatusLabel()
    {
        Label label = new()
        {
            Name = "BattleAreaLabel",
            ZIndex = 3,
            Position = new Vector2(150.0f, 8.0f),
            Size = new Vector2(240.0f, 28.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 18);
        _battleArea.AddChild(label);
        return label;
    }

    public override void _Process(double delta)
    {
        if (_battleEnded)
        {
            return;
        }

        _shootTimer -= (float)delta;
        if (_shootTimer <= 0.0f)
        {
            FireVolley();
            _shootTimer += GetFireInterval();
        }

        _battleTime += (float)delta;
        if (_phase == BattlePhase.Wave)
        {
            RunWaveTimeline();
            RunPickupPairSchedule();
        }
        else if (_phase == BattlePhase.Boss)
        {
            RunBossShockwaveAttacks((float)delta);
        }

        CheckEnemyLineDamage();
        RemoveMissedPickups();
        CheckBossDefeatedFallback();
        CheckFailure();
        ProcessFeedbacks((float)delta);
        ProcessTrainHitFeedback((float)delta);
        RefreshWeaponStats();
        RefreshCombatStatus();
        RefreshTemporaryBuffs();
        RefreshWaveStatus();
    }

    public override void _ExitTree()
    {
        ClearTemporaryBuffs();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _isDragging = mouseButton.Pressed;
            if (_isDragging)
            {
                SetPlayerX(mouseButton.Position.X);
            }
        }
        else if (inputEvent is InputEventMouseMotion mouseMotion && _isDragging)
        {
            SetPlayerX(mouseMotion.Position.X);
        }
        else if (inputEvent is InputEventScreenTouch touch)
        {
            _isDragging = touch.Pressed;
            if (_isDragging)
            {
                SetPlayerX(touch.Position.X);
            }
        }
        else if (inputEvent is InputEventScreenDrag drag)
        {
            SetPlayerX(drag.Position.X);
        }
    }

    private GameManager State => GetNode<GameManager>("/root/GameManager");

    private void SetPlayerX(float rawX)
    {
        float clampedX = Mathf.Clamp(rawX, _level.PlayerMinX, _level.PlayerMaxX);
        _playerRig.Position = new Vector2(clampedX, 0.0f);
    }

    private void LoadObjectPrefabs()
    {
        _heroScene = GD.Load<PackedScene>(HeroScenePath);
        _turretScene = GD.Load<PackedScene>(TurretScenePath);
        _bulletScene = GD.Load<PackedScene>(BulletScenePath);
        _enemyScene = GD.Load<PackedScene>(EnemyScenePath);
        _pickupsScene = GD.Load<PackedScene>(PickupsScenePath);
        _bossScene = GD.Load<PackedScene>(BossScenePath);
        _soliderScene = GD.Load<PackedScene>(SoliderScenePath);
        _shockwaveSmallScene = GD.Load<PackedScene>(ShockwaveSmallScenePath);
        _shockwaveBigScene = GD.Load<PackedScene>(ShockwaveBigScenePath);
        for (int i = 0; i < TurretLevelTexturePaths.Length; i += 1)
        {
            _turretLevelTextures[i] = GD.Load<Texture2D>(TurretLevelTexturePaths[i]);
        }
    }

    private static T InstantiateTypedOrFallback<T>(PackedScene scene, string scenePath) where T : Node, new()
    {
        Node node = scene.Instantiate();
        if (node is T typedNode)
        {
            return typedNode;
        }

        GD.PushWarning($"{scenePath} root script is not loaded as {typeof(T).Name}. Using fallback instance.");
        node.QueueFree();
        return new T();
    }

    private void SpawnPlayerPrefabs()
    {
        _playerRig.AddChild(_heroScene.Instantiate<Control>());
        Control turret = _turretScene.Instantiate<Control>();
        _turretSprite = turret.GetNodeOrNull<Sprite2D>("Sprite2D");
        ApplyTurretVisualLevel(1);
        _playerRig.AddChild(turret);
    }

    private int UpgradeTurretVisual()
    {
        ApplyTurretVisualLevel(_turretVisualLevel + 1);
        return _turretVisualLevel;
    }

    private void ApplyTurretVisualLevel(int level)
    {
        _turretVisualLevel = Mathf.Clamp(level, 1, TurretLevelTexturePaths.Length);
        if (_turretSprite == null)
        {
            GD.PushWarning("Turret Sprite2D not found. Visual upgrade skipped.");
            return;
        }

        Texture2D texture = _turretLevelTextures[_turretVisualLevel - 1];
        if (texture == null)
        {
            GD.PushWarning($"Turret level texture not found: {TurretLevelTexturePaths[_turretVisualLevel - 1]}");
            return;
        }

        _turretSprite.Texture = texture;
    }

    private void BuildSceneTrackLines()
    {
        _trackLines.Clear();
        RegisterTrackLine("left", "LeftTrack", 0);
        RegisterTrackLine("lefttrack", "LeftTrack", 0);
        RegisterTrackLine("right", "RightTrack", 1);
        RegisterTrackLine("righttrack", "RightTrack", 1);
    }

    private void RegisterTrackLine(string trackId, string nodeName, int fallbackLaneIndex)
    {
        TrackLine line = TryCreateTrackLineFromNode(nodeName) ?? CreateVerticalFallbackTrackLine(fallbackLaneIndex);
        _trackLines[NormalizeTrackId(trackId)] = line;
    }

    private TrackLine TryCreateTrackLineFromNode(string nodeName)
    {
        Control track = _battleArea.GetNodeOrNull<Control>(nodeName);
        if (track == null)
        {
            return null;
        }

        Transform2D toBattleArea = _battleArea.GetGlobalTransform().AffineInverse() * track.GetGlobalTransform();
        Vector2 centerTop = toBattleArea * new Vector2(track.Size.X * 0.5f, 0.0f);
        Vector2 centerBottom = toBattleArea * new Vector2(track.Size.X * 0.5f, track.Size.Y);
        return new TrackLine(centerTop, centerBottom);
    }

    private TrackLine CreateVerticalFallbackTrackLine(int laneIndex)
    {
        float laneX = _level.GetLaneX(laneIndex);
        return new TrackLine(new Vector2(laneX, 0.0f), new Vector2(laneX, _level.TrainLineY));
    }

    private TrackLine GetTrackLine(string trackId, int laneIndex)
    {
        string normalized = NormalizeTrackId(trackId);
        if (!string.IsNullOrWhiteSpace(normalized) && _trackLines.TryGetValue(normalized, out TrackLine line))
        {
            return line;
        }

        string fallback = laneIndex % 2 == 0 ? "lefttrack" : "righttrack";
        if (_trackLines.TryGetValue(fallback, out TrackLine fallbackLine))
        {
            return fallbackLine;
        }

        return CreateVerticalFallbackTrackLine(laneIndex);
    }

    private static string NormalizeTrackId(string trackId)
    {
        return trackId.Trim().Replace("_", "").Replace("-", "").ToLowerInvariant();
    }

    private int GetHeroDamage()
    {
        return Mathf.Max(1, Mathf.RoundToInt((_level.Player.HeroDamage + State.GetCannonDamageBonus() + _temporaryAttackAdd) * _temporaryAttackMultiplier));
    }

    private int GetTurretDamage()
    {
        return Mathf.Max(1, Mathf.RoundToInt((_level.Player.TurretDamage + State.GetCannonDamageBonus() + _temporaryAttackAdd) * _temporaryAttackMultiplier * _temporaryTurretMultiplier));
    }

    private float GetFireInterval()
    {
        float configInterval = _level.Player.HeroFireRate > 0.0f ? 1.0f / _level.Player.HeroFireRate : State.GetCannonFireInterval();
        float baseInterval = Mathf.Min(State.GetCannonFireInterval(), configInterval);
        return Mathf.Max(_level.Player.MinFireInterval, baseInterval / _temporaryFireRateMultiplier);
    }

    private void RefreshWeaponStats()
    {
        _weaponStatsLabel.Text =
            $"Hero {GetHeroDamage()}  Turret {GetTurretDamage()}\n" +
            $"Fire {GetFireInterval():0.00}s";
    }

    private void RefreshCombatStatus()
    {
        _combatStatusLabel.Text =
            $"Train HP {State.TrainCurrentHp}/{State.TrainMaxHp}\n" +
            $"Kills {_killCount}\n" +
            $"Station {State.CurrentStation}";
    }

    private void RefreshWaveStatus()
    {
        if (_waveStatusLabel == null)
        {
            return;
        }

        float displayTime = Mathf.Min(_battleTime, _level.BossStartTime);
        string phaseName = GetWavePhaseName(displayTime);
        _waveStatusLabel.Text = _phase == BattlePhase.Boss
            ? "Boss Phase"
            : $"{_level.Id} {phaseName}  {displayTime:0}/{_level.BossStartTime:0}s";
    }

    private string GetWavePhaseName(float time)
    {
        if (_waveCompleted)
        {
            return _phase == BattlePhase.Boss ? "Boss" : "Complete";
        }

        float bossStart = Mathf.Max(1.0f, _level.BossStartTime);
        if (time < bossStart * 0.15f)
        {
            return "Warmup";
        }

        if (time < bossStart * 0.45f)
        {
            return "Pressure";
        }

        if (time < bossStart * 0.75f)
        {
            return "Heavy";
        }

        return "Final Push";
    }

    private void RefreshTemporaryBuffs()
    {
        string explosiveText = _temporaryExplosiveRadius > 0.0f ? "ON" : "OFF";
        _temporaryBuffLabel.Text =
            "Buffs\n" +
            $"ATK +{_temporaryAttackAdd}  DMG x{_temporaryAttackMultiplier:0.0}\n" +
            $"Rate x{_temporaryFireRateMultiplier:0.0}  Bullets +{_temporaryBulletAdd}\n" +
            $"Scrap +{_bonusScrapReward}  EXP {explosiveText}";
    }

    private void FireVolley()
    {
        float playerX = _playerRig.Position.X;
        SpawnBullet(new Vector2(playerX + _level.Objects.HeroMuzzleOffsetX, _level.Objects.HeroMuzzleY), GetHeroDamage());

        int turretShotCount = Mathf.Max(1, _level.Player.BulletCount + _temporaryBulletAdd);
        for (int i = 0; i < turretShotCount; i += 1)
        {
            float offset = GetTurretShotOffset(i, turretShotCount, _level.Objects.TurretMuzzleOffsetX, _level.Objects.TurretBulletSpacing);
            SpawnBullet(new Vector2(playerX + offset, _level.Objects.TurretMuzzleY), GetTurretDamage());
        }
    }

    private static float GetTurretShotOffset(int index, int count, float centerOffset, float spacing)
    {
        if (count <= 1)
        {
            return centerOffset;
        }

        return centerOffset + (index - (count - 1) * 0.5f) * spacing;
    }

    private void SpawnBullet(Vector2 position, int damage)
    {
        Bullet bullet = InstantiateTypedOrFallback<Bullet>(_bulletScene, BulletScenePath);
        bullet.Position = position;
        bullet.Size = new Vector2(_level.Objects.BulletWidth, _level.Objects.BulletHeight);
        bullet.Speed = _level.Objects.BulletSpeed;
        bullet.Damage = damage;
        bullet.ExplosionRadius = _temporaryExplosiveRadius;
        bullet.DestroyDistanceAboveTop = _level.Objects.BulletDestroyDistanceAboveTop;

        _battleArea.AddChild(bullet);
    }

    private void SpawnEnemy(int laneIndex, string trackId, string enemyTypeId, int countOverride)
    {
        LevelConfig.EnemyTypeConfig enemyConfig = _level.GetEnemyType(enemyTypeId);
        TrackLine trackLine = GetTrackLine(trackId, laneIndex);
        int spawnCount = Mathf.Max(1, countOverride > 0 ? countOverride : enemyConfig.SpawnCount);

        for (int i = 0; i < spawnCount; i += 1)
        {
            Vector2 offset = GetEnemyGroupOffset(trackLine, i, enemyConfig.GroupSpacing, enemyConfig.GroupSpread);
            Vector2 centerStart = trackLine.VisualStart + offset;
            Vector2 centerEnd = trackLine.VisualEnd + offset;

            SpawnEnemyInstance(enemyConfig, centerStart, centerEnd);
        }
    }

    private void SpawnEnemyInstance(LevelConfig.EnemyTypeConfig enemyConfig, Vector2 centerStart, Vector2 centerEnd)
    {
        Enemy enemy = InstantiateTypedOrFallback<Enemy>(_enemyScene, EnemyScenePath);
        enemy.Size = new Vector2(_level.Objects.EnemySize, _level.Objects.EnemySize);
        enemy.Color = string.IsNullOrWhiteSpace(enemyConfig.TexturePath)
            ? enemyConfig.Color
            : new Color(1.0f, 1.0f, 1.0f, 0.0f);
        enemy.TexturePath = enemyConfig.TexturePath;
        enemy.MaxHp = enemyConfig.Hp;
        enemy.Speed = enemyConfig.Speed;
        enemy.HeroLineDamage = enemyConfig.Damage;
        enemy.TrainLineDamage = enemyConfig.TrainDamage;
        enemy.ScrapReward = enemyConfig.Reward;
        enemy.SetTrackLine(centerStart, centerEnd);
        enemy.Died += position => OnEnemyDied(position, enemy.ScrapReward);

        _battleArea.AddChild(enemy);
    }

    private static Vector2 GetEnemyGroupOffset(TrackLine trackLine, int index, float spacing, float spread)
    {
        int sideBand = index % 3 - 1;
        float alongOffset = index * spacing;
        float sideOffset = sideBand * spread;
        return trackLine.Direction * alongOffset + trackLine.Normal * sideOffset;
    }

    private void SpawnPickup(string pickupId, int laneIndex)
    {
        LevelConfig.PickupConfig pickupConfig = _level.GetPickup(pickupId);
        TrackLine trackLine = GetTrackLine(string.Empty, laneIndex);
        Vector2 centerStart = trackLine.PointAtY(_level.Objects.PickupSpawnY);
        Vector2 centerEnd = trackLine.PointAtY(_level.TrainLineY);

        Pickup pickup = InstantiateTypedOrFallback<Pickup>(_pickupsScene, PickupsScenePath);
        pickup.Size = new Vector2(_level.Objects.PickupSize, _level.Objects.PickupSize);
        pickup.Color = _level.PickupPairs.Color;
        pickup.MaxHp = _level.PickupPairs.Hp;
        pickup.Speed = _level.PickupPairs.Speed;
        pickup.UpgradeType = pickupId;
        pickup.PickupKind = pickupConfig.Kind;
        pickup.DisplayName = pickupConfig.Name;
        pickup.SetTrackLine(centerStart, centerEnd);
        pickup.Destroyed += OnPickupDestroyed;

        _battleArea.AddChild(pickup);
    }

    private void SpawnSoliderCrew(int count)
    {
        for (int i = 0; i < count; i += 1)
        {
            Control solider = _soliderScene.Instantiate<Control>();
            solider.Position = GetSoliderFollowerPosition(_soliderCount, solider.Size * 0.5f);
            _soliderCount += 1;
            _playerRig.AddChild(solider);
        }
    }

    private Vector2 GetSoliderFollowerPosition(int index, Vector2 soliderHalfSize)
    {
        Vector2 heroCenter = new(0.0f, 784.0f);
        float followDistance = Mathf.Max(0.0f, _level.Objects.SoliderFollowDistance);
        int slot = index % SoliderFollowerSlots.Length;
        int ring = index / SoliderFollowerSlots.Length;
        Vector2 slotDirection = SoliderFollowerSlots[slot];
        Vector2 centerOffset = slotDirection * followDistance;

        if (ring > 0)
        {
            centerOffset += slotDirection.Normalized() * followDistance * 0.5f * ring;
        }

        return heroCenter + centerOffset - soliderHalfSize;
    }

    private float GetLaneX(int laneIndex)
    {
        return _level.GetLaneX(laneIndex);
    }

    private void OnPickupDestroyed(string pickupId, Vector2 position)
    {
        LevelConfig.PickupConfig pickup = _level.GetPickup(pickupId);
        string feedbackText = pickup.Name;
        switch (pickup.Kind)
        {
            case "attack_add":
                int attackAdd = Mathf.RoundToInt(pickup.Value);
                _temporaryAttackAdd += attackAdd;
                feedbackText = $"+{attackAdd} ATK";
                break;
            case "fire_rate":
                _temporaryFireRateMultiplier *= Mathf.Max(1.0f, pickup.Value);
                feedbackText = $"RATE x{_temporaryFireRateMultiplier:0.0}";
                break;
            case "heal":
                int heal = Mathf.RoundToInt(pickup.Value);
                State.HealTrain(heal);
                feedbackText = $"+{heal} TRAIN HP";
                break;
            case "bullet_add":
                int bullets = Mathf.Max(1, Mathf.RoundToInt(pickup.Value));
                _temporaryBulletAdd += bullets;
                SpawnSoliderCrew(bullets);
                feedbackText = $"+{bullets} BULLET";
                break;
            case "coins":
                int scrap = Mathf.RoundToInt(pickup.Value);
                _bonusScrapReward += scrap;
                feedbackText = $"+{scrap} SCRAP";
                break;
            case "attack_mult":
                _temporaryAttackMultiplier *= Mathf.Max(1.0f, pickup.Value);
                feedbackText = $"ATK x{_temporaryAttackMultiplier:0.0}";
                break;
            case "turret_mult":
            case "turrent_mult":
                _temporaryTurretMultiplier *= Mathf.Max(1.0f, pickup.Value);
                int turretLevel = UpgradeTurretVisual();
                feedbackText = $"TURRET Lv{turretLevel} x{_temporaryTurretMultiplier:0.0}";
                break;
            case "explosive":
                _temporaryExplosiveRadius = Mathf.Max(_temporaryExplosiveRadius, _level.Player.ExplosiveRadius * Mathf.Max(1.0f, pickup.Value));
                feedbackText = "EXPLOSIVE";
                break;
        }

        SpawnBurst(position, new Color(0.5f, 1.0f, 0.78f, 0.9f));
        ShowFloatingText(feedbackText, position + new Vector2(-70.0f, -24.0f), new Color(0.72f, 1.0f, 0.86f, 1.0f), 18);
        RefreshWeaponStats();
        RefreshCombatStatus();
        RefreshTemporaryBuffs();
    }

    private void OnEnemyDied(Vector2 position, int scrapReward)
    {
        _killCount += 1;
        _enemyScrapReward += scrapReward;
        SpawnBurst(position, new Color(1.0f, 0.46f, 0.26f, 0.88f));
        ShowFloatingText($"+{scrapReward} Scrap", position + new Vector2(-42.0f, -18.0f), new Color(1.0f, 0.78f, 0.44f, 1.0f), 16);
        RefreshCombatStatus();
    }

    private void CheckEnemyLineDamage()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is not Enemy enemy || enemy.IsQueuedForDeletion())
            {
                continue;
            }

            if (!enemy.CheckedHeroLine && enemy.BottomY() >= _level.HeroLineY)
            {
                enemy.CheckedHeroLine = true;
                if (Mathf.Abs(enemy.CenterX() - _playerRig.Position.X) <= _level.HeroBlockRadius)
                {
                    DamageTrainWithFeedback(enemy.HeroLineDamage, new Vector2(enemy.CenterX() - 56.0f, _level.HeroLineY - 28.0f), "Hero Hit");
                    enemy.QueueFree();
                    CheckFailure();
                    continue;
                }
            }

            if (enemy.BottomY() >= _level.TrainLineY)
            {
                DamageTrainWithFeedback(enemy.TrainLineDamage, new Vector2(enemy.CenterX() - 56.0f, _level.TrainLineY - 34.0f), "Train Hit");
                enemy.QueueFree();
                CheckFailure();
            }
        }
    }

    private void RemoveMissedPickups()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Pickup pickup && !pickup.IsQueuedForDeletion() && pickup.BottomY() >= _level.Objects.PickupMissY)
            {
                pickup.QueueFree();
            }
        }
    }

    private void ClearTemporaryBuffs()
    {
        _temporaryAttackAdd = 0;
        _temporaryBulletAdd = 0;
        _bonusScrapReward = 0;
        _enemyScrapReward = 0;
        _temporaryFireRateMultiplier = 1.0f;
        _temporaryAttackMultiplier = 1.0f;
        _temporaryTurretMultiplier = 1.0f;
        _temporaryExplosiveRadius = 0.0f;
    }

    private void BuildWaveTimeline()
    {
        _waveTimeline.Clear();
        _nextWaveEventIndex = 0;
        _nextPickupPairIndex = 0;
        _nextPickupPairTime = _level.PickupPairs.StartTime;
        _battleTime = 0.0f;
        _waveCompleted = false;

        foreach (LevelConfig.TimelineEventConfig item in _level.Timeline)
        {
            if (item.Type == "pickup")
            {
                continue;
            }

            WaveSpawnKind kind = item.Type == "pickup" ? WaveSpawnKind.Pickup : WaveSpawnKind.Enemy;
            int laneIndex = _level.ResolveLaneIndex(item.Track, item.Lane);
            _waveTimeline.Add(new WaveSpawnEvent(item.Time, kind, laneIndex, item.Track, item.Enemy, item.Pickup, item.Count));
        }

        _waveTimeline.Sort((left, right) => left.Time.CompareTo(right.Time));
    }

    private void RunWaveTimeline()
    {
        while (_nextWaveEventIndex < _waveTimeline.Count && _waveTimeline[_nextWaveEventIndex].Time <= _battleTime)
        {
            WaveSpawnEvent spawnEvent = _waveTimeline[_nextWaveEventIndex];
            if (spawnEvent.Kind == WaveSpawnKind.Enemy)
            {
                SpawnEnemy(spawnEvent.LaneIndex, spawnEvent.TrackId, spawnEvent.EnemyType, spawnEvent.Count);
            }

            _nextWaveEventIndex += 1;
        }

        if (!_waveCompleted && _battleTime >= _level.BossStartTime)
        {
            _waveCompleted = true;
            StartBossPhase();
        }
    }

    private void RunPickupPairSchedule()
    {
        LevelConfig.PickupPairConfig schedule = _level.PickupPairs;
        if (schedule.Interval <= 0.0f || _battleTime < schedule.StartTime || _battleTime > schedule.EndTime)
        {
            return;
        }

        while (_battleTime >= _nextPickupPairTime && _nextPickupPairTime <= schedule.EndTime)
        {
            SpawnPickupPair(_nextPickupPairIndex);
            _nextPickupPairIndex += 1;
            _nextPickupPairTime += schedule.Interval;
        }
    }

    private void SpawnPickupPair(int pairIndex)
    {
        LevelConfig.PickupPairOptionConfig pair = _level.PickupPairs.GetOption(pairIndex);
        SpawnPickup(pair.Left, 0);
        SpawnPickup(pair.Right, 1);
    }

    private void StartBossPhase()
    {
        if (_bossSpawned || _battleEnded)
        {
            return;
        }

        ClearRemainingWaveObjects();
        SpawnBoss();
        ResetBossShockwaveAttacks();
        _phase = BattlePhase.Boss;
        _bossSpawned = true;
        ShowBanner("BOSS INCOMING", new Color(1.0f, 0.56f, 0.26f, 1.0f));
        RefreshWaveStatus();
    }

    private void ClearRemainingWaveObjects()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Enemy or Pickup or Shockwave)
            {
                child.QueueFree();
            }
        }
    }

    private void SpawnBoss()
    {
        _boss = InstantiateTypedOrFallback<Boss>(_bossScene, BossScenePath);
        _boss.Position = new Vector2(_level.Boss.SpawnX, _level.Boss.SpawnY);
        _boss.Size = new Vector2(_level.Boss.Width, _level.Boss.Height);
        _boss.MaxHp = _level.Boss.MaxHp;
        _boss.DamagePerHit = _level.Boss.DamagePerHit;
        _boss.DisplayName = _level.Boss.Name;
        _boss.DefeatedReached += OnBossDefeated;
        _boss.Defeated += OnBossDefeated;
        _boss.PhaseChanged += OnBossPhaseChanged;
        _battleArea.AddChild(_boss);
    }

    private void ResetBossShockwaveAttacks()
    {
        _bossAttackInProgress = false;
        _activeBossShockwaves = 0;
        _staggeredShockwavesRemaining = 0;
        _nextStaggeredLaneIndex = 0;
        _staggeredSpawnTimer = 0.0f;
        _nextBossAttack = BossAttackKind.StaggeredShockwaves;
        _currentBossAttack = BossAttackKind.StaggeredShockwaves;
        _bossAttackCooldownTimer = _level.Boss.PatternStartDelay;
    }

    private void RunBossShockwaveAttacks(float delta)
    {
        if (_bossDefeatHandled || _battleEnded)
        {
            return;
        }

        if (_bossAttackInProgress)
        {
            RunActiveBossShockwaveAttack(delta);
            return;
        }

        _bossAttackCooldownTimer -= delta;
        if (_bossAttackCooldownTimer <= 0.0f)
        {
            StartNextBossShockwaveAttack();
        }
    }

    private void RunActiveBossShockwaveAttack(float delta)
    {
        if (_currentBossAttack == BossAttackKind.StaggeredShockwaves && _staggeredShockwavesRemaining > 0)
        {
            _staggeredSpawnTimer -= delta;
            if (_staggeredSpawnTimer <= 0.0f)
            {
                SpawnNextStaggeredShockwave();
            }
        }

        if (_staggeredShockwavesRemaining <= 0 && _activeBossShockwaves <= 0)
        {
            _bossAttackInProgress = false;
            _bossAttackCooldownTimer = _level.Boss.BetweenAttackDelay;
        }
    }

    private void StartNextBossShockwaveAttack()
    {
        _bossAttackInProgress = true;
        _currentBossAttack = _nextBossAttack;

        if (_currentBossAttack == BossAttackKind.StaggeredShockwaves)
        {
            _staggeredShockwavesRemaining = Mathf.Max(1, _level.Boss.StaggeredCount);
            _nextStaggeredLaneIndex = 0;
            _staggeredSpawnTimer = 0.0f;
            _nextBossAttack = BossAttackKind.WideShockwave;
            ShowBanner("STAGGERED SHOCKWAVES", new Color(1.0f, 0.72f, 0.28f, 1.0f));
            SpawnNextStaggeredShockwave();
        }
        else
        {
            _nextBossAttack = BossAttackKind.StaggeredShockwaves;
            ShowBanner("WIDE SHOCKWAVE", new Color(1.0f, 0.36f, 0.22f, 1.0f));
            SpawnWideShockwave();
        }
    }

    private void SpawnNextStaggeredShockwave()
    {
        int laneIndex = _nextStaggeredLaneIndex % 2;
        _nextStaggeredLaneIndex += 1;
        _staggeredShockwavesRemaining = Mathf.Max(0, _staggeredShockwavesRemaining - 1);
        _staggeredSpawnTimer = Mathf.Max(0.05f, _level.Boss.StaggeredSpawnInterval);
        SpawnShockwave(
            _shockwaveSmallScene,
            ShockwaveSmallScenePath,
            new Vector2(GetLaneX(laneIndex) - _level.Boss.StaggeredWidth * 0.5f, _level.Boss.ShockwaveSpawnY),
            new Vector2(_level.Boss.StaggeredWidth, _level.Boss.StaggeredHeight),
            _level.Boss.StaggeredHp,
            _level.Boss.StaggeredSpeed,
            _level.Boss.StaggeredDamage,
            new Color(1.0f, 0.45f, 0.18f, 0.92f));
    }

    private void SpawnWideShockwave()
    {
        float centerX = (_level.GetLaneX(0) + _level.GetLaneX(1)) * 0.5f;
        SpawnShockwave(
            _shockwaveBigScene,
            ShockwaveBigScenePath,
            new Vector2(centerX - _level.Boss.WideWidth * 0.5f, _level.Boss.ShockwaveSpawnY),
            new Vector2(_level.Boss.WideWidth, _level.Boss.WideHeight),
            _level.Boss.WideHp,
            _level.Boss.WideSpeed,
            _level.Boss.WideDamage,
            new Color(0.94f, 0.18f, 0.14f, 0.95f));
    }

    private void SpawnShockwave(PackedScene scene, string scenePath, Vector2 position, Vector2 size, int hp, float speed, int damage, Color color)
    {
        Shockwave shockwave = InstantiateTypedOrFallback<Shockwave>(scene, scenePath);
        bool usesPrefabSprite = CenterShockwaveSprite(shockwave, size);
        shockwave.ZIndex = 2;
        shockwave.Position = position;
        shockwave.Size = size;
        shockwave.Color = usesPrefabSprite ? new Color(color.R, color.G, color.B, 0.0f) : color;
        shockwave.MaxHp = Mathf.Max(1, hp);
        shockwave.Speed = Mathf.Max(1.0f, speed);
        shockwave.Damage = Mathf.Max(0, damage);
        shockwave.ImpactY = _level.Boss.ShockwaveImpactY;
        shockwave.DisplayName = "冲击波";
        shockwave.Destroyed += OnShockwaveDestroyed;
        shockwave.ReachedTrain += OnShockwaveReachedTrain;
        _activeBossShockwaves += 1;
        _battleArea.AddChild(shockwave);
    }

    private static bool CenterShockwaveSprite(Shockwave shockwave, Vector2 size)
    {
        Sprite2D sprite = shockwave.GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite == null)
        {
            return false;
        }

        sprite.Position = size * 0.5f;
        return true;
    }

    private void OnShockwaveDestroyed(Shockwave shockwave, Vector2 position)
    {
        _activeBossShockwaves = Mathf.Max(0, _activeBossShockwaves - 1);
        SpawnBurst(position, new Color(1.0f, 0.72f, 0.36f, 0.88f));
        ShowFloatingText("冲击波击破", position + new Vector2(-72.0f, -22.0f), new Color(1.0f, 0.86f, 0.44f, 1.0f), 17);
    }

    private void OnShockwaveReachedTrain(Shockwave shockwave)
    {
        _activeBossShockwaves = Mathf.Max(0, _activeBossShockwaves - 1);
        DamageTrainWithFeedback(shockwave.Damage, new Vector2(shockwave.CenterX() - 70.0f, _level.TrainLineY - 44.0f), "Shockwave");
        CheckFailure();
    }

    private void OnBossDefeated()
    {
        if (_bossDefeatHandled || _battleEnded)
        {
            return;
        }

        _bossDefeatHandled = true;
        ShowBanner("BOSS DEFEATED", new Color(0.62f, 1.0f, 0.7f, 1.0f));
        ShowResultPanel(true);
    }

    private void CheckBossDefeatedFallback()
    {
        if (_bossDefeatHandled || _battleEnded || _phase != BattlePhase.Boss || _boss == null)
        {
            return;
        }

        if (GodotObject.IsInstanceValid(_boss) && _boss.Hp <= 0)
        {
            OnBossDefeated();
        }
    }

    private void OnBossPhaseChanged(int phase)
    {
        ShowBanner($"BOSS PHASE {phase}", phase == 2
            ? new Color(1.0f, 0.76f, 0.3f, 1.0f)
            : new Color(1.0f, 0.34f, 0.24f, 1.0f));
    }

    private void CheckFailure()
    {
        if (!_battleEnded && State.TrainCurrentHp <= 0)
        {
            ShowResultPanel(false);
        }
    }

    private void ShowResultPanel(bool victory)
    {
        if (_battleEnded)
        {
            return;
        }

        _battleEnded = true;
        _battleWon = victory;
        CalculateRewards(victory);
        _phase = BattlePhase.Ended;
        _bossWarningRect?.QueueFree();
        _bossWarningRect = null;
        ClearCombatObjects();
        ClearFeedbackObjects();
        ResetTrainHitFeedback();

        if (victory)
        {
            OnReturnToTrainPressed();
            return;
        }

        _resultPanel = new ColorRect
        {
            ZIndex = 50,
            Position = new Vector2(70.0f, 320.0f),
            Size = new Vector2(400.0f, 330.0f),
            Color = victory
                ? new Color(0.12f, 0.34f, 0.22f, 0.96f)
                : new Color(0.34f, 0.12f, 0.12f, 0.96f),
            MouseFilter = MouseFilterEnum.Stop
        };

        Label title = new()
        {
            Position = new Vector2(0.0f, 26.0f),
            Size = new Vector2(_resultPanel.Size.X, 52.0f),
            Text = victory ? "VICTORY REPORT" : "DEFEAT REPORT",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 34);
        _resultPanel.AddChild(title);

        Label summary = new()
        {
            Position = new Vector2(24.0f, 88.0f),
            Size = new Vector2(_resultPanel.Size.X - 48.0f, 150.0f),
            Text = victory
                ? $"Scrap +{_scrapReward}\nFuel +{_fuelReward}\nFood +{_foodReward}\nParts +{_partsReward}\nKills: {_killCount}"
                : $"Scrap +0\nFuel +0\nFood +0\nParts +0\nKills: {_killCount}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        summary.AddThemeFontSizeOverride("font_size", 20);
        _resultPanel.AddChild(summary);

        Button returnButton = new()
        {
            Position = new Vector2(64.0f, 250.0f),
            Size = new Vector2(272.0f, 56.0f),
            Text = "Return to Train"
        };
        returnButton.AddThemeFontSizeOverride("font_size", 22);
        returnButton.Pressed += OnReturnToTrainPressed;
        _resultPanel.AddChild(returnButton);

        _battleArea.AddChild(_resultPanel);
        RefreshWaveStatus();
    }

    private void CalculateRewards(bool victory)
    {
        if (!victory)
        {
            _scrapReward = 0;
            _fuelReward = 0;
            _foodReward = 0;
            _partsReward = 0;
            return;
        }

        _scrapReward = _level.Rewards.ScrapBase + _level.Rewards.ScrapPerKill * _killCount + _enemyScrapReward + _bonusScrapReward;
        _fuelReward = _level.Rewards.Fuel;
        _foodReward = _level.Rewards.Food;
        _partsReward = _level.Rewards.Parts;
    }

    private void OnReturnToTrainPressed()
    {
        if (_battleWon && !_rewardApplied)
        {
            State.CompleteStation(_scrapReward, _fuelReward, _foodReward, _partsReward);
            _rewardApplied = true;
            State.StopGameBgm();
        }
        else if (!_battleWon)
        {
            State.RecoverTrainForNextAttempt();
        }

        ClearTemporaryBuffs();
        GetTree().ChangeSceneToFile(_battleWon ? EndMenuScenePath : TrainScreenScenePath);
    }

    private void ClearCombatObjects()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Enemy or Pickup or Shockwave or Bullet or Boss)
            {
                child.QueueFree();
            }
        }
    }

    private void DamageTrainWithFeedback(int damage, Vector2 textPosition, string reason)
    {
        State.DamageTrain(damage);
        TriggerTrainHitFeedback();
        ShowFloatingText($"-{damage} {reason}", textPosition, new Color(1.0f, 0.36f, 0.28f, 1.0f), 17);
    }

    private void TriggerTrainHitFeedback()
    {
        _trainHitFeedbackTimer = 0.28f;
        _trainBase.Color = new Color(0.82f, 0.24f, 0.18f, 1.0f);
    }

    private void ResetTrainHitFeedback()
    {
        _trainHitFeedbackTimer = 0.0f;
        _trainBase.Position = _trainBaseStartPosition;
        _trainBase.Color = _trainBaseStartColor;
    }

    private void ProcessTrainHitFeedback(float delta)
    {
        if (_trainHitFeedbackTimer <= 0.0f)
        {
            _trainBase.Position = _trainBaseStartPosition;
            _trainBase.Color = _trainBaseStartColor;
            return;
        }

        _trainHitFeedbackTimer -= delta;
        float ratio = Mathf.Clamp(_trainHitFeedbackTimer / 0.28f, 0.0f, 1.0f);
        float shakeX = Mathf.Sin(ratio * 36.0f) * 7.0f * ratio;
        _trainBase.Position = _trainBaseStartPosition + new Vector2(shakeX, 0.0f);
        _trainBase.Color = _trainBaseStartColor.Lerp(new Color(0.82f, 0.24f, 0.18f, 1.0f), ratio);
    }

    private void ShowBanner(string text, Color color)
    {
        ShowFloatingText(text, new Vector2(110.0f, 160.0f), color, 30, 1.15f, new Vector2(110.0f, 126.0f));
    }

    private void ShowFloatingText(string text, Vector2 position, Color color, int fontSize, float duration = 0.85f, Vector2? endPosition = null)
    {
        Label label = new()
        {
            Position = position,
            Size = new Vector2(320.0f, 38.0f),
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = color,
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        _battleArea.AddChild(label);

        _feedbacks.Add(new TimedFeedback
        {
            Node = label,
            Duration = duration,
            StartPosition = position,
            EndPosition = endPosition ?? position + new Vector2(0.0f, -34.0f),
            StartModulate = color
        });
    }

    private void SpawnBurst(Vector2 center, Color color)
    {
        ColorRect burst = new()
        {
            Position = center - new Vector2(14.0f, 14.0f),
            Size = new Vector2(28.0f, 28.0f),
            PivotOffset = new Vector2(14.0f, 14.0f),
            Color = color,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _battleArea.AddChild(burst);

        _feedbacks.Add(new TimedFeedback
        {
            Node = burst,
            Duration = 0.28f,
            StartPosition = burst.Position,
            EndPosition = burst.Position,
            StartScale = Vector2.One,
            EndScale = new Vector2(2.0f, 2.0f),
            StartModulate = color
        });
    }

    private void ProcessFeedbacks(float delta)
    {
        for (int i = _feedbacks.Count - 1; i >= 0; i -= 1)
        {
            TimedFeedback feedback = _feedbacks[i];
            if (feedback.Node.IsQueuedForDeletion())
            {
                _feedbacks.RemoveAt(i);
                continue;
            }

            feedback.Age += delta;
            float ratio = Mathf.Clamp(feedback.Age / feedback.Duration, 0.0f, 1.0f);
            feedback.Node.Position = feedback.StartPosition.Lerp(feedback.EndPosition, ratio);
            feedback.Node.Scale = feedback.StartScale.Lerp(feedback.EndScale, ratio);
            Color modulate = feedback.StartModulate;
            modulate.A = 1.0f - ratio;
            feedback.Node.Modulate = modulate;

            if (ratio >= 1.0f)
            {
                feedback.Node.QueueFree();
                _feedbacks.RemoveAt(i);
            }
        }
    }

    private void ClearFeedbackObjects()
    {
        foreach (TimedFeedback feedback in _feedbacks)
        {
            if (!feedback.Node.IsQueuedForDeletion())
            {
                feedback.Node.QueueFree();
            }
        }

        _feedbacks.Clear();
    }
}
