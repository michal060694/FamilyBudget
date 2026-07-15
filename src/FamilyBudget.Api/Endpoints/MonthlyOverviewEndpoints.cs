using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Endpoints;

public static class MonthlyOverviewEndpoints
{
    public static void MapMonthlyOverviewEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monthly-overview", GetMonthlyOverview);
        app.MapGet("/api/tithe-setting", GetTitheSetting);
        app.MapPut("/api/tithe-setting", SetTitheSetting);
    }

    private static TransactionResponse ToResponse(Transaction transaction) => new(
        transaction.Id,
        transaction.Date,
        transaction.Amount,
        transaction.AmountFormula,
        transaction.Type,
        transaction.PaymentMethod,
        transaction.IsTitheApplicable,
        transaction.Description);

    private static MonthlyExpenseBudgetItemView ToView(MonthlyExpenseBudgetItem item) => new(
        item.Id,
        item.Year,
        item.Month,
        item.Name,
        item.Type,
        item.BudgetedAmount,
        item.BudgetedAmountFormula,
        item.UsedAmount,
        item.UsedAmountFormula,
        item.Remaining);

    private static async Task<IResult> GetMonthlyOverview(int year, int month, MonthlyOverviewQueryService queryService)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var overview = await queryService.GetOverviewAsync(year, month);

        return Results.Ok(new MonthlyOverviewResponse(
            overview.Year,
            overview.Month,
            new IncomeSection(
                overview.TitheApplicableIncomeLines.Select(ToResponse).ToList(),
                overview.TitheObligation.TitheApplicableIncome),
            new IncomeSection(
                overview.NonTitheApplicableIncomeLines.Select(ToResponse).ToList(),
                overview.TitheObligation.NonTitheApplicableIncome),
            new TitheObligationView(
                overview.TitheObligation.GrossTitheTarget,
                overview.TitheObligation.FixedDonationsThisMonth,
                overview.TitheObligation.CreditCarriedIn,
                overview.TitheObligation.SmallCharityAppliedThisMonth,
                overview.TitheObligation.NetTitheDue),
            new SmallCharityOffsetLedgerView(
                overview.SmallCharityLedger.SmallCharityExpenseTotal,
                overview.SmallCharityLedger.AvailableFromPriorMonth,
                overview.SmallCharityLedger.AppliedThisMonth,
                overview.SmallCharityLedger.UnappliedRemainder),
            new DonationsSection(
                overview.DonationLines.Select(ToResponse).ToList(),
                overview.GivenThisMonth,
                overview.RemainingToGive),
            new ExpenseBudgetSection(overview.FixedExpenseItems.Select(ToView).ToList(), overview.FixedExpenseUsedTotal),
            new ExpenseBudgetSection(overview.RegularExpenseItems.Select(ToView).ToList(), overview.RegularExpenseUsedTotal),
            overview.DebtRepaymentsSummary,
            overview.TotalOutflow,
            overview.TotalIncome,
            overview.RemainingToSave));
    }

    private static async Task<IResult> GetTitheSetting(ITitheSettingRepository repository)
    {
        var rate = await repository.GetRateAsync();
        return Results.Ok(new TitheSettingResponse(rate));
    }

    private static async Task<IResult> SetTitheSetting(SetTitheRateRequest request, ITitheSettingRepository repository)
    {
        if (request.Rate <= 0 || request.Rate > 1)
        {
            return Results.BadRequest("rate must be strictly greater than 0 and at most 1.");
        }

        await repository.SetRateAsync(request.Rate);
        return Results.Ok(new TitheSettingResponse(request.Rate));
    }
}
