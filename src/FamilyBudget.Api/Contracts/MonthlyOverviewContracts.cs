namespace FamilyBudget.Api.Contracts;

public record MonthlyOverviewResponse(
    int Year,
    int Month,
    IncomeSection TitheApplicableIncome,
    IncomeSection NonTitheApplicableIncome,
    TitheObligationView TitheObligation,
    ExpenseBudgetSection FixedExpenses,
    ExpenseBudgetSection RegularExpenses,
    AnnualWithdrawalSection AnnualWithdrawals,
    decimal DebtRepaymentsSummary,
    decimal TotalOutflow,
    decimal TotalIncome,
    decimal RemainingToSave);

public record IncomeSection(IReadOnlyList<TransactionResponse> Lines, decimal Subtotal);

public record ExpenseBudgetSection(IReadOnlyList<MonthlyExpenseBudgetItemView> Items, decimal UsedTotal);

/// <summary>
/// The redesigned "מעשרות" (tithes) card: four figures — (א) gross target, (ב) this month's fixed
/// donation standing orders with drill-down, (ג) prior month's ad-hoc/small-charity donations with
/// drill-down, and (ד) the unprotected "still to donate after fixed donations" figure that
/// deliberately does not net out (ג) — see spec.md Assumptions. <c>NetTitheDue</c> is the fully
/// protected obligation (nets out both ב and ג) used internally/by the Dashboard.
/// </summary>
public record TitheObligationView(
    decimal GrossTitheTarget,
    FixedDonationsSection FixedDonationsThisMonth,
    PriorMonthDonationsSection PriorMonthSmallCharity,
    decimal StillToDonateAfterFixed,
    decimal NetTitheDue);

public record FixedDonationsSection(IReadOnlyList<FixedDonationStandingOrderView> Items, decimal Total);

public record PriorMonthDonationsSection(IReadOnlyList<TransactionResponse> Lines, decimal Total);

public record AnnualWithdrawalLine(Guid AnnualBudgetItemId, string Name, decimal TotalAmount);

public record AnnualWithdrawalSection(IReadOnlyList<AnnualWithdrawalLine> Lines, decimal Total);

public record TitheSettingResponse(decimal Rate);

public record SetTitheRateRequest(decimal Rate);
