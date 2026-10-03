using Godot;

namespace DungeonHunter.Game;

public enum MonsterBehavior
{
    Guard,
    Rest,
    Explorer,
}

public enum MonsterState
{
    Normal,
    Investigate,
    Alert,
    Search,
    Hunt,
}

public sealed class MonsterType
{
    public string Id { get; init; }
    public string DisplayName { get; init; }
    public string Sprite { get; init; }
    public int Hp { get; init; }
    public int Damage { get; init; }
    public int SightRange { get; init; }

    public static MonsterType Get(string id) => id switch
    {
        _ => new MonsterType { Id = "goblin", DisplayName = "Gobelin", Sprite = "goblin", Hp = 1, Damage = 1, SightRange = 8 },
    };
}

public sealed class Monster
{
    public int Id { get; init; }
    public MonsterType Type { get; init; }
    public MonsterBehavior Behavior { get; init; }
    public Vector2I Home { get; init; }
    public Vector2I Position { get; set; }
    public MonsterState State { get; set; } = MonsterState.Normal;
    public int Hp { get; set; }
    public int ArrowsStuck { get; set; }
    public Vector2I? Target { get; set; }
    public Vector2I LastSeenHero { get; set; }
    public bool Lingering { get; set; }
    public int LingerTurns { get; set; }
    public Vector2I LingerCenter { get; set; }
    public bool ReturnToHunt { get; set; }
    public Vector2I? DoorToClose { get; set; }
    public bool IsDead => Hp <= 0;

    public string StateLabel => State switch
    {
        MonsterState.Normal => Behavior switch
        {
            MonsterBehavior.Guard => "Garde",
            MonsterBehavior.Rest => "Repos",
            _ => "Explore",
        },
        MonsterState.Investigate => "Intrigué",
        MonsterState.Alert => "Alerte",
        MonsterState.Search => "Cherche",
        _ => "Chasse",
    };
}
