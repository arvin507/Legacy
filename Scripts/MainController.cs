using AncientLife.Managers;
using AncientLife.Models;
using AncientLife.UI;
using Godot;

namespace AncientLife;

public partial class MainController : Control
{
    private GameManager _gameManager = null!;
    private MainScreen _screen = null!;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("GameManager");
        _screen = GetNode<MainScreen>("MainScreen");

        _screen.ActionRequested += OnActionRequested;
        _screen.EndMonthRequested += OnEndMonthRequested;
        _screen.RestartRequested += OnRestartRequested;
        _gameManager.StateChanged += Render;
        _gameManager.ActionResolved += OnActionResolved;
        _gameManager.MonthSettled += OnMonthSettled;
        _gameManager.GameEnded += OnGameEnded;

        _screen.InitializeActions(_gameManager.Session.Actions);
        Render();
    }

    public override void _ExitTree()
    {
        if (_screen is not null)
        {
            _screen.ActionRequested -= OnActionRequested;
            _screen.EndMonthRequested -= OnEndMonthRequested;
            _screen.RestartRequested -= OnRestartRequested;
        }

        if (_gameManager is not null)
        {
            _gameManager.StateChanged -= Render;
            _gameManager.ActionResolved -= OnActionResolved;
            _gameManager.MonthSettled -= OnMonthSettled;
            _gameManager.GameEnded -= OnGameEnded;
        }
    }

    private void OnActionRequested(string actionId)
    {
        _gameManager.PerformAction(actionId);
    }

    private void OnEndMonthRequested()
    {
        _gameManager.EndMonth();
    }

    private void OnRestartRequested()
    {
        _screen.HideGameOver();
        _gameManager.Restart();
        _screen.ShowFeedback("新的人生开始了。", true);
    }

    private void OnActionResolved(ActionResult result)
    {
        _screen.ShowFeedback(result.Message, result.Success);
    }

    private void OnMonthSettled(MonthlySettlementResult result)
    {
        _screen.ShowFeedback(result.Message, result.HealthChange >= 0);
    }

    private void OnGameEnded(GameSummary summary)
    {
        _screen.ShowGameOver(summary);
    }

    private void Render()
    {
        var session = _gameManager.Session;
        var availability = session.Actions.ToDictionary(
            action => action.Id,
            action => session.GetActionAvailability(action.Id),
            StringComparer.Ordinal);

        _screen.Render(
            session.Character,
            session.Calendar,
            session.ProfessionGoal,
            availability,
            session.IsGameOver);
    }
}
