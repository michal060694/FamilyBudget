using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IMonthEndItemRepository
{
    Task<IReadOnlyList<MonthEndItem>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task<MonthEndItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MonthEndItem item, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}