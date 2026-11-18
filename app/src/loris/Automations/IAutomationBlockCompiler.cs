namespace Loris.Automations;

public interface IAutomationBlockCompiler<TDto, TResult>
{
    public TResult Compile(TDto dto, LookupContext? lookup);

    public Task<BlockValidationResult> ValidateAsync(
        TDto dto,
        CancellationToken cancellationToken = default
    );
}
