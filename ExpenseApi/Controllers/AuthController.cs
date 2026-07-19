using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseApi.Models.Requests;
using ExpenseApi.Models.Responses;
using ExpenseApi.Services;

namespace ExpenseApi.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login, and token refresh.
/// </summary>
[AllowAnonymous]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Newly created user information</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate request
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequestWithDetails(errors);
        }

        // Attempt registration
        var (success, message, user) = await _authService.RegisterAsync(request);
        if (!success)
        {
            _logger.LogWarning("Registration failed for email {Email}: {Message}", request.Email, message);
            return BadRequest(new ErrorResponse
            {
                Message = message,
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }

        return CreatedAtAction(nameof(Register), new { }, new
        {
            message = "User registered successfully. Please log in.",
            email = user?.Email
        });
    }

    /// <summary>
    /// Authenticate user and return JWT tokens.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Access token and refresh token</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validate request
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequestWithDetails(errors);
        }

        // Attempt login
        var (success, message, response) = await _authService.LoginAsync(request);
        if (!success)
        {
            _logger.LogWarning("Login failed for email {Email}: {Message}", request.Email, message);
            return Unauthorized(new ErrorResponse
            {
                Message = message,
                StatusCode = 401,
                LogId = HttpContext.TraceIdentifier
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using a valid refresh token.
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request?.RefreshToken))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Refresh token is required",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }

        var (success, message, response) = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (!success)
        {
            _logger.LogWarning("Token refresh failed: {Message}", message);
            return Unauthorized(new ErrorResponse
            {
                Message = message,
                StatusCode = 401,
                LogId = HttpContext.TraceIdentifier
            });
        }

        return Ok(response);
    }
}
