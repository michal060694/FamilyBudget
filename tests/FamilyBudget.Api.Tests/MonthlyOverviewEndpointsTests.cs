using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyBudget.Api.Tests;

public class MonthlyOverviewEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public MonthlyOverviewEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMonthlyOverview_SplitsIncomeAndComputesRemainingToGive()
    {
        const int year = 2050;
        const int month = 3;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.TitheSettings.Add(new TitheSetting(0.2m));
            db.Transactions.AddRange(
                new Transaction(Guid.NewGuid(), new DateOnly(year, month, 1), 1000m, TransactionType.Income, PaymentMethod.BankTransfer, true, "Salary"),
                new Transaction(Guid.NewGuid(), new DateOnly(year, month, 2), 300m, TransactionType.Income, PaymentMethod.Cash, false, "Gift"),
                new Transaction(Guid.NewGuid(), new DateOnly(year, month, 3), 120m, TransactionType.FixedDonation, PaymentMethod.BankTransfer, null, "Standing order"));
            db.MonthlyExpenseBudgetItems.AddRange(
                new MonthlyExpenseBudgetItem(Guid.NewGuid(), year, month, "Rent", TransactionType.FixedExpense, budgetedAmount: 400m, usedAmount: 400m),
                new MonthlyExpenseBudgetItem(Guid.NewGuid(), year, month, "Groceries", TransactionType.RegularExpense, budgetedAmount: 200m, usedAmount: 150m));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<MonthlyOverviewResponse>(
            $"/api/monthly-overview?year={year}&month={month}", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(1000m, response!.TitheApplicableIncome.Subtotal);
        Assert.Equal(300m, response.NonTitheApplicableIncome.Subtotal);
        Assert.Equal(200m, response.TitheObligation.GrossTitheTarget); // 1000 * 0.2
        Assert.Equal(80m, response.TitheObligation.NetTitheDue); // 200 - 120 fixed donation
        Assert.Equal(120m, response.Donations.GivenThisMonth);
        Assert.Equal(0m, response.Donations.RemainingToGive); // netTitheDue(80) - given(120) floors at 0
        Assert.Equal(0m, response.DebtRepaymentsSummary);

        var rentLine = Assert.Single(response.FixedExpenses.Items);
        Assert.Equal("Rent", rentLine.Name);
        Assert.Equal(400m, rentLine.BudgetedAmount);
        Assert.Equal(400m, rentLine.UsedAmount);
        Assert.Equal(0m, rentLine.Remaining);

        var groceriesLine = Assert.Single(response.RegularExpenses.Items);
        Assert.Equal(200m, groceriesLine.BudgetedAmount);
        Assert.Equal(150m, groceriesLine.UsedAmount);
        Assert.Equal(50m, groceriesLine.Remaining);

        var totalIncome = 1000m + 300m;
        var totalOutflow = 120m + 400m + 150m; // donations + fixed used + regular used (+0 debt)
        Assert.Equal(totalOutflow, response.TotalOutflow);
        Assert.Equal(totalIncome, response.TotalIncome);
        Assert.Equal(totalIncome - totalOutflow, response.RemainingToSave);
    }

    [Fact]
    public async Task GetMonthlyOverview_InvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/monthly-overview?year=2050&month=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TitheSetting_GetDefaultsToChomeshRate_AndCanBeUpdated()
    {
        var client = _factory.CreateClient();

        var initial = await client.GetFromJsonAsync<TitheSettingResponse>("/api/tithe-setting");
        Assert.Equal(0.2m, initial!.Rate);

        var updateResponse = await client.PutAsJsonAsync("/api/tithe-setting", new SetTitheRateRequest(0.1m));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await client.GetFromJsonAsync<TitheSettingResponse>("/api/tithe-setting");
        Assert.Equal(0.1m, updated!.Rate);

        // Restore default so other tests in this class (sharing the same factory/db) are unaffected.
        await client.PutAsJsonAsync("/api/tithe-setting", new SetTitheRateRequest(0.2m));
    }

    [Fact]
    public async Task TitheSetting_InvalidRate_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/tithe-setting", new SetTitheRateRequest(1.5m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
