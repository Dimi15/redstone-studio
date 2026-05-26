namespace redstone_stone.Domain.Entities;

public class Lever : Entity
{
    private bool _on;

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

    public bool IsOn => _on;
}