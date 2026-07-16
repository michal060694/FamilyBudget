using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Tests;

public class MonthlyTemplateEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public MonthlyTemplateEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTemplateItem_Income_PersistsAndAppearsInList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-template-items",
            new CreateMonthlyTemplateItemRequest(TransactionType.Income, "Salary Template", 6000m, IsTitheApplicable: true));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyTemplateItemView>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(6000m, created!.Amount);

        var list = await client.GetFromJsonAsync<MonthlyTemplateListResponse>("/api/monthly-template", JsonOptions);
        Assert.Contains(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task CreateTemplateItem_IncomeWithoutTitheFlag_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/monthly-template-items",
            new CreateMonthlyTemplateItemRequest(TransactionType.Income, "Salary", 6000m, IsTitheApplicable: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTemplateItem_UnsupportedType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/monthly-template-items",
            new CreateMonthlyTemplateItemRequest(TransactionType.FixedDonation, "X", 100m, IsTitheApplicable: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTemplateItem_ChangesAmount()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-template-items",
            new CreateMonthlyTemplateItemRequest(TransactionType.FixedExpense, "Arnona", 500m, IsTitheApplicable: null));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyTemplateItemView>(JsonOptions);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/monthly-template-items/{created!.Id}",
            new UpdateMonthlyTemplateItemRequest("Arnona", 550m, IsTitheApplicable: null));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MonthlyTemplateItemView>(JsonOptions);
        Assert.Equal(550m, updated!.Amount);
    }

    [Fact]
    public async Task DeleteTemplateItem_RemovesItFromList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-template-items",
            new CreateMonthlyTemplateItemRequest(TransactionType.RegularExpense, "To Delete", 100m, IsTitheApplicable: null));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyTemplateItemView>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/monthly-template-items/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<MonthlyTemplateListResponse>("/api/monthly-template", JsonOptions);
        Assert.DoesNotContain(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task ApplyTemplate_CreatesIncomeAndExpenseRecordsForTheMonth_AndIsIdempotent()
    {
        const int year = 2052;
        const int month = 5;
        var client = _factory.CreateClient();

        var incomeId = (await (await client.PostAsJsonAsync(
                "/api/monthly-template-items",
                new CreateMonthlyTemplateItemRequest(TransactionType.Income, "Apply Test Salary", 7000m, IsTitheApplicable: true)))
            .Content.ReadFromJsonAsync<MonthlyTemplateItemView>(JsonOptions))!.Id;
        var fixedId = (await (await client.PostAsJsonAsync(
                "/api/monthly-template-items",
                new CreateMonthlyTemplateItemRequest(TransactionType.FixedExpense, "Apply Test Arnona", 500m, IsTitheApplicable: null)))
            .Content.ReadFromJsonAsync<MonthlyTemplateItemView>(JsonOptions))!.Id;

        try
        {
            var firstApply = await client.PostAsync($"/api/monthly-template/apply?year={year}&month={month}", null);
            Assert.Equal(HttpStatusCode.OK, firstApply.StatusCode);
            var firstResult = await firstApply.Content.ReadFromJsonAsync<ApplyMonthlyTemplateResponse>(JsonOptions);
            Assert.Equal(2, firstResult!.ItemsApplied);
            Assert.Equal(0, firstResult.ItemsSkipped);

            var overview = await client.GetFromJsonAsync<MonthlyOverviewResponse>(
                $"/api/monthly-overview?year={year}&month={month}", JsonOptions);
            Assert.Contains(overview!.TitheApplicableIncome.Lines, l => l.Description == "Apply Test Salary" && l.Amount == 7000m);
            var fixedLine = Assert.Single(overview.FixedExpenses.Items, i => i.Name == "Apply Test Arnona");
            Assert.Equal(500m, fixedLine.BudgetedAmount);

            // Applying again for the same month must not duplicate the already-applied items.
            var secondApply = await client.PostAsync($"/api/monthly-template/apply?year={year}&month={month}", null);
            var secondResult = await secondApply.Content.ReadFromJsonAsync<ApplyMonthlyTemplateResponse>(JsonOptions);
            Assert.Equal(0, secondResult!.ItemsApplied);
            Assert.Equal(2, secondResult.ItemsSkipped);
        }
        finally
        {
            await client.DeleteAsync($"/api/monthly-template-items/{incomeId}");
            await client.DeleteAsync($"/api/monthly-template-items/{fixedId}");
        }
    }

    [Fact]
    public async Task ApplyTemplate_InvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/monthly-template/apply?year=2050&month=13", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
