namespace ExpenseApi.Models.Responses;

/// <summary>
/// Response model for expense data.
/// </summary>
public class ExpenseResponse
{
    /// <summary>
    /// Unique identifier for the expense.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Description of the expense.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Amount of the expense.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Category of the expense.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Source or vendor.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Date of purchase.
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Receipt image URL.
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
