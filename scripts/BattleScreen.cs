using Godot;
using System.Collections.Generic;

public partial class BattleScreen : Control
{
    private const string HeroScenePath = "res://scenes/objects/Hero.tscn";
    private const string TurretScenePath = "res://scenes/objects/Turret.tscn";
    private const string BulletScenePath = "res://scenes/objects/Bullet.tscn";
    private const string EnemyScenePath = "res://scenes/objects/Enemy.tscn";
    private const string PickupsScenePath = "res://scenes/objects/Pickups.tscn";

    private LevelConfig _level = null!;
    private Control _battleArea = null!;
    private Control _playerRig = null!;
    private ColorRect _trainBase = null!;
    private PackedScene _heroScene = null!;
    private PackedScene _turretScene = null!;
    private PackedScene _bulletScene = null!;
    private PackedScene _enemyScene = null!;
    private PackedScene _pickupsScene = null!;
    private Label _weaponStatsLabel = null!;
    private Label _combatStatusLabel = null!;
    private Label _temporaryBuffLabel = null!;
    private Label _waveStatusLabel = null!;
    private bool _isDragging;
    private float _shootTimer;
    private float _battleTime;
    private float _bossAttackTimer;
    private float _bossWarningTimer;
    private int _killCount;
    private int _nextWaveEventIndex;
    private int _nextBossLaneIndex;
    private int _pendingBossLaneIndex = -1;
    private int _temporaryAttackAdd;
    private int _temporaryBulletAdd;
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
    private bool _rewardApplied;
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

    private BattlePhase _phase = BattlePhase.Wave;

    private sealed class WaveSpawnEvent
    {
        public WaveSpawnEvent(float time, WaveSpawnKind kind, int laneIndex, string enemyType, string pickupType)
        {
            Time = time;
            Kind = kind;
            LaneIndex = laneIndex;
            EnemyType = enemyType;
            PickupType = pickupType;
        }

        public float Time { get; }
        public WaveSpawnKind Kind { get; }
        public int LaneIndex { get; }
        public string EnemyType { get; }
        public string PickupType { get; }
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

    public override void _Ready()
    {
        _level = LevelConfig.LoadSelected();
        _battleArea = GetNode<Control>("BattleArea540x960");
        _playerRig = GetNode<Control>("BattleArea540x960/PlayerRig");
        _trainBase = GetNode<ColorRect>("BattleArea540x960/TrainBasePlaceholder");
        _waveStatusLabel = GetNode<Label>("BattleArea540x960/BattleAreaLabel");
        _weaponStatsLabel = GetNode<Label>("BattleArea540x960/WeaponStatsLabel");
        _combatStatusLabel = GetNode<Label>("BattleArea540x960/CombatStatusLabel");
        _temporaryBuffLabel = GetNode<Label>("BattleArea540x960/TemporaryBuffLabel");
        _trainBaseStartPosition = _trainBase.Position;
        _trainBaseStartColor = _trainBase.Color;

        LoadObjectPrefabs();
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
        }
        else if (_phase == BattlePhase.Boss)
        {
            RunBossAttack((float)delta);
        }

        CheckEnemyLineDamage();
        RemoveMissedPickups();
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
    }

    private void SpawnPlayerPrefabs()
    {
        _playerRig.AddChild(_heroScene.Instantiate<Control>());
        _playerRig.AddChild(_turretScene.Instantiate<Control>());
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
        SpawnBullet(new Vector2(playerX - 11.0f, _level.Objects.HeroMuzzleY), GetHeroDamage());

        int turretShotCount = Mathf.Max(1, _level.Player.BulletCount + _temporaryBulletAdd);
        for (int i = 0; i < turretShotCount; i += 1)
        {
            float offset = GetTurretShotOffset(i, turretShotCount);
            SpawnBullet(new Vector2(playerX + offset, _level.Objects.TurretMuzzleY + (i % 2 == 0 ? 0.0f : 4.0f)), GetTurretDamage());
        }
    }

    private static float GetTurretShotOffset(int index, int count)
    {
        if (count <= 1)
        {
            return -5.0f;
        }

        return -5.0f + (index - (count - 1) * 0.5f) * 18.0f;
    }

    private void SpawnBullet(Vector2 position, int damage)
    {
        Bullet bullet = _bulletScene.Instantiate<Bullet>();
        bullet.Position = position;
        bullet.Size = new Vector2(_level.Objects.BulletWidth, _level.Objects.BulletHeight);
        bullet.Color = new Color(1.0f, 0.94f, 0.35f, 1.0f);
        bullet.Speed = _level.Objects.BulletSpeed;
        bullet.Damage = damage;
        bullet.ExplosionRadius = _temporaryExplosiveRadius;

        _battleArea.AddChild(bullet);
    }

    private void SpawnEnemy(int laneIndex, string enemyTypeId)
    {
        float laneX = GetLaneX(laneIndex);
        LevelConfig.EnemyTypeConfig enemyConfig = _level.GetEnemyType(enemyTypeId);

        Enemy enemy = _enemyScene.Instantiate<Enemy>();
        enemy.Position = new Vector2(laneX - _level.Objects.EnemySize * 0.5f, _level.Objects.EnemySpawnY);
        enemy.Size = new Vector2(_level.Objects.EnemySize, _level.Objects.EnemySize);
        enemy.Color = enemyConfig.Color;
        enemy.MaxHp = enemyConfig.Hp;
        enemy.Speed = enemyConfig.Speed;
        enemy.HeroLineDamage = enemyConfig.Damage;
        enemy.TrainLineDamage = enemyConfig.TrainDamage;
        enemy.ScrapReward = enemyConfig.Reward;
        enemy.Died += position => OnEnemyDied(position, enemy.ScrapReward);

        _battleArea.AddChild(enemy);
    }

    private void SpawnPickup(int laneIndex, string pickupId)
    {
        float laneX = GetLaneX(laneIndex);
        LevelConfig.PickupConfig pickupConfig = _level.GetPickup(pickupId);

        Pickup pickup = _pickupsScene.Instantiate<Pickup>();
        pickup.Position = new Vector2(laneX - _level.Objects.PickupSize * 0.5f, _level.Objects.PickupSpawnY);
        pickup.Size = new Vector2(_level.Objects.PickupSize, _level.Objects.PickupSize);
        pickup.Color = pickupConfig.Color;
        pickup.MaxHp = pickupConfig.Hp;
        pickup.Speed = pickupConfig.Speed;
        pickup.UpgradeType = pickupId;
        pickup.DisplayName = pickupConfig.Name;
        pickup.Destroyed += OnPickupDestroyed;

        _battleArea.AddChild(pickup);
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
                _temporaryTurretMultiplier *= Mathf.Max(1.0f, pickup.Value);
                feedbackText = $"TURRET x{_temporaryTurretMultiplier:0.0}";
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
        _battleTime = 0.0f;
        _waveCompleted = false;

        foreach (LevelConfig.TimelineEventConfig item in _level.Timeline)
        {
            WaveSpawnKind kind = item.Type == "pickup" ? WaveSpawnKind.Pickup : WaveSpawnKind.Enemy;
            _waveTimeline.Add(new WaveSpawnEvent(item.Time, kind, item.Lane, item.Enemy, item.Pickup));
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
                SpawnEnemy(spawnEvent.LaneIndex, spawnEvent.EnemyType);
            }
            else
            {
                SpawnPickup(spawnEvent.LaneIndex, spawnEvent.PickupType);
            }

            _nextWaveEventIndex += 1;
        }

        if (!_waveCompleted && _battleTime >= _level.BossStartTime)
        {
            _waveCompleted = true;
            StartBossPhase();
        }
    }

    private void StartBossPhase()
    {
        if (_bossSpawned || _battleEnded)
        {
            return;
        }

        ClearRemainingWaveObjects();
        SpawnBoss();
        _phase = BattlePhase.Boss;
        _bossSpawned = true;
        _bossAttackTimer = _level.Boss.InitialAttackDelay;
        ShowBanner("BOSS INCOMING", new Color(1.0f, 0.56f, 0.26f, 1.0f));
        RefreshWaveStatus();
    }

    private void ClearRemainingWaveObjects()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Enemy or Pickup)
            {
                child.QueueFree();
            }
        }
    }

    private void SpawnBoss()
    {
        _boss = new Boss
        {
            Position = new Vector2(_level.Boss.SpawnX, _level.Boss.SpawnY),
            Size = new Vector2(_level.Boss.Width, _level.Boss.Height),
            Color = new Color(0.42f, 0.28f, 0.2f, 1.0f),
            MaxHp = _level.Boss.MaxHp,
            DisplayName = _level.Boss.Name
        };
        _boss.Defeated += OnBossDefeated;
        _boss.PhaseChanged += OnBossPhaseChanged;
        _battleArea.AddChild(_boss);
    }

    private void RunBossAttack(float delta)
    {
        if (_pendingBossLaneIndex >= 0)
        {
            _bossWarningTimer -= delta;
            if (_bossWarningTimer <= 0.0f)
            {
                ResolveBossLaneAttack();
            }

            return;
        }

        _bossAttackTimer -= delta;
        if (_bossAttackTimer <= 0.0f)
        {
            StartBossLaneWarning(_nextBossLaneIndex);
            _nextBossLaneIndex += 1;
            _bossAttackTimer = _level.Boss.AttackInterval;
        }
    }

    private void StartBossLaneWarning(int laneIndex)
    {
        _pendingBossLaneIndex = laneIndex;
        _bossWarningTimer = _level.Boss.WarningDelay;

        float laneX = GetLaneX(laneIndex);
        _bossWarningRect?.QueueFree();
        _bossWarningRect = new ColorRect
        {
            Position = new Vector2(laneX - _level.Boss.WarningWidth * 0.5f, _level.Boss.WarningTopY),
            Size = new Vector2(_level.Boss.WarningWidth, _level.Boss.WarningBottomY - _level.Boss.WarningTopY),
            Color = new Color(1.0f, 0.16f, 0.08f, 0.38f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        _battleArea.AddChild(_bossWarningRect);
    }

    private void ResolveBossLaneAttack()
    {
        float dangerX = GetLaneX(_pendingBossLaneIndex);
        _bossWarningRect?.QueueFree();
        _bossWarningRect = null;

        if (Mathf.Abs(_playerRig.Position.X - dangerX) <= _level.Boss.DangerRadius)
        {
            DamageTrainWithFeedback(_level.Boss.AttackDamage, new Vector2(dangerX - 58.0f, 704.0f), "Boss Strike");
            CheckFailure();
        }

        _pendingBossLaneIndex = -1;
    }

    private void OnBossDefeated()
    {
        ShowBanner("BOSS DEFEATED", new Color(0.62f, 1.0f, 0.7f, 1.0f));
        ShowResultPanel(true);
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

        _resultPanel = new ColorRect
        {
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
        }

        ClearTemporaryBuffs();
        GetTree().ChangeSceneToFile("res://scenes/TrainScreen.tscn");
    }

    private void ClearCombatObjects()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Enemy or Pickup or Bullet or Boss)
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
