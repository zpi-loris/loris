namespace Loris.Automations;

public readonly record struct BlockValidationResult
{
    private readonly LookupContext? _lookup;
    private readonly IReadOnlyList<BlockError>? _errors;

    private BlockValidationResult(
        LookupContext? lookup,
        bool isValid,
        IReadOnlyList<BlockError>? errors
    )
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

    public static BlockValidationResult Success() => new(default, true, default);

    public static BlockValidationResult Success(Dictionary<string, object> lookup) =>
        new(LookupContext.FromDict(lookup), true, default);

    public static BlockValidationResult Failure(IReadOnlyList<BlockError> errors) =>
        new(default, false, errors);
}
