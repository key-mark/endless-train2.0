using Godot;

public partial class GameManager : Node
{
    public int Scrap { get; private set; } = 100;
    public int Fuel { get; private set; } = 50;
    public int Food { get; private set; } = 30;
    public int Parts { get; private set; } = 0;
    public int CannonLevel { get; private set; } = 1;
    public int TrainLevel { get; private set; } = 1;
    public int CurrentStation { get; private set; } = 1;
    public int TrainMaxHp { get; private set; } = 100;
    public int TrainCurrentHp { get; private set; } = 100;

    public int GetCannonUpgradeCost()
    {
        return CannonLevel * 50;
    }

    public int GetCannonDamage()
    {
        return 10 + (CannonLevel - 1) * 5;
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
