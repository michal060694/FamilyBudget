using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IFundEarmarkRepository
{
    Task<IReadOnlyList<FundEarmark>> GetByFundIdAsync(Guid fundId, CancellationToken cancellationToken = default);

    Task<FundEarmark?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(FundEarmark earmark, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no earmark with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity (e.g. via <see cref="FundEarmark.Update"/>).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
