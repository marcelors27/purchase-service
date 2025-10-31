using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PurchaseService.Api.Configuration;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Domain;
using PurchaseService.Api.Infrastructure;
using PurchaseService.Api.Services.Currency;
using PurchaseService.Api.Validation;

SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.RemoveTypeMap(typeof(DateOnly));
SqlMapper.RemoveTypeMap(typeof(DateOnly?));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<TreasuryRatesOptions>(builder.Configuration.GetSection("TreasuryRates"));

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<CurrencyConversionService>();
builder.Services.AddHttpClient<ITreasuryRatesClient, TreasuryRatesClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TreasuryRatesOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<SchemaInitializer>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<SchemaInitializer>>();
    await initializer.EnsureSchemaAsync(logger, CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/purchases", async (CreatePurchaseRequest request, IPurchaseRepository repository, CancellationToken cancellationToken) =>
{
    var validationErrors = PurchaseRequestValidator.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var purchase = new Purchase(
        Guid.NewGuid(),
        request.Description.Trim(),
        request.TransactionDate,
        Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
        DateTimeOffset.UtcNow);

    await repository.CreateAsync(purchase, cancellationToken);

    return Results.Created($"/purchases/{purchase.Id}", PurchaseResponse.FromPurchase(purchase));
})
.WithName("CreatePurchase")
.WithOpenApi();

app.MapGet("/purchases/{id:guid}", async (
    Guid id,
    string currency,
    IPurchaseRepository repository,
    CurrencyConversionService conversionService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(currency))
    {
        return Results.BadRequest(new { error = "Currency query parameter is required." });
    }

    var purchase = await repository.GetByIdAsync(id, cancellationToken);
    if (purchase is null)
    {
        return Results.NotFound();
    }

    try
    {
        var converted = await conversionService.ConvertAsync(purchase, currency, cancellationToken);
        return Results.Ok(converted);
    }
    catch (CurrencyConversionException ex)
    {
        return Results.Problem(
            title: "Currency conversion failed",
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest);
    }
})
.WithName("GetPurchase")
.WithOpenApi();

app.Run();
