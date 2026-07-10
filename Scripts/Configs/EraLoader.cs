using System.Text.Json;
using System.Text.Json.Serialization;
using AncientLife.Models.Calendar;
using GodotFileAccess = Godot.FileAccess;

namespace AncientLife.Configs;

public static class EraLoader
{
    private const string DefaultPath = "res://Configs/Eras.json";

    public static EraConfig Load(string path = DefaultPath)
    {
        if (!GodotFileAccess.FileExists(path))
        {
            throw new FileNotFoundException($"Era configuration was not found: {path}");
        }

        var source = JsonSerializer.Deserialize<EraConfigEntry>(GodotFileAccess.GetFileAsString(path))
            ?? throw new InvalidDataException("Era configuration is empty.");
        var eras = source.Eras
            .Select(era => era.Trim())
            .Where(era => !string.IsNullOrWhiteSpace(era))
            .ToArray();

        if (eras.Length == 0)
        {
            throw new InvalidDataException("At least one era must be configured.");
        }

        if (eras.Distinct(StringComparer.Ordinal).Count() != eras.Length)
        {
            throw new InvalidDataException("Era names must be unique.");
        }

        return new EraConfig { Eras = eras };
    }

    private sealed class EraConfigEntry
    {
        [JsonPropertyName("eras")]
        public List<string> Eras { get; init; } = [];
    }
}
