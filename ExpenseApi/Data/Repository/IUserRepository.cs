namespace ExpenseApi.Data.Repository;

/// <summary>
/// Repository interface for user data operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Create a new user.
    /// </summary>
    Task<Models.User> CreateAsync(Models.User user);

    /// <summary>
    /// Get user by ID.
    /// </summary>
    Task<Models.User?> GetByIdAsync(string id);

    /// <summary>
    /// Get user by email address.
    /// </summary>
    Task<Models.User?> GetByEmailAsync(string email);

    /// <summary>
    /// Update an existing user.
    /// </summary>
    Task<Models.User> UpdateAsync(Models.User user);

    /// <summary>
    /// Check if user exists by email.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email);

    /// <summary>
    /// Get user by refresh token.
    /// </summary>
    Task<Models.User?> GetByRefreshTokenAsync(string refreshToken);
}
