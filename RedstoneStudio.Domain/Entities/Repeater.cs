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
        _delay  = Math.Clamp(delay, 1, 4);
        _on     = false;
    }

    public override bool IsPowering()
    {
        return _on;
    }

    public override void ChangeState()
    {
        if (_delay < 4)
        {
            _delay = _delay + 1;
        }
        else
        {
            _delay = 1;
        }
    }

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        _on = incomingPower.Value > 0;
        CurrentPower = new SignalStrength(_on ? 15 : 0);
    }

    bool IConnectable.CanConnectTo(Direction direction)
    {
        if (_output == Direction.North) return direction == Direction.South;
        if (_output == Direction.East)  return direction == Direction.West;
        if (_output == Direction.South) return direction == Direction.North;
        if (_output == Direction.West)  return direction == Direction.East;
        return false;
    }
}
