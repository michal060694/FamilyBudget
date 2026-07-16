using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class MonthlyTemplateItemRepository : IMonthlyTemplateItemRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public MonthlyTemplateItemRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MonthlyTemplateItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonthlyTemplateItems.ToListAsync(cancellationToken);
    }

    public async Task<MonthlyTemplateItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonthlyTemplateItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task AddAsync(MonthlyTemplateItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.MonthlyTemplateItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _dbContext.MonthlyTemplateItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
