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

    public async Task<string?> GetFormulaAsync(int year, CancellationToken cancellationToken = default)
    {
        var reserve = await _dbContext.AnnualReserves
            .FirstOrDefaultAsync(r => r.Year == year, cancellationToken);
        return reserve?.AmountFormula;
    }

    public async Task SetAmountAsync(int year, decimal amount, string? amountFormula = null, CancellationToken cancellationToken = default)
    {
        var reserve = await _dbContext.AnnualReserves
            .FirstOrDefaultAsync(r => r.Year == year, cancellationToken);

        if (reserve is null)
        {
            _dbContext.AnnualReserves.Add(new AnnualReserve(year, amount, amountFormula));
        }
        else
        {
            reserve.Update(amount, amountFormula);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
