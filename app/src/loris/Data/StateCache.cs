using Loris.Protocol;
using Loris.Types;

namespace Loris.Data;

public sealed class StateCache
{
    private readonly Dictionary<DevEui, Report> state = [];
}
