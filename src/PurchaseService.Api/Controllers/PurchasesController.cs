using Microsoft.AspNetCore.Mvc;
using PurchaseService.Api.Application.Purchases;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Mediator;

namespace PurchaseService.Api.Controllers;

[ApiController]
[Route("purchases")]
public sealed class PurchasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PurchasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchase(
        [FromBody] CreatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePurchaseCommand(
            request.Description,
            request.TransactionDate,
            request.Amount);

        var response = await _mediator.Send(command, cancellationToken);
        return Created($"/purchases/{response.Id}", response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConvertedPurchaseResponse>> GetPurchase(
        Guid id,
        [FromQuery] string currency,
        CancellationToken cancellationToken)
    {
        var query = new GetPurchaseQuery(id, currency);
        var converted = await _mediator.Send(query, cancellationToken);

        if (converted is null)
        {
            return NotFound();
        }

        return Ok(converted);
    }
}
