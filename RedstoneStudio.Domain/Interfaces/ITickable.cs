namespace redstone_stone.Domain.Interfaces;

/// <summary>
/// Implemented by entities that need to advance their own internal state each simulation tick
/// (e.g. a button counting down its pulse duration).
/// </summary>
public interface ITickable
{
    void Tick();
}
