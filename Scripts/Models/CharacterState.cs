namespace AncientLife.Models;

public sealed class CharacterState
{
    public string Name { get; private set; } = "李二";
    public int Age { get; internal set; } = 16;
    public int MaxEnergy { get; private set; } = 10;
    public int Energy { get; internal set; } = 10;
    public int Health { get; internal set; } = 100;
    public int Money { get; internal set; } = 100;
    public int Food { get; internal set; } = 10;
    public int Culture { get; internal set; }

    public static CharacterState CreateDefault() => new();

    public void RestoreDailyEnergy()
    {
        Energy = MaxEnergy;
    }
}
