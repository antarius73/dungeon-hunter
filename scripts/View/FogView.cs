using DungeonHunter.Core;
using DungeonHunter.Game;
using Godot;

namespace DungeonHunter.View;

public partial class FogView : Node2D
{
    private static readonly Color Unseen = new(0, 0, 0, 1);
    private static readonly Color Remembered = new(0.02f, 0.02f, 0.06f, 0.72f);

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
        var size = new Vector2(Grid.CellSize, Grid.CellSize);
        for (var y = -1; y < _world.Level.Height; y++)
        {
            for (var x = 0; x < _world.Level.Width; x++)
            {
                var cell = new Vector2I(x, y);
                if (_world.Lit.Contains(cell))
                {
                    continue;
                }
                var color = _world.Seen.Contains(cell) ? Remembered : Unseen;
                DrawRect(new Rect2(Grid.CellToWorld(cell), size), color);
            }
        }
    }
}
