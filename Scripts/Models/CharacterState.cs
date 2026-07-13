namespace AncientLife.Models;

public sealed class CharacterState
{
  public string Name { get; private set; } = "李二";
  public int Age { get; internal set; } = 16;
  public int MaxEnergy { get; internal set; } = 10;
  public int Energy { get; internal set; } = 10;
  public int MaxHealth { get; internal set; } = 100;
  public int Health { get; internal set; } = 100;
  public int Money { get; internal set; } = 100;
  public int Food { get; internal set; } = 10;
  public int Culture { get; internal set; }
  public int FarmingSkill { get; internal set; }
  public int ScholarshipSkill { get; internal set; }
  public int Reputation { get; internal set; }
  public string ProfessionId { get; internal set; } = "commoner";
  public string ProfessionName { get; internal set; } = "平民";

  public static CharacterState CreateDefault() => new();

  public int GetSkill(SkillType skillType) => skillType switch
  {
    SkillType.Farming => FarmingSkill,
    SkillType.Scholarship => ScholarshipSkill,
    _ => 0
  };

  public void AddSkill(SkillType skillType, int amount)
  {
    if (amount <= 0)
    {
      return;
    }

    switch (skillType)
    {
      case SkillType.Farming:
        FarmingSkill += amount;
        break;
      case SkillType.Scholarship:
        ScholarshipSkill += amount;
        break;
    }
  }

  public void RestoreMonthlyEnergy()
  {
    Energy = MaxEnergy;
  }
}
