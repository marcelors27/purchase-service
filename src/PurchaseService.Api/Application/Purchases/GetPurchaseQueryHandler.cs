using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Mediator;
using PurchaseService.Api.Services.Currency;

namespace PurchaseService.Api.Application.Purchases;

public sealed class GetPurchaseQueryHandler : IRequestHandler<GetPurchaseQuery, ConvertedPurchaseResponse?>
{
    private readonly IPurchaseRepository _repository;
    private readonly CurrencyConversionService _conversionService;

    public GetPurchaseQueryHandler(IPurchaseRepository repository, CurrencyConversionService conversionService)
    {
        _repository = repository;
        _conversionService = conversionService;
    }

    public async Task<ConvertedPurchaseResponse?> Handle(GetPurchaseQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new CurrencyConversionException("Currency query parameter is required.");
        }

        var purchase = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (purchase is null)
        {
            return null;
        }

        return await _conversionService.ConvertAsync(purchase, request.Currency, cancellationToken);
    }
}
