using Godot;

namespace AncientLife.UI.Components;

public partial class StatCard : PanelContainer
{
    private TextureRect _icon = null!;
    private Label _title = null!;
    private Label _value = null!;
    private ProgressBar _progress = null!;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("Margin/Content/Header/Icon");
        _title = GetNode<Label>("Margin/Content/Header/Title");
        _value = GetNode<Label>("Margin/Content/Value");
        _progress = GetNode<ProgressBar>("Margin/Content/Progress");
    }

    public void Configure(string title, string iconPath, Color accent, bool showProgress)
    {
        _title.Text = title;
        _icon.Texture = GD.Load<Texture2D>(iconPath);
        _progress.Visible = showProgress;
        AddThemeStyleboxOverride("panel", StylePalette.CreateCardStyle(accent, 0.94f));

        var fillStyle = new StyleBoxFlat
        {
            BgColor = accent,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            AntiAliasing = true
        };
        var backgroundStyle = new StyleBoxFlat
        {
            BgColor = new Color(accent.R, accent.G, accent.B, 0.13f),
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            AntiAliasing = true
        };
        _progress.AddThemeStyleboxOverride("fill", fillStyle);
        _progress.AddThemeStyleboxOverride("background", backgroundStyle);
    }

    public void SetValue(string text, int current = 0, int maximum = 0)
    {
        _value.Text = text;
        if (!_progress.Visible || maximum <= 0)
        {
            return;
        }

        _progress.MaxValue = maximum;
        _progress.Value = Math.Clamp(current, 0, maximum);
    }
}
