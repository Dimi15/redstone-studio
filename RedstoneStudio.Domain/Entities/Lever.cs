namespace redstone_stone.Domain.Entities;

public class Lever : Entity
{
    private bool _on { get; set; }

    public Lever(bool on = false)
    {
        _on = on;
    }

    public override bool IsPowering()
    {
        return _on;
    }
    
    public override void ChangeState()
    {
        _on = !_on;
    }

    /// <summary>Returns whether the lever is currently switched on.</summary>
    public bool IsOn => _on;
}