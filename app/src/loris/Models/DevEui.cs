using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Loris.Models;

[JsonConverter(typeof(JsonConverter))]
public readonly record struct DevEui(ulong Value) : IParsable<DevEui>
{
    public static DevEui Parse(string hex) => Parse(hex, null);

    public static DevEui Parse(byte[] bytes)
    {
        return new DevEui(BinaryPrimitives.ReadUInt64BigEndian(bytes));
    }

    public override string ToString() => Value.ToString("X16");

    public byte[] ToBytes()
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, Value);
        return bytes;
    }

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

    public class ValueConverter : ValueConverter<DevEui, byte[]>
    {
        public ValueConverter()
            : base(v => v.ToBytes(), v => Parse(v)) { }
    }

    public class JsonConverter : JsonConverter<DevEui>
    {
        public override DevEui Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) => Parse(reader.GetString() ?? "");

        public override void Write(
            Utf8JsonWriter writer,
            DevEui value,
            JsonSerializerOptions options
        ) => writer.WriteStringValue(value.ToString());
    }
}
