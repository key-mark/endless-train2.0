using Godot;

public partial class BattleScreen : Control
{
    private const float BattleWidth = 540.0f;
    private const float PlayerSideMargin = 52.0f;
    private const float PlayerMinX = PlayerSideMargin;
    private const float PlayerMaxX = BattleWidth - PlayerSideMargin;
    private const float PlayerStartX = 270.0f;

    private Control _playerRig = null!;
    private bool _isDragging;

    public override void _Ready()
    {
        _playerRig = GetNode<Control>("BattleArea540x960/PlayerRig");
        SetPlayerX(PlayerStartX);
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
}
