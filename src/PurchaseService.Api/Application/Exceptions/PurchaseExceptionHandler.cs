using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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
        if (exception is RequestValidationException validationException)
        {
            _logger.LogWarning(exception, "Request validation failed.");

            var errors = new Dictionary<string, string[]>(validationException.Errors, StringComparer.OrdinalIgnoreCase);
            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed"
            };

            await WriteProblemDetailsAsync(httpContext, problemDetails, cancellationToken);
            return true;
        }

        if (exception is CurrencyConversionException conversionException)
        {
            _logger.LogWarning(exception, "Currency conversion failed.");

            var problemDetails = new ProblemDetails
            {
                Title = "Currency conversion failed",
                Detail = conversionException.Message,
                Status = StatusCodes.Status400BadRequest
            };

            await WriteProblemDetailsAsync(httpContext, problemDetails, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception.");

        var fallbackProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred."
        };

        await WriteProblemDetailsAsync(httpContext, fallbackProblem, cancellationToken);
        return true;
    }

    private static async Task WriteProblemDetailsAsync(HttpContext httpContext, ProblemDetails problemDetails, CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), jsonOptions);
        await httpContext.Response.WriteAsync(json, cancellationToken);
    }
}
