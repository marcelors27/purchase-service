using PurchaseService.Api.Contracts;
using PurchaseService.Api.Domain;
using PurchaseService.Api.Services.Currency;
using PurchaseService.Api.Tests.TestDoubles;

namespace PurchaseService.Api.Tests.Services;

public sealed class CurrencyConversionServiceTests
{
    [Fact]
    public async Task ConvertAsync_ReturnsConvertedPurchase_WhenRateAvailable()
    {
        var purchase = new Purchase(Guid.NewGuid(), "Laptop", new DateOnly(2024, 5, 20), 1000m, DateTimeOffset.UtcNow);
        var rate = new ExchangeRateDetails("Eur", new DateOnly(2024, 5, 15), 0.9m);
        var client = new TestTreasuryRatesClient((_, _, _) => Task.FromResult<ExchangeRateDetails?>(rate));
        var service = new CurrencyConversionService(client);

        var response = await service.ConvertAsync(purchase, "Eur", CancellationToken.None);

        Assert.Equal(900m, response.ConvertedAmount);
        Assert.Equal("Eur", response.Currency);
        Assert.Equal(rate.EffectiveDate, response.ExchangeRateDate);
        Assert.Equal(rate.Rate, response.ExchangeRate);
    }

    [Fact]
    public async Task ConvertAsync_Throws_WhenRateOlderThanSixMonths()
    {
        var purchase = new Purchase(Guid.NewGuid(), "Laptop", new DateOnly(2024, 7, 1), 100m, DateTimeOffset.UtcNow);
        var oldRate = new ExchangeRateDetails("Eur", new DateOnly(2023, 12, 31), 0.9m);
        var client = new TestTreasuryRatesClient((_, _, _) => Task.FromResult<ExchangeRateDetails?>(oldRate));
        var service = new CurrencyConversionService(client);

        await Assert.ThrowsAsync<CurrencyConversionException>(() => service.ConvertAsync(purchase, "Eur", CancellationToken.None));
    }

    [Fact]
    public async Task ConvertAsync_Throws_WhenRateMissing()
    {
        var purchase = new Purchase(Guid.NewGuid(), "Laptop", new DateOnly(2024, 7, 1), 100m, DateTimeOffset.UtcNow);
        var client = new TestTreasuryRatesClient((_, _, _) => Task.FromResult<ExchangeRateDetails?>(null));
        var service = new CurrencyConversionService(client);

        await Assert.ThrowsAsync<CurrencyConversionException>(() => service.ConvertAsync(purchase, "Eur", CancellationToken.None));
    }
}
