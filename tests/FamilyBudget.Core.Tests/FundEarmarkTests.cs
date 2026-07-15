using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class FundEarmarkTests
{
    [Fact]
    public void Constructor_EmptyPurposeLabel_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FundEarmark(Guid.NewGuid(), Guid.NewGuid(), "  ", 100m));
    }

    [Fact]
    public void Constructor_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FundEarmark(Guid.NewGuid(), Guid.NewGuid(), "פאה", 0m));
    }

    [Fact]
    public void Update_ChangesPurposeLabelAndAmount()
    {
        var earmark = new FundEarmark(Guid.NewGuid(), Guid.NewGuid(), "פאה", 10000m);

        earmark.Update("שנתי 26", 5000m);

        Assert.Equal("שנתי 26", earmark.PurposeLabel);
        Assert.Equal(5000m, earmark.Amount);
    }
}
