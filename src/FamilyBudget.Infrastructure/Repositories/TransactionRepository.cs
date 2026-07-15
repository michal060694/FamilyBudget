using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly FamilyBudgetDbContext _dbContext;

    public TransactionRepository(FamilyBudgetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByMonthAsync(
        int year,
        int month,
        TransactionType? type = null,
        PaymentMethod? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var query = _dbContext.Transactions.Where(t => t.Date >= start && t.Date < end);

        if (type is { } requestedType)
        {
            query = query.Where(t => t.Type == requestedType);
        }

        if (paymentMethod is { } requestedPaymentMethod)
        {
            query = query.Where(t => t.PaymentMethod == requestedPaymentMethod);
        }

        return await query.OrderBy(t => t.Date).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetUpToMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var end = new DateOnly(year, month, 1).AddMonths(1);

        return await _dbContext.Transactions
            .Where(t => t.Date < end)
            .OrderBy(t => t.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transaction = await GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
