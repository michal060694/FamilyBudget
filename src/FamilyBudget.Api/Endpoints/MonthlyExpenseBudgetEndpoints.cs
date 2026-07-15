using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Endpoints;

public static class MonthlyExpenseBudgetEndpoints
{
    public static void MapMonthlyExpenseBudgetEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monthly-expense-budgets", GetMonthlyExpenseBudgets);
        app.MapPost("/api/monthly-expense-budgets", CreateMonthlyExpenseBudgetItem);
        app.MapPut("/api/monthly-expense-budgets/{id:guid}", UpdateMonthlyExpenseBudgetItem);
        app.MapDelete("/api/monthly-expense-budgets/{id:guid}", DeleteMonthlyExpenseBudgetItem);
    }

    private static MonthlyExpenseBudgetItemView ToView(MonthlyExpenseBudgetItem item) => new(
        item.Id,
        item.Year,
        item.Month,
        item.Name,
        item.Type,
        item.BudgetedAmount,
        item.BudgetedAmountFormula,
        item.UsedAmount,
        item.UsedAmountFormula,
        item.Remaining);

    private static async Task<IResult> GetMonthlyExpenseBudgets(
        int year, int month, IMonthlyExpenseBudgetItemRepository repository, TransactionType? type = null)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var items = await repository.GetByMonthAsync(year, month, type);

        return Results.Ok(new MonthlyExpenseBudgetListResponse(year, month, items.Select(ToView).ToList()));
    }

    private static async Task<IResult> CreateMonthlyExpenseBudgetItem(
        CreateMonthlyExpenseBudgetItemRequest request, IMonthlyExpenseBudgetItemRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.BudgetedAmount < 0)
        {
            return Results.BadRequest("budgetedAmount must be >= 0.");
        }

        if (request.Type is not (TransactionType.FixedExpense or TransactionType.RegularExpense))
        {
            return Results.BadRequest("type must be FixedExpense or RegularExpense.");
        }

        if (request.Month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var item = new MonthlyExpenseBudgetItem(
            Guid.NewGuid(),
            request.Year,
            request.Month,
            request.Name,
            request.Type,
            request.BudgetedAmount,
            budgetedAmountFormula: request.BudgetedAmountFormula);

        await repository.AddAsync(item);

        return Results.Created($"/api/monthly-expense-budgets/{item.Id}", ToView(item));
    }

    private static async Task<IResult> UpdateMonthlyExpenseBudgetItem(
        Guid id, UpdateMonthlyExpenseBudgetItemRequest request, IMonthlyExpenseBudgetItemRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.BudgetedAmount < 0 || request.UsedAmount < 0)
        {
            return Results.BadRequest("budgetedAmount and usedAmount must be >= 0.");
        }

        var item = await repository.GetByIdAsync(id);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.Rename(request.Name);
        item.SetBudgetedAmount(request.BudgetedAmount, request.BudgetedAmountFormula);
        item.SetUsedAmount(request.UsedAmount, request.UsedAmountFormula);
        await repository.SaveChangesAsync();

        return Results.Ok(ToView(item));
    }

    private static async Task<IResult> DeleteMonthlyExpenseBudgetItem(Guid id, IMonthlyExpenseBudgetItemRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
