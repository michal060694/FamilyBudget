namespace FamilyBudget.Api.Contracts;

public record FixedDonationStandingOrderView(
    Guid Id,
    string Name,
    decimal Amount,
    string? AmountFormula,
    int? ValidUntilYear,
    int? ValidUntilMonth);

public record FixedDonationStandingOrderListResponse(
    int Year, int Month, IReadOnlyList<FixedDonationStandingOrderView> Items, decimal Total);

public record CreateFixedDonationStandingOrderRequest(
    string Name,
    decimal Amount,
    int? ValidUntilYear,
    int? ValidUntilMonth,
    string? AmountFormula = null);

public record UpdateFixedDonationStandingOrderRequest(
    string Name,
    decimal Amount,
    int? ValidUntilYear,
    int? ValidUntilMonth,
    string? AmountFormula = null);
