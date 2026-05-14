using redstone_stone.Domain.Enums;

namespace redstone_stone.Domain.Interfaces;

public interface IConnectable
{
    public bool CanConnectTo(Direction direction);
}