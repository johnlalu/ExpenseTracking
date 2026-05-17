using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ExpenseApi.Models;

namespace ExpenseApi.Data.Repository;

/// <summary>
/// Implementation of IUserRepository for Cosmos DB.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(CosmosDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Models.User> CreateAsync(Models.User user)
    {
        try
        {
            var container = await _context.GetUsersContainerAsync();
            user.Id = Guid.NewGuid().ToString();
            user.UserId = user.Id; // Set partition key
            user.CreatedAt = DateTime.UtcNow;
            
            // Use userId as partition key
            var response = await container.CreateItemAsync(user, new PartitionKey(user.UserId));
            
            _logger.LogInformation($"User created: {user.Id} with email {user.Email}");
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error creating user: {ex.Message}");
            throw;
        }
    }

    public async Task<Models.User?> GetByIdAsync(string id)
    {
        try
        {
            var container = await _context.GetUsersContainerAsync();
            var response = await container.ReadItemAsync<Models.User>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error reading user {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<Models.User?> GetByEmailAsync(string email)
    {
        try
        {
            var container = await _context.GetUsersContainerAsync();
            
            var query = container.GetItemLinqQueryable<Models.User>()
                .Where(u => u.Email == email)
                .Take(1);
            
            var iterator = query.ToFeedIterator();
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
            
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error getting user by email {email}: {ex.Message}");
            throw;
        }
    }

    public async Task<Models.User> UpdateAsync(Models.User user)
    {
        try
        {
            var container = await _context.GetUsersContainerAsync();
            user.CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt;
            user.UserId = user.Id; // Ensure partition key is set
            
            var response = await container.ReplaceItemAsync(
                user,
                user.Id ?? throw new ArgumentNullException(nameof(user.Id)),
                new PartitionKey(user.Id)
            );
            
            _logger.LogInformation($"User updated: {user.Id}");
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error updating user {user.Id}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        try
        {
            var user = await GetByEmailAsync(email);
            return user != null;
        }
        catch
        {
            throw;
        }
    }
}
