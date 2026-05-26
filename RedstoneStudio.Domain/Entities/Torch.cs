using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

public class Torch : Entity, IConnectable, IPowerable
{
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(15);

    private readonly Direction _placement;
    private readonly bool _vertical;
    private bool _on = true;

    public bool IsOn
    {
        get { return _on; }
    }

    public Direction Placement
    {
        get { return _placement; }
    }

    public bool IsVertical
    {
        get { return _vertical; }
    }

    public Torch(bool vertical, Direction placement) : base()
    {
        _vertical = vertical;
        _placement = placement;
    }

    public override bool IsPowering()
    {
        return _on;
    }

    bool IConnectable.CanConnectTo(Direction direction)
    {
        // Wall torches do not output power back toward the block they are mounted on.
        if (!_vertical)
        {
            if (direction == _placement)
            {
                return false;
            }
        }
        return true;
    }

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        // Torch inverts: it turns off when its input is powered.
        if (incomingPower.Value == 0)
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
}
