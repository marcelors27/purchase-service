using PurchaseService.Api.Services.Currency;

namespace PurchaseService.Api.Tests.TestDoubles;

public sealed class TestTreasuryRatesClient(
    Func<string, DateOnly, CancellationToken, Task<ExchangeRateDetails?>> handler) : ITreasuryRatesClient
{
    private readonly Func<string, DateOnly, CancellationToken, Task<ExchangeRateDetails?>> _handler = handler;

    public Task<ExchangeRateDetails?> GetExchangeRateAsync(
        string currencyCode,
        DateOnly purchaseDate,
        CancellationToken cancellationToken) =>
        _handler(currencyCode, purchaseDate, cancellationToken);
}
