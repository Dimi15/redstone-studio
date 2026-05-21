namespace redstone_stone.Domain.Entities;

public class RedstoneBlock : Entity
{

    public RedstoneBlock() : base(x, y)
    {}

    public override IsPowering()
    {

        return true;
    }
}