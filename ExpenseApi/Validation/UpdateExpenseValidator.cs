using FluentValidation;

namespace ExpenseApi.Validation;

/// <summary>
/// Validation rules for expense updates.
/// </summary>
public class UpdateExpenseValidator : AbstractValidator<Models.Requests.UpdateExpenseRequest>
{
    public UpdateExpenseValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Amount cannot exceed 999,999.99");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty().WithMessage("Purchase date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Purchase date cannot be in the future");

    }
}
