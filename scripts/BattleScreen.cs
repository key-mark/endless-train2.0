using Godot;

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
    private const float EnemySpawnInterval = 1.65f;
    private const float UpgradeTargetSpawnY = -64.0f;
    private const float UpgradeTargetSize = 50.0f;
    private const float UpgradeTargetSpawnInterval = 4.8f;
    private const float TargetMissDestroyY = 870.0f;
    private const float HeroHitLineY = 760.0f;
    private const float TrainHitLineY = 820.0f;
    private const float HeroHitDistance = 46.0f;
    private const int EnemyHeroDamage = 20;
    private const int EnemyTrainDamage = 8;
    private const int AttackAddAmount = 5;
    private const float FireRateStep = 0.08f;
    private const int HealAmount = 18;

    private Control _battleArea = null!;
    private Control _playerRig = null!;
    private Label _weaponStatsLabel = null!;
    private Label _combatStatusLabel = null!;
    private Label _temporaryBuffLabel = null!;
    private bool _isDragging;
    private float _shootTimer;
    private float _enemySpawnTimer;
    private float _upgradeTargetSpawnTimer;
    private int _killCount;
    private int _nextEnemyLaneIndex;
    private int _nextUpgradeLaneIndex = 1;
    private int _nextUpgradeTypeIndex;
    private int _temporaryAttackAdd;
    private int _temporaryFireRateStacks;
    private int _temporaryBulletAdd;

    private static readonly string[] UpgradeTypes =
    {
        "attack_add",
        "fire_rate",
        "heal",
        "bullet_add"
    };

    public override void _Ready()
    {
        _battleArea = GetNode<Control>("BattleArea540x960");
        _playerRig = GetNode<Control>("BattleArea540x960/PlayerRig");
        _weaponStatsLabel = GetNode<Label>("BattleArea540x960/WeaponStatsLabel");
        _combatStatusLabel = GetNode<Label>("BattleArea540x960/CombatStatusLabel");
        _temporaryBuffLabel = GetNode<Label>("BattleArea540x960/TemporaryBuffLabel");
        ClearTemporaryBuffs();
        SetPlayerX(PlayerStartX);
        RefreshWeaponStats();
        RefreshCombatStatus();
        RefreshTemporaryBuffs();
        _shootTimer = GetFireInterval();
        _enemySpawnTimer = 0.2f;
        _upgradeTargetSpawnTimer = 2.0f;
    }

    public override void _Process(double delta)
    {
        _shootTimer -= (float)delta;
        if (_shootTimer <= 0.0f)
        {
            FireVolley();
            _shootTimer += GetFireInterval();
        }

        _enemySpawnTimer -= (float)delta;
        if (_enemySpawnTimer <= 0.0f)
        {
            SpawnEnemy();
            _enemySpawnTimer += EnemySpawnInterval;
        }

        _upgradeTargetSpawnTimer -= (float)delta;
        if (_upgradeTargetSpawnTimer <= 0.0f)
        {
            SpawnUpgradeTarget();
            _upgradeTargetSpawnTimer += UpgradeTargetSpawnInterval;
        }

        CheckEnemyLineDamage();
        RemoveMissedUpgradeTargets();
        RefreshWeaponStats();
        RefreshCombatStatus();
        RefreshTemporaryBuffs();
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

    private void SpawnEnemy()
    {
        float laneX = _nextEnemyLaneIndex % 2 == 0 ? LeftLaneX : RightLaneX;
        _nextEnemyLaneIndex += 1;

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

    private void SpawnUpgradeTarget()
    {
        float laneX = _nextUpgradeLaneIndex % 2 == 0 ? LeftLaneX : RightLaneX;
        _nextUpgradeLaneIndex += 1;

        string upgradeType = UpgradeTypes[_nextUpgradeTypeIndex % UpgradeTypes.Length];
        _nextUpgradeTypeIndex += 1;

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
                    continue;
                }
            }

            if (enemy.BottomY() >= TrainHitLineY)
            {
                State.DamageTrain(EnemyTrainDamage);
                enemy.QueueFree();
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
}
