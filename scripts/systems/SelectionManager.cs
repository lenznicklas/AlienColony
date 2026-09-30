using Godot;
using System;
using AlienColony.Buildings;

namespace AlienColony.Systems;

public partial class SelectionManager : Node
{
    public event Action<Building?>? SelectionChanged;

    public Building? SelectedBuilding { get; private set; }

    public void Select(Building building)
    {
        if (SelectedBuilding == building)
            return;

        SelectedBuilding?.SetSelected(false);
        SelectedBuilding = building;
        SelectedBuilding.SetSelected(true);

        SelectionChanged?.Invoke(SelectedBuilding);
    }

    public void ClearSelection()
    {
        SelectedBuilding?.SetSelected(false);
        SelectedBuilding = null;
        SelectionChanged?.Invoke(null);
    }
}
