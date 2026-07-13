using AncientLife.Models;
using AncientLife.Models.Events;

namespace AncientLife.Systems.Events;

public sealed class RewardExecutor
{
  private readonly Dictionary<string, Action<CharacterState, RewardData>> _handlers =
    new(StringComparer.OrdinalIgnoreCase);

  public RewardExecutor()
  {
    Register("Money", (character, reward) =>
      character.Money = Math.Max(0, character.Money + reward.Amount));
    Register("Health", (character, reward) =>
      character.Health = Math.Clamp(character.Health + reward.Amount, 0, character.MaxHealth));
    Register("Culture", (character, reward) =>
      character.Culture = Math.Max(0, character.Culture + reward.Amount));
    Register("Stamina", (character, reward) =>
      character.Energy = Math.Clamp(character.Energy + reward.Amount, 0, character.MaxEnergy));
    Register("Energy", (character, reward) =>
      character.Energy = Math.Clamp(character.Energy + reward.Amount, 0, character.MaxEnergy));
    Register("FarmingSkill", (character, reward) =>
      character.FarmingSkill = Math.Max(0, character.FarmingSkill + reward.Amount));
    Register("ScholarshipSkill", (character, reward) =>
      character.ScholarshipSkill = Math.Max(0, character.ScholarshipSkill + reward.Amount));
    Register("Reputation", (character, reward) =>
      character.Reputation = Math.Max(0, character.Reputation + reward.Amount));
  }

  public void Register(string rewardType, Action<CharacterState, RewardData> handler)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(rewardType);
    ArgumentNullException.ThrowIfNull(handler);
    _handlers[rewardType] = handler;
  }

  public void Apply(IReadOnlyList<RewardData> rewards, CharacterState character)
  {
    foreach (var reward in rewards)
    {
      if (!_handlers.TryGetValue(reward.Type, out var handler))
      {
        throw new InvalidDataException($"Unsupported event reward type: {reward.Type}");
      }

      handler(character, reward);
    }
  }
}
