using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PurchaseService.Api.Application.Exceptions;
using PurchaseService.Api.Services.Currency;

namespace PurchaseService.Api.Tests.Application;

public sealed class PurchaseExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_WritesValidationProblemDetails()
    {
        var handler = new PurchaseExceptionHandler(NullLogger<PurchaseExceptionHandler>.Instance);
        var httpContext = CreateHttpContext();

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["description"] = ["Description must not exceed 50 characters."]
        };

        var exception = new RequestValidationException(errors);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal("Request validation failed", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.TryGetProperty("errors", out var errorsNode));
        Assert.True(errorsNode.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task TryHandleAsync_WritesConversionProblemDetails()
    {
        var handler = new PurchaseExceptionHandler(NullLogger<PurchaseExceptionHandler>.Instance);
        var httpContext = CreateHttpContext();

        var exception = new CurrencyConversionException("Conversion failed.");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        Assert.Equal("Currency conversion failed", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Conversion failed.", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_UnwrapsTargetInvocation_ForValidationExceptions()
    {
        var handler = new PurchaseExceptionHandler(NullLogger<PurchaseExceptionHandler>.Instance);
        var httpContext = CreateHttpContext();

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["description"] = ["Description must not exceed 50 characters."]
        };

        var wrapped = new TargetInvocationException(new RequestValidationException(errors));

        var handled = await handler.TryHandleAsync(httpContext, wrapped, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_UnwrapsTargetInvocation_ForCurrencyExceptions()
    {
        var handler = new PurchaseExceptionHandler(NullLogger<PurchaseExceptionHandler>.Instance);
        var httpContext = CreateHttpContext();

        var wrapped = new TargetInvocationException(new CurrencyConversionException("Conversion failed."));

        var handled = await handler.TryHandleAsync(httpContext, wrapped, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Response.Body = new MemoryStream();

        return context;
    }
}
