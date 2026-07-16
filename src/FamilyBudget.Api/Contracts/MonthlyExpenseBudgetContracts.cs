using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Contracts;

public record MonthlyExpenseBudgetItemView(
    Guid Id,
    int Year,
    int Month,
    string Name,
    TransactionType Type,
    decimal BudgetedAmount,
    string? BudgetedAmountFormula,
    decimal UsedAmount,
    string? UsedAmountFormula,
    decimal Remaining,
    bool IncludeInOutflowTotal);

public record MonthlyExpenseBudgetListResponse(
    int Year, int Month, IReadOnlyList<MonthlyExpenseBudgetItemView> Items);

public record CreateMonthlyExpenseBudgetItemRequest(
    int Year,
    int Month,
    string Name,
    TransactionType Type,
    decimal BudgetedAmount,
    string? BudgetedAmountFormula = null,
    bool IncludeInOutflowTotal = true);

public record UpdateMonthlyExpenseBudgetItemRequest(
    string Name,
    decimal BudgetedAmount,
    decimal UsedAmount,
    string? BudgetedAmountFormula = null,
    string? UsedAmountFormula = null,
    bool IncludeInOutflowTotal = true);
