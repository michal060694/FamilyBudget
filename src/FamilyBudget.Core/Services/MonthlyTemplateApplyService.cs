using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>
/// Applies the household's recurring monthly plan (<see cref="MonthlyTemplateItem"/>) to a specific
/// calendar month in one action, instead of the user re-entering every income/expense category from
/// scratch each month. Idempotent per month: an item already represented that month (matched by name
/// within its type) is skipped rather than duplicated, so applying twice is harmless.
/// </summary>
public class MonthlyTemplateApplyService
{
    private readonly IMonthlyTemplateItemRepository _templateRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMonthlyExpenseBudgetItemRepository _expenseRepository;

    public MonthlyTemplateApplyService(
        IMonthlyTemplateItemRepository templateRepository,
        ITransactionRepository transactionRepository,
        IMonthlyExpenseBudgetItemRepository expenseRepository)
    {
        _templateRepository = templateRepository;
        _transactionRepository = transactionRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<(int Applied, int Skipped)> ApplyToMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var templateItems = await _templateRepository.GetAllAsync(cancellationToken);
        var existingTransactions = await _transactionRepository.GetByMonthAsync(year, month, TransactionType.Income, cancellationToken: cancellationToken);
        var existingExpenses = await _expenseRepository.GetByMonthAsync(year, month, cancellationToken: cancellationToken);

        var firstOfMonth = new DateOnly(year, month, 1);
        var applied = 0;
        var skipped = 0;

        foreach (var item in templateItems)
        {
            if (item.Type == TransactionType.Income)
            {
                if (existingTransactions.Any(t => t.Description == item.Name))
                {
                    skipped++;
                    continue;
                }

                await _transactionRepository.AddAsync(new Transaction(
                    Guid.NewGuid(), firstOfMonth, item.Amount, TransactionType.Income,
                    PaymentMethod.BankTransfer, item.IsTitheApplicable, item.Name, item.AmountFormula), cancellationToken);
            }
            else
            {
                if (existingExpenses.Any(e => e.Type == item.Type && e.Name == item.Name))
                {
                    skipped++;
                    continue;
                }

                await _expenseRepository.AddAsync(new MonthlyExpenseBudgetItem(
                    Guid.NewGuid(), year, month, item.Name, item.Type, item.Amount,
                    budgetedAmountFormula: item.AmountFormula), cancellationToken);
            }

            applied++;
        }

        return (applied, skipped);
    }
}
