namespace FamilyBudget.Core.Services;

/// <summary>
/// Plain integer arithmetic over a fixed 12-month Gregorian year (see research.md — no calendar
/// library dependency is needed; a Gregorian year is always exactly 12 months).
/// </summary>
public class CalendarYearCycle
{
    public const int MonthsInYear = 12;

    public (int Year, int Month, int Day) GetCurrent()
    {
        var now = DateTime.Now;
        return (now.Year, now.Month, now.Day);
    }

    /// <summary>
    /// Number of calendar months remaining, counting the current month, per FR-002/FR-007:
    /// to <paramref name="targetMonth"/> if it has not yet passed this year, otherwise (or when
    /// no target month is given) to calendar year-end (December).
    /// </summary>
    public int GetMonthsRemaining(int currentMonth, int? targetMonth)
    {
        if (targetMonth is { } target && target >= currentMonth)
        {
            return target - currentMonth + 1;
        }

        return MonthsInYear - currentMonth + 1;
    }
}
