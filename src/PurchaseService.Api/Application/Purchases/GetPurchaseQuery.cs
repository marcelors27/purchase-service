using PurchaseService.Api.Contracts;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Application.Purchases;

public sealed record GetPurchaseQuery(Guid Id, string Currency) : IQuery<ConvertedPurchaseResponse?>;
