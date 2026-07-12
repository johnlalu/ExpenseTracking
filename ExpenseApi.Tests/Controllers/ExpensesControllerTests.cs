using ExpenseApi.Controllers;
using ExpenseApi.Data.Repository;
using ExpenseApi.Models;
using ExpenseApi.Models.Requests;
using ExpenseApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExpenseApi.Tests.Controllers;

public class ExpensesControllerTests
{
    private readonly Mock<IExpenseRepository> _repo = new();
    private readonly Mock<ILogger<ExpensesController>> _logger = new();

    private ExpensesController CreateSut(string? userId = "user-123")
    {
        var controller = new ExpensesController(_repo.Object, _logger.Object);
        controller.ControllerContext = TestHelpers.MakeControllerContext(userId);
        return controller;
    }

    // --- GetExpenses ---

    [Fact]
    public async Task GetExpenses_ReturnsOk_WithMonthAndYear()
    {
        var expenses = new List<Expense>
        {
            new() { Id = "e1", UserId = "user-123", Amount = 50m, Currency = "USD" }
        };
        _repo.Setup(r => r.GetByMonthYearAsync("user-123", 6, 2026)).ReturnsAsync(expenses);

        var result = await CreateSut().GetExpenses(month: 6, year: 2026);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ExpenseListResponse>(ok.Value);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task GetExpenses_ReturnsBadRequest_WhenMonthMissing()
    {
        var result = await CreateSut().GetExpenses(month: null, year: 2026);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetExpenses_ReturnsBadRequest_WhenYearMissing()
    {
        var result = await CreateSut().GetExpenses(month: 6, year: null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task GetExpenses_ReturnsBadRequest_WhenMonthOutOfRange(int invalidMonth)
    {
        var result = await CreateSut().GetExpenses(month: invalidMonth, year: 2026);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetExpenses_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).GetExpenses(month: 6, year: 2026);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // --- GetExpenseById ---

    [Fact]
    public async Task GetExpenseById_ReturnsOk_WhenFound()
    {
        var expense = new Expense { Id = "e-1", UserId = "user-123", Amount = 25m };
        _repo.Setup(r => r.GetByIdAsync("e-1", "user-123")).ReturnsAsync(expense);

        var result = await CreateSut().GetExpenseById("e-1");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Expense>(ok.Value);
        Assert.Equal("e-1", returned.Id);
    }

    [Fact]
    public async Task GetExpenseById_ReturnsNotFound_WhenExpenseMissing()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", "user-123")).ReturnsAsync((Expense?)null);

        var result = await CreateSut().GetExpenseById("missing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetExpenseById_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).GetExpenseById("e-1");

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // --- CreateExpense ---

    [Fact]
    public async Task CreateExpense_ReturnsCreated_WhenValid()
    {
        var request = new CreateExpenseRequest
        {
            Description = "Lunch",
            Amount = 12.50m,
            Currency = "USD",
            Category = "Food",
            PurchaseDate = DateTime.UtcNow.AddDays(-1)
        };
        var created = new Expense { Id = "new-e1", UserId = "user-123", Amount = 12.50m };
        _repo.Setup(r => r.CreateAsync(It.IsAny<Expense>())).ReturnsAsync(created);

        var result = await CreateSut().CreateExpense(request);

        var created201 = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<Expense>(created201.Value);
        Assert.Equal("new-e1", returned.Id);
    }

    [Fact]
    public async Task CreateExpense_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var request = new CreateExpenseRequest { Amount = 10m, Currency = "USD" };

        var result = await CreateSut(userId: null).CreateExpense(request);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateExpense_ReturnsBadRequest_WhenRepositoryThrows()
    {
        _repo.Setup(r => r.CreateAsync(It.IsAny<Expense>()))
             .ThrowsAsync(new Exception("Cosmos error"));
        var request = new CreateExpenseRequest { Amount = 10m, Currency = "USD" };

        var result = await CreateSut().CreateExpense(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // --- UpdateExpense ---

    [Fact]
    public async Task UpdateExpense_ReturnsOk_WhenFound()
    {
        var existing = new Expense { Id = "e-1", UserId = "user-123", Amount = 10m, Currency = "USD" };
        var updated = new Expense { Id = "e-1", UserId = "user-123", Amount = 20m, Currency = "USD" };
        var request = new UpdateExpenseRequest { Amount = 20m };

        _repo.Setup(r => r.GetByIdAsync("e-1", "user-123")).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync("e-1", "user-123", It.IsAny<Expense>())).ReturnsAsync(updated);

        var result = await CreateSut().UpdateExpense("e-1", request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<Expense>(ok.Value);
        Assert.Equal(20m, returned.Amount);
    }

    [Fact]
    public async Task UpdateExpense_ReturnsNotFound_WhenExpenseMissing()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", "user-123")).ReturnsAsync((Expense?)null);

        var result = await CreateSut().UpdateExpense("missing", new UpdateExpenseRequest());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateExpense_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).UpdateExpense("e-1", new UpdateExpenseRequest());

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    // --- DeleteExpense ---

    [Fact]
    public async Task DeleteExpense_ReturnsNoContent_WhenFound()
    {
        var expense = new Expense { Id = "e-1", UserId = "user-123" };
        _repo.Setup(r => r.GetByIdAsync("e-1", "user-123")).ReturnsAsync(expense);
        _repo.Setup(r => r.DeleteAsync("e-1", "user-123")).Returns(Task.CompletedTask);

        var result = await CreateSut().DeleteExpense("e-1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteExpense_ReturnsNotFound_WhenExpenseMissing()
    {
        _repo.Setup(r => r.GetByIdAsync("missing", "user-123")).ReturnsAsync((Expense?)null);

        var result = await CreateSut().DeleteExpense("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteExpense_ReturnsUnauthorized_WhenNoUserClaim()
    {
        var result = await CreateSut(userId: null).DeleteExpense("e-1");

        Assert.IsType<UnauthorizedResult>(result);
    }

    // --- GetExpensesByDateRange ---

    [Fact]
    public async Task GetExpensesByDateRange_ReturnsBadRequest_WhenStartAfterEnd()
    {
        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddDays(-1);

        var result = await CreateSut().GetExpensesByDateRange(start, end);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetExpensesByDateRange_ReturnsOk_WhenValidRange()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        _repo.Setup(r => r.GetByDateRangeAsync("user-123", start, end))
             .ReturnsAsync(new List<Expense>());

        var result = await CreateSut().GetExpensesByDateRange(start, end);

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
