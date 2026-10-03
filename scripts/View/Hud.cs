using System;
using System.Collections.Generic;
using DungeonHunter.Core;
using DungeonHunter.Game;
using Godot;

namespace DungeonHunter.View;

public enum HudAction
{
    None,
    Dagger,
    Bow,
    Stone,
    Interact,
}

public partial class Hud : CanvasLayer
{
    public const int BarHeight = 96;

    private readonly List<TextureRect> _hearts = new();
    private Button _daggerSlot;
    private Button _bowSlot;
    private Label _arrows;
    private Button _stoneButton;
    private TextureRect _stoneIcon;
    private TextureButton _interactButton;
    private HudAction _active;
    private World _world;
    private Label _message;
    private Label _turn;

    public event Action HelpPressed;
    public event Action PausePressed;
    public event Action<Weapon> WeaponPressed;
    public event Action StonePressed;
    public event Action InteractPressed;
    public event Action WaitPressed;

    public override void _Ready()
    {
        var root = new Control
        {
            AnchorLeft = 0,
            AnchorRight = 1,
            AnchorTop = 1,
            AnchorBottom = 1,
            OffsetTop = -BarHeight,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        AddChild(root);

        var bar = new Panel { AnchorRight = 1, AnchorBottom = 1, MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddThemeStyleboxOverride("panel", GuiSkin.Box("bar_dark_xl", 26, 26, 26, 26));
        root.AddChild(bar);

        var row = new HBoxContainer { AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 26, OffsetRight = -26, OffsetTop = 11, OffsetBottom = -10 };
        row.AddThemeConstantOverride("separation", 22);
        root.AddChild(row);

        var hearts = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        hearts.AddThemeConstantOverride("separation", 4);
        for (var i = 0; i < Hero.HpCap; i++)
        {
            var heart = PixelIcon("ui_heart_full", new Vector2(45, 42));
            _hearts.Add(heart);
            hearts.AddChild(heart);
        }
        row.AddChild(hearts);

        _daggerSlot = WeaponSlot("weapon_knife", "1", Weapon.Dagger);
        _bowSlot = WeaponSlot("weapon_bow", "2", Weapon.Bow);
        row.AddChild(_daggerSlot);
        row.AddChild(_bowSlot);

        _arrows = Badge(_bowSlot);

        _stoneButton = SquareButton("coin_anim_f0", "L", "Pierre de lumière (L)", out _stoneIcon);
        _stoneButton.Pressed += () => StonePressed?.Invoke();
        row.AddChild(_stoneButton);

        _interactButton = IconButtonWithKey("interact", "G", "Interagir : ramasser, fermer une porte, récupérer la pierre (G)");
        _interactButton.Pressed += () => InteractPressed?.Invoke();
        row.AddChild(_interactButton);

        var wait = IconButtonWithKey("wait", "Esp", "Attendre (Espace)");
        wait.Pressed += () => WaitPressed?.Invoke();
        row.AddChild(wait);

        var info = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        info.AddThemeConstantOverride("separation", 2);
        _message = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, ClipText = true, CustomMinimumSize = new Vector2(0, 50), VerticalAlignment = VerticalAlignment.Center };
        _message.AddThemeFontSizeOverride("font_size", 21);
        _message.AddThemeColorOverride("font_color", GuiSkin.LightText);
        _turn = new Label { HorizontalAlignment = HorizontalAlignment.Right };
        _turn.AddThemeFontSizeOverride("font_size", 17);
        _turn.AddThemeColorOverride("font_color", new Color(0.78f, 0.68f, 0.52f));
        info.AddChild(_message);
        info.AddChild(_turn);
        row.AddChild(info);

        var help = GuiSkin.IconButton("help", "Aide (H)");
        help.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        help.Pressed += () => HelpPressed?.Invoke();
        row.AddChild(help);
        var pause = GuiSkin.IconButton("pause", "Pause (Échap)");
        pause.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        pause.Pressed += () => PausePressed?.Invoke();
        row.AddChild(pause);
    }

    public void Refresh(World world)
    {
        _world = world;
        var hero = world.Hero;
        for (var i = 0; i < _hearts.Count; i++)
        {
            _hearts[i].Visible = i < hero.MaxHp;
            _hearts[i].Texture = TileAtlas.Get(i < hero.Hp ? "ui_heart_full" : "ui_heart_empty");
        }
        SetEquipped(_daggerSlot, hero.Equipped == Weapon.Dagger, _active == HudAction.Dagger);
        SetEquipped(_bowSlot, hero.Equipped == Weapon.Bow, _active == HudAction.Bow);
        _arrows.Text = hero.Arrows.ToString();
        _stoneButton.Disabled = !hero.StoneCarried;
        _stoneButton.TooltipText = !hero.StoneCarried ? "Pierre de lumière : au sol (R ou clic dessus pour la récupérer)" : hero.StoneLit ? "Pierre de lumière : allumée (L)" : "Pierre de lumière : éteinte (L)";
        SetSquareState(_stoneButton, _active == HudAction.Stone);
        _stoneIcon.Modulate = !hero.StoneCarried ? new Color(1, 1, 1, 0.3f) : hero.StoneLit ? Colors.White : new Color(0.4f, 0.4f, 0.45f);
        _interactButton.Modulate = _active == HudAction.Interact ? new Color(1.25f, 1.25f, 0.9f) : Colors.White;
        _turn.Text = $"Tour {world.Turn}";
    }

    public void ShowMessage(string text) => _message.Text = text;

    public void SetActive(HudAction action)
    {
        _active = action;
        if (_world != null)
        {
            Refresh(_world);
        }
    }

    private static void SetEquipped(Button slot, bool equipped, bool active = false)
    {
        slot.AddThemeStyleboxOverride("normal", GuiSkin.SquareButtonBox(active ? "pressed" : equipped ? "hover" : "disabled"));
        slot.AddThemeStyleboxOverride("hover", GuiSkin.SquareButtonBox(active ? "pressed" : equipped ? "hover" : "normal"));
    }

    private static void SetSquareState(Button button, bool active)
    {
        button.AddThemeStyleboxOverride("normal", GuiSkin.SquareButtonBox(active ? "pressed" : "normal"));
        button.AddThemeStyleboxOverride("hover", GuiSkin.SquareButtonBox(active ? "pressed" : "hover"));
    }

    private static Button SquareButton(string texture, string key, string tooltip, out TextureRect icon)
    {
        var button = new Button { CustomMinimumSize = new Vector2(74, 74), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, FocusMode = Control.FocusModeEnum.None, TooltipText = tooltip };
        button.AddThemeStyleboxOverride("pressed", GuiSkin.SquareButtonBox("pressed"));
        button.AddThemeStyleboxOverride("disabled", GuiSkin.SquareButtonBox("disabled"));
        SetSquareState(button, false);
        icon = PixelIcon(texture, new Vector2(40, 46));
        icon.Position = new Vector2(17, 14);
        icon.Size = new Vector2(40, 46);
        button.AddChild(icon);
        button.AddChild(KeyLabel(key));
        return button;
    }

    private static TextureButton IconButtonWithKey(string icon, string key, string tooltip)
    {
        var button = GuiSkin.IconButton(icon, tooltip);
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.AddChild(KeyLabel(key));
        return button;
    }

    private static Label KeyLabel(string key)
    {
        var label = new Label { Text = key, Position = new Vector2(10, 4), MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", GuiSkin.LightText);
        label.AddThemeColorOverride("font_outline_color", new Color(0.15f, 0.25f, 0.05f));
        label.AddThemeConstantOverride("outline_size", 5);
        return label;
    }

    private static Label Badge(Control parent)
    {
        var badge = new Label { Position = new Vector2(44, 46), Size = new Vector2(28, 26), HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = Control.MouseFilterEnum.Ignore };
        badge.AddThemeFontSizeOverride("font_size", 20);
        badge.AddThemeColorOverride("font_color", Colors.White);
        badge.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.06f, 0.02f));
        badge.AddThemeConstantOverride("outline_size", 6);
        parent.AddChild(badge);
        return badge;
    }

    private Button WeaponSlot(string texture, string key, Weapon weapon)
    {
        var slot = new Button { CustomMinimumSize = new Vector2(74, 74), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, FocusMode = Control.FocusModeEnum.None, TooltipText = weapon == Weapon.Bow ? "Arc (2)" : "Dague (1)" };
        slot.AddThemeStyleboxOverride("pressed", GuiSkin.SquareButtonBox("pressed"));
        SetEquipped(slot, false);
        var icon = PixelIcon(texture, new Vector2(46, 50));
        icon.Position = new Vector2(14, 12);
        icon.Size = new Vector2(46, 50);
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        slot.AddChild(icon);
        slot.AddChild(KeyLabel(key));
        slot.Pressed += () => WeaponPressed?.Invoke(weapon);
        return slot;
    }

    private static TextureRect PixelIcon(string texture, Vector2 size) => new()
    {
        Texture = TileAtlas.Get(texture),
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
