using Godot;
using System;

public partial class Shockwave : ColorRect
{
    public event Action<Shockwave, Vector2> Destroyed;
    public event Action<Shockwave> ReachedTrain;

    [Export] public float Speed { get; set; } = 150.0f;
    [Export] public int MaxHp { get; set; } = 60;
    [Export] public int Damage { get; set; } = 12;
    [Export] public float ImpactY { get; set; } = 845.0f;
    [Export] public string DisplayName { get; set; } = "冲击波";

    public int Hp { get; private set; }
    public bool IsResolved { get; private set; }

    private Label _nameLabel = null!;
    private Label _hpLabel = null!;
    private ColorRect _hpBar = null!;
    private Color _baseColor;
    private float _hitFlashTimer;

    public override void _Ready()
    {
        Hp = MaxHp;
        _baseColor = Color;
        MouseFilter = MouseFilterEnum.Ignore;

        _nameLabel = new Label
        {
            Position = new Vector2(0.0f, Size.Y * 0.25f - 13.0f),
            Size = new Vector2(Size.X, 24.0f),
            Text = DisplayName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_nameLabel);

        _hpLabel = new Label
        {
            Position = new Vector2(0.0f, Size.Y * 0.6f - 12.0f),
            Size = new Vector2(Size.X, 22.0f),
            Text = $"HP {Hp}",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _hpLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_hpLabel);

        ColorRect hpBack = new()
        {
            Position = new Vector2(6.0f, Size.Y - 8.0f),
            Size = new Vector2(Size.X - 12.0f, 5.0f),
            Color = new Color(0.12f, 0.08f, 0.08f, 1.0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(hpBack);

        _hpBar = new ColorRect
        {
            Position = hpBack.Position,
            Size = hpBack.Size,
            Color = new Color(1.0f, 0.78f, 0.38f, 1.0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_hpBar);
    }

    public override void _Process(double delta)
    {
        if (IsResolved)
        {
            return;
        }

        Position += Vector2.Down * Speed * (float)delta;

        if (_hitFlashTimer > 0.0f)
        {
            _hitFlashTimer -= (float)delta;
            float ratio = Mathf.Clamp(_hitFlashTimer / 0.12f, 0.0f, 1.0f);
            Color = _baseColor.Lerp(new Color(1.0f, 0.94f, 0.58f, 1.0f), ratio);
            Scale = new Vector2(1.0f + ratio * 0.04f, 1.0f + ratio * 0.04f);
        }
        else
        {
            Color = _baseColor;
            Scale = Vector2.One;
        }

        if (BottomY() >= ImpactY)
        {
            ResolveReachedTrain();
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
        if (IsResolved)
        {
            return;
        }

        Hp = Mathf.Max(0, Hp - damage);
        _hitFlashTimer = 0.12f;
        _hpLabel.Text = $"HP {Hp}";
        _hpBar.Size = new Vector2((Size.X - 12.0f) * Hp / MaxHp, _hpBar.Size.Y);

        if (Hp <= 0)
        {
            ResolveDestroyed();
        }
    }

    private void ResolveDestroyed()
    {
        if (IsResolved)
        {
            return;
        }

        IsResolved = true;
        Destroyed?.Invoke(this, Position + Size * 0.5f);
        QueueFree();
    }

    private void ResolveReachedTrain()
    {
        if (IsResolved)
        {
            return;
        }

        IsResolved = true;
        ReachedTrain?.Invoke(this);
        QueueFree();
    }
}
