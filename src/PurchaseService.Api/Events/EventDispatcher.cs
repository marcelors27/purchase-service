using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurchaseService.Api.Events;

public sealed class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(IServiceProvider serviceProvider, ILogger<EventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>().ToArray();
        if (handlers.Length == 0)
        {
            _logger.LogDebug("No event handlers registered for {EventType}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
        }
    }
}
