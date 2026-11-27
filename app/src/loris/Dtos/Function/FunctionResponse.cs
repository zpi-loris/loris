using Loris.Protocol;

namespace Loris.Dtos.Function;

public record FunctionResponse
{
    public required Guid FunctionId { get; init; }
    public required int DeviceFunctionId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required FuncArgType ArgType { get; init; }

    public static FunctionResponse FromModel(Models.Function function)
    {
        return new FunctionResponse
        {
            FunctionId = function.FunctionId,
            DeviceFunctionId = function.DeviceFunctionId,
            Name = function.Name,
            Description = function.Description,
            ArgType = function.ArgType,
        };
    }
}
