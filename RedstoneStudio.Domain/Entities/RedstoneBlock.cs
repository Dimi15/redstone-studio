namespace redstone_stone.Domain.Entities;

public class RedstoneBlock : Entity
{
    public RedstoneBlock() : base()
    {
    }

    public override bool IsPowering()
    {
        return true;
    }
}