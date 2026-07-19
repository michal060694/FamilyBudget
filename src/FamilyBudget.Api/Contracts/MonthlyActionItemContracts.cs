namespace FamilyBudget.Api.Contracts;

public record MonthlyActionItemView(
    Guid Id, int Year, int Month, string Description, decimal? Amount, DateOnly? DeadlineDate, bool IsCompleted, bool IsOverdue);

public record MonthlyActionItemListResponse(int Year, int Month, IReadOnlyList<MonthlyActionItemView> Items);

public record CreateMonthlyActionItemRequest(int Year, int Month, string Description, decimal? Amount = null, DateOnly? DeadlineDate = null);

public record UpdateMonthlyActionItemRequest(string Description, decimal? Amount, DateOnly? DeadlineDate);

public record SetMonthlyActionItemCompleteRequest(bool IsCompleted);

public record SendRemindersNowResponse(int RemindersSent);
