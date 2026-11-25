namespace Loris.Results.Errors;

public static class FunctionErrors
{
    public static Error NotFound(Guid functionId) =>
        Error.NotFound(
            "Functions.NotFound",
            $"No function was found with the functionId {functionId}."
        );

    public static Error InvalidArgument(string functionArgType, string arg) =>
        Error.Invalid(
            "Functions.InvalidArgument",
            $"The argument '{arg}' is not valid for the function argument type '{functionArgType}'."
        );
}
