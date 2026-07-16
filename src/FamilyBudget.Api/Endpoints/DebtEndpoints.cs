using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Api.Endpoints;

public static class DebtEndpoints
{
    public static void MapDebtEndpoints(this WebApplication app)
    {
        app.MapGet("/api/debts", GetDebts);
        app.MapPost("/api/debts", CreateDebt);
        app.MapPut("/api/debts/{id:guid}", UpdateDebt);
        app.MapDelete("/api/debts/{id:guid}", DeleteDebt);
        app.MapPost("/api/debts/{id:guid}/repayments", RecordRepayment);
        app.MapPatch("/api/debts/{id:guid}/balance", SetBalance);
    }

    private static DebtResponse ToResponse(Debt debt) => new(
        debt.Id,
        debt.Direction,
        debt.CounterpartyName,
        debt.OriginalAmount,
        debt.CurrentBalance,
        debt.Status,
        debt.TargetDate,
        debt.RepaymentRate,
        debt.Notes);

    private static string? ValidateRequest(string counterpartyName, decimal originalAmount, decimal? repaymentRate)
    {
        if (string.IsNullOrWhiteSpace(counterpartyName))
        {
            return "counterpartyName must not be empty.";
        }

        if (originalAmount <= 0)
        {
            return "originalAmount must be strictly positive.";
        }

        if (repaymentRate is <= 0)
        {
            return "repaymentRate must be strictly positive when specified.";
        }

        return null;
    }

    private static async Task<IResult> GetDebts(IDebtRepository repository)
    {
        var debts = await repository.GetAllAsync();

        var receivables = debts.Where(d => d.Direction == DebtDirection.Receivable).Select(ToResponse).ToList();
        var payables = debts.Where(d => d.Direction == DebtDirection.Payable).Select(ToResponse).ToList();

        return Results.Ok(new DebtsLedgerResponse(receivables, payables));
    }

    private static async Task<IResult> CreateDebt(CreateDebtRequest request, IDebtRepository repository)
    {
        var validationError = ValidateRequest(request.CounterpartyName, request.OriginalAmount, request.RepaymentRate);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var debt = new Debt(
            Guid.NewGuid(), request.Direction, request.CounterpartyName, request.OriginalAmount,
            request.TargetDate, request.RepaymentRate, request.Notes);

        await repository.AddAsync(debt);

        return Results.Created($"/api/debts/{debt.Id}", ToResponse(debt));
    }

    private static async Task<IResult> UpdateDebt(Guid id, UpdateDebtRequest request, IDebtRepository repository)
    {
        var validationError = ValidateRequest(request.CounterpartyName, request.OriginalAmount, request.RepaymentRate);
        if (validationError is not null)
        {
            return Results.BadRequest(validationError);
        }

        var debt = await repository.GetByIdAsync(id);
        if (debt is null)
        {
            return Results.NotFound();
        }

        debt.Update(request.CounterpartyName, request.OriginalAmount, request.TargetDate, request.RepaymentRate, request.Notes);
        await repository.SaveChangesAsync();

        return Results.Ok(ToResponse(debt));
    }

    private static async Task<IResult> DeleteDebt(Guid id, IDebtRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> SetBalance(Guid id, SetDebtBalanceRequest request, IDebtRepository repository)
    {
        if (request.CurrentBalance < 0)
        {
            return Results.BadRequest("currentBalance must be >= 0.");
        }

        var debt = await repository.GetByIdAsync(id);
        if (debt is null)
        {
            return Results.NotFound();
        }

        debt.SetCurrentBalance(request.CurrentBalance);
        await repository.SaveChangesAsync();

        return Results.Ok(ToResponse(debt));
    }

    private static async Task<IResult> RecordRepayment(
        Guid id, RecordRepaymentRequest request, DebtRepaymentService repaymentService, IDebtRepository debtRepository)
    {
        var existing = await debtRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return Results.NotFound();
        }

        if (existing.Status == DebtStatus.Closed)
        {
            return Results.Conflict("This debt is already closed.");
        }

        if (request.Amount <= 0 || request.Amount > existing.CurrentBalance)
        {
            return Results.BadRequest("amount must be strictly positive and not exceed the current balance.");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var updated = await repaymentService.RecordRepaymentAsync(id, request.Amount, request.PaymentMethod, today);

        return Results.Ok(ToResponse(updated!));
    }
}
