using AncientLife.Models;

namespace AncientLife.Systems;

public sealed class AgingSystem
{
  public AgingResult ApplyMonthlyWear(CharacterState character)
  {
    var healthCost = character.Age switch
    {
      >= 80 => 4,
      >= 70 => 2,
      >= 60 => 1,
      _ => 0
    };

    if (healthCost == 0)
    {
      return AgingResult.None;
    }

    var previousHealth = character.Health;
    character.Health = Math.Max(0, character.Health - healthCost);
    var actualChange = character.Health - previousHealth;
    return new AgingResult($"年岁渐高，本月健康下降了 {-actualChange} 点。", actualChange);
  }

  public AgingResult ApplyBirthday(CharacterState character)
  {
    var healthLoss = character.Age switch
    {
      >= 80 => 5,
      >= 70 => 4,
      >= 60 => 3,
      >= 50 => 2,
      >= 40 => 1,
      _ => 0
    };
    var energyLoss = character.Age >= 50 && character.Age % 5 == 0 ? 1 : 0;

    if (healthLoss == 0 && energyLoss == 0)
    {
      return AgingResult.None;
    }

    var previousMaxHealth = character.MaxHealth;
    var previousMaxEnergy = character.MaxEnergy;
    character.MaxHealth = Math.Max(20, character.MaxHealth - healthLoss);
    character.MaxEnergy = Math.Max(4, character.MaxEnergy - energyLoss);
    character.Health = Math.Min(character.Health, character.MaxHealth);
    character.Energy = Math.Min(character.Energy, character.MaxEnergy);

    var maxHealthChange = character.MaxHealth - previousMaxHealth;
    var maxEnergyChange = character.MaxEnergy - previousMaxEnergy;
    var parts = new List<string>();
    if (maxHealthChange < 0)
    {
      parts.Add($"健康上限下降了 {-maxHealthChange} 点");
    }

    if (maxEnergyChange < 0)
    {
      parts.Add($"体力上限下降了 {-maxEnergyChange} 点");
    }

    return new AgingResult(
      $"你已年满 {character.Age} 岁，{string.Join("，", parts)}。",
      0,
      maxHealthChange,
      maxEnergyChange);
  }
}
