using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class BudgetSmoothingEdgeCaseTests
{
    private readonly BudgetSmoothingEngine _engine = new(new CalendarYearCycle());

    [Fact]
    public void CalculateAllocation_FullyFundedEarly_YieldsZero()
    {
        var item = new AnnualBudgetItem(
            id: Guid.NewGuid(),
            year: 2026,
            name: "Insurance",
            totalAmount: 1200m,
            targetMonth: null,
            amountAlreadySetAside: 1200m);

        var allocation = _engine.CalculateAllocation(item, currentMonth: 3);

        Assert.Equal(0m, allocation);
    }

    [Fact]
    public void CalculateAllocation_SingleMonthRemaining_AbsorbsFullRemainingBalance()
    {
        var item = new AnnualBudgetItem(
            id: Guid.NewGuid(),
            year: 2026,
            name: "December Holidays",
            totalAmount: 4000m,
            targetMonth: 12,
            amountAlreadySetAside: 3200m);

        // Month 12 (December) is the last month — 1 month remains.
        var allocation = _engine.CalculateAllocation(item, currentMonth: 12);

        Assert.Equal(800m, allocation);
    }
}
