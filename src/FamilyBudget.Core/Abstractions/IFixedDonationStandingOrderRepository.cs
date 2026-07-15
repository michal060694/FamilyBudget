using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IFixedDonationStandingOrderRepository
{
    /// <summary>All standing orders still active during the given calendar month (FixedDonationStandingOrder.AppliesTo).</summary>
    Task<IReadOnlyList<FixedDonationStandingOrder>> GetActiveForMonthAsync(
        int year, int month, CancellationToken cancellationToken = default);

    Task<FixedDonationStandingOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(FixedDonationStandingOrder order, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no order with the given id exists.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists in-place mutations made to an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
