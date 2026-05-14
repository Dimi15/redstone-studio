using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

public abstract class Entity
{
    private Position position { get; set; }
    
    public Entity()
    {
        position = new Position();
    }

    public virtual bool IsPowered()
    {
        return false;
    }
    
    public virtual void ChangeState()
    {
        return; // Does nothing
    }
}