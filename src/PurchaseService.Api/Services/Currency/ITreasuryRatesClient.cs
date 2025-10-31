namespace PurchaseService.Api.Services.Currency;

public interface ITreasuryRatesClient
{
    Task<ExchangeRateDetails?> GetExchangeRateAsync(
        string currencyCode,
        DateOnly purchaseDate,
        CancellationToken cancellationToken);
}
