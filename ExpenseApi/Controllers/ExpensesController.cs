using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseApi.Data.Repository;
using ExpenseApi.Models;
using ExpenseApi.Models.Requests;
using ExpenseApi.Models.Responses;

namespace ExpenseApi.Controllers;

/// <summary>
/// Controller for managing expenses.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : BaseController
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        IExpenseRepository expenseRepository,
        ILogger<ExpensesController> logger)
    {
        _expenseRepository = expenseRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new expense.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Expense>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User ID not found in token",
                StatusCode = 401,
                LogId = HttpContext.TraceIdentifier
            });
        }

        try
        {
            var expense = new Expense
            {
                UserId = userId,
                Description = request.Description,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                Category = request.Category,
                PurchaseDate = request.PurchaseDate,
                ReceiptUrl = request.ReceiptUrl,
                Paid = request.Paid
            };

            var createdExpense = await _expenseRepository.CreateAsync(expense);
            _logger.LogInformation($"Expense created: {createdExpense.Id} for user {userId}");

            return CreatedAtAction(nameof(GetExpenseById), new { id = createdExpense.Id }, createdExpense);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating expense: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error creating expense",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get all expenses for the current user (optionally filtered by month/year).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseListResponse>> GetExpenses(
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            if (!month.HasValue || !year.HasValue)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Month and year parameters are required",
                    StatusCode = 400,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            if (month < 1 || month > 12)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid month (1-12)",
                    StatusCode = 400,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            var expenses = await _expenseRepository.GetByMonthYearAsync(userId, month.Value, year.Value);
            var response = new ExpenseListResponse
            {
                Items = expenses,
                TotalCount = expenses.Count,
                PageSize = expenses.Count
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving expenses: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving expenses",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get expense by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Expense>> GetExpenseById([FromRoute] string id)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var expense = await _expenseRepository.GetByIdAsync(id, userId);
            if (expense == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Expense not found",
                    StatusCode = 404,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving expense {id}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving expense",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get expenses for a specific month.
    /// </summary>
    [HttpGet("month/{month}/{year}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetExpensesByMonth([FromRoute] int month, [FromRoute] int year)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            if (month < 1 || month > 12)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Invalid month (1-12)",
                    StatusCode = 400,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            var expenses = await _expenseRepository.GetByMonthYearAsync(userId, month, year);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving expenses for {month}/{year}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving expenses",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get expenses in a date range.
    /// </summary>
    [HttpGet("daterange")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetExpensesByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Start date must be before end date",
                    StatusCode = 400,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            var expenses = await _expenseRepository.GetByDateRangeAsync(userId, startDate, endDate);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving expenses for date range: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving expenses",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get expenses by category.
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Expense>>> GetExpensesByCategory([FromRoute] string category)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var expenses = await _expenseRepository.GetByCategoryAsync(userId, category);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving expenses for category {category}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving expenses",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Update an expense.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Expense>> UpdateExpense(
        [FromRoute] string id,
        [FromBody] UpdateExpenseRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var existingExpense = await _expenseRepository.GetByIdAsync(id, userId);
            if (existingExpense == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Expense not found",
                    StatusCode = 404,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            existingExpense.Description = request.Description ?? existingExpense.Description;
            existingExpense.Amount = request.Amount > 0 ? request.Amount : existingExpense.Amount;
            existingExpense.Category = request.Category ?? existingExpense.Category;
            existingExpense.PurchaseDate = request.PurchaseDate > DateTime.MinValue ? request.PurchaseDate : existingExpense.PurchaseDate;
            existingExpense.ReceiptUrl = request.ReceiptUrl ?? existingExpense.ReceiptUrl;
            existingExpense.Paid = request.Paid;

            var updatedExpense = await _expenseRepository.UpdateAsync(id, userId, existingExpense);
            _logger.LogInformation($"Expense updated: {id}");

            return Ok(updatedExpense);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating expense {id}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error updating expense",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete an expense.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExpense([FromRoute] string id)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var existingExpense = await _expenseRepository.GetByIdAsync(id, userId);
            if (existingExpense == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Expense not found",
                    StatusCode = 404,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            await _expenseRepository.DeleteAsync(id, userId);
            _logger.LogInformation($"Expense deleted: {id}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting expense {id}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error deleting expense",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get monthly summary of expenses.
    /// </summary>
    [HttpGet("summary/monthly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetMonthlySummary()
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var summary = await _expenseRepository.GetMonthlySummaryAsync(userId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving monthly summary: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving summary",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }
}
