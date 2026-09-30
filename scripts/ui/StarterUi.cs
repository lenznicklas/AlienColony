using Godot;
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
    private Label _energyLabel = null!;

    private PanelContainer _selectionPanel = null!;
    private Label _selectionName = null!;
    private Button _moveButton = null!;
    private Button _deleteButton = null!;

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
        _energyLabel = GetNode<Label>("TopBar/Panel/Margin/Resources/Energy");

        _selectionPanel = GetNode<PanelContainer>("SelectionPanel");
        _selectionName = GetNode<Label>("SelectionPanel/Margin/Column/Name");
        _moveButton = GetNode<Button>("SelectionPanel/Margin/Column/Actions/Move");
        _deleteButton = GetNode<Button>("SelectionPanel/Margin/Column/Actions/Delete");

        _moveButton.Pressed += _placement.BeginMoveSelected;
        _deleteButton.Pressed += _placement.DeleteSelected;

        _resources.ResourcesChanged += RefreshResources;
        _selection.SelectionChanged += OnSelectionChanged;

        RefreshResources();
        OnSelectionChanged(null);
    }

    private void RefreshResources()
    {
        _oreLabel.Text = $"ORE  {_resources.Ore:0}/{_resources.OreCapacity}";
        _metalLabel.Text = $"METAL  {_resources.Metal:0}/{_resources.MetalCapacity}";
        _energyLabel.Text = $"ENERGY  {_resources.Energy:0}";
    }

    private void OnSelectionChanged(Building? building)
    {
        _selectionPanel.Visible = building != null;

        if (building == null)
            return;

        _selectionName.Text = building.DisplayName;

        bool core = building.BuildingId == "main_core";
        _moveButton.Disabled = core;
        _deleteButton.Disabled = core;
    }
}
