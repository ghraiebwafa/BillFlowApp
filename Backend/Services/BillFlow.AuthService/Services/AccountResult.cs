namespace BillFlow.AuthService.Services;

public sealed class AccountResult<T>
{
    public T? Value { get; init; }

    public string? Error { get; init; }

    public int StatusCode { get; init; } = StatusCodes.Status400BadRequest;

    public bool IsSuccess => Value is not null;

    public static AccountResult<T> Ok(T value, int statusCode = StatusCodes.Status200OK) =>
        new() { Value = value, StatusCode = statusCode };

    public static AccountResult<T> Fail(string error, int statusCode) =>
        new() { Error = error, StatusCode = statusCode };
}
