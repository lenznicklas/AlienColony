using Godot;
using System.Collections.Generic;

namespace AlienColony.Systems;

public partial class GridManager : Node
{
    [Export] public int CellSize { get; set; } = 64;
    [Export] public Vector2I MapSize { get; set; } = new(30, 30);

    private readonly Dictionary<Vector2I, Node2D> _occupiedCells = new();

    public Vector2I WorldToGrid(Vector2 worldPosition)
    {
        return new Vector2I(
            Mathf.FloorToInt(worldPosition.X / CellSize),
            Mathf.FloorToInt(worldPosition.Y / CellSize)
        );
    }

    public Vector2 GridToWorld(Vector2I gridPosition)
    {
        return new Vector2(
            gridPosition.X * CellSize,
            gridPosition.Y * CellSize
        );
    }

    public Vector2 GridToWorldCentered(Vector2I origin, Vector2I size)
    {
        return GridToWorld(origin) + new Vector2(
            size.X * CellSize * 0.5f,
            size.Y * CellSize * 0.5f
        );
    }

    public bool IsInsideMap(Vector2I cell)
    {
        return cell.X >= 0 &&
               cell.Y >= 0 &&
               cell.X < MapSize.X &&
               cell.Y < MapSize.Y;
    }

    public bool CanPlace(Vector2I origin, Vector2I size)
    {
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                Vector2I cell = origin + new Vector2I(x, y);

                if (!IsInsideMap(cell) || _occupiedCells.ContainsKey(cell))
                    return false;
            }
        }

        return true;
    }

    public void OccupyArea(Vector2I origin, Vector2I size, Node2D building)
    {
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                _occupiedCells[origin + new Vector2I(x, y)] = building;
            }
        }
    }

    public void FreeArea(Vector2I origin, Vector2I size)
    {
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                _occupiedCells.Remove(origin + new Vector2I(x, y));
            }
        }
    }
}
