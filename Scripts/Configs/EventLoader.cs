using System.Text.Json;
using System.Text.Json.Serialization;
using AncientLife.Models.Events;
using GodotFileAccess = Godot.FileAccess;

namespace AncientLife.Configs;

public static class EventLoader
{
    private const string DefaultPath = "res://Configs/Events.json";

    public static EventConfig Load(string path = DefaultPath)
    {
        if (!GodotFileAccess.FileExists(path))
        {
            throw new FileNotFoundException($"Event configuration was not found: {path}");
        }

        var json = GodotFileAccess.GetFileAsString(path);
        var source = JsonSerializer.Deserialize<EventConfigEntry>(json)
            ?? throw new InvalidDataException("Event configuration is empty.");

        var config = new EventConfig
        {
            TriggerChance = source.TriggerChance,
            Events = source.Events.Select(ToEventData).ToArray()
        };

        Validate(config);
        return config;
    }

    private static EventData ToEventData(EventEntry entry) => new()
    {
        Id = entry.Id,
        Title = entry.Title,
        Description = entry.Description,
        IllustrationPath = entry.IllustrationPath,
        Weight = entry.Weight,
        Unique = entry.Unique,
        CooldownMonths = entry.CooldownMonths,
        Tags = entry.Tags,
        Conditions = entry.Conditions.Select(condition => new ConditionData
        {
            Field = condition.Field,
            Operator = condition.Operator,
            Value = condition.Value.ToString()
        }).ToArray(),
        Choices = entry.Choices.Select(choice => new EventChoice
        {
            Id = choice.Id,
            Text = choice.Text,
            Rewards = choice.Rewards.Select(reward => new RewardData
            {
                Type = reward.Type,
                Amount = reward.Amount,
                Key = reward.Key,
                Value = reward.Value
            }).ToArray(),
            NextEventId = choice.NextEventId,
            CloseEvent = choice.CloseEvent
        }).ToArray()
    };

    private static void Validate(EventConfig config)
    {
        if (config.TriggerChance is < 0 or > 1)
        {
            throw new InvalidDataException("Event trigger_chance must be between 0 and 1.");
        }

        if (config.Events.Count == 0)
        {
            throw new InvalidDataException("At least one event must be configured.");
        }

        var duplicateEventId = config.Events
            .GroupBy(gameEvent => gameEvent.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateEventId is not null)
        {
            throw new InvalidDataException($"Duplicate event id: {duplicateEventId}");
        }

        var eventIds = config.Events.Select(gameEvent => gameEvent.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var gameEvent in config.Events)
        {
            if (string.IsNullOrWhiteSpace(gameEvent.Id) ||
                string.IsNullOrWhiteSpace(gameEvent.Title) ||
                string.IsNullOrWhiteSpace(gameEvent.Description))
            {
                throw new InvalidDataException("Event id, title and description are required.");
            }

            if (gameEvent.Weight <= 0)
            {
                throw new InvalidDataException($"Event '{gameEvent.Id}' must have a positive weight.");
            }

            if (gameEvent.CooldownMonths < 0)
            {
                throw new InvalidDataException($"Event '{gameEvent.Id}' has a negative cooldown.");
            }

            if (gameEvent.Choices.Count is < 2 or > 4)
            {
                throw new InvalidDataException($"Event '{gameEvent.Id}' must contain between 2 and 4 choices.");
            }

            var duplicateChoiceId = gameEvent.Choices
                .GroupBy(choice => choice.Id, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1)?.Key;
            if (duplicateChoiceId is not null)
            {
                throw new InvalidDataException($"Event '{gameEvent.Id}' has duplicate choice id '{duplicateChoiceId}'.");
            }

            foreach (var choice in gameEvent.Choices)
            {
                if (string.IsNullOrWhiteSpace(choice.Id) || string.IsNullOrWhiteSpace(choice.Text))
                {
                    throw new InvalidDataException($"Event '{gameEvent.Id}' has a choice without id or text.");
                }

                if (!string.IsNullOrWhiteSpace(choice.NextEventId) && !eventIds.Contains(choice.NextEventId))
                {
                    throw new InvalidDataException(
                        $"Event '{gameEvent.Id}' points to unknown next event '{choice.NextEventId}'.");
                }

                if (!choice.CloseEvent && string.IsNullOrWhiteSpace(choice.NextEventId))
                {
                    throw new InvalidDataException(
                        $"Event '{gameEvent.Id}' choice '{choice.Id}' must close or continue to another event.");
                }

                if (choice.Rewards.Any(reward => string.IsNullOrWhiteSpace(reward.Type)))
                {
                    throw new InvalidDataException($"Event '{gameEvent.Id}' has a reward without type.");
                }
            }
        }
    }

    private sealed class EventConfigEntry
    {
        [JsonPropertyName("trigger_chance")]
        public double TriggerChance { get; init; }

        [JsonPropertyName("events")]
        public List<EventEntry> Events { get; init; } = [];
    }

    private sealed class EventEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("illustration_path")]
        public string IllustrationPath { get; init; } = string.Empty;

        [JsonPropertyName("weight")]
        public int Weight { get; init; }

        [JsonPropertyName("unique")]
        public bool Unique { get; init; }

        [JsonPropertyName("cooldown_months")]
        public int CooldownMonths { get; init; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; init; } = [];

        [JsonPropertyName("conditions")]
        public List<ConditionEntry> Conditions { get; init; } = [];

        [JsonPropertyName("choices")]
        public List<ChoiceEntry> Choices { get; init; } = [];
    }

    private sealed class ConditionEntry
    {
        [JsonPropertyName("field")]
        public string Field { get; init; } = string.Empty;

        [JsonPropertyName("operator")]
        public string Operator { get; init; } = string.Empty;

        [JsonPropertyName("value")]
        public JsonElement Value { get; init; }
    }

    private sealed class ChoiceEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;

        [JsonPropertyName("rewards")]
        public List<RewardEntry> Rewards { get; init; } = [];

        [JsonPropertyName("nextEventId")]
        public string? NextEventId { get; init; }

        [JsonPropertyName("closeEvent")]
        public bool CloseEvent { get; init; } = true;
    }

    private sealed class RewardEntry
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("amount")]
        public int Amount { get; init; }

        [JsonPropertyName("key")]
        public string? Key { get; init; }

        [JsonPropertyName("value")]
        public string? Value { get; init; }
    }
}
