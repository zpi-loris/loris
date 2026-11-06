using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Data
{
    public class StateCache
    {
        private readonly Dictionary<DevEui, Report> state = [];
    }
}
