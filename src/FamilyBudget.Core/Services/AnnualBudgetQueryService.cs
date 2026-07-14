using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>Provides the annual budget table view per FR-004: January-first, general items last.</summary>
public class AnnualBudgetQueryService
{
    private readonly IAnnualBudgetItemRepository _itemRepository;
    private readonly IAnnualReserveRepository _reserveRepository;

    public AnnualBudgetQueryService(IAnnualBudgetItemRepository itemRepository, IAnnualReserveRepository reserveRepository)
    {
        _itemRepository = itemRepository;
        _reserveRepository = reserveRepository;
    }

    public static IReadOnlyList<AnnualBudgetItem> OrderForDisplay(IEnumerable<AnnualBudgetItem> items) =>
        items.OrderBy(item => item.TargetMonth ?? int.MaxValue).ToList();

    public async Task<IReadOnlyList<AnnualBudgetItem>> GetOrderedForYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var items = await _itemRepository.GetByYearAsync(year, cancellationToken);
        return OrderForDisplay(items);
    }

    /// <summary>
    /// The flat, year-level overview: how much all annual items add up to, how much of that is
    /// not yet covered by money already on hand, and the flat monthly allocation (always divided
    /// by the fixed 12-month year, independent of any single item's own target month).
    /// </summary>
    public async Task<AnnualBudgetSummary> GetSummaryAsync(int year, CancellationToken cancellationToken = default)
    {
        var items = await _itemRepository.GetByYearAsync(year, cancellationToken);
        var reserveOnHand = await _reserveRepository.GetAmountAsync(year, cancellationToken);

        var totalAnnualBudget = items.Sum(item => item.TotalAmount);
        var notYetCovered = Math.Max(0, totalAnnualBudget - reserveOnHand);
        var monthlyAllocation = notYetCovered / CalendarYearCycle.MonthsInYear;

        return new AnnualBudgetSummary(reserveOnHand, totalAnnualBudget, notYetCovered, monthlyAllocation);
    }
}

public record AnnualBudgetSummary(
    decimal ReserveOnHand,
    decimal TotalAnnualBudget,
    decimal NotYetCovered,
    decimal MonthlyAllocation);
