using Godot;
using System;
using AlienColony.Systems;
using AlienColony.Buildings;

namespace AlienColony.UI;

public partial class StarterUi : CanvasLayer
{
    private PlacementManager _placement = null!;
    private ResourceManager _resources = null!;
    private SelectionManager _selection = null!;

    private Label _oreLabel = null!;
    private Label _metalLabel = null!;
    private Label _powerLabel = null!;
    private Label _messageLabel = null!;

    private PanelContainer _selectionPanel = null!;
    private Label _selectionName = null!;
    private Label _selectionDetails = null!;
    private Button _moveButton = null!;
    private Button _deleteButton = null!;

    private double _messageTimer;

    public override void _Ready()
    {
        _placement = GetNode<PlacementManager>("../PlacementManager");
        _resources = GetNode<ResourceManager>("../ResourceManager");
        _selection = GetNode<SelectionManager>("../SelectionManager");

        GetNode<Button>("BuildBar/Panel/Margin/Row/MiningDrill").Pressed += _placement.BeginMiningDrill;
        GetNode<Button>("BuildBar/Panel/Margin/Row/Storage").Pressed += _placement.BeginStorage;
        GetNode<Button>("BuildBar/Panel/Margin/Row/Smelter").Pressed += _placement.BeginSmelter;
        GetNode<Button>("BuildBar/Panel/Margin/Row/PowerGenerator").Pressed += _placement.BeginPowerGenerator;
        GetNode<Button>("BuildBar/Panel/Margin/Row/Cancel").Pressed += _placement.CancelPlacement;

        _oreLabel = GetNode<Label>("TopBar/Panel/Margin/Resources/Ore");
        _metalLabel = GetNode<Label>("TopBar/Panel/Margin/Resources/Metal");
        _powerLabel = GetNode<Label>("TopBar/Panel/Margin/Resources/Power");
        _messageLabel = GetNode<Label>("Message");

        _selectionPanel = GetNode<PanelContainer>("SelectionPanel");
        _selectionName = GetNode<Label>("SelectionPanel/Margin/Column/Name");
        _selectionDetails = GetNode<Label>("SelectionPanel/Margin/Column/Details");
        _moveButton = GetNode<Button>("SelectionPanel/Margin/Column/Actions/Move");
        _deleteButton = GetNode<Button>("SelectionPanel/Margin/Column/Actions/Delete");

        _moveButton.Pressed += _placement.BeginMoveSelected;
        _deleteButton.Pressed += _placement.DeleteSelected;

        _resources.ResourcesChanged += RefreshResources;
        _selection.SelectionChanged += OnSelectionChanged;
        _placement.MessageRequested += ShowMessage;

        RefreshResources();
        OnSelectionChanged(null);
    }

    public override void _Process(double delta)
    {
        if (_messageTimer <= 0)
            return;

        _messageTimer -= delta;

        if (_messageTimer <= 0)
            _messageLabel.Visible = false;
    }

    private void RefreshResources()
    {
        _oreLabel.Text = $"ORE  {_resources.Ore:0}/{_resources.OreCapacity}";
        _metalLabel.Text = $"METAL  {_resources.Metal:0}/{_resources.MetalCapacity}";

        string powerState = _resources.PowerDemand > _resources.PowerProduction
            ? "LOW POWER"
            : "POWER";

        _powerLabel.Text =
            $"{powerState}  {_resources.PowerDemand}/{_resources.PowerProduction} MW";

        if (_selection.SelectedBuilding != null)
            RefreshSelectionDetails(_selection.SelectedBuilding);
    }

    private void OnSelectionChanged(Building? building)
    {
        _selectionPanel.Visible = building != null;

        if (building == null)
            return;

        _selectionName.Text = building.DisplayName;
        RefreshSelectionDetails(building);

        bool core = building.BuildingId == "main_core";

        _moveButton.Disabled = core;
        _deleteButton.Disabled = core;
    }

    private void RefreshSelectionDetails(Building building)
    {
        BuildingDefinition def = BuildingCatalog.Get(building.BuildingId);

        string power = def.PowerProduction > 0
            ? $"Produces {def.PowerProduction} MW"
            : def.PowerConsumption > 0
                ? $"Uses {def.PowerConsumption} MW"
                : "No power required";

        string production = building.BuildingId switch
        {
            "mining_drill" => "Produces 2 Ore/s while powered",
            "smelter" => "3 Ore -> 1 Metal/s while powered",
            "storage" => "+250 Ore / +200 Metal capacity",
            "main_core" => "Colony command center",
            "power_generator" => "Expands colony power grid",
            _ => ""
        };

        _selectionDetails.Text =
            $"{power}\n{production}\n{_resources.GetPowerStatus(building.BuildingId)}";
    }

    private void ShowMessage(string message)
    {
        _messageLabel.Text = message;
        _messageLabel.Visible = true;
        _messageTimer = 2.5;
    }
}
