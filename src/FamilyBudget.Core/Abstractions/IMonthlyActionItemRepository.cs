using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IMonthlyActionItemRepository
{
    Task<IReadOnlyList<MonthlyActionItem>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<MonthlyActionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Items with a deadline strictly before <paramref name="asOfDate"/>, not yet completed, and not yet reminded about — no year/month restriction, since a leftover item from an earlier month must still surface.</summary>
    Task<IReadOnlyList<MonthlyActionItem>> GetOverdueUncompletedWithoutReminderAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);

    Task AddAsync(MonthlyActionItem item, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no item with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity (e.g. via <see cref="MonthlyActionItem.SetCompleted"/>).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
