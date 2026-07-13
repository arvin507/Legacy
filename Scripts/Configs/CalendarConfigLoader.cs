using System.Text.Json;
using System.Text.Json.Serialization;
using AncientLife.Models;
using AncientLife.Models.Calendar;
using GodotFileAccess = Godot.FileAccess;

namespace AncientLife.Configs;

public static class CalendarConfigLoader
{
    private const string DefaultPath = "res://Configs/Calendar.json";

    public static CalendarConfig Load(string path = DefaultPath)
    {
        if (!GodotFileAccess.FileExists(path))
        {
            throw new FileNotFoundException($"Calendar configuration was not found: {path}");
        }

        var source = JsonSerializer.Deserialize<CalendarConfigEntry>(GodotFileAccess.GetFileAsString(path))
            ?? throw new InvalidDataException("Calendar configuration is empty.");
        var months = source.Months.Select(ToMonthData).ToArray();

        if (months.Length != 12)
        {
            throw new InvalidDataException("The ancient calendar must contain exactly 12 months.");
        }

        if (months.Select(month => month.Name).Distinct(StringComparer.Ordinal).Count() != months.Length)
        {
            throw new InvalidDataException("Month names must be unique.");
        }

        if (months.Any(month => month.MonthlyOrders.Count == 0))
        {
            throw new InvalidDataException("Every month must contain at least one monthly order.");
        }

        return new CalendarConfig
        {
            Months = months
        };
    }

    private static MonthData ToMonthData(MonthEntry entry)
    {
        var season = entry.Season switch
        {
            "春" => Season.Spring,
            "夏" => Season.Summer,
            "秋" => Season.Autumn,
            "冬" => Season.Winter,
            _ => throw new InvalidDataException($"Unknown season '{entry.Season}' for month '{entry.Name}'.")
        };

        return new MonthData
        {
            Name = entry.Name.Trim(),
            Season = season,
            MonthlyOrders = entry.MonthlyOrders
                .Select(order => order.Trim())
                .Where(order => !string.IsNullOrWhiteSpace(order))
                .ToArray()
        };
    }

    private sealed class CalendarConfigEntry
    {
        [JsonPropertyName("months")]
        public List<MonthEntry> Months { get; init; } = [];
    }

    private sealed class MonthEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("season")]
        public string Season { get; init; } = string.Empty;

        [JsonPropertyName("monthly_orders")]
        public List<string> MonthlyOrders { get; init; } = [];
    }
}
