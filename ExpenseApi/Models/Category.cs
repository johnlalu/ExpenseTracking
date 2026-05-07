namespace ExpenseApi.Models;

/// <summary>
/// Represents an expense category.
/// </summary>
public class Category
{
    /// <summary>
    /// Unique identifier for the category.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// User ID who created this category (email for custom categories).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Category name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Indicates if this is a default category (system-wide).
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Timestamp when category was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; set; }
}
