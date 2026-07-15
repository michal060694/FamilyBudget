using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

/// <summary>
/// The small-charity offset carry-forward mechanism (FR-010; User Story 3), distinct from the
/// generic excess-deduction credit tested in TitheCreditCarryForwardTests.
/// </summary>
public class SmallCharityOffsetCarryForwardTests
{
    private static Transaction Income(int year, int month, decimal amount) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 15), amount, TransactionType.Income, PaymentMethod.BankTransfer, isTitheApplicable: true);

    private static Transaction SmallCharity(int year, int month, decimal amount) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 20), amount, TransactionType.SmallCharityExpense, PaymentMethod.Cash, null);

    [Fact]
    public void ComputeMonth_PriorMonthSmallCharityRemainder_OffsetsCurrentMonthTarget()
    {
        // Month 1: no income, just a small-charity expense of 30 — not eligible to offset month 1
        // itself (edge case), only eligible starting month 2.
        // Month 2: income yields a gross target of 100; the 30 carried in should reduce it to 70.
        var transactions = new List<Transaction>
        {
            SmallCharity(2026, 1, 30m),
            Income(2026, 2, 500m), // gross target 100 at rate 0.2
        };

        var (obligation, ledger) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 2);

        Assert.Equal(100m, obligation.GrossTitheTarget);
        Assert.Equal(30m, obligation.SmallCharityAppliedThisMonth);
        Assert.Equal(70m, obligation.NetTitheDue);
        Assert.Equal(30m, ledger.AvailableFromPriorMonth);
        Assert.Equal(30m, ledger.AppliedThisMonth);
        Assert.Equal(0m, ledger.UnappliedRemainder);
    }

    [Fact]
    public void ComputeMonth_SameMonthSmallCharityExpense_DoesNotOffsetItsOwnMonth()
    {
        // A small-charity expense recorded in the same month as the income it could theoretically
        // offset must NOT reduce that month's own tithe target (edge case) — it only becomes
        // eligible starting next month.
        var transactions = new List<Transaction>
        {
            Income(2026, 3, 500m), // gross target 100
            SmallCharity(2026, 3, 40m),
        };

        var (obligation, ledger) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 3);

        Assert.Equal(100m, obligation.NetTitheDue); // unaffected by this month's own small charity
        Assert.Equal(0m, obligation.SmallCharityAppliedThisMonth);
        Assert.Equal(40m, ledger.SmallCharityExpenseTotal);
        Assert.Equal(40m, ledger.UnappliedRemainder); // carries forward to next month instead
    }

    [Fact]
    public void ComputeMonth_SmallCharityLargerThanCapacity_PartiallyAppliesAndCarriesRemainderForward()
    {
        // Month 1: small-charity expense of 150.
        // Month 2: gross target only 40 (small income) -> only 40 of the 150 can be applied;
        // 110 remains and must carry forward again to month 3.
        // Month 3: gross target 200 -> the remaining 110 is applied there.
        var transactions = new List<Transaction>
        {
            SmallCharity(2026, 4, 150m),
            Income(2026, 5, 200m), // gross target 40
            Income(2026, 6, 1000m), // gross target 200
        };

        var (month2Obligation, month2Ledger) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 5);
        Assert.Equal(40m, month2Obligation.SmallCharityAppliedThisMonth);
        Assert.Equal(0m, month2Obligation.NetTitheDue);
        Assert.Equal(110m, month2Ledger.UnappliedRemainder);

        var (month3Obligation, month3Ledger) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 6);
        Assert.Equal(110m, month3Ledger.AvailableFromPriorMonth);
        Assert.Equal(110m, month3Obligation.SmallCharityAppliedThisMonth);
        Assert.Equal(90m, month3Obligation.NetTitheDue); // 200 - 110
        Assert.Equal(0m, month3Ledger.UnappliedRemainder);
    }

    [Fact]
    public void ComputeMonth_AcrossConsecutiveMonths_EveryAmountIsAppliedOrCarried_NeverLostOrDoubleCounted()
    {
        // SC-003: sum of (applied across all months) + (final unapplied remainder) must equal
        // the total small-charity expense ever recorded, for any sequence of months.
        var transactions = new List<Transaction>
        {
            SmallCharity(2026, 7, 25m),
            SmallCharity(2026, 8, 60m), // recorded same month as some income below
            Income(2026, 8, 100m),      // gross target 20; only the 25 from July is eligible here
            Income(2026, 9, 100m),      // gross target 20; some of the 60 becomes eligible here
            Income(2026, 10, 100m),     // gross target 20; remainder mops up here
        };

        decimal totalApplied = 0m;
        SmallCharityOffsetLedger? lastLedger = null;

        foreach (var month in new[] { 7, 8, 9, 10 })
        {
            var (obligation, ledger) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: month);
            totalApplied += obligation.SmallCharityAppliedThisMonth;
            lastLedger = ledger;
        }

        var totalSmallCharityRecorded = 25m + 60m;
        Assert.Equal(totalSmallCharityRecorded, totalApplied + lastLedger!.UnappliedRemainder);
    }
}
