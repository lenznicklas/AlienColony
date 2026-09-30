using Godot;
using AlienColony.Systems;

namespace AlienColony.World;

public partial class WorldGrid : Node2D
{
    private GridManager _grid = null!;

    public override void _Ready()
    {
        _grid = GetNode<GridManager>("../GridManager");
        QueueRedraw();
    }

    public override void _Draw()
    {
        int cell = _grid.CellSize;
        int width = _grid.MapSize.X * cell;
        int height = _grid.MapSize.Y * cell;

        DrawRect(
            new Rect2(0, 0, width, height),
            new Color("#24362f")
        );

        Color minor = new("#31483e");
        Color major = new("#436052");

        for (int x = 0; x <= _grid.MapSize.X; x++)
        {
            float px = x * cell;
            DrawLine(
                new Vector2(px, 0),
                new Vector2(px, height),
                x % 5 == 0 ? major : minor,
                x % 5 == 0 ? 2f : 1f
            );
        }

        for (int y = 0; y <= _grid.MapSize.Y; y++)
        {
            float py = y * cell;
            DrawLine(
                new Vector2(0, py),
                new Vector2(width, py),
                y % 5 == 0 ? major : minor,
                y % 5 == 0 ? 2f : 1f
            );
        }

        DrawRect(
            new Rect2(0, 0, width, height),
            new Color("#87a98e"),
            false,
            4f
        );
    }
}
