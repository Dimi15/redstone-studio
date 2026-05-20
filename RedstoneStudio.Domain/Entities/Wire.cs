using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

public class Wire : Entity, IConnectable, IPowerable
{
    // A wire starts with no power
    public SignalStrength CurrentPower { get; private set; } = new  SignalStrength(0);
    
    /// <summary>
    /// A basic cable can connet in all 4 directions so it always returns true
    /// </summary>
    /// <param name="direction"></param>
    /// <returns>True</returns>
    public bool CanConnectTo(Direction direction)
    {
        return true;
    }

    public void UpdatePower(SignalStrength incomingPower)
    {
        // Redstone power decays by 1 for every wire block it passes through.
        // If the engine passes the max power found, the wire takes that power and substracts 1
        int decayedPower;
        if (incomingPower.Value > 0)
        {
            decayedPower = incomingPower.Value - 1;
        }
        else
        {
            decayedPower = 0;
        }
    }
}