using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseService.Api.Contracts;
using PurchaseService.Api.Data;
using PurchaseService.Api.Domain;
using PurchaseService.Api.Services.Currency;
using PurchaseService.Api.Validation;

namespace PurchaseService.Api.Controllers;

[ApiController]
[Route("purchases")]
public sealed class PurchasesController : ControllerBase
{
    private readonly IPurchaseRepository _repository;
    private readonly CurrencyConversionService _conversionService;

    public PurchasesController(IPurchaseRepository repository, CurrencyConversionService conversionService)
    {
        _repository = repository;
        _conversionService = conversionService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchase(
        [FromBody] CreatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = PurchaseRequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(validationErrors));
        }

        var purchase = new Purchase(
            Guid.NewGuid(),
            request.Description.Trim(),
            request.TransactionDate,
            Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            DateTimeOffset.UtcNow);

        await _repository.CreateAsync(purchase, cancellationToken);

        return Created($"/purchases/{purchase.Id}", PurchaseResponse.FromPurchase(purchase));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConvertedPurchaseResponse>> GetPurchase(
        Guid id,
        [FromQuery] string currency,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return BadRequest(new { error = "Currency query parameter is required." });
        }

        var purchase = await _repository.GetByIdAsync(id, cancellationToken);
        if (purchase is null)
        {
            return NotFound();
        }

        try
        {
            var converted = await _conversionService.ConvertAsync(purchase, currency, cancellationToken);
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
