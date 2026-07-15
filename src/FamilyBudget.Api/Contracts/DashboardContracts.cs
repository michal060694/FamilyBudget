namespace FamilyBudget.Api.Contracts;

public record DashboardResponse(
    int Year,
    int Month,
    decimal ProjectedIncome,
    decimal FixedExpenses,
    decimal TotalRequiredAllocation,
    decimal FreeBalance,
    IReadOnlyList<AllocationLine> AllocationLines,
    TitheDueView TitheDue);

public record AllocationLine(
    Guid AnnualBudgetItemId,
    string Name,
    int? TargetMonth,
    decimal TotalAmount,
    decimal AmountAlreadySetAside,
    decimal AmountUsed,
    decimal AllocatedMonthly);

/// <summary>The dashboard's traceable tithe breakdown (FR-012, FR-013).</summary>
public record TitheDueView(
    decimal GrossTitheTarget,
    decimal FixedDonationsDeduction,
    decimal SmallCharityDeduction,
    decimal NetTitheDue);
