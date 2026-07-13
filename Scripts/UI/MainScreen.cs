using AncientLife.Models;
using AncientLife.UI.Components;
using Godot;

namespace AncientLife.UI;

public partial class MainScreen : Control
{
    private readonly Dictionary<string, ActionButton> _actionButtons = new(StringComparer.Ordinal);
    private readonly PackedScene _actionButtonScene = GD.Load<PackedScene>("res://Scenes/Components/ActionButton.tscn");

    private MarginContainer _mainMargin = null!;
    private PanelContainer _headerPanel = null!;
    private PanelContainer _seasonBadge = null!;
    private Label _yearLabel = null!;
    private Label _seasonLabel = null!;
    private Label _monthLabel = null!;
    private Label _monthlyOrderLabel = null!;
    private Label _nameLabel = null!;
    private Label _ageLabel = null!;
    private Label _standingLabel = null!;
    private Label _professionGoalLabel = null!;
    private Label _actionStatusLabel = null!;
    private PanelContainer _feedbackPanel = null!;
    private Label _feedbackLabel = null!;
    private VBoxContainer _actionList = null!;
    private Button _endMonthButton = null!;
    private Control _gameOverLayer = null!;
    private PanelContainer _gameOverPanel = null!;
    private Label _gameOverSubtitle = null!;
    private Label _monthsSummary = null!;
    private Label _wealthSummary = null!;
    private Label _cultureSummary = null!;
    private Label _ageSummary = null!;
    private Label _professionSummary = null!;
    private Label _recordsSummary = null!;
    private Button _restartButton = null!;

    private StatCard _energyCard = null!;
    private StatCard _healthCard = null!;
    private StatCard _moneyCard = null!;
    private StatCard _foodCard = null!;
    private StatCard _cultureCard = null!;

    public event System.Action<string>? ActionRequested;
    public event System.Action? EndMonthRequested;
    public event System.Action? RestartRequested;

    public override void _Ready()
    {
        BindNodes();
        ConfigureStaticVisuals();
        _endMonthButton.Pressed += () => EndMonthRequested?.Invoke();
        _restartButton.Pressed += () => RestartRequested?.Invoke();
        ApplyResponsiveMargins();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && IsNodeReady())
        {
            ApplyResponsiveMargins();
        }
    }

    public void InitializeActions(IReadOnlyList<ActionDefinition> actions)
    {
        foreach (var child in _actionList.GetChildren())
        {
            child.QueueFree();
        }

        _actionButtons.Clear();
        foreach (var definition in actions)
        {
            var button = _actionButtonScene.Instantiate<ActionButton>();
            _actionList.AddChild(button);
            button.Configure(definition);
            button.ActionRequested += actionId => ActionRequested?.Invoke(actionId);
            _actionButtons.Add(definition.Id, button);
        }
    }

    public void Render(
        CharacterState character,
        CalendarState calendar,
        string professionGoal,
        IReadOnlyDictionary<string, ActionAvailability> availability,
        bool isGameOver)
    {
        _yearLabel.Text = calendar.EraYearName;
        _seasonLabel.Text = calendar.SeasonName;
        _monthLabel.Text = calendar.MonthName;
        _monthlyOrderLabel.Text = calendar.MonthlyOrder;
        _nameLabel.Text = character.Name;
        _ageLabel.Text = $"{character.Age}岁";
        _standingLabel.Text = $"{character.ProfessionName} · 农事 {character.FarmingSkill} · 学识 {character.ScholarshipSkill}";
        _professionGoalLabel.Text = professionGoal;

        _energyCard.SetValue($"{character.Energy} / {character.MaxEnergy}", character.Energy, character.MaxEnergy);
        _healthCard.SetValue($"{character.Health} / {character.MaxHealth}", character.Health, character.MaxHealth);
        _moneyCard.SetValue($"{character.Money} 文");
        _foodCard.SetValue(character.Food.ToString());
        _cultureCard.SetValue(character.Culture.ToString());
        _actionStatusLabel.Text = $"尚余体力 {character.Energy}";

        foreach (var pair in _actionButtons)
        {
            pair.Value.SetAvailability(availability.GetValueOrDefault(
                pair.Key,
                new ActionAvailability(false, "行动不可用")));
        }

        _endMonthButton.Disabled = isGameOver;
    }

    public void ShowFeedback(string message, bool positive = true)
    {
        _feedbackLabel.Text = message;
        var accent = positive ? StylePalette.Jade : StylePalette.Cinnabar;
        _feedbackPanel.AddThemeStyleboxOverride(
            "panel",
            StylePalette.CreatePanelStyle(
                new Color(StylePalette.PaperRaised.R, StylePalette.PaperRaised.G, StylePalette.PaperRaised.B, 0.9f),
                new Color(accent.R, accent.G, accent.B, 0.45f),
                6,
                false));
    }

    public void ShowGameOver(GameSummary summary)
    {
        _monthsSummary.Text = $"存活月数　{summary.MonthsSurvived} 月";
        _wealthSummary.Text = $"最终财富　{summary.FinalWealth} 文";
        _cultureSummary.Text = $"文化积累　{summary.Culture}";
        _ageSummary.Text = $"享年　　　{summary.Age} 岁";
        _professionSummary.Text = $"最终身份　{summary.ProfessionName}";
        _gameOverSubtitle.Text = summary.EndingTitle;
        _recordsSummary.Text = string.Join("\n", summary.LifeRecords
            .TakeLast(5)
            .Select(record => $"{record.EraYear}{record.MonthName} · {record.Text}"));
        _gameOverLayer.Visible = true;
        _restartButton.GrabFocus();
    }

    public void HideGameOver()
    {
        _gameOverLayer.Visible = false;
    }

    private void BindNodes()
    {
        _mainMargin = GetNode<MarginContainer>("MainMargin");
        _headerPanel = GetNode<PanelContainer>("MainMargin/Layout/Header");
        _seasonBadge = GetNode<PanelContainer>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/TimeRow/SeasonBadge");
        _yearLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/TimeRow/Year");
        _seasonLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/TimeRow/SeasonBadge/Season");
        _monthLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/TimeRow/Month");
        _monthlyOrderLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/Eyebrow");
        _nameLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/IdentityRow/Name");
        _ageLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/IdentityRow/Age");
        _standingLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/IdentityRow/Standing");
        _professionGoalLabel = GetNode<Label>("MainMargin/Layout/Header/HeaderMargin/HeaderContent/ProfessionGoal");
        _actionStatusLabel = GetNode<Label>("MainMargin/Layout/ActionHeading/Status");
        _feedbackPanel = GetNode<PanelContainer>("MainMargin/Layout/Feedback");
        _feedbackLabel = GetNode<Label>("MainMargin/Layout/Feedback/FeedbackMargin/Message");
        _actionList = GetNode<VBoxContainer>("MainMargin/Layout/ActionScroll/ActionList");
        _endMonthButton = GetNode<Button>("MainMargin/Layout/Footer/FooterMargin/FooterContent/EndMonth");

        _energyCard = GetNode<StatCard>("MainMargin/Layout/Stats/Vitals/Energy");
        _healthCard = GetNode<StatCard>("MainMargin/Layout/Stats/Vitals/Health");
        _moneyCard = GetNode<StatCard>("MainMargin/Layout/Stats/Resources/Money");
        _foodCard = GetNode<StatCard>("MainMargin/Layout/Stats/Resources/Food");
        _cultureCard = GetNode<StatCard>("MainMargin/Layout/Stats/Resources/Culture");

        _gameOverLayer = GetNode<Control>("GameOverLayer");
        _gameOverPanel = GetNode<PanelContainer>("GameOverLayer/Center/GameOverPanel");
        _gameOverSubtitle = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Subtitle");
        _monthsSummary = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Summary/Months");
        _wealthSummary = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Summary/Wealth");
        _cultureSummary = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Summary/Culture");
        _ageSummary = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Summary/Age");
        _professionSummary = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Summary/Profession");
        _recordsSummary = GetNode<Label>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Records");
        _restartButton = GetNode<Button>("GameOverLayer/Center/GameOverPanel/ModalMargin/ModalContent/Restart");
    }

    private void ConfigureStaticVisuals()
    {
        _headerPanel.AddThemeStyleboxOverride(
            "panel",
            StylePalette.CreatePanelStyle(Color.FromHtml("#35463F"), Color.FromHtml("#8F5B4C")));
        _seasonBadge.AddThemeStyleboxOverride(
            "panel",
            StylePalette.CreatePanelStyle(StylePalette.Cinnabar, StylePalette.Cinnabar.Darkened(0.18f), 6, false));
        _gameOverPanel.AddThemeStyleboxOverride(
            "panel",
            StylePalette.CreatePanelStyle(StylePalette.PaperRaised, StylePalette.Cinnabar));

        _energyCard.Configure("体力", "res://Resources/Icons/stat_energy.svg", StylePalette.Cinnabar, true);
        _healthCard.Configure("健康", "res://Resources/Icons/stat_health.svg", Color.FromHtml("#A5433C"), true);
        _moneyCard.Configure("金钱", "res://Resources/Icons/stat_money.svg", StylePalette.Gold, false);
        _foodCard.Configure("粮食", "res://Resources/Icons/stat_food.svg", Color.FromHtml("#56704D"), false);
        _cultureCard.Configure("文化", "res://Resources/Icons/stat_culture.svg", StylePalette.Jade, false);

        _endMonthButton.AddThemeStyleboxOverride("normal", StylePalette.CreateSolidButtonStyle(StylePalette.Cinnabar));
        _endMonthButton.AddThemeStyleboxOverride("hover", StylePalette.CreateSolidButtonStyle(StylePalette.Cinnabar.Lightened(0.08f)));
        _endMonthButton.AddThemeStyleboxOverride("pressed", StylePalette.CreateSolidButtonStyle(StylePalette.Cinnabar, true));
        _endMonthButton.AddThemeStyleboxOverride("disabled", StylePalette.CreateSolidButtonStyle(StylePalette.Cinnabar, false, true));
        _restartButton.AddThemeStyleboxOverride("normal", StylePalette.CreateSolidButtonStyle(StylePalette.Jade));
        _restartButton.AddThemeStyleboxOverride("hover", StylePalette.CreateSolidButtonStyle(StylePalette.Jade.Lightened(0.08f)));
        _restartButton.AddThemeStyleboxOverride("pressed", StylePalette.CreateSolidButtonStyle(StylePalette.Jade, true));

        ShowFeedback("月序初开，本月尚有许多可为。", true);
    }

    private void ApplyResponsiveMargins()
    {
        var horizontalMargin = Mathf.Clamp(Mathf.RoundToInt(Size.X * 0.045f), 28, 72);
        var verticalMargin = Mathf.Clamp(Mathf.RoundToInt(Size.Y * 0.022f), 34, 64);
        _mainMargin.AddThemeConstantOverride("margin_left", horizontalMargin);
        _mainMargin.AddThemeConstantOverride("margin_right", horizontalMargin);
        _mainMargin.AddThemeConstantOverride("margin_top", verticalMargin);
        _mainMargin.AddThemeConstantOverride("margin_bottom", verticalMargin);
    }

}
