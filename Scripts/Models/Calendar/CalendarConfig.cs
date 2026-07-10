namespace AncientLife.Models.Calendar;

public sealed class CalendarConfig
{
    public int DaysPerMonth { get; init; }
    public IReadOnlyList<MonthData> Months { get; init; } = [];
}
