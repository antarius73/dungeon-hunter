using DungeonHunter.Core;
using DungeonHunter.Game;
using Godot;

namespace DungeonHunter.View;

public partial class LevelView : Node2D
{
    private static readonly Color VoidColor = new(0.11f, 0.09f, 0.12f);
    private static readonly string[] FloorVariants = { "floor_2", "floor_3", "floor_4", "floor_5", "floor_6", "floor_7", "floor_8" };

    private World _world;

    public void Bind(World world)
    {
        _world = world;
        QueueRedraw();
    }

    public void Refresh() => QueueRedraw();

    public override void _Draw()
    {
        if (_world == null)
        {
            return;
        }
        var level = _world.Level;
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var cell = new Vector2I(x, y);
                var position = Grid.CellToWorld(cell);
                if (_world.IsWall(cell))
                {
                    DrawRect(new Rect2(position, new Vector2(Grid.CellSize, Grid.CellSize)), VoidColor);
                    continue;
                }
                DrawTexture(TileAtlas.Get(FloorFor(cell)), position);
                if (_world.TerrainAt(cell) == Terrain.Stairs)
                {
                    DrawTexture(TileAtlas.Get("floor_stairs"), position);
                }
            }
        }

        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var cell = new Vector2I(x, y);
                if (_world.IsWall(cell))
                {
                    DrawWall(cell);
                }
            }
        }

        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var cell = new Vector2I(x, y);
                if (_world.IsDoor(cell) && _world.Level.DoorLeft(cell) == cell)
                {
                    DrawDoor(cell);
                }
            }
        }
    }

    private void DrawDoor(Vector2I left)
    {
        if (!_world.DoorRemembered(left, out var open))
        {
            return;
        }
        var origin = Grid.CellToWorld(left) + new Vector2(0, -Grid.CellSize);
        DrawTexture(TileAtlas.Get("doors_frame_top"), origin + new Vector2(0, -Grid.CellSize));
        DrawTexture(TileAtlas.Get(open ? "doors_leaf_open" : "doors_leaf_closed"), origin);
        DrawTexture(TileAtlas.Get("doors_frame_left"), origin + new Vector2(-Grid.CellSize, 0));
        DrawTexture(TileAtlas.Get("doors_frame_right"), origin + new Vector2(Grid.CellSize * 2, 0));
    }

    private void DrawWall(Vector2I cell)
    {
        var position = Grid.CellToWorld(cell);
        var below = cell + Vector2I.Down;
        var faceVisible = _world.InBounds(below) && !_world.IsWall(below);
        if (faceVisible)
        {
            DrawTexture(TileAtlas.Get("wall_mid"), position);
            DrawTexture(TileAtlas.Get("wall_top_mid"), position + new Vector2(0, -Grid.CellSize));
            return;
        }
        var left = cell + Vector2I.Left;
        var right = cell + Vector2I.Right;
        if (_world.InBounds(right) && !_world.IsWall(right) && !_world.IsDoor(right))
        {
            DrawTexture(TileAtlas.Get("wall_edge_mid_left"), position);
        }
        if (_world.InBounds(left) && !_world.IsWall(left) && !_world.IsDoor(left))
        {
            DrawTexture(TileAtlas.Get("wall_edge_mid_right"), position);
        }
    }

    private static string FloorFor(Vector2I cell)
    {
        var hash = (uint)(cell.X * 73856093 ^ cell.Y * 19349663);
        hash ^= hash >> 13;
        hash *= 0x5bd1e995;
        hash ^= hash >> 15;
        return hash % 100 < 80 ? "floor_1" : FloorVariants[hash % (uint)FloorVariants.Length];
    }
}
