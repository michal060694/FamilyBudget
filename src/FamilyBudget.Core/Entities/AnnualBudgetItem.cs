namespace FamilyBudget.Core.Entities;

/// <summary>
/// A single planned annual expense for a Gregorian calendar year (spec.md Key Entities).
/// </summary>
public class AnnualBudgetItem
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public string Name { get; private set; }
    public decimal TotalAmount { get; private set; }

    /// <summary>Raw formula the user typed for <see cref="TotalAmount"/> (e.g. "600-200"), or null if entered as a plain number.</summary>
    public string? TotalAmountFormula { get; private set; }

    /// <summary>1-12 Gregorian month (January=1), or null for general/month-independent items.</summary>
    public int? TargetMonth { get; private set; }

    /// <summary>Amount already deposited/reserved toward this item — not amount spent.</summary>
    public decimal AmountAlreadySetAside { get; private set; }

    /// <summary>Actual amount spent against this item so far. Overrun (FR-006) is AmountUsed &gt; TotalAmount.</summary>
    public decimal AmountUsed { get; private set; }

    /// <summary>Raw formula the user typed for <see cref="AmountUsed"/>, or null if entered as a plain number.</summary>
    public string? AmountUsedFormula { get; private set; }

    public string? Notes { get; private set; }

    private AnnualBudgetItem()
    {
        Name = string.Empty;
    }

    public AnnualBudgetItem(
        Guid id,
        int year,
        string name,
        decimal totalAmount,
        int? targetMonth,
        decimal amountAlreadySetAside,
        decimal amountUsed = 0,
        string? totalAmountFormula = null,
        string? amountUsedFormula = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (totalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "TotalAmount must be strictly positive.");
        }

        if (amountAlreadySetAside < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountAlreadySetAside), "AmountAlreadySetAside must be >= 0.");
        }

        if (amountUsed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountUsed), "AmountUsed must be >= 0.");
        }

        if (targetMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(targetMonth), "TargetMonth must be between 1 and 12 when specified.");
        }

        Id = id;
        Year = year;
        Name = name;
        TotalAmount = totalAmount;
        TotalAmountFormula = totalAmountFormula;
        TargetMonth = targetMonth;
        AmountAlreadySetAside = amountAlreadySetAside;
        AmountUsed = amountUsed;
        AmountUsedFormula = amountUsedFormula;
        Notes = notes;
    }

    /// <summary>Normal (non-overrun) smoothing requirement still owed toward the deposit target (FR-002).</summary>
    public decimal RemainingToDeposit => Math.Max(0, TotalAmount - AmountAlreadySetAside);

    /// <summary>Excess actually spent beyond the target amount (Story 3 / FR-006).</summary>
    public decimal Overrun => Math.Max(0, AmountUsed - TotalAmount);

    public bool IsFullyFunded => RemainingToDeposit == 0 && Overrun == 0;

    public void RecordSetAside(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposited amount must be >= 0.");
        }

        AmountAlreadySetAside += amount;
    }

    public void SetAmountUsed(decimal amount, string? formula = null)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Used amount must be >= 0.");
        }

        AmountUsed = amount;
        AmountUsedFormula = formula;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
    }
}
