namespace FamilyBudget.Api.Contracts;

public record MonthlyOverviewResponse(
    int Year,
    int Month,
    IncomeSection TitheApplicableIncome,
    IncomeSection NonTitheApplicableIncome,
    TitheObligationView TitheObligation,
    SmallCharityOffsetLedgerView SmallCharityOffsetLedger,
    DonationsSection Donations,
    ExpenseBudgetSection FixedExpenses,
    ExpenseBudgetSection RegularExpenses,
    decimal DebtRepaymentsSummary,
    decimal TotalOutflow,
    decimal TotalIncome,
    decimal RemainingToSave);

public record IncomeSection(IReadOnlyList<TransactionResponse> Lines, decimal Subtotal);

public record ExpenseBudgetSection(IReadOnlyList<MonthlyExpenseBudgetItemView> Items, decimal UsedTotal);

public record DonationsSection(IReadOnlyList<TransactionResponse> Lines, decimal GivenThisMonth, decimal RemainingToGive);

public record TitheObligationView(
    decimal GrossTitheTarget,
    decimal FixedDonationsThisMonth,
    decimal CreditCarriedIn,
    decimal SmallCharityAppliedThisMonth,
    decimal NetTitheDue);

public record SmallCharityOffsetLedgerView(
    decimal SmallCharityExpenseTotal,
    decimal AvailableFromPriorMonth,
    decimal AppliedThisMonth,
    decimal UnappliedRemainder);

public record TitheSettingResponse(decimal Rate);

public record SetTitheRateRequest(decimal Rate);
