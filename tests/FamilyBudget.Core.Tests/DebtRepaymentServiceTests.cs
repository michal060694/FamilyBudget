using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class DebtRepaymentServiceTests
{
    private sealed class FakeDebtRepository : IDebtRepository
    {
        private readonly Dictionary<Guid, Debt> _debts = new();

        public void Seed(Debt debt) => _debts[debt.Id] = debt;

        public Task<IReadOnlyList<Debt>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Debt>>(_debts.Values.ToList());

        public Task<Debt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_debts.GetValueOrDefault(id));

        public Task AddAsync(Debt debt, CancellationToken cancellationToken = default)
        {
            _debts[debt.Id] = debt;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_debts.Remove(id));

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public readonly List<Transaction> Added = new();

        public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Added.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<Transaction>> GetByMonthAsync(
            int year, int month, TransactionType? type = null, PaymentMethod? paymentMethod = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(Added
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .Where(t => type is null || t.Type == type)
                .Where(t => paymentMethod is null || t.PaymentMethod == paymentMethod)
                .ToList());

        public Task<IReadOnlyList<Transaction>> GetUpToMonthAsync(int year, int month, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(Added.ToList());

        public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            Added.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Added.RemoveAll(t => t.Id == id) > 0);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task RecordRepaymentAsync_Receivable_CreatesIncomeTransaction_NotTitheApplicable()
    {
        var debtRepository = new FakeDebtRepository();
        var transactionRepository = new FakeTransactionRepository();
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi Cohen", 1000m);
        debtRepository.Seed(debt);
        var service = new DebtRepaymentService(debtRepository, transactionRepository);

        var updated = await service.RecordRepaymentAsync(debt.Id, 400m, PaymentMethod.BankTransfer, new DateOnly(2026, 7, 15));

        Assert.Equal(600m, updated!.CurrentBalance);
        var transaction = Assert.Single(transactionRepository.Added);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(400m, transaction.Amount);
        Assert.False(transaction.IsTitheApplicable);
    }

    [Fact]
    public async Task RecordRepaymentAsync_Payable_CreatesDebtRepaymentTransaction()
    {
        var debtRepository = new FakeDebtRepository();
        var transactionRepository = new FakeTransactionRepository();
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 5000m);
        debtRepository.Seed(debt);
        var service = new DebtRepaymentService(debtRepository, transactionRepository);

        var updated = await service.RecordRepaymentAsync(debt.Id, 2000m, PaymentMethod.Cash, new DateOnly(2026, 7, 15));

        Assert.Equal(3000m, updated!.CurrentBalance);
        var transaction = Assert.Single(transactionRepository.Added);
        Assert.Equal(TransactionType.DebtRepayment, transaction.Type);
        Assert.Equal(2000m, transaction.Amount);
        Assert.Null(transaction.IsTitheApplicable);
    }

    [Fact]
    public async Task RecordRepaymentAsync_PartialPayment_TransactionReflectsOnlyPaidAmount()
    {
        var debtRepository = new FakeDebtRepository();
        var transactionRepository = new FakeTransactionRepository();
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 5000m);
        debtRepository.Seed(debt);
        var service = new DebtRepaymentService(debtRepository, transactionRepository);

        await service.RecordRepaymentAsync(debt.Id, 1200m, PaymentMethod.Cash, new DateOnly(2026, 7, 15));

        var transaction = Assert.Single(transactionRepository.Added);
        Assert.Equal(1200m, transaction.Amount); // not the original 5000, not the remaining balance
    }

    [Fact]
    public async Task RecordRepaymentAsync_UnknownDebtId_ReturnsNull()
    {
        var service = new DebtRepaymentService(new FakeDebtRepository(), new FakeTransactionRepository());

        var result = await service.RecordRepaymentAsync(Guid.NewGuid(), 100m, PaymentMethod.Cash, new DateOnly(2026, 7, 15));

        Assert.Null(result);
    }
}
