using Microsoft.Azure.Cosmos;
using ExpenseApi.Models;

namespace ExpenseApi.Data;

/// <summary>
/// Cosmos DB context for managing database connections and containers.
/// </summary>
public class CosmosDbContext
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;

    public CosmosDbContext(IConfiguration configuration)
    {
        var cosmosSettings = configuration.GetSection("CosmosDb").Get<AppConfig.CosmosDbSettings>()
            ?? throw new InvalidOperationException("CosmosDb configuration not found");

        _databaseName = cosmosSettings.DatabaseName 
            ?? throw new InvalidOperationException("CosmosDb DatabaseName not configured");
        
        _cosmosClient = new CosmosClient(
            accountEndpoint: cosmosSettings.EndpointUri ?? throw new InvalidOperationException("CosmosDb EndpointUri not configured"),
            authKeyOrResourceToken: cosmosSettings.PrimaryKey ?? throw new InvalidOperationException("CosmosDb PrimaryKey not configured")
        );
    }

    /// <summary>
    /// Gets the Users container from Cosmos DB.
    /// </summary>
    public async Task<Container> GetUsersContainerAsync()
    {
        return _cosmosClient.GetContainer(_databaseName, "users");
    }

    /// <summary>
    /// Gets the Expenses container from Cosmos DB.
    /// </summary>
    public async Task<Container> GetExpensesContainerAsync()
    {
        return _cosmosClient.GetContainer(_databaseName, "expenses");
    }

    /// <summary>
    /// Gets the Categories container from Cosmos DB.
    /// </summary>
    public async Task<Container> GetCategoriesContainerAsync()
    {
        return _cosmosClient.GetContainer(_databaseName, "categories");
    }

    /// <summary>
    /// Initialize Cosmos DB database and containers if they don't exist.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Get or create database
            var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);

            // Define container properties
            var containerProperties = new[]
            {
                new ContainerProperties
                {
                    Id = "users",
                    PartitionKeyPath = "/userId"
                },
                new ContainerProperties
                {
                    Id = "expenses",
                    PartitionKeyPath = "/userId"
                },
                new ContainerProperties
                {
                    Id = "categories",
                    PartitionKeyPath = "/userId"
                }
            };

            // Create containers if they don't exist
            foreach (var containerProp in containerProperties)
            {
                await database.Database.CreateContainerIfNotExistsAsync(containerProp);
            }
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Database or container already exists
            }
            else
            {
                throw;
            }
        }
    }

    public void Dispose()
    {
        _cosmosClient?.Dispose();
    }
}
