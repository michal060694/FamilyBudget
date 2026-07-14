using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Endpoints;

public static class AnnualBudgetEndpoints
{
    public static void MapAnnualBudgetEndpoints(this WebApplication app)
    {
        app.MapGet("/api/annual-budget", GetAnnualBudget);
        app.MapPost("/api/annual-budget-items", CreateAnnualBudgetItem);
        app.MapPut("/api/reserve", SetReserve);
    }

    private static async Task<IResult> GetAnnualBudget(int year, AnnualBudgetQueryService queryService)
    {
        var items = await queryService.GetOrderedForYearAsync(year);
        var summary = await queryService.GetSummaryAsync(year);

        var views = items
            .Select(item => new AnnualBudgetItemView(
                item.Id,
                item.Name,
                item.TargetMonth,
                item.TotalAmount,
                item.AmountAlreadySetAside,
                item.IsFullyFunded))
            .ToList();

        return Results.Ok(new AnnualBudgetResponse(
            year,
            summary.ReserveOnHand,
            summary.TotalAnnualBudget,
            summary.NotYetCovered,
            summary.MonthlyAllocation,
            views));
    }

    private static async Task<IResult> CreateAnnualBudgetItem(
        CreateAnnualBudgetItemRequest request,
        IAnnualBudgetItemRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.TotalAmount <= 0)
        {
            return Results.BadRequest("totalAmount must be strictly positive.");
        }

        if (request.TargetMonth is < 1 or > 12)
        {
            return Results.BadRequest("targetMonth must be between 1 and 12 when specified.");
        }

        var item = new AnnualBudgetItem(
            Guid.NewGuid(),
            request.Year,
            request.Name,
            request.TotalAmount,
            request.TargetMonth,
            amountAlreadySetAside: 0m);

        await repository.AddAsync(item);

        var view = new AnnualBudgetItemView(
            item.Id, item.Name, item.TargetMonth, item.TotalAmount, item.AmountAlreadySetAside, item.IsFullyFunded);

        return Results.Created($"/api/annual-budget-items/{item.Id}", view);
    }

    private static async Task<IResult> SetReserve(int year, SetReserveRequest request, IAnnualReserveRepository repository)
    {
        if (request.Amount < 0)
        {
            return Results.BadRequest("amount must be >= 0.");
        }

        await repository.SetAmountAsync(year, request.Amount);
        return Results.Ok(new { year, amount = request.Amount });
    }
}
