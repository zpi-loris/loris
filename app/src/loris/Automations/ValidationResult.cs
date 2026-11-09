using Loris.Types;

namespace Loris.Automations;

public readonly record struct ValidationResult
{
    private readonly LookupContext? _lookup;
    private readonly IReadOnlyList<BlockError>? _errors;

    private ValidationResult(LookupContext? lookup, bool isValid, IReadOnlyList<BlockError>? errors)
    {
        _lookup = lookup;
        _errors = errors;
        IsSuccess = isValid;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<BlockError> Errors =>
        IsFailure
            ? _errors!
            : throw new InvalidOperationException(
                "Cannot read errors of a successful validation result"
            );
    public LookupContext? Lookup =>
        IsSuccess
            ? _lookup
            : throw new InvalidOperationException(
                "Cannot read the value of a failed validation result"
            );

    public static ValidationResult Success() => new(default, true, default);

    public static ValidationResult Success(Dictionary<string, object> lookup) =>
        new(LookupContext.FromDict(lookup), true, default);

    public static ValidationResult Failure(IReadOnlyList<BlockError> errors) =>
        new(default, false, errors);
}
