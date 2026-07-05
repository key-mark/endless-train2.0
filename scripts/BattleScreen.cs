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
    private const float HeroHitLineY = 760.0f;
    private const float TrainHitLineY = 820.0f;
    private const float HeroHitDistance = 46.0f;
    private const int EnemyHeroDamage = 20;
    private const int EnemyTrainDamage = 8;

    private Control _battleArea = null!;
    private Control _playerRig = null!;
    private Label _weaponStatsLabel = null!;
    private Label _combatStatusLabel = null!;
    private bool _isDragging;
    private float _shootTimer;
    private float _enemySpawnTimer;
    private int _killCount;
    private int _nextEnemyLaneIndex;

    public override void _Ready()
    {
        _battleArea = GetNode<Control>("BattleArea540x960");
        _playerRig = GetNode<Control>("BattleArea540x960/PlayerRig");
        _weaponStatsLabel = GetNode<Label>("BattleArea540x960/WeaponStatsLabel");
        _combatStatusLabel = GetNode<Label>("BattleArea540x960/CombatStatusLabel");
        SetPlayerX(PlayerStartX);
        RefreshWeaponStats();
        RefreshCombatStatus();
        _shootTimer = GetFireInterval();
        _enemySpawnTimer = 0.2f;
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

        CheckEnemyLineDamage();
        RefreshWeaponStats();
        RefreshCombatStatus();
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
        return State.GetCannonDamage();
    }

    private float GetFireInterval()
    {
        return State.GetCannonFireInterval();
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

    private void FireVolley()
    {
        float playerX = _playerRig.Position.X;
        SpawnBullet(new Vector2(playerX - 11.0f, HeroMuzzleY));
        SpawnBullet(new Vector2(playerX - 5.0f, TurretMuzzleY));
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
}
