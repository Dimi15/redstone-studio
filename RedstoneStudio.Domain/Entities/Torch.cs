using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

public class Torch : Entity, IConnectable, IPowerable
{
    SignalStrength CurrentPower { get; } => new SignalStrength(15);

    private bool _vertical { get; set; }
    private Direction _placement { get; set; }
    private bool _on;

    public Torch(bool vertical, Direction placement) : base()
    {
        _vertical = vertical;
        _placement = placement;
        _on = true;
    }

    public override bool IsPowering()
    {
        return _on;
    }

    bool IConnectable.CanConnectTo(Direction direction)
    {
        if(_vertical) return true;
        else if(_placement == Direction.North && direction == Direction.South) return false;
        else if(_placement == Direction.East && direction == Direction.West) return false;
        else if(_placement == Direction.South && direction == Direction.North) return false;
        else if(_placement == Direction.West && direction == Direction.East) return false;

        return true;
    }

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        _on = incomingPower > 0 ? false : true;
    }
}