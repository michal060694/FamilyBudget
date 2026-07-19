using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class AnnualBudgetSummaryTests
{
    [Fact]
    public async Task GetSummaryAsync_NotYetCoveredIsSumOfPerItemRemainingBalances_NotNettedAgainstReserve()
    {
        // Item A: 30000 total, 10000 already used -> 20000 remaining. Item B: 18000 total, 8000
        // used -> 10000 remaining. NotYetCovered = 20000 + 10000 = 30000, exactly matching what
        // the per-item "יתרה לנצול" column would sum to. reserveOnHand (12000) is reported as its
        // own figure and must NOT be subtracted from NotYetCovered itself -- that was the old
        // (wrong) "funding gap" reading; this field means "not yet used," not "not yet saved for."
        // The reserve DOES reduce the monthly deposit pace though: 30000 needed - 12000 already
        // sitting in reserve = 18000 still to save up -> 18000 / 12 = 1500/month.
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 30000m, null, 0m, amountUsed: 10000m),
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item B", 18000m, 6, 0m, amountUsed: 8000m),
        });
        var reserves = new FakeReserveRepository(12000m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 1, currentDay: 1);

        Assert.Equal(12000m, summary.ReserveOnHand);
        Assert.Equal(48000m, summary.TotalAnnualBudget);
        Assert.Equal(30000m, summary.NotYetCovered);
        Assert.Equal(1500m, summary.MonthlyAllocation); // (30000 - 12000 reserve) / 12 months
    }

    [Fact]
    public async Task GetSummaryAsync_ReserveAlreadyCoversTheNeed_MonthlyAllocationIsZero()
    {
        // 8000 needed for the year, 10000 already sitting in the reserve -> nothing left to
        // deposit. The reserve covering the gap fully must not go negative and wrap around.
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 8000m, null, 0m),
        });
        var reserves = new FakeReserveRepository(10000m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 1, currentDay: 1);

        Assert.Equal(10000m, summary.ReserveOnHand);
        Assert.Equal(8000m, summary.NotYetCovered);
        Assert.Equal(0m, summary.MonthlyAllocation);
    }

    [Fact]
    public async Task GetSummaryAsync_MonthlyAllocationSpreadsOverMonthsRemainingToYearEnd()
    {
        // 36K not yet covered, current month = October 1st -> 3 deposit months remaining
        // (Oct, Nov, Dec) -> 36000 / 3 = 12000/month, not the flat 36000 / 12 = 3000/month.
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 36000m, null, 0m),
        });
        var reserves = new FakeReserveRepository(0m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 10, currentDay: 1);

        Assert.Equal(36000m, summary.NotYetCovered);
        Assert.Equal(12000m, summary.MonthlyAllocation);
    }

    [Fact]
    public async Task GetSummaryAsync_PastTheFirstOfTheMonth_ExcludesCurrentMonthFromDepositsRemaining()
    {
        // Household deposits happen on the 1st of each month. Checking on July 16th means July's
        // own deposit opportunity already passed -> only Aug, Sep, Oct, Nov, Dec remain (5 months),
        // not 6 (which is what a same-month-inclusive count would wrongly give).
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 30000m, null, 0m),
        });
        var reserves = new FakeReserveRepository(0m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 7, currentDay: 16);

        Assert.Equal(30000m, summary.NotYetCovered);
        Assert.Equal(6000m, summary.MonthlyAllocation); // 30000 / 5 remaining deposit months
    }

    [Fact]
    public async Task GetSummaryAsync_PastDecemberFirst_FallsBackToOneMonthInsteadOfDividingByZero()
    {
        // Checking on December 15th: December's own deposit opportunity is gone and there are no
        // more months left this year. Rather than divide by zero, treat it as "due now" (1 month).
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 6000m, null, 0m),
        });
        var reserves = new FakeReserveRepository(0m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 12, currentDay: 15);

        Assert.Equal(6000m, summary.NotYetCovered);
        Assert.Equal(6000m, summary.MonthlyAllocation);
    }

    [Fact]
    public async Task GetSummaryAsync_ItemOverrun_AddsToNotYetCoveredInsteadOfCancelingIt()
    {
        // A 250 budget with 800 already used is 550 *overrun* -- unplanned money already spent
        // that the household still needs to find. It must show up as +550, not as a -550 that
        // silently reduces the total (which is the bug this test guards against).
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 250m, null, 0m, amountUsed: 800m),
        });
        var reserves = new FakeReserveRepository(0m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 1, currentDay: 1);

        Assert.Equal(550m, summary.NotYetCovered);
        Assert.Equal(550m / 12, summary.MonthlyAllocation);
    }

    [Fact]
    public async Task GetSummaryAsync_OverrunOnOneItem_DoesNotShrinkAnotherItemsRemainingBalance()
    {
        // Item A still has 800 left to spend; Item B overran by 550. The household needs both:
        // 800 (still to spend on A) + 550 (to cover B's overrun) = 1350, not 800 - 550 = 250.
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 1000m, null, 0m, amountUsed: 200m),
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item B", 250m, null, 0m, amountUsed: 800m),
        });
        var reserves = new FakeReserveRepository(0m);
        var service = new AnnualBudgetQueryService(items, reserves, new CalendarYearCycle());

        var summary = await service.GetSummaryAsync(2026, currentMonth: 1, currentDay: 1);

        Assert.Equal(1350m, summary.NotYetCovered);
    }

    private sealed class FakeItemRepository : IAnnualBudgetItemRepository
    {
        private readonly IReadOnlyList<AnnualBudgetItem> _items;

        public FakeItemRepository(IReadOnlyList<AnnualBudgetItem> items) => _items = items;

        public Task<IReadOnlyList<AnnualBudgetItem>> GetByYearAsync(int year, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items);

        public Task AddAsync(AnnualBudgetItem item, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AnnualBudgetItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeReserveRepository : IAnnualReserveRepository
    {
        private readonly decimal _amount;

        public FakeReserveRepository(decimal amount) => _amount = amount;

        public Task<decimal> GetAmountAsync(int year, CancellationToken cancellationToken = default) =>
            Task.FromResult(_amount);

        public Task<string?> GetFormulaAsync(int year, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task SetAmountAsync(int year, decimal amount, string? amountFormula = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
