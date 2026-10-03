using Godot;

namespace DungeonHunter.Game;

public enum Weapon
{
    Dagger,
    Bow,
}

public sealed class Hero
{
    public const int HpCap = 3;
    public const int BowMinRange = 2;
    public const int BowMaxRange = 8;
    public const int ThrowMaxRange = 6;

    public Vector2I Position { get; set; }
    public int Hp { get; set; } = 3;
    public int MaxHp { get; set; } = 3;
    public int Arrows { get; set; } = 5;
    public Weapon Equipped { get; set; } = Weapon.Dagger;
    public bool StoneCarried { get; set; } = true;
    public bool StoneLit { get; set; } = true;
    public Vector2I StonePosition { get; set; }
    public bool IsDead => Hp <= 0;
}
