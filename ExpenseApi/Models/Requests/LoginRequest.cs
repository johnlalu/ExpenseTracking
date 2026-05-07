namespace ExpenseApi.Models.Requests;

/// <summary>
/// Request model for user login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User password.
    /// </summary>
    public string? Password { get; set; }
}
