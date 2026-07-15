using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class TransactionValidationTests
{
    [Fact]
    public void Constructor_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transaction(
            Guid.NewGuid(), new DateOnly(2026, 7, 15), 0m, TransactionType.RegularExpense, PaymentMethod.Cash, isTitheApplicable: null));
    }

    [Fact]
    public void Constructor_IncomeWithoutTitheApplicableFlag_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            Guid.NewGuid(), new DateOnly(2026, 7, 15), 100m, TransactionType.Income, PaymentMethod.BankTransfer, isTitheApplicable: null));

        Assert.Equal("isTitheApplicable", ex.ParamName);
    }

    [Fact]
    public void Constructor_NonIncomeWithTitheApplicableFlag_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            Guid.NewGuid(), new DateOnly(2026, 7, 15), 100m, TransactionType.RegularExpense, PaymentMethod.Cash, isTitheApplicable: true));

        Assert.Equal("isTitheApplicable", ex.ParamName);
    }

    [Fact]
    public void Constructor_IncomeWithTitheApplicableFlag_Succeeds()
    {
        var transaction = new Transaction(
            Guid.NewGuid(), new DateOnly(2026, 7, 15), 500m, TransactionType.Income, PaymentMethod.BankTransfer, isTitheApplicable: true, "Salary");

        Assert.True(transaction.IsTitheApplicable);
        Assert.Equal(500m, transaction.Amount);
    }

    [Fact]
    public void Update_RevalidatesInvariants()
    {
        var transaction = new Transaction(
            Guid.NewGuid(), new DateOnly(2026, 7, 15), 500m, TransactionType.Income, PaymentMethod.BankTransfer, isTitheApplicable: true);

        Assert.Throws<ArgumentException>(() => transaction.Update(
            new DateOnly(2026, 7, 16), 500m, TransactionType.Income, PaymentMethod.Cash, isTitheApplicable: null, description: null));
    }
}
