using redstone_stone.Domain.Enums;
using redstone_stone.Domain.ValueObjects;
using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

public class Repeater : Entity, IPowerable, IConnectable
{
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(0);

    private Direction _output { get; set; }
    private int _delay { get; set; }
    private bool _on;

    /// <summary>Returns the direction this repeater outputs power toward.</summary>
    public Direction Output => _output;

    /// <summary>Returns the current tick delay (1–4).</summary>
    public int Delay => _delay;

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

    /// <summary>Cycles the repeater delay between 1 and 4 ticks.</summary>
    public override void ChangeState()
    {
        _delay = _delay < 4 ? _delay + 1 : 1;
    }

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        _on = incomingPower.Value > 0;
        CurrentPower = new SignalStrength(_on ? 15 : 0);
    }

    /// <summary>Repeater only accepts input from the side opposite its output direction.</summary>
    bool IConnectable.CanConnectTo(Direction direction) =>
        (_output == Direction.North && direction == Direction.South) ||
        (_output == Direction.East  && direction == Direction.West)  ||
        (_output == Direction.South && direction == Direction.North) ||
        (_output == Direction.West  && direction == Direction.East);
}
