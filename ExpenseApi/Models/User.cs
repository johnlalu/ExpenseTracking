using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ExpenseApi.Models;

/// <summary>
/// User identity model for authentication and authorization.
/// </summary>
public class User
{
    /// <summary>
    /// Unique user identifier (also serves as document ID in Cosmos DB).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// User ID for partition key (same as Id).
    /// </summary>
    [JsonPropertyName("userId")]
    [JsonProperty("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// User's email address (unique).
    /// </summary>
    [JsonPropertyName("email")]
    [JsonProperty("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Hashed password.
    /// </summary>
    [JsonPropertyName("passwordHash")]
    [JsonProperty("passwordHash")]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Full name of the user.
    /// </summary>
    [JsonPropertyName("fullName")]
    [JsonProperty("fullName")]
    public string? FullName { get; set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    [JsonPropertyName("isActive")]
    [JsonProperty("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Account creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last login timestamp.
    /// </summary>
    [JsonPropertyName("lastLoginAt")]
    [JsonProperty("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Refresh token for token rotation.
    /// </summary>
    [JsonPropertyName("refreshToken")]
    [JsonProperty("refreshToken")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Refresh token expiration.
    /// </summary>
    [JsonPropertyName("refreshTokenExpiresAt")]
    [JsonProperty("refreshTokenExpiresAt")]
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    [JsonProperty("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
