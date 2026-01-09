using System.Net;
using BuildingBlocks.Observability.ApiResponse;
using BuildingBlocks.Results;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? Array.Empty<IValidator<TRequest>>();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {

        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);


        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .Where(r => r != null)
            .SelectMany(r => r!.Errors ?? new List<ValidationFailure>())
            .Where(f => f != null && !string.IsNullOrWhiteSpace(f.ErrorMessage))
            .GroupBy(f => f!.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x!.ErrorMessage).Distinct().ToArray()
            );

        if (failures.Count == 0)
            return await next();

        var respType = typeof(TResponse);
        if (respType.IsGenericType && respType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var inner = respType.GetGenericArguments()[0];
            var resultType = typeof(Result<>).MakeGenericType(inner);

            var method = resultType.GetMethod(
                "ResponseError",
                new[] { typeof(string), typeof(string), typeof(HttpStatusCode), typeof(object) }
            );

            if (method != null)
            {
                var instance = method.Invoke(null, new object[]
                {
                    ErrorCodes.ValidationFailed,
                    "Validation failed.",
                    HttpStatusCode.BadRequest,
                    failures
                });

                return (TResponse)instance!;
            }
        }
        return await next();
    }
}
