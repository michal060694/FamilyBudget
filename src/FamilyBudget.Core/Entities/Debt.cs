namespace FamilyBudget.Core.Entities;

public enum DebtDirection
{
    /// <summary>Owed to the household (accounts receivable).</summary>
    Receivable,

    /// <summary>Owed by the household (accounts payable).</summary>
    Payable,
}

public enum DebtStatus
{
    Open,
    Closed,
}

/// <summary>
/// A single mutual-debt record, in either direction (spec.md Key Entities; FR-001-FR-009).
/// </summary>
public class Debt
{
    public Guid Id { get; private set; }
    public DebtDirection Direction { get; private set; }
    public string CounterpartyName { get; private set; }
    public decimal OriginalAmount { get; private set; }
    public decimal CurrentBalance { get; private set; }

    /// <summary>Raw formula the user typed for <see cref="CurrentBalance"/> via <see cref="SetCurrentBalance"/> (e.g. "1000-250"), or null if entered as a plain number or set by another path (repayment, edit).</summary>
    public string? CurrentBalanceFormula { get; private set; }

    public DebtStatus Status { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public decimal? RepaymentRate { get; private set; }
    public string? Notes { get; private set; }

    private Debt()
    {
        CounterpartyName = string.Empty;
    }

    public Debt(
        Guid id,
        DebtDirection direction,
        string counterpartyName,
        decimal originalAmount,
        DateOnly? targetDate = null,
        decimal? repaymentRate = null,
        string? notes = null)
    {
        Validate(counterpartyName, originalAmount, repaymentRate);

        Id = id;
        Direction = direction;
        CounterpartyName = counterpartyName;
        OriginalAmount = originalAmount;
        CurrentBalance = originalAmount;
        Status = DebtStatus.Open;
        TargetDate = targetDate;
        RepaymentRate = repaymentRate;
        Notes = notes;
    }

    /// <summary>
    /// Edits the debt's details, recomputing <see cref="CurrentBalance"/> so the amount already
    /// paid so far is preserved against the corrected original amount (research.md).
    /// </summary>
    public void Update(string counterpartyName, decimal originalAmount, DateOnly? targetDate, decimal? repaymentRate, string? notes)
    {
        Validate(counterpartyName, originalAmount, repaymentRate);

        var amountAlreadyPaid = OriginalAmount - CurrentBalance;

        CounterpartyName = counterpartyName;
        OriginalAmount = originalAmount;
        CurrentBalance = Math.Max(0m, originalAmount - amountAlreadyPaid);
        CurrentBalanceFormula = null;
        Status = CurrentBalance == 0m ? DebtStatus.Closed : DebtStatus.Open;
        TargetDate = targetDate;
        RepaymentRate = repaymentRate;
        Notes = notes;
    }

    /// <summary>Directly corrects the current balance (e.g. fixing a data-entry mistake), bypassing repayment tracking.</summary>
    public void SetCurrentBalance(decimal newBalance, string? formula = null)
    {
        if (newBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newBalance), "CurrentBalance must be >= 0.");
        }

        CurrentBalance = newBalance;
        CurrentBalanceFormula = formula;
        Status = newBalance == 0m ? DebtStatus.Closed : DebtStatus.Open;
    }

    /// <summary>Records a repayment, reducing the balance and auto-closing at zero (FR-006, FR-008, FR-009).</summary>
    public void RecordRepayment(decimal amount)
    {
        if (Status == DebtStatus.Closed)
        {
            throw new InvalidOperationException("Cannot record a repayment against an already-closed debt.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Repayment amount must be strictly positive.");
        }

        if (amount > CurrentBalance)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Repayment amount must not exceed the current balance.");
        }

        CurrentBalance -= amount;
        CurrentBalanceFormula = null;
        if (CurrentBalance == 0m)
        {
            Status = DebtStatus.Closed;
        }
    }

    private static void Validate(string counterpartyName, decimal originalAmount, decimal? repaymentRate)
    {
        if (string.IsNullOrWhiteSpace(counterpartyName))
        {
            throw new ArgumentException("CounterpartyName must not be empty.", nameof(counterpartyName));
        }

        if (originalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalAmount), "OriginalAmount must be strictly positive.");
        }

        if (repaymentRate is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repaymentRate), "RepaymentRate must be strictly positive when specified.");
        }
    }
}
