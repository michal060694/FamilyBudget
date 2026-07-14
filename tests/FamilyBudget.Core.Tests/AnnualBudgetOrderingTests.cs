using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class AnnualBudgetOrderingTests
{
    [Fact]
    public void OrderForDisplay_MonthMappedItemsComeFirst_SortedFromJanuary_GeneralItemsLast()
    {
        var carTest = new AnnualBudgetItem(Guid.NewGuid(), 2026, "Car Test", 600m, null, 0m);
        var springTrip = new AnnualBudgetItem(Guid.NewGuid(), 2026, "Spring Trip", 2000m, 4, 0m);
        var holidays = new AnnualBudgetItem(Guid.NewGuid(), 2026, "December Holidays", 4000m, 12, 1000m);
        var insurance = new AnnualBudgetItem(Guid.NewGuid(), 2026, "Insurance", 1200m, null, 0m);

        var ordered = AnnualBudgetQueryService.OrderForDisplay(new[] { carTest, holidays, springTrip, insurance });

        Assert.Equal(new[] { springTrip, holidays, carTest, insurance }, ordered);
    }

    [Fact]
    public void OrderForDisplay_FullyFundedItem_IsFlaggedFullyFunded()
    {
        var fullyFunded = new AnnualBudgetItem(Guid.NewGuid(), 2026, "Fully Funded", 500m, 3, 500m);

        Assert.True(fullyFunded.IsFullyFunded);
    }
}
