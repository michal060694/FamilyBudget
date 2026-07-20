namespace FamilyBudget.Api.Contracts;

public record AnnualBudgetResponse(
    int Year,
    decimal ReserveOnHand,
    string? ReserveOnHandFormula,
    decimal TotalAnnualBudget,
    decimal NotYetCovered,
    decimal MonthlyAllocation,
    IReadOnlyList<AnnualBudgetItemView> Items);

public record AnnualBudgetItemView(
    Guid AnnualBudgetItemId,
    string Name,
    int? TargetMonth,
    decimal TotalAmount,
    string? TotalAmountFormula,
    decimal AmountAlreadySetAside,
    decimal AmountUsed,
    string? AmountUsedFormula,
    string? Notes,
    bool IsFullyUsed);

public record CreateAnnualBudgetItemRequest(
    int Year, string Name, decimal TotalAmount, int? TargetMonth, string? TotalAmountFormula = null, string? Notes = null);

public record SetReserveRequest(decimal Amount, string? AmountFormula = null);

public record SetUsageRequest(decimal Amount, string? AmountFormula = null);

public record SetTotalAmountRequest(decimal Amount, string? AmountFormula = null);

public record SetAnnualBudgetItemNotesRequest(string? Notes);

public record CopyAnnualBudgetYearRequest(int SourceYear, int TargetYear);

public record CopyAnnualBudgetYearResponse(int SourceYear, int TargetYear, int ItemsCopied, int ItemsSkipped);
