namespace PurchaseService.Api.Services.Currency;

public sealed record ExchangeRateDetails(
    string Currency,
    DateOnly EffectiveDate,
    decimal Rate);
