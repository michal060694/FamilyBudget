namespace FamilyBudget.Api.Contracts;

public record FundEarmarkResponse(Guid Id, string PurposeLabel, decimal Amount, string? AmountFormula);

public record FundSummaryResponse(
    Guid FundId,
    string Name,
    decimal TotalBalance,
    decimal EarmarkedTotal,
    decimal Discrepancy,
    IReadOnlyList<FundEarmarkResponse> Earmarks);

public record FundListResponse(IReadOnlyList<FundSummaryResponse> Funds);

public record CreateFundRequest(string Name, decimal TotalBalance);

public record UpdateFundRequest(string Name, decimal TotalBalance);

public record CreateFundEarmarkRequest(string PurposeLabel, decimal Amount, string? AmountFormula = null);

public record UpdateFundEarmarkRequest(string PurposeLabel, decimal Amount, string? AmountFormula = null);
