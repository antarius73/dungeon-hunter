using System;
using System.Collections.Generic;
using Godot;

namespace DungeonHunter.View;

public enum PanelDecoration
{
    None,
    Victory,
}

public partial class Panels : CanvasLayer
{
    private Control _current;

    public bool IsOpen => _current != null;

    public override void _Ready()
    {
        Layer = 5;
    }

    public void Open(string title, string body, string button, Action onConfirm, PanelDecoration decoration = PanelDecoration.None, int width = 900, int fontSize = 25)
    {
        Open(title, body, new List<(string, Action)> { (button, onConfirm) }, decoration, width, fontSize);
    }

    public void Open(string title, string body, IReadOnlyList<(string Label, Action Action)> buttons, PanelDecoration decoration = PanelDecoration.None, int width = 900, int fontSize = 25)
    {
        Close();
        var root = new Control
        {
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = Control.MouseFilterEnum.Stop,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        };
        root.AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.72f), AnchorRight = 1, AnchorBottom = 1 });
        var center = new CenterContainer { AnchorRight = 1, AnchorBottom = 1 };
        root.AddChild(center);

        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", -52);
        center.AddChild(stack);

        if (decoration == PanelDecoration.Victory)
        {
            stack.AddChild(VictoryCrest());
        }

        var ribbon = GuiSkin.Ribbon(title);
        ribbon.ZIndex = 2;
        stack.AddChild(ribbon);

        var window = new PanelContainer { CustomMinimumSize = new Vector2(width, 0) };
        var style = GuiSkin.Box("window_wide", 120, 130, 120, 130);
        style.ContentMarginLeft = 110;
        style.ContentMarginRight = 110;
        style.ContentMarginTop = 100;
        style.ContentMarginBottom = 90;
        window.AddThemeStyleboxOverride("panel", style);
        stack.AddChild(window);

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 30);
        window.AddChild(column);

        var text = new RichTextLabel { BbcodeEnabled = true, FitContent = true, Text = body, ScrollActive = false, CustomMinimumSize = new Vector2(width - 220, 0) };
        text.AddThemeFontSizeOverride("normal_font_size", fontSize);
        text.AddThemeFontSizeOverride("bold_font_size", fontSize);
        text.AddThemeFontSizeOverride("italics_font_size", fontSize);
        text.AddThemeColorOverride("default_color", GuiSkin.InkColor);
        column.AddChild(text);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 24);
        column.AddChild(row);
        Button first = null;
        foreach (var (label, action) in buttons)
        {
            var button = GuiSkin.LongButton(label);
            var callback = action;
            button.Pressed += () =>
            {
                Close();
                callback?.Invoke();
            };
            row.AddChild(button);
            first ??= button;
        }

        AddChild(root);
        _current = root;
        first?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private static Control VictoryCrest()
    {
        var rays = GuiSkin.Texture("fx_light_rays");
        var crown = GuiSkin.Texture("crown");
        var holder = new Control { CustomMinimumSize = new Vector2(rays.GetWidth(), rays.GetHeight()), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        var raysRect = new TextureRect { Texture = rays, Position = Vector2.Zero, Size = rays.GetSize(), MouseFilter = Control.MouseFilterEnum.Ignore };
        var crownRect = new TextureRect
        {
            Texture = crown,
            Size = crown.GetSize(),
            Position = new Vector2((rays.GetWidth() - crown.GetWidth()) / 2f, rays.GetHeight() - crown.GetHeight() - 50),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        holder.AddChild(raysRect);
        holder.AddChild(crownRect);
        holder.ZIndex = 3;
        return holder;
    }

    public void Close()
    {
        _current?.QueueFree();
        _current = null;
    }
}
