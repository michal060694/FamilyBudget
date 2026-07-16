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
    public async Task GetMonthlyOverview_SplitsIncomeAndComputesTitheBreakdown()
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
                // prior month (month - 1) small-charity donation — eligible to offset this month's NetTitheDue
                new Transaction(Guid.NewGuid(), new DateOnly(year, month - 1, 20), 30m, TransactionType.SmallCharityExpense, PaymentMethod.Cash, null, "Extra charity"));
            db.FixedDonationStandingOrders.Add(
                new FixedDonationStandingOrder(Guid.NewGuid(), "Yeshiva", 120m, null, null));
            db.MonthlyExpenseBudgetItems.AddRange(
                new MonthlyExpenseBudgetItem(Guid.NewGuid(), year, month, "Rent", TransactionType.FixedExpense, budgetedAmount: 400m, usedAmount: 400m),
                new MonthlyExpenseBudgetItem(Guid.NewGuid(), year, month, "Groceries", TransactionType.RegularExpense, budgetedAmount: 200m, usedAmount: 150m));
            db.AnnualBudgetItems.Add(
                new AnnualBudgetItem(Guid.NewGuid(), year, "December-style Holiday", 900m, month, amountAlreadySetAside: 900m));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<MonthlyOverviewResponse>(
            $"/api/monthly-overview?year={year}&month={month}", JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(1000m, response!.TitheApplicableIncome.Subtotal);
        Assert.Equal(300m, response.NonTitheApplicableIncome.Subtotal);
        Assert.Equal(200m, response.TitheObligation.GrossTitheTarget); // 1000 * 0.2
        Assert.Equal(120m, response.TitheObligation.FixedDonationsThisMonth.Total);
        Assert.Equal("Yeshiva", Assert.Single(response.TitheObligation.FixedDonationsThisMonth.Items).Name);
        Assert.Equal(30m, response.TitheObligation.PriorMonthSmallCharity.Total);
        Assert.Equal(80m, response.TitheObligation.StillToDonateAfterFixed); // 200 - 120, ignoring the 30 prior-month offset
        Assert.Equal(50m, response.TitheObligation.NetTitheDue); // 200 - 120 - 30, fully protected
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

        var withdrawalLine = Assert.Single(response.AnnualWithdrawals.Lines);
        Assert.Equal("December-style Holiday", withdrawalLine.Name);
        Assert.Equal(900m, withdrawalLine.TotalAmount);
        Assert.Equal(900m, response.AnnualWithdrawals.Total);

        var totalIncome = 1000m + 300m;
        var totalOutflow = 120m + 400m + 150m + 900m; // fixed donations + fixed used + regular used + annual withdrawals (+0 debt)
        Assert.Equal(totalOutflow, response.TotalOutflow);
        Assert.Equal(totalIncome, response.TotalIncome);
        Assert.Equal(totalIncome - totalOutflow, response.RemainingToSave);
    }

    [Fact]
    public async Task GetMonthlyOverview_DebtRepaymentsSummary_SumsRepaymentRateOfOpenPayableDebts()
    {
        const int year = 2051;
        const int month = 4;

        // Debts (unlike transactions/annual items) aren't year-scoped, so they're visible to every
        // month's overview and to every other test sharing this fixture's database. Clean up after
        // this test so it doesn't leak into DebtRepaymentsSummary elsewhere.
        var debtIds = new List<Guid>();

        try
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();

                // Counts: open payable debt with a repayment rate.
                var payableWithRate = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 5000m, repaymentRate: 750m);

                // Excluded: payable debt with no repayment rate set (contributes 0, not an error).
                var payableNoRate = new Debt(Guid.NewGuid(), DebtDirection.Payable, "No Rate Set", 1000m);

                // Excluded: receivable debt (money owed *to* the household, not a household repayment).
                var receivable = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi Cohen", 2000m, repaymentRate: 300m);

                // Excluded: closed payable debt (fully repaid, no further monthly pace needed).
                var closedDebt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Paid Off Loan", 400m, repaymentRate: 100m);
                closedDebt.RecordRepayment(400m);

                db.Debts.AddRange(payableWithRate, payableNoRate, receivable, closedDebt);
                debtIds.AddRange(new[] { payableWithRate.Id, payableNoRate.Id, receivable.Id, closedDebt.Id });
                await db.SaveChangesAsync();
            }

            var client = _factory.CreateClient();
            var response = await client.GetFromJsonAsync<MonthlyOverviewResponse>(
                $"/api/monthly-overview?year={year}&month={month}", JsonOptions);

            Assert.NotNull(response);
            Assert.Equal(750m, response!.DebtRepaymentsSummary);
            Assert.Equal(750m, response.TotalOutflow); // no other outflow sources seeded for this month
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.Debts.RemoveRange(db.Debts.Where(d => debtIds.Contains(d.Id)));
            await db.SaveChangesAsync();
        }
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
