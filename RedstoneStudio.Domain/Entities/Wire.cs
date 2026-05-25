using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

public class Wire : Entity, IConnectable, IPowerable
{
    // A wire starts with no power
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(0);

    /// <summary>
    /// A basic wire can connect in all 4 directions so it always returns true.
    /// </summary>
    public bool CanConnectTo(Direction direction)
    {
        return true;
    }

    public void UpdatePower(SignalStrength incomingPower)
    {
        // Redstone power decays by 1 for every wire block it passes through.
        int decayedPower = incomingPower.Value > 0 ? incomingPower.Value - 1 : 0;
        CurrentPower = new SignalStrength(decayedPower);
    }
}
