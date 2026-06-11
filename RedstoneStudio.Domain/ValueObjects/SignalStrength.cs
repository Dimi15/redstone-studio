namespace redstone_stone.Domain.ValueObjects;

public class SignalStrength
{
    public int Value { get; set; }
    public static SignalStrength Min { get; } = new SignalStrength(0);
    public static SignalStrength Max { get; } = new SignalStrength(15);

    public SignalStrength(int value)
    {
        if (value < 0)
        {
            Value = 0;
        }
        else if (value > 15)
        {
            Value = 15;
        }
        else
        {
            Value = value;
        }
    }
}
