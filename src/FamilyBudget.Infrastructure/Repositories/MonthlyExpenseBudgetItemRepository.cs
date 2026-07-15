using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class MonthlyExpenseBudgetItemRepository : IMonthlyExpenseBudgetItemRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public MonthlyExpenseBudgetItemRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MonthlyExpenseBudgetItem>> GetByMonthAsync(
        int year, int month, TransactionType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MonthlyExpenseBudgetItems.Where(i => i.Year == year && i.Month == month);

        if (type is { } requestedType)
        {
            query = query.Where(i => i.Type == requestedType);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<MonthlyExpenseBudgetItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonthlyExpenseBudgetItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task AddAsync(MonthlyExpenseBudgetItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.MonthlyExpenseBudgetItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _dbContext.MonthlyExpenseBudgetItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
