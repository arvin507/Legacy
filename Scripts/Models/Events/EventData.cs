namespace AncientLife.Models.Events;

public sealed class EventData
{
  public required string Id { get; init; }
  public required string Title { get; init; }
  public required string Description { get; init; }
  public string IllustrationPath { get; init; } = string.Empty;
  public int Weight { get; init; }
  public bool Unique { get; init; }
  public int CooldownMonths { get; init; }
  public IReadOnlyList<string> Tags { get; init; } = [];
  public IReadOnlyList<ConditionData> Conditions { get; init; } = [];
  public IReadOnlyList<EventChoice> Choices { get; init; } = [];
}
