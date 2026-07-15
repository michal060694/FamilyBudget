using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IDebtRepository
{
    Task<IReadOnlyList<Debt>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Debt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Debt debt, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no debt with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity (e.g. via <see cref="Debt.RecordRepayment"/>).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
