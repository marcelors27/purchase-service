using PurchaseService.Api.Application.Exceptions;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Domain;
using PurchaseService.Api.Mediator;
using PurchaseService.Api.Validation;

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
        var validationErrors = PurchaseRequestValidator.Validate(new CreatePurchaseRequest(
            request.Description,
            request.TransactionDate,
            request.Amount));

        if (validationErrors.Count > 0)
        {
            throw new RequestValidationException(validationErrors);
        }

        var purchase = new Purchase(
            Guid.NewGuid(),
            request.Description.Trim(),
            request.TransactionDate,
            Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            DateTimeOffset.UtcNow);

        await _repository.CreateAsync(purchase, cancellationToken);

        return PurchaseResponse.FromPurchase(purchase);
    }
}
