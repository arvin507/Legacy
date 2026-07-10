namespace AncientLife.Models.Events;

public sealed class ConditionData
{
    public required string Field { get; init; }
    public required string Operator { get; init; }
    public required string Value { get; init; }
}
