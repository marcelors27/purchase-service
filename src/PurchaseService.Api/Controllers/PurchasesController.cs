using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseService.Api.Application.Exceptions;
using PurchaseService.Api.Application.Purchases;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Mediator;
using PurchaseService.Api.Services.Currency;

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
        try
        {
            var command = new CreatePurchaseCommand(
                request.Description,
                request.TransactionDate,
                request.Amount);

            var response = await _mediator.Send(command, cancellationToken);
            return Created($"/purchases/{response.Id}", response);
        }
        catch (RequestValidationException ex)
        {
            var errors = ex.Errors.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value);

            return ValidationProblem(new ValidationProblemDetails(errors));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConvertedPurchaseResponse>> GetPurchase(
        Guid id,
        [FromQuery] string currency,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetPurchaseQuery(id, currency);
            var converted = await _mediator.Send(query, cancellationToken);

            if (converted is null)
            {
                return NotFound();
            }

            return Ok(converted);
        }
        catch (CurrencyConversionException ex)
        {
            return Problem(
                title: "Currency conversion failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
