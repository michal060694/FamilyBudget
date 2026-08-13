using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>Assembles the Monthly Overview screen (User Story 2; FR-014–FR-022).</summary>
public class MonthlyOverviewQueryService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMonthlyExpenseBudgetItemRepository _monthlyExpenseBudgetItemRepository;
    private readonly IFixedDonationStandingOrderRepository _standingOrderRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly IMonthlyCashSnapshotRepository _cashSnapshotRepository;
    private readonly TitheEngine _titheEngine;

    public MonthlyOverviewQueryService(
        ITransactionRepository transactionRepository,
        IMonthlyExpenseBudgetItemRepository monthlyExpenseBudgetItemRepository,
        IFixedDonationStandingOrderRepository standingOrderRepository,
        IDebtRepository debtRepository,
        IMonthlyCashSnapshotRepository cashSnapshotRepository,
        TitheEngine titheEngine)
    {
        _transactionRepository = transactionRepository;
        _monthlyExpenseBudgetItemRepository = monthlyExpenseBudgetItemRepository;
        _standingOrderRepository = standingOrderRepository;
        _debtRepository = debtRepository;
        _cashSnapshotRepository = cashSnapshotRepository;
        _titheEngine = titheEngine;
    }

    public async Task<MonthlyOverview> GetOverviewAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var obligation = await _titheEngine.ComputeMonthAsync(year, month, cancellationToken);
        var monthTransactions = await _transactionRepository.GetByMonthAsync(year, month, cancellationToken: cancellationToken);

        var titheApplicableIncomeLines = monthTransactions
            .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == true)
            .ToList();
        var nonTitheApplicableIncomeLines = monthTransactions
            .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == false)
            .ToList();

        var standingOrders = await _standingOrderRepository.GetActiveForMonthAsync(year, month, cancellationToken);

        var (priorYear, priorMonth) = TitheEngine.PreviousMonth(year, month);
        var priorMonthDonations = await _transactionRepository.GetByMonthAsync(
            priorYear, priorMonth, TransactionType.SmallCharityExpense, cancellationToken: cancellationToken);

        var fixedExpenseItems = await _monthlyExpenseBudgetItemRepository.GetByMonthAsync(
            year, month, TransactionType.FixedExpense, cancellationToken);
        var regularExpenseItems = await _monthlyExpenseBudgetItemRepository.GetByMonthAsync(
            year, month, TransactionType.RegularExpense, cancellationToken);

        var annualWithdrawalItems = await _transactionRepository.GetByMonthAsync(
            year, month, TransactionType.AnnualReserveWithdrawal, cancellationToken: cancellationToken);
        var annualWithdrawalsTotal = annualWithdrawalItems.Sum(i => i.Amount);

        var fixedExpenseUsedTotal = fixedExpenseItems.Sum(i => i.UsedAmount);
        var regularExpenseUsedTotal = regularExpenseItems.Sum(i => i.UsedAmount);

        // TotalOutflow is a forward-looking "how much do I need this month" figure, not a record of
        // money already moved — so every component is the planned/target amount, not the amount
        // actually used/paid so far: the full tithe target (not just standing orders already given),
        // budgeted expense amounts (not used-so-far), and the planned debt repayment pace.
        var fixedExpenseBudgetedTotal = fixedExpenseItems.Sum(i => i.BudgetedAmount);
        var regularExpenseBudgetedTotal = regularExpenseItems
            .Where(i => i.IncludeInOutflowTotal)
            .Sum(i => i.BudgetedAmount);

        // The household's planned monthly repayment pace for what it owes, not the actual amount
        // paid this month — matches how fixed/regular expenses show "מתוכנן" (planned), and a
        // debt already fully repaid (Closed) or with no rate set no longer needs a monthly pace.
        var allDebts = await _debtRepository.GetAllAsync(cancellationToken);
        var debtRepaymentsSummary = allDebts
            .Where(d => d.Direction == DebtDirection.Payable && d.Status == DebtStatus.Open)
            .Sum(d => d.RepaymentRate ?? 0m);

        // Net out last month's ad-hoc small-charity giving (bullet ג, "קיזוז שהועבר מחודש קודם") —
        // it already counts toward this month's tithe obligation (see NetTitheDue), so it shouldn't
        // also inflate how much still needs to go out this month.
        var totalOutflow = obligation.GrossTitheTarget + fixedExpenseBudgetedTotal + regularExpenseBudgetedTotal +
            annualWithdrawalsTotal + debtRepaymentsSummary - obligation.PriorMonthSmallCharityTotal;
        var totalIncome = obligation.TitheApplicableIncome + obligation.NonTitheApplicableIncome;
        var remainingToSave = totalIncome - totalOutflow;

        var cashSnapshot = await _cashSnapshotRepository.GetAsync(year, month, cancellationToken);
        var expectedAccountBalance = totalOutflow - cashSnapshot.MoneyNotYetInAccount;

        return new MonthlyOverview(
            year, month,
            titheApplicableIncomeLines, nonTitheApplicableIncomeLines,
            obligation,
            standingOrders,
            priorMonthDonations,
            fixedExpenseItems, fixedExpenseUsedTotal,
            regularExpenseItems, regularExpenseUsedTotal,
            annualWithdrawalItems, annualWithdrawalsTotal,
            debtRepaymentsSummary, totalOutflow, totalIncome, remainingToSave,
            cashSnapshot.CashInAccount, cashSnapshot.MoneyNotYetInAccount, expectedAccountBalance);
    }
}

public record MonthlyOverview(
    int Year,
    int Month,
    IReadOnlyList<Transaction> TitheApplicableIncomeLines,
    IReadOnlyList<Transaction> NonTitheApplicableIncomeLines,
    MonthlyTitheObligation TitheObligation,
    IReadOnlyList<FixedDonationStandingOrder> FixedDonationStandingOrders,
    IReadOnlyList<Transaction> PriorMonthSmallCharityDonations,
    IReadOnlyList<MonthlyExpenseBudgetItem> FixedExpenseItems,
    decimal FixedExpenseUsedTotal,
    IReadOnlyList<MonthlyExpenseBudgetItem> RegularExpenseItems,
    decimal RegularExpenseUsedTotal,
    IReadOnlyList<Transaction> AnnualWithdrawalItems,
    decimal AnnualWithdrawalsTotal,
    decimal DebtRepaymentsSummary,
    decimal TotalOutflow,
    decimal TotalIncome,
    decimal RemainingToSave,
    decimal CashInAccount,
    decimal MoneyNotYetInAccount,
    decimal ExpectedAccountBalance);
