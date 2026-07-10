using AncientLife.Models.Events;
using Godot;

namespace AncientLife.UI.Events;

public partial class EventPopup : PopupPanel
{
    private Label _titleLabel = null!;
    private Label _descriptionLabel = null!;
    private PanelContainer _illustrationFrame = null!;
    private TextureRect _illustration = null!;
    private VBoxContainer _choiceList = null!;
    private bool _requiresChoice;
    private Vector2I _popupSize = new(900, 700);

    public event System.Action<string>? ChoiceSelected;

    public override void _Ready()
    {
        const string contentPath = "Center/DialogPanel/ContentMargin/Content";
        _titleLabel = GetNode<Label>($"{contentPath}/Header/Title");
        _descriptionLabel = GetNode<Label>($"{contentPath}/Description");
        _illustrationFrame = GetNode<PanelContainer>($"{contentPath}/IllustrationFrame");
        _illustration = GetNode<TextureRect>($"{contentPath}/IllustrationFrame/Illustration");
        _choiceList = GetNode<VBoxContainer>($"{contentPath}/Choices");

        Exclusive = true;
        Unresizable = true;
        PopupHide += OnPopupHidden;
        CloseRequested += OnCloseRequested;
    }

    public void ShowEvent(EventData gameEvent)
    {
        Title = gameEvent.Title;
        _titleLabel.Text = gameEvent.Title;
        _descriptionLabel.Text = gameEvent.Description;
        var hasIllustration = ConfigureIllustration(gameEvent.IllustrationPath);
        BuildChoices(gameEvent.Choices);
        ResizeForContent(gameEvent.Choices.Count, hasIllustration);
        _requiresChoice = true;

        if (!Visible)
        {
            PopupCentered(_popupSize);
        }
    }

    public void CloseAfterResolution()
    {
        _requiresChoice = false;
        Hide();
    }

    private bool ConfigureIllustration(string illustrationPath)
    {
        var hasIllustration = !string.IsNullOrWhiteSpace(illustrationPath) &&
                              ResourceLoader.Exists(illustrationPath, "Texture2D");
        _illustrationFrame.Visible = hasIllustration;
        _illustration.Texture = hasIllustration
            ? GD.Load<Texture2D>(illustrationPath)
            : null;
        return hasIllustration;
    }

    private void ResizeForContent(int choiceCount, bool hasIllustration)
    {
        var contentHeight = 340 + choiceCount * 102 + (hasIllustration ? 236 : 0);
        _popupSize = new Vector2I(900, contentHeight);
        Size = _popupSize;
    }

    private void BuildChoices(IReadOnlyList<EventChoice> choices)
    {
        foreach (var child in _choiceList.GetChildren())
        {
            _choiceList.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var choice in choices)
        {
            var button = CreateChoiceButton(choice);
            _choiceList.AddChild(button);
        }
    }

    private Button CreateChoiceButton(EventChoice choice)
    {
        var button = new Button
        {
            Text = choice.Text,
            CustomMinimumSize = new Vector2(0, 88),
            FocusMode = Control.FocusModeEnum.All,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            Alignment = HorizontalAlignment.Left,
            ActionMode = BaseButton.ActionModeEnum.Press
        };
        button.AddThemeFontSizeOverride("font_size", 28);
        button.AddThemeColorOverride("font_color", StylePalette.Ink);
        button.AddThemeColorOverride("font_hover_color", StylePalette.Ink);
        button.AddThemeColorOverride("font_pressed_color", StylePalette.Ink);
        button.AddThemeStyleboxOverride(
            "normal",
            StylePalette.CreateActionStyle(StylePalette.Jade, StylePalette.ButtonState.Normal));
        button.AddThemeStyleboxOverride(
            "hover",
            StylePalette.CreateActionStyle(StylePalette.Jade, StylePalette.ButtonState.Hover));
        button.AddThemeStyleboxOverride(
            "pressed",
            StylePalette.CreateActionStyle(StylePalette.Jade, StylePalette.ButtonState.Pressed));
        button.Pressed += () => OnChoicePressed(choice.Id);
        return button;
    }

    private void OnChoicePressed(string choiceId)
    {
        GetViewport().SetInputAsHandled();
        SetChoicesEnabled(false);
        ChoiceSelected?.Invoke(choiceId);
    }

    private void SetChoicesEnabled(bool enabled)
    {
        foreach (var button in _choiceList.GetChildren().OfType<Button>())
        {
            button.Disabled = !enabled;
        }
    }

    private void OnCloseRequested()
    {
        if (_requiresChoice)
        {
            CallDeferred(MethodName.ReopenRequiredPopup);
        }
    }

    private void OnPopupHidden()
    {
        if (_requiresChoice)
        {
            CallDeferred(MethodName.ReopenRequiredPopup);
        }
    }

    private void ReopenRequiredPopup()
    {
        PopupCentered(_popupSize);
    }
}
