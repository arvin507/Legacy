using System.Text.Json;
using System.Text.Json.Serialization;
using AncientLife.Models;
using Godot;
using GodotFileAccess = Godot.FileAccess;

namespace AncientLife.Configs;

public static class ActionConfigLoader
{
  private const string DefaultPath = "res://Configs/actions.json";

  public static IReadOnlyList<ActionDefinition> Load(string path = DefaultPath)
  {
    if (!GodotFileAccess.FileExists(path))
    {
      throw new FileNotFoundException($"Action configuration was not found: {path}");
    }

    var json = GodotFileAccess.GetFileAsString(path);
    var entries = JsonSerializer.Deserialize<List<ActionConfigEntry>>(json) ?? [];
    var actions = entries.Select(ToDefinition).ToArray();
    Validate(actions);
    return actions;
  }

  private static ActionDefinition ToDefinition(ActionConfigEntry entry)
  {
    if (!Enum.TryParse<RewardType>(entry.RewardType, true, out var rewardType))
    {
      throw new InvalidDataException($"Unknown reward type '{entry.RewardType}' in action '{entry.Id}'.");
    }

    if (!Enum.TryParse<SkillType>(entry.SkillType, true, out var skillType))
    {
      throw new InvalidDataException($"Unknown skill type '{entry.SkillType}' in action '{entry.Id}'.");
    }

    return new ActionDefinition
    {
      Id = entry.Id,
      Name = entry.Name,
      Description = entry.Description,
      EnergyCost = entry.EnergyCost,
      RewardType = rewardType,
      RewardMin = entry.RewardMin,
      RewardMax = entry.RewardMax,
      RewardLabel = entry.RewardLabel,
      MonthlyLimit = entry.MonthlyLimit,
      SkillType = skillType,
      SkillGain = entry.SkillGain,
      IconPath = entry.IconPath,
      AccentColor = entry.AccentColor
    };
  }

  private static void Validate(IReadOnlyList<ActionDefinition> actions)
  {
    if (actions.Count == 0)
    {
      throw new InvalidDataException("At least one action must be configured.");
    }

    var duplicateId = actions
      .GroupBy(action => action.Id, StringComparer.Ordinal)
      .FirstOrDefault(group => group.Count() > 1)?.Key;
    if (duplicateId is not null)
    {
      throw new InvalidDataException($"Duplicate action id: {duplicateId}");
    }

    foreach (var action in actions)
    {
      if (string.IsNullOrWhiteSpace(action.Id) || string.IsNullOrWhiteSpace(action.Name))
      {
        throw new InvalidDataException("Action id and name are required.");
      }

      if (action.EnergyCost < 0 || action.RewardMin < 0 || action.RewardMax < action.RewardMin ||
          action.MonthlyLimit < 0 || action.SkillGain < 0)
      {
        throw new InvalidDataException($"Action '{action.Id}' contains an invalid numeric range.");
      }
    }
  }

  private sealed class ActionConfigEntry
  {
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("energy_cost")]
    public int EnergyCost { get; init; }

    [JsonPropertyName("reward_type")]
    public string RewardType { get; init; } = string.Empty;

    [JsonPropertyName("reward_min")]
    public int RewardMin { get; init; }

    [JsonPropertyName("reward_max")]
    public int RewardMax { get; init; }

    [JsonPropertyName("reward_label")]
    public string RewardLabel { get; init; } = string.Empty;

    [JsonPropertyName("monthly_limit")]
    public int MonthlyLimit { get; init; }

    [JsonPropertyName("skill_type")]
    public string SkillType { get; init; } = "none";

    [JsonPropertyName("skill_gain")]
    public int SkillGain { get; init; }

    [JsonPropertyName("icon_path")]
    public string IconPath { get; init; } = string.Empty;

    [JsonPropertyName("accent_color")]
    public string AccentColor { get; init; } = "#8C5B3E";
  }
}
