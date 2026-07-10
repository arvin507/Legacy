using AncientLife.Models;
using Godot;

namespace AncientLife.UI.Components;

public partial class ActionButton : Button
{
    private TextureRect _icon = null!;
    private ColorRect _accentStrip = null!;
    private Label _nameLabel = null!;
    private Label _descriptionLabel = null!;
    private Label _costLabel = null!;
    private Label _rewardLabel = null!;
    private string _actionId = string.Empty;
    private string _baseTooltip = string.Empty;

    public event System.Action<string>? ActionRequested;

    public override void _Ready()
    {
        _accentStrip = GetNode<ColorRect>("Layout/AccentStrip");
        _icon = GetNode<TextureRect>("Layout/Margin/Content/Icon");
        _nameLabel = GetNode<Label>("Layout/Margin/Content/Details/Name");
        _descriptionLabel = GetNode<Label>("Layout/Margin/Content/Details/Description");
        _costLabel = GetNode<Label>("Layout/Margin/Content/Meta/Cost");
        _rewardLabel = GetNode<Label>("Layout/Margin/Content/Meta/Reward");
        Pressed += () => ActionRequested?.Invoke(_actionId);
    }

    public void Configure(ActionDefinition definition)
    {
        _actionId = definition.Id;
        _nameLabel.Text = definition.Name;
        _descriptionLabel.Text = definition.Description;
        _costLabel.Text = definition.EnergyCost == 0 ? "无需体力" : $"体力 -{definition.EnergyCost}";
        _rewardLabel.Text = definition.RewardLabel;
        _icon.Texture = GD.Load<Texture2D>(definition.IconPath);

        var accent = Color.FromHtml(definition.AccentColor);
        _accentStrip.Color = accent;
        _icon.SelfModulate = accent;
        AddThemeStyleboxOverride("normal", StylePalette.CreateActionStyle(accent, StylePalette.ButtonState.Normal));
        AddThemeStyleboxOverride("hover", StylePalette.CreateActionStyle(accent, StylePalette.ButtonState.Hover));
        AddThemeStyleboxOverride("pressed", StylePalette.CreateActionStyle(accent, StylePalette.ButtonState.Pressed));
        AddThemeStyleboxOverride("disabled", StylePalette.CreateActionStyle(accent, StylePalette.ButtonState.Disabled));
        _baseTooltip = $"{definition.Name}：{definition.RewardLabel}";
        TooltipText = _baseTooltip;
    }

    public void SetAvailability(ActionAvailability availability)
    {
        Disabled = !availability.CanPerform;
        SelfModulate = availability.CanPerform ? Colors.White : new Color(1, 1, 1, 0.68f);
        TooltipText = availability.CanPerform ? _baseTooltip : availability.Reason;
    }
}
