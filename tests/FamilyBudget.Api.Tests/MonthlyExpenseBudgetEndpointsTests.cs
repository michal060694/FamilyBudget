using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Tests;

public class MonthlyExpenseBudgetEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public MonthlyExpenseBudgetEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAndGet_PersistsAndAppearsInMonthList()
    {
        const int year = 2060;
        const int month = 5;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-expense-budgets",
            new CreateMonthlyExpenseBudgetItemRequest(year, month, "Electricity", TransactionType.FixedExpense, 300m));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyExpenseBudgetItemView>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(0m, created!.UsedAmount);
        Assert.Equal(300m, created.Remaining);

        var list = await client.GetFromJsonAsync<MonthlyExpenseBudgetListResponse>(
            $"/api/monthly-expense-budgets?year={year}&month={month}", JsonOptions);
        Assert.Contains(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task Create_InvalidType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/monthly-expense-budgets",
            new CreateMonthlyExpenseBudgetItemRequest(2061, 6, "Salary?", TransactionType.Income, 100m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesNameBudgetedAndUsedAmount()
    {
        const int year = 2062;
        const int month = 8;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-expense-budgets",
            new CreateMonthlyExpenseBudgetItemRequest(year, month, "Groceries", TransactionType.RegularExpense, 600m));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyExpenseBudgetItemView>(JsonOptions);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/monthly-expense-budgets/{created!.Id}",
            new UpdateMonthlyExpenseBudgetItemRequest("Groceries & Household", 600m, 400m, UsedAmountFormula: "600-200"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MonthlyExpenseBudgetItemView>(JsonOptions);
        Assert.Equal("Groceries & Household", updated!.Name);
        Assert.Equal(400m, updated.UsedAmount);
        Assert.Equal("600-200", updated.UsedAmountFormula);
        Assert.Equal(200m, updated.Remaining);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/monthly-expense-budgets/{Guid.NewGuid()}",
            new UpdateMonthlyExpenseBudgetItemRequest("X", 100m, 0m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesItFromSubsequentGet()
    {
        const int year = 2063;
        const int month = 9;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-expense-budgets",
            new CreateMonthlyExpenseBudgetItemRequest(year, month, "Internet", TransactionType.FixedExpense, 100m));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyExpenseBudgetItemView>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/monthly-expense-budgets/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<MonthlyExpenseBudgetListResponse>(
            $"/api/monthly-expense-budgets?year={year}&month={month}", JsonOptions);
        Assert.DoesNotContain(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/monthly-expense-budgets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
