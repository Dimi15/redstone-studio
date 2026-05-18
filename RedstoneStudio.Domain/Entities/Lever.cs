namespace redstone_stone.Domain.Entities;

public class Lever : Entity
{
    private bool _on { get; set; }

    public Lever()
    {
        _on = false;
    }

    public override IsPowering()
    {
        return _on;
    }
    
    public override ChangeState()
    {
        _on = !_on;
    }
}