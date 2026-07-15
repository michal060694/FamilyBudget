using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class TitheEngineTests
{
    private static Transaction Income(int year, int month, decimal amount, bool titheApplicable) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 15), amount, TransactionType.Income, PaymentMethod.BankTransfer, titheApplicable);

    private static Transaction FixedDonation(int year, int month, decimal amount) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 5), amount, TransactionType.FixedDonation, PaymentMethod.BankTransfer, null);

    private static Transaction SmallCharity(int year, int month, decimal amount) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 20), amount, TransactionType.SmallCharityExpense, PaymentMethod.Cash, null);

    [Fact]
    public void ComputeMonth_OnlyTitheApplicableIncome_CountsTowardGrossTarget()
    {
        var transactions = new List<Transaction>
        {
            Income(2026, 3, 1000m, titheApplicable: true),
            Income(2026, 3, 500m, titheApplicable: false),
        };

        var (obligation, _) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 3);

        Assert.Equal(1000m, obligation.TitheApplicableIncome);
        Assert.Equal(500m, obligation.NonTitheApplicableIncome);
        Assert.Equal(200m, obligation.GrossTitheTarget); // non-tithe-applicable income excluded (FR-007)
        Assert.Equal(200m, obligation.NetTitheDue);
    }

    [Fact]
    public void ComputeMonth_FixedDonationsReduceNetDue()
    {
        var transactions = new List<Transaction>
        {
            Income(2026, 4, 1000m, titheApplicable: true),
            FixedDonation(2026, 4, 120m),
        };

        var (obligation, _) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 4);

        Assert.Equal(200m, obligation.GrossTitheTarget);
        Assert.Equal(120m, obligation.FixedDonationsThisMonth);
        Assert.Equal(80m, obligation.NetTitheDue);
    }

    [Fact]
    public void ComputeMonth_DeductionsExceedTarget_FloorsNetDueAtZero()
    {
        var transactions = new List<Transaction>
        {
            Income(2026, 5, 1000m, titheApplicable: true),
            FixedDonation(2026, 5, 250m), // exceeds the 200 gross target
        };

        var (obligation, _) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 5);

        Assert.Equal(200m, obligation.GrossTitheTarget);
        Assert.Equal(0m, obligation.NetTitheDue);
        Assert.Equal(50m, obligation.CreditCarriedOut); // 250 - 200
    }

    [Fact]
    public void ComputeMonth_NoTransactionsYet_ReturnsEmptyObligation()
    {
        var (obligation, ledger) = TitheEngine.ComputeMonth(new List<Transaction>(), rate: 0.2m, targetYear: 2026, targetMonth: 1);

        Assert.Equal(0m, obligation.GrossTitheTarget);
        Assert.Equal(0m, obligation.NetTitheDue);
        Assert.Equal(0m, ledger.SmallCharityExpenseTotal);
    }

    [Fact]
    public void ComputeMonth_GapMonthWithNoTransactions_StillCarriesCreditThroughCorrectly()
    {
        // Month 1: overrun of 50 credit. Month 2: no transactions at all (gap). Month 3: income
        // generates a 100 target, which should be reduced by the still-outstanding 50 credit.
        var transactions = new List<Transaction>
        {
            Income(2026, 6, 1000m, titheApplicable: true), // gross target 200
            FixedDonation(2026, 6, 250m), // credit out = 50
            Income(2026, 8, 500m, titheApplicable: true), // gross target 100
        };

        var (obligation, _) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 8);

        Assert.Equal(100m, obligation.GrossTitheTarget);
        Assert.Equal(50m, obligation.CreditCarriedIn);
        Assert.Equal(50m, obligation.NetTitheDue); // 100 - 50 credit carried through the gap month
    }
}
