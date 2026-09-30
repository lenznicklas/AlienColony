using Godot;

namespace AlienColony.Systems;

public partial class ResourceManager : Node
{
    [Signal] public delegate void ResourcesChangedEventHandler();

    public double Ore { get; private set; } = 40;
    public double Metal { get; private set; } = 20;
    public double Energy { get; private set; } = 0;

    public int OreCapacity { get; private set; } = 200;
    public int MetalCapacity { get; private set; } = 100;

    public int MiningDrills { get; private set; }
    public int Smelters { get; private set; }
    public int PowerGenerators { get; private set; }
    public int Storages { get; private set; }

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

        EmitSignal(SignalName.ResourcesChanged);
    }

    public void UnregisterBuilding(string buildingId)
    {
        switch (buildingId)
        {
            case "mining_drill":
                MiningDrills = Mathf.Max(0, MiningDrills - 1);
                break;
            case "smelter":
                Smelters = Mathf.Max(0, Smelters - 1);
                break;
            case "power_generator":
                PowerGenerators = Mathf.Max(0, PowerGenerators - 1);
                break;
            case "storage":
                Storages = Mathf.Max(0, Storages - 1);
                RecalculateCapacity();
                break;
        }

        EmitSignal(SignalName.ResourcesChanged);
    }

    private void RecalculateCapacity()
    {
        OreCapacity = 200 + Storages * 200;
        MetalCapacity = 100 + Storages * 150;
        Ore = Mathf.Min(Ore, OreCapacity);
        Metal = Mathf.Min(Metal, MetalCapacity);
    }

    private void SimulateOneSecond()
    {
        // Early prototype rates:
        // Drill: +1 ore / sec
        // Generator: +5 energy / sec
        // Smelter: consumes 2 ore and 2 energy / sec -> +1 metal / sec.
        Ore = Mathf.Min(OreCapacity, Ore + MiningDrills);
        Energy += PowerGenerators * 5.0;

        int activeSmelters = Smelters;
        for (int i = 0; i < activeSmelters; i++)
        {
            if (Ore < 2 || Energy < 2 || Metal >= MetalCapacity)
                break;

            Ore -= 2;
            Energy -= 2;
            Metal = Mathf.Min(MetalCapacity, Metal + 1);
        }

        EmitSignal(SignalName.ResourcesChanged);
    }
}
