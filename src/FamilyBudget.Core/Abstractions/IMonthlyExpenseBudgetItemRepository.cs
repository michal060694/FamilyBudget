using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IMonthlyExpenseBudgetItemRepository
{
    Task<IReadOnlyList<MonthlyExpenseBudgetItem>> GetByMonthAsync(
        int year, int month, TransactionType? type = null, CancellationToken cancellationToken = default);

    Task<MonthlyExpenseBudgetItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MonthlyExpenseBudgetItem item, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no item with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
