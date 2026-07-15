using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>Assembles the Monthly Overview screen (User Story 2; FR-014–FR-018).</summary>
public class MonthlyOverviewQueryService
{
    /// <summary>Placeholder until a future Debts Ledger feature exists (FR-017).</summary>
    public const decimal DebtRepaymentsSummaryPlaceholder = 0m;

    private readonly ITransactionRepository _transactionRepository;
    private readonly IMonthlyExpenseBudgetItemRepository _monthlyExpenseBudgetItemRepository;
    private readonly TitheEngine _titheEngine;

    public MonthlyOverviewQueryService(
        ITransactionRepository transactionRepository,
        IMonthlyExpenseBudgetItemRepository monthlyExpenseBudgetItemRepository,
        TitheEngine titheEngine)
    {
        _transactionRepository = transactionRepository;
        _monthlyExpenseBudgetItemRepository = monthlyExpenseBudgetItemRepository;
        _titheEngine = titheEngine;
    }

    public async Task<MonthlyOverview> GetOverviewAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var (obligation, ledger) = await _titheEngine.ComputeMonthAsync(year, month, cancellationToken);
        var monthTransactions = await _transactionRepository.GetByMonthAsync(year, month, cancellationToken: cancellationToken);

        var titheApplicableIncomeLines = monthTransactions
            .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == true)
            .ToList();
        var nonTitheApplicableIncomeLines = monthTransactions
            .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == false)
            .ToList();
        var donationLines = monthTransactions
            .Where(t => t.Type is TransactionType.FixedDonation or TransactionType.SmallCharityExpense)
            .ToList();

        // Fixed/Regular expenses are tracked as budgeted-vs-used planning lines (like
        // AnnualBudgetItem, scoped to this month) rather than a raw transaction log — the user's
        // "how much I planned / how much I've used / what's left" request.
        var fixedExpenseItems = await _monthlyExpenseBudgetItemRepository.GetByMonthAsync(
            year, month, TransactionType.FixedExpense, cancellationToken);
        var regularExpenseItems = await _monthlyExpenseBudgetItemRepository.GetByMonthAsync(
            year, month, TransactionType.RegularExpense, cancellationToken);

        var givenThisMonth = donationLines.Sum(t => t.Amount);
        var remainingToGive = Math.Max(0m, obligation.NetTitheDue - givenThisMonth);

        var fixedExpenseUsedTotal = fixedExpenseItems.Sum(i => i.UsedAmount);
        var regularExpenseUsedTotal = regularExpenseItems.Sum(i => i.UsedAmount);

        var totalOutflow = givenThisMonth + fixedExpenseUsedTotal + regularExpenseUsedTotal + DebtRepaymentsSummaryPlaceholder;
        var totalIncome = obligation.TitheApplicableIncome + obligation.NonTitheApplicableIncome;
        var remainingToSave = totalIncome - totalOutflow;

        return new MonthlyOverview(
            year, month,
            titheApplicableIncomeLines, nonTitheApplicableIncomeLines,
            obligation, ledger,
            donationLines, givenThisMonth, remainingToGive,
            fixedExpenseItems, fixedExpenseUsedTotal,
            regularExpenseItems, regularExpenseUsedTotal,
            DebtRepaymentsSummaryPlaceholder, totalOutflow, totalIncome, remainingToSave);
    }
}

public record MonthlyOverview(
    int Year,
    int Month,
    IReadOnlyList<Transaction> TitheApplicableIncomeLines,
    IReadOnlyList<Transaction> NonTitheApplicableIncomeLines,
    MonthlyTitheObligation TitheObligation,
    SmallCharityOffsetLedger SmallCharityLedger,
    IReadOnlyList<Transaction> DonationLines,
    decimal GivenThisMonth,
    decimal RemainingToGive,
    IReadOnlyList<MonthlyExpenseBudgetItem> FixedExpenseItems,
    decimal FixedExpenseUsedTotal,
    IReadOnlyList<MonthlyExpenseBudgetItem> RegularExpenseItems,
    decimal RegularExpenseUsedTotal,
    decimal DebtRepaymentsSummary,
    decimal TotalOutflow,
    decimal TotalIncome,
    decimal RemainingToSave);
