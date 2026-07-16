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
    public void SetCurrentBalance_PositiveValue_UpdatesBalanceAndStaysOpen()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 1000m);
        debt.RecordRepayment(400m); // balance now 600

        debt.SetCurrentBalance(750m);

        Assert.Equal(750m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public void SetCurrentBalance_Zero_ClosesDebt()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);

        debt.SetCurrentBalance(0m);

        Assert.Equal(0m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Closed, debt.Status);
    }

    [Fact]
    public void SetCurrentBalance_ReopensAClosedDebt()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);
        debt.RecordRepayment(1000m); // closes it

        debt.SetCurrentBalance(200m); // correcting a mistaken close

        Assert.Equal(200m, debt.CurrentBalance);
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public void SetCurrentBalance_Negative_Throws()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);

        Assert.Throws<ArgumentOutOfRangeException>(() => debt.SetCurrentBalance(-1m));
        Assert.Equal(1000m, debt.CurrentBalance); // unaffected by the rejected attempt
    }

    [Fact]
    public void SetCurrentBalance_WithFormula_StoresFormula()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Receivable, "Yossi", 1000m);

        debt.SetCurrentBalance(600m, "1000-400");

        Assert.Equal(600m, debt.CurrentBalance);
        Assert.Equal("1000-400", debt.CurrentBalanceFormula);
    }

    [Fact]
    public void RecordRepayment_ClearsAnyPreviousBalanceFormula()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 1000m);
        debt.SetCurrentBalance(800m, "1000-200");

        debt.RecordRepayment(300m);

        Assert.Equal(500m, debt.CurrentBalance);
        Assert.Null(debt.CurrentBalanceFormula);
    }

    [Fact]
    public void Update_ClearsAnyPreviousBalanceFormula()
    {
        var debt = new Debt(Guid.NewGuid(), DebtDirection.Payable, "Gemach", 1000m);
        debt.SetCurrentBalance(800m, "1000-200");

        debt.Update("Gemach", 1200m, null, null, null);

        Assert.Null(debt.CurrentBalanceFormula);
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
