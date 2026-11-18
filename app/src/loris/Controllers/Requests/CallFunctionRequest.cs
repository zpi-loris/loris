using Loris.OpenApi;

namespace Loris.Controllers.Requests;

public record CallFunctionRequest
{
    [JsonOneOfTypes(typeof(bool), typeof(int), typeof(float), typeof(string))]
    public required object Arg { get; set; }
}
