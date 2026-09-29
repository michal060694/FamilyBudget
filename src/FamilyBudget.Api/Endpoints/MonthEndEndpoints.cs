using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Endpoints;

public static class MonthEndEndpoints
{
    public static void MapMonthEndEndpoints(this WebApplication app)
    {
        app.MapGet("/api/month-end", GetItems);
        app.MapPost("/api/month-end", CreateItem);
        app.MapPut("/api/month-end/{id:guid}", UpdateItem);
        app.MapDelete("/api/month-end/{id:guid}", DeleteItem);
    }

    private static async Task<IResult> GetItems(int year, int month, IMonthEndItemRepository repository)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
        {
            return Results.BadRequest("year must be between 1 and 9999 and month must be between 1 and 12.");
        }

        var items = await repository.GetByMonthAsync(year, month);
        return Results.Ok(items.Select(ToView).ToList());
    }

    private static async Task<IResult> CreateItem(CreateMonthEndItemRequest request, IMonthEndItemRepository repository)
    {
        if (request.Year is < 1 or > 9999 || request.Month is < 1 or > 12 ||
            !Enum.IsDefined(request.Direction) || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 || request.Amount <= 0)
        {
            return Results.BadRequest("A valid year, month, direction, name (up to 200 characters), and positive amount are required.");
        }

        var item = new MonthEndItem(Guid.NewGuid(), request.Year, request.Month, request.Direction, request.Name, request.Amount);
        await repository.AddAsync(item);
        return Results.Created($"/api/month-end/{item.Id}", ToView(item));
    }

    private static async Task<IResult> UpdateItem(Guid id, UpdateMonthEndItemRequest request, IMonthEndItemRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 || request.Amount <= 0)
        {
            return Results.BadRequest("A name (up to 200 characters) and positive amount are required.");
        }

        var item = await repository.GetByIdAsync(id);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.Update(request.Name, request.Amount);
        await repository.SaveChangesAsync();
        return Results.Ok(ToView(item));
    }

    private static async Task<IResult> DeleteItem(Guid id, IMonthEndItemRepository repository) =>
        await repository.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();

    private static MonthEndItemView ToView(MonthEndItem item) =>
        new(item.Id, item.Year, item.Month, item.Direction, item.Name, item.Amount);
}