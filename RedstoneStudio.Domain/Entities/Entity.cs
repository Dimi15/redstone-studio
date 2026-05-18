using System.Collections;
using System.Linq.Expressions;
using redstone_stone.Domain.ValueObjects;

namespace redstone_stone.Domain.Entities;

public abstract class Entity
{
    private Position position { get; set; }

    public Entity()
    {
        position = new Position();
    }

    /// <summary>
    /// Tells if the block is receiving power
    /// </summary>
    /// <returns>If the block is receiving power</returns>
    public virtual bool IsPowered()
    {
        return false;
    }

    /// <summary>
    /// Tells if the block is emitting power
    /// </summary>
    /// <returns>If the block is emitting power</returns>
    public virtual bool IsPowering()
    {
        return false;
    }
    
    public virtual void ChangeState()
    {
        return; // Does nothing
    }
}