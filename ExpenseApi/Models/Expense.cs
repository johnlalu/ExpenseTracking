namespace ExpenseApi.Models;

/// <summary>
/// Represents an expense in the system.
/// </summary>
public class Expense
{
    /// <summary>
    /// Unique identifier for the expense.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// User ID who created this expense (email).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Brief description of the expense.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Amount of the expense.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR).
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Category of the expense (Meals, Travel, Office, etc.).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Source or vendor of the expense.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Date of purchase.
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// URL to receipt image in Blob Storage.
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Timestamp when expense was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when expense was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; set; }
}
