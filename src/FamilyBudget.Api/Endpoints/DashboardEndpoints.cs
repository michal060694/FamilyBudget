using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard", GetDashboard);
    }

    private static async Task<IResult> GetDashboard(
        int year,
        int month,
        IAnnualBudgetItemRepository repository,
        BudgetSmoothingEngine engine,
        TitheEngine titheEngine,
        decimal projectedIncome = 0m,
        decimal fixedExpenses = 0m)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var items = await repository.GetByYearAsync(year);

        var lines = items
            .Select(item => new AllocationLine(
                item.Id,
                item.Name,
                item.TargetMonth,
                item.TotalAmount,
                item.AmountAlreadySetAside,
                item.AmountUsed,
                engine.CalculateAllocation(item, month)))
            .ToList();

        var totalRequiredAllocation = lines.Sum(line => line.AllocatedMonthly);
        var freeBalance = projectedIncome - fixedExpenses - totalRequiredAllocation;

        var (obligation, _) = await titheEngine.ComputeMonthAsync(year, month);
        var titheDue = new TitheDueView(
            obligation.GrossTitheTarget,
            obligation.FixedDonationsThisMonth + obligation.CreditCarriedIn,
            obligation.SmallCharityAppliedThisMonth,
            obligation.NetTitheDue);

        return Results.Ok(new DashboardResponse(
            year,
            month,
            projectedIncome,
            fixedExpenses,
            totalRequiredAllocation,
            freeBalance,
            lines,
            titheDue));
    }
}
