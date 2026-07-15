using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>
/// Records a repayment against a debt and generates the matching transaction (feature 002) in the
/// same operation — the one place in this feature where the <see cref="Debt"/> and the
/// <see cref="Transaction"/> ledger must always change together (FR-010, FR-011).
/// </summary>
public class DebtRepaymentService
{
    private readonly IDebtRepository _debtRepository;
    private readonly ITransactionRepository _transactionRepository;

    public DebtRepaymentService(IDebtRepository debtRepository, ITransactionRepository transactionRepository)
    {
        _debtRepository = debtRepository;
        _transactionRepository = transactionRepository;
    }

    /// <summary>Returns null if no debt with the given id exists.</summary>
    public async Task<Debt?> RecordRepaymentAsync(
        Guid debtId, decimal amount, PaymentMethod paymentMethod, DateOnly date, CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(debtId, cancellationToken);
        if (debt is null)
        {
            return null;
        }

        debt.RecordRepayment(amount);
        await _debtRepository.SaveChangesAsync(cancellationToken);

        var transaction = debt.Direction == DebtDirection.Receivable
            ? new Transaction(
                Guid.NewGuid(), date, amount, TransactionType.Income, paymentMethod,
                isTitheApplicable: false, description: $"החזר חוב - {debt.CounterpartyName}")
            : new Transaction(
                Guid.NewGuid(), date, amount, TransactionType.DebtRepayment, paymentMethod,
                isTitheApplicable: null, description: $"החזר חוב - {debt.CounterpartyName}");

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        return debt;
    }
}
