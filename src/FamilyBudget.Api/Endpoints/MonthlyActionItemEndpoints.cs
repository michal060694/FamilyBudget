using FamilyBudget.Api.Contracts;
using FamilyBudget.Api.Services;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Endpoints;

public static class MonthlyActionItemEndpoints
{
    public static void MapMonthlyActionItemEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monthly-action-items", GetMonthlyActionItems);
        app.MapPost("/api/monthly-action-items", CreateMonthlyActionItem);
        app.MapPut("/api/monthly-action-items/{id:guid}", UpdateMonthlyActionItem);
        app.MapPatch("/api/monthly-action-items/{id:guid}/complete", SetComplete);
        app.MapDelete("/api/monthly-action-items/{id:guid}", DeleteMonthlyActionItem);
        app.MapPost("/api/monthly-action-items/send-reminders-now", SendRemindersNow);
    }

    private static MonthlyActionItemView ToView(MonthlyActionItem item)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var isOverdue = item.DeadlineDate is { } deadline && deadline < today && !item.IsCompleted;

        return new MonthlyActionItemView(item.Id, item.Year, item.Month, item.Description, item.Amount, item.DeadlineDate, item.IsCompleted, isOverdue);
    }

    private static async Task<IResult> GetMonthlyActionItems(int year, int month, IMonthlyActionItemRepository repository)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var items = await repository.GetByMonthAsync(year, month);
        return Results.Ok(new MonthlyActionItemListResponse(year, month, items.Select(ToView).ToList()));
    }

    private static async Task<IResult> CreateMonthlyActionItem(
        CreateMonthlyActionItemRequest request, IMonthlyActionItemRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Results.BadRequest("description must not be empty.");
        }

        if (request.Month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var item = new MonthlyActionItem(Guid.NewGuid(), request.Year, request.Month, request.Description, request.Amount, request.DeadlineDate);
        await repository.AddAsync(item);

        return Results.Created($"/api/monthly-action-items/{item.Id}", ToView(item));
    }

    private static async Task<IResult> UpdateMonthlyActionItem(
        Guid id, UpdateMonthlyActionItemRequest request, IMonthlyActionItemRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Results.BadRequest("description must not be empty.");
        }

        var item = await repository.GetByIdAsync(id);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.Rename(request.Description);
        item.SetAmount(request.Amount);
        item.SetDeadline(request.DeadlineDate);
        await repository.SaveChangesAsync();

        return Results.Ok(ToView(item));
    }

    private static async Task<IResult> SetComplete(
        Guid id, SetMonthlyActionItemCompleteRequest request, IMonthlyActionItemRepository repository)
    {
        var item = await repository.GetByIdAsync(id);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.SetCompleted(request.IsCompleted);
        await repository.SaveChangesAsync();

        return Results.Ok(ToView(item));
    }

    private static async Task<IResult> DeleteMonthlyActionItem(Guid id, IMonthlyActionItemRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> SendRemindersNow(MonthlyActionItemReminderService reminderService)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var sentCount = await reminderService.SendOverdueRemindersAsync(today);
        return Results.Ok(new SendRemindersNowResponse(sentCount));
    }
}
