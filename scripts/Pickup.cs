using Godot;

public partial class Pickup : ColorRect
{
    [Signal]
    public delegate void DestroyedEventHandler(string upgradeType, Vector2 position);

    [Export] public float Speed { get; set; } = 82.0f;
    [Export] public int MaxHp { get; set; } = 25;
    [Export] public string UpgradeType { get; set; } = "attack_add";
    [Export] public string PickupKind { get; set; } = "attack_add";
    [Export] public string DisplayName { get; set; } = "";

    public int Hp { get; private set; }

    private Label _typeLabel = null!;
    private Label _hpLabel = null!;
    private ColorRect _hpBar = null!;
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
        Position += _moveDirection * Speed * (float)delta;

        if (_hitFlashTimer > 0.0f)
        {
            _hitFlashTimer -= (float)delta;
            float ratio = Mathf.Clamp(_hitFlashTimer / 0.12f, 0.0f, 1.0f);
            ApplyHitVisual(ratio);
            Scale = new Vector2(1.0f + ratio * 0.08f, 1.0f + ratio * 0.08f);
        }
        else
        {
            ResetHitVisual();
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
        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            return DisplayName;
        }

        return UpgradeType switch
        {
            "attack_add" => "ATK",
            "fire_rate" => "RATE",
            "heal" => "HEAL",
            "bullet_add" => "+SHOT",
            _ => "UP"
        };
    }

    public void SetTrackLine(Vector2 centerStart, Vector2 centerEnd)
    {
        Position = centerStart - Size * 0.5f;
        Vector2 direction = centerEnd - centerStart;
        _moveDirection = direction.LengthSquared() > 0.01f ? direction.Normalized() : Vector2.Down;
    }

    private void SetupTextureVisual()
    {
        string texturePath = GetTexturePath();
        Texture2D texture = GD.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            GD.PushWarning($"Pickup texture not found: {texturePath}");
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

    private string GetTexturePath()
    {
        string kind = string.IsNullOrWhiteSpace(PickupKind) ? UpgradeType : PickupKind;
        return $"res://resource/figure/pickups/{NormalizeTextureKey(kind)}.png";
    }

    private static string NormalizeTextureKey(string value)
    {
        return value.Trim().Replace("/", "").Replace("\\", "").Replace("..", "");
    }

    private void ApplyHitVisual(float ratio)
    {
        Color flash = new(1.0f, 0.94f, 0.78f, 1.0f);
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
