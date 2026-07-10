using AncientLife.Models;
using AncientLife.Models.Calendar;
using AncientLife.Systems.Calendar;

namespace AncientLife.Managers;

public sealed class EraManager
{
    private readonly IReadOnlyList<string> _eras;
    private readonly Random _random;

    public EraManager(EraConfig config, Random? random = null)
    {
        if (config.Eras.Count == 0)
        {
            throw new ArgumentException("At least one era is required.", nameof(config));
        }

        _eras = config.Eras;
        _random = random ?? Random.Shared;
    }

    public string SelectRandomEra(string? excludedEra = null)
    {
        var candidates = _eras.Count > 1 && !string.IsNullOrWhiteSpace(excludedEra)
            ? _eras.Where(era => !string.Equals(era, excludedEra, StringComparison.Ordinal)).ToArray()
            : _eras.ToArray();
        return candidates[_random.Next(candidates.Length)];
    }

    public void BeginNewEra(CalendarState calendar, string? eraName = null, string? excludedEra = null)
    {
        var selectedEra = string.IsNullOrWhiteSpace(eraName)
            ? SelectRandomEra(excludedEra)
            : eraName;
        if (!_eras.Contains(selectedEra, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Era '{selectedEra}' is not configured.", nameof(eraName));
        }

        calendar.EraName = selectedEra;
        calendar.Year = 1;
        RefreshDisplayYear(calendar);
    }

    public void ChangeEra(CalendarState calendar, string? eraName = null)
    {
        BeginNewEra(calendar, eraName, calendar.EraName);
    }

    public void RefreshDisplayYear(CalendarState calendar)
    {
        calendar.EraYearName = $"{calendar.EraName}{AncientYearFormatter.FormatEraYear(calendar.Year)}年";
    }
}
