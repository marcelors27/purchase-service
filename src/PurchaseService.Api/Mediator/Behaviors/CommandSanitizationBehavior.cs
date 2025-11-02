using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PurchaseService.Api.Application.Exceptions;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Mediator.Behaviors;

public sealed class CommandSanitizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICommandSanitizer<TRequest>? _sanitizer;
    private readonly ILogger<CommandSanitizationBehavior<TRequest, TResponse>> _logger;

    public CommandSanitizationBehavior(
        IEnumerable<ICommandSanitizer<TRequest>> sanitizers,
        ILogger<CommandSanitizationBehavior<TRequest, TResponse>> logger)
    {
        _sanitizer = sanitizers.FirstOrDefault();
        _logger = logger;
    }

    public Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TRequest, TResponse> next)
    {
        if (request is not ICommand<TResponse> || _sanitizer is null)
        {
            return next(request, cancellationToken);
        }

        var (sanitized, errors) = _sanitizer.Sanitize(request);
        if (errors is { Count: > 0 })
        {
            _logger.LogWarning(
                "Command {CommandName} failed sanitization with errors {@Errors}",
                typeof(TRequest).Name,
                errors);

            throw new RequestValidationException(errors);
        }

        if (!ReferenceEquals(request, sanitized))
        {
            _logger.LogDebug(
                "Command {CommandName} sanitized successfully.",
                typeof(TRequest).Name);
        }

        return next(sanitized, cancellationToken);
    }
}
