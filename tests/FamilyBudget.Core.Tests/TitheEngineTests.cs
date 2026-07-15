using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class TitheEngineTests
{
    [Fact]
    public void ComputeMonth_OnlyTitheApplicableIncome_CountsTowardGrossTarget()
    {
        var obligation = TitheEngine.ComputeMonth(
            titheApplicableIncome: 1000m, nonTitheApplicableIncome: 500m, rate: 0.2m,
            fixedDonationsThisMonth: 0m, priorMonthSmallCharityTotal: 0m, year: 2026, month: 3);

        Assert.Equal(1000m, obligation.TitheApplicableIncome);
        Assert.Equal(500m, obligation.NonTitheApplicableIncome);
        Assert.Equal(200m, obligation.GrossTitheTarget); // non-tithe-applicable income excluded (FR-007)
        Assert.Equal(200m, obligation.NetTitheDue);
    }

    [Fact]
    public void ComputeMonth_FixedDonationsReduceNetDue()
    {
        var obligation = TitheEngine.ComputeMonth(
            titheApplicableIncome: 1000m, nonTitheApplicableIncome: 0m, rate: 0.2m,
            fixedDonationsThisMonth: 120m, priorMonthSmallCharityTotal: 0m, year: 2026, month: 4);

        Assert.Equal(200m, obligation.GrossTitheTarget);
        Assert.Equal(120m, obligation.FixedDonationsThisMonth);
        Assert.Equal(80m, obligation.NetTitheDue);
    }

    [Fact]
    public void ComputeMonth_PriorMonthSmallCharityReducesNetDue()
    {
        var obligation = TitheEngine.ComputeMonth(
            titheApplicableIncome: 1000m, nonTitheApplicableIncome: 0m, rate: 0.2m,
            fixedDonationsThisMonth: 0m, priorMonthSmallCharityTotal: 50m, year: 2026, month: 4);

        Assert.Equal(200m, obligation.GrossTitheTarget);
        Assert.Equal(50m, obligation.PriorMonthSmallCharityTotal);
        Assert.Equal(150m, obligation.NetTitheDue);
    }

    [Fact]
    public void ComputeMonth_DeductionsExceedTarget_FloorsNetDueAtZero()
    {
        var obligation = TitheEngine.ComputeMonth(
            titheApplicableIncome: 1000m, nonTitheApplicableIncome: 0m, rate: 0.2m,
            fixedDonationsThisMonth: 150m, priorMonthSmallCharityTotal: 100m, year: 2026, month: 5);

        Assert.Equal(200m, obligation.GrossTitheTarget);
        Assert.Equal(0m, obligation.NetTitheDue); // 200 - 150 - 100 would be negative, floors at 0
    }

    [Fact]
    public void ComputeMonth_StillToDonateAfterFixed_DoesNotSubtractPriorMonthOffset()
    {
        // Per explicit user direction, "still to donate" (bullet ד) is gross minus fixed
        // donations only — it deliberately does NOT net out the prior-month small-charity amount,
        // unlike the fully protected NetTitheDue.
        var obligation = TitheEngine.ComputeMonth(
            titheApplicableIncome: 1000m, nonTitheApplicableIncome: 0m, rate: 0.2m,
            fixedDonationsThisMonth: 50m, priorMonthSmallCharityTotal: 100m, year: 2026, month: 6);

        Assert.Equal(200m, obligation.GrossTitheTarget);
        Assert.Equal(150m, obligation.StillToDonateAfterFixed); // 200 - 50, ignoring the 100 prior-month offset
        Assert.Equal(50m, obligation.NetTitheDue); // 200 - 50 - 100, the fully protected figure
    }

    [Fact]
    public void ComputeMonth_NoIncome_ReturnsZeroTarget()
    {
        var obligation = TitheEngine.ComputeMonth(
            titheApplicableIncome: 0m, nonTitheApplicableIncome: 0m, rate: 0.2m,
            fixedDonationsThisMonth: 0m, priorMonthSmallCharityTotal: 0m, year: 2026, month: 1);

        Assert.Equal(0m, obligation.GrossTitheTarget);
        Assert.Equal(0m, obligation.NetTitheDue);
        Assert.Equal(0m, obligation.StillToDonateAfterFixed);
    }

    [Theory]
    [InlineData(2026, 7, 2026, 6)]
    [InlineData(2026, 1, 2025, 12)]
    public void PreviousMonth_HandlesYearRollover(int year, int month, int expectedYear, int expectedMonth)
    {
        var (priorYear, priorMonth) = TitheEngine.PreviousMonth(year, month);

        Assert.Equal(expectedYear, priorYear);
        Assert.Equal(expectedMonth, priorMonth);
    }
}
