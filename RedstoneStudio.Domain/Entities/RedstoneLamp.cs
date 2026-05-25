using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

/// <summary>
/// Lights up when it receives any redstone power. Does not emit or transmit signal —
/// it is a pure output indicator, like a light bulb.
/// </summary>
public class RedstoneLamp : Entity, IPowerable
{
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(0);

    public override bool IsPowered() => CurrentPower.Value > 0;

    public void UpdatePower(SignalStrength incomingPower)
    {
        CurrentPower = incomingPower;
    }
}
