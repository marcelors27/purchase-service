using PurchaseService.Api.Application.Purchases.Events;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Domain;
using PurchaseService.Api.Events;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Application.Purchases;

public sealed class CreatePurchaseCommandHandler : IRequestHandler<CreatePurchaseCommand, PurchaseResponse>
{
    private readonly IPurchaseRepository _repository;
    private readonly IEventDispatcher _eventDispatcher;

    public CreatePurchaseCommandHandler(IPurchaseRepository repository, IEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PurchaseResponse> Handle(CreatePurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = new Purchase(
            Guid.NewGuid(),
            request.Description,
            request.TransactionDate,
            request.Amount,
            DateTimeOffset.UtcNow);

        await _repository.CreateAsync(purchase, cancellationToken);
        await _eventDispatcher.PublishAsync(
            new PurchaseCreated(
                purchase.Id,
                purchase.Description,
                purchase.TransactionDate,
                purchase.Amount),
            cancellationToken);

        return PurchaseResponse.FromPurchase(purchase);
    }
}
