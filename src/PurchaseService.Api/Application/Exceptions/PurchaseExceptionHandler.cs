using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PurchaseService.Api.Services.Currency;

namespace PurchaseService.Api.Application.Exceptions;

public sealed class PurchaseExceptionHandler : IExceptionHandler
{
    private readonly ILogger<PurchaseExceptionHandler> _logger;

    public PurchaseExceptionHandler(ILogger<PurchaseExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var rootException = Unwrap(exception);

        if (rootException is RequestValidationException validationException)
        {
            _logger.LogWarning(exception, "Request validation failed.");

            var errors = new Dictionary<string, string[]>(validationException.Errors, StringComparer.OrdinalIgnoreCase);
            var result = Results.ValidationProblem(errors, title: "Request validation failed");
            await result.ExecuteAsync(httpContext);
            return true;
        }

        if (rootException is CurrencyConversionException conversionException)
        {
            _logger.LogWarning(exception, "Currency conversion failed.");

            var result = Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Currency conversion failed",
                detail: conversionException.Message);

            await result.ExecuteAsync(httpContext);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception.");

        var fallbackResult = Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.");

        await fallbackResult.ExecuteAsync(httpContext);
        return true;
    }

    private static Exception Unwrap(Exception exception)
    {
        var current = exception;

        while (current is TargetInvocationException { InnerException: not null } targetInvocationException)
        {
            current = targetInvocationException.InnerException;
        }

        if (current is AggregateException aggregateException &&
            aggregateException.InnerExceptions.Count == 1 &&
            aggregateException.InnerExceptions[0] is Exception singleInner)
        {
            current = singleInner;
        }

        return current ?? exception;
    }
}
