using PurchaseService.Api.Domain;

namespace PurchaseService.Api.Contracts;

public sealed record PurchaseResponse(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal AmountUsd)
{
    public static PurchaseResponse FromPurchase(Purchase purchase) =>
        new(purchase.Id, purchase.Description, purchase.TransactionDate, purchase.AmountUsd);
}
