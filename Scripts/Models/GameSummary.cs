namespace AncientLife.Models;

public sealed record GameSummary(
  int MonthsSurvived,
  int FinalWealth,
  int Culture,
  int Age = 16,
  string ProfessionName = "平民",
  string EndingTitle = "平凡一生",
  IReadOnlyList<LifeRecord>? Records = null)
{
  public IReadOnlyList<LifeRecord> LifeRecords => Records ?? [];
}
