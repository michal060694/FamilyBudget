using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class MonthlyActionItemTests
{
    [Fact]
    public void Constructor_ValidInput_InitializesAsIncompleteWithNoReminderSent()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "שלחי כסף לתרומה", new DateOnly(2026, 7, 10));

        Assert.False(item.IsCompleted);
        Assert.Null(item.ReminderSentAt);
        Assert.Equal(new DateOnly(2026, 7, 10), item.DeadlineDate);
    }

    [Fact]
    public void Constructor_NoDeadline_AllowsNull()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "משהו כללי");

        Assert.Null(item.DeadlineDate);
    }

    [Fact]
    public void Constructor_InvalidMonth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonthlyActionItem(Guid.NewGuid(), 2026, 13, "משהו"));
    }

    [Fact]
    public void Constructor_EmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "   "));
    }

    [Fact]
    public void Rename_EmptyDescription_Throws()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "משהו");

        Assert.Throws<ArgumentException>(() => item.Rename(" "));
    }

    [Fact]
    public void Rename_ValidDescription_Updates()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "משהו");

        item.Rename("תיאור מעודכן");

        Assert.Equal("תיאור מעודכן", item.Description);
    }

    [Fact]
    public void SetDeadline_CanClearToNull()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "משהו", new DateOnly(2026, 7, 10));

        item.SetDeadline(null);

        Assert.Null(item.DeadlineDate);
    }

    [Fact]
    public void SetCompleted_TogglesFlag()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "משהו");

        item.SetCompleted(true);
        Assert.True(item.IsCompleted);

        item.SetCompleted(false);
        Assert.False(item.IsCompleted);
    }

    [Fact]
    public void MarkReminderSent_SetsReminderSentAt()
    {
        var item = new MonthlyActionItem(Guid.NewGuid(), 2026, 7, "משהו", new DateOnly(2026, 7, 1));

        item.MarkReminderSent(new DateOnly(2026, 7, 5));

        Assert.Equal(new DateOnly(2026, 7, 5), item.ReminderSentAt);
    }
}
