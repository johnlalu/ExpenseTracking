using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseApi.Data.Repository;
using ExpenseApi.Models;
using ExpenseApi.Models.Responses;

namespace ExpenseApi.Controllers;

/// <summary>
/// Controller for managing expense categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : BaseController
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryRepository categoryRepository,
        ILogger<CategoriesController> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Category>>> GetAllCategories()
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var categories = await _categoryRepository.GetAllAsync(userId);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving categories: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving categories",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Get category by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Category>> GetCategoryById([FromRoute] string id)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, userId);
            if (category == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Category not found",
                    StatusCode = 404,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving category {id}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error retrieving category",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Create a new custom category.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] CreateCategoryRequest request)
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
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Category name is required",
                    StatusCode = 400,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            var category = new Category
            {
                UserId = userId,
                Name = request.Name.Trim(),
                IsDefault = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var createdCategory = await _categoryRepository.CreateAsync(category);
            _logger.LogInformation($"Category created: {createdCategory.Id} for user {userId}");

            return CreatedAtAction(nameof(GetCategoryById), new { id = createdCategory.Id }, createdCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating category: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error creating category",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// Delete a custom category.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteCategory([FromRoute] string id)
    {
        var userId = GetUserIdFromClaims();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var existingCategory = await _categoryRepository.GetByIdAsync(id, userId);
            if (existingCategory == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "Category not found",
                    StatusCode = 404,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            // Prevent deletion of default categories
            if (existingCategory.IsDefault)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Cannot delete default categories",
                    StatusCode = 400,
                    LogId = HttpContext.TraceIdentifier
                });
            }

            await _categoryRepository.DeleteAsync(id, userId);
            _logger.LogInformation($"Category deleted: {id}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting category {id}: {ex.Message}");
            return BadRequest(new ErrorResponse
            {
                Message = "Error deleting category",
                StatusCode = 400,
                LogId = HttpContext.TraceIdentifier
            });
        }
    }
}

/// <summary>
/// Request model for creating a category.
/// </summary>
public class CreateCategoryRequest
{
    /// <summary>
    /// Category name.
    /// </summary>
    public string? Name { get; set; }
}
