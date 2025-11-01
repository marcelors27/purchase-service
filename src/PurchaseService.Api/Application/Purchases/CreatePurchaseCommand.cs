using PurchaseService.Api.Contracts;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Application.Purchases;

public sealed record CreatePurchaseCommand(
    string Description,
    DateOnly TransactionDate,
    decimal Amount) : ICommand<PurchaseResponse>;
