using Godot;
using AlienColony.Systems;

namespace AlienColony.World;

public partial class WorldGrid : Node2D
{
    private GridManager _grid = null!;
    private Texture2D? _groundTexture;

    public override void _Ready()
    {
        _grid = GetNode<GridManager>("../GridManager");

        if (ResourceLoader.Exists("res://assets/terrain/ground.png"))
            _groundTexture = GD.Load<Texture2D>("res://assets/terrain/ground.png");

        QueueRedraw();
    }

    public override void _Draw()
    {
        int cell = _grid.CellSize;
        int width = _grid.MapSize.X * cell;
        int height = _grid.MapSize.Y * cell;

        // Draw the generated ground art once across the whole playable map.
        // It stays subtle so the placement grid remains easy to read.
        if (_groundTexture != null)
        {
            DrawTextureRect(
                _groundTexture,
                new Rect2(0, 0, width, height),
                true,
                new Color(0.78f, 0.82f, 0.78f, 1f)
            );
        }
        else
        {
            DrawRect(new Rect2(0, 0, width, height), new Color("#24362f"));
        }

        Color minor = new(0.15f, 0.23f, 0.20f, 0.34f);
        Color major = new(0.30f, 0.46f, 0.39f, 0.55f);

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
