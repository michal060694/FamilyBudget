using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IMonthlyTemplateItemRepository
{
    Task<IReadOnlyList<MonthlyTemplateItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MonthlyTemplateItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(MonthlyTemplateItem item, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no item with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
