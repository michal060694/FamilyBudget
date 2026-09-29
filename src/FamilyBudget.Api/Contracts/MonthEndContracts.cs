using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Contracts;

public record MonthEndItemView(Guid Id, int Year, int Month, MonthEndDirection Direction, string Name, decimal Amount);

public record CreateMonthEndItemRequest(int Year, int Month, MonthEndDirection Direction, string Name, decimal Amount);

public record UpdateMonthEndItemRequest(string Name, decimal Amount);