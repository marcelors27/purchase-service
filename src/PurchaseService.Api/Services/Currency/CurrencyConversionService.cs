using PurchaseService.Api.Contracts;
using PurchaseService.Api.Domain;

namespace PurchaseService.Api.Services.Currency;

public sealed class CurrencyConversionService
{
    private readonly ITreasuryRatesClient _treasuryRatesClient;

    public CurrencyConversionService(ITreasuryRatesClient treasuryRatesClient)
    {
        _treasuryRatesClient = treasuryRatesClient;
    }

    public async Task<ConvertedPurchaseResponse> ConvertAsync(
        Purchase purchase,
        string currency,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new CurrencyConversionException("Target currency is required.");
        }

        var exchangeRate = await _treasuryRatesClient.GetExchangeRateAsync(
            currency,
            purchase.TransactionDate,
            cancellationToken);

        if (exchangeRate is null)
        {
            throw new CurrencyConversionException(
                $"No exchange rate found for {currency} on or before {purchase.TransactionDate:yyyy-MM-dd}.");
        }

        if (!IsWithinSixMonths(purchase.TransactionDate, exchangeRate.EffectiveDate))
        {
            throw new CurrencyConversionException(
                $"No exchange rate within six months prior to {purchase.TransactionDate:yyyy-MM-dd} for {currency}.");
        }

        var convertedAmount = Math.Round(
            purchase.AmountUsd * exchangeRate.Rate,
            2,
            MidpointRounding.AwayFromZero);

        return ConvertedPurchaseResponse.FromPurchase(
            purchase,
            currency,
            exchangeRate,
            convertedAmount);
    }

    private static bool IsWithinSixMonths(DateOnly purchaseDate, DateOnly rateDate) =>
        rateDate <= purchaseDate && rateDate >= purchaseDate.AddMonths(-6);
}
