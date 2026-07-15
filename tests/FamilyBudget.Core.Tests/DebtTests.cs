using FamilyBudget.Core.Entities;

namespace FamilyBudget.Core.Tests;

public class DebtTests
{
    [Fact]
    public void Constructor_SetsCurrentBalanceToOriginalAmount_AndStatusOpen()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi Cohen", 1000m);

        Assert.Equal(1000m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Debt(Guid.NewGuid(), DebtDirection.Payable, " ", 100m));
    }

    [Fact]
    public void Constructor_NonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Debt(Guid.NewGuid(), DebtDirection.Payable, "X", 0m));
    }

    [Fact]
    public void Constructor_NonPositiveRepaymentRate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Debt(Guid.NewGuid(), DebtDirection.Payable, "X", 100m, repaymentRate: 0m));
    }

    [Fact]
    public void RecordRepayment_Partial_ReducesBalance_StaysOpen()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 5000m);

        debt.RecordRepayment(2000m);

        Assert.Equal(3000m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public void RecordRepayment_ExactBalance_ClosesDebt()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);

        debt.RecordRepayment(1000m);

        Assert.Equal(0m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Closed, debt.Status);
    }

    [Fact]
    public void RecordRepayment_ExceedsBalance_Throws()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);

        Assert.Throws<ArgumentOutOfRangeException>(() => debt.RecordRepayment(1001m));
        Assert.Equal(1000m, debt.CurrentBalance); // unaffected by the rejected attempt
    }

    [Fact]
    public void RecordRepayment_NonPositiveAmount_Throws()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);

        Assert.Throws<ArgumentOutOfRangeException>(() => debt.RecordRepayment(0m));
    }

    [Fact]
    public void RecordRepayment_AlreadyClosed_Throws()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);
        debt.RecordRepayment(1000m); // closes it

        Assert.Throws<InvalidOperationException>(() => debt.RecordRepayment(1m));
    }

    [Fact]
    public void Update_CorrectingAmount_PreservesAlreadyPaidAmount()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 1000m);
        debt.RecordRepayment(400m); // balance now 600, 400 already paid

        debt.Update("Gemach", 900m, null, null, null); // corrected original amount

        Assert.Equal(500m, debt.CurrentBalance); // 900 - 400 already paid
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public void Update_CorrectedAmountLessThanAlreadyPaid_ClosesDebtAndFloorsAtZero()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 1000m);
        debt.RecordRepayment(800m); // 800 already paid, balance 200

        debt.Update("Gemach", 800m, null, null, null); // corrected amount equals what was paid

        Assert.Equal(0m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Closed, debt.Status);
    }
}
