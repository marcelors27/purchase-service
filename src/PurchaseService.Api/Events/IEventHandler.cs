using System.Threading;
using System.Threading.Tasks;

namespace PurchaseService.Api.Events;

public interface IEventHandler<in TEvent>
    where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
