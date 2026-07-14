using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IAnnualBudgetItemRepository
{
    Task<IReadOnlyList<AnnualBudgetItem>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    Task AddAsync(AnnualBudgetItem item, CancellationToken cancellationToken = default);
}
