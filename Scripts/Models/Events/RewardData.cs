namespace AncientLife.Models.Events;

public sealed class RewardData
{
    public required string Type { get; init; }
    public int Amount { get; init; }
    public string? Key { get; init; }
    public string? Value { get; init; }
}
