using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class MonthEndItemRepository : IMonthEndItemRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public MonthEndItemRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MonthEndItem>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default) =>
        await _dbContext.MonthEndItems
            .Where(item => item.Year == year && item.Month == month)
            .OrderBy(item => item.Direction)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

    public Task<MonthEndItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.MonthEndItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task AddAsync(MonthEndItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.MonthEndItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _dbContext.MonthEndItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}