namespace AncientLife.Models.Events;

public sealed record EventResolution(
    EventData ResolvedEvent,
    EventChoice SelectedChoice,
    EventData? NextEvent,
    bool IsComplete);
