using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>
/// Copies a year's annual budget items into another year — typically the previous year into a new
/// one at year-start — so the household doesn't need to re-enter every category from scratch.
/// Mirrors <see cref="MonthlyTemplateApplyService"/>'s idempotent-by-name copy: an item already
/// present in the target year (matched by name) is skipped rather than duplicated, so retrying is
/// harmless. <see cref="AnnualBudgetItem.AmountUsed"/> and <see cref="AnnualBudgetItem.AmountAlreadySetAside"/>
/// are deliberately not carried over — the new year starts with nothing spent or set aside yet.
/// </summary>
public class AnnualBudgetCopyService
{
    private readonly IAnnualBudgetItemRepository _repository;

    public AnnualBudgetCopyService(IAnnualBudgetItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<(int Copied, int Skipped)> CopyYearAsync(int sourceYear, int targetYear, CancellationToken cancellationToken = default)
    {
        var sourceItems = await _repository.GetByYearAsync(sourceYear, cancellationToken);
        var targetItems = await _repository.GetByYearAsync(targetYear, cancellationToken);
        var existingNames = targetItems.Select(i => i.Name).ToHashSet();

        var copied = 0;
        var skipped = 0;

        foreach (var item in sourceItems)
        {
            if (existingNames.Contains(item.Name))
            {
                skipped++;
                continue;
            }

            await _repository.AddAsync(new AnnualBudgetItem(
                Guid.NewGuid(), targetYear, item.Name, item.TotalAmount, item.TargetMonth,
                amountAlreadySetAside: 0m, totalAmountFormula: item.TotalAmountFormula), cancellationToken);
            copied++;
        }

        return (copied, skipped);
    }
}
