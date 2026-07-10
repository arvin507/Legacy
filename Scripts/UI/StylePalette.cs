using Godot;

namespace AncientLife.UI;

public static class StylePalette
{
    public static readonly Color Ink = Color.FromHtml("#352B20");
    public static readonly Color MutedInk = Color.FromHtml("#756754");
    public static readonly Color Paper = Color.FromHtml("#F5ECD9");
    public static readonly Color PaperRaised = Color.FromHtml("#FBF5E8");
    public static readonly Color Cinnabar = Color.FromHtml("#973B33");
    public static readonly Color Jade = Color.FromHtml("#345C57");
    public static readonly Color Gold = Color.FromHtml("#B38A4D");

    public static StyleBoxFlat CreateCardStyle(Color accent, float alpha = 1f)
    {
        return CreateStyle(
            new Color(PaperRaised.R, PaperRaised.G, PaperRaised.B, alpha),
            new Color(accent.R, accent.G, accent.B, 0.32f),
            3,
            8,
            new Color(0.19f, 0.14f, 0.09f, 0.13f),
            8,
            new Vector2(0, 5));
    }

    public static StyleBoxFlat CreateActionStyle(Color accent, ButtonState state)
    {
        var background = state switch
        {
            ButtonState.Hover => Color.FromHtml("#FFF9EC"),
            ButtonState.Pressed => Color.FromHtml("#E8DBC1"),
            ButtonState.Disabled => Color.FromHtml("#DED5C3"),
            _ => PaperRaised
        };

        var border = state == ButtonState.Disabled
            ? new Color(0.36f, 0.32f, 0.27f, 0.2f)
            : new Color(accent.R, accent.G, accent.B, state == ButtonState.Hover ? 0.8f : 0.5f);

        return CreateStyle(
            background,
            border,
            state == ButtonState.Hover ? 4 : 3,
            8,
            new Color(0.17f, 0.12f, 0.08f, state == ButtonState.Disabled ? 0.04f : 0.12f),
            state == ButtonState.Pressed ? 3 : 7,
            new Vector2(0, state == ButtonState.Pressed ? 2 : 5));
    }

    public static StyleBoxFlat CreateSolidButtonStyle(Color color, bool pressed = false, bool disabled = false)
    {
        var background = disabled
            ? Color.FromHtml("#9C9487")
            : pressed ? color.Darkened(0.12f) : color;

        return CreateStyle(
            background,
            color.Darkened(0.2f),
            2,
            8,
            new Color(0.13f, 0.09f, 0.06f, disabled ? 0.04f : 0.2f),
            pressed ? 2 : 8,
            new Vector2(0, pressed ? 2 : 5));
    }

    public static StyleBoxFlat CreatePanelStyle(Color background, Color border, int radius = 8, bool shadow = true)
    {
        return CreateStyle(
            background,
            border,
            2,
            radius,
            new Color(0.13f, 0.09f, 0.06f, shadow ? 0.16f : 0f),
            shadow ? 8 : 0,
            new Vector2(0, shadow ? 5 : 0));
    }

    private static StyleBoxFlat CreateStyle(
        Color background,
        Color border,
        int borderWidth,
        int cornerRadius,
        Color shadowColor,
        int shadowSize,
        Vector2 shadowOffset)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = cornerRadius,
            CornerRadiusTopRight = cornerRadius,
            CornerRadiusBottomLeft = cornerRadius,
            CornerRadiusBottomRight = cornerRadius,
            ShadowColor = shadowColor,
            ShadowSize = shadowSize,
            ShadowOffset = shadowOffset,
            AntiAliasing = true
        };
    }

    public enum ButtonState
    {
        Normal,
        Hover,
        Pressed,
        Disabled
    }
}
