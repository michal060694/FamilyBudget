using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Endpoints;

public static class MonthlyTemplateEndpoints
{
    public static void MapMonthlyTemplateEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monthly-template", GetMonthlyTemplate);
        app.MapPost("/api/monthly-template-items", CreateMonthlyTemplateItem);
        app.MapPut("/api/monthly-template-items/{id:guid}", UpdateMonthlyTemplateItem);
        app.MapDelete("/api/monthly-template-items/{id:guid}", DeleteMonthlyTemplateItem);
        app.MapPost("/api/monthly-template/apply", ApplyMonthlyTemplate);
    }

    private static MonthlyTemplateItemView ToView(MonthlyTemplateItem item) => new(
        item.Id, item.Type, item.Name, item.Amount, item.AmountFormula, item.IsTitheApplicable);

    private static string? ValidateRequest(TransactionType type, string name, decimal amount, bool? isTitheApplicable)
    {
        if (type is not (TransactionType.Income or TransactionType.FixedExpense or TransactionType.RegularExpense))
        {
            return "type must be Income, FixedExpense, or RegularExpense.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "name must not be empty.";
        }

        if (amount <= 0)
        {
            return "amount must be strictly positive.";
        }

        if (type == TransactionType.Income && isTitheApplicable is null)
        {
            return "isTitheApplicable is required for Income items.";
        }

        if (type != TransactionType.Income && isTitheApplicable is not null)
        {
            return "isTitheApplicable must be null for non-Income items.";
        }

        return null;
    }

    private static async Task<IResult> GetMonthlyTemplate(IMonthlyTemplateItemRepository repository)
    {
        var items = await repository.GetAllAsync();
        return Results.Ok(new MonthlyTemplateListResponse(items.Select(ToView).ToList()));
    }

    private static async Task<IResult> CreateMonthlyTemplateItem(
        CreateMonthlyTemplateItemRequest request, IMonthlyTemplateItemRepository repository)
    {
        var validationError = ValidateRequest(request.Type, request.Name, request.Amount, request.IsTitheApplicable);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var item = new MonthlyTemplateItem(
            Guid.NewGuid(), request.Type, request.Name, request.Amount, request.IsTitheApplicable, request.AmountFormula);

        await repository.AddAsync(item);

        return Results.Created($"/api/monthly-template-items/{item.Id}", ToView(item));
    }

    private static async Task<IResult> UpdateMonthlyTemplateItem(
        Guid id, UpdateMonthlyTemplateItemRequest request, IMonthlyTemplateItemRepository repository)
    {
        var item = await repository.GetByIdAsync(id);
        if (item is null)
        {
            return Results.NotFound();
        }

        var validationError = ValidateRequest(item.Type, request.Name, request.Amount, request.IsTitheApplicable);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        item.Update(request.Name, request.Amount, request.IsTitheApplicable, request.AmountFormula);
        await repository.SaveChangesAsync();

        return Results.Ok(ToView(item));
    }

    private static async Task<IResult> DeleteMonthlyTemplateItem(Guid id, IMonthlyTemplateItemRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ApplyMonthlyTemplate(int year, int month, MonthlyTemplateApplyService applyService)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var (applied, skipped) = await applyService.ApplyToMonthAsync(year, month);

        return Results.Ok(new ApplyMonthlyTemplateResponse(year, month, applied, skipped));
    }
}
