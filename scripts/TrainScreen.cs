using Godot;

public partial class TrainScreen : Control
{
    private Label _resourcesLabel = null!;
    private Label _levelsLabel = null!;
    private Label _hpLabel = null!;
    private Label _messageLabel = null!;
    private Button _upgradeCannonButton = null!;
    private Button _goBattleButton = null!;

    public override void _Ready()
    {
        _resourcesLabel = GetNode<Label>("ResourcePlaceholder/ResourceLabel");
        _levelsLabel = GetNode<Label>("StatsPanel/LevelsLabel");
        _hpLabel = GetNode<Label>("StatsPanel/HpLabel");
        _messageLabel = GetNode<Label>("MessageLabel");
        _upgradeCannonButton = GetNode<Button>("CannonUpgradeButton");
        _goBattleButton = GetNode<Button>("GoBattleButton");

        _upgradeCannonButton.Pressed += OnUpgradeCannonPressed;
        _goBattleButton.Pressed += OnGoBattlePressed;

        RefreshDisplay();
    }

    private GameManager State => GetNode<GameManager>("/root/GameManager");

    private void RefreshDisplay()
    {
        GameManager state = State;
        _resourcesLabel.Text = $"Scrap: {state.Scrap}   Fuel: {state.Fuel}\nFood: {state.Food}   Parts: {state.Parts}";
        _levelsLabel.Text = $"Train Lv: {state.TrainLevel}   Cannon Lv: {state.CannonLevel}\nStation: {state.CurrentStation}";
        _hpLabel.Text = $"Train HP: {state.TrainCurrentHp} / {state.TrainMaxHp}";
        _upgradeCannonButton.Text = $"Upgrade Cannon ({state.GetCannonUpgradeCost()} Scrap)";
    }

    private void OnUpgradeCannonPressed()
    {
        if (State.TryUpgradeCannon())
        {
            _messageLabel.Text = "Cannon upgraded.";
        }
        else
        {
            _messageLabel.Text = "Not enough Scrap.";
        }

        RefreshDisplay();
    }

    private void OnGoBattlePressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/BattleScreen.tscn");
    }
}
