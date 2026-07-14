using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class CalendarYearCycleTests
{
    private readonly CalendarYearCycle _cycle = new();

    [Fact]
    public void GetMonthsRemaining_GeneralItem_CountsToDecemberInclusive()
    {
        var remaining = _cycle.GetMonthsRemaining(currentMonth: 10, targetMonth: null);

        Assert.Equal(3, remaining); // October, November, December
    }

    [Fact]
    public void GetMonthsRemaining_TargetMonthNotYetReached_CountsInclusiveToTarget()
    {
        var remaining = _cycle.GetMonthsRemaining(currentMonth: 2, targetMonth: 7);

        Assert.Equal(6, remaining); // months 2..7 inclusive
    }

    [Fact]
    public void GetMonthsRemaining_TargetMonthAlreadyPassed_FallsBackToYearEnd()
    {
        var remaining = _cycle.GetMonthsRemaining(currentMonth: 9, targetMonth: 7);

        Assert.Equal(4, remaining); // months 9..12 inclusive (falls back to year-end)
    }

    [Fact]
    public void GetMonthsRemaining_December_SingleMonthRemains()
    {
        var remaining = _cycle.GetMonthsRemaining(currentMonth: 12, targetMonth: null);

        Assert.Equal(1, remaining);
    }
}
