using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Mediator.Behaviors;

public sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TRequest, TResponse> next)
    {
        var requestName = typeof(TRequest).Name;
        var category = request switch
        {
            ICommand<TResponse> => "Command",
            IQuery<TResponse> => "Query",
            _ => "Request"
        };

        _logger.LogInformation(
            "Starting {Category} {RequestName} with payload {@Request}",
            category,
            requestName,
            request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed {Category} {RequestName} in {ElapsedMilliseconds}ms",
                category,
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "{Category} {RequestName} failed after {ElapsedMilliseconds}ms",
                category,
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
