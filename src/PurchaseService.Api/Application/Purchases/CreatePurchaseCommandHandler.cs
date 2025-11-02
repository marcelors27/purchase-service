using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Domain;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Application.Purchases;

public sealed class CreatePurchaseCommandHandler : IRequestHandler<CreatePurchaseCommand, PurchaseResponse>
{
    private readonly IPurchaseRepository _repository;

    public CreatePurchaseCommandHandler(IPurchaseRepository repository)
    {
        _repository = repository;
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

        return PurchaseResponse.FromPurchase(purchase);
    }
}
