using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;
using FamilyBudget.Core.Services;

namespace FamilyBudget.Core.Tests;

public class AnnualBudgetCopyServiceTests
{
    [Fact]
    public async Task CopyYearAsync_CopiesAllSourceItems_ResettingUsageAndSetAside()
    {
        var repository = new FakeAnnualBudgetItemRepository();
        await repository.AddAsync(new AnnualBudgetItem(
            Guid.NewGuid(), 2025, "Car Insurance", 1200m, null, amountAlreadySetAside: 600m, amountUsed: 1200m));
        await repository.AddAsync(new AnnualBudgetItem(
            Guid.NewGuid(), 2025, "Passover", 2000m, 7, amountAlreadySetAside: 0m));
        var service = new AnnualBudgetCopyService(repository);

        var (copied, skipped) = await service.CopyYearAsync(2025, 2026);

        Assert.Equal(2, copied);
        Assert.Equal(0, skipped);

        var targetItems = await repository.GetByYearAsync(2026);
        Assert.Equal(2, targetItems.Count);

        var carInsurance = Assert.Single(targetItems, i => i.Name == "Car Insurance");
        Assert.Equal(1200m, carInsurance.TotalAmount);
        Assert.Equal(0m, carInsurance.AmountAlreadySetAside);
        Assert.Equal(0m, carInsurance.AmountUsed);

        var passover = Assert.Single(targetItems, i => i.Name == "Passover");
        Assert.Equal(7, passover.TargetMonth);
    }

    [Fact]
    public async Task CopyYearAsync_SkipsItemsAlreadyPresentInTargetYearByName()
    {
        var repository = new FakeAnnualBudgetItemRepository();
        await repository.AddAsync(new AnnualBudgetItem(Guid.NewGuid(), 2025, "Rent", 12000m, null, amountAlreadySetAside: 0m));
        await repository.AddAsync(new AnnualBudgetItem(Guid.NewGuid(), 2025, "Vacation", 3000m, null, amountAlreadySetAside: 0m));
        await repository.AddAsync(new AnnualBudgetItem(Guid.NewGuid(), 2026, "Rent", 12500m, null, amountAlreadySetAside: 0m));
        var service = new AnnualBudgetCopyService(repository);

        var (copied, skipped) = await service.CopyYearAsync(2025, 2026);

        Assert.Equal(1, copied);
        Assert.Equal(1, skipped);

        var targetItems = await repository.GetByYearAsync(2026);
        Assert.Equal(2, targetItems.Count);
        Assert.Equal(12500m, Assert.Single(targetItems, i => i.Name == "Rent").TotalAmount); // pre-existing row untouched
    }

    [Fact]
    public async Task CopyYearAsync_CalledTwice_IsIdempotent()
    {
        var repository = new FakeAnnualBudgetItemRepository();
        await repository.AddAsync(new AnnualBudgetItem(Guid.NewGuid(), 2025, "Rent", 12000m, null, amountAlreadySetAside: 0m));
        var service = new AnnualBudgetCopyService(repository);

        await service.CopyYearAsync(2025, 2026);
        var (copied, skipped) = await service.CopyYearAsync(2025, 2026);

        Assert.Equal(0, copied);
        Assert.Equal(1, skipped);
        Assert.Single(await repository.GetByYearAsync(2026));
    }

    private sealed class FakeAnnualBudgetItemRepository : IAnnualBudgetItemRepository
    {
        private readonly List<AnnualBudgetItem> _items = [];

        public Task<IReadOnlyList<AnnualBudgetItem>> GetByYearAsync(int year, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AnnualBudgetItem>>(_items.Where(i => i.Year == year).ToList());

        public Task<AnnualBudgetItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(AnnualBudgetItem item, CancellationToken cancellationToken = default)
        {
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
