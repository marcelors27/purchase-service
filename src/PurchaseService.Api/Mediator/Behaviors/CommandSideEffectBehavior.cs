using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Mediator.Behaviors;

public sealed class CommandSideEffectBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<CommandSideEffectBehavior<TRequest, TResponse>> _logger;

    public CommandSideEffectBehavior(ILogger<CommandSideEffectBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TRequest, TResponse> next)
    {
        var response = await next(request, cancellationToken).ConfigureAwait(false);

        if (request is ICommand<TResponse>)
        {
            _logger.LogInformation(
                "Command {CommandName} completed; an AMQP side-effect message would be queued here.",
                typeof(TRequest).Name);
        }

        return response;
    }
}
