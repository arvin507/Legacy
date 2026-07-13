using AncientLife.Models;

namespace AncientLife.Systems;

public sealed class ActionSystem
{
  private readonly Random _random;

  public ActionSystem(Random? random = null)
  {
    _random = random ?? Random.Shared;
  }

  public ActionAvailability GetAvailability(
    ActionDefinition definition,
    CharacterState character,
    IReadOnlyDictionary<string, int> monthlyUsage,
    bool isGameOver)
  {
    if (isGameOver)
    {
      return new(false, "本局已结束");
    }

    if (definition.MonthlyLimit > 0 &&
        monthlyUsage.GetValueOrDefault(definition.Id) >= definition.MonthlyLimit)
    {
      return new(false, "本月次数已用完");
    }

    if (definition.RewardType == RewardType.Energy && character.Energy >= character.MaxEnergy)
    {
      return new(false, "体力已满");
    }

    if (character.Energy < definition.EnergyCost)
    {
      return new(false, "体力不足");
    }

    return ActionAvailability.Available;
  }

  public ActionResult Perform(
    ActionDefinition definition,
    CharacterState character,
    IDictionary<string, int> monthlyUsage,
    bool isGameOver)
  {
    var availability = GetAvailability(
      definition,
      character,
      new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(monthlyUsage),
      isGameOver);
    if (!availability.CanPerform)
    {
      return ActionResult.Failed(definition.Id, availability.Reason);
    }

    character.Energy -= definition.EnergyCost;
    var baseReward = _random.Next(definition.RewardMin, definition.RewardMax + 1);
    var skillBonus = CalculateSkillBonus(definition, character);
    var reward = baseReward + skillBonus;

    switch (definition.RewardType)
    {
      case RewardType.Money:
        character.Money += reward;
        break;
      case RewardType.Culture:
        character.Culture += reward;
        break;
      case RewardType.Energy:
        var energyBeforeRecovery = character.Energy;
        character.Energy = Math.Min(character.MaxEnergy, character.Energy + reward);
        reward = character.Energy - energyBeforeRecovery;
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(definition.RewardType));
    }

    character.AddSkill(definition.SkillType, definition.SkillGain);
    monthlyUsage.TryGetValue(definition.Id, out var usageCount);
    monthlyUsage[definition.Id] = usageCount + 1;
    return new ActionResult(
      true,
      definition.Id,
      BuildResultMessage(definition, reward),
      reward,
      definition.SkillType,
      definition.SkillGain);
  }

  private static int CalculateSkillBonus(ActionDefinition definition, CharacterState character)
  {
    var skill = character.GetSkill(definition.SkillType);
    return definition.RewardType switch
    {
      RewardType.Money => skill / 12,
      RewardType.Culture => skill / 15,
      _ => 0
    };
  }

  private static string BuildResultMessage(ActionDefinition definition, int reward)
  {
    var rewardMessage = definition.RewardType switch
    {
      RewardType.Money when reward == 0 => "本月未有收获",
      RewardType.Money => $"获得 {reward} 文",
      RewardType.Culture => $"文化增加 {reward}",
      RewardType.Energy => $"体力恢复 {reward}",
      _ => string.Empty
    };
    var skillMessage = definition.SkillType switch
    {
      SkillType.Farming when definition.SkillGain > 0 => $"，农事 +{definition.SkillGain}",
      SkillType.Scholarship when definition.SkillGain > 0 => $"，学识 +{definition.SkillGain}",
      _ => string.Empty
    };
    return $"{definition.Name} · {rewardMessage}{skillMessage}";
  }
}
