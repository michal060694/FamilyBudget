using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class MonthlyExpenseBudgetItemTests
{
    [Fact]
    public void Constructor_ComputesRemaining_AsBudgetedMinusUsed()
    {
        var item = new MonthlyExpenseBudgetItem(
            Guid.NewGuid(), 2026, 7, "Rent", TransactionType.FixedExpense, budgetedAmount: 400m, usedAmount: 150m);

        Assert.Equal(250m, item.Remaining);
    }

    [Theory]
    [InlineData(TransactionType.Income)]
    [InlineData(TransactionType.FixedDonation)]
    [InlineData(TransactionType.SmallCharityExpense)]
    public void Constructor_DisallowsTypesOtherThanFixedOrRegularExpense(TransactionType type)
    {
        Assert.Throws<ArgumentException>(() => new MonthlyExpenseBudgetItem(
            Guid.NewGuid(), 2026, 7, "Something", type, budgetedAmount: 100m));
    }

    [Fact]
    public void Constructor_InvalidMonth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthlyExpenseBudgetItem(
            Guid.NewGuid(), 2026, 13, "Something", TransactionType.FixedExpense, budgetedAmount: 100m));
    }

    [Fact]
    public void SetUsedAmount_OverwritesToExactValue_AndUpdatesRemaining()
    {
        var item = new MonthlyExpenseBudgetItem(
            Guid.NewGuid(), 2026, 7, "Groceries", TransactionType.RegularExpense, budgetedAmount: 600m);

        item.SetUsedAmount(400m, "600-200");

        Assert.Equal(400m, item.UsedAmount);
        Assert.Equal("600-200", item.UsedAmountFormula);
        Assert.Equal(200m, item.Remaining);
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var item = new MonthlyExpenseBudgetItem(
            Guid.NewGuid(), 2026, 7, "Groceries", TransactionType.RegularExpense, budgetedAmount: 600m);

        Assert.Throws<ArgumentException>(() => item.Rename("  "));
    }
}
