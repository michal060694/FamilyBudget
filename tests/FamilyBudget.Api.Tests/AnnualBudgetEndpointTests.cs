using System.Net;
using System.Net.Http.Json;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyBudget.Api.Tests;

public class AnnualBudgetEndpointTests : IClassFixture<FamilyBudgetApiFactory>
{
    private readonly FamilyBudgetApiFactory _factory;

    public AnnualBudgetEndpointTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAnnualBudget_OrdersMonthMappedItemsFirst_GeneralItemsLast()
    {
        const int year = 2027;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.AnnualBudgetItems.AddRange(
                new AnnualBudgetItem(Guid.NewGuid(), year, "Car Test", 600m, null, 0m),
                new AnnualBudgetItem(Guid.NewGuid(), year, "Spring Trip", 2000m, 4, 0m),
                new AnnualBudgetItem(Guid.NewGuid(), year, "December Holidays", 4000m, 12, 0m, amountUsed: 4000m));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<AnnualBudgetResponse>(
            $"/api/annual-budget?year={year}");

        Assert.NotNull(response);
        Assert.Equal(new[] { "Spring Trip", "December Holidays", "Car Test" }, response!.Items.Select(i => i.Name));
        Assert.True(response.Items.Single(i => i.Name == "December Holidays").IsFullyUsed);
    }

    [Fact]
    public async Task GetAnnualBudget_ComputesMonthlySummary_FromPerItemRemainingBalancesAndMonthsRemaining()
    {
        const int year = 2028;
        var client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            // Item A: 30000 total, 10000 used -> 20000 remaining. Item B: 18000 total, 8000 used
            // -> 10000 remaining. NotYetCovered = 30000, independent of the reserve set below.
            db.AnnualBudgetItems.AddRange(
                new AnnualBudgetItem(Guid.NewGuid(), year, "Item A", 30000m, null, 0m, amountUsed: 10000m),
                new AnnualBudgetItem(Guid.NewGuid(), year, "Item B", 18000m, 6, 0m, amountUsed: 8000m));
            await db.SaveChangesAsync();
        }

        var setReserve = await client.PutAsJsonAsync($"/api/reserve?year={year}", new SetReserveRequest(12000m));
        Assert.Equal(HttpStatusCode.OK, setReserve.StatusCode);

        var response = await client.GetFromJsonAsync<AnnualBudgetResponse>($"/api/annual-budget?year={year}");

        // monthlyAllocation spreads (notYetCovered - reserveOnHand) over the deposit months (the
        // 1st of each month) still remaining to year-end, excluding the current month once its own
        // 1st has passed — mirror the same CalendarYearCycle math the endpoint uses.
        var now = DateTime.Now;
        var effectiveMonth = now.Day > 1 ? now.Month + 1 : now.Month;
        var monthsRemaining = Math.Max(1, new CalendarYearCycle().GetMonthsRemaining(effectiveMonth, targetMonth: null));
        var expectedMonthlyAllocation = (30000m - 12000m) / monthsRemaining;

        Assert.NotNull(response);
        Assert.Equal(12000m, response!.ReserveOnHand);
        Assert.Equal(48000m, response.TotalAnnualBudget);
        Assert.Equal(30000m, response.NotYetCovered);
        Assert.Equal(expectedMonthlyAllocation, response.MonthlyAllocation);
    }

    [Fact]
    public async Task CreateAnnualBudgetItem_PersistsAndAppearsInSubsequentGet()
    {
        const int year = 2029;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/annual-budget-items",
            new CreateAnnualBudgetItemRequest(year, "Clothing", 3600m, null));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AnnualBudgetItemView>();
        Assert.NotNull(created);
        Assert.Equal("Clothing", created!.Name);
        Assert.Equal(0m, created.AmountAlreadySetAside);

        var listResponse = await client.GetFromJsonAsync<AnnualBudgetResponse>($"/api/annual-budget?year={year}");
        Assert.Contains(listResponse!.Items, item => item.Name == "Clothing");
    }

    [Fact]
    public async Task CreateAnnualBudgetItem_InvalidTargetMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/annual-budget-items",
            new CreateAnnualBudgetItemRequest(2030, "Bad Item", 100m, 13));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetUsage_OverwritesToExactValue_UpdatesAmountUsedAndIsFullyUsedFlag()
    {
        const int year = 2031;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/annual-budget-items",
            new CreateAnnualBudgetItemRequest(year, "Dry Cleaning", 300m, null));
        var created = await createResponse.Content.ReadFromJsonAsync<AnnualBudgetItemView>();

        var partial = await client.PatchAsJsonAsync(
            $"/api/annual-budget-items/{created!.AnnualBudgetItemId}/usage", new SetUsageRequest(100m));
        Assert.Equal(HttpStatusCode.OK, partial.StatusCode);
        var partialView = await partial.Content.ReadFromJsonAsync<AnnualBudgetItemView>();
        Assert.Equal(100m, partialView!.AmountUsed);
        Assert.False(partialView.IsFullyUsed);

        // Setting again overwrites the previous value rather than adding to it.
        var full = await client.PatchAsJsonAsync(
            $"/api/annual-budget-items/{created.AnnualBudgetItemId}/usage", new SetUsageRequest(300m));
        var fullView = await full.Content.ReadFromJsonAsync<AnnualBudgetItemView>();
        Assert.Equal(300m, fullView!.AmountUsed);
        Assert.True(fullView.IsFullyUsed);
    }

    [Fact]
    public async Task SetUsage_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/annual-budget-items/{Guid.NewGuid()}/usage", new SetUsageRequest(50m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetUsage_NegativeAmount_ReturnsBadRequest()
    {
        const int year = 2032;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/annual-budget-items",
            new CreateAnnualBudgetItemRequest(year, "Something", 100m, null));
        var created = await createResponse.Content.ReadFromJsonAsync<AnnualBudgetItemView>();

        var response = await client.PatchAsJsonAsync(
            $"/api/annual-budget-items/{created!.AnnualBudgetItemId}/usage", new SetUsageRequest(-1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetUsage_ZeroAmount_IsAllowed()
    {
        const int year = 2033;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/annual-budget-items",
            new CreateAnnualBudgetItemRequest(year, "Something Else", 100m, null));
        var created = await createResponse.Content.ReadFromJsonAsync<AnnualBudgetItemView>();

        var response = await client.PatchAsJsonAsync(
            $"/api/annual-budget-items/{created!.AnnualBudgetItemId}/usage", new SetUsageRequest(0m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = await response.Content.ReadFromJsonAsync<AnnualBudgetItemView>();
        Assert.Equal(0m, view!.AmountUsed);
    }

    [Fact]
    public async Task DeleteAnnualBudgetItem_RemovesItFromSubsequentGet()
    {
        const int year = 2034;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/annual-budget-items",
            new CreateAnnualBudgetItemRequest(year, "To Be Deleted", 100m, null));
        var created = await createResponse.Content.ReadFromJsonAsync<AnnualBudgetItemView>();

        var deleteResponse = await client.DeleteAsync($"/api/annual-budget-items/{created!.AnnualBudgetItemId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await client.GetFromJsonAsync<AnnualBudgetResponse>($"/api/annual-budget?year={year}");
        Assert.DoesNotContain(listResponse!.Items, item => item.AnnualBudgetItemId == created.AnnualBudgetItemId);
    }

    [Fact]
    public async Task DeleteAnnualBudgetItem_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/annual-budget-items/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
