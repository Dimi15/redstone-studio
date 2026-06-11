using redstone_stone.Domain.Enums;

namespace redstone_stone.Domain.ValueObjects;

public class Position
{
    public int X { get; set; }
    public int Y { get; set; }

    public Position()
    {
    }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Position Move(Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return new Position(X, Y - 1);
            case Direction.South: return new Position(X, Y + 1);
            case Direction.West: return new Position(X - 1, Y);
            case Direction.East: return new Position(X + 1, Y);
            default: return new Position(X, Y);
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is Position p)
        {
            return p.X == X && p.Y == Y;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}