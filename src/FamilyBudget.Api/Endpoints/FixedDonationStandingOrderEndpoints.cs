using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Endpoints;

public static class FixedDonationStandingOrderEndpoints
{
    public static void MapFixedDonationStandingOrderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/fixed-donation-standing-orders", GetActiveForMonth);
        app.MapPost("/api/fixed-donation-standing-orders", CreateOrder);
        app.MapPut("/api/fixed-donation-standing-orders/{id:guid}", UpdateOrder);
        app.MapDelete("/api/fixed-donation-standing-orders/{id:guid}", DeleteOrder);
    }

    private static FixedDonationStandingOrderView ToView(FixedDonationStandingOrder order) => new(
        order.Id, order.Name, order.Amount, order.AmountFormula, order.ValidUntilYear, order.ValidUntilMonth);

    private static string? ValidateValidUntil(int? year, int? month)
    {
        if (year is null != month is null)
        {
            return "validUntilYear and validUntilMonth must both be set or both be null.";
        }

        if (month is < 1 or > 12)
        {
            return "validUntilMonth must be between 1 and 12 when specified.";
        }

        return null;
    }

    private static async Task<IResult> GetActiveForMonth(int year, int month, IFixedDonationStandingOrderRepository repository)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var orders = await repository.GetActiveForMonthAsync(year, month);
        var views = orders.Select(ToView).ToList();

        return Results.Ok(new FixedDonationStandingOrderListResponse(year, month, views, views.Sum(v => v.Amount)));
    }

    private static async Task<IResult> CreateOrder(
        CreateFixedDonationStandingOrderRequest request, IFixedDonationStandingOrderRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest("amount must be strictly positive.");
        }

        var validationError = ValidateValidUntil(request.ValidUntilYear, request.ValidUntilMonth);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var order = new FixedDonationStandingOrder(
            Guid.NewGuid(), request.Name, request.Amount, request.ValidUntilYear, request.ValidUntilMonth, request.AmountFormula);

        await repository.AddAsync(order);

        return Results.Created($"/api/fixed-donation-standing-orders/{order.Id}", ToView(order));
    }

    private static async Task<IResult> UpdateOrder(
        Guid id, UpdateFixedDonationStandingOrderRequest request, IFixedDonationStandingOrderRepository repository)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("name must not be empty.");
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest("amount must be strictly positive.");
        }

        var validationError = ValidateValidUntil(request.ValidUntilYear, request.ValidUntilMonth);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var order = await repository.GetByIdAsync(id);
        if (order is null)
        {
            return Results.NotFound();
        }

        order.Update(request.Name, request.Amount, request.ValidUntilYear, request.ValidUntilMonth, request.AmountFormula);
        await repository.SaveChangesAsync();

        return Results.Ok(ToView(order));
    }

    private static async Task<IResult> DeleteOrder(Guid id, IFixedDonationStandingOrderRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
