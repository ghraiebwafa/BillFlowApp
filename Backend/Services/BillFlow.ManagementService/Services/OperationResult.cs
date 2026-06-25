namespace BillFlow.ManagementService.Services;

public sealed class OperationResult<T>
{
    public T? Value { get; init; }

    public string? Error { get; init; }

    public int StatusCode { get; init; } = StatusCodes.Status400BadRequest;

    public bool IsSuccess { get; init; }

    public static OperationResult<T> Ok(T value, int statusCode = StatusCodes.Status200OK) =>
        new() { Value = value, StatusCode = statusCode, IsSuccess = true };

    public static OperationResult<T> Fail(string error, int statusCode) =>
        new() { Error = error, StatusCode = statusCode, IsSuccess = false };
}
