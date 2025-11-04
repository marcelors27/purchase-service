using Microsoft.Extensions.Logging.Abstractions;
using PurchaseService.Api.Application.Exceptions;
using PurchaseService.Api.Application.Purchases;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Mediator.Behaviors;

namespace PurchaseService.Api.Tests.Mediator;

public sealed class CommandSanitizationBehaviorTests
{
    [Fact]
    public async Task Handle_ThrowsRequestValidationException_WhenSanitizerReturnsErrors()
    {
        var sanitizer = new CreatePurchaseCommandSanitizer();
        var behavior = new CommandSanitizationBehavior<CreatePurchaseCommand, PurchaseResponse>(
            new[] { sanitizer },
            NullLogger<CommandSanitizationBehavior<CreatePurchaseCommand, PurchaseResponse>>.Instance);

        var command = new CreatePurchaseCommand(new string('x', 51), new DateOnly(2024, 6, 1), 10m);

        Task<PurchaseResponse> Next(CreatePurchaseCommand _, CancellationToken __) =>
            Task.FromResult(new PurchaseResponse(Guid.NewGuid(), string.Empty, new DateOnly(2024, 6, 1), 10m));

        await Assert.ThrowsAsync<RequestValidationException>(() => behavior.Handle(command, CancellationToken.None, Next));
    }
}
