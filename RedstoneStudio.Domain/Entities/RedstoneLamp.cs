using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

// Lights up when it receives any redstone power.
// Does not emit or transmit signal — it is a pure output indicator.
public class RedstoneLamp : Entity, IPowerable
{
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(0);

    public override bool IsPowered()
    {
        return CurrentPower.Value > 0;
    }

    public void UpdatePower(SignalStrength incomingPower)
    {
        CurrentPower = incomingPower;
    }
}
