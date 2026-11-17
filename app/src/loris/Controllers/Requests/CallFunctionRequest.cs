using Loris.OpenApi;

namespace Loris.Controllers.Requests;

public class CallFunctionRequest
{
    [OneOfTypes(typeof(bool), typeof(int), typeof(float), typeof(string))]
    public object Arg { get; set; } = null!;
}
