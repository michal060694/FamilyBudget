using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>
/// Computes the monthly smoothing allocation for an <see cref="AnnualBudgetItem"/> per FR-002,
/// FR-006, and FR-007. See data-model.md for the RemainingToDeposit/Overrun split rationale.
/// </summary>
public class BudgetSmoothingEngine
{
    private readonly CalendarYearCycle _calendarYearCycle;

    public BudgetSmoothingEngine(CalendarYearCycle calendarYearCycle)
    {
        _calendarYearCycle = calendarYearCycle;
    }

    public decimal CalculateAllocation(AnnualBudgetItem item, int currentMonth)
    {
        var requiredAmount = item.RemainingToDeposit + item.Overrun;
        if (requiredAmount <= 0)
        {
            return 0m;
        }

        var monthsRemaining = _calendarYearCycle.GetMonthsRemaining(currentMonth, item.TargetMonth);
        return requiredAmount / monthsRemaining;
    }
}
