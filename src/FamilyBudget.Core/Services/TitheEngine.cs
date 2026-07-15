using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>The computed result for a given calendar month (spec.md Key Entities; FR-007–FR-009).</summary>
public record MonthlyTitheObligation(
    int Year,
    int Month,
    decimal TitheApplicableIncome,
    decimal NonTitheApplicableIncome,
    decimal TitheRate,
    decimal GrossTitheTarget,
    decimal FixedDonationsThisMonth,
    decimal CreditCarriedIn,
    decimal SmallCharityAppliedThisMonth,
    decimal NetTitheDue,
    decimal CreditCarriedOut)
{
    public static MonthlyTitheObligation Empty(int year, int month, decimal rate) =>
        new(year, month, 0m, 0m, rate, 0m, 0m, 0m, 0m, 0m, 0m);
}

/// <summary>Per-month small-charity offset tracking, distinct from the generic credit carry-forward above (FR-010; User Story 3).</summary>
public record SmallCharityOffsetLedger(
    int Year,
    int Month,
    decimal SmallCharityExpenseTotal,
    decimal AvailableFromPriorMonth,
    decimal AppliedThisMonth,
    decimal UnappliedRemainder)
{
    public static SmallCharityOffsetLedger Empty(int year, int month) => new(year, month, 0m, 0m, 0m, 0m);
}

/// <summary>
/// Computes the protected monthly tithe (chomesh) obligation via the forward-walk algorithm
/// documented in research.md: because a month's carried-in credit and small-charity remainder
/// are recursively defined in terms of every prior month, the calculation walks forward from the
/// household's earliest relevant transaction up to the requested month rather than looking only
/// one month back.
/// </summary>
public class TitheEngine
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITitheSettingRepository _titheSettingRepository;

    public TitheEngine(ITransactionRepository transactionRepository, ITitheSettingRepository titheSettingRepository)
    {
        _transactionRepository = transactionRepository;
        _titheSettingRepository = titheSettingRepository;
    }

    public async Task<(MonthlyTitheObligation Obligation, SmallCharityOffsetLedger Ledger)> ComputeMonthAsync(
        int year, int month, CancellationToken cancellationToken = default)
    {
        var rate = await _titheSettingRepository.GetRateAsync(cancellationToken);
        var transactions = await _transactionRepository.GetUpToMonthAsync(year, month, cancellationToken);
        return ComputeMonth(transactions, rate, year, month);
    }

    /// <summary>
    /// Pure calculation, independently testable without a repository or database.
    /// <paramref name="transactionsUpToAndIncludingTargetMonth"/> must contain every transaction
    /// dated on or before the end of <paramref name="targetYear"/>/<paramref name="targetMonth"/>.
    /// </summary>
    public static (MonthlyTitheObligation Obligation, SmallCharityOffsetLedger Ledger) ComputeMonth(
        IReadOnlyList<Transaction> transactionsUpToAndIncludingTargetMonth,
        decimal rate,
        int targetYear,
        int targetMonth)
    {
        var monthsWithData = transactionsUpToAndIncludingTargetMonth
            .Select(t => (t.Date.Year, t.Date.Month))
            .Distinct()
            .ToList();

        if (monthsWithData.Count == 0)
        {
            return (MonthlyTitheObligation.Empty(targetYear, targetMonth, rate), SmallCharityOffsetLedger.Empty(targetYear, targetMonth));
        }

        var earliest = monthsWithData.MinBy(m => MonthIndex(m.Year, m.Month));

        decimal creditCarriedIn = 0m;
        decimal unappliedSmallCharity = 0m;
        MonthlyTitheObligation obligation = MonthlyTitheObligation.Empty(targetYear, targetMonth, rate);
        SmallCharityOffsetLedger ledger = SmallCharityOffsetLedger.Empty(targetYear, targetMonth);

        foreach (var (year, month) in EnumerateMonths(earliest, (targetYear, targetMonth)))
        {
            var monthTransactions = transactionsUpToAndIncludingTargetMonth
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .ToList();

            var titheApplicableIncome = monthTransactions
                .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == true)
                .Sum(t => t.Amount);
            var nonTitheApplicableIncome = monthTransactions
                .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == false)
                .Sum(t => t.Amount);
            var fixedDonations = monthTransactions
                .Where(t => t.Type == TransactionType.FixedDonation)
                .Sum(t => t.Amount);
            var newSmallCharityExpense = monthTransactions
                .Where(t => t.Type == TransactionType.SmallCharityExpense)
                .Sum(t => t.Amount);

            var grossTarget = titheApplicableIncome * rate;

            // Per the edge case: this month's OWN small-charity expenses are not eligible to
            // offset this month's tithe — only the remainder carried in from the prior month is.
            var availableSmallCharity = unappliedSmallCharity;
            var remainingCapacity = Math.Max(0m, grossTarget - fixedDonations - creditCarriedIn);
            var appliedSmallCharity = Math.Min(availableSmallCharity, remainingCapacity);

            var netBeforeFloor = grossTarget - fixedDonations - creditCarriedIn - appliedSmallCharity;
            var netTitheDue = Math.Max(0m, netBeforeFloor);
            var creditCarriedOut = Math.Max(0m, -netBeforeFloor);

            var unappliedRemainderOut = (availableSmallCharity - appliedSmallCharity) + newSmallCharityExpense;

            obligation = new MonthlyTitheObligation(
                year, month,
                titheApplicableIncome, nonTitheApplicableIncome,
                rate, grossTarget, fixedDonations,
                creditCarriedIn, appliedSmallCharity, netTitheDue, creditCarriedOut);

            ledger = new SmallCharityOffsetLedger(
                year, month,
                newSmallCharityExpense, availableSmallCharity, appliedSmallCharity, unappliedRemainderOut);

            creditCarriedIn = creditCarriedOut;
            unappliedSmallCharity = unappliedRemainderOut;
        }

        return (obligation, ledger);
    }

    private static int MonthIndex(int year, int month) => (year * 12) + (month - 1);

    private static IEnumerable<(int Year, int Month)> EnumerateMonths((int Year, int Month) start, (int Year, int Month) end)
    {
        var cursor = MonthIndex(start.Year, start.Month);
        var endIndex = MonthIndex(end.Year, end.Month);

        for (var index = cursor; index <= endIndex; index++)
        {
            yield return (index / 12, (index % 12) + 1);
        }
    }
}
