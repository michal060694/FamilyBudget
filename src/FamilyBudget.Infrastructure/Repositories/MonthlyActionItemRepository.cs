using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class MonthlyActionItemRepository : IMonthlyActionItemRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public MonthlyActionItemRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MonthlyActionItem>> GetByMonthAsync(
        int year, int month, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonthlyActionItems
            .Where(i => i.Year == year && i.Month == month)
            .ToListAsync(cancellationToken);
    }

    public async Task<MonthlyActionItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonthlyActionItems
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyActionItem>> GetOverdueUncompletedWithoutReminderAsync(
        DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MonthlyActionItems
            .Where(i => i.DeadlineDate != null && i.DeadlineDate < asOfDate && !i.IsCompleted && i.ReminderSentAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MonthlyActionItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.MonthlyActionItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _dbContext.MonthlyActionItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
