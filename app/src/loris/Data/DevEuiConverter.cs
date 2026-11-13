using Loris.Types;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Loris.Data;

public class DevEuiConverter : ValueConverter<DevEui, byte[]>
{
    public DevEuiConverter()
        : base(v => v.ToBytes(), v => DevEui.Parse(v)) { }
}
