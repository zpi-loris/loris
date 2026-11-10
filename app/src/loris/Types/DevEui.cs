namespace Loris.Types;

public readonly record struct DevEui(ulong Value)
{
    public static DevEui Parse(string hex) => new(Convert.ToUInt64(hex, 16));

    public static DevEui Parse(byte[] bytes) => new(BitConverter.ToUInt64(bytes));

    public override string ToString() => Value.ToString("X16");

    public byte[] ToBytes() => BitConverter.GetBytes(Value);
}
