namespace AncientLife.Models;

public sealed class ActionDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int EnergyCost { get; init; }
    public RewardType RewardType { get; init; }
    public int RewardMin { get; init; }
    public int RewardMax { get; init; }
    public required string RewardLabel { get; init; }
    public int DailyLimit { get; init; }
    public required string IconPath { get; init; }
    public required string AccentColor { get; init; }
}
