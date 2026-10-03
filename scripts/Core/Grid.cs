using System;
using Godot;

namespace DungeonHunter.Core;

public static class Grid
{
    public const int CellSize = 16;

    public static readonly Vector2I[] Orthogonal = { Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left };

    public static readonly Vector2I[] AllDirections =
    {
        Vector2I.Up, Vector2I.Right, Vector2I.Down, Vector2I.Left,
        new(1, -1), new(1, 1), new(-1, 1), new(-1, -1),
    };

    public static int Chebyshev(Vector2I a, Vector2I b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    public static int DistanceSquared(Vector2I a, Vector2I b)
    {
        var d = a - b;
        return d.X * d.X + d.Y * d.Y;
    }

    public static bool IsOrthogonal(Vector2I direction) => (direction.X == 0) != (direction.Y == 0);

    public static bool IsOrthogonallyAdjacent(Vector2I a, Vector2I b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;

    public static Vector2 CellToWorld(Vector2I cell) => new(cell.X * CellSize, cell.Y * CellSize);

    public static Vector2 CellFeet(Vector2I cell) => new(cell.X * CellSize + CellSize / 2f, cell.Y * CellSize + CellSize);

    public static Vector2 CellCenter(Vector2I cell) => new(cell.X * CellSize + CellSize / 2f, cell.Y * CellSize + CellSize / 2f);

    public static Vector2I WorldToCell(Vector2 position) => new((int)Mathf.Floor(position.X / CellSize), (int)Mathf.Floor(position.Y / CellSize));
}
