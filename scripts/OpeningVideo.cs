using Godot;

public partial class OpeningVideo : Control
{
    private const string VideoDirectory = "res://resource/video";
    private const string StartMenuPath = "res://scenes/StartMenu.tscn";

    private VideoStreamPlayer _videoPlayer = null!;
    private bool _transitioned;

    public override void _Ready()
    {
        _videoPlayer = GetNode<VideoStreamPlayer>("VideoPlayer");
        _videoPlayer.Finished += GoToStartMenu;

        string videoPath = FindOpeningVideoPath();
        if (string.IsNullOrEmpty(videoPath))
        {
            GD.PushWarning($"Opening video not found in {VideoDirectory}.");
            GoToStartMenu();
            return;
        }

        VideoStream stream = ResourceLoader.Load<VideoStream>(videoPath);
        if (stream == null)
        {
            GD.PushWarning($"Opening video cannot be loaded as a VideoStream: {videoPath}");
            GoToStartMenu();
            return;
        }

        _videoPlayer.Stream = stream;
        _videoPlayer.Play();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
        {
            GoToStartMenu();
            return;
        }

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            GoToStartMenu();
            return;
        }

        if (@event is InputEventScreenTouch screenTouch && screenTouch.Pressed)
        {
            GoToStartMenu();
        }
    }

    private static string FindOpeningVideoPath()
    {
        DirAccess dir = DirAccess.Open(VideoDirectory);
        if (dir == null)
        {
            return string.Empty;
        }

        string mp4Fallback = string.Empty;
        dir.ListDirBegin();
        while (true)
        {
            string fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName))
            {
                break;
            }

            if (dir.CurrentIsDir() || fileName.EndsWith(".import"))
            {
                continue;
            }

            string lowerName = fileName.ToLowerInvariant();
            if (lowerName.EndsWith(".ogv") || lowerName.EndsWith(".webm"))
            {
                dir.ListDirEnd();
                return $"{VideoDirectory}/{fileName}";
            }

            if (lowerName.EndsWith(".mp4") && string.IsNullOrEmpty(mp4Fallback))
            {
                mp4Fallback = $"{VideoDirectory}/{fileName}";
            }
        }

        dir.ListDirEnd();
        return mp4Fallback;
    }

    private void GoToStartMenu()
    {
        if (_transitioned)
        {
            return;
        }

        _transitioned = true;
        GetTree().ChangeSceneToFile(StartMenuPath);
    }
}
