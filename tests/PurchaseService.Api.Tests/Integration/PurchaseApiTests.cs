using System.Net;
using System.Net.Http.Json;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Services.Currency;
using PurchaseService.Api.Tests.Fixtures;
using PurchaseService.Api.Tests.Infrastructure;

namespace PurchaseService.Api.Tests.Integration;

public sealed class PurchaseApiTests(PurchaseApiFactory factory) : IClassFixture<PurchaseApiFactory>
{
    private readonly PurchaseApiFactory _factory = factory;

    [DockerFact]
    public async Task PostPurchase_PersistsPurchase()
    {
        _factory.SetExchangeRateHandler((currency, date, _) =>
            Task.FromResult<ExchangeRateDetails?>(new ExchangeRateDetails(currency.ToUpperInvariant(), date, 1m)));

        var client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();

        var request = new
        {
            description = "Coffee beans",
            transactionDate = new DateOnly(2024, 6, 15),
            amount = 25.50m
        };

        var response = await client.PostAsJsonAsync("/purchases", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var purchase = await response.Content.ReadFromJsonAsync<PurchaseResponse>();
        Assert.NotNull(purchase);
        Assert.NotEqual(Guid.Empty, purchase!.Id);
        Assert.Equal(request.description, purchase.Description);
        Assert.Equal(request.transactionDate, purchase.TransactionDate);
        Assert.Equal(request.amount, purchase.AmountUsd);
    }

    [DockerFact]
    public async Task GetPurchase_ReturnsConvertedAmount()
    {
        _factory.SetExchangeRateHandler((currency, date, _) =>
            Task.FromResult<ExchangeRateDetails?>(new ExchangeRateDetails(currency.ToUpperInvariant(), date.AddDays(-1), 2m)));

        var client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();

        var createRequest = new
        {
            description = "Conference ticket",
            transactionDate = new DateOnly(2024, 5, 10),
            amount = 150m
        };

        var createResponse = await client.PostAsJsonAsync("/purchases", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<PurchaseResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/purchases/{created!.Id}?currency=eur");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var converted = await getResponse.Content.ReadFromJsonAsync<ConvertedPurchaseResponse>();
        Assert.NotNull(converted);
        Assert.Equal("EUR", converted!.Currency);
        Assert.Equal(300m, converted.ConvertedAmount);
        Assert.Equal(2m, converted.ExchangeRate);
    }

    [DockerFact]
    public async Task GetPurchase_ReturnsBadRequest_WhenRateUnavailable()
    {
        _factory.SetExchangeRateHandler((_, _, _) => Task.FromResult<ExchangeRateDetails?>(null));

        var client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();

        var createRequest = new
        {
            description = "Hotel",
            transactionDate = new DateOnly(2024, 1, 5),
            amount = 200m
        };

        var createResponse = await client.PostAsJsonAsync("/purchases", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<PurchaseResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/purchases/{created!.Id}?currency=JPY");
        Assert.Equal(HttpStatusCode.BadRequest, getResponse.StatusCode);
    }
}
