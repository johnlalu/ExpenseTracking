namespace ExpenseApi.Models.Responses;

/// <summary>
/// Response containing a list of expenses with pagination metadata.
/// </summary>
public class ExpenseListResponse
{
    /// <summary>
    /// List of expenses.
    /// </summary>
    public List<Expense> Items { get; set; } = new();

    /// <summary>
    /// Total count of items (for pagination).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Page size (for pagination).
    /// </summary>
    public int PageSize { get; set; }
}
