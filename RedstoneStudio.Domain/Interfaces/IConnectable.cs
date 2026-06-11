using redstone_stone.Domain.Enums;

namespace redstone_stone.Domain.Interfaces;

public interface IConnectable
{
    /// <summary>
    /// Verifies if a block accepts a connection towards a specific direction
    /// </summary>
    /// <param name="direction">Direction of the block</param>
    /// <returns>If a block accepts a connection towards a specific direction</returns>
    bool CanConnectTo(Direction direction);
}