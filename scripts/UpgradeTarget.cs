using Godot;

public partial class UpgradeTarget : ColorRect
{
    [Signal]
    public delegate void DestroyedEventHandler(string upgradeType, Vector2 position);

    [Export] public float Speed { get; set; } = 82.0f;
    [Export] public int MaxHp { get; set; } = 25;
    [Export] public string UpgradeType { get; set; } = "attack_add";

    public int Hp { get; private set; }

    private Label _typeLabel = null!;
    private Label _hpLabel = null!;
    private ColorRect _hpBar = null!;
    private Color _baseColor;
    private float _hitFlashTimer;

    public override void _Ready()
    {
        Hp = MaxHp;
        _baseColor = Color;

        _typeLabel = new Label
        {
            Position = new Vector2(-18.0f, 14.0f),
            Size = new Vector2(Size.X + 36.0f, 22.0f),
            Text = GetShortName(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _typeLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_typeLabel);

        _hpLabel = new Label
        {
            Position = new Vector2(-8.0f, -23.0f),
            Size = new Vector2(Size.X + 16.0f, 18.0f),
            Text = $"HP {Hp}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _hpLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_hpLabel);

        ColorRect hpBack = new ColorRect
        {
            Position = new Vector2(0.0f, -6.0f),
            Size = new Vector2(Size.X, 5.0f),
            Color = new Color(0.1f, 0.12f, 0.14f, 1.0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(hpBack);

        _hpBar = new ColorRect
        {
            Position = new Vector2(0.0f, -6.0f),
            Size = new Vector2(Size.X, 5.0f),
            Color = new Color(0.48f, 1.0f, 0.84f, 1.0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_hpBar);
    }

    public override void _Process(double delta)
    {
        Position += Vector2.Down * Speed * (float)delta;

        if (_hitFlashTimer > 0.0f)
        {
            _hitFlashTimer -= (float)delta;
            float ratio = Mathf.Clamp(_hitFlashTimer / 0.12f, 0.0f, 1.0f);
            Color = _baseColor.Lerp(new Color(1.0f, 1.0f, 1.0f, 1.0f), ratio);
            Scale = new Vector2(1.0f + ratio * 0.08f, 1.0f + ratio * 0.08f);
        }
        else
        {
            Color = _baseColor;
            Scale = Vector2.One;
        }
    }

    public Rect2 GetHitRect()
    {
        return new Rect2(Position, Size);
    }

    public float BottomY()
    {
        return Position.Y + Size.Y;
    }

    public void TakeDamage(int damage)
    {
        Hp = Mathf.Max(0, Hp - damage);
        _hpLabel.Text = $"HP {Hp}";
        _hpBar.Size = new Vector2(Size.X * Hp / MaxHp, _hpBar.Size.Y);
        _hitFlashTimer = 0.12f;

        if (Hp <= 0)
        {
            EmitSignal(SignalName.Destroyed, UpgradeType, new Vector2(Position.X + Size.X * 0.5f, Position.Y + Size.Y * 0.5f));
            QueueFree();
        }
    }

    private string GetShortName()
    {
        return UpgradeType switch
        {
            "attack_add" => "ATK",
            "fire_rate" => "RATE",
            "heal" => "HEAL",
            "bullet_add" => "+SHOT",
            _ => "UP"
        };
    }
}
