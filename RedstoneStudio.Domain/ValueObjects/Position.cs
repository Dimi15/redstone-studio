using redstone_stone.Domain.Enums;

namespace redstone_stone.Domain.ValueObjects;

// Like X, Y
public class Position
{
    public int X { get; set; }
    public int Y { get; set; }

    public Position() { }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Returns the neighboring position one step in the given direction.
    /// </summary>
    public Position Move(Direction direction) => direction switch
    {
        Direction.North => new Position(X, Y - 1),
        Direction.South => new Position(X, Y + 1),
        Direction.West  => new Position(X - 1, Y),
        Direction.East  => new Position(X + 1, Y),
        _               => new Position(X, Y)
    };

    public override bool Equals(object? obj) => obj is Position p && p.X == X && p.Y == Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
}
