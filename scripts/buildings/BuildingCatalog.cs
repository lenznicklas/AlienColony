using Godot;
using System.Collections.Generic;

namespace AlienColony.Buildings;

public static class BuildingCatalog
{
    private static readonly Dictionary<string, BuildingDefinition> Definitions = new()
    {
        ["main_core"] = new BuildingDefinition
        {
            Id = "main_core",
            DisplayName = "Main Core",
            GridSize = new Vector2I(3, 3),
            PowerProduction = 12,
            TexturePath = "res://assets/buildings/main_core.png"
        },

        ["mining_drill"] = new BuildingDefinition
        {
            Id = "mining_drill",
            DisplayName = "Mining Drill",
            GridSize = new Vector2I(2, 2),
            MetalCost = 25,
            PowerConsumption = 4,
            TexturePath = "res://assets/buildings/mining_drill.png"
        },

        ["storage"] = new BuildingDefinition
        {
            Id = "storage",
            DisplayName = "Storage",
            GridSize = new Vector2I(2, 2),
            MetalCost = 30,
            TexturePath = "res://assets/buildings/storage.png"
        },

        ["smelter"] = new BuildingDefinition
        {
            Id = "smelter",
            DisplayName = "Smelter",
            GridSize = new Vector2I(2, 2),
            OreCost = 20,
            MetalCost = 40,
            PowerConsumption = 8,
            TexturePath = "res://assets/buildings/smelter.png"
        },

        ["power_generator"] = new BuildingDefinition
        {
            Id = "power_generator",
            DisplayName = "Power Generator",
            GridSize = new Vector2I(2, 2),
            OreCost = 15,
            MetalCost = 35,
            PowerProduction = 20,
            TexturePath = "res://assets/buildings/power_generator.png"
        },

        ["wall"] = new BuildingDefinition
        {
            Id = "wall",
            DisplayName = "Wall",
            GridSize = Vector2I.One,
            MetalCost = 5,
            TexturePath = "res://assets/buildings/wall.png"
        },

        ["turret"] = new BuildingDefinition
        {
            Id = "turret",
            DisplayName = "Turret",
            GridSize = new Vector2I(2, 2),
            MetalCost = 60,
            PowerConsumption = 5,
            TexturePath = "res://assets/buildings/turret.png"
        }
    };

    public static BuildingDefinition Get(string id)
    {
        return Definitions[id];
    }

    public static bool TryGet(string id, out BuildingDefinition definition)
    {
        return Definitions.TryGetValue(id, out definition!);
    }
}
