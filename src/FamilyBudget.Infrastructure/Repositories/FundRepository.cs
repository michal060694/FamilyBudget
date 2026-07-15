using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class FundRepository : IFundRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public FundRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Fund>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Funds.ToListAsync(cancellationToken);
    }

    public async Task<Fund?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Funds.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task AddAsync(Fund fund, CancellationToken cancellationToken = default)
    {
        _dbContext.Funds.Add(fund);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fund = await GetByIdAsync(id, cancellationToken);
        if (fund is null)
        {
            return false;
        }

        _dbContext.Funds.Remove(fund);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
