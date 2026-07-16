namespace FamilyBudget.Core.Entities;

/// <summary>
/// A named purpose-allocation line within a <see cref="Fund"/> (e.g., "פאה", "שנתי 26",
/// "חיסכון כללי") — spec.md Key Entities; FR-005-FR-008.
/// </summary>
public class FundEarmark
{
    public Guid Id { get; private set; }
    public Guid FundId { get; private set; }
    public string PurposeLabel { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>Raw formula the user typed for <see cref="Amount"/> (e.g. "100+500"), or null if entered as a plain number.</summary>
    public string? AmountFormula { get; private set; }

    private FundEarmark()
    {
        PurposeLabel = string.Empty;
    }

    public FundEarmark(Guid id, Guid fundId, string purposeLabel, decimal amount, string? amountFormula = null)
    {
        Validate(purposeLabel, amount);

        Id = id;
        FundId = fundId;
        PurposeLabel = purposeLabel;
        Amount = amount;
        AmountFormula = amountFormula;
    }

    public void Update(string purposeLabel, decimal amount, string? amountFormula = null)
    {
        Validate(purposeLabel, amount);
        PurposeLabel = purposeLabel;
        Amount = amount;
        AmountFormula = amountFormula;
    }

    private static void Validate(string purposeLabel, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(purposeLabel))
        {
            throw new ArgumentException("PurposeLabel must not be empty.", nameof(purposeLabel));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be strictly positive.");
        }
    }
}
