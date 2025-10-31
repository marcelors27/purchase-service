using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PurchaseService.Api.Services.Currency;
using PurchaseService.Api.Tests.TestDoubles;
using Testcontainers.PostgreSql;

namespace PurchaseService.Api.Tests.Fixtures;

public sealed class PurchaseApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private Func<string, DateOnly, CancellationToken, Task<ExchangeRateDetails?>> _exchangeRateHandler = (
        currency,
        purchaseDate,
        _) => Task.FromResult<ExchangeRateDetails?>(new ExchangeRateDetails(currency.ToUpperInvariant(), purchaseDate, 1m));

    public void SetExchangeRateHandler(Func<string, DateOnly, CancellationToken, Task<ExchangeRateDetails?>> handler) =>
        _exchangeRateHandler = handler;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_postgresContainer is null)
        {
            return;
        }

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = _postgresContainer.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITreasuryRatesClient>();
            services.AddSingleton<ITreasuryRatesClient>(new TestTreasuryRatesClient((currency, purchaseDate, token) =>
                _exchangeRateHandler(currency, purchaseDate, token)));
        });
    }

    public async Task InitializeAsync()
    {
        var toggle = Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS");
        if (!string.Equals(toggle, "1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(toggle, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("purchases")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .WithImage("postgres:16.4-alpine")
                .Build();

            await _postgresContainer.StartAsync();
        }
        catch (DockerUnavailableException)
        {
            _postgresContainer = null;
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        if (_postgresContainer is null)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "TRUNCATE TABLE purchases;";
        await command.ExecuteNonQueryAsync();
    }
}
