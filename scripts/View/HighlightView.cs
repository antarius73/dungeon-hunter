using System.Collections.Generic;
using DungeonHunter.Core;
using Godot;

namespace DungeonHunter.View;

public partial class HighlightView : Node2D
{
    private readonly List<(Vector2I Cell, Color Color)> _cells = new();
    private Vector2I? _ring;

    public void ShowRing(Vector2I? cell)
    {
        _ring = cell;
        QueueRedraw();
    }

    public void Show(IEnumerable<(Vector2I Cell, Color Color)> cells)
    {
        _cells.Clear();
        _cells.AddRange(cells);
        QueueRedraw();
    }

    public void Clear()
    {
        _cells.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_ring.HasValue)
        {
            var center = Grid.CellCenter(_ring.Value);
            DrawCircle(center, 7.5f, new Color(1f, 0.9f, 0.45f, 0.18f));
            DrawArc(center, 7.5f, 0, Mathf.Tau, 32, new Color(1f, 0.88f, 0.4f, 0.95f), 1.2f, true);
        }
        foreach (var (cell, color) in _cells)
        {
            var rect = new Rect2(Grid.CellToWorld(cell) + Vector2.One, new Vector2(Grid.CellSize - 2, Grid.CellSize - 2));
            DrawRect(rect, color);
            DrawRect(rect, new Color(color, Mathf.Min(1f, color.A * 2.5f)), false, 1f);
        }
    }
}
