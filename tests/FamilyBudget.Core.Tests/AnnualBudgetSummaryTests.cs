using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class AnnualBudgetSummaryTests
{
    [Fact]
    public async Task GetSummaryAsync_ReserveReducesFlatMonthlyAllocation()
    {
        // Matches the user-provided example: 48K total annual budget, 12K reserve on hand ->
        // (48000 - 12000) / 12 = 3000/month.
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 30000m, null, 0m),
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item B", 18000m, 6, 0m),
        });
        var reserves = new FakeReserveRepository(12000m);
        var service = new AnnualBudgetQueryService(items, reserves);

        var summary = await service.GetSummaryAsync(2026);

        Assert.Equal(12000m, summary.ReserveOnHand);
        Assert.Equal(48000m, summary.TotalAnnualBudget);
        Assert.Equal(36000m, summary.NotYetCovered);
        Assert.Equal(3000m, summary.MonthlyAllocation);
    }

    [Fact]
    public async Task GetSummaryAsync_ReserveExceedsTotalBudget_NotYetCoveredNeverNegative()
    {
        var items = new FakeItemRepository(new[]
        {
            new AnnualBudgetItem(Guid.NewGuid(), 2026, "Item A", 1000m, null, 0m),
        });
        var reserves = new FakeReserveRepository(5000m);
        var service = new AnnualBudgetQueryService(items, reserves);

        var summary = await service.GetSummaryAsync(2026);

        Assert.Equal(0m, summary.NotYetCovered);
        Assert.Equal(0m, summary.MonthlyAllocation);
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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeReserveRepository : IAnnualReserveRepository
    {
        private readonly decimal _amount;

        public FakeReserveRepository(decimal amount) => _amount = amount;

        public Task<decimal> GetAmountAsync(int year, CancellationToken cancellationToken = default) =>
            Task.FromResult(_amount);

        public Task SetAmountAsync(int year, decimal amount, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
