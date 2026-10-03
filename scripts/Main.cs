using System.Collections.Generic;
using System.Linq;
using DungeonHunter.Core;
using DungeonHunter.Game;
using DungeonHunter.View;
using Godot;

namespace DungeonHunter;

public partial class Main : Node2D
{
    private const string LevelPath = "res://levels/level_01.json";
    private const float CameraZoom = 2.4f;
    private const float CameraFollowSpeed = 8f;

    private enum InputMode
    {
        Play,
        Attack,
        Aim,
        Stone,
        Interact,
        Locked,
    }

    private static readonly Color MoveColor = new(0.45f, 0.85f, 1f, 0.16f);
    private static readonly Color AttackColor = new(1f, 0.35f, 0.3f, 0.28f);
    private static readonly Color AimColor = new(1f, 0.85f, 0.35f, 0.22f);

    private World _world;
    private Camera2D _camera;
    private LevelView _levelView;
    private Node2D _items;
    private Node2D _actors;
    private FogView _fog;
    private HighlightView _highlight;
    private HighlightView _highlightDark;
    private Node2D _sounds;
    private Hud _hud;
    private Panels _panels;
    private Label _tooltip;
    private ActorView _hero;
    private AnimatedSprite2D _stone;
    private readonly Dictionary<int, ActorView> _monsterViews = new();
    private InputMode _mode = InputMode.Locked;
    private Vector2I _aimTarget;
    private Vector2I _stoneCursor;
    private bool _pickHover;
    private bool _mouseMoved;
    private double _busyUntil;
    private System.Func<bool> _pendingAction;

    public override void _Ready()
    {
        var level = LevelData.Load(LevelPath);
        _world = new World(level, (int)(Time.GetUnixTimeFromSystem() % int.MaxValue));

        _camera = new Camera2D { Zoom = new Vector2(CameraZoom, CameraZoom) };
        AddChild(_camera);

        _levelView = new LevelView { Name = "Level" };
        _items = new Node2D { Name = "Items" };
        _actors = new Node2D { Name = "Actors", YSortEnabled = true };
        _fog = new FogView { Name = "Fog" };
        _highlight = new HighlightView { Name = "Highlight" };
        _highlightDark = new HighlightView { Name = "HighlightDark" };
        _sounds = new Node2D { Name = "Sounds" };
        AddChild(_levelView);
        AddChild(_highlight);
        AddChild(_items);
        AddChild(_actors);
        AddChild(_fog);
        AddChild(_highlightDark);
        AddChild(_sounds);

        _hud = new Hud { Name = "Hud" };
        AddChild(_hud);
        _hud.HelpPressed += ShowHelp;
        _hud.PausePressed += ShowPause;
        _hud.WeaponPressed += OnWeaponSlot;
        _hud.StonePressed += OnStoneButton;
        _hud.InteractPressed += OnInteractButton;
        _hud.WaitPressed += OnWaitButton;
        _panels = new Panels { Name = "Panels" };
        AddChild(_panels);

        var tooltipLayer = new CanvasLayer { Layer = 4 };
        _tooltip = new Label { Visible = false, TextureFilter = CanvasItem.TextureFilterEnum.Linear };
        _tooltip.AddThemeFontSizeOverride("font_size", 22);
        _tooltip.AddThemeColorOverride("font_color", GuiSkin.InkColor);
        var tooltipStyle = GuiSkin.Box("slot_value", 14, 14, 14, 14);
        tooltipStyle.ContentMarginLeft = 18;
        tooltipStyle.ContentMarginRight = 18;
        tooltipStyle.ContentMarginTop = 8;
        tooltipStyle.ContentMarginBottom = 8;
        _tooltip.AddThemeStyleboxOverride("normal", tooltipStyle);
        tooltipLayer.AddChild(_tooltip);
        AddChild(tooltipLayer);

        _levelView.Bind(_world);
        _fog.Bind(_world);

        _hero = ActorView.Create("elf_m", _world.Hero.Position);
        _actors.AddChild(_hero);
        foreach (var monster in _world.Monsters)
        {
            var view = ActorView.Create(monster.Type.Sprite, monster.Position);
            _monsterViews[monster.Id] = view;
            _actors.AddChild(view);
        }
        _stone = new AnimatedSprite2D { SpriteFrames = TileAtlas.BuildFrames("coin"), Visible = false };
        _stone.Play("idle");
        _items.AddChild(_stone);

        RefreshAll();
        _camera.Position = CameraTarget();
        _hud.ShowMessage(level.Name);
        _panels.Open("Dungeon Hunter", StoryText(), "Démarrer", () => SetMode(InputMode.Play));
    }

    public override void _Process(double delta)
    {
        if (_pendingAction != null && !IsBusy && _mode != InputMode.Locked && !_panels.IsOpen)
        {
            Act(_pendingAction);
        }
        UpdateHover();
        _mouseMoved = false;
        _camera.Position = _camera.Position.Lerp(CameraTarget(), Mathf.Min(1f, (float)delta * CameraFollowSpeed));
        UpdateTooltip();
    }

    private Vector2 CameraTarget()
    {
        var viewport = GetViewportRect().Size;
        var visibleHeight = (viewport.Y - Hud.BarHeight) / CameraZoom;
        var levelWidth = _world.Level.Width * Grid.CellSize;
        var levelHeight = _world.Level.Height * Grid.CellSize;
        var hudShift = Hud.BarHeight / (2f * CameraZoom);
        var top = -Grid.CellSize * 2f;
        var focus = _hero.Position.Y - Grid.CellSize / 2f;
        float y;
        if (visibleHeight >= levelHeight - top)
        {
            y = (top + levelHeight) / 2f;
        }
        else
        {
            y = Mathf.Clamp(focus, top + visibleHeight / 2f, levelHeight - visibleHeight / 2f);
        }
        var visibleWidth = viewport.X / CameraZoom;
        var x = visibleWidth >= levelWidth ? levelWidth / 2f : Mathf.Clamp(_hero.Position.X, visibleWidth / 2f, levelWidth - visibleWidth / 2f);
        return new Vector2(x, y + hudShift);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        if (@event is InputEventMouseMotion)
        {
            _mouseMoved = true;
        }
        if (@event is InputEventKey { Pressed: true } key)
        {
            HandleKey(key);
        }
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            HandleClick(Grid.WorldToCell(GetGlobalMousePosition()));
        }
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            Cancel();
        }
    }

    private void HandleKey(InputEventKey key)
    {
        var code = key.PhysicalKeycode;
        if (code == Key.Escape)
        {
            if (_mode == InputMode.Play)
            {
                ShowPause();
                return;
            }
            Cancel();
            return;
        }
        if (code is Key.H or Key.F1)
        {
            ShowHelp();
            return;
        }
        if (key.Echo && (_mode != InputMode.Play || IsBusy))
        {
            return;
        }

        var direction = DirectionFor(code);
        switch (_mode)
        {
            case InputMode.Play:
                HandlePlayKey(code, direction, key.Echo);
                break;
            case InputMode.Attack:
                HandleAttackKey(code, direction);
                break;
            case InputMode.Aim:
                HandleAimKey(code, direction);
                break;
            case InputMode.Stone:
                HandleStoneKey(code, direction);
                break;
            case InputMode.Interact:
                HandleInteractKey(code, direction);
                break;
        }
    }

    private void HandlePlayKey(Key code, Vector2I? direction, bool echo)
    {
        if (direction.HasValue)
        {
            Act(() => _world.TryMove(direction.Value));
            return;
        }
        if (echo)
        {
            return;
        }
        switch (code)
        {
            case Key.Space or Key.Kp5:
                Act(_world.Wait);
                break;
            case Key.Key1:
                OnWeaponSlot(Weapon.Dagger);
                break;
            case Key.Key2:
                OnWeaponSlot(Weapon.Bow);
                break;
            case Key.F:
                StartAttack();
                break;
            case Key.L:
                OnStoneButton();
                break;
            case Key.R:
                if (_world.Hero.StoneCarried)
                {
                    _hud.ShowMessage("Vous avez déjà la pierre.");
                    break;
                }
                Act(_world.RecallStone);
                break;
            case Key.G:
                OnInteractButton();
                break;
        }
    }

    private void Cancel()
    {
        if (_mode is InputMode.Play or InputMode.Locked)
        {
            return;
        }
        SetMode(InputMode.Play);
        _hud.ShowMessage("Action annulée.");
    }

    private void StartAttack()
    {
        if (_world.Hero.Equipped == Weapon.Bow)
        {
            StartAim();
            return;
        }
        SetMode(InputMode.Attack);
        _hud.ShowMessage("Dague : une direction ou un clic sur une case en surbrillance. Échap ou clic droit pour annuler.");
    }

    private void HandleAttackKey(Key code, Vector2I? direction)
    {
        if (direction.HasValue)
        {
            if (!Grid.IsOrthogonal(direction.Value))
            {
                _hud.ShowMessage("La dague frappe dans les 4 directions seulement.");
                return;
            }
            SetMode(InputMode.Play);
            var target = direction.Value;
            Act(() => _world.TryAttack(target));
            return;
        }
        if (code is Key.F or Key.Key1)
        {
            Cancel();
        }
    }

    private void StartStone()
    {
        var hero = _world.Hero;
        if (!hero.StoneCarried)
        {
            _hud.ShowMessage("La pierre est au sol : R ou clic dessus pour la récupérer.");
            return;
        }
        _stoneCursor = hero.Position;
        SetMode(InputMode.Stone);
        _hud.ShowMessage("Pierre : clic sur une case pour la lancer (direction puis Entrée au clavier). L ou bouton pierre : " + (hero.StoneLit ? "l'éteindre." : "l'allumer."));
    }

    private void HandleStoneKey(Key code, Vector2I? direction)
    {
        if (direction.HasValue)
        {
            MoveStoneCursor(direction.Value);
            return;
        }
        switch (code)
        {
            case Key.L:
                ToggleStoneLight();
                break;
            case Key.Enter or Key.KpEnter or Key.Space:
                ThrowStone(_stoneCursor);
                break;
        }
    }

    private void MoveStoneCursor(Vector2I direction)
    {
        var hero = _world.Hero.Position;
        var targets = _world.StoneTargets();
        var offset = _stoneCursor - hero;
        var distance = Grid.Chebyshev(_stoneCursor, hero);
        var currentDirection = distance == 0 ? Vector2I.Zero : new Vector2I(Mathf.Sign(offset.X), Mathf.Sign(offset.Y));
        Vector2I next;
        if (distance > 0 && currentDirection == direction)
        {
            next = _stoneCursor + direction;
        }
        else if (distance > 0 && currentDirection == -direction)
        {
            next = _stoneCursor - currentDirection;
        }
        else
        {
            next = hero;
            for (var i = 1; i <= Hero.ThrowMaxRange; i++)
            {
                if (!targets.Contains(hero + direction * i))
                {
                    break;
                }
                next = hero + direction * i;
            }
        }
        if (targets.Contains(next))
        {
            _stoneCursor = next;
        }
        RefreshHighlight();
    }

    private void ThrowStone(Vector2I target)
    {
        SetMode(InputMode.Play);
        Act(() => _world.ThrowStoneAt(target));
    }

    private void ToggleStoneLight()
    {
        SetMode(InputMode.Play);
        Act(_world.ToggleStone);
    }

    private void OnStoneButton()
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        if (_mode == InputMode.Stone)
        {
            ToggleStoneLight();
            return;
        }
        StartStone();
    }

    private void OnInteractButton()
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        if (_mode == InputMode.Interact)
        {
            Cancel();
            return;
        }
        var targets = _world.InteractionTargets();
        if (targets.Count == 0)
        {
            _hud.ShowMessage("Rien à ramasser ni à fermer à portée.");
            return;
        }
        if (targets.Count == 1)
        {
            SetMode(InputMode.Play);
            var target = targets[0];
            Act(() => _world.Interact(target));
            return;
        }
        SetMode(InputMode.Interact);
        _hud.ShowMessage("Interagir : clic sur une case en surbrillance, ou une direction (G ou Entrée pour votre case). Échap pour annuler.");
    }

    private void HandleInteractKey(Key code, Vector2I? direction)
    {
        var hero = _world.Hero.Position;
        var targets = _world.InteractionTargets();
        Vector2I? choice = null;
        if (direction.HasValue && targets.Contains(hero + direction.Value))
        {
            choice = hero + direction.Value;
        }
        else if (code is Key.G or Key.Enter or Key.KpEnter or Key.Space && targets.Contains(hero))
        {
            choice = hero;
        }
        if (choice.HasValue)
        {
            SetMode(InputMode.Play);
            var target = choice.Value;
            Act(() => _world.Interact(target));
        }
    }

    private void OnWaitButton()
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        SetMode(InputMode.Play);
        Act(_world.Wait);
    }

    private void StartAim()
    {
        var targets = AimTargets();
        _aimTarget = targets.Count > 0 ? targets[0] : _world.Hero.Position + Vector2I.Right * Hero.BowMaxRange;
        SetMode(InputMode.Aim);
        _hud.ShowMessage(targets.Count > 0
            ? "Arc : une direction ou un clic pour tirer, Tab pour changer de cible, Entrée pour tirer sur la cible. Échap ou clic droit pour annuler."
            : "Arc : une direction ou un clic pour tirer dans cette direction. Échap ou clic droit pour annuler.");
    }

    private List<Vector2I> AimTargets() => _world.LivingMonsters
        .Where(m => _world.IsMonsterVisible(m))
        .Select(m => m.Position)
        .Where(c => Grid.Chebyshev(c, _world.Hero.Position) is >= Hero.BowMinRange and <= Hero.BowMaxRange)
        .OrderBy(c => Grid.DistanceSquared(c, _world.Hero.Position))
        .ToList();

    private void HandleAimKey(Key code, Vector2I? direction)
    {
        if (direction.HasValue)
        {
            SetMode(InputMode.Play);
            var shot = direction.Value;
            Act(() => _world.TryShoot(shot));
            return;
        }
        switch (code)
        {
            case Key.Tab:
                var targets = AimTargets();
                if (targets.Count > 0)
                {
                    var index = targets.IndexOf(_aimTarget);
                    _aimTarget = targets[(index + 1) % targets.Count];
                }
                RefreshHighlight();
                break;
            case Key.Enter or Key.KpEnter or Key.F or Key.Space:
                SetMode(InputMode.Play);
                var target = _aimTarget;
                Act(() => _world.TryShootAt(target));
                break;
            case Key.Key2:
                Cancel();
                break;
        }
    }

    private void HandleClick(Vector2I cell)
    {
        var hero = _world.Hero.Position;
        var delta = cell - hero;
        switch (_mode)
        {
            case InputMode.Aim:
                if (delta != Vector2I.Zero)
                {
                    SetMode(InputMode.Play);
                    Act(() => _world.TryShootAt(cell));
                }
                return;
            case InputMode.Attack:
                if (Grid.Chebyshev(cell, hero) == 1 && Grid.IsOrthogonal(delta))
                {
                    SetMode(InputMode.Play);
                    Act(() => _world.TryAttack(delta));
                }
                return;
            case InputMode.Stone:
                if (_world.StoneTargets().Contains(cell))
                {
                    ThrowStone(cell);
                }
                return;
            case InputMode.Interact:
                if (_world.InteractionTargets().Contains(cell))
                {
                    SetMode(InputMode.Play);
                    Act(() => _world.Interact(cell));
                }
                return;
            case InputMode.Play:
                break;
            default:
                return;
        }
        if (!_world.Hero.StoneCarried && cell == _world.Hero.StonePosition)
        {
            Act(_world.RecallStone);
            return;
        }
        if (_world.CanReachArrows(cell))
        {
            Act(() => _world.PickUpArrows(cell));
            return;
        }
        if (delta == Vector2I.Zero)
        {
            Act(_world.Wait);
            return;
        }
        if (Grid.Chebyshev(cell, hero) == 1)
        {
            Act(() => _world.TryMove(delta));
            return;
        }
        if (_world.Hero.Equipped == Weapon.Bow)
        {
            Act(() => _world.TryShootAt(cell));
        }
    }

    private void UpdateHover()
    {
        var overWorld = GetViewport().GetMousePosition().Y < GetViewportRect().Size.Y - Hud.BarHeight && !_panels.IsOpen;
        var cell = Grid.WorldToCell(GetGlobalMousePosition());
        var hero = _world.Hero;
        var pickable = overWorld && _mode == InputMode.Play
            && ((!hero.StoneCarried && cell == hero.StonePosition) || _world.CanReachArrows(cell));
        if (pickable != _pickHover)
        {
            _pickHover = pickable;
            Input.SetDefaultCursorShape(pickable ? Input.CursorShape.PointingHand : Input.CursorShape.Arrow);
            _highlight.ShowRing(pickable ? cell : null);
        }
        if (!_mouseMoved || !overWorld)
        {
            return;
        }
        if (_mode == InputMode.Aim && cell != _aimTarget && cell != hero.Position && _world.InBounds(cell))
        {
            _aimTarget = cell;
            RefreshHighlight();
        }
        else if (_mode == InputMode.Stone && cell != _stoneCursor && _world.StoneTargets().Contains(cell))
        {
            _stoneCursor = cell;
            RefreshHighlight();
        }
    }

    private bool IsBusy => Time.GetTicksMsec() / 1000.0 < _busyUntil;

    private void Act(System.Func<bool> action)
    {
        if (IsBusy)
        {
            _pendingAction = action;
            return;
        }
        _pendingAction = null;
        var turnTaken = action();
        ProcessEvents();
        RefreshAll();
        if (turnTaken)
        {
            _busyUntil = Time.GetTicksMsec() / 1000.0 + ActorView.StepDuration + 0.02;
        }
        if (_world.Lost)
        {
            SetMode(InputMode.Locked);
            GetTree().CreateTimer(0.6).Timeout += () => _panels.Open("Game Over", "Vous avez succombé dans les profondeurs.\n\nLes gobelins se partagent déjà votre équipement.", new List<(string, System.Action)> { ("Recommencer", Restart), ("Quitter", () => GetTree().Quit()) });
        }
        else if (_world.Won)
        {
            SetMode(InputMode.Locked);
            GetTree().CreateTimer(0.4).Timeout += () => _panels.Open("Niveau terminé", $"Vous avez trouvé l'escalier vers le niveau inférieur en {_world.Turn} tours.\n\nFin du prototype V0.", "Rejouer", Restart, PanelDecoration.Victory);
        }
    }

    private void Restart() => GetTree().ReloadCurrentScene();

    private void ShowHelp()
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        SetMode(InputMode.Play);
        _panels.Open("Commandes", HelpText(), "Reprendre", () => SetMode(InputMode.Play), PanelDecoration.None, 1400, 21);
    }

    private void ShowPause()
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        SetMode(InputMode.Play);
        _panels.Open("Pause", "La partie est en pause.", new List<(string, System.Action)>
        {
            ("Reprendre", () => SetMode(InputMode.Play)),
            ("Recommencer", Restart),
            ("Quitter", () => GetTree().Quit()),
        });
    }

    private void OnWeaponSlot(Weapon weapon)
    {
        if (_panels.IsOpen || _mode == InputMode.Locked)
        {
            return;
        }
        if (_world.Hero.Equipped != weapon)
        {
            SetMode(InputMode.Play);
            Act(() => _world.Equip(weapon));
            return;
        }
        if (_mode is InputMode.Attack or InputMode.Aim)
        {
            Cancel();
            return;
        }
        StartAttack();
    }

    private void ProcessEvents()
    {
        string lastMessage = null;
        foreach (var e in _world.Events)
        {
            switch (e.Kind)
            {
                case GameEventKind.HeroMoved:
                    _hero.MoveTo(e.To);
                    break;
                case GameEventKind.HeroAttacked:
                case GameEventKind.HeroShot:
                    _hero.Attack(DirectionOf(e.To - e.From));
                    break;
                case GameEventKind.HeroHurt:
                    _hero.Hurt();
                    break;
                case GameEventKind.MonsterMoved:
                    _monsterViews[e.ActorId].MoveTo(e.To);
                    break;
                case GameEventKind.MonsterAttacked:
                    _monsterViews[e.ActorId].Lunge(e.To - e.From);
                    break;
                case GameEventKind.MonsterHurt:
                    _monsterViews[e.ActorId].Hurt();
                    break;
                case GameEventKind.MonsterDied:
                    _monsterViews[e.ActorId].Die();
                    _monsterViews.Remove(e.ActorId);
                    break;
                case GameEventKind.Arrow:
                    ShowArrow(e.From, e.To);
                    break;
                case GameEventKind.StoneThrown:
                    ShowStoneFlight(e.From, e.To);
                    break;
                case GameEventKind.DoorChanged:
                    _levelView.Refresh();
                    break;
                case GameEventKind.Sound:
                    ShowSound(e.To, e.Text);
                    break;
                case GameEventKind.Message:
                    lastMessage = e.Text;
                    break;
            }
        }
        _world.Events.Clear();
        if (lastMessage != null)
        {
            _hud.ShowMessage(lastMessage);
        }
    }

    private static Vector2I DirectionOf(Vector2I delta)
    {
        if (delta == Vector2I.Zero)
        {
            return Vector2I.Right;
        }
        var angle = Mathf.Atan2(delta.Y, delta.X);
        var octant = Mathf.RoundToInt(angle / (Mathf.Pi / 4));
        return new Vector2I(Mathf.RoundToInt(Mathf.Cos(octant * Mathf.Pi / 4)), Mathf.RoundToInt(Mathf.Sin(octant * Mathf.Pi / 4)));
    }

    private void RefreshWeapon()
    {
        var bow = _world.Hero.Equipped == Weapon.Bow;
        _hero.SetWeapon(bow ? "weapon_bow" : "weapon_knife", bow);
    }

    private void RefreshAll()
    {
        RefreshWeapon();
        _levelView.Refresh();
        _fog.Refresh();
        _hud.Refresh(_world);
        foreach (var monster in _world.Monsters.Where(m => !m.IsDead))
        {
            if (_monsterViews.TryGetValue(monster.Id, out var view))
            {
                view.Visible = _world.IsMonsterVisible(monster);
            }
        }
        RefreshItems();
        RefreshHighlight();
    }

    private void RefreshItems()
    {
        foreach (var child in _items.GetChildren())
        {
            if (child != _stone)
            {
                child.QueueFree();
            }
        }
        foreach (var (cell, count) in _world.RememberedArrows)
        {
            for (var i = 0; i < Mathf.Min(count, 3); i++)
            {
                _items.AddChild(new Sprite2D
                {
                    Texture = TileAtlas.Get("weapon_arrow"),
                    Position = Grid.CellCenter(cell) + new Vector2(i * 2 - 2, 0),
                    Rotation = Mathf.Pi / 2 + i * 0.35f,
                    Scale = new Vector2(0.6f, 0.6f),
                });
            }
        }
        var hero = _world.Hero;
        _stone.Visible = !hero.StoneCarried;
        if (!hero.StoneCarried)
        {
            _stone.Position = Grid.CellCenter(hero.StonePosition);
        }
    }

    private void RefreshHighlight()
    {
        var hero = _world.Hero.Position;
        var cells = new List<(Vector2I, Color)>();
        switch (_mode)
        {
            case InputMode.Play:
                foreach (var direction in Grid.AllDirections)
                {
                    var cell = hero + direction;
                    var monster = _world.MonsterAt(cell);
                    if (monster != null && _world.IsMonsterVisible(monster))
                    {
                        if (Grid.IsOrthogonal(direction) && _world.Hero.Equipped == Weapon.Dagger)
                        {
                            cells.Add((cell, AttackColor));
                        }
                    }
                    else if (_world.InBounds(cell) && _world.CornerFree(hero, direction) && (_world.IsPassable(cell) || (_world.IsClosedDoor(cell) && Grid.IsOrthogonal(direction))))
                    {
                        cells.Add((cell, MoveColor));
                    }
                }
                break;
            case InputMode.Attack:
                foreach (var direction in Grid.Orthogonal)
                {
                    cells.Add((hero + direction, AttackColor));
                }
                break;
            case InputMode.Aim:
                foreach (var cell in _world.ArrowPath(_aimTarget))
                {
                    cells.Add((cell, cell == _aimTarget ? AttackColor : AimColor));
                }
                if (Grid.Chebyshev(_aimTarget, hero) >= Hero.BowMinRange)
                {
                    cells.Add((_aimTarget, AttackColor));
                }
                break;
            case InputMode.Stone:
                foreach (var cell in _world.StoneTargets())
                {
                    cells.Add((cell, cell == _stoneCursor ? AttackColor : AimColor));
                }
                break;
            case InputMode.Interact:
                foreach (var cell in _world.InteractionTargets())
                {
                    cells.Add((cell, AimColor));
                }
                break;
        }
        _highlight.Show(cells.Where(c => _world.Lit.Contains(c.Item1)));
        _highlightDark.Show(cells.Where(c => !_world.Lit.Contains(c.Item1)));
    }

    private void SetMode(InputMode mode)
    {
        _mode = mode;
        _hud.SetActive(mode switch
        {
            InputMode.Attack => HudAction.Dagger,
            InputMode.Aim => HudAction.Bow,
            InputMode.Stone => HudAction.Stone,
            InputMode.Interact => HudAction.Interact,
            _ => HudAction.None,
        });
        RefreshHighlight();
    }

    private void ShowArrow(Vector2I from, Vector2I to)
    {
        var arrow = new Sprite2D { Texture = TileAtlas.Get("weapon_arrow"), Position = Grid.CellCenter(from) };
        var direction = (Vector2)(to - from);
        arrow.Rotation = direction.Angle() + Mathf.Pi / 2;
        _sounds.AddChild(arrow);
        var tween = CreateTween();
        tween.TweenProperty(arrow, "position", Grid.CellCenter(to), 0.05f * Mathf.Max(1, Grid.Chebyshev(from, to)));
        tween.TweenCallback(Callable.From(arrow.QueueFree));
    }

    private void ShowStoneFlight(Vector2I from, Vector2I to)
    {
        if (from == to)
        {
            return;
        }
        var flying = new AnimatedSprite2D { SpriteFrames = _stone.SpriteFrames, Position = Grid.CellCenter(from) };
        flying.Play("idle");
        _sounds.AddChild(flying);
        var tween = CreateTween();
        tween.TweenProperty(flying, "position", Grid.CellCenter(to), 0.18f);
        tween.TweenCallback(Callable.From(flying.QueueFree));
    }

    private void ShowSound(Vector2I cell, string text)
    {
        var label = new Label { Text = text + " !", Position = Grid.CellToWorld(cell) + new Vector2(-6, -6), Scale = new Vector2(0.5f, 0.5f) };
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.6f));
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
        _sounds.AddChild(label);
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", label.Position.Y - 8, 0.9f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0f, 0.9f);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private void UpdateTooltip()
    {
        if (_panels.IsOpen)
        {
            _tooltip.Visible = false;
            return;
        }
        var cell = Grid.WorldToCell(GetGlobalMousePosition());
        var monster = _world.MonsterAt(cell);
        if (monster == null || !_world.IsMonsterVisible(monster))
        {
            _tooltip.Visible = false;
            return;
        }
        _tooltip.Text = $"{monster.Type.DisplayName}  ·  PV {monster.Hp}/{monster.Type.Hp}  ·  {monster.StateLabel}";
        _tooltip.Position = GetViewport().GetMousePosition() + new Vector2(24, 18);
        _tooltip.Visible = true;
    }

    private static Vector2I? DirectionFor(Key code) => code switch
    {
        Key.Up or Key.W or Key.Kp8 => Vector2I.Up,
        Key.Down or Key.S or Key.Kp2 => Vector2I.Down,
        Key.Left or Key.A or Key.Kp4 => Vector2I.Left,
        Key.Right or Key.D or Key.Kp6 => Vector2I.Right,
        Key.Q or Key.Kp7 => new Vector2I(-1, -1),
        Key.E or Key.Kp9 => new Vector2I(1, -1),
        Key.Z or Key.Kp1 => new Vector2I(-1, 1),
        Key.C or Key.Kp3 => new Vector2I(1, 1),
        _ => null,
    };

    private static string StoryText() =>
        "Sous les collines, un ancien complexe nain oublié est passé aux mains du mal. Tout au fond règne [b]Zoltan[/b], le tyran qui terrorise la région.\n\n" +
        "Guidé par votre [b]pierre de lumière[/b], descendez de niveau en niveau. Dans ce premier niveau, trouvez l'[b]escalier[/b] qui mène plus bas.\n\n" +
        "Les gobelins voient dans le noir, entendent le moindre bruit et sont attirés par la lumière. Avancez prudemment.\n\n" +
        "[i][color=#7a5a35]Touche H pour l'aide à tout moment.[/color][/i]";

    private static string HelpText() =>
        "Chaque action se fait au clavier ou à la souris. Une action ciblée allume les cases possibles : on choisit la case au clavier ou d'un clic. [b]Échap[/b] ou [b]clic droit[/b] annule.\n\n" +
        "[b]Se déplacer[/b] : flèches, ZQSD ou pavé numérique, diagonales A E W C. Souris : clic sur une case voisine.\n" +
        "[b]Attendre[/b] : Espace, bouton Attendre, ou clic sur le héros.\n" +
        "[b]Ramasser[/b] : clic sur des flèches sur votre case ou une case voisine (le curseur devient une main).\n" +
        "[b]Dague[/b] (1 ou &) : avancer sur un gobelin, ou F puis une des 4 directions (gobelin, mur ou vide). Souris : clic sur un gobelin voisin, ou bouton Dague puis clic sur une case.\n" +
        "[b]Arc[/b] (2 ou é) : F puis une direction, ou Tab et Entrée pour viser un gobelin. Souris : bouton Arc puis clic dans la direction voulue, ou clic direct sur une case lointaine. La flèche file jusqu'à 8 cases.\n" +
        "[b]Pierre de lumière[/b] : L ou bouton Pierre allume les cases de lancer ; clic sur une case ou direction puis Entrée pour lancer. L ou bouton Pierre une 2e fois : allumer / éteindre. R ou clic sur la pierre au sol : la récupérer.\n" +
        "[b]Interagir[/b] : G ou bouton Interagir pour ramasser des flèches sur votre case ou une case voisine, fermer une porte voisine ou récupérer la pierre. Le curseur change sur un objet à ramasser.\n" +
        "[b]Porte[/b] : avancer dedans pour l'ouvrir.\n" +
        "[b]Aide[/b] : H. [b]Menu[/b] : Échap.";
}
