using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class FixedDonationStandingOrderRepository : IFixedDonationStandingOrderRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public FixedDonationStandingOrderRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<FixedDonationStandingOrder>> GetActiveForMonthAsync(
        int year, int month, CancellationToken cancellationToken = default)
    {
        var targetIndex = (year * 12) + month;

        return await _dbContext.FixedDonationStandingOrders
            .Where(o => o.ValidUntilYear == null || ((o.ValidUntilYear!.Value * 12) + o.ValidUntilMonth!.Value) >= targetIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<FixedDonationStandingOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FixedDonationStandingOrders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(FixedDonationStandingOrder order, CancellationToken cancellationToken = default)
    {
        _dbContext.FixedDonationStandingOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return false;
        }

        _dbContext.FixedDonationStandingOrders.Remove(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
