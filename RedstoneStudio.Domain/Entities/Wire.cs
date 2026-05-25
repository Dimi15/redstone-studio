using redstone_stone.Domain.Enums;
using redstone_stone.Domain.Interfaces;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

/// <summary>
/// Carries redstone signal in all four directions.
/// Power decays by 1 per block (range 0–15). The BFS engine handles the decay step;
/// UpdatePower just stores whatever the engine calculated.
/// </summary>
public class Wire : Entity, IConnectable, IPowerable
{
    public SignalStrength CurrentPower { get; private set; } = new SignalStrength(0);

    public bool CanConnectTo(Direction direction) => true;

    public void UpdatePower(SignalStrength incomingPower)
    {
        CurrentPower = incomingPower;
    }
}
