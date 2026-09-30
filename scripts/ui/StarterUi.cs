using Godot;
using AlienColony.Systems;

namespace AlienColony.UI;

public partial class StarterUi : CanvasLayer
{
    private PlacementManager _placement = null!;

    public override void _Ready()
    {
        _placement = GetNode<PlacementManager>("../PlacementManager");

        Button miningButton = GetNode<Button>("BottomBar/Panel/Margin/Row/MiningDrill");
        Button storageButton = GetNode<Button>("BottomBar/Panel/Margin/Row/Storage");
        Button cancelButton = GetNode<Button>("BottomBar/Panel/Margin/Row/Cancel");

        miningButton.Pressed += _placement.BeginMiningDrill;
        storageButton.Pressed += _placement.BeginStorage;
        cancelButton.Pressed += _placement.CancelPlacement;
    }
}
