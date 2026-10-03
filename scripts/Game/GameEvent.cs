using Godot;

namespace DungeonHunter.Game;

public enum GameEventKind
{
    HeroMoved,
    HeroAttacked,
    HeroShot,
    HeroHurt,
    HeroDied,
    MonsterMoved,
    MonsterAttacked,
    MonsterHurt,
    MonsterDied,
    Arrow,
    StoneThrown,
    DoorChanged,
    Sound,
    Message,
}

public readonly record struct GameEvent(GameEventKind Kind, int ActorId = -1, Vector2I From = default, Vector2I To = default, string Text = null);
