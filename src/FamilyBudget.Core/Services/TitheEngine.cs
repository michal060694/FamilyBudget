using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>
/// The computed result for a given calendar month (spec.md Key Entities; FR-007–FR-009).
/// Per explicit user direction, the protected tithe calculation looks back exactly one calendar
/// month (no further multi-month recursion) — see research.md's amendment note.
/// </summary>
public record MonthlyTitheObligation(
    int Year,
    int Month,
    decimal TitheApplicableIncome,
    decimal NonTitheApplicableIncome,
    decimal TitheRate,
    decimal GrossTitheTarget,
    decimal FixedDonationsThisMonth,
    decimal PriorMonthSmallCharityTotal,
    decimal NetTitheDue,
    decimal StillToDonateAfterFixed)
{
    public static MonthlyTitheObligation Empty(int year, int month, decimal rate) =>
        new(year, month, 0m, 0m, rate, 0m, 0m, 0m, 0m, 0m);
}

/// <summary>
/// Computes the protected monthly tithe (chomesh/maaser) obligation: this month's gross target
/// (tithe-applicable income × rate), less this month's active fixed-donation standing orders, less
/// the prior calendar month's small-charity/ad-hoc donations — floored at zero (FR-009). Also
/// exposes a separate, unprotected "still to donate after fixed donations" figure (bullet ד in the
/// client) that intentionally does NOT net out the prior-month amount, per explicit user direction
/// — see research.md.
/// </summary>
public class TitheEngine
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITitheSettingRepository _titheSettingRepository;
    private readonly IFixedDonationStandingOrderRepository _standingOrderRepository;

    public TitheEngine(
        ITransactionRepository transactionRepository,
        ITitheSettingRepository titheSettingRepository,
        IFixedDonationStandingOrderRepository standingOrderRepository)
    {
        _transactionRepository = transactionRepository;
        _titheSettingRepository = titheSettingRepository;
        _standingOrderRepository = standingOrderRepository;
    }

    public async Task<MonthlyTitheObligation> ComputeMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var rate = await _titheSettingRepository.GetRateAsync(cancellationToken);

        var monthTransactions = await _transactionRepository.GetByMonthAsync(year, month, cancellationToken: cancellationToken);
        var titheApplicableIncome = monthTransactions
            .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == true)
            .Sum(t => t.Amount);
        var nonTitheApplicableIncome = monthTransactions
            .Where(t => t.Type == TransactionType.Income && t.IsTitheApplicable == false)
            .Sum(t => t.Amount);

        var activeStandingOrders = await _standingOrderRepository.GetActiveForMonthAsync(year, month, cancellationToken);
        var fixedDonationsThisMonth = activeStandingOrders.Sum(o => o.Amount);

        var (priorYear, priorMonth) = PreviousMonth(year, month);
        var priorMonthDonations = await _transactionRepository.GetByMonthAsync(
            priorYear, priorMonth, TransactionType.SmallCharityExpense, cancellationToken: cancellationToken);
        var priorMonthSmallCharityTotal = priorMonthDonations.Sum(t => t.Amount);

        return ComputeMonth(titheApplicableIncome, nonTitheApplicableIncome, rate, fixedDonationsThisMonth, priorMonthSmallCharityTotal, year, month);
    }

    /// <summary>Pure calculation, independently testable without a repository or database.</summary>
    public static MonthlyTitheObligation ComputeMonth(
        decimal titheApplicableIncome,
        decimal nonTitheApplicableIncome,
        decimal rate,
        decimal fixedDonationsThisMonth,
        decimal priorMonthSmallCharityTotal,
        int year,
        int month)
    {
        var grossTarget = titheApplicableIncome * rate;
        var netTitheDue = Math.Max(0m, grossTarget - fixedDonationsThisMonth - priorMonthSmallCharityTotal);
        var stillToDonateAfterFixed = Math.Max(0m, grossTarget - fixedDonationsThisMonth);

        return new MonthlyTitheObligation(
            year, month,
            titheApplicableIncome, nonTitheApplicableIncome,
            rate, grossTarget, fixedDonationsThisMonth, priorMonthSmallCharityTotal,
            netTitheDue, stillToDonateAfterFixed);
    }

    public static (int Year, int Month) PreviousMonth(int year, int month) =>
        month == 1 ? (year - 1, 12) : (year, month - 1);
}
