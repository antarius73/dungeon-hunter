using System.Collections.Generic;
using Godot;

namespace DungeonHunter.View;

public static class GuiSkin
{
    public static readonly Color InkColor = new(0.25f, 0.15f, 0.07f);
    public static readonly Color LightText = new(0.98f, 0.94f, 0.82f);
    public static readonly Color PlaqueText = new(0.2f, 0.27f, 0.3f);

    private static readonly Dictionary<string, Texture2D> Textures = new();

    public static Texture2D Texture(string name)
    {
        if (!Textures.TryGetValue(name, out var texture))
        {
            texture = GD.Load<Texture2D>($"res://assets/gui/{name}.png");
            Textures[name] = texture;
        }
        return texture;
    }

    public static StyleBoxTexture Box(string name, float left, float top, float right, float bottom, float contentX = -1, float contentY = -1)
    {
        var box = new StyleBoxTexture
        {
            Texture = Texture(name),
            TextureMarginLeft = left,
            TextureMarginTop = top,
            TextureMarginRight = right,
            TextureMarginBottom = bottom,
        };
        if (contentX >= 0)
        {
            box.ContentMarginLeft = contentX;
            box.ContentMarginRight = contentX;
        }
        if (contentY >= 0)
        {
            box.ContentMarginTop = contentY;
            box.ContentMarginBottom = contentY;
        }
        return box;
    }

    public static StyleBoxTexture LongButtonBox(string state) => Box($"btn_long_{state}", 30, 22, 30, 26, 34, 14);

    public static StyleBoxTexture SquareButtonBox(string state) => Box($"btn_square_{state}", 18, 18, 18, 18, 8, 8);

    public static Button LongButton(string text, int fontSize = 30)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(280, 89), FocusMode = Control.FocusModeEnum.All };
        button.AddThemeStyleboxOverride("normal", LongButtonBox("normal"));
        button.AddThemeStyleboxOverride("hover", LongButtonBox("hover"));
        button.AddThemeStyleboxOverride("pressed", LongButtonBox("pressed"));
        button.AddThemeStyleboxOverride("disabled", LongButtonBox("disabled"));
        button.AddThemeStyleboxOverride("focus", LongButtonBox("hover"));
        button.AddThemeFontSizeOverride("font_size", fontSize);
        foreach (var color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
        {
            button.AddThemeColorOverride(color, LightText);
        }
        button.AddThemeColorOverride("font_outline_color", new Color(0.18f, 0.32f, 0.04f));
        button.AddThemeConstantOverride("outline_size", 8);
        return button;
    }

    public static TextureButton IconButton(string icon, string tooltip)
    {
        var button = new TextureButton
        {
            TextureNormal = Texture($"btn_{icon}_normal"),
            TextureHover = Texture($"btn_{icon}_hover"),
            TexturePressed = Texture($"btn_{icon}_pressed"),
            TextureDisabled = Texture($"btn_{icon}_disabled"),
            TooltipText = tooltip,
            FocusMode = Control.FocusModeEnum.None,
        };
        return button;
    }

    public static Control Ribbon(string title)
    {
        var texture = Texture("ribbon_xl");
        var ribbon = new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = texture.GetSize(),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var label = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(140, 6),
            Size = new Vector2(texture.GetWidth() - 280, 72),
            ClipText = true,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_color", PlaqueText);
        label.AddThemeColorOverride("font_outline_color", new Color(1, 1, 1, 0.7f));
        label.AddThemeConstantOverride("outline_size", 4);
        ribbon.AddChild(label);
        return ribbon;
    }
}
