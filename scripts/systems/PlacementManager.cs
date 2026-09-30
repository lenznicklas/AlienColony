using Godot;
using AlienColony.Buildings;

namespace AlienColony.Systems;

public partial class PlacementManager : Node2D
{
    private GridManager _grid = null!;
    private Node2D _buildings = null!;
    private Building? _preview;

    private string _selectedId = "";
    private string _selectedName = "";
    private Vector2I _selectedSize = Vector2I.One;

    public override void _Ready()
    {
        _grid = GetNode<GridManager>("../GridManager");
        _buildings = GetNode<Node2D>("../Buildings");

        // Starter building so the colony has an initial center.
        SpawnBuilding("main_core", "Main Core", new Vector2I(3, 3), new Vector2I(13, 13));
    }

    public override void _Process(double delta)
    {
        if (_preview == null)
            return;

        Vector2I origin = GetCurrentGridOrigin();
        _preview.GlobalPosition = _grid.GridToWorldCentered(origin, _selectedSize);

        bool valid = _grid.CanPlace(origin, _selectedSize);
        _preview.Modulate = valid
            ? new Color(0.55f, 1f, 0.65f, 0.72f)
            : new Color(1f, 0.35f, 0.35f, 0.72f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Key1)
                BeginPlacement("mining_drill", "Mining Drill", new Vector2I(2, 2));
            else if (key.Keycode == Key.Key2)
                BeginPlacement("storage", "Storage", new Vector2I(2, 2));
            else if (key.Keycode == Key.Escape)
                CancelPlacement();
        }

        if (_preview == null)
            return;

        if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                Vector2I origin = GetCurrentGridOrigin();

                if (_grid.CanPlace(origin, _selectedSize))
                    PlaceSelected(origin);

                GetViewport().SetInputAsHandled();
            }
            else if (mouse.ButtonIndex == MouseButton.Right)
            {
                CancelPlacement();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void BeginMiningDrill()
    {
        BeginPlacement("mining_drill", "Mining Drill", new Vector2I(2, 2));
    }

    public void BeginStorage()
    {
        BeginPlacement("storage", "Storage", new Vector2I(2, 2));
    }

    public void CancelPlacement()
    {
        _preview?.QueueFree();
        _preview = null;
        _selectedId = "";
    }

    private void BeginPlacement(string id, string displayName, Vector2I size)
    {
        CancelPlacement();

        _selectedId = id;
        _selectedName = displayName;
        _selectedSize = size;

        _preview = CreateBuildingNode(id, displayName, size);
        _preview.ZIndex = 100;
        AddChild(_preview);
    }

    private Vector2I GetCurrentGridOrigin()
    {
        Vector2 mouseWorld = GetGlobalMousePosition();
        return _grid.WorldToGrid(mouseWorld);
    }

    private void PlaceSelected(Vector2I origin)
    {
        SpawnBuilding(_selectedId, _selectedName, _selectedSize, origin);
        // Keep placement mode active so the player can place several buildings.
    }

    private void SpawnBuilding(string id, string displayName, Vector2I size, Vector2I origin)
    {
        if (!_grid.CanPlace(origin, size))
            return;

        Building building = CreateBuildingNode(id, displayName, size);
        building.GridOrigin = origin;
        building.GlobalPosition = _grid.GridToWorldCentered(origin, size);

        _buildings.AddChild(building);
        _grid.OccupyArea(origin, size, building);
    }

    private Building CreateBuildingNode(string id, string displayName, Vector2I size)
    {
        return new Building
        {
            BuildingId = id,
            DisplayName = displayName,
            GridSize = size,
            CellSize = _grid.CellSize
        };
    }
}
