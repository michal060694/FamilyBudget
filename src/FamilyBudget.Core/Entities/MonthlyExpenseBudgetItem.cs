namespace FamilyBudget.Core.Entities;

/// <summary>
/// A named recurring expense category planned for a specific calendar month (e.g. "ארנונה" under
/// FixedExpense, or "מכולת" under RegularExpense), tracking how much was budgeted for the month
/// against how much has actually been used so far — the same budgeted-vs-used pattern as
/// <see cref="AnnualBudgetItem"/>, but scoped to a single calendar month rather than a full year.
/// Defined fresh each month (no automatic recurrence/carry-over), consistent with this project's
/// existing no-auto-recurrence convention for monthly items.
/// </summary>
public class MonthlyExpenseBudgetItem
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Name { get; private set; }

    /// <summary>Restricted to <see cref="TransactionType.FixedExpense"/> or <see cref="TransactionType.RegularExpense"/>.</summary>
    public TransactionType Type { get; private set; }

    public decimal BudgetedAmount { get; private set; }
    public string? BudgetedAmountFormula { get; private set; }

    public decimal UsedAmount { get; private set; }
    public string? UsedAmountFormula { get; private set; }

    private MonthlyExpenseBudgetItem()
    {
        Name = string.Empty;
    }

    public MonthlyExpenseBudgetItem(
        Guid id,
        int year,
        int month,
        string name,
        TransactionType type,
        decimal budgetedAmount,
        decimal usedAmount = 0m,
        string? budgetedAmountFormula = null,
        string? usedAmountFormula = null)
    {
        ValidateType(type);

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (budgetedAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budgetedAmount), "BudgetedAmount must be >= 0.");
        }

        if (usedAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(usedAmount), "UsedAmount must be >= 0.");
        }

        Id = id;
        Year = year;
        Month = month;
        Name = name;
        Type = type;
        BudgetedAmount = budgetedAmount;
        BudgetedAmountFormula = budgetedAmountFormula;
        UsedAmount = usedAmount;
        UsedAmountFormula = usedAmountFormula;
    }

    public decimal Remaining => BudgetedAmount - UsedAmount;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        Name = name;
    }

    public void SetBudgetedAmount(decimal amount, string? formula = null)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "BudgetedAmount must be >= 0.");
        }

        BudgetedAmount = amount;
        BudgetedAmountFormula = formula;
    }

    public void SetUsedAmount(decimal amount, string? formula = null)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "UsedAmount must be >= 0.");
        }

        UsedAmount = amount;
        UsedAmountFormula = formula;
    }

    private static void ValidateType(TransactionType type)
    {
        if (type is not (TransactionType.FixedExpense or TransactionType.RegularExpense))
        {
            throw new ArgumentException(
                "Type must be FixedExpense or RegularExpense.", nameof(type));
        }
    }
}
