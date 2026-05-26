using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

// Emits a full-strength pulse for a fixed number of ticks, then turns off automatically.
// Click the placed button again to trigger a new pulse.
public class Button : Entity, ITickable
{
    private int _ticksLeft;
    private readonly int _pulseDuration;

    public Button(int pulseDuration = 3) : base()
    {
        _pulseDuration = pulseDuration;
    }

    public override bool IsPowering()
    {
        return _ticksLeft > 0;
    }

    // Re-arms the pulse countdown.
    public override void ChangeState()
    {
        _ticksLeft = _pulseDuration;
    }

    // Called once per tick by the simulator to count down the pulse.
    public void Tick()
    {
        if (_ticksLeft > 0)
        {
            _ticksLeft--;
        }
    }
}
