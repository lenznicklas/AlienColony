using Godot;

namespace AlienColony.Buildings;

public partial class Building : Node2D
{
    [Signal] public delegate void SelectedEventHandler(Building building);

    [Export] public string BuildingId { get; set; } = "building";
    [Export] public string DisplayName { get; set; } = "Building";
    [Export] public Vector2I GridSize { get; set; } = Vector2I.One;
    [Export] public int CellSize { get; set; } = 64;

    public Vector2I GridOrigin { get; set; }
    public bool IsSelected { get; private set; }

    private Sprite2D? _sprite;

    public override void _Ready()
    {
        CreateSprite();
        QueueRedraw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse ||
            !mouse.Pressed ||
            mouse.ButtonIndex != MouseButton.Left)
            return;

        Vector2 local = ToLocal(GetGlobalMousePosition());
        Vector2 pixelSize = new(GridSize.X * CellSize, GridSize.Y * CellSize);
        Rect2 bounds = new(-pixelSize * 0.5f, pixelSize);

        if (!bounds.HasPoint(local))
            return;

        EmitSignal(SignalName.Selected, this);
        GetViewport().SetInputAsHandled();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 pixelSize = new(GridSize.X * CellSize, GridSize.Y * CellSize);
        Rect2 footprint = new(-pixelSize * 0.5f, pixelSize);

        if (IsSelected)
        {
            DrawRect(
                footprint.Grow(5),
                new Color(0.25f, 0.9f, 1f, 0.18f),
                true
            );
            DrawRect(
                footprint.Grow(5),
                new Color("#63e2ff"),
                false,
                4f
            );
        }
    }

    private void CreateSprite()
    {
        string path = GetTexturePath();

        if (!ResourceLoader.Exists(path))
            return;

        Texture2D texture = GD.Load<Texture2D>(path);

        _sprite = new Sprite2D
        {
            Texture = texture,
            Centered = true
        };

        // Generated source assets are large. Scale every building into a predictable
        // visual box while keeping its transparent proportions.
        Vector2 target = new(
            GridSize.X * CellSize * 1.35f,
            GridSize.Y * CellSize * 1.55f
        );

        Vector2 texSize = texture.GetSize();
        float scale = Mathf.Min(target.X / texSize.X, target.Y / texSize.Y);
        _sprite.Scale = Vector2.One * scale;

        // Lift the art slightly so its base sits more naturally on the footprint.
        _sprite.Position = new Vector2(0, -CellSize * 0.18f);
        AddChild(_sprite);
    }

    private string GetTexturePath()
    {
        return BuildingId switch
        {
            "main_core" => "res://assets/buildings/main_core.png",
            "mining_drill" => "res://assets/buildings/mining_drill.png",
            "storage" => "res://assets/buildings/storage.png",
            "smelter" => "res://assets/buildings/smelter.png",
            "power_generator" => "res://assets/buildings/power_generator.png",
            "wall" => "res://assets/buildings/wall.png",
            "turret" => "res://assets/buildings/turret.png",
            _ => ""
        };
    }
}
