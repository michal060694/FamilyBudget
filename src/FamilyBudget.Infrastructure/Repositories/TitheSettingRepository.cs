using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class TitheSettingRepository : ITitheSettingRepository
{
    private const decimal DefaultRate = 0.2m;

    private readonly FamilyBudgetDbContext _dbContext;

    public TitheSettingRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetRateAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.TitheSettings
            .FirstOrDefaultAsync(s => s.Id == TitheSetting.SingletonId, cancellationToken);
        return setting?.Rate ?? DefaultRate;
    }

    public async Task SetRateAsync(decimal rate, CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.TitheSettings
            .FirstOrDefaultAsync(s => s.Id == TitheSetting.SingletonId, cancellationToken);

        if (setting is null)
        {
            _dbContext.TitheSettings.Add(new TitheSetting(rate));
        }
        else
        {
            setting.Update(rate);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
