using redstone_stone.Domain.Entities;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Application.Grid;

// To manage the grid (Probably a matrix...?)
public class CircuitBoard
{
    private Entity[,] _circuitBoard { get; }

    public CircuitBoard(int width, int heigh)
    {
        _circuitBoard = new Entity[heigh > 0 ? heigh : 1, width > 0 ? width : 1];   // Min 1 x 1

        // By default the circuit board is filled with air blocks
        for (int i = 0; i < heigh; i++)
        {
            for (int j = 0; j < width; j++)
            {
                _circuitBoard[i, j] = new Air();
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

    public void RemoveEntity(Position position)
    {
        _circuitBoard[position.Y, position.X] = new Air();
    }
}