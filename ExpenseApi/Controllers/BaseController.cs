using Microsoft.AspNetCore.Mvc;

namespace ExpenseApi.Controllers;

/// <summary>
/// Base controller with common functionality.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Get current user ID from JWT claims.
    /// </summary>
    protected string? GetUserId()
    {
        return User.FindFirst("sub")?.Value ?? User.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Get current user ID from JWT claims (alias for GetUserId).
    /// </summary>
    protected string? GetUserIdFromClaims()
    {
        return GetUserId();
    }

    /// <summary>
    /// Return standardized error response.
    /// </summary>
    protected IActionResult BadRequestWithDetails(Dictionary<string, string[]> errors)
    {
        var response = new Models.Responses.ErrorResponse
        {
            Message = "Validation failed",
            StatusCode = 400,
            Details = errors,
            LogId = HttpContext.TraceIdentifier
        };
        return BadRequest(response);
    }
}
