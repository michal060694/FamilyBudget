namespace FamilyBudget.Core.Abstractions;

public interface IAnnualReserveRepository
{
    /// <summary>Returns 0 if no reserve has been set for the year yet.</summary>
    Task<decimal> GetAmountAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>Returns null if no reserve has been set for the year yet, or if it was entered as a plain number.</summary>
    Task<string?> GetFormulaAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates the reserve amount for the year.</summary>
    Task SetAmountAsync(int year, decimal amount, string? amountFormula = null, CancellationToken cancellationToken = default);
}
