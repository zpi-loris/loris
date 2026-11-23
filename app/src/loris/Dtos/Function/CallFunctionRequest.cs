using Loris.OpenApi;

namespace Loris.Dtos.Function;

public record CallFunctionRequest
{
    [JsonOneOfTypes(typeof(bool), typeof(int), typeof(float), typeof(string))]
    public required object Arg { get; init; }
}
