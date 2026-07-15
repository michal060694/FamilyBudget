namespace FamilyBudget.Core.Entities;

/// <summary>
/// The user-configurable tithe rate applied to tithe-applicable income each month (FR-006).
/// Single-row table, following the same pattern as <see cref="AnnualReserve"/>.
/// </summary>
public class TitheSetting
{
    public const int SingletonId = 1;

    public int Id { get; private set; }
    public decimal Rate { get; private set; }

    private TitheSetting()
    {
    }

    public TitheSetting(decimal rate)
    {
        Validate(rate);
        Id = SingletonId;
        Rate = rate;
    }

    public void Update(decimal rate)
    {
        Validate(rate);
        Rate = rate;
    }

    private static void Validate(decimal rate)
    {
        if (rate <= 0 || rate > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be strictly greater than 0 and at most 1.");
        }
    }
}
