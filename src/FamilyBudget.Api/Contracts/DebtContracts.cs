using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Contracts;

public record DebtResponse(
    Guid Id,
    DebtDirection Direction,
    string CounterpartyName,
    decimal OriginalAmount,
    decimal CurrentBalance,
    string? CurrentBalanceFormula,
    DebtStatus Status,
    DateOnly? TargetDate,
    decimal? RepaymentRate,
    string? Notes);

public record DebtsLedgerResponse(IReadOnlyList<DebtResponse> Receivables, IReadOnlyList<DebtResponse> Payables);

public record CreateDebtRequest(
    DebtDirection Direction,
    string CounterpartyName,
    decimal OriginalAmount,
    DateOnly? TargetDate = null,
    decimal? RepaymentRate = null,
    string? Notes = null);

public record UpdateDebtRequest(
    string CounterpartyName,
    decimal OriginalAmount,
    DateOnly? TargetDate = null,
    decimal? RepaymentRate = null,
    string? Notes = null);

public record RecordRepaymentRequest(decimal Amount, PaymentMethod PaymentMethod);

public record SetDebtBalanceRequest(decimal CurrentBalance, string? CurrentBalanceFormula = null);
