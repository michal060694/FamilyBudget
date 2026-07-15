using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class FundTests
{
    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Fund(Guid.NewGuid(), "  ", 1000m));
    }

    [Fact]
    public void Constructor_NegativeBalance_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fund(Guid.NewGuid(), "Meitav", -1m));
    }

    [Fact]
    public void Constructor_ZeroBalance_IsAllowed()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 0m);

        Assert.Equal(0m, fund.TotalBalance);
    }

    [Fact]
    public void Rename_UpdatesName()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 1000m);

        fund.Rename("IBI");

        Assert.Equal("IBI", fund.Name);
    }

    [Fact]
    public void SetTotalBalance_OverwritesToExactValue()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 1000m);

        fund.SetTotalBalance(1500m);

        Assert.Equal(1500m, fund.TotalBalance);
    }

    [Fact]
    public void SetTotalBalance_Negative_Throws()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 1000m);

        Assert.Throws<ArgumentOutOfRangeException>(() => fund.SetTotalBalance(-1m));
    }
}
