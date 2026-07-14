namespace FamilyBudget.Api.Contracts;

public record AnnualBudgetResponse(
    int Year,
    decimal ReserveOnHand,
    decimal TotalAnnualBudget,
    decimal NotYetCovered,
    decimal MonthlyAllocation,
    IReadOnlyList<AnnualBudgetItemView> Items);

public record AnnualBudgetItemView(
    Guid AnnualBudgetItemId,
    string Name,
    int? TargetMonth,
    decimal TotalAmount,
    decimal AmountAlreadySetAside,
    decimal AmountUsed,
    bool IsFullyUsed);

public record CreateAnnualBudgetItemRequest(int Year, string Name, decimal TotalAmount, int? TargetMonth);

public record SetReserveRequest(decimal Amount);

public record RecordUsageRequest(decimal Amount);
