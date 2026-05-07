namespace ExpenseApi.Services;

/// <summary>
/// Service for user authentication and account management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user account.
    /// </summary>
    Task<(bool Success, string? Message, Models.User? User)> RegisterAsync(Models.Requests.RegisterRequest request);

    /// <summary>
    /// Authenticate user with email and password.
    /// </summary>
    Task<(bool Success, string? Message, Models.Responses.AuthResponse? Response)> LoginAsync(Models.Requests.LoginRequest request);

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    Task<(bool Success, string? Message, Models.Responses.AuthResponse? Response)> RefreshTokenAsync(string refreshToken, string userId);

    /// <summary>
    /// Verify a user's password.
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Hash a password for storage.
    /// </summary>
    string HashPassword(string password);
}

/// <summary>
/// Authentication service implementation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthService(
        ILogger<AuthService> logger,
        TokenService tokenService,
        IConfiguration configuration)
    {
        _logger = logger;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    public async Task<(bool Success, string? Message, Models.User? User)> RegisterAsync(Models.Requests.RegisterRequest request)
    {
        try
        {
            // Validation would be done by FluentValidation middleware
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return (false, "Email and password are required", null);
            }

            // In a real scenario, check if user exists in database
            // For now, we'll simulate the creation
            var user = new Models.User
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User {Email} registered successfully", request.Email);
            return (true, "Registration successful", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", request.Email);
            return (false, "An error occurred during registration", null);
        }
    }

    /// <summary>
    /// Authenticate user with email and password.
    /// </summary>
    public async Task<(bool Success, string? Message, Models.Responses.AuthResponse? Response)> LoginAsync(Models.Requests.LoginRequest request)
    {
        try
        {
            // Validation would be done by FluentValidation middleware
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return (false, "Email and password are required", null);
            }

            // In a real scenario, fetch user from database
            // For now, simulate a successful login
            // This would be replaced with actual database lookup
            var user = await GetUserByEmailAsync(request.Email);
            
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash ?? string.Empty))
            {
                _logger.LogWarning("Failed login attempt for {Email}", request.Email);
                return (false, "Invalid email or password", null);
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id ?? string.Empty, user.Email ?? string.Empty);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // In real scenario, save refresh token to database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;

            var response = new Models.Responses.AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email,
                ExpiresIn = 900 // 15 minutes
            };

            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            return (true, "Login successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user {Email}", request.Email);
            return (false, "An error occurred during login", null);
        }
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    public async Task<(bool Success, string? Message, Models.Responses.AuthResponse? Response)> RefreshTokenAsync(string refreshToken, string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(userId))
            {
                return (false, "Refresh token and user ID are required", null);
            }

            // In real scenario, verify refresh token against database
            var user = await GetUserByIdAsync(userId);
            
            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid refresh token for user {UserId}", userId);
                return (false, "Invalid or expired refresh token", null);
            }

            // Generate new tokens
            var newAccessToken = _tokenService.GenerateAccessToken(user.Id ?? string.Empty, user.Email ?? string.Empty);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Save new refresh token to database
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            var response = new Models.Responses.AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Email = user.Email,
                ExpiresIn = 900
            };

            _logger.LogInformation("Token refreshed for user {UserId}", userId);
            return (true, "Token refresh successful", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token for user {UserId}", userId);
            return (false, "An error occurred during token refresh", null);
        }
    }

    /// <summary>
    /// Hash a password using BCrypt algorithm.
    /// </summary>
    public string HashPassword(string password)
    {
        // Using BCrypt for password hashing
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verify password against hash.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }

    /// <summary>
    /// Get user by email (simulated - replace with actual DB call).
    /// </summary>
    private async Task<Models.User?> GetUserByEmailAsync(string email)
    {
        // TODO: Replace with actual database query
        // For now, return null (user not found)
        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Get user by ID (simulated - replace with actual DB call).
    /// </summary>
    private async Task<Models.User?> GetUserByIdAsync(string userId)
    {
        // TODO: Replace with actual database query
        // For now, return null (user not found)
        await Task.CompletedTask;
        return null;
    }
}
