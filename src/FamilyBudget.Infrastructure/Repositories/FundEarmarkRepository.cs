using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class FundEarmarkRepository : IFundEarmarkRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public FundEarmarkRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FundEarmark>> GetByFundIdAsync(Guid fundId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FundEarmarks
            .Where(e => e.FundId == fundId)
            .OrderBy(e => e.PurposeLabel)
            .ToListAsync(cancellationToken);
    }

    public async Task<FundEarmark?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FundEarmarks.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(FundEarmark earmark, CancellationToken cancellationToken = default)
    {
        _dbContext.FundEarmarks.Add(earmark);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var earmark = await GetByIdAsync(id, cancellationToken);
        if (earmark is null)
        {
            return false;
        }

        _dbContext.FundEarmarks.Remove(earmark);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
