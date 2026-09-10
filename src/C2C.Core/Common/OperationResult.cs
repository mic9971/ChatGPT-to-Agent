namespace C2C.Core.Common;

/// <summary>
/// Typed operation result envelope preventing exceptions for expected domain states.
/// </summary>
public sealed record OperationResult<T>(T? Value, OperationError? Error)
{
    public bool IsSuccess => Error is null;
    public bool IsFailure => Error is not null;

    public static OperationResult<T> Success(T value) => new(value, null);

    public static OperationResult<T> Failure(string code, string message, string? target = null) =>
        new(default, new OperationError(code, message, target));

    public static OperationResult<T> Failure(OperationError error) =>
        new(default, error);
}
