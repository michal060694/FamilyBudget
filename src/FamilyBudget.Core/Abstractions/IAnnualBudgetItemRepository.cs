using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IAnnualBudgetItemRepository
{
    Task<IReadOnlyList<AnnualBudgetItem>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    Task<AnnualBudgetItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(AnnualBudgetItem item, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity (e.g. via <see cref="AnnualBudgetItem.RecordUsage"/>).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
