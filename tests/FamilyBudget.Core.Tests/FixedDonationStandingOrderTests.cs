using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class FixedDonationStandingOrderTests
{
    [Fact]
    public void AppliesTo_NoEndDate_AlwaysApplies()
    {
        var order = new FixedDonationStandingOrder(Guid.NewGuid(), "Yeshiva", 100m, null, null);

        Assert.True(order.AppliesTo(2026, 1));
        Assert.True(order.AppliesTo(2030, 12));
    }

    [Fact]
    public void AppliesTo_BeforeOrOnEndMonth_Applies()
    {
        var order = new FixedDonationStandingOrder(Guid.NewGuid(), "Yeshiva", 100m, 2026, 6);

        Assert.True(order.AppliesTo(2026, 5));
        Assert.True(order.AppliesTo(2026, 6));
    }

    [Fact]
    public void AppliesTo_AfterEndMonth_DoesNotApply()
    {
        var order = new FixedDonationStandingOrder(Guid.NewGuid(), "Yeshiva", 100m, 2026, 6);

        Assert.False(order.AppliesTo(2026, 7));
    }

    [Fact]
    public void Constructor_OnlyOneOfValidUntilYearMonthSet_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FixedDonationStandingOrder(Guid.NewGuid(), "X", 100m, 2026, null));
        Assert.Throws<ArgumentException>(() => new FixedDonationStandingOrder(Guid.NewGuid(), "X", 100m, null, 6));
    }

    [Fact]
    public void Constructor_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FixedDonationStandingOrder(Guid.NewGuid(), "X", 0m, null, null));
    }

    [Fact]
    public void Update_ChangesEndDate()
    {
        var order = new FixedDonationStandingOrder(Guid.NewGuid(), "Yeshiva", 100m, null, null);

        order.Update("Yeshiva", 150m, 2026, 3);

        Assert.Equal(150m, order.Amount);
        Assert.True(order.AppliesTo(2026, 3));
        Assert.False(order.AppliesTo(2026, 4));
    }
}
