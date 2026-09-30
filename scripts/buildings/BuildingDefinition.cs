using Godot;

namespace AlienColony.Buildings;

public sealed class BuildingDefinition
{
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public Vector2I GridSize { get; init; } = Vector2I.One;

    public int OreCost { get; init; }
    public int MetalCost { get; init; }

    public int PowerProduction { get; init; }
    public int PowerConsumption { get; init; }

    public string TexturePath { get; init; } = "";
}
