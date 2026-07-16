namespace FamilyBudget.Core.Entities;

/// <summary>
/// A reusable line in the household's recurring monthly plan — income, fixed expense, or regular
/// expense — that can be applied to a specific calendar month in one action instead of re-entering
/// the same categories every month. <see cref="FixedDonationStandingOrder"/> already solves this
/// for donations; this covers the other recurring pieces of the Monthly Overview.
/// </summary>
public class MonthlyTemplateItem
{
    public Guid Id { get; private set; }

    /// <summary>Restricted to <see cref="TransactionType.Income"/>, <see cref="TransactionType.FixedExpense"/>, or <see cref="TransactionType.RegularExpense"/>.</summary>
    public TransactionType Type { get; private set; }

    public string Name { get; private set; }
    public decimal Amount { get; private set; }
    public string? AmountFormula { get; private set; }

    /// <summary>Required (non-null) when <see cref="Type"/> is Income; MUST be null otherwise (mirrors <see cref="Transaction"/>).</summary>
    public bool? IsTitheApplicable { get; private set; }

    private MonthlyTemplateItem()
    {
        Name = string.Empty;
    }

    public MonthlyTemplateItem(
        Guid id,
        TransactionType type,
        string name,
        decimal amount,
        bool? isTitheApplicable,
        string? amountFormula = null)
    {
        Validate(type, name, amount, isTitheApplicable);

        Id = id;
        Type = type;
        Name = name;
        Amount = amount;
        AmountFormula = amountFormula;
        IsTitheApplicable = isTitheApplicable;
    }

    public void Update(string name, decimal amount, bool? isTitheApplicable, string? amountFormula = null)
    {
        Validate(Type, name, amount, isTitheApplicable);

        Name = name;
        Amount = amount;
        AmountFormula = amountFormula;
        IsTitheApplicable = isTitheApplicable;
    }

    private static void Validate(TransactionType type, string name, decimal amount, bool? isTitheApplicable)
    {
        if (type is not (TransactionType.Income or TransactionType.FixedExpense or TransactionType.RegularExpense))
        {
            throw new ArgumentException("Type must be Income, FixedExpense, or RegularExpense.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be strictly positive.");
        }

        if (type == TransactionType.Income && isTitheApplicable is null)
        {
            throw new ArgumentException("IsTitheApplicable is required for Income items.", nameof(isTitheApplicable));
        }

        if (type != TransactionType.Income && isTitheApplicable is not null)
        {
            throw new ArgumentException("IsTitheApplicable must be null for non-Income items.", nameof(isTitheApplicable));
        }
    }
}
