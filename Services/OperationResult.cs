namespace gift_of_the_givers.Services;

public sealed record OperationResult(bool Succeeded, string? Error = null);

public sealed record OperationResult<T>(bool Succeeded, T? Value = default, string? Error = null);
