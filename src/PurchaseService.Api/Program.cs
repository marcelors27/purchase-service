using Dapper;
using PurchaseService.Api.Application.Purchases;
using PurchaseService.Api.Configuration;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Infrastructure;
using PurchaseService.Api.Mediator;
using PurchaseService.Api.Services.Currency;

SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.RemoveTypeMap(typeof(DateOnly));
SqlMapper.RemoveTypeMap(typeof(DateOnly?));

var builder = WebApplication.CreateBuilder(args);

const string DefaultCorsPolicy = "DefaultCorsPolicy";

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.ConfigureDatabaseOptions(builder.Configuration, builder.Environment.EnvironmentName);
builder.Services.Configure<TreasuryRatesOptions>(builder.Configuration.GetSection("TreasuryRates"));
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        DefaultCorsPolicy,
        policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin());
});

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<CurrencyConversionService>();
builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<IRequestHandler<CreatePurchaseCommand, PurchaseResponse>, CreatePurchaseCommandHandler>();
builder.Services.AddScoped<IRequestHandler<GetPurchaseQuery, ConvertedPurchaseResponse?>, GetPurchaseQueryHandler>();
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

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors(DefaultCorsPolicy);

app.MapControllers();

app.Run();
