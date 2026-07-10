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
        IReadOnlyDictionary<string, int> dailyUsage,
        bool isGameOver)
    {
        if (isGameOver)
        {
            return new(false, "本局已结束");
        }

        if (definition.DailyLimit > 0 &&
            dailyUsage.GetValueOrDefault(definition.Id) >= definition.DailyLimit)
        {
            return new(false, "今日次数已用完");
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
        IDictionary<string, int> dailyUsage,
        bool isGameOver)
    {
        var availability = GetAvailability(
            definition,
            character,
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(dailyUsage),
            isGameOver);

        if (!availability.CanPerform)
        {
            return ActionResult.Failed(definition.Id, availability.Reason);
        }

        character.Energy -= definition.EnergyCost;
        var reward = _random.Next(definition.RewardMin, definition.RewardMax + 1);

        switch (definition.RewardType)
        {
            case RewardType.Money:
                character.Money += reward;
                break;
            case RewardType.Culture:
                character.Culture += reward;
                break;
            case RewardType.Energy:
                character.Energy = Math.Min(character.MaxEnergy, character.Energy + reward);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(definition.RewardType));
        }

        dailyUsage.TryGetValue(definition.Id, out var usageCount);
        dailyUsage[definition.Id] = usageCount + 1;
        return new(true, definition.Id, BuildResultMessage(definition, reward), reward);
    }

    private static string BuildResultMessage(ActionDefinition definition, int reward)
    {
        return definition.RewardType switch
        {
            RewardType.Money when reward == 0 => $"{definition.Name} · 今日未有收获",
            RewardType.Money => $"{definition.Name} · 获得 {reward} 文",
            RewardType.Culture => $"{definition.Name} · 文化增加 {reward}",
            RewardType.Energy => $"{definition.Name} · 体力恢复 {reward}",
            _ => definition.Name
        };
    }
}
