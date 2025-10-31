using System.Globalization;
using PurchaseService.Api.Contracts;

namespace PurchaseService.Api.Validation;

public static class PurchaseRequestValidator
{
    public static IDictionary<string, string[]> Validate(CreatePurchaseRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["description"] = ["Description is required."];
        }
        else if (request.Description.Trim().Length > 50)
        {
            errors["description"] = ["Description must not exceed 50 characters."];
        }

        if (request.TransactionDate == default)
        {
            errors["transactionDate"] = ["Transaction date is required."];
        }

        if (request.Amount <= 0)
        {
            errors["amount"] = ["Amount must be a positive value."];
        }
        else if (!IsRoundedToCent(request.Amount))
        {
            errors["amount"] = ["Amount must be rounded to the nearest cent (two decimal places)."];
        }

        return errors;
    }

    private static bool IsRoundedToCent(decimal amount)
    {
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        return decimal.Compare(amount, rounded) == 0;
    }
}
