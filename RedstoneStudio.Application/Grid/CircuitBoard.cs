using System.Collections.Generic;
using redstone_stone.Domain.Entities;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Grid;

// Manages the 2D grid of entities that make up a redstone circuit.
// Internally the array is indexed [row, column] i.e. [Y, X].
public class CircuitBoard
{
    private readonly Entity[,] _circuitBoard;

    public int Width
    {
        get { return _circuitBoard.GetLength(1); }
    }

    public int Height
    {
        get { return _circuitBoard.GetLength(0); }
    }

    public CircuitBoard(int width, int height)
    {
        int safeWidth = width > 0 ? width : 1;
        int safeHeight = height > 0 ? height : 1;
        _circuitBoard = new Entity[safeHeight, safeWidth];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _circuitBoard[y, x] = new Air();
            }
        }
    }

    public Entity GetEntity(Position position)
    {
        return _circuitBoard[position.Y, position.X];
    }

    public void PlaceEntity(Entity entity, Position position)
    {
        _circuitBoard[position.Y, position.X] = entity;
    }

    // Replaces the cell with Air, effectively erasing the component.
    public void RemoveEntity(Position position)
    {
        _circuitBoard[position.Y, position.X] = new Air();
    }

    // Returns true if the position is within the grid boundaries.
    public bool IsInBounds(Position position)
    {
        bool xOk = position.X >= 0 && position.X < Width;
        bool yOk = position.Y >= 0 && position.Y < Height;
        return xOk && yOk;
    }

    // Returns a list of every position on the board in row-first order (top-left to bottom-right).
    public List<Position> AllPositions()
    {
        var positions = new List<Position>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                positions.Add(new Position(x, y));
            }
        }

        return positions;
    }
}