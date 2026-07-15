using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

/// <summary>
/// The generic excess-deduction credit carry-forward (FR-009 edge case): when a month's
/// deductions exceed its gross tithe target, the excess reduces the following month's gross
/// target before that month's own deductions apply.
/// </summary>
public class TitheCreditCarryForwardTests
{
    private static Transaction Income(int year, int month, decimal amount) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 15), amount, TransactionType.Income, PaymentMethod.BankTransfer, isTitheApplicable: true);

    private static Transaction FixedDonation(int year, int month, decimal amount) =>
        new(Guid.NewGuid(), new DateOnly(year, month, 5), amount, TransactionType.FixedDonation, PaymentMethod.BankTransfer, null);

    [Fact]
    public void ComputeMonth_ExcessDeduction_CarriesForwardAsCreditReducingNextMonth()
    {
        // Month 1: gross target 200 (1000 * 0.2), fixed donation 300 -> net due 0, credit out 100.
        // Month 2: gross target 100 (500 * 0.2), no new donations -> credit-in of 100 wipes it out to 0.
        var transactions = new List<Transaction>
        {
            Income(2026, 9, 1000m),
            FixedDonation(2026, 9, 300m),
            Income(2026, 10, 500m),
        };

        var (month2Obligation, _) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 10);

        Assert.Equal(100m, month2Obligation.GrossTitheTarget);
        Assert.Equal(100m, month2Obligation.CreditCarriedIn);
        Assert.Equal(0m, month2Obligation.NetTitheDue);
        Assert.Equal(0m, month2Obligation.CreditCarriedOut); // fully absorbed, nothing left over
    }

    [Fact]
    public void ComputeMonth_CreditLargerThanNextMonthsTarget_PartiallyPersistsToThirdMonth()
    {
        // Month 1: gross target 200, donation 500 -> net 0, credit out 300.
        // Month 2: gross target 50 (250 * 0.2) -> fully absorbed by credit, credit out 250 remains.
        // Month 3: gross target 100 -> credit-in 250 still exceeds it, net due 0, credit out 150.
        var transactions = new List<Transaction>
        {
            Income(2026, 1, 1000m),
            FixedDonation(2026, 1, 500m),
            Income(2026, 2, 250m),
            Income(2026, 3, 500m),
        };

        var (month3Obligation, _) = TitheEngine.ComputeMonth(transactions, rate: 0.2m, targetYear: 2026, targetMonth: 3);

        Assert.Equal(100m, month3Obligation.GrossTitheTarget);
        Assert.Equal(250m, month3Obligation.CreditCarriedIn);
        Assert.Equal(0m, month3Obligation.NetTitheDue);
        Assert.Equal(150m, month3Obligation.CreditCarriedOut); // 250 - 100
    }
}
