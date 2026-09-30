using Godot;
using System;
using AlienColony.Buildings;

namespace AlienColony.Systems;

public partial class PlacementManager : Node2D
{
    public event Action<bool>? PlacementModeChanged;
    public event Action<string>? MessageRequested;

    private GridManager _grid = null!;
    private ResourceManager _resources = null!;
    private SelectionManager _selection = null!;
    private Node2D _buildings = null!;

    private Building? _preview;
    private Building? _movingBuilding;
    private Vector2I _movingOriginalOrigin;

    private string _selectedId = "";

    public bool IsPlacing => _preview != null;

    public override void _Ready()
    {
        _grid = GetNode<GridManager>("../GridManager");
        _resources = GetNode<ResourceManager>("../ResourceManager");
        _selection = GetNode<SelectionManager>("../SelectionManager");
        _buildings = GetNode<Node2D>("../Buildings");

        SpawnBuilding("main_core", new Vector2I(13, 13), false);
    }

    public override void _Process(double delta)
    {
        if (_preview == null)
            return;

        BuildingDefinition def = BuildingCatalog.Get(_selectedId);
        Vector2I origin = GetCurrentGridOrigin();

        _preview.GlobalPosition =
            _grid.GridToWorldCentered(origin, def.GridSize);

        bool valid = _grid.CanPlace(origin, def.GridSize, _movingBuilding);
        bool affordable = _movingBuilding != null || _resources.CanAfford(_selectedId);

        _preview.Modulate = valid && affordable
            ? new Color(0.55f, 1f, 0.65f, 0.74f)
            : new Color(1f, 0.35f, 0.35f, 0.74f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.Key1: BeginMiningDrill(); break;
                case Key.Key2: BeginStorage(); break;
                case Key.Key3: BeginSmelter(); break;
                case Key.Key4: BeginPowerGenerator(); break;
                case Key.Escape: CancelPlacement(); break;
                case Key.Delete: DeleteSelected(); break;
            }
        }

        if (_preview == null)
            return;

        if (@event is not InputEventMouseButton mouse || !mouse.Pressed)
            return;

        if (mouse.ButtonIndex == MouseButton.Left)
        {
            ConfirmCurrentPlacement();
            GetViewport().SetInputAsHandled();
        }
        else if (mouse.ButtonIndex == MouseButton.Right)
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
        }
    }

    public void BeginMiningDrill() => BeginPlacement("mining_drill");
    public void BeginStorage() => BeginPlacement("storage");
    public void BeginSmelter() => BeginPlacement("smelter");
    public void BeginPowerGenerator() => BeginPlacement("power_generator");

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
        CreatePreview();

        PlacementModeChanged?.Invoke(true);
    }

    public void DeleteSelected()
    {
        Building? building = _selection.SelectedBuilding;

        if (building == null || building.BuildingId == "main_core")
            return;

        _grid.FreeArea(building.GridOrigin, building.GridSize, building);

        _resources.UnregisterBuilding(building.BuildingId);
        _resources.RefundBuilding(building.BuildingId, 0.5);

        _selection.ClearSelection();
        building.QueueFree();

        MessageRequested?.Invoke("Building dismantled — 50% resources refunded.");
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

            _grid.OccupyArea(
                _movingOriginalOrigin,
                _movingBuilding.GridSize,
                _movingBuilding
            );

            _movingBuilding = null;
        }

        _selectedId = "";
        PlacementModeChanged?.Invoke(false);
    }

    private void BeginPlacement(string buildingId)
    {
        CancelPlacement();
        _selection.ClearSelection();

        _selectedId = buildingId;

        if (!_resources.CanAfford(buildingId))
        {
            BuildingDefinition def = BuildingCatalog.Get(buildingId);
            MessageRequested?.Invoke(
                $"Not enough resources — needs {def.OreCost} Ore / {def.MetalCost} Metal."
            );
        }

        CreatePreview();
        PlacementModeChanged?.Invoke(true);
    }

    private void CreatePreview()
    {
        BuildingDefinition def = BuildingCatalog.Get(_selectedId);

        _preview = CreateBuildingNode(def);
        _preview.ZIndex = 1000;

        AddChild(_preview);
    }

    private void ConfirmCurrentPlacement()
    {
        BuildingDefinition def = BuildingCatalog.Get(_selectedId);
        Vector2I origin = GetCurrentGridOrigin();

        if (!_grid.CanPlace(origin, def.GridSize, _movingBuilding))
        {
            MessageRequested?.Invoke("Cannot build here.");
            return;
        }

        if (_movingBuilding != null)
        {
            Building building = _movingBuilding;

            building.Visible = true;
            building.GridOrigin = origin;
            building.GlobalPosition =
                _grid.GridToWorldCentered(origin, building.GridSize);

            _grid.OccupyArea(origin, building.GridSize, building);

            _movingBuilding = null;
            _preview?.QueueFree();
            _preview = null;
            _selectedId = "";

            _selection.Select(building);
            PlacementModeChanged?.Invoke(false);
            return;
        }

        if (!_resources.TryPayForBuilding(_selectedId))
        {
            MessageRequested?.Invoke("Not enough resources.");
            return;
        }

        Building? placed = SpawnBuilding(_selectedId, origin, true);

        if (placed == null)
        {
            // Should be rare because grid was already checked.
            _resources.RefundBuilding(_selectedId, 1.0);
            return;
        }

        MessageRequested?.Invoke($"{def.DisplayName} constructed.");
    }

    private Building? SpawnBuilding(
        string buildingId,
        Vector2I origin,
        bool registerResources)
    {
        BuildingDefinition def = BuildingCatalog.Get(buildingId);

        if (!_grid.CanPlace(origin, def.GridSize))
            return null;

        Building building = CreateBuildingNode(def);

        building.GridOrigin = origin;
        building.GlobalPosition =
            _grid.GridToWorldCentered(origin, def.GridSize);

        building.Selected += OnBuildingSelected;

        _buildings.AddChild(building);
        _grid.OccupyArea(origin, def.GridSize, building);

        if (registerResources)
            _resources.RegisterBuilding(buildingId);

        return building;
    }

    private Building CreateBuildingNode(BuildingDefinition def)
    {
        return new Building
        {
            BuildingId = def.Id,
            DisplayName = def.DisplayName,
            GridSize = def.GridSize,
            CellSize = _grid.CellSize
        };
    }

    private Vector2I GetCurrentGridOrigin()
    {
        return _grid.WorldToGrid(GetGlobalMousePosition());
    }

    private void OnBuildingSelected(Building building)
    {
        if (_preview != null)
            return;

        _selection.Select(building);
    }
}
