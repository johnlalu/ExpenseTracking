namespace ExpenseApi.Data.Repository;

/// <summary>
/// Repository interface for expense data operations.
/// </summary>
public interface IExpenseRepository
{
    /// <summary>
    /// Create a new expense.
    /// </summary>
    Task<Models.Expense> CreateAsync(Models.Expense expense);

    /// <summary>
    /// Get expense by ID for specific user.
    /// </summary>
    Task<Models.Expense?> GetByIdAsync(string id, string userId);

    /// <summary>
    /// Get all expenses for a user in a specific month.
    /// </summary>
    Task<List<Models.Expense>> GetByMonthYearAsync(string userId, int month, int year);

    /// <summary>
    /// Get expenses in a date range.
    /// </summary>
    Task<List<Models.Expense>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get expenses by category.
    /// </summary>
    Task<List<Models.Expense>> GetByCategoryAsync(string userId, string category);

    /// <summary>
    /// Update an existing expense.
    /// </summary>
    Task<Models.Expense> UpdateAsync(string id, string userId, Models.Expense expense);

    /// <summary>
    /// Soft delete an expense.
    /// </summary>
    Task DeleteAsync(string id, string userId);

    /// <summary>
    /// Get monthly summary aggregation.
    /// </summary>
    Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(string userId);
}
