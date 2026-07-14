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

    public async Task AddAsync(AnnualBudgetItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.AnnualBudgetItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
