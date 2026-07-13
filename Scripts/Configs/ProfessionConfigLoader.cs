using System.Text.Json;
using System.Text.Json.Serialization;
using AncientLife.Models;
using Godot;
using GodotFileAccess = Godot.FileAccess;

namespace AncientLife.Configs;

public static class ProfessionConfigLoader
{
  private const string DefaultPath = "res://Configs/Professions.json";

  public static IReadOnlyList<ProfessionDefinition> Load(string path = DefaultPath)
  {
    if (!GodotFileAccess.FileExists(path))
    {
      throw new FileNotFoundException($"Profession configuration was not found: {path}");
    }

    var json = GodotFileAccess.GetFileAsString(path);
    var entries = JsonSerializer.Deserialize<List<ProfessionConfigEntry>>(json) ?? [];
    var definitions = entries.Select(ToDefinition).ToArray();
    Validate(definitions);
    return definitions;
  }

  private static ProfessionDefinition ToDefinition(ProfessionConfigEntry entry) => new()
  {
    Id = entry.Id,
    Name = entry.Name,
    Description = entry.Description,
    Track = entry.Track,
    Tier = entry.Tier,
    MonthlyMoney = entry.MonthlyMoney,
    MonthlyFood = entry.MonthlyFood,
    RequiredFarmingSkill = entry.RequiredFarmingSkill,
    RequiredScholarshipSkill = entry.RequiredScholarshipSkill,
    RequiredCulture = entry.RequiredCulture,
    RequiredMoney = entry.RequiredMoney,
    PromotionCost = entry.PromotionCost,
    NextProfessionId = entry.NextProfessionId
  };

  private static void Validate(IReadOnlyList<ProfessionDefinition> definitions)
  {
    if (definitions.Count == 0 || definitions.All(definition => definition.Id != "commoner"))
    {
      throw new InvalidDataException("Profession configuration must contain the commoner profession.");
    }

    var duplicateId = definitions
      .GroupBy(definition => definition.Id, StringComparer.Ordinal)
      .FirstOrDefault(group => group.Count() > 1)?.Key;
    if (duplicateId is not null)
    {
      throw new InvalidDataException($"Duplicate profession id: {duplicateId}");
    }

    var ids = definitions.Select(definition => definition.Id).ToHashSet(StringComparer.Ordinal);
    foreach (var definition in definitions)
    {
      if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name))
      {
        throw new InvalidDataException("Profession id and name are required.");
      }

      if (definition.PromotionCost < 0 || definition.MonthlyMoney < 0 || definition.MonthlyFood < 0)
      {
        throw new InvalidDataException($"Profession '{definition.Id}' contains invalid numeric values.");
      }

      if (definition.NextProfessionId is not null && !ids.Contains(definition.NextProfessionId))
      {
        throw new InvalidDataException(
          $"Profession '{definition.Id}' references unknown next profession '{definition.NextProfessionId}'.");
      }
    }
  }

  private sealed class ProfessionConfigEntry
  {
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("track")]
    public string Track { get; init; } = string.Empty;

    [JsonPropertyName("tier")]
    public int Tier { get; init; }

    [JsonPropertyName("monthly_money")]
    public int MonthlyMoney { get; init; }

    [JsonPropertyName("monthly_food")]
    public int MonthlyFood { get; init; }

    [JsonPropertyName("required_farming_skill")]
    public int RequiredFarmingSkill { get; init; }

    [JsonPropertyName("required_scholarship_skill")]
    public int RequiredScholarshipSkill { get; init; }

    [JsonPropertyName("required_culture")]
    public int RequiredCulture { get; init; }

    [JsonPropertyName("required_money")]
    public int RequiredMoney { get; init; }

    [JsonPropertyName("promotion_cost")]
    public int PromotionCost { get; init; }

    [JsonPropertyName("next_profession_id")]
    public string? NextProfessionId { get; init; }
  }
}
