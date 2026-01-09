using System.Net;

namespace BuildingBlocks.Results;

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    public string Message { get; }

    private Result(T value, string message = "Successul")
    {
        IsSuccess = true;
        Value = value;
        Error = null;
        Message = message;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
        Message = error.Message;
    }

    public static Result<T> ResponseSuccess(T value, string message = "Successful") => new(value, message);
    public static Result<T> ResponseError(string code, string message, HttpStatusCode status, object? details = null)
        => new(new Error(code, message, status, details));

}

public readonly record struct Error(
    string Code,
    string Message,
    HttpStatusCode StatusCode,
    object? Details = null
);
