namespace FamilyBudget.Core.Entities;

/// <summary>
/// The two manually-entered account-balance figures shown alongside a given month's Monthly
/// Overview: how much cash currently sits in the account, and how much of this month's income
/// hasn't landed in the account yet. Combined with the overview's TotalOutflow, these drive the
/// "how much should be in the account" figure (see Services.MonthlyOverviewQueryService).
/// </summary>
public class MonthlyCashSnapshot
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal CashInAccount { get; private set; }
    public decimal MoneyNotYetInAccount { get; private set; }

    private MonthlyCashSnapshot()
    {
    }

    public MonthlyCashSnapshot(int year, int month, decimal cashInAccount = 0m, decimal moneyNotYetInAccount = 0m)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");
        }

        ValidateAmount(cashInAccount, nameof(cashInAccount));
        ValidateAmount(moneyNotYetInAccount, nameof(moneyNotYetInAccount));

        Year = year;
        Month = month;
        CashInAccount = cashInAccount;
        MoneyNotYetInAccount = moneyNotYetInAccount;
    }

    public void SetCashInAccount(decimal amount)
    {
        ValidateAmount(amount, nameof(amount));
        CashInAccount = amount;
    }

    public void SetMoneyNotYetInAccount(decimal amount)
    {
        ValidateAmount(amount, nameof(amount));
        MoneyNotYetInAccount = amount;
    }

    private static void ValidateAmount(decimal amount, string paramName)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Amount must be >= 0.");
        }
    }
}