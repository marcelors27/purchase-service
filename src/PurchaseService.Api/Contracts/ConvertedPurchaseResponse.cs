using PurchaseService.Api.Domain;
using PurchaseService.Api.Services.Currency;

namespace PurchaseService.Api.Contracts;

public sealed record ConvertedPurchaseResponse(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal AmountUsd,
    string Currency,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    decimal ConvertedAmount)
{
    public static ConvertedPurchaseResponse FromPurchase(
        Purchase purchase,
        string currency,
        ExchangeRateDetails exchangeRate,
        decimal convertedAmount) =>
        new(
            purchase.Id,
            purchase.Description,
            purchase.TransactionDate,
            purchase.AmountUsd,
            currency,
            exchangeRate.Rate,
            exchangeRate.EffectiveDate,
            convertedAmount);
}
