using PurchaseService.Api.Contracts;
using PurchaseService.Api.Validation;

namespace PurchaseService.Api.Tests.Validation;

public sealed class PurchaseRequestValidatorTests
{
    [Fact]
    public void Validate_ReturnsError_WhenDescriptionMissing()
    {
        var request = new CreatePurchaseRequest(string.Empty, new DateOnly(2024, 6, 1), 10.00m);

        var result = PurchaseRequestValidator.Validate(request);

        Assert.True(result.ContainsKey("description"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenDescriptionTooLong()
    {
        var request = new CreatePurchaseRequest(new string('a', 51), new DateOnly(2024, 6, 1), 10.00m);

        var result = PurchaseRequestValidator.Validate(request);

        Assert.True(result.ContainsKey("description"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenAmountNotRoundedToCent()
    {
        var request = new CreatePurchaseRequest("coffee", new DateOnly(2024, 6, 1), 10.005m);

        var result = PurchaseRequestValidator.Validate(request);

        Assert.True(result.ContainsKey("amount"));
    }

    [Fact]
    public void Validate_ReturnsError_WhenAmountIsNegative()
    {
        var request = new CreatePurchaseRequest("coffee", new DateOnly(2024, 6, 1), -1m);

        var result = PurchaseRequestValidator.Validate(request);

        Assert.True(result.ContainsKey("amount"));
    }

    [Fact]
    public void Validate_ReturnsEmpty_WhenValid()
    {
        var request = new CreatePurchaseRequest("coffee", new DateOnly(2024, 6, 1), 12.34m);

        var result = PurchaseRequestValidator.Validate(request);

        Assert.Empty(result);
    }
}
