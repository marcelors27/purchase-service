namespace PurchaseService.Api.Contracts;

public sealed record CreatePurchaseRequest(
    string Description,
    DateOnly TransactionDate,
    decimal Amount);
