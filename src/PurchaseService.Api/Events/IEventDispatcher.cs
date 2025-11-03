using System.Threading;
using System.Threading.Tasks;

namespace PurchaseService.Api.Events;

public interface IEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IEvent;
}
