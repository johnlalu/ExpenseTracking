namespace ExpenseApi.Models.Responses;

/// <summary>
/// Response model for authentication endpoints.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT access token.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// User email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; } = 900; // 15 minutes
}
