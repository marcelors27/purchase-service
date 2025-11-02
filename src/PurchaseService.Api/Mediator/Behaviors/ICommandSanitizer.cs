using System.Collections.Generic;

namespace PurchaseService.Api.Mediator.Behaviors;

public interface ICommandSanitizer<TCommand>
{
    (TCommand Sanitized, IDictionary<string, string[]>? Errors) Sanitize(TCommand command);
}
