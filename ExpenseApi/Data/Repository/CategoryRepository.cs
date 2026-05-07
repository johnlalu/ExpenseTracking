using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ExpenseApi.Models;

namespace ExpenseApi.Data.Repository;

/// <summary>
/// Implementation of ICategoryRepository for Cosmos DB.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<CategoryRepository> _logger;

    public CategoryRepository(
        CosmosDbContext context, 
        ILogger<CategoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Category>> GetAllAsync(string userId)
    {
        try
        {
            var container = await _context.GetCategoriesContainerAsync();
            
            var query = container.GetItemLinqQueryable<Category>()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderBy(c => c.Name);
            
            var iterator = query.ToFeedIterator();
            var categories = new List<Category>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                categories.AddRange(response.ToList());
            }
            
            // Add default categories if none exist
            if (categories.Count == 0)
            {
                categories = await InitializeDefaultCategoriesAsync(userId);
            }
            
            return categories;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error getting categories for user {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<Category?> GetByIdAsync(string id, string userId)
    {
        try
        {
            var container = await _context.GetCategoriesContainerAsync();
            var response = await container.ReadItemAsync<Category>(
                id,
                new PartitionKey(userId)
            );
            
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error reading category {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<Category> CreateAsync(Category category)
    {
        try
        {
            var container = await _context.GetCategoriesContainerAsync();
            category.Id = Guid.NewGuid().ToString();
            category.CreatedAt = DateTime.UtcNow;
            category.IsDefault = false;
            
            var response = await container.CreateItemAsync(
                category,
                new PartitionKey(category.UserId)
            );
            
            _logger.LogInformation($"Category created: {category.Id} for user {category.UserId}");
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error creating category: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string id, string userId)
    {
        try
        {
            var container = await _context.GetCategoriesContainerAsync();
            
            // Soft delete - set IsDeleted flag
            var category = await GetByIdAsync(id, userId)
                ?? throw new InvalidOperationException($"Category {id} not found");
            
            category.IsDeleted = true;
            
            await container.ReplaceItemAsync(
                category,
                id,
                new PartitionKey(userId)
            );
            
            _logger.LogInformation($"Category deleted: {id}");
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error deleting category {id}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Initialize default categories for a user.
    /// </summary>
    private async Task<List<Category>> InitializeDefaultCategoriesAsync(string userId)
    {
        try
        {
            var container = await _context.GetCategoriesContainerAsync();
            var defaultCategories = AppConfig.DefaultCategories;
            
            var categories = new List<Category>();
            
            foreach (var categoryName in defaultCategories)
            {
                var category = new Category
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Name = categoryName,
                    IsDefault = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                
                var response = await container.CreateItemAsync(
                    category,
                    new PartitionKey(userId)
                );
                
                categories.Add(response.Resource);
            }
            
            _logger.LogInformation($"Default categories initialized for user {userId}");
            return categories;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error initializing default categories: {ex.Message}");
            throw;
        }
    }
}
