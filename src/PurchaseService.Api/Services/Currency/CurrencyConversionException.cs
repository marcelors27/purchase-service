namespace PurchaseService.Api.Services.Currency;

public sealed class CurrencyConversionException : Exception
{
    public CurrencyConversionException(string message)
        : base(message)
    {
    }

    public CurrencyConversionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
