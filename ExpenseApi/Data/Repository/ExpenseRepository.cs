using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ExpenseApi.Models;

namespace ExpenseApi.Data.Repository;

/// <summary>
/// Implementation of IExpenseRepository for Cosmos DB.
/// </summary>
public class ExpenseRepository : IExpenseRepository
{
    private readonly CosmosDbContext _context;
    private readonly ILogger<ExpenseRepository> _logger;

    public ExpenseRepository(CosmosDbContext context, ILogger<ExpenseRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Expense> CreateAsync(Expense expense)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            expense.Id = Guid.NewGuid().ToString();
            expense.CreatedAt = DateTime.UtcNow;
            
            var response = await container.CreateItemAsync(
                expense,
                new PartitionKey(expense.UserId)
            );
            
            _logger.LogInformation($"Expense created: {expense.Id} for user {expense.UserId}");
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error creating expense: {ex.Message}");
            throw;
        }
    }

    public async Task<Expense?> GetByIdAsync(string id, string userId)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            var response = await container.ReadItemAsync<Expense>(
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
            _logger.LogError($"Error reading expense {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Expense>> GetByMonthYearAsync(string userId, int month, int year)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddSeconds(-1);
            
            var query = container.GetItemLinqQueryable<Expense>()
                .Where(e => e.UserId == userId 
                    && !e.IsDeleted 
                    && e.PurchaseDate >= startDate 
                    && e.PurchaseDate <= endDate)
                .OrderByDescending(e => e.PurchaseDate);
            
            var iterator = query.ToFeedIterator();
            var expenses = new List<Expense>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                expenses.AddRange(response.ToList());
            }
            
            return expenses;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error getting expenses for month {month}/{year}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Expense>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            
            var query = container.GetItemLinqQueryable<Expense>()
                .Where(e => e.UserId == userId 
                    && !e.IsDeleted 
                    && e.PurchaseDate >= startDate 
                    && e.PurchaseDate <= endDate)
                .OrderByDescending(e => e.PurchaseDate);
            
            var iterator = query.ToFeedIterator();
            var expenses = new List<Expense>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                expenses.AddRange(response.ToList());
            }
            
            return expenses;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error getting expenses in date range: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Expense>> GetByCategoryAsync(string userId, string category)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            
            var query = container.GetItemLinqQueryable<Expense>()
                .Where(e => e.UserId == userId 
                    && !e.IsDeleted 
                    && e.Category == category)
                .OrderByDescending(e => e.PurchaseDate);
            
            var iterator = query.ToFeedIterator();
            var expenses = new List<Expense>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                expenses.AddRange(response.ToList());
            }
            
            return expenses;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error getting expenses by category {category}: {ex.Message}");
            throw;
        }
    }

    public async Task<Expense> UpdateAsync(string id, string userId, Expense expense)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            expense.Id = id;
            expense.UserId = userId;
            expense.UpdatedAt = DateTime.UtcNow;
            
            var response = await container.ReplaceItemAsync(
                expense,
                id,
                new PartitionKey(userId)
            );
            
            _logger.LogInformation($"Expense updated: {id}");
            return response.Resource;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error updating expense {id}: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string id, string userId)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            
            // Soft delete - set IsDeleted flag
            var expense = await GetByIdAsync(id, userId)
                ?? throw new InvalidOperationException($"Expense {id} not found");
            
            expense.IsDeleted = true;
            expense.UpdatedAt = DateTime.UtcNow;
            
            await container.ReplaceItemAsync(
                expense,
                id,
                new PartitionKey(userId)
            );
            
            _logger.LogInformation($"Expense deleted: {id}");
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error deleting expense {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(string userId)
    {
        try
        {
            var container = await _context.GetExpensesContainerAsync();
            
            var query = container.GetItemLinqQueryable<Expense>()
                .Where(e => e.UserId == userId && !e.IsDeleted);
            
            var iterator = query.ToFeedIterator();
            var expenses = new List<Expense>();
            
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                expenses.AddRange(response.ToList());
            }
            
            // Group by month and sum amounts
            var summary = expenses
                .GroupBy(e => e.PurchaseDate.ToString("yyyy-MM"))
                .OrderByDescending(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(e => e.Amount)
                );
            
            return summary;
        }
        catch (CosmosException ex)
        {
            _logger.LogError($"Error getting monthly summary: {ex.Message}");
            throw;
        }
    }
}
