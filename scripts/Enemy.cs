using Godot;

public partial class Enemy : ColorRect
{
    [Signal]
    public delegate void DiedEventHandler();

    [Export] public float Speed { get; set; } = 96.0f;
    [Export] public int MaxHp { get; set; } = 30;

    public int Hp { get; private set; }
    public bool CheckedHeroLine { get; set; }

    private Label _hpLabel = null!;

    public override void _Ready()
    {
        Hp = MaxHp;
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

        if (Hp <= 0)
        {
            EmitSignal(SignalName.Died);
            QueueFree();
        }
    }
}
