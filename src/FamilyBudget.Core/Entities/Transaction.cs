namespace FamilyBudget.Core.Entities;

public enum TransactionType
{
    Income,
    FixedExpense,
    RegularExpense,
    FixedDonation,
    SmallCharityExpense,

    /// <summary>
    /// An outflow transaction auto-generated when a Payable debt's repayment is recorded (feature
    /// 004). Distinguishable from FixedExpense/RegularExpense so it can be summed separately for
    /// the Monthly Overview's debt-repayments figure.
    /// </summary>
    DebtRepayment,

    /// <summary>
    /// A manually-logged withdrawal from the annual reserve (קופה שנתית) during the month, entered
    /// by the user rather than auto-derived from Annual Budget items due this month.
    /// </summary>
    AnnualReserveWithdrawal,
}

public enum PaymentMethod
{
    CreditCard,
    BankTransfer,
    Cash,
    Check,
}

/// <summary>
/// A single recorded income or expense event (spec.md Key Entities; FR-001, FR-001a).
/// </summary>
public class Transaction
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>
    /// The raw arithmetic expression the user typed (e.g. "600-200"), if the amount was entered
    /// as a formula rather than a plain number. Null when the amount was entered directly.
    /// <see cref="Amount"/> always holds the computed result, which is what all calculations use.
    /// </summary>
    public string? AmountFormula { get; private set; }

    public TransactionType Type { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Required (non-null) when <see cref="Type"/> is Income; MUST be null otherwise (FR-001a).</summary>
    public bool? IsTitheApplicable { get; private set; }

    public string? Description { get; private set; }

    private Transaction()
    {
    }

    public Transaction(
        Guid id,
        DateOnly date,
        decimal amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        bool? isTitheApplicable,
        string? description = null,
        string? amountFormula = null)
    {
        Validate(amount, type, isTitheApplicable);

        Id = id;
        Date = date;
        Amount = amount;
        AmountFormula = amountFormula;
        Type = type;
        PaymentMethod = paymentMethod;
        IsTitheApplicable = isTitheApplicable;
        Description = description;
    }

    public void Update(
        DateOnly date,
        decimal amount,
        TransactionType type,
        PaymentMethod paymentMethod,
        bool? isTitheApplicable,
        string? description,
        string? amountFormula = null)
    {
        Validate(amount, type, isTitheApplicable);

        Date = date;
        Amount = amount;
        AmountFormula = amountFormula;
        Type = type;
        PaymentMethod = paymentMethod;
        IsTitheApplicable = isTitheApplicable;
        Description = description;
    }

    private static void Validate(decimal amount, TransactionType type, bool? isTitheApplicable)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be strictly positive.");
        }

        if (type == TransactionType.Income && isTitheApplicable is null)
        {
            throw new ArgumentException(
                "IsTitheApplicable is required for Income transactions.", nameof(isTitheApplicable));
        }

        if (type != TransactionType.Income && isTitheApplicable is not null)
        {
            throw new ArgumentException(
                "IsTitheApplicable must be null for non-Income transactions.", nameof(isTitheApplicable));
        }
    }
}
