using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Services;

/// <summary>
/// Assembles the per-fund summary view (User Story 2/3; FR-004, FR-008-FR-012), computing the
/// earmarked-sum-vs-balance reconciliation fresh on every read (no persisted snapshot — see
/// research.md).
/// </summary>
public class FundSummaryQueryService
{
    private readonly IFundRepository _fundRepository;
    private readonly IFundEarmarkRepository _earmarkRepository;

    public FundSummaryQueryService(IFundRepository fundRepository, IFundEarmarkRepository earmarkRepository)
    {
        _fundRepository = fundRepository;
        _earmarkRepository = earmarkRepository;
    }

    public async Task<IReadOnlyList<FundSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var funds = await _fundRepository.GetAllAsync(cancellationToken);
        var summaries = new List<FundSummary>(funds.Count);

        foreach (var fund in funds)
        {
            var earmarks = await _earmarkRepository.GetByFundIdAsync(fund.Id, cancellationToken);
            summaries.Add(BuildSummary(fund, earmarks));
        }

        return summaries;
    }

    /// <summary>Pure assembly, independently testable without a repository or database.</summary>
    public static FundSummary BuildSummary(Fund fund, IReadOnlyList<FundEarmark> earmarks)
    {
        var earmarkedTotal = earmarks.Sum(e => e.Amount);
        var discrepancy = fund.TotalBalance - earmarkedTotal;

        return new FundSummary(fund.Id, fund.Name, fund.TotalBalance, earmarks, earmarkedTotal, discrepancy);
    }
}

/// <summary>
/// A fund's balance alongside its earmark breakdown and the reconciliation discrepancy: <c>0</c>
/// means fully reconciled, <c>&gt; 0</c> means an unearmarked remainder, <c>&lt; 0</c> means an
/// over-earmarked mismatch that must be flagged (FR-010).
/// </summary>
public record FundSummary(
    Guid FundId,
    string Name,
    decimal TotalBalance,
    IReadOnlyList<FundEarmark> Earmarks,
    decimal EarmarkedTotal,
    decimal Discrepancy);
