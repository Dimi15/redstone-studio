using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;
using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

public class Repeater : Entity, IPowerable, IConnectable
{
    public SignalStrength CurrentPower { get; private set; }

    private Direction _output;
    private bool _on;

    public Repeater() : base()
    {
        CurrentPower = new SignalStrength(0);
    }
    
    public Direction Output => _output;

    public int Delay { get; private set; }

    public Repeater(Direction output, int delay = 1) : base()
    {
        CurrentPower = new SignalStrength(0);
        
        _output = output;
        _on = false;

        if (delay < 1)
        {
            Delay = 1;
        }
        else if (delay > 4)
        {
            Delay = 4;
        }
        else
        {
            Delay = delay;
        }
    }

    public override bool IsPowering()
    {
        return _on;
    }

    public override void ChangeState()
    {
        if (Delay < 4)
        {
            Delay += 1;
        }
        else
        {
            Delay = 1;
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