namespace Loris.Types;

public readonly record struct DevEui(ulong Value)
{
    public static DevEui Parse(string hex) => new(Convert.ToUInt64(hex, 16));

    public override string ToString() => Value.ToString("X16");
}
