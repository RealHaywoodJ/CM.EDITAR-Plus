namespace CM.EDITAR.Core;

/// <summary>Generic outcome wrapper for service calls. Avoids throwing for expected failures.</summary>
public readonly record struct OperationResult(bool Success, string? Message = null, Exception? Exception = null)
{
    public static OperationResult Ok(string? message = null) => new(true, message);
    public static OperationResult Fail(string message, Exception? ex = null) => new(false, message, ex);
}

/// <summary>Result variant carrying a value on success.</summary>
public readonly record struct OperationResult<T>(bool Success, T? Value, string? Message = null, Exception? Exception = null)
{
    public static OperationResult<T> Ok(T value, string? message = null) => new(true, value, message);
    public static OperationResult<T> Fail(string message, Exception? ex = null) => new(false, default, message, ex);
}

/// <summary>Outcome of an Apply pipeline run.</summary>
public sealed record ApplyResult(
    bool Success,
    Guid ManifestId,
    string? SnapshotPath,
    int OperationsAttempted,
    int OperationsSucceeded,
    bool RolledBack,
    string? Message);
