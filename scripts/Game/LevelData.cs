using System.Collections.Generic;
using Godot;

namespace DungeonHunter.Game;

public enum Terrain
{
    Wall,
    Floor,
    Door,
    Stairs,
}

public sealed class MonsterSpawn
{
    public string Kind { get; init; }
    public Vector2I Position { get; init; }
    public MonsterBehavior Behavior { get; init; }
}

public sealed class LevelData
{
    public string Name { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public Terrain[,] Terrain { get; private set; }
    public HashSet<Vector2I> OpenDoors { get; } = new();
    public Dictionary<Vector2I, Vector2I> DoorPartners { get; } = new();
    public Vector2I HeroStart { get; private set; }
    public List<MonsterSpawn> Monsters { get; } = new();

    public static LevelData Load(string path)
    {
        var text = FileAccess.GetFileAsString(path);
        var root = Json.ParseString(text).AsGodotDictionary();
        var rows = root["map"].AsStringArray();
        var level = new LevelData
        {
            Name = root.ContainsKey("name") ? root["name"].AsString() : path.GetFile(),
            Height = rows.Length,
            Width = rows.Length > 0 ? rows[0].Length : 0,
        };
        level.Terrain = new Terrain[level.Width, level.Height];
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var symbol = x < rows[y].Length ? rows[y][x] : '#';
                var cell = new Vector2I(x, y);
                level.Terrain[x, y] = symbol switch
                {
                    '#' => Game.Terrain.Wall,
                    '+' or '/' => Game.Terrain.Door,
                    '>' => Game.Terrain.Stairs,
                    _ => Game.Terrain.Floor,
                };
                switch (symbol)
                {
                    case '/':
                        level.OpenDoors.Add(cell);
                        break;
                    case '@':
                        level.HeroStart = cell;
                        break;
                    case 'g':
                        level.Monsters.Add(new MonsterSpawn { Kind = "goblin", Position = cell, Behavior = MonsterBehavior.Guard });
                        break;
                    case 'r':
                        level.Monsters.Add(new MonsterSpawn { Kind = "goblin", Position = cell, Behavior = MonsterBehavior.Rest });
                        break;
                    case 'e':
                        level.Monsters.Add(new MonsterSpawn { Kind = "goblin", Position = cell, Behavior = MonsterBehavior.Explorer });
                        break;
                }
            }
        }
        level.PairDoors();
        return level;
    }

    private void PairDoors()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var cell = new Vector2I(x, y);
                if (Terrain[x, y] != Game.Terrain.Door || DoorPartners.ContainsKey(cell))
                {
                    continue;
                }
                var right = cell + Vector2I.Right;
                if (x + 1 >= Width || Terrain[right.X, right.Y] != Game.Terrain.Door)
                {
                    throw new System.InvalidOperationException($"Porte isolée en ({x}, {y}) : une porte occupe toujours deux cases côte à côte (++).");
                }
                DoorPartners[cell] = right;
                DoorPartners[right] = cell;
                if (OpenDoors.Contains(cell) || OpenDoors.Contains(right))
                {
                    OpenDoors.Add(cell);
                    OpenDoors.Add(right);
                }
            }
        }
    }

    public Vector2I DoorLeft(Vector2I cell) => DoorPartners.TryGetValue(cell, out var other) && other.X < cell.X ? other : cell;
}
