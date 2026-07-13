using AncientLife.Models;
using AncientLife.Models.Events;
using AncientLife.Systems.Events;

namespace AncientLife.Managers;

public sealed class EventManager
{
  private readonly EventConfig _config;
  private readonly IReadOnlyDictionary<string, EventData> _eventsById;
  private readonly ConditionEvaluator _conditionEvaluator;
  private readonly RewardExecutor _rewardExecutor;
  private readonly WeightedRandom _weightedRandom;
  private readonly Random _chanceRandom;
  private readonly Dictionary<string, int> _lastResolvedMonth = new(StringComparer.Ordinal);
  private readonly HashSet<string> _completedUniqueEvents = new(StringComparer.Ordinal);
  private int _currentMonth;

  public EventManager(
    EventConfig config,
    Random? chanceRandom = null,
    WeightedRandom? weightedRandom = null,
    ConditionEvaluator? conditionEvaluator = null,
    RewardExecutor? rewardExecutor = null)
  {
    _config = config;
    _chanceRandom = chanceRandom ?? Random.Shared;
    _weightedRandom = weightedRandom ?? new WeightedRandom();
    _conditionEvaluator = conditionEvaluator ?? new ConditionEvaluator();
    _rewardExecutor = rewardExecutor ?? new RewardExecutor();
    _eventsById = config.Events.ToDictionary(gameEvent => gameEvent.Id, StringComparer.Ordinal);
  }

  public EventData? CurrentEvent { get; private set; }
  public bool HasActiveEvent => CurrentEvent is not null;

  public bool TryStartRandomEvent(
    CharacterState character,
    CalendarState calendar,
    out EventData? selectedEvent)
  {
    selectedEvent = null;
    if (HasActiveEvent || _chanceRandom.NextDouble() >= _config.TriggerChance)
    {
      return false;
    }

    _currentMonth = calendar.TotalMonthsSurvived;
    var eligibleEvents = _config.Events
      .Where(gameEvent => IsAvailable(gameEvent, character, calendar))
      .ToArray();
    selectedEvent = _weightedRandom.Choose(eligibleEvents, gameEvent => gameEvent.Weight);
    CurrentEvent = selectedEvent;
    return selectedEvent is not null;
  }

  public EventResolution ResolveChoice(string choiceId, CharacterState character)
  {
    var currentEvent = CurrentEvent
      ?? throw new InvalidOperationException("There is no active event to resolve.");
    var choice = currentEvent.Choices.FirstOrDefault(candidate =>
      string.Equals(candidate.Id, choiceId, StringComparison.Ordinal));
    if (choice is null)
    {
      throw new ArgumentException(
        $"Event '{currentEvent.Id}' does not contain choice '{choiceId}'.",
        nameof(choiceId));
    }

    _rewardExecutor.Apply(choice.Rewards, character);
    _lastResolvedMonth[currentEvent.Id] = _currentMonth;
    if (currentEvent.Unique)
    {
      _completedUniqueEvents.Add(currentEvent.Id);
    }

    EventData? nextEvent = null;
    if (!string.IsNullOrWhiteSpace(choice.NextEventId))
    {
      nextEvent = _eventsById[choice.NextEventId];
    }

    CurrentEvent = nextEvent;
    return new EventResolution(currentEvent, choice, nextEvent, nextEvent is null && choice.CloseEvent);
  }

  public void Reset()
  {
    CurrentEvent = null;
    _lastResolvedMonth.Clear();
    _completedUniqueEvents.Clear();
    _currentMonth = 0;
  }

  private bool IsAvailable(EventData gameEvent, CharacterState character, CalendarState calendar)
  {
    if (gameEvent.Unique && _completedUniqueEvents.Contains(gameEvent.Id))
    {
      return false;
    }

    if (gameEvent.CooldownMonths > 0 &&
        _lastResolvedMonth.TryGetValue(gameEvent.Id, out var lastMonth) &&
        calendar.TotalMonthsSurvived - lastMonth < gameEvent.CooldownMonths)
    {
      return false;
    }

    return _conditionEvaluator.AreMet(gameEvent.Conditions, character, calendar);
  }
}
