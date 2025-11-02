using System;
using System.Collections.Generic;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Mediator.Behaviors;
using PurchaseService.Api.Validation;

namespace PurchaseService.Api.Application.Purchases;

public sealed class CreatePurchaseCommandSanitizer : ICommandSanitizer<CreatePurchaseCommand>
{
    public (CreatePurchaseCommand Sanitized, IDictionary<string, string[]>? Errors) Sanitize(CreatePurchaseCommand command)
    {
        var trimmedDescription = command.Description?.Trim() ?? string.Empty;
        var roundedAmount = Math.Round(command.Amount, 2, MidpointRounding.AwayFromZero);

        var validationErrors = PurchaseRequestValidator.Validate(new CreatePurchaseRequest(
            trimmedDescription,
            command.TransactionDate,
            roundedAmount));

        if (validationErrors.Count > 0)
        {
            return (command, validationErrors);
        }

        var sanitized = command with
        {
            Description = trimmedDescription,
            Amount = roundedAmount
        };

        return (sanitized, null);
    }
}
