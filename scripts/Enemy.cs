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
    [Export] public string TexturePath { get; set; } = "";

    public int Hp { get; private set; }
    public bool CheckedHeroLine { get; set; }

    private Label _hpLabel = null!;
    private TextureRect _sprite = null!;
    private Color _baseColor;
    private Color _baseSpriteModulate = Colors.White;
    private float _hitFlashTimer;
    private bool _usesTexture;
    private Vector2 _moveDirection = Vector2.Down;

    public override void _Ready()
    {
        Hp = MaxHp;
        SetupTextureVisual();
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
        Position += _moveDirection * Speed * (float)delta;

        if (_hitFlashTimer > 0.0f)
        {
            _hitFlashTimer -= (float)delta;
            float ratio = Mathf.Clamp(_hitFlashTimer / 0.12f, 0.0f, 1.0f);
            ApplyHitVisual(ratio);
        }
        else
        {
            ResetHitVisual();
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

    public void SetTrackLine(Vector2 centerStart, Vector2 centerEnd)
    {
        Position = centerStart - Size * 0.5f;
        Vector2 direction = centerEnd - centerStart;
        _moveDirection = direction.LengthSquared() > 0.01f ? direction.Normalized() : Vector2.Down;
    }

    private void SetupTextureVisual()
    {
        if (string.IsNullOrWhiteSpace(TexturePath))
        {
            return;
        }

        Texture2D texture = GD.Load<Texture2D>(TexturePath);
        if (texture == null)
        {
            GD.PushWarning($"Enemy texture not found: {TexturePath}");
            return;
        }

        _sprite = new TextureRect
        {
            Position = Vector2.Zero,
            Size = Size,
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_sprite);
        _baseSpriteModulate = _sprite.Modulate;
        _usesTexture = true;
        Color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
    }

    private void ApplyHitVisual(float ratio)
    {
        Color flash = new Color(1.0f, 0.94f, 0.78f, 1.0f);
        if (_usesTexture)
        {
            _sprite.Modulate = _baseSpriteModulate.Lerp(flash, ratio);
        }
        else
        {
            Color = _baseColor.Lerp(flash, ratio);
        }
    }

    private void ResetHitVisual()
    {
        if (_usesTexture)
        {
            _sprite.Modulate = _baseSpriteModulate;
        }
        else
        {
            Color = _baseColor;
        }
    }
}
