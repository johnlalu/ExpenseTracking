namespace ExpenseApi.Services;

using BCrypt.Net;
using ExpenseApi.Data.Repository;

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
    private readonly IUserRepository _userRepository;

    public AuthService(
        ILogger<AuthService> logger,
        TokenService tokenService,
        IConfiguration configuration,
        IUserRepository userRepository)
    {
        _logger = logger;
        _tokenService = tokenService;
        _configuration = configuration;
        _userRepository = userRepository;
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

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt for existing email: {Email}", request.Email);
                return (false, "User with this email already exists", null);
            }

            // Create new user
            var user = new Models.User
            {
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FullName = request.Email.Split('@')[0], // Default to email prefix
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);
            _logger.LogInformation("User {Email} registered successfully", request.Email);
            return (true, "Registration successful", createdUser);
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

            // Fetch user from database
            var user = await _userRepository.GetByEmailAsync(request.Email);
            
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash ?? string.Empty))
            {
                _logger.LogWarning("Failed login attempt for {Email}", request.Email);
                return (false, "Invalid email or password", null);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user {Email}", request.Email);
                return (false, "User account is not active", null);
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id ?? string.Empty, user.Email ?? string.Empty);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Update user with refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

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

            // Get user from database
            var user = await _userRepository.GetByIdAsync(userId);
            
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
            await _userRepository.UpdateAsync(user);

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
        return BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verify password against hash.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }
}
