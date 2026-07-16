using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class MonthlyTemplateItemTests
{
    [Fact]
    public void Constructor_Income_RequiresIsTitheApplicable()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonthlyTemplateItem(Guid.NewGuid(), TransactionType.Income, "Salary", 5000m, isTitheApplicable: null));
    }

    [Fact]
    public void Constructor_NonIncome_RejectsIsTitheApplicable()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonthlyTemplateItem(Guid.NewGuid(), TransactionType.FixedExpense, "Arnona", 500m, isTitheApplicable: true));
    }

    [Theory]
    [InlineData(TransactionType.FixedDonation)]
    [InlineData(TransactionType.SmallCharityExpense)]
    [InlineData(TransactionType.DebtRepayment)]
    public void Constructor_UnsupportedType_Throws(TransactionType type)
    {
        Assert.Throws<ArgumentException>(() =>
            new MonthlyTemplateItem(Guid.NewGuid(), type, "X", 100m, isTitheApplicable: null));
    }

    [Fact]
    public void Constructor_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonthlyTemplateItem(Guid.NewGuid(), TransactionType.RegularExpense, "Groceries", 0m, isTitheApplicable: null));
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonthlyTemplateItem(Guid.NewGuid(), TransactionType.RegularExpense, " ", 100m, isTitheApplicable: null));
    }

    [Fact]
    public void Update_ChangesAmountAndKeepsType()
    {
        var item = new MonthlyTemplateItem(Guid.NewGuid(), TransactionType.FixedExpense, "Arnona", 500m, isTitheApplicable: null);

        item.Update("Arnona", 550m, isTitheApplicable: null);

        Assert.Equal(550m, item.Amount);
        Assert.Equal(TransactionType.FixedExpense, item.Type);
    }

    [Fact]
    public void Update_IncomeItem_RequiresIsTitheApplicable()
    {
        var item = new MonthlyTemplateItem(Guid.NewGuid(), TransactionType.Income, "Salary", 5000m, isTitheApplicable: true);

        Assert.Throws<ArgumentException>(() => item.Update("Salary", 5200m, isTitheApplicable: null));
    }
}
