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
    /// Category of the expense.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Date of purchase.
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// URL to receipt image in Blob Storage.
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Whether this expense has been paid/reimbursed.
    /// </summary>
    public bool Paid { get; set; }
}
