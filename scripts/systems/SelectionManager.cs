using Godot;
using AlienColony.Buildings;

namespace AlienColony.Systems;

public partial class SelectionManager : Node
{
    [Signal] public delegate void SelectionChangedEventHandler(Building? building);

    public Building? SelectedBuilding { get; private set; }

    public void Select(Building building)
    {
        if (SelectedBuilding == building)
            return;

        SelectedBuilding?.SetSelected(false);
        SelectedBuilding = building;
        SelectedBuilding.SetSelected(true);

        EmitSignal(SignalName.SelectionChanged, SelectedBuilding);
    }

    public void ClearSelection()
    {
        SelectedBuilding?.SetSelected(false);
        SelectedBuilding = null;
        EmitSignal(SignalName.SelectionChanged, Variant.From<Building?>(null));
    }
}
