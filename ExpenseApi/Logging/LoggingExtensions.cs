using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ExpenseApi.Logging;

/// <summary>
/// Extension methods for Serilog configuration.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configure Serilog with Application Insights sink.
    /// </summary>
    public static void ConfigureSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        var instrumentationKey = configuration["ApplicationInsights:InstrumentationKey"];
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "ExpenseReimbursement")
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
    }

    /// <summary>
    /// Log an expense operation.
    /// </summary>
    public static void LogExpenseOperation(
        this Microsoft.Extensions.Logging.ILogger logger,
        string operation,
        string userId,
        string? expenseId = null,
        Dictionary<string, object>? context = null)
    {
        var logContext = context ?? new Dictionary<string, object>();
        logContext["Operation"] = operation;
        logContext["UserId"] = userId;
        if (!string.IsNullOrEmpty(expenseId))
            logContext["ExpenseId"] = expenseId;

        logger.LogInformation("Expense operation: {Operation} by {UserId}", operation, userId);
    }
}
