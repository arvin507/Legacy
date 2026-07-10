using AncientLife.Models;
using AncientLife.Systems;

namespace AncientLife.Managers;

public sealed class GameSession
{
    private readonly ActionSystem _actionSystem;
    private readonly DailySettlementSystem _dailySettlementSystem;
    private readonly TimeSystem _timeSystem;
    private readonly Dictionary<string, int> _dailyUsage = new(StringComparer.Ordinal);

    public GameSession(
        IReadOnlyList<ActionDefinition> actions,
        ActionSystem? actionSystem = null,
        DailySettlementSystem? dailySettlementSystem = null,
        TimeSystem? timeSystem = null)
    {
        Actions = actions;
        _actionSystem = actionSystem ?? new ActionSystem();
        _dailySettlementSystem = dailySettlementSystem ?? new DailySettlementSystem();
        _timeSystem = timeSystem ?? throw new ArgumentNullException(nameof(timeSystem));
        Character = CharacterState.CreateDefault();
        Calendar = _timeSystem.CreateCalendar();
    }

    public event System.Action? StateChanged;
    public event System.Action<ActionResult>? ActionResolved;
    public event System.Action<DailySettlementResult>? DaySettled;
    public event System.Action<GameSummary>? GameEnded;

    public IReadOnlyList<ActionDefinition> Actions { get; }
    public CharacterState Character { get; private set; }
    public CalendarState Calendar { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsEndingDay { get; private set; }
    public GameSummary? Summary { get; private set; }

    public ActionAvailability GetActionAvailability(string actionId)
    {
        var definition = FindAction(actionId);
        return definition is null
            ? new(false, "行动不存在")
            : _actionSystem.GetAvailability(definition, Character, _dailyUsage, IsGameOver || IsEndingDay);
    }

    public ActionResult PerformAction(string actionId)
    {
        var definition = FindAction(actionId);
        var result = definition is null
            ? ActionResult.Failed(actionId, "行动不存在")
            : _actionSystem.Perform(definition, Character, _dailyUsage, IsGameOver || IsEndingDay);

        ActionResolved?.Invoke(result);
        if (result.Success)
        {
            StateChanged?.Invoke();
        }

        return result;
    }

    public void EndDay()
    {
        if (BeginEndDay())
        {
            CompleteEndDay();
        }
    }

    public bool BeginEndDay()
    {
        if (IsGameOver || IsEndingDay)
        {
            return false;
        }

        IsEndingDay = true;

        var settlement = _dailySettlementSystem.Settle(Character);
        DaySettled?.Invoke(settlement);

        if (Character.Health <= 0)
        {
            CompleteEndDay();
            return false;
        }

        StateChanged?.Invoke();
        return true;
    }

    public bool CompleteEndDay()
    {
        if (!IsEndingDay || IsGameOver)
        {
            return false;
        }

        _timeSystem.CompleteCurrentMonth(Calendar, Character);
        _dailyUsage.Clear();
        IsEndingDay = false;

        if (Character.Health <= 0)
        {
            IsGameOver = true;
            Summary = new GameSummary(
                Calendar.TotalMonthsSurvived,
                Character.Money,
                Character.Culture);
            StateChanged?.Invoke();
            GameEnded?.Invoke(Summary);
            return false;
        }

        Character.RestoreDailyEnergy();
        StateChanged?.Invoke();
        return true;
    }

    public void Restart()
    {
        Character = CharacterState.CreateDefault();
        Calendar = _timeSystem.CreateCalendar(Calendar.EraName);
        _dailyUsage.Clear();
        IsGameOver = false;
        IsEndingDay = false;
        Summary = null;
        StateChanged?.Invoke();
    }

    private ActionDefinition? FindAction(string actionId) =>
        Actions.FirstOrDefault(action => string.Equals(action.Id, actionId, StringComparison.Ordinal));
}
