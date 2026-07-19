namespace FamilyBudget.Core.Entities;

/// <summary>
/// A single manually-entered "money we already have set aside, not tied to any specific item"
/// total for a given year. Reduces the flat monthly allocation need
/// (see <see cref="Services.AnnualBudgetSummary"/>): MonthlyAllocation = (TotalAnnualBudget -
/// ReserveOnHand) / 12.
/// </summary>
public class AnnualReserve
{
    public int Year { get; private set; }
    public decimal Amount { get; private set; }

    /// <summary>Raw formula the user typed for <see cref="Amount"/> (e.g. "600-200"), or null if entered as a plain number.</summary>
    public string? AmountFormula { get; private set; }

    private AnnualReserve()
    {
    }

    public AnnualReserve(int year, decimal amount, string? amountFormula = null)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be >= 0.");
        }

        Year = year;
        Amount = amount;
        AmountFormula = amountFormula;
    }

    public void Update(decimal amount, string? amountFormula = null)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be >= 0.");
        }

        Amount = amount;
        AmountFormula = amountFormula;
    }
}
