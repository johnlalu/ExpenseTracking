using ExpenseApi.Models.Requests;
using ExpenseApi.Validation;

namespace ExpenseApi.Tests.Validation;

public class CreateExpenseValidatorTests
{
    private readonly CreateExpenseValidator _validator = new();

    private static CreateExpenseRequest ValidRequest() => new()
    {
        Description = "Coffee",
        Amount = 5.00m,
        Category = "Food",
        PurchaseDate = DateTime.UtcNow.AddHours(-1)
    };

    [Fact]
    public void Validate_ReturnsValid_WhenAllFieldsCorrect()
    {
        var result = _validator.Validate(ValidRequest());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_Fails_WhenDescriptionEmpty(string? description)
    {
        var request = ValidRequest();
        request.Description = description;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Description));
    }

    [Fact]
    public void Validate_Fails_WhenDescriptionTooLong()
    {
        var request = ValidRequest();
        request.Description = new string('x', 501);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Description));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Fails_WhenAmountNotPositive(decimal amount)
    {
        var request = ValidRequest();
        request.Amount = amount;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Amount));
    }

    [Fact]
    public void Validate_Fails_WhenAmountExceedsMax()
    {
        var request = ValidRequest();
        request.Amount = 1_000_000m;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_Fails_WhenCategoryEmpty(string? category)
    {
        var request = ValidRequest();
        request.Category = category;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.Category));
    }

    [Fact]
    public void Validate_Fails_WhenPurchaseDateInFuture()
    {
        var request = ValidRequest();
        request.PurchaseDate = DateTime.UtcNow.AddDays(1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.PurchaseDate));
    }
}
