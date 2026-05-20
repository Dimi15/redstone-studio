using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;
using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

public class Repeater : Entity, IPowerable, IConnectable
{
    private Direction _facing { get; set; }
    private int _delay { get; set; }
    private bool _on;

    public Repeater() : base()
    {
        _facing = Direction.North;
        _delay = 1;
        _on = false;
    }

    public override bool IsPowering()
    {
        return _on;
    }

    public override void ChangeState()
    {
        if(_delay < 4)
        {
            _delay++;
        }
        else
        {
            _delay = 1;
        }
    }

    void UpdatePower(SignalStrength incomingPower)
    {
        _on = incomingPower > 0;
    }

    bool CanConnectTo(Direction direction)
    {
        if(_facing == Direction.North && direction == Direction.South) return true;
        else if(_facing == Direction.East && direction == Direction.West) return true;
        else if(_facing == Direction.South && direction == Direction.North) return true;
        else if(_facing == Direction.West && direction == Direction.East) return true;

        return false;
    }
}