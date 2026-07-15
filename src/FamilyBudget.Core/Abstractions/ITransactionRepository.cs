using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetByMonthAsync(
        int year,
        int month,
        TransactionType? type = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// All transactions dated on or before the end of the given calendar month, ordered
    /// chronologically. Used by <see cref="Services.TitheEngine"/>'s forward-walk carry-forward
    /// algorithm (research.md), which needs every month from the household's earliest transaction
    /// up to the target month — not just the target month itself.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetUpToMonthAsync(int year, int month, CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no transaction with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity (e.g. via <see cref="Transaction.Update"/>).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
