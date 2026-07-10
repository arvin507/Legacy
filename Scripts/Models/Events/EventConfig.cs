namespace AncientLife.Models.Events;

public sealed class EventConfig
{
    public double TriggerChance { get; init; }
    public IReadOnlyList<EventData> Events { get; init; } = [];
}
