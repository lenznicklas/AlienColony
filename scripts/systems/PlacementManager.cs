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

    // Mobile pointer state.
    private bool _touchPlacementActive;
    private Vector2 _touchWorldPosition;
    private bool _hasTouchWorldPosition;

    // Tap/click selection state.
    private bool _pointerDown;
    private Vector2 _pointerDownScreen;
    private const float TapThreshold = 18f;

    public bool IsPlacing => _preview != null;

    public override void _Ready()
    {
        _grid = GetNode<GridManager>("../GridManager");
        _resources = GetNode<ResourceManager>("../ResourceManager");
        _selection = GetNode<SelectionManager>("../SelectionManager");
        _buildings = GetNode<Node2D>("../Buildings");

        SpawnBuilding("main_core", new Vector2I(13, 13), false);

        SetProcessInput(true);
    }

    public override void _Process(double delta)
    {
        if (_preview == null || string.IsNullOrEmpty(_selectedId))
            return;

        BuildingDefinition def =
            BuildingCatalog.Get(_selectedId);

        Vector2 worldPosition =
            _hasTouchWorldPosition
                ? _touchWorldPosition
                : GetGlobalMousePosition();

        Vector2I origin =
            _grid.WorldToGrid(worldPosition);

        _preview.GlobalPosition =
            _grid.GridToWorldCentered(
                origin,
                def.GridSize
            );

        bool valid =
            _grid.CanPlace(
                origin,
                def.GridSize,
                _movingBuilding
            );

        bool affordable =
            _movingBuilding != null ||
            _resources.CanAfford(_selectedId);

        _preview.Modulate = valid && affordable
            ? new Color(0.55f, 1f, 0.65f, 0.80f)
            : new Color(1f, 0.30f, 0.30f, 0.80f);
    }

    public override void _Input(InputEvent @event)
    {
        HandleKeyboard(@event);

        if (@event is InputEventScreenTouch touch)
        {
            HandleTouch(touch);
            return;
        }

        if (@event is InputEventScreenDrag drag)
        {
            HandleTouchDrag(drag);
            return;
        }

        if (@event is InputEventMouseButton mouse)
        {
            HandleMouseButton(mouse);
            return;
        }
    }

    private void HandleKeyboard(InputEvent @event)
    {
        if (@event is not InputEventKey key ||
            !key.Pressed ||
            key.Echo)
            return;

        switch (key.Keycode)
        {
            case Key.Key1:
                BeginMiningDrill();
                GetViewport().SetInputAsHandled();
                break;

            case Key.Key2:
                BeginStorage();
                GetViewport().SetInputAsHandled();
                break;

            case Key.Key3:
                BeginSmelter();
                GetViewport().SetInputAsHandled();
                break;

            case Key.Key4:
                BeginPowerGenerator();
                GetViewport().SetInputAsHandled();
                break;

            case Key.Escape:
                if (IsPlacing)
                {
                    CancelPlacement();
                    GetViewport().SetInputAsHandled();
                }
                break;

            case Key.Delete:
                if (!IsPlacing)
                {
                    DeleteSelected();
                    GetViewport().SetInputAsHandled();
                }
                break;
        }
    }

    private void HandleTouch(InputEventScreenTouch touch)
    {
        Vector2 world =
            ScreenToWorld(touch.Position);

        if (IsPlacing)
        {
            _hasTouchWorldPosition = true;
            _touchWorldPosition = world;

            if (touch.Pressed)
            {
                // Finger may now drag the preview around.
                _touchPlacementActive = true;
            }
            else if (_touchPlacementActive)
            {
                // Confirm only when the finger is released.
                _touchPlacementActive = false;
                ConfirmPlacementAt(world);
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (touch.Pressed)
        {
            _pointerDown = true;
            _pointerDownScreen = touch.Position;
            return;
        }

        if (_pointerDown)
        {
            float moved =
                touch.Position.DistanceTo(_pointerDownScreen);

            _pointerDown = false;

            if (moved <= TapThreshold)
                SelectOrClearAt(world);
        }
    }

    private void HandleTouchDrag(InputEventScreenDrag drag)
    {
        if (!IsPlacing || !_touchPlacementActive)
            return;

        _hasTouchWorldPosition = true;
        _touchWorldPosition =
            ScreenToWorld(drag.Position);

        GetViewport().SetInputAsHandled();
    }

    private void HandleMouseButton(
        InputEventMouseButton mouse)
    {
        if (mouse.ButtonIndex == MouseButton.Left)
        {
            if (IsPlacing)
            {
                if (mouse.Pressed)
                {
                    ConfirmPlacementAt(
                        GetGlobalMousePosition()
                    );

                    GetViewport().SetInputAsHandled();
                }

                return;
            }

            if (mouse.Pressed)
            {
                _pointerDown = true;
                _pointerDownScreen = mouse.Position;
            }
            else if (_pointerDown)
            {
                float moved =
                    mouse.Position.DistanceTo(_pointerDownScreen);

                _pointerDown = false;

                if (moved <= TapThreshold)
                {
                    SelectOrClearAt(
                        GetGlobalMousePosition()
                    );
                }
            }

            return;
        }

        if (mouse.ButtonIndex == MouseButton.Right &&
            mouse.Pressed &&
            IsPlacing)
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
        }
    }

    private void SelectOrClearAt(Vector2 worldPosition)
    {
        Vector2I cell =
            _grid.WorldToGrid(worldPosition);

        Building? building =
            _grid.GetBuildingAt(cell);

        if (building == null)
        {
            _selection.ClearSelection();
            return;
        }

        _selection.Select(building);
    }

    public void BeginMiningDrill() =>
        BeginPlacement("mining_drill");

    public void BeginStorage() =>
        BeginPlacement("storage");

    public void BeginSmelter() =>
        BeginPlacement("smelter");

    public void BeginPowerGenerator() =>
        BeginPlacement("power_generator");

    public void BeginMoveSelected()
    {
        Building? building =
            _selection.SelectedBuilding;

        if (building == null ||
            building.BuildingId == "main_core")
            return;

        CancelPlacement();

        _movingBuilding = building;
        _movingOriginalOrigin = building.GridOrigin;

        _grid.FreeArea(
            building.GridOrigin,
            building.GridSize,
            building
        );

        building.Visible = false;

        _selectedId = building.BuildingId;

        // Start the preview at the building's old position.
        _touchWorldPosition =
            _grid.GridToWorldCentered(
                _movingOriginalOrigin,
                building.GridSize
            );

        _hasTouchWorldPosition = true;

        CreatePreview();

        MessageRequested?.Invoke(
            "Drag the building and release to place it."
        );

        PlacementModeChanged?.Invoke(true);
    }

    public void DeleteSelected()
    {
        Building? building =
            _selection.SelectedBuilding;

        if (building == null ||
            building.BuildingId == "main_core")
            return;

        _grid.FreeArea(
            building.GridOrigin,
            building.GridSize,
            building
        );

        _resources.UnregisterBuilding(
            building.BuildingId
        );

        _resources.RefundBuilding(
            building.BuildingId,
            0.5
        );

        _selection.ClearSelection();
        building.QueueFree();

        MessageRequested?.Invoke(
            "Building dismantled — 50% resources refunded."
        );
    }

    public void CancelPlacement()
    {
        if (_preview != null)
        {
            _preview.QueueFree();
            _preview = null;
        }

        if (_movingBuilding != null)
        {
            _movingBuilding.Visible = true;
            _movingBuilding.GridOrigin =
                _movingOriginalOrigin;

            _movingBuilding.GlobalPosition =
                _grid.GridToWorldCentered(
                    _movingOriginalOrigin,
                    _movingBuilding.GridSize
                );

            _grid.OccupyArea(
                _movingOriginalOrigin,
                _movingBuilding.GridSize,
                _movingBuilding
            );

            _movingBuilding = null;
        }

        _selectedId = "";
        _touchPlacementActive = false;
        _hasTouchWorldPosition = false;

        PlacementModeChanged?.Invoke(false);
    }

    private void BeginPlacement(string buildingId)
    {
        CancelPlacement();
        _selection.ClearSelection();

        _selectedId = buildingId;
        _hasTouchWorldPosition = false;

        CreatePreview();

        if (!_resources.CanAfford(buildingId))
        {
            BuildingDefinition def =
                BuildingCatalog.Get(buildingId);

            MessageRequested?.Invoke(
                $"Not enough resources — needs " +
                $"{def.OreCost} Ore / {def.MetalCost} Metal."
            );
        }

        PlacementModeChanged?.Invoke(true);
    }

    private void CreatePreview()
    {
        BuildingDefinition def =
            BuildingCatalog.Get(_selectedId);

        _preview =
            CreateBuildingNode(def);

        _preview.InputEnabled = false;
        _preview.ZIndex = 1000;

        AddChild(_preview);
    }

    private void ConfirmPlacementAt(
        Vector2 worldPosition)
    {
        if (string.IsNullOrEmpty(_selectedId))
            return;

        BuildingDefinition def =
            BuildingCatalog.Get(_selectedId);

        Vector2I origin =
            _grid.WorldToGrid(worldPosition);

        if (!_grid.CanPlace(
                origin,
                def.GridSize,
                _movingBuilding))
        {
            MessageRequested?.Invoke(
                "Cannot build here."
            );
            return;
        }

        if (_movingBuilding != null)
        {
            Building building =
                _movingBuilding;

            building.Visible = true;
            building.GridOrigin = origin;

            building.GlobalPosition =
                _grid.GridToWorldCentered(
                    origin,
                    building.GridSize
                );

            _grid.OccupyArea(
                origin,
                building.GridSize,
                building
            );

            _movingBuilding = null;

            _preview?.QueueFree();
            _preview = null;
            _selectedId = "";

            _hasTouchWorldPosition = false;
            _touchPlacementActive = false;

            _selection.ClearSelection();
            PlacementModeChanged?.Invoke(false);

            MessageRequested?.Invoke(
                "Building moved."
            );

            return;
        }

        if (!_resources.TryPayForBuilding(
                _selectedId))
        {
            MessageRequested?.Invoke(
                "Not enough resources."
            );
            return;
        }

        Building? placed =
            SpawnBuilding(
                _selectedId,
                origin,
                true
            );

        if (placed == null)
        {
            _resources.RefundBuilding(
                _selectedId,
                1.0
            );
            return;
        }

        MessageRequested?.Invoke(
            $"{def.DisplayName} constructed."
        );

        if (!_resources.CanAfford(_selectedId))
            CancelPlacement();
    }

    private Building? SpawnBuilding(
        string buildingId,
        Vector2I origin,
        bool registerResources)
    {
        BuildingDefinition def =
            BuildingCatalog.Get(buildingId);

        if (!_grid.CanPlace(
                origin,
                def.GridSize))
            return null;

        Building building =
            CreateBuildingNode(def);

        building.InputEnabled = true;
        building.GridOrigin = origin;

        building.GlobalPosition =
            _grid.GridToWorldCentered(
                origin,
                def.GridSize
            );

        _buildings.AddChild(building);

        _grid.OccupyArea(
            origin,
            def.GridSize,
            building
        );

        if (registerResources)
            _resources.RegisterBuilding(
                buildingId
            );

        return building;
    }

    private Building CreateBuildingNode(
        BuildingDefinition def)
    {
        return new Building
        {
            BuildingId = def.Id,
            DisplayName = def.DisplayName,
            GridSize = def.GridSize,
            CellSize = _grid.CellSize
        };
    }

    private Vector2 ScreenToWorld(
        Vector2 screenPosition)
    {
        Transform2D inverse =
            GetViewport()
                .GetCanvasTransform()
                .AffineInverse();

        return inverse * screenPosition;
    }
}
