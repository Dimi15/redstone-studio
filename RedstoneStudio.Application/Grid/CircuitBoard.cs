using redstone_stone.Domain.Entities;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Grid;

// Manages the 2D grid of entities that make up a redstone circuit.
public class CircuitBoard
{
    private Entity[,] _circuitBoard { get; }

    public int Width  => _circuitBoard.GetLength(1);
    public int Height => _circuitBoard.GetLength(0);

    public CircuitBoard(int width, int height)
    {
        _circuitBoard = new Entity[height > 0 ? height : 1, width > 0 ? width : 1];   // Min 1 x 1

        // By default the circuit board is filled with air blocks
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _circuitBoard[y, x] = new Air();
    }

    public Entity GetEntity(Position position)
    {
        return _circuitBoard[position.Y, position.X];
    }

    public void PlaceEntity(Entity entity, Position position)
    {
        _circuitBoard[position.Y, position.X] = entity;
    }

    public void RemoveEntity(Position position)
    {
        _circuitBoard[position.Y, position.X] = new Air();
    }

    /// <summary>Returns true if the position falls within the grid boundaries.</summary>
    public bool IsInBounds(Position position) =>
        position.X >= 0 && position.X < Width &&
        position.Y >= 0 && position.Y < Height;

    /// <summary>Iterates every position on the board in row-major order.</summary>
    public IEnumerable<Position> AllPositions()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return new Position(x, y);
    }
}
