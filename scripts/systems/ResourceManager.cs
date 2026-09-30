using Godot;
using System;
using AlienColony.Buildings;

namespace AlienColony.Systems;

public partial class ResourceManager : Node
{
    public event Action? ResourcesChanged;

    public double Ore { get; private set; } = 80;
    public double Metal { get; private set; } = 120;

    public int OreCapacity { get; private set; } = 250;
    public int MetalCapacity { get; private set; } = 200;

    public int MiningDrills { get; private set; }
    public int Smelters { get; private set; }
    public int PowerGenerators { get; private set; }
    public int Storages { get; private set; }

    public int PowerProduction { get; private set; } = 12;
    public int PowerDemand { get; private set; }
    public int PoweredMiningDrills { get; private set; }
    public int PoweredSmelters { get; private set; }

    private double _tickAccumulator;

    public override void _Process(double delta)
    {
        _tickAccumulator += delta;

        while (_tickAccumulator >= 1.0)
        {
            _tickAccumulator -= 1.0;
            SimulateOneSecond();
        }
    }

    public bool CanAfford(string buildingId)
    {
        BuildingDefinition def = BuildingCatalog.Get(buildingId);
        return Ore >= def.OreCost && Metal >= def.MetalCost;
    }

    public bool TryPayForBuilding(string buildingId)
    {
        if (!CanAfford(buildingId))
            return false;

        BuildingDefinition def = BuildingCatalog.Get(buildingId);

        Ore -= def.OreCost;
        Metal -= def.MetalCost;

        ResourcesChanged?.Invoke();
        return true;
    }

    public void RefundBuilding(string buildingId, double fraction = 0.5)
    {
        BuildingDefinition def = BuildingCatalog.Get(buildingId);

        Ore = Math.Min(OreCapacity, Ore + def.OreCost * fraction);
        Metal = Math.Min(MetalCapacity, Metal + def.MetalCost * fraction);

        ResourcesChanged?.Invoke();
    }

    public void RegisterBuilding(string buildingId)
    {
        switch (buildingId)
        {
            case "mining_drill":
                MiningDrills++;
                break;

            case "smelter":
                Smelters++;
                break;

            case "power_generator":
                PowerGenerators++;
                break;

            case "storage":
                Storages++;
                RecalculateCapacity();
                break;
        }

        RecalculatePower();
        ResourcesChanged?.Invoke();
    }

    public void UnregisterBuilding(string buildingId)
    {
        switch (buildingId)
        {
            case "mining_drill":
                MiningDrills = Math.Max(0, MiningDrills - 1);
                break;

            case "smelter":
                Smelters = Math.Max(0, Smelters - 1);
                break;

            case "power_generator":
                PowerGenerators = Math.Max(0, PowerGenerators - 1);
                break;

            case "storage":
                Storages = Math.Max(0, Storages - 1);
                RecalculateCapacity();
                break;
        }

        RecalculatePower();
        ResourcesChanged?.Invoke();
    }

    public string GetPowerStatus(string buildingId)
    {
        return buildingId switch
        {
            "main_core" => "Power source",
            "power_generator" => "+20 MW",
            "storage" => "No power required",
            "mining_drill" => $"{PoweredMiningDrills}/{MiningDrills} drills powered",
            "smelter" => $"{PoweredSmelters}/{Smelters} smelters powered",
            _ => ""
        };
    }

    private void RecalculateCapacity()
    {
        OreCapacity = 250 + Storages * 250;
        MetalCapacity = 200 + Storages * 200;

        Ore = Math.Min(Ore, OreCapacity);
        Metal = Math.Min(Metal, MetalCapacity);
    }

    private void RecalculatePower()
    {
        // Main Core provides the initial 12 MW.
        PowerProduction = 12 + PowerGenerators * 20;

        int drillDemand = MiningDrills * 4;
        int smelterDemand = Smelters * 8;

        PowerDemand = drillDemand + smelterDemand;

        int available = PowerProduction;

        // Simple automatic allocation:
        // 1. Mining first, so the base never completely starves.
        // 2. Smelting gets the remaining power.
        PoweredMiningDrills = Math.Min(MiningDrills, available / 4);
        available -= PoweredMiningDrills * 4;

        PoweredSmelters = Math.Min(Smelters, available / 8);
    }

    private void SimulateOneSecond()
    {
        RecalculatePower();

        // Each powered drill produces 2 ore/sec.
        Ore = Math.Min(OreCapacity, Ore + PoweredMiningDrills * 2.0);

        // Each powered smelter turns 3 ore -> 1 metal/sec.
        for (int i = 0; i < PoweredSmelters; i++)
        {
            if (Ore < 3 || Metal >= MetalCapacity)
                break;

            Ore -= 3;
            Metal = Math.Min(MetalCapacity, Metal + 1);
        }

        ResourcesChanged?.Invoke();
    }
}
