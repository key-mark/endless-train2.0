using Godot;
using System.Collections.Generic;

public partial class BattleScreen : Control
{
    private const float BattleWidth = 540.0f;
    private const float PlayerSideMargin = 52.0f;
    private const float PlayerMinX = PlayerSideMargin;
    private const float PlayerMaxX = BattleWidth - PlayerSideMargin;
    private const float PlayerStartX = 270.0f;
    private const float HeroMuzzleY = 724.0f;
    private const float TurretMuzzleY = 758.0f;
    private const float LeftLaneX = 175.0f;
    private const float RightLaneX = 365.0f;
    private const float EnemySpawnY = -56.0f;
    private const float EnemySize = 44.0f;
    private const float UpgradeTargetSpawnY = -64.0f;
    private const float UpgradeTargetSize = 50.0f;
    private const float TargetMissDestroyY = 870.0f;
    private const float WaveDuration = 72.0f;
    private const float BossSpawnY = 122.0f;
    private const float BossAttackInterval = 3.8f;
    private const float BossWarningDelay = 1.15f;
    private const float BossWarningTopY = 188.0f;
    private const float BossWarningBottomY = 820.0f;
    private const float BossLaneWidth = 110.0f;
    private const float BossDangerDistance = 58.0f;
    private const float HeroHitLineY = 760.0f;
    private const float TrainHitLineY = 820.0f;
    private const float HeroHitDistance = 46.0f;
    private const int EnemyHeroDamage = 20;
    private const int EnemyTrainDamage = 8;
    private const int BossMaxHp = 300;
    private const int BossLaneDamage = 18;
    private const int AttackAddAmount = 5;
    private const float FireRateStep = 0.08f;
    private const int HealAmount = 18;

    private Control _battleArea = null!;
    private Control _playerRig = null!;
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
    private int _nextUpgradeTypeIndex;
    private int _nextWaveEventIndex;
    private int _nextBossLaneIndex;
    private int _pendingBossLaneIndex = -1;
    private int _temporaryAttackAdd;
    private int _temporaryFireRateStacks;
    private int _temporaryBulletAdd;
    private bool _waveCompleted;
    private bool _bossSpawned;
    private bool _battleEnded;
    private Boss _boss = null!;
    private ColorRect _bossWarningRect = null!;
    private ColorRect _resultPanel = null!;
    private readonly List<WaveSpawnEvent> _waveTimeline = new();

    private static readonly string[] UpgradeTypes =
    {
        "attack_add",
        "fire_rate",
        "heal",
        "bullet_add"
    };

    private enum WaveSpawnKind
    {
        Enemy,
        UpgradeTarget
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
        public WaveSpawnEvent(float time, WaveSpawnKind kind, int laneIndex, string upgradeType = "")
        {
            Time = time;
            Kind = kind;
            LaneIndex = laneIndex;
            UpgradeType = upgradeType;
        }

        public float Time { get; }
        public WaveSpawnKind Kind { get; }
        public int LaneIndex { get; }
        public string UpgradeType { get; }
    }

    public override void _Ready()
    {
        _battleArea = GetNode<Control>("BattleArea540x960");
        _playerRig = GetNode<Control>("BattleArea540x960/PlayerRig");
        _waveStatusLabel = GetNode<Label>("BattleArea540x960/BattleAreaLabel");
        _weaponStatsLabel = GetNode<Label>("BattleArea540x960/WeaponStatsLabel");
        _combatStatusLabel = GetNode<Label>("BattleArea540x960/CombatStatusLabel");
        _temporaryBuffLabel = GetNode<Label>("BattleArea540x960/TemporaryBuffLabel");
        ClearTemporaryBuffs();
        BuildWaveTimeline();
        SetPlayerX(PlayerStartX);
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
        RemoveMissedUpgradeTargets();
        CheckFailure();
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

    private void SetPlayerX(float rawX)
    {
        float clampedX = Mathf.Clamp(rawX, PlayerMinX, PlayerMaxX);
        _playerRig.Position = new Vector2(clampedX, 0.0f);
    }

    private GameManager State => GetNode<GameManager>("/root/GameManager");

    private int GetDamage()
    {
        return State.GetCannonDamage() + _temporaryAttackAdd;
    }

    private float GetFireInterval()
    {
        return Mathf.Max(0.16f, State.GetCannonFireInterval() - _temporaryFireRateStacks * FireRateStep);
    }

    private void RefreshWeaponStats()
    {
        _weaponStatsLabel.Text = $"Cannon Damage: {GetDamage()}\nFire Interval: {GetFireInterval():0.00}s";
    }

    private void RefreshCombatStatus()
    {
        _combatStatusLabel.Text =
            $"Train HP: {State.TrainCurrentHp}/{State.TrainMaxHp}\n" +
            $"Kill Count: {_killCount}\n" +
            $"Current Station: {State.CurrentStation}";
    }

    private void RefreshWaveStatus()
    {
        float displayTime = Mathf.Min(_battleTime, WaveDuration);
        string phaseName = GetWavePhaseName(displayTime);
        _waveStatusLabel.Text = $"Wave: {phaseName}  {displayTime:0}/{WaveDuration:0}s";
    }

    private string GetWavePhaseName(float time)
    {
        if (_waveCompleted)
        {
            return _phase == BattlePhase.Boss ? "Boss" : "Complete";
        }

        if (time < 10.0f)
        {
            return "Warmup";
        }

        if (time < 35.0f)
        {
            return "Pressure";
        }

        if (time < 60.0f)
        {
            return "Heavy";
        }

        return "Final Push";
    }

    private void RefreshTemporaryBuffs()
    {
        _temporaryBuffLabel.Text =
            "Temporary Buffs\n" +
            $"ATK +{_temporaryAttackAdd}\n" +
            $"Fire Rate x{_temporaryFireRateStacks}\n" +
            $"Bullets +{_temporaryBulletAdd}";
    }

    private void FireVolley()
    {
        float playerX = _playerRig.Position.X;
        SpawnBullet(new Vector2(playerX - 11.0f, HeroMuzzleY));
        SpawnBullet(new Vector2(playerX - 5.0f, TurretMuzzleY));

        for (int i = 0; i < _temporaryBulletAdd; i += 1)
        {
            float offset = i % 2 == 0 ? 13.0f + i * 4.0f : -19.0f - i * 4.0f;
            SpawnBullet(new Vector2(playerX + offset, TurretMuzzleY + 4.0f));
        }
    }

    private void SpawnBullet(Vector2 position)
    {
        Bullet bullet = new Bullet
        {
            Position = position,
            Size = new Vector2(10.0f, 20.0f),
            Color = new Color(1.0f, 0.94f, 0.35f, 1.0f),
            Damage = GetDamage()
        };

        _battleArea.AddChild(bullet);
    }

    private void SpawnEnemy(int laneIndex)
    {
        float laneX = GetLaneX(laneIndex);

        Enemy enemy = new Enemy
        {
            Position = new Vector2(laneX - EnemySize * 0.5f, EnemySpawnY),
            Size = new Vector2(EnemySize, EnemySize),
            Color = new Color(0.85f, 0.24f, 0.2f, 1.0f),
            MaxHp = 30,
            Speed = 96.0f
        };
        enemy.Died += OnEnemyDied;

        _battleArea.AddChild(enemy);
    }

    private void SpawnUpgradeTarget(int laneIndex, string upgradeType)
    {
        float laneX = GetLaneX(laneIndex);

        UpgradeTarget target = new UpgradeTarget
        {
            Position = new Vector2(laneX - UpgradeTargetSize * 0.5f, UpgradeTargetSpawnY),
            Size = new Vector2(UpgradeTargetSize, UpgradeTargetSize),
            Color = GetUpgradeColor(upgradeType),
            MaxHp = 25,
            Speed = 82.0f,
            UpgradeType = upgradeType
        };
        target.Destroyed += OnUpgradeTargetDestroyed;

        _battleArea.AddChild(target);
    }

    private float GetLaneX(int laneIndex)
    {
        return laneIndex % 2 == 0 ? LeftLaneX : RightLaneX;
    }

    private Color GetUpgradeColor(string upgradeType)
    {
        return upgradeType switch
        {
            "attack_add" => new Color(0.98f, 0.62f, 0.18f, 1.0f),
            "fire_rate" => new Color(0.34f, 0.92f, 1.0f, 1.0f),
            "heal" => new Color(0.28f, 0.9f, 0.46f, 1.0f),
            "bullet_add" => new Color(0.76f, 0.58f, 1.0f, 1.0f),
            _ => new Color(0.8f, 0.8f, 0.8f, 1.0f)
        };
    }

    private void OnUpgradeTargetDestroyed(string upgradeType)
    {
        switch (upgradeType)
        {
            case "attack_add":
                _temporaryAttackAdd += AttackAddAmount;
                break;
            case "fire_rate":
                _temporaryFireRateStacks += 1;
                break;
            case "heal":
                State.HealTrain(HealAmount);
                break;
            case "bullet_add":
                _temporaryBulletAdd += 1;
                break;
        }

        RefreshWeaponStats();
        RefreshCombatStatus();
        RefreshTemporaryBuffs();
    }

    private void OnEnemyDied()
    {
        _killCount += 1;
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

            if (!enemy.CheckedHeroLine && enemy.BottomY() >= HeroHitLineY)
            {
                enemy.CheckedHeroLine = true;
                if (Mathf.Abs(enemy.CenterX() - _playerRig.Position.X) <= HeroHitDistance)
                {
                    State.DamageTrain(EnemyHeroDamage);
                    enemy.QueueFree();
                    CheckFailure();
                    continue;
                }
            }

            if (enemy.BottomY() >= TrainHitLineY)
            {
                State.DamageTrain(EnemyTrainDamage);
                enemy.QueueFree();
                CheckFailure();
            }
        }
    }

    private void RemoveMissedUpgradeTargets()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is UpgradeTarget target && !target.IsQueuedForDeletion() && target.BottomY() >= TargetMissDestroyY)
            {
                target.QueueFree();
            }
        }
    }

    private void ClearTemporaryBuffs()
    {
        _temporaryAttackAdd = 0;
        _temporaryFireRateStacks = 0;
        _temporaryBulletAdd = 0;
    }

    private void BuildWaveTimeline()
    {
        _waveTimeline.Clear();
        _nextWaveEventIndex = 0;
        _battleTime = 0.0f;
        _waveCompleted = false;
        _nextUpgradeTypeIndex = 0;

        int laneIndex = 0;
        for (float time = 1.5f; time < 10.0f; time += 3.2f)
        {
            AddEnemyEvent(time, laneIndex);
            laneIndex += 1;
        }

        for (float time = 10.5f; time < 35.0f; time += 2.4f)
        {
            AddEnemyEvent(time, laneIndex);
            laneIndex += 1;
        }

        AddUpgradeEvent(14.0f, 1);
        AddUpgradeEvent(23.0f, 0);
        AddUpgradeEvent(32.0f, 1);

        int heavyIndex = 0;
        for (float time = 35.0f; time < 60.0f; time += 1.9f)
        {
            AddEnemyEvent(time, laneIndex);
            if (heavyIndex % 4 == 2)
            {
                AddEnemyEvent(time + 0.45f, laneIndex + 1);
            }

            laneIndex += 1;
            heavyIndex += 1;
        }

        AddUpgradeEvent(42.0f, 0);
        AddUpgradeEvent(53.0f, 1);

        for (float time = 60.0f; time < 70.0f; time += 1.7f)
        {
            AddEnemyEvent(time, laneIndex);
            laneIndex += 1;
        }

        AddUpgradeEvent(64.0f, 0);
        _waveTimeline.Sort((left, right) => left.Time.CompareTo(right.Time));
    }

    private void AddEnemyEvent(float time, int laneIndex)
    {
        _waveTimeline.Add(new WaveSpawnEvent(time, WaveSpawnKind.Enemy, laneIndex));
    }

    private void AddUpgradeEvent(float time, int laneIndex)
    {
        string upgradeType = UpgradeTypes[_nextUpgradeTypeIndex % UpgradeTypes.Length];
        _nextUpgradeTypeIndex += 1;
        _waveTimeline.Add(new WaveSpawnEvent(time, WaveSpawnKind.UpgradeTarget, laneIndex, upgradeType));
    }

    private void RunWaveTimeline()
    {
        while (_nextWaveEventIndex < _waveTimeline.Count && _waveTimeline[_nextWaveEventIndex].Time <= _battleTime)
        {
            WaveSpawnEvent spawnEvent = _waveTimeline[_nextWaveEventIndex];
            if (spawnEvent.Kind == WaveSpawnKind.Enemy)
            {
                SpawnEnemy(spawnEvent.LaneIndex);
            }
            else
            {
                SpawnUpgradeTarget(spawnEvent.LaneIndex, spawnEvent.UpgradeType);
            }

            _nextWaveEventIndex += 1;
        }

        if (!_waveCompleted && _battleTime >= WaveDuration)
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
        _bossAttackTimer = 1.6f;
        RefreshWaveStatus();
    }

    private void ClearRemainingWaveObjects()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Enemy or UpgradeTarget)
            {
                child.QueueFree();
            }
        }
    }

    private void SpawnBoss()
    {
        _boss = new Boss
        {
            Position = new Vector2(140.0f, BossSpawnY),
            Size = new Vector2(260.0f, 92.0f),
            Color = new Color(0.42f, 0.28f, 0.2f, 1.0f),
            MaxHp = BossMaxHp
        };
        _boss.Defeated += OnBossDefeated;
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
            _bossAttackTimer = BossAttackInterval;
        }
    }

    private void StartBossLaneWarning(int laneIndex)
    {
        _pendingBossLaneIndex = laneIndex;
        _bossWarningTimer = BossWarningDelay;

        float laneX = GetLaneX(laneIndex);
        _bossWarningRect?.QueueFree();
        _bossWarningRect = new ColorRect
        {
            Position = new Vector2(laneX - BossLaneWidth * 0.5f, BossWarningTopY),
            Size = new Vector2(BossLaneWidth, BossWarningBottomY - BossWarningTopY),
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

        if (Mathf.Abs(_playerRig.Position.X - dangerX) <= BossDangerDistance)
        {
            State.DamageTrain(BossLaneDamage);
            CheckFailure();
        }

        _pendingBossLaneIndex = -1;
    }

    private void OnBossDefeated()
    {
        ShowResultPanel(true);
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
        _phase = BattlePhase.Ended;
        _bossWarningRect?.QueueFree();
        _bossWarningRect = null;
        ClearCombatObjects();

        _resultPanel = new ColorRect
        {
            Position = new Vector2(70.0f, 320.0f),
            Size = new Vector2(400.0f, 220.0f),
            Color = victory
                ? new Color(0.12f, 0.34f, 0.22f, 0.96f)
                : new Color(0.34f, 0.12f, 0.12f, 0.96f),
            MouseFilter = MouseFilterEnum.Ignore
        };

        Label title = new Label
        {
            Position = new Vector2(0.0f, 42.0f),
            Size = new Vector2(_resultPanel.Size.X, 52.0f),
            Text = victory ? "VICTORY" : "DEFEAT",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 34);
        _resultPanel.AddChild(title);

        Label summary = new Label
        {
            Position = new Vector2(24.0f, 108.0f),
            Size = new Vector2(_resultPanel.Size.X - 48.0f, 74.0f),
            Text = victory
                ? $"Boss defeated\nKills: {_killCount}\nTrain HP: {State.TrainCurrentHp}/{State.TrainMaxHp}"
                : $"Train destroyed\nKills: {_killCount}\nBoss fight failed",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        summary.AddThemeFontSizeOverride("font_size", 20);
        _resultPanel.AddChild(summary);

        _battleArea.AddChild(_resultPanel);
        RefreshWaveStatus();
    }

    private void ClearCombatObjects()
    {
        foreach (Node child in _battleArea.GetChildren())
        {
            if (child is Enemy or UpgradeTarget or Bullet or Boss)
            {
                child.QueueFree();
            }
        }
    }
}
