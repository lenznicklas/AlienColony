using Godot;

namespace AlienColony.Buildings;

public partial class Building : Node2D
{
    [Export] public string BuildingId { get; set; } = "building";
    [Export] public string DisplayName { get; set; } = "Building";
    [Export] public Vector2I GridSize { get; set; } = Vector2I.One;
    [Export] public int CellSize { get; set; } = 64;

    public Vector2I GridOrigin { get; set; }

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 pixelSize = new(GridSize.X * CellSize, GridSize.Y * CellSize);
        Rect2 rect = new(-pixelSize * 0.5f, pixelSize);

        Color body = BuildingId switch
        {
            "main_core" => new Color("#56a8ff"),
            "mining_drill" => new Color("#d49b45"),
            "storage" => new Color("#85c46c"),
            _ => new Color("#c8c8c8")
        };

        DrawRect(rect.Grow(-5), body);
        DrawRect(rect.Grow(-5), new Color("#e7f1f5"), false, 3f);

        // Simple prototype details so each building is recognizable without assets.
        if (BuildingId == "main_core")
        {
            DrawCircle(Vector2.Zero, Mathf.Min(pixelSize.X, pixelSize.Y) * 0.22f, new Color("#d8f3ff"));
            DrawCircle(Vector2.Zero, Mathf.Min(pixelSize.X, pixelSize.Y) * 0.11f, new Color("#2e6fa7"));
        }
        else if (BuildingId == "mining_drill")
        {
            DrawLine(new Vector2(-18, -24), new Vector2(18, 24), new Color("#3d3023"), 8f);
            DrawLine(new Vector2(18, -24), new Vector2(-18, 24), new Color("#3d3023"), 8f);
        }
        else if (BuildingId == "storage")
        {
            DrawRect(new Rect2(-22, -16, 44, 32), new Color("#45663b"), false, 5f);
        }
    }
}
