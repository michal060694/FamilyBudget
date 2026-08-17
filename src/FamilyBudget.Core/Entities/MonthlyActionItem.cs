namespace FamilyBudget.Core.Entities;

/// <summary>
/// A manually-added to-do item for a given calendar month (e.g. "שלחי כסף לתרומה"), independent of
/// any Transaction/Fund/Debt record — a pure checklist. An optional <see cref="DeadlineDate"/> drives
/// the overdue-reminder email; <see cref="ReminderSentAt"/> tracks whether that email has already
/// gone out for this item, so a reminder fires once per overdue item rather than on every check.
/// </summary>
public class MonthlyActionItem
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Description { get; private set; }
    public decimal? Amount { get; private set; }
    public DateOnly? DeadlineDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateOnly? CompletedDate { get; private set; }
    public DateOnly? ReminderSentAt { get; private set; }

    private MonthlyActionItem()
    {
        Description = string.Empty;
    }

    public MonthlyActionItem(Guid id, int year, int month, string description, decimal? amount = null, DateOnly? deadlineDate = null)
    {
        ValidateMonth(month);
        ValidateDescription(description);
        ValidateAmount(amount);

        Id = id;
        Year = year;
        Month = month;
        Description = description;
        Amount = amount;
        DeadlineDate = deadlineDate;
        IsCompleted = false;
        CompletedDate = null;
        ReminderSentAt = null;
    }

    public void Rename(string description)
    {
        ValidateDescription(description);
        Description = description;
    }

    public void SetAmount(decimal? amount)
    {
        ValidateAmount(amount);
        Amount = amount;
    }

    public void SetDeadline(DateOnly? deadlineDate)
    {
        DeadlineDate = deadlineDate;
    }

    /// <summary>
    /// <paramref name="completedDate"/> is the "approval"/completion date shown in the history view;
    /// it is only kept while <paramref name="isCompleted"/> is true, and cleared when an item is
    /// reopened.
    /// </summary>
    public void SetCompleted(bool isCompleted, DateOnly? completedDate = null)
    {
        IsCompleted = isCompleted;
        CompletedDate = isCompleted ? completedDate : null;
    }

    public void MarkReminderSent(DateOnly asOfDate)
    {
        ReminderSentAt = asOfDate;
    }

    private static void ValidateMonth(int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description must not be empty.", nameof(description));
        }
    }

    private static void ValidateAmount(decimal? amount)
    {
        if (amount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be >= 0 when specified.");
        }
    }
}
