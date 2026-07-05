using Godot;

public partial class Boss : ColorRect
{
    [Signal]
    public delegate void DefeatedEventHandler();

    [Signal]
    public delegate void PhaseChangedEventHandler(int phase);

    private const int SegmentCount = 3;

    [Export] public int MaxHp { get; set; } = 300;

    public int Hp { get; private set; }
    public int CurrentPhase { get; private set; } = 1;

    private Label _hpLabel = null!;
    private ColorRect[] _hpBars = null!;
    private Color _phaseColor;
    private float _hitFlashTimer;

    public override void _Ready()
    {
        Hp = MaxHp;
        _phaseColor = Color;

        Label nameLabel = new Label
        {
            Position = new Vector2(0.0f, 10.0f),
            Size = new Vector2(Size.X, 26.0f),
            Text = "Station Breaker",
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
    }

    public Rect2 GetHitRect()
    {
        return new Rect2(Position, Size);
    }

    public void TakeDamage(int damage)
    {
        Hp = Mathf.Max(0, Hp - damage);
        _hitFlashTimer = 0.14f;
        _hpLabel.Text = GetHpText();
        RefreshHpBars();
        UpdatePhase();

        if (Hp <= 0)
        {
            EmitSignal(SignalName.Defeated);
            QueueFree();
        }
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
}
