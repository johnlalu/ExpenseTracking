namespace ExpenseApi.Data.Repository;

/// <summary>
/// Repository interface for category data operations.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Get all categories (default + user custom).
    /// </summary>
    Task<List<Models.Category>> GetAllAsync(string userId);

    /// <summary>
    /// Get category by ID.
    /// </summary>
    Task<Models.Category?> GetByIdAsync(string id, string userId);

    /// <summary>
    /// Create a new category.
    /// </summary>
    Task<Models.Category> CreateAsync(Models.Category category);

    /// <summary>
    /// Delete a category.
    /// </summary>
    Task DeleteAsync(string id, string userId);
}
