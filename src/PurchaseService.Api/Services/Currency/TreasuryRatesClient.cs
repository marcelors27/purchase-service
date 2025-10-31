using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PurchaseService.Api.Services.Currency;

public sealed class TreasuryRatesClient : ITreasuryRatesClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TreasuryRatesClient> _logger;

    public TreasuryRatesClient(HttpClient httpClient, ILogger<TreasuryRatesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExchangeRateDetails?> GetExchangeRateAsync(
        string currencyCode,
        DateOnly purchaseDate,
        CancellationToken cancellationToken)
    {
        var normalizedCurrency = currencyCode.ToUpperInvariant();
        var query = $"?fields=record_date,currency,exchange_rate&filter=currency:eq:{Uri.EscapeDataString(normalizedCurrency)},record_date:lte:{purchaseDate:yyyy-MM-dd}&sort=-record_date&limit=1";

        using var response = await _httpClient.GetAsync(query, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Treasury API returned {StatusCode}: {Body}", response.StatusCode, body);
            throw new CurrencyConversionException("Failed to retrieve exchange rate from Treasury API.");
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning("Unexpected payload from Treasury API: {Payload}", document.RootElement);
            throw new CurrencyConversionException("Treasury API returned an unexpected response.");
        }

        if (dataElement.GetArrayLength() == 0)
        {
            return null;
        }

        var first = dataElement[0];
        if (!TryReadExchangeRate(first, normalizedCurrency, out var rateDetails))
        {
            throw new CurrencyConversionException("Treasury API returned an unreadable exchange rate.");
        }

        return rateDetails;
    }

    private bool TryReadExchangeRate(JsonElement element, string currency, out ExchangeRateDetails rateDetails)
    {
        rateDetails = default!;

        if (!element.TryGetProperty("record_date", out var recordDateElement) ||
            recordDateElement.GetString() is not { } recordDateString ||
            !DateOnly.TryParse(recordDateString, out var recordDate))
        {
            _logger.LogWarning("Unable to read record_date from Treasury API payload: {Payload}", element);
            return false;
        }

        if (!element.TryGetProperty("exchange_rate", out var rateElement) ||
            rateElement.GetString() is not { } rateString ||
            !decimal.TryParse(rateString, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
        {
            _logger.LogWarning("Unable to read exchange_rate from Treasury API payload: {Payload}", element);
            return false;
        }

        rateDetails = new ExchangeRateDetails(currency, recordDate, rate);
        return true;
    }
}
