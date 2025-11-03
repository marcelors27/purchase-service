using System;
using PurchaseService.Api.Events;

namespace PurchaseService.Api.Application.Purchases.Events;

public sealed record PurchaseCreated(
    Guid PurchaseId,
    string Description,
    DateOnly TransactionDate,
    decimal Amount) : IEvent;
