namespace FamilyBudget.Api.Contracts;

public record DashboardResponse(
    int Year,
    int Month,
    decimal ProjectedIncome,
    decimal FixedExpenses,
    decimal TotalRequiredAllocation,
    decimal FreeBalance,
    IReadOnlyList<AllocationLine> AllocationLines);

public record AllocationLine(
    Guid AnnualBudgetItemId,
    string Name,
    int? TargetMonth,
    decimal TotalAmount,
    decimal AmountAlreadySetAside,
    decimal AmountUsed,
    decimal AllocatedMonthly);
