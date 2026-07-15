using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IFundRepository
{
    Task<IReadOnlyList<Fund>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Fund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Fund fund, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no fund with the given id exists. Cascades to its FundEarmark rows.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity (e.g. via <see cref="Fund.SetTotalBalance"/>).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
