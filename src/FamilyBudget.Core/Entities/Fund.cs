namespace FamilyBudget.Core.Entities;

/// <summary>
/// A named investment/savings account (e.g., "מיטב", "IBI") with a manually-tracked current
/// total balance (spec.md Key Entities; FR-001-FR-004).
/// </summary>
public class Fund
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal TotalBalance { get; private set; }

    private Fund()
    {
        Name = string.Empty;
    }

    public Fund(Guid id, string name, decimal totalBalance)
    {
        ValidateName(name);
        ValidateBalance(totalBalance);

        Id = id;
        Name = name;
        TotalBalance = totalBalance;
    }

    public void Rename(string name)
    {
        ValidateName(name);
        Name = name;
    }

    public void SetTotalBalance(decimal amount)
    {
        ValidateBalance(amount);
        TotalBalance = amount;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name must not be empty.", nameof(name));
        }
    }

    private static void ValidateBalance(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "TotalBalance must be >= 0.");
        }
    }
}
