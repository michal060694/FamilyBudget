namespace FamilyBudget.Core.Abstractions;

public interface ITitheSettingRepository
{
    /// <summary>Returns 0.2 (chomesh) if no rate has been explicitly set yet.</summary>
    Task<decimal> GetRateAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates the household's tithe rate.</summary>
    Task SetRateAsync(decimal rate, CancellationToken cancellationToken = default);
}
