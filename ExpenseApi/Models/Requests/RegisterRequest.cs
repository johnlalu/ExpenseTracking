namespace ExpenseApi.Models.Requests;

/// <summary>
/// Request model for user registration.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Password confirmation.
    /// </summary>
    public string? ConfirmPassword { get; set; }

    /// <summary>
    /// User's full name.
    /// </summary>
    public string? FullName { get; set; }
}
