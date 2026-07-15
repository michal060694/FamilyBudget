namespace FamilyBudget.Core.Entities;

/// <summary>
/// A recurring monthly donation standing order (הוראת קבע) — a named, fixed amount that counts
/// toward every calendar month's fixed-donations deduction (FR-008a) until an optional end month,
/// rather than requiring a fresh transaction entry every month.
/// </summary>
public class FixedDonationStandingOrder
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Amount { get; private set; }
    public string? AmountFormula { get; private set; }

    /// <summary>The last calendar year/month this standing order still applies to. Null means it has no end date (ongoing indefinitely).</summary>
    public int? ValidUntilYear { get; private set; }
    public int? ValidUntilMonth { get; private set; }

    private FixedDonationStandingOrder()
    {
        Name = string.Empty;
    }

    public FixedDonationStandingOrder(
        Guid id,
        string name,
        decimal amount,
        int? validUntilYear,
        int? validUntilMonth,
        string? amountFormula = null)
    {
        Validate(name, amount, validUntilYear, validUntilMonth);

        Id = id;
        Name = name;
        Amount = amount;
        AmountFormula = amountFormula;
        ValidUntilYear = validUntilYear;
        ValidUntilMonth = validUntilMonth;
    }

    public void Update(string name, decimal amount, int? validUntilYear, int? validUntilMonth, string? amountFormula = null)
    {
        Validate(name, amount, validUntilYear, validUntilMonth);

        Name = name;
        Amount = amount;
        AmountFormula = amountFormula;
        ValidUntilYear = validUntilYear;
        ValidUntilMonth = validUntilMonth;
    }

    /// <summary>Whether this standing order is still active during the given calendar month.</summary>
    public bool AppliesTo(int year, int month)
    {
        if (ValidUntilYear is null)
        {
            return true;
        }

        var targetIndex = (year * 12) + month;
        var validUntilIndex = (ValidUntilYear.Value * 12) + ValidUntilMonth!.Value;
        return targetIndex <= validUntilIndex;
    }

    private static void Validate(string name, decimal amount, int? validUntilYear, int? validUntilMonth)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be strictly positive.");
        }

        if (validUntilYear is null != validUntilMonth is null)
        {
            throw new ArgumentException(
                "ValidUntilYear and ValidUntilMonth must both be set or both be null.", nameof(validUntilMonth));
        }

        if (validUntilMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(validUntilMonth), "ValidUntilMonth must be between 1 and 12 when specified.");
        }
    }
}
