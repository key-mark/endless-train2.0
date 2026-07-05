using Godot;

public partial class StartMenu : Control
{
    private const string BattleScreenPath = "res://scenes/BattleScreen.tscn";
    private GameManager State => GetNode<GameManager>("/root/GameManager");

    public override void _Ready()
    {
        GetNode<Button>("StartButton").Pressed += OnStartPressed;
        GetNode<Button>("QuitButton").Pressed += OnQuitPressed;
    }

    private void OnStartPressed()
    {
        State.PlayGameBgm();
        GetTree().ChangeSceneToFile(BattleScreenPath);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
