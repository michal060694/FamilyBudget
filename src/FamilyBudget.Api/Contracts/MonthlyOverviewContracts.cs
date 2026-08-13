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
    decimal RemainingToSave,
    decimal CashInAccount,
    decimal MoneyNotYetInAccount,
    decimal ExpectedAccountBalance);

public record IncomeSection(IReadOnlyList<TransactionResponse> Lines, decimal Subtotal);

public record ExpenseBudgetSection(IReadOnlyList<MonthlyExpenseBudgetItemView> Items, decimal UsedTotal);

/// <summary>
/// The redesigned "מעשרות" (tithes) card: four figures — (א) gross target, (ב) this month's fixed
/// donation standing orders with drill-down, (ג) prior month's ad-hoc/small-charity donations with
/// drill-down, and (ד) the amount still to donate, which is the protected <c>NetTitheDue</c> (nets
/// out both ב and ג). <c>StillToDonateAfterFixed</c> is retained only as an intermediate,
/// unprotected figure (nets out ב only) — it must not be shown as (ד) or treated as the final
/// obligation anywhere.
/// </summary>
public record TitheObligationView(
    decimal GrossTitheTarget,
    FixedDonationsSection FixedDonationsThisMonth,
    PriorMonthDonationsSection PriorMonthSmallCharity,
    decimal StillToDonateAfterFixed,
    decimal NetTitheDue);

public record FixedDonationsSection(IReadOnlyList<FixedDonationStandingOrderView> Items, decimal Total);

public record PriorMonthDonationsSection(IReadOnlyList<TransactionResponse> Lines, decimal Total);

public record AnnualWithdrawalSection(IReadOnlyList<TransactionResponse> Lines, decimal Total);

public record TitheSettingResponse(decimal Rate);

public record SetTitheRateRequest(decimal Rate);

public record SetCashInAccountRequest(decimal Amount);

public record SetMoneyNotYetInAccountRequest(decimal Amount);
