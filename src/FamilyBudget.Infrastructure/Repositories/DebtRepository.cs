using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class DebtRepository : IDebtRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public DebtRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Debt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Debts.OrderBy(d => d.CounterpartyName).ToListAsync(cancellationToken);
    }

    public async Task<Debt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Debts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddAsync(Debt debt, CancellationToken cancellationToken = default)
    {
        _dbContext.Debts.Add(debt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var debt = await GetByIdAsync(id, cancellationToken);
        if (debt is null)
        {
            return false;
        }

        _dbContext.Debts.Remove(debt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
