using System.ComponentModel.DataAnnotations;

namespace PurchaseService.Api.Contracts;

public sealed record CreatePurchaseRequest(
    [property: Required] string Description,
    [property: Required] DateOnly TransactionDate,
    [property: Range(0.01, double.MaxValue)] decimal Amount);
