using Godot;

public partial class GameManager : Node
{
    private const string BattleBgmPath = "res://resource/music/bg.mp3";

    public int Scrap { get; private set; } = 100;
    public int Fuel { get; private set; } = 50;
    public int Food { get; private set; } = 30;
    public int Parts { get; private set; } = 0;
    public int CannonLevel { get; private set; } = 1;
    public int TrainLevel { get; private set; } = 1;
    public int CurrentStation { get; private set; } = 1;
    public int TrainMaxHp { get; private set; } = 100;
    public int TrainCurrentHp { get; private set; } = 100;
    private AudioStreamPlayer _bgmPlayer = null!;

    public override void _Ready()
    {
        _bgmPlayer = new AudioStreamPlayer
        {
            Name = "BgmPlayer",
            Stream = GD.Load<AudioStream>(BattleBgmPath),
            Bus = "Master"
        };
        AddChild(_bgmPlayer);

        if (_bgmPlayer.Stream is AudioStreamMP3 mp3Stream)
        {
            mp3Stream.Loop = true;
        }
    }

    public void PlayGameBgm()
    {
        if (_bgmPlayer == null || _bgmPlayer.Stream == null || _bgmPlayer.Playing)
        {
            return;
        }

        _bgmPlayer.Play();
    }

    public void StopGameBgm()
    {
        if (_bgmPlayer == null || !_bgmPlayer.Playing)
        {
            return;
        }

        _bgmPlayer.Stop();
    }

    public int GetCannonUpgradeCost()
    {
        return CannonLevel * 50;
    }

    public int GetCannonDamage()
    {
        return 10 + (CannonLevel - 1) * 5;
    }

    public int GetCannonDamageBonus()
    {
        return (CannonLevel - 1) * 5;
    }

    public float GetCannonFireInterval()
    {
        return Mathf.Max(0.18f, 0.75f - (CannonLevel - 1) * 0.08f);
    }

    public void DamageTrain(int damage)
    {
        TrainCurrentHp = Mathf.Max(0, TrainCurrentHp - damage);
    }

    public void HealTrain(int amount)
    {
        TrainCurrentHp = Mathf.Min(TrainMaxHp, TrainCurrentHp + amount);
    }

    public void RecoverTrainForNextAttempt()
    {
        TrainCurrentHp = TrainMaxHp;
    }

    public void PrepareBattleAttempt()
    {
        if (TrainCurrentHp <= 0)
        {
            RecoverTrainForNextAttempt();
        }
    }

    public void CompleteStation(int scrapReward, int fuelReward, int foodReward, int partsReward)
    {
        Scrap += scrapReward;
        Fuel += fuelReward;
        Food += foodReward;
        Parts += partsReward;
        CurrentStation += 1;
    }

    public bool TryUpgradeCannon()
    {
        int cost = GetCannonUpgradeCost();
        if (Scrap < cost)
        {
            return false;
        }

        Scrap -= cost;
        CannonLevel += 1;
        return true;
    }
}
