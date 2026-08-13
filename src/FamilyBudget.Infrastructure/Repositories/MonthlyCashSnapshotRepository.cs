using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class MonthlyCashSnapshotRepository : IMonthlyCashSnapshotRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public MonthlyCashSnapshotRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MonthlyCashSnapshot> GetAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var snapshot = await _dbContext.MonthlyCashSnapshots
            .FirstOrDefaultAsync(s => s.Year == year && s.Month == month, cancellationToken);
        return snapshot ?? new MonthlyCashSnapshot(year, month);
    }

    public async Task SetCashInAccountAsync(int year, int month, decimal amount, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetOrCreateAsync(year, month, cancellationToken);
        snapshot.SetCashInAccount(amount);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetMoneyNotYetInAccountAsync(int year, int month, decimal amount, CancellationToken cancellationToken = default)
    {
        var snapshot = await GetOrCreateAsync(year, month, cancellationToken);
        snapshot.SetMoneyNotYetInAccount(amount);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MonthlyCashSnapshot> GetOrCreateAsync(int year, int month, CancellationToken cancellationToken)
    {
        var snapshot = await _dbContext.MonthlyCashSnapshots
            .FirstOrDefaultAsync(s => s.Year == year && s.Month == month, cancellationToken);

        if (snapshot is null)
        {
            snapshot = new MonthlyCashSnapshot(year, month);
            _dbContext.MonthlyCashSnapshots.Add(snapshot);
        }

        return snapshot;
    }
}