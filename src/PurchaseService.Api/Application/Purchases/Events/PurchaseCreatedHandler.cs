using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PurchaseService.Api.Events;

namespace PurchaseService.Api.Application.Purchases.Events;

public sealed class PurchaseCreatedHandler : IEventHandler<PurchaseCreated>
{
    private readonly ILogger<PurchaseCreatedHandler> _logger;

    public PurchaseCreatedHandler(ILogger<PurchaseCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PurchaseCreated @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PurchaseCreated event handled for PurchaseId {PurchaseId}: internal side-effect could be placed here.",
            @event.PurchaseId);

        return Task.CompletedTask;
    }
}
