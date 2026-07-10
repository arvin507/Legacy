namespace AncientLife.Models.Calendar;

public sealed class MonthData
{
    public required string Name { get; init; }
    public Season Season { get; init; }
    public IReadOnlyList<string> MonthlyOrders { get; init; } = [];
}
