using ExpenseApi.Controllers;
using ExpenseApi.Data.Repository;
using ExpenseApi.Models;
using ExpenseApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExpenseApi.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryRepository> _repo = new();
    private readonly Mock<ILogger<CategoriesController>> _logger = new();

    private CategoriesController CreateSut(string? userId = "user-123")
    {
        var controller = new CategoriesController(_repo.Object, _logger.Object);
        controller.ControllerContext = TestHelpers.MakeControllerContext(userId);
        return controller;
    }

    // --- GetAllCategories ---

    [Fact]
    public async Task GetAllCategories_ReturnsOkWithList_WhenUserAuthenticated()
    {
        var categories = new List<Category>
        {
            new() { Id = "1", Name = "Food", UserId = "user-123" },
            new() { Id = "2", Name = "Travel", UserId = "user-123" }
        };
        _repo.Setup(r => r.GetAllAsync("user-123")).ReturnsAsync(categories);

        var result = await CreateSut().GetAllCategories();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<List<Category>>(ok.Value);
        Assert.Equal(2, returned.Count);
    }

    [Fact]
    public async Task GetAllCategories_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).GetAllCategories();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetAllCategories_ReturnsBadRequest_WhenRepositoryThrows()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<string>()))
             .ThrowsAsync(new Exception("Cosmos unavailable"));

        var result = await CreateSut().GetAllCategories();

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // --- GetCategoryById ---

    [Fact]
    public async Task GetCategoryById_ReturnsOk_WhenFound()
    {
        var category = new Category { Id = "cat-1", Name = "Food", UserId = "user-123" };
        _repo.Setup(r => r.GetByIdAsync("cat-1", "user-123")).ReturnsAsync(category);

        var result = await CreateSut().GetCategoryById("cat-1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Category>(ok.Value);
        Assert.Equal("cat-1", returned.Id);
    }

    [Fact]
    public async Task GetCategoryById_ReturnsNotFound_WhenCategoryMissing()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", "user-123")).ReturnsAsync((Category?)null);

        var result = await CreateSut().GetCategoryById("missing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCategoryById_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).GetCategoryById("cat-1");

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // --- CreateCategory ---

    [Fact]
    public async Task CreateCategory_ReturnsCreated_WhenValid()
    {
        var created = new Category { Id = "new-1", Name = "Fitness", UserId = "user-123" };
        _repo.Setup(r => r.CreateAsync(It.IsAny<Category>())).ReturnsAsync(created);

        var result = await CreateSut().CreateCategory(new CreateCategoryRequest { Name = "Fitness" });

        var created201 = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<Category>(created201.Value);
        Assert.Equal("new-1", returned.Id);
    }

    [Fact]
    public async Task CreateCategory_ReturnsBadRequest_WhenNameEmpty()
    {
        var result = await CreateSut().CreateCategory(new CreateCategoryRequest { Name = "" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCategory_ReturnsBadRequest_WhenNameWhitespace()
    {
        var result = await CreateSut().CreateCategory(new CreateCategoryRequest { Name = "   " });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateCategory_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).CreateCategory(new CreateCategoryRequest { Name = "Fitness" });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    // --- DeleteCategory ---

    [Fact]
    public async Task DeleteCategory_ReturnsNoContent_WhenFound()
    {
        var category = new Category { Id = "cat-1", Name = "Food", UserId = "user-123" };
        _repo.Setup(r => r.GetByIdAsync("cat-1", "user-123")).ReturnsAsync(category);
        _repo.Setup(r => r.DeleteAsync("cat-1", "user-123")).Returns(Task.CompletedTask);

        var result = await CreateSut().DeleteCategory("cat-1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNotFound_WhenCategoryMissing()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", "user-123")).ReturnsAsync((Category?)null);

        var result = await CreateSut().DeleteCategory("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).DeleteCategory("cat-1");

        Assert.IsType<UnauthorizedResult>(result);
    }
}
