namespace PurchaseService.Api.Configuration;

public sealed class TreasuryRatesOptions
{
    public const string DefaultBaseUrl = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange/";

    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public int HttpTimeoutSeconds { get; set; } = 10;

    public string UserAgent { get; set; } = "purchase-service/1.0";

    public int CacheDurationSeconds { get; set; } = 300;
}
