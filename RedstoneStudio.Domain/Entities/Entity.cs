namespace redstone_stone.Domain.Entities;

public abstract class Entity
{
    public virtual bool IsPowered()
    {
        return false;
    }

    public virtual bool IsPowering()
    {
        return false;
    }

    public virtual void ChangeState()
    {
    }
}
