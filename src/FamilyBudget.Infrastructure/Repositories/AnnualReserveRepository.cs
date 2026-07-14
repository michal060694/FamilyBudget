using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class AnnualReserveRepository : IAnnualReserveRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public AnnualReserveRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetAmountAsync(int year, CancellationToken cancellationToken = default)
    {
        var reserve = await _dbContext.AnnualReserves
            .FirstOrDefaultAsync(r => r.Year == year, cancellationToken);
        return reserve?.Amount ?? 0m;
    }

    public async Task SetAmountAsync(int year, decimal amount, CancellationToken cancellationToken = default)
    {
        var reserve = await _dbContext.AnnualReserves
            .FirstOrDefaultAsync(r => r.Year == year, cancellationToken);

        if (reserve is null)
        {
            _dbContext.AnnualReserves.Add(new AnnualReserve(year, amount));
        }
        else
        {
            reserve.Update(amount);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
