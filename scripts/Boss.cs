using Godot;
using System;

public partial class Boss : ColorRect
{
    [Signal]
    public delegate void DefeatedEventHandler();

    [Signal]
    public delegate void PhaseChangedEventHandler(int phase);

    [Signal]
    public delegate void LaneWarningStartedEventHandler(int laneIndex);

    [Signal]
    public delegate void LaneAttackResolvedEventHandler(int laneIndex, int damage);

    private const int SegmentCount = 3;

    [Export] public int MaxHp { get; set; } = 300;
    [Export] public int DamagePerHit { get; set; } = 5;
    [Export] public string DisplayName { get; set; } = "Station Breaker";

    public event Action DefeatedReached;

    public int Hp { get; private set; }
    public int CurrentPhase { get; private set; } = 1;
    public bool IsDefeated { get; private set; }

    private Label _hpLabel = null!;
    private ColorRect[] _hpBars = null!;
    private Color _phaseColor;
    private float _hitFlashTimer;
    private bool _attacksEnabled;
    private float _attackInterval = 4.0f;
    private float _attackTimer = 1.6f;
    private float _warningDelay = 1.0f;
    private float _warningTimer;
    private int _attackDamage = 18;
    private int _nextLaneIndex;
    private int _pendingLaneIndex = -1;

    public override void _Ready()
    {
        Hp = MaxHp;
        _phaseColor = Color;

        Label nameLabel = new Label
        {
            Position = new Vector2(0.0f, 10.0f),
            Size = new Vector2(Size.X, 26.0f),
            Text = DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        AddChild(nameLabel);

        _hpLabel = new Label
        {
            Position = new Vector2(0.0f, 38.0f),
            Size = new Vector2(Size.X, 22.0f),
            Text = GetHpText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _hpLabel.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_hpLabel);

        _hpBars = new ColorRect[SegmentCount];
        float barWidth = (Size.X - 32.0f) / SegmentCount;
        for (int i = 0; i < SegmentCount; i += 1)
        {
            ColorRect back = new ColorRect
            {
                Position = new Vector2(12.0f + i * (barWidth + 4.0f), 66.0f),
                Size = new Vector2(barWidth, 8.0f),
                Color = new Color(0.12f, 0.11f, 0.1f, 1.0f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            AddChild(back);

            _hpBars[i] = new ColorRect
            {
                Position = back.Position,
                Size = back.Size,
                Color = i switch
                {
                    0 => new Color(0.95f, 0.28f, 0.22f, 1.0f),
                    1 => new Color(0.95f, 0.58f, 0.18f, 1.0f),
                    _ => new Color(0.95f, 0.82f, 0.26f, 1.0f)
                },
                MouseFilter = MouseFilterEnum.Ignore
            };
            AddChild(_hpBars[i]);
        }

        RefreshHpBars();
    }

    public override void _Process(double delta)
    {
        if (_hitFlashTimer > 0.0f)
        {
            _hitFlashTimer -= (float)delta;
            float ratio = Mathf.Clamp(_hitFlashTimer / 0.14f, 0.0f, 1.0f);
            Color = _phaseColor.Lerp(new Color(1.0f, 0.86f, 0.5f, 1.0f), ratio);
            Scale = new Vector2(1.0f + ratio * 0.025f, 1.0f + ratio * 0.025f);
        }
        else
        {
            Color = _phaseColor;
            Scale = Vector2.One;
        }

        RunAttackTimer((float)delta);
    }

    public Rect2 GetHitRect()
    {
        return new Rect2(Position, Size);
    }

    public void TakeDamage(int damage)
    {
        if (IsDefeated)
        {
            return;
        }

        int appliedDamage = DamagePerHit > 0 ? DamagePerHit : damage;
        Hp = Mathf.Max(0, Hp - appliedDamage);
        _hitFlashTimer = 0.14f;
        _hpLabel.Text = GetHpText();
        RefreshHpBars();
        UpdatePhase();

        if (Hp <= 0)
        {
            Defeat();
        }
    }

    private void Defeat()
    {
        if (IsDefeated)
        {
            return;
        }

        IsDefeated = true;
        _attacksEnabled = false;
        EmitSignal(SignalName.Defeated);
        DefeatedReached?.Invoke();
        QueueFree();
    }

    public void ConfigureAttack(float attackInterval, float initialAttackDelay, float warningDelay, int attackDamage)
    {
        _attackInterval = Mathf.Max(0.1f, attackInterval);
        _attackTimer = Mathf.Max(0.0f, initialAttackDelay);
        _warningDelay = Mathf.Max(0.0f, warningDelay);
        _attackDamage = Mathf.Max(0, attackDamage);
        _pendingLaneIndex = -1;
        _nextLaneIndex = 0;
    }

    public void SetAttacksEnabled(bool enabled)
    {
        _attacksEnabled = enabled;
    }

    private string GetHpText()
    {
        return $"Boss HP {Hp}/{MaxHp}";
    }

    private void RefreshHpBars()
    {
        int segmentHp = MaxHp / SegmentCount;
        for (int i = 0; i < SegmentCount; i += 1)
        {
            int segmentRemaining = Mathf.Clamp(Hp - i * segmentHp, 0, segmentHp);
            float ratio = (float)segmentRemaining / segmentHp;
            _hpBars[i].Scale = new Vector2(ratio, 1.0f);
        }
    }

    private void UpdatePhase()
    {
        int nextPhase = Hp > MaxHp * 2 / 3 ? 1 : Hp > MaxHp / 3 ? 2 : 3;
        if (nextPhase == CurrentPhase)
        {
            return;
        }

        CurrentPhase = nextPhase;
        _phaseColor = CurrentPhase switch
        {
            2 => new Color(0.52f, 0.24f, 0.18f, 1.0f),
            3 => new Color(0.62f, 0.16f, 0.14f, 1.0f),
            _ => new Color(0.42f, 0.28f, 0.2f, 1.0f)
        };
        EmitSignal(SignalName.PhaseChanged, CurrentPhase);
    }

    private void RunAttackTimer(float delta)
    {
        if (!_attacksEnabled || Hp <= 0)
        {
            return;
        }

        if (_pendingLaneIndex >= 0)
        {
            _warningTimer -= delta;
            if (_warningTimer <= 0.0f)
            {
                EmitSignal(SignalName.LaneAttackResolved, _pendingLaneIndex, _attackDamage);
                _pendingLaneIndex = -1;
                _attackTimer = _attackInterval;
            }

            return;
        }

        _attackTimer -= delta;
        if (_attackTimer <= 0.0f)
        {
            _pendingLaneIndex = _nextLaneIndex;
            _nextLaneIndex += 1;
            _warningTimer = _warningDelay;
            EmitSignal(SignalName.LaneWarningStarted, _pendingLaneIndex);
        }
    }
}
