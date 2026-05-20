namespace redstone_stone.Domain.ValueObjects;

public class SignalStrength
{
    public int Value { get; set; }
    public static SignalStrength Min { get; } = new SignalStrength(0);
    public static SignalStrength Max { get; } = new SignalStrength(15);

    public SignalStrength(int value)
    {
        // Enforce the 0-15 boundary rule with clamping
        Value = Math.Clamp(value, 0, 15);
    }
}