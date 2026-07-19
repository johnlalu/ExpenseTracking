using ExpenseApi.Controllers;
using ExpenseApi.Models.Requests;
using ExpenseApi.Models.Responses;
using ExpenseApi.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExpenseApi.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IValidator<RegisterRequest>> _registerValidator = new();
    private readonly Mock<IValidator<LoginRequest>> _loginValidator = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    private AuthController CreateSut()
    {
        var controller = new AuthController(
            _authService.Object,
            _registerValidator.Object,
            _loginValidator.Object,
            _logger.Object);
        controller.ControllerContext = TestHelpers.MakeControllerContext();
        return controller;
    }

    // --- RefreshToken ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task RefreshToken_ReturnsBadRequest_WhenTokenNullOrEmpty(string? token)
    {
        var result = await CreateSut().RefreshToken(new RefreshTokenRequest { RefreshToken = token });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOk_WhenServiceSucceeds()
    {
        var authResponse = new AuthResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };
        _authService.Setup(s => s.RefreshTokenAsync("valid-token"))
                    .ReturnsAsync((true, "Token refresh successful", authResponse));

        var result = await CreateSut().RefreshToken(new RefreshTokenRequest { RefreshToken = "valid-token" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(authResponse, ok.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_WhenServiceFails()
    {
        _authService.Setup(s => s.RefreshTokenAsync("expired-token"))
                    .ReturnsAsync((false, "Invalid or expired refresh token", null));

        var result = await CreateSut().RefreshToken(new RefreshTokenRequest { RefreshToken = "expired-token" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshToken_DoesNotRequireAuthenticatedUser()
    {
        // The endpoint must work even when access token is expired (unauthenticated context)
        var authResponse = new AuthResponse { AccessToken = "new", RefreshToken = "new-rt", ExpiresIn = 3600 };
        _authService.Setup(s => s.RefreshTokenAsync("rt"))
                    .ReturnsAsync((true, "ok", authResponse));

        var controller = new AuthController(
            _authService.Object,
            _registerValidator.Object,
            _loginValidator.Object,
            _logger.Object);
        controller.ControllerContext = TestHelpers.MakeControllerContext(userId: null);

        var result = await controller.RefreshToken(new RefreshTokenRequest { RefreshToken = "rt" });

        Assert.IsType<OkObjectResult>(result);
    }

    // --- Login ---

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsValid()
    {
        _loginValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), default))
                       .ReturnsAsync(new ValidationResult());
        var authResponse = new AuthResponse { AccessToken = "token", ExpiresIn = 3600 };
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                    .ReturnsAsync((true, "Login successful", authResponse));

        var result = await CreateSut().Login(new LoginRequest { Email = "a@b.com", Password = "pass" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        _loginValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), default))
                       .ReturnsAsync(new ValidationResult());
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
                    .ReturnsAsync((false, "Invalid email or password", null));

        var result = await CreateSut().Login(new LoginRequest { Email = "a@b.com", Password = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenValidationFails()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required")
        };
        _loginValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), default))
                       .ReturnsAsync(new ValidationResult(failures));

        var result = await CreateSut().Login(new LoginRequest());

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
