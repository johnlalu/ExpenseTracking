using FluentValidation;

namespace ExpenseApi.Validation;

/// <summary>
/// Validation rules for user login.
/// </summary>
public class LoginRequestValidator : AbstractValidator<Models.Requests.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
