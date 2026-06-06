using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ExpenseApi.Models;

/// <summary>
/// Represents an expense category.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique identifier for the category.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// User ID who created this category (email for custom categories).
    /// </summary>
    [JsonPropertyName("userId")]
    [JsonProperty("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Category name.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Indicates if this is a default category (system-wide).
    /// </summary>
    [JsonPropertyName("isDefault")]
    [JsonProperty("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Timestamp when category was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    [JsonProperty("isDeleted")]
    public bool IsDeleted { get; set; }
}
