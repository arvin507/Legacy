namespace AncientLife.Models.Calendar;

public sealed class CalendarConfig
{
    public IReadOnlyList<MonthData> Months { get; init; } = [];
}
