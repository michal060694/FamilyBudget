namespace FamilyBudget.Core.Entities;

public enum MonthEndDirection
{
    Asset,
    Liability,
}

public class MonthEndItem
{
    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public MonthEndDirection Direction { get; private set; }
    public string Name { get; private set; }
    public decimal Amount { get; private set; }

    private MonthEndItem()
    {
        Name = string.Empty;
    }

    public MonthEndItem(Guid id, int year, int month, MonthEndDirection direction, string name, decimal amount)
    {
        Validate(month, name, amount);
        Id = id;
        Year = year;
        Month = month;
        Direction = direction;
        Name = name.Trim();
        Amount = amount;
    }

    public void Update(string name, decimal amount)
    {
        Validate(Month, name, amount);
        Name = name.Trim();
        Amount = amount;
    }

    private static void Validate(int month, string name, decimal amount)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be strictly positive.");
        }
    }
}