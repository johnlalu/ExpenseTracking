namespace ExpenseApi.Models;

/// <summary>
/// User identity model for authentication and authorization.
/// </summary>
public class User
{
    /// <summary>
    /// Unique user identifier (also serves as document ID in Cosmos DB).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// User ID for partition key (same as Id).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User's email address (unique).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Hashed password.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Refresh token for token rotation.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Refresh token expiration.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}
