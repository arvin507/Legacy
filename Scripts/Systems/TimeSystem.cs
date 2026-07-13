using AncientLife.Managers;
using AncientLife.Models;
using AncientLife.Models.Calendar;

namespace AncientLife.Systems;

public sealed class TimeSystem
{
  private readonly CalendarConfig _config;
  private readonly EraManager _eraManager;
  private readonly Random _monthlyOrderRandom;

  public TimeSystem(CalendarConfig config, EraManager eraManager, Random? monthlyOrderRandom = null)
  {
    _config = config;
    _eraManager = eraManager;
    _monthlyOrderRandom = monthlyOrderRandom ?? Random.Shared;
  }

  public int MonthsPerYear => _config.Months.Count;

  public CalendarState CreateCalendar(string? excludedEra = null)
  {
    var calendar = CalendarState.CreateDefault();
    _eraManager.BeginNewEra(calendar, excludedEra: excludedEra);
    ApplyMonth(calendar);
    return calendar;
  }

  public void CompleteCurrentMonth(CalendarState calendar, CharacterState character)
  {
    calendar.TotalMonthsSurvived++;
    calendar.Month++;
    if (calendar.Month <= MonthsPerYear)
    {
      ApplyMonth(calendar);
      return;
    }

    calendar.Month = 1;
    calendar.Year++;
    character.Age++;
    _eraManager.RefreshDisplayYear(calendar);
    ApplyMonth(calendar);
  }

  private void ApplyMonth(CalendarState calendar)
  {
    var month = _config.Months[calendar.Month - 1];
    calendar.MonthName = month.Name;
    calendar.Season = month.Season;
    calendar.MonthlyOrder = month.MonthlyOrders[_monthlyOrderRandom.Next(month.MonthlyOrders.Count)];
  }
}
