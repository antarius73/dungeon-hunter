using DungeonHunter.Core;
using Godot;

namespace DungeonHunter.View;

public partial class ActorView : Node2D
{
    public const float StepDuration = 0.14f;

    private static readonly Vector2 HandOffset = new(5, -9);

    private AnimatedSprite2D _sprite;
    private Tween _moveTween;
    private Node2D _weaponPivot;
    private Sprite2D _weapon;
    private int _baseTop;
    private Tween _weaponTween;
    private bool _weaponIsBow;
    private bool _attacking;

    public static ActorView Create(string spriteName, Vector2I cell)
    {
        var view = new ActorView { Name = spriteName, Position = Grid.CellFeet(cell) };
        var frames = TileAtlas.BuildFrames(spriteName);
        var sprite = new AnimatedSprite2D { SpriteFrames = frames, Centered = true };
        var height = frames.GetFrameTexture("idle", 0).GetHeight();
        sprite.Offset = new Vector2(0, -height / 2f);
        view._sprite = sprite;
        view.AddChild(sprite);
        sprite.Play("idle");
        sprite.Frame = (int)(GD.Randi() % 4);
        return view;
    }

    public void SetWeapon(string spriteName, bool isBow)
    {
        if (_weapon == null)
        {
            _weaponPivot = new Node2D { Name = "WeaponPivot" };
            AddChild(_weaponPivot);
            MoveChild(_weaponPivot, 0);
            _weapon = new Sprite2D { Centered = true };
            _weaponPivot.AddChild(_weapon);
            _baseTop = TileAtlas.OpaqueTop(_sprite.SpriteFrames.GetFrameTexture("idle", 0));
        }
        var texture = TileAtlas.Get(spriteName);
        _weapon.Texture = texture;
        _weapon.Offset = isBow ? Vector2.Zero : new Vector2(0, -texture.GetHeight() / 2f + 2);
        _weapon.Scale = isBow ? new Vector2(0.7f, 0.7f) : new Vector2(0.85f, 0.85f);
        _weaponIsBow = isBow;
        RestWeapon();
    }

    private void RestWeapon()
    {
        if (_weapon == null || _attacking)
        {
            return;
        }
        var side = _sprite.FlipH ? -1f : 1f;
        _weapon.Position = new Vector2(HandOffset.X * side, HandOffset.Y);
        _weapon.Rotation = _weaponIsBow ? (_sprite.FlipH ? Mathf.Pi : 0f) : 0.45f * side;
        _weapon.FlipV = _weaponIsBow && _sprite.FlipH;
    }

    public override void _Process(double delta)
    {
        if (_weaponPivot == null)
        {
            return;
        }
        var texture = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        _weaponPivot.Position = new Vector2(0, TileAtlas.OpaqueTop(texture) - _baseTop) + _sprite.Position;
    }

    public void Attack(Vector2I direction)
    {
        Face(direction);
        if (_weapon == null)
        {
            Lunge(direction);
            return;
        }
        var dir = ((Vector2)direction).Normalized();
        var center = new Vector2(0, -9);
        _attacking = true;
        _weaponTween?.Kill();
        _weaponTween = CreateTween();
        if (_weaponIsBow)
        {
            _weapon.Rotation = dir.Angle();
            _weapon.FlipV = false;
            _weapon.Position = center + dir * 7;
            _weaponTween.TweenProperty(_weapon, "position", center + dir * 4, 0.12f);
            _weaponTween.TweenProperty(_weapon, "position", center + dir * 9, 0.06f);
            _weaponTween.TweenInterval(0.12f);
        }
        else
        {
            var angle = dir.Angle() + Mathf.Pi / 2;
            _weapon.Rotation = angle - 0.9f;
            _weapon.Position = center + dir * 4;
            _weaponTween.TweenProperty(_weapon, "rotation", angle + 0.5f, 0.1f);
            _weaponTween.Parallel().TweenProperty(_weapon, "position", center + dir * 11, 0.1f);
            _weaponTween.TweenInterval(0.08f);
        }
        _weaponTween.TweenCallback(Callable.From(() =>
        {
            _attacking = false;
            RestWeapon();
        }));
        var body = CreateTween();
        body.TweenProperty(_sprite, "position", dir * 2, 0.06f);
        body.TweenProperty(_sprite, "position", Vector2.Zero, 0.1f);
    }

    public void MoveTo(Vector2I cell)
    {
        var target = Grid.CellFeet(cell);
        if (target.X < Position.X)
        {
            _sprite.FlipH = true;
        }
        else if (target.X > Position.X)
        {
            _sprite.FlipH = false;
        }
        RestWeapon();
        _moveTween?.Kill();
        _sprite.Play("run");
        _moveTween = CreateTween();
        _moveTween.TweenProperty(this, "position", target, StepDuration);
        _moveTween.TweenCallback(Callable.From(() => _sprite.Play("idle")));
    }

    public void Face(Vector2I direction)
    {
        if (direction.X != 0)
        {
            _sprite.FlipH = direction.X < 0;
        }
        RestWeapon();
    }

    public void Lunge(Vector2I direction)
    {
        Face(direction);
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "position", new Vector2(direction.X, direction.Y) * 4, 0.06f);
        tween.TweenProperty(_sprite, "position", Vector2.Zero, 0.08f);
    }

    public void Hurt()
    {
        if (_sprite.SpriteFrames.HasAnimation("hit"))
        {
            _sprite.Play("hit");
            GetTree().CreateTimer(0.3f).Timeout += () =>
            {
                if (IsInstanceValid(_sprite))
                {
                    _sprite.Play("idle");
                }
            };
        }
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", new Color(1, 0.3f, 0.3f), 0.05f);
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.25f);
    }

    public void Die()
    {
        _moveTween?.Kill();
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, 0.35f);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
