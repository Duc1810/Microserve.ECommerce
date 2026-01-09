using System.Net;
using BuildingBlocks.Results;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Observability.ApiResponse;

public static class ResultToActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(ApiResponseNew<T>.Ok(result.Value!, result.Message ?? "Successful"));

        var err = result.Error!.Value;
        var payload = ApiResponseNew<object>.Fail(err.Code, err.Message, err.Details);
        var status = (int)err.StatusCode;

        return new ObjectResult(payload) { StatusCode = status };
    }
    public static IActionResult ToCreatedAtActionResult<T>(
        this Result<T> result,
        string?actionName,
        object? routeValues = null)
    {
        if (!result.IsSuccess)
        {
            var err = result.Error!.Value;
            var errorPayload = ApiResponseNew<object>.Fail(err.Code, err.Message, err.Details);
            return new ObjectResult(errorPayload) { StatusCode = (int)err.StatusCode };
        }

        var payload = ApiResponseNew<T>.Ok(result.Value!, result.Message ?? "Created");
        return new CreatedAtActionResult(
            actionName,
            controllerName: null,  
            routeValues: routeValues,
            value: payload
        );
    }

    public static IActionResult Fail(HttpStatusCode status, string code, string message, object? details = null)
        => new ObjectResult(ApiResponseNew<object>.Fail(code, message, details)) { StatusCode = (int)status };
}

public class ApiResponseNew<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public T? Data { get; init; }
    public object? Errors { get; init; }

    public static ApiResponseNew<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponseNew<T> Fail(string errorCode, string message, object? errors = null)
        => new() { Success = false, ErrorCode = errorCode, Message = message, Errors = errors };
}
