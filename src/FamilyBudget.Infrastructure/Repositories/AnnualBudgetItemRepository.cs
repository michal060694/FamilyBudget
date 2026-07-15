using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class AnnualBudgetItemRepository : IAnnualBudgetItemRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public AnnualBudgetItemRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AnnualBudgetItem>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnnualBudgetItems
            .Where(item => item.Year == year)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnnualBudgetItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AnnualBudgetItems
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task AddAsync(AnnualBudgetItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.AnnualBudgetItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _dbContext.AnnualBudgetItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
