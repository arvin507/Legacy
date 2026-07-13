namespace AncientLife.Models;

public sealed class CalendarState
{
  public string EraName { get; internal set; } = string.Empty;
  public string EraYearName { get; internal set; } = string.Empty;
  public int Year { get; internal set; } = 1;
  public int Month { get; internal set; } = 1;
  public string MonthName { get; internal set; } = string.Empty;
  public string MonthlyOrder { get; internal set; } = string.Empty;
  public Season Season { get; internal set; } = Season.Spring;
  public int TotalMonthsSurvived { get; internal set; }

  public string SeasonName => Season switch
  {
    Season.Spring => "春",
    Season.Summer => "夏",
    Season.Autumn => "秋",
    Season.Winter => "冬",
    _ => "春"
  };

  public static CalendarState CreateDefault() => new();
}
