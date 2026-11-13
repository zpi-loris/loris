using System.Text.Json.Serialization;

namespace Loris.Automations;

public interface IAutomationBlock
{
    [JsonIgnore]
    public string ServiceKey { get; }
}
