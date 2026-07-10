using AncientLife.Models.Events;
using AncientLife.Managers;
using Godot;

namespace AncientLife.UI.Events;

public partial class EventPopupController : Node
{
    [Export]
    public NodePath PopupPath { get; set; } = new("../EventPopup");

    [Export]
    public NodePath ManagerPath { get; set; } = new("../GameManager");

    private EventPopup _popup = null!;
    private GameManager _gameManager = null!;

    public override void _Ready()
    {
        _popup = GetNode<EventPopup>(PopupPath);
        _gameManager = GetNode<GameManager>(ManagerPath);
        _popup.ChoiceSelected += OnChoiceSelected;
        _gameManager.RandomEventStarted += Present;
    }

    public override void _ExitTree()
    {
        if (_popup is not null)
        {
            _popup.ChoiceSelected -= OnChoiceSelected;
        }

        if (_gameManager is not null)
        {
            _gameManager.RandomEventStarted -= Present;
        }
    }

    public void Present(EventData gameEvent)
    {
        _popup.ShowEvent(gameEvent);
    }

    public void Dismiss()
    {
        _popup.CloseAfterResolution();
    }

    private void OnChoiceSelected(string choiceId)
    {
        var resolution = _gameManager.ResolveEventChoice(choiceId);
        if (resolution?.NextEvent is null)
        {
            DismissAfterChoiceInput();
        }
    }

    private async void DismissAfterChoiceInput()
    {
        await ToSignal(GetTree().CreateTimer(0.12), SceneTreeTimer.SignalName.Timeout);
        Dismiss();
    }
}
