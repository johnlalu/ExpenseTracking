namespace ExpenseApi.Models.Responses;

/// <summary>
/// Standardized error response model.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// User-friendly error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Correlation ID for tracking in Application Insights.
    /// </summary>
    public string? LogId { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Field-level validation errors (if applicable).
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }
}
