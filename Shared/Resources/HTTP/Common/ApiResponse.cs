namespace Shared.Resources.HTTP.Common;

public record ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }

    public static ApiResponse Ok(string? message = null)
        => new() { Success = true, Message = message, StatusCode = 200 };

    public static ApiResponse Fail(string error, int statusCode = 400)
        => new() { Success = false, Error = error, StatusCode = statusCode };
}

public record ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
    public string? Token { get; init; }

    public static ApiResponse<T> Success(T data, string? token = null)
        => new() { Success = true, Data = data, Token = token, StatusCode = 200 };

    public static ApiResponse<T> Created(T data, string? token = null)
        => new() { Success = true, Data = data, Token = token, StatusCode = 201 };

    public new static ApiResponse<T> Fail(string error, int statusCode = 400)
        => new() { Success = false, Error = error, StatusCode = statusCode };
}
