namespace AncientLife.Models;

public sealed class ProfessionDefinition
{
  public required string Id { get; init; }
  public required string Name { get; init; }
  public required string Description { get; init; }
  public required string Track { get; init; }
  public int Tier { get; init; }
  public int MonthlyMoney { get; init; }
  public int MonthlyFood { get; init; }
  public int RequiredFarmingSkill { get; init; }
  public int RequiredScholarshipSkill { get; init; }
  public int RequiredCulture { get; init; }
  public int RequiredMoney { get; init; }
  public int PromotionCost { get; init; }
  public string? NextProfessionId { get; init; }
}
