using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>Provides the annual budget table view per FR-004: January-first, general items last.</summary>
public class AnnualBudgetQueryService
{
    private readonly IAnnualBudgetItemRepository _itemRepository;
    private readonly IAnnualReserveRepository _reserveRepository;
    private readonly CalendarYearCycle _calendarYearCycle;

    public AnnualBudgetQueryService(
        IAnnualBudgetItemRepository itemRepository,
        IAnnualReserveRepository reserveRepository,
        CalendarYearCycle calendarYearCycle)
    {
        _itemRepository = itemRepository;
        _reserveRepository = reserveRepository;
        _calendarYearCycle = calendarYearCycle;
    }

    public static IReadOnlyList<AnnualBudgetItem> OrderForDisplay(IEnumerable<AnnualBudgetItem> items) =>
        items.OrderBy(item => item.TargetMonth ?? int.MaxValue).ToList();

    public async Task<IReadOnlyList<AnnualBudgetItem>> GetOrderedForYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var items = await _itemRepository.GetByYearAsync(year, cancellationToken);
        return OrderForDisplay(items);
    }

    /// <summary>
    /// The flat, year-level overview: how much all annual items add up to (<c>totalAnnualBudget</c>),
    /// how much money the household still needs to find this year across every item
    /// (<c>notYetCovered</c>), and the monthly pace needed to deposit whatever <c>notYetCovered</c>
    /// isn't already sitting in the reserve.
    ///
    /// <c>notYetCovered</c> itself is a pure usage figure and is deliberately **not** netted against
    /// <c>reserveOnHand</c>: per item it's <c>|totalAmount - amountUsed|</c>, not a plain signed
    /// subtraction, so an item with budget left to spend still needs that remaining amount funded,
    /// while an *overrun* item (amountUsed &gt; totalAmount, e.g. a 250 budget with 800 already used)
    /// contributes its 550 overrun as extra need rather than as a credit that cancels some other
    /// item's unused balance.
    ///
    /// The reserve *does* matter for the monthly pace, though: if <c>reserveOnHand</c> already covers
    /// (or exceeds) <c>notYetCovered</c>, there is nothing left to deposit and the monthly figure is 0
    /// — the household only needs to keep saving for the gap between what's needed and what's already
    /// sitting in the reserve, spread over the deposit opportunities (the 1st of each month) still
    /// remaining this year. If <paramref name="currentDay"/> is past the 1st, the current month's own
    /// opportunity has already gone by and is excluded from that count.
    /// </summary>
    public async Task<AnnualBudgetSummary> GetSummaryAsync(int year, int currentMonth, int currentDay, CancellationToken cancellationToken = default)
    {
        var items = await _itemRepository.GetByYearAsync(year, cancellationToken);
        var reserveOnHand = await _reserveRepository.GetAmountAsync(year, cancellationToken);

        var totalAnnualBudget = items.Sum(item => item.TotalAmount);
        var notYetCovered = items.Sum(item => Math.Abs(item.TotalAmount - item.AmountUsed));
        var stillNeedToDeposit = Math.Max(0, notYetCovered - reserveOnHand);
        var effectiveMonth = currentDay > 1 ? currentMonth + 1 : currentMonth;
        var monthsRemaining = Math.Max(1, _calendarYearCycle.GetMonthsRemaining(effectiveMonth, targetMonth: null));
        var monthlyAllocation = stillNeedToDeposit / monthsRemaining;

        return new AnnualBudgetSummary(reserveOnHand, totalAnnualBudget, notYetCovered, monthlyAllocation);
    }
}

public record AnnualBudgetSummary(
    decimal ReserveOnHand,
    decimal TotalAnnualBudget,
    decimal NotYetCovered,
    decimal MonthlyAllocation);
