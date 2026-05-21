using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;
using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

public class Repeater : Entity, IPowerable, IConnectable
{
    SignalStrength IPowerable.CurrentPower => new SignalStrength(0);
    private Direction _output { get; set; }
    private int _delay { get; set; }
    private bool _on;

    public Repeater(Direction output) : base()
    {
        _output = output;
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

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        _on = incomingPower.Value > 0;
    }

    bool IConnectable.CanConnectTo(Direction direction)
    {
        if(_output == Direction.North && direction == Direction.South) return true;
        else if(_output == Direction.East && direction == Direction.West) return true;
        else if(_output == Direction.South && direction == Direction.North) return true;
        else if(_output == Direction.West && direction == Direction.East) return true;

        return false;
    }
}