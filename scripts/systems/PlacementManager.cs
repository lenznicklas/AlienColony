using Godot;
using AlienColony.Buildings;

namespace AlienColony.Systems;

public partial class PlacementManager : Node2D
{
    [Signal] public delegate void PlacementModeChangedEventHandler(bool active);

    private GridManager _grid = null!;
    private ResourceManager _resources = null!;
    private SelectionManager _selection = null!;
    private Node2D _buildings = null!;

    private Building? _preview;
    private Building? _movingBuilding;
    private Vector2I _movingOriginalOrigin;

    private string _selectedId = "";
    private string _selectedName = "";
    private Vector2I _selectedSize = Vector2I.One;

    public bool IsPlacing => _preview != null;

    public override void _Ready()
    {
        _grid = GetNode<GridManager>("../GridManager");
        _resources = GetNode<ResourceManager>("../ResourceManager");
        _selection = GetNode<SelectionManager>("../SelectionManager");
        _buildings = GetNode<Node2D>("../Buildings");

        SpawnBuilding("main_core", "Main Core", new Vector2I(3, 3), new Vector2I(13, 13), false);
    }

    public override void _Process(double delta)
    {
        if (_preview == null)
            return;

        Vector2I origin = GetCurrentGridOrigin();
        _preview.GlobalPosition = _grid.GridToWorldCentered(origin, _selectedSize);

        bool valid = _grid.CanPlace(origin, _selectedSize, _movingBuilding);
        _preview.Modulate = valid
            ? new Color(0.55f, 1f, 0.65f, 0.70f)
            : new Color(1f, 0.35f, 0.35f, 0.70f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.Key1:
                    BeginMiningDrill();
                    break;
                case Key.Key2:
                    BeginStorage();
                    break;
                case Key.Key3:
                    BeginSmelter();
                    break;
                case Key.Key4:
                    BeginPowerGenerator();
                    break;
                case Key.Escape:
                    CancelPlacement();
                    break;
                case Key.Delete:
                    DeleteSelected();
                    break;
            }
        }

        if (_preview == null)
            return;

        if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                Vector2I origin = GetCurrentGridOrigin();

                if (_grid.CanPlace(origin, _selectedSize, _movingBuilding))
                    ConfirmPlacement(origin);

                GetViewport().SetInputAsHandled();
            }
            else if (mouse.ButtonIndex == MouseButton.Right)
            {
                CancelPlacement();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void BeginMiningDrill() =>
        BeginPlacement("mining_drill", "Mining Drill", new Vector2I(2, 2));

    public void BeginStorage() =>
        BeginPlacement("storage", "Storage", new Vector2I(2, 2));

    public void BeginSmelter() =>
        BeginPlacement("smelter", "Smelter", new Vector2I(2, 2));

    public void BeginPowerGenerator() =>
        BeginPlacement("power_generator", "Power Generator", new Vector2I(2, 2));

    public void BeginMoveSelected()
    {
        Building? building = _selection.SelectedBuilding;

        if (building == null || building.BuildingId == "main_core")
            return;

        CancelPlacement();

        _movingBuilding = building;
        _movingOriginalOrigin = building.GridOrigin;

        _grid.FreeArea(building.GridOrigin, building.GridSize, building);
        building.Visible = false;

        _selectedId = building.BuildingId;
        _selectedName = building.DisplayName;
        _selectedSize = building.GridSize;

        CreatePreview();
        EmitSignal(SignalName.PlacementModeChanged, true);
    }

    public void DeleteSelected()
    {
        Building? building = _selection.SelectedBuilding;

        if (building == null || building.BuildingId == "main_core")
            return;

        _grid.FreeArea(building.GridOrigin, building.GridSize, building);
        _resources.UnregisterBuilding(building.BuildingId);
        _selection.ClearSelection();
        building.QueueFree();
    }

    public void CancelPlacement()
    {
        _preview?.QueueFree();
        _preview = null;

        if (_movingBuilding != null)
        {
            _movingBuilding.Visible = true;
            _movingBuilding.GlobalPosition =
                _grid.GridToWorldCentered(_movingOriginalOrigin, _movingBuilding.GridSize);
            _movingBuilding.GridOrigin = _movingOriginalOrigin;
            _grid.OccupyArea(_movingOriginalOrigin, _movingBuilding.GridSize, _movingBuilding);
            _movingBuilding = null;
        }

        _selectedId = "";
        EmitSignal(SignalName.PlacementModeChanged, false);
    }

    private void BeginPlacement(string id, string displayName, Vector2I size)
    {
        CancelPlacement();
        _selection.ClearSelection();

        _selectedId = id;
        _selectedName = displayName;
        _selectedSize = size;

        CreatePreview();
        EmitSignal(SignalName.PlacementModeChanged, true);
    }

    private void CreatePreview()
    {
        _preview = CreateBuildingNode(_selectedId, _selectedName, _selectedSize);
        _preview.ZIndex = 1000;
        AddChild(_preview);
    }

    private Vector2I GetCurrentGridOrigin()
    {
        return _grid.WorldToGrid(GetGlobalMousePosition());
    }

    private void ConfirmPlacement(Vector2I origin)
    {
        if (_movingBuilding != null)
        {
            Building building = _movingBuilding;
            building.Visible = true;
            building.GridOrigin = origin;
            building.GlobalPosition = _grid.GridToWorldCentered(origin, building.GridSize);
            _grid.OccupyArea(origin, building.GridSize, building);

            _movingBuilding = null;
            _preview?.QueueFree();
            _preview = null;
            _selectedId = "";

            _selection.Select(building);
            EmitSignal(SignalName.PlacementModeChanged, false);
            return;
        }

        SpawnBuilding(_selectedId, _selectedName, _selectedSize, origin, true);
    }

    private Building? SpawnBuilding(
        string id,
        string displayName,
        Vector2I size,
        Vector2I origin,
        bool registerResources)
    {
        if (!_grid.CanPlace(origin, size))
            return null;

        Building building = CreateBuildingNode(id, displayName, size);
        building.GridOrigin = origin;
        building.GlobalPosition = _grid.GridToWorldCentered(origin, size);
        building.Selected += OnBuildingSelected;

        _buildings.AddChild(building);
        _grid.OccupyArea(origin, size, building);

        if (registerResources)
            _resources.RegisterBuilding(id);

        return building;
    }

    private void OnBuildingSelected(Building building)
    {
        if (_preview != null)
            return;

        _selection.Select(building);
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
