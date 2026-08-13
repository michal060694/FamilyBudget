using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Abstractions;

public interface IMonthlyCashSnapshotRepository
{
    /// <summary>Returns a zeroed, unsaved snapshot if none has been entered yet for the month.</summary>
    Task<MonthlyCashSnapshot> GetAsync(int year, int month, CancellationToken cancellationToken = default);

    Task SetCashInAccountAsync(int year, int month, decimal amount, CancellationToken cancellationToken = default);

    Task SetMoneyNotYetInAccountAsync(int year, int month, decimal amount, CancellationToken cancellationToken = default);
}