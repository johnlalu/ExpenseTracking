namespace ExpenseApi.Models.Requests;

/// <summary>
/// Request model for updating an existing expense.
/// </summary>
public class UpdateExpenseRequest
{
    /// <summary>
    /// Brief description of the expense.
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
    /// Source or vendor name.
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
}
