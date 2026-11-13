using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Loris.Types;

// TODO: write tests for devEUI
public readonly record struct DevEui(ulong Value) : IParsable<DevEui>
{
    public static DevEui Parse(string hex) => Parse(hex, null);

    public static DevEui Parse(byte[] bytes) => new(BitConverter.ToUInt64(bytes));

    public override string ToString() => Value.ToString("X16");

    public byte[] ToBytes() => BitConverter.GetBytes(Value);

    //
    // IParsable<DebEui> interface implementation
    // parsing from string - used in controller actions Model Binding
    //

    public static DevEui Parse(string s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var value))
        {
            throw new ArgumentException("Could not parse supplied value.", nameof(s));
        }

        return value;
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out DevEui result
    )
    {
        if (s is not null && ulong.TryParse(s, NumberStyles.HexNumber, provider, out var value))
        {
            result = new DevEui(value);
            return true;
        }
        result = default;
        return false;
    }
}
