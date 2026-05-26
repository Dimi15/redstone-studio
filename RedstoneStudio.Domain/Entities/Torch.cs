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

    /// <summary>True if the torch is currently lit and emitting power.</summary>
    public bool IsOn => _on;

    /// <summary>The direction of the block face this torch is mounted on.</summary>
    public Direction Placement => _placement;

    /// <summary>True for a floor/ceiling torch; false for a wall-mounted torch.</summary>
    public bool IsVertical => _vertical;

    public Torch(bool vertical, Direction placement) : base()
    {
        _vertical = vertical;
        _placement = placement;
    }

    public override bool IsPowering()
    {
        return _on;
    }

    /// <summary>Wall torches do not output power back toward the block they are mounted on.</summary>
    bool IConnectable.CanConnectTo(Direction direction)
    {
        if (!_vertical && direction == _placement) return false;
        return true;
    }

    void IPowerable.UpdatePower(SignalStrength incomingPower)
    {
        // Torch inverts: it turns off when its input is powered.
        _on = incomingPower.Value == 0;
        CurrentPower = new SignalStrength(_on ? 15 : 0);
    }
}
