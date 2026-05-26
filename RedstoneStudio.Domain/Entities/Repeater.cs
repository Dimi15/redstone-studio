using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;
using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

public class Repeater : Entity, IPowerable, IConnectable
{
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(0);

    private Direction _output;
    private int _delay;
    private bool _on;

    public Direction Output
    {
        get { return _output; }
    }

    public int Delay
    {
        get { return _delay; }
    }

    public Repeater(Direction output, int delay = 1) : base()
    {
        _output = output;
        _on = false;

        if (delay < 1)
        {
            _delay = 1;
        }
        else if (delay > 4)
        {
            _delay = 4;
        }
        else
        {
            _delay = delay;
        }
    }

    public override bool IsPowering()
    {
        return _on;
    }

    public override void ChangeState()
    {
        if (_delay < 4)
        {
            _delay += 1;
        }
        else
        {
            _delay = 1;
        }
    }

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        if (incomingPower.Value > 0)
        {
            _on = true;
        }
        else
        {
            _on = false;
        }

        if (_on)
        {
            CurrentPower = new SignalStrength(15);
        }
        else
        {
            CurrentPower = new SignalStrength(0);
        }
    }

    bool IConnectable.CanConnectTo(Direction direction)
    {
        switch (_output)
        {
            case Direction.North:
                return direction == Direction.South;
            case Direction.East:
                return direction == Direction.West;
            case Direction.South:
                return direction == Direction.North;
            case Direction.West:
                return direction == Direction.East;
            default:
                return false;
        }
    }
}