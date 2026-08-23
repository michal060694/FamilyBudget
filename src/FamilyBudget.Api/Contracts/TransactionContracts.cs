using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Contracts;

public record TransactionResponse(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string? AmountFormula,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    bool? IsTitheApplicable,
    string? Description,
    bool TransferredToAnnual);

public record TransactionListResponse(int Year, int Month, IReadOnlyList<TransactionResponse> Transactions);

public record CreateTransactionRequest(
    DateOnly Date,
    decimal Amount,
    TransactionType Type,
    PaymentMethod PaymentMethod,
    bool? IsTitheApplicable,
    string? Description,
    string? AmountFormula = null,
    bool TransferredToAnnual = false);
