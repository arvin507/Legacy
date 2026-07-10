using AncientLife.Configs;
using AncientLife.Models;
using AncientLife.Models.Events;
using AncientLife.Systems;
using Godot;

namespace AncientLife.Managers;

public partial class GameManager : Node
{
    public event System.Action? StateChanged;
    public event System.Action<ActionResult>? ActionResolved;
    public event System.Action<DailySettlementResult>? DaySettled;
    public event System.Action<GameSummary>? GameEnded;
    public event System.Action<EventData>? RandomEventStarted;
    public event System.Action<EventResolution>? RandomEventResolved;

    public GameSession Session { get; private set; } = null!;
    public EventManager Events { get; private set; } = null!;
    public EraManager Eras { get; private set; } = null!;

    public override void _Ready()
    {
        Eras = new EraManager(EraLoader.Load());
        var timeSystem = new TimeSystem(CalendarConfigLoader.Load(), Eras);
        Session = new GameSession(ActionConfigLoader.Load(), timeSystem: timeSystem);
        Events = new EventManager(EventLoader.Load());
        Session.StateChanged += () => StateChanged?.Invoke();
        Session.ActionResolved += result => ActionResolved?.Invoke(result);
        Session.DaySettled += result => DaySettled?.Invoke(result);
        Session.GameEnded += summary => GameEnded?.Invoke(summary);
    }

    public ActionResult PerformAction(string actionId) => Session.PerformAction(actionId);

    public void EndDay()
    {
        if (!Session.BeginEndDay())
        {
            return;
        }

        if (Events.TryStartRandomEvent(Session.Character, Session.Calendar, out var selectedEvent))
        {
            RandomEventStarted?.Invoke(selectedEvent!);
            return;
        }

        Session.CompleteEndDay();
    }

    public EventResolution? ResolveEventChoice(string choiceId)
    {
        if (!Session.IsEndingDay || !Events.HasActiveEvent)
        {
            return null;
        }

        var resolution = Events.ResolveChoice(choiceId, Session.Character);
        RandomEventResolved?.Invoke(resolution);

        if (resolution.NextEvent is not null)
        {
            StateChanged?.Invoke();
            RandomEventStarted?.Invoke(resolution.NextEvent);
            return resolution;
        }

        Session.CompleteEndDay();
        return resolution;
    }

    public void Restart()
    {
        Events.Reset();
        Session.Restart();
    }
}
