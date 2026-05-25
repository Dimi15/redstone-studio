using redstone_stone.Domain.Interfaces;

namespace redstone_stone.Domain.Entities;

/// <summary>
/// Emits a full-strength pulse for a fixed number of ticks when activated, then turns off
/// automatically. Click the placed button to trigger a new pulse.
/// </summary>
public class Button : Entity, ITickable
{
    private int _ticksLeft;
    private readonly int _pulseDuration;

    /// <summary>Creates a button with the given pulse length in ticks (default 3).</summary>
    public Button(int pulseDuration = 3) : base()
    {
        _pulseDuration = pulseDuration;
    }

    public override bool IsPowering() => _ticksLeft > 0;

    /// <summary>Triggers a new pulse (or re-arms while already pulsing).</summary>
    public override void ChangeState() => _ticksLeft = _pulseDuration;

    /// <summary>Called once per tick by the simulator to count down the pulse.</summary>
    public void Tick()
    {
        if (_ticksLeft > 0) _ticksLeft--;
    }
}
