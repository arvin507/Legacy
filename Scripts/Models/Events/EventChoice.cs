namespace AncientLife.Models.Events;

public sealed class EventChoice
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public IReadOnlyList<RewardData> Rewards { get; init; } = [];
    public string? NextEventId { get; init; }
    public bool CloseEvent { get; init; } = true;
}
