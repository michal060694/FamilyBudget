using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Contracts;

public record MonthlyTemplateItemView(
    Guid Id,
    TransactionType Type,
    string Name,
    decimal Amount,
    string? AmountFormula,
    bool? IsTitheApplicable);

public record MonthlyTemplateListResponse(IReadOnlyList<MonthlyTemplateItemView> Items);

public record CreateMonthlyTemplateItemRequest(
    TransactionType Type,
    string Name,
    decimal Amount,
    bool? IsTitheApplicable,
    string? AmountFormula = null);

public record UpdateMonthlyTemplateItemRequest(
    string Name,
    decimal Amount,
    bool? IsTitheApplicable,
    string? AmountFormula = null);

public record ApplyMonthlyTemplateResponse(int Year, int Month, int ItemsApplied, int ItemsSkipped);
