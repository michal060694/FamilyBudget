using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class BudgetSmoothingEngineTests
{
    private readonly BudgetSmoothingEngine _engine = new(new CalendarYearCycle());

    [Fact]
    public void CalculateAllocation_OctoberToDecemberScenario_SplitsRemainingAmountAcrossThreeMonths()
    {
        // December holiday item; current month is October: 3 months remain (Oct, Nov, Dec).
        var item = new AnnualBudgetItem(
            id: Guid.NewGuid(),
            year: 2026,
            name: "December Holidays",
            totalAmount: 4000m,
            targetMonth: 12,
            amountAlreadySetAside: 1000m);

        var allocation = _engine.CalculateAllocation(item, currentMonth: 10);

        Assert.Equal(1000m, allocation);
    }

    [Fact]
    public void CalculateAllocation_GeneralItem_UsesMonthsRemainingToDecember()
    {
        var item = new AnnualBudgetItem(
            id: Guid.NewGuid(),
            year: 2026,
            name: "Car Test",
            totalAmount: 600m,
            targetMonth: null,
            amountAlreadySetAside: 0m);

        // From month 6, 7 months remain (6..12 inclusive).
        var allocation = _engine.CalculateAllocation(item, currentMonth: 6);

        Assert.Equal(600m / 7m, allocation);
    }
}
