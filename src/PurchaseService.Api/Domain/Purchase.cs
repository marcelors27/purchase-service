namespace PurchaseService.Api.Domain;

public sealed record Purchase(
    Guid Id,
    string Description,
    DateOnly TransactionDate,
    decimal AmountUsd,
    DateTimeOffset CreatedAt);
