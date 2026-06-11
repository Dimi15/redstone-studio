using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Interfaces;

public interface IPowerable
{
    // Current level of energy (0-15)
    SignalStrength CurrentPower { get; }
    
    /// <summary>
    /// The engine calls this method to update the energy of the block, based on the power calculated from close blocks
    /// </summary>
    /// <param name="incomingPower"></param>
    void UpdatePower(SignalStrength incomingPower);
}