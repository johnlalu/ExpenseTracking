namespace ExpenseApi;

/// <summary>
/// Application configuration constants.
/// </summary>
public static class AppConfig
{
    /// <summary>
    /// Default expense categories available to all users.
    /// </summary>
    public static readonly List<string> DefaultCategories = new()
    {
        "Travel",
        "Meals",
        "Office Supplies",
        "Technology",
        "Accommodation",
        "Transportation",
        "Other"
    };

    /// <summary>
    /// JWT configuration.
    /// </summary>
    public class JwtSettings
    {
        public string? SecretKey { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int ExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }

    /// <summary>
    /// Cosmos DB configuration.
    /// </summary>
    public class CosmosDbSettings
    {
        public string? EndpointUri { get; set; }
        public string? PrimaryKey { get; set; }
        public string? DatabaseName { get; set; }
        public string? ContainerName { get; set; }
    }
}
