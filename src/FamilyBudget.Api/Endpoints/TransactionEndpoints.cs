using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/transactions", CreateTransaction);
        app.MapGet("/api/transactions", GetTransactions);
        app.MapPut("/api/transactions/{id:guid}", UpdateTransaction);
        app.MapDelete("/api/transactions/{id:guid}", DeleteTransaction);
    }

    private static TransactionResponse ToResponse(Transaction transaction) => new(
        transaction.Id,
        transaction.Date,
        transaction.Amount,
        transaction.AmountFormula,
        transaction.Type,
        transaction.PaymentMethod,
        transaction.IsTitheApplicable,
        transaction.Description);

    private static string? ValidateRequest(CreateTransactionRequest request)
    {
        if (request.Amount <= 0)
        {
            return "amount must be strictly positive.";
        }

        if (request.Type == TransactionType.Income && request.IsTitheApplicable is null)
        {
            return "isTitheApplicable is required for Income transactions.";
        }

        if (request.Type != TransactionType.Income && request.IsTitheApplicable is not null)
        {
            return "isTitheApplicable must be omitted for non-Income transactions.";
        }

        return null;
    }

    private static async Task<IResult> CreateTransaction(CreateTransactionRequest request, ITransactionRepository repository)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var transaction = new Transaction(
            Guid.NewGuid(),
            request.Date,
            request.Amount,
            request.Type,
            request.PaymentMethod,
            request.IsTitheApplicable,
            request.Description,
            request.AmountFormula);

        await repository.AddAsync(transaction);

        return Results.Created($"/api/transactions/{transaction.Id}", ToResponse(transaction));
    }

    private static async Task<IResult> GetTransactions(
        int year,
        int month,
        ITransactionRepository repository,
        TransactionType? type = null,
        PaymentMethod? paymentMethod = null)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest("month must be between 1 and 12.");
        }

        var transactions = await repository.GetByMonthAsync(year, month, type, paymentMethod);

        return Results.Ok(new TransactionListResponse(year, month, transactions.Select(ToResponse).ToList()));
    }

    private static async Task<IResult> UpdateTransaction(Guid id, CreateTransactionRequest request, ITransactionRepository repository)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var transaction = await repository.GetByIdAsync(id);
        if (transaction is null)
        {
            return Results.NotFound();
        }

        transaction.Update(
            request.Date, request.Amount, request.Type, request.PaymentMethod,
            request.IsTitheApplicable, request.Description, request.AmountFormula);
        await repository.SaveChangesAsync();

        return Results.Ok(ToResponse(transaction));
    }

    private static async Task<IResult> DeleteTransaction(Guid id, ITransactionRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
