using Godot;

public partial class Enemy : ColorRect
{
    [Signal]
    public delegate void DiedEventHandler(Vector2 position);

    [Export] public float Speed { get; set; } = 96.0f;
    [Export] public int MaxHp { get; set; } = 30;
    [Export] public int HeroLineDamage { get; set; } = 12;
    [Export] public int TrainLineDamage { get; set; } = 6;
    [Export] public int ScrapReward { get; set; } = 2;

    public int Hp { get; private set; }
    public bool CheckedHeroLine { get; set; }

    private Label _hpLabel = null!;
    private Color _baseColor;
    private float _hitFlashTimer;

    public override void _Ready()
    {
        Hp = MaxHp;
        _baseColor = Color;
        _hpLabel = new Label
        {
            Position = new Vector2(0.0f, 10.0f),
            Size = Size,
            Text = Hp.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _hpLabel.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_hpLabel);
    }

    public override void _Process(double delta)
    {
        Position += Vector2.Down * Speed * (float)delta;

        if (_hitFlashTimer > 0.0f)
        {
            _hitFlashTimer -= (float)delta;
            float ratio = Mathf.Clamp(_hitFlashTimer / 0.12f, 0.0f, 1.0f);
            Color = _baseColor.Lerp(new Color(1.0f, 0.94f, 0.78f, 1.0f), ratio);
        }
        else
        {
            Color = _baseColor;
        }
    }

    public Rect2 GetHitRect()
    {
        return new Rect2(Position, Size);
    }

    public float CenterX()
    {
        return Position.X + Size.X * 0.5f;
    }

    public float BottomY()
    {
        return Position.Y + Size.Y;
    }

    public void TakeDamage(int damage)
    {
        Hp = Mathf.Max(0, Hp - damage);
        _hpLabel.Text = Hp.ToString();
        _hitFlashTimer = 0.12f;

        if (Hp <= 0)
        {
            EmitSignal(SignalName.Died, new Vector2(CenterX(), Position.Y + Size.Y * 0.5f));
            QueueFree();
        }
    }
}
