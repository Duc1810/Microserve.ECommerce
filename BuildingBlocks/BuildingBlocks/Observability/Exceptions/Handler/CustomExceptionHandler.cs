using BuildingBlocks.Observability.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace BuildingBlocks.Observability.Exceptions.Handler
{
   

    public class CustomExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<CustomExceptionHandler> _logger;

        public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                "Error Message: {exceptionMessage}, Time of occurrence: {time}",
                exception.Message, DateTime.UtcNow);


            (string Detail, string Title, int StatusCode) details = exception switch
            {
                InternalServerException =>
                    (exception.Message, nameof(InternalServerException), StatusCodes.Status500InternalServerError),
                ValidationException =>
                    (exception.Message, nameof(ValidationException), StatusCodes.Status400BadRequest),
                BadRequestException =>
                    (exception.Message, nameof(BadRequestException), StatusCodes.Status400BadRequest),
                NotFoundException =>
                    (exception.Message, nameof(NotFoundException), StatusCodes.Status404NotFound),
                UnauthorizedException =>
                    (exception.Message, nameof(UnauthorizedException), StatusCodes.Status401Unauthorized),
                ForbiddenException =>
                    (exception.Message, nameof(ForbiddenException), StatusCodes.Status403Forbidden),
                _ =>
                    (exception.Message, exception.GetType().Name, StatusCodes.Status500InternalServerError)
            };

            context.Response.StatusCode = details.StatusCode;
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Title = details.Title,
                Detail = details.Detail,
                Status = details.StatusCode,
                Instance = context.Request.Path
            };

            problemDetails.Extensions.Add("traceId", context.TraceIdentifier);
            
            if (exception is ValidationException validationException)
            {
                problemDetails.Extensions.Add("ValidationErrors", validationException.Errors);
            }

            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
            return true;
        }
    }
}