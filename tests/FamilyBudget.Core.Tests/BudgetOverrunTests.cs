using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class BudgetOverrunTests
{
    private readonly BudgetSmoothingEngine _engine = new(new CalendarYearCycle());

    [Fact]
    public void CalculateAllocation_OverrunItem_SpreadsExcessAcrossRemainingMonths()
    {
        // Fully deposited (AmountAlreadySetAside == TotalAmount) but AmountUsed exceeded the
        // target by 600 — FR-006: that excess must be spread across the remaining months.
        var item = new AnnualBudgetItem(
            id: Guid.NewGuid(),
            year: 2026,
            name: "December Holidays",
            totalAmount: 4000m,
            targetMonth: 12,
            amountAlreadySetAside: 4000m,
            amountUsed: 4600m);

        // From month 10 (October), 3 months remain (Oct, Nov, Dec).
        var allocation = _engine.CalculateAllocation(item, currentMonth: 10);

        Assert.Equal(200m, allocation); // 600 excess / 3 remaining months
    }

    [Fact]
    public void CalculateAllocation_PassedTargetMonthStillUnderfunded_TreatedAsSpreadOverRemainingMonths()
    {
        // April (target month 4) has already passed; current month is 6 and the item is still
        // short 900 — FR-007: treat like year-end smoothing for what's left.
        var item = new AnnualBudgetItem(
            id: Guid.NewGuid(),
            year: 2026,
            name: "Spring Trip",
            totalAmount: 2000m,
            targetMonth: 4,
            amountAlreadySetAside: 1100m);

        // From month 6, 7 months remain to December (6..12 inclusive).
        var allocation = _engine.CalculateAllocation(item, currentMonth: 6);

        Assert.Equal(900m / 7m, allocation);
    }
}
