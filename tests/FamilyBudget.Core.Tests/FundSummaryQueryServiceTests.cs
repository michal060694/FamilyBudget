using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class FundSummaryQueryServiceTests
{
    [Fact]
    public void BuildSummary_EarmarksMatchBalance_DiscrepancyIsZero()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 15000m);
        var earmarks = new List<FundEarmark>
        {
            new(Guid.NewGuid(), fund.Id, "פאה", 10000m),
            new(Guid.NewGuid(), fund.Id, "שנתי 26", 5000m),
        };

        var summary = FundSummaryQueryService.BuildSummary(fund, earmarks);

        Assert.Equal(15000m, summary.EarmarkedTotal);
        Assert.Equal(0m, summary.Discrepancy);
    }

    [Fact]
    public void BuildSummary_EarmarksLessThanBalance_PositiveDiscrepancy_UnearmarkedRemainder()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 15000m);
        var earmarks = new List<FundEarmark> { new(Guid.NewGuid(), fund.Id, "פאה", 10000m) };

        var summary = FundSummaryQueryService.BuildSummary(fund, earmarks);

        Assert.Equal(5000m, summary.Discrepancy); // 15000 - 10000, still to earmark
    }

    [Fact]
    public void BuildSummary_EarmarksExceedBalance_NegativeDiscrepancy_FlaggedMismatch()
    {
        var fund = new Fund(Guid.NewGuid(), "Meitav", 15000m);
        var earmarks = new List<FundEarmark>
        {
            new(Guid.NewGuid(), fund.Id, "פאה", 10000m),
            new(Guid.NewGuid(), fund.Id, "שנתי 26", 8000m),
        };

        var summary = FundSummaryQueryService.BuildSummary(fund, earmarks);

        Assert.Equal(-3000m, summary.Discrepancy); // 15000 - 18000, over-earmarked
    }

    [Fact]
    public void BuildSummary_NoEarmarks_DiscrepancyEqualsFullBalance()
    {
        var fund = new Fund(Guid.NewGuid(), "IBI", 8000m);

        var summary = FundSummaryQueryService.BuildSummary(fund, new List<FundEarmark>());

        Assert.Equal(0m, summary.EarmarkedTotal);
        Assert.Equal(8000m, summary.Discrepancy);
        Assert.Empty(summary.Earmarks);
    }
}
