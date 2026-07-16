using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Tests;

public class DebtEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public DebtEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDebt_Receivable_PersistsAndAppearsInReceivablesList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts",
            new CreateDebtRequest(DebtDirection.Receivable, "Yossi Cohen", 1000m, new DateOnly(2026, 9, 1)));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(1000m, created!.CurrentBalance);
        Assert.Equal(DebtStatus.Open, created.Status);

        var ledger = await client.GetFromJsonAsync<DebtsLedgerResponse>("/api/debts", JsonOptions);
        Assert.Contains(ledger!.Receivables, d => d.Id == created.Id);
    }

    [Fact]
    public async Task CreateDebt_Payable_AppearsInPayablesList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts",
            new CreateDebtRequest(DebtDirection.Payable, "Gemach", 5000m, RepaymentRate: 500m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var ledger = await client.GetFromJsonAsync<DebtsLedgerResponse>("/api/debts", JsonOptions);
        Assert.Contains(ledger!.Payables, d => d.Id == created!.Id);
    }

    [Fact]
    public async Task CreateDebt_EmptyName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "", 100m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDebt_ChangesNameAndAmount()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Payable, "Original", 1000m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/debts/{created!.Id}", new UpdateDebtRequest("Renamed", 1200m));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.Equal("Renamed", updated!.CounterpartyName);
        Assert.Equal(1200m, updated.CurrentBalance); // no repayments yet, so balance == new amount
    }

    [Fact]
    public async Task UpdateDebt_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/debts/{Guid.NewGuid()}", new UpdateDebtRequest("X", 100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDebt_RemovesItFromLedger()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "To Delete", 500m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/debts/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var ledger = await client.GetFromJsonAsync<DebtsLedgerResponse>("/api/debts", JsonOptions);
        Assert.DoesNotContain(ledger!.Receivables, d => d.Id == created.Id);
    }

    [Fact]
    public async Task DeleteDebt_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/debts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetBalance_UpdatesCurrentBalanceDirectly()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Payable, "Balance Fix", 1000m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var balanceResponse = await client.PatchAsJsonAsync(
            $"/api/debts/{created!.Id}/balance", new SetDebtBalanceRequest(650m));

        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        var updated = await balanceResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.Equal(650m, updated!.CurrentBalance);
        Assert.Equal(DebtStatus.Open, updated.Status);
    }

    [Fact]
    public async Task SetBalance_WithFormula_PersistsAmountAndFormula()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Payable, "Formula Balance", 1000m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var balanceResponse = await client.PatchAsJsonAsync(
            $"/api/debts/{created!.Id}/balance", new SetDebtBalanceRequest(600m, "1000-400"));

        var updated = await balanceResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.Equal(600m, updated!.CurrentBalance);
        Assert.Equal("1000-400", updated.CurrentBalanceFormula);
    }

    [Fact]
    public async Task SetBalance_Zero_ClosesDebt()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "Zeroed Out", 500m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var balanceResponse = await client.PatchAsJsonAsync(
            $"/api/debts/{created!.Id}/balance", new SetDebtBalanceRequest(0m));

        var updated = await balanceResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.Equal(0m, updated!.CurrentBalance);
        Assert.Equal(DebtStatus.Closed, updated.Status);
    }

    [Fact]
    public async Task SetBalance_Negative_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "Negative Attempt", 500m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var response = await client.PatchAsJsonAsync(
            $"/api/debts/{created!.Id}/balance", new SetDebtBalanceRequest(-1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetBalance_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/debts/{Guid.NewGuid()}/balance", new SetDebtBalanceRequest(100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RecordRepayment_Receivable_FullAmount_ClosesDebtAndCreatesIncomeTransaction()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "Yossi Full Repay", 1000m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var repayResponse = await client.PostAsJsonAsync(
            $"/api/debts/{created!.Id}/repayments", new RecordRepaymentRequest(1000m, PaymentMethod.BankTransfer));

        Assert.Equal(HttpStatusCode.OK, repayResponse.StatusCode);
        var updated = await repayResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.Equal(0m, updated!.CurrentBalance);
        Assert.Equal(DebtStatus.Closed, updated.Status);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var transactions = await client.GetFromJsonAsync<TransactionListResponse>(
            $"/api/transactions?year={today.Year}&month={today.Month}", JsonOptions);
        Assert.Contains(transactions!.Transactions, t => t.Amount == 1000m && t.Type == TransactionType.Income && t.IsTitheApplicable == false);
    }

    [Fact]
    public async Task RecordRepayment_Payable_Partial_StaysOpenAndCreatesDebtRepaymentTransaction()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Payable, "Gemach Partial", 5000m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var repayResponse = await client.PostAsJsonAsync(
            $"/api/debts/{created!.Id}/repayments", new RecordRepaymentRequest(1500m, PaymentMethod.Cash));

        var updated = await repayResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);
        Assert.Equal(3500m, updated!.CurrentBalance);
        Assert.Equal(DebtStatus.Open, updated.Status);

        var today = DateOnly.FromDateTime(DateTime.Now);
        var transactions = await client.GetFromJsonAsync<TransactionListResponse>(
            $"/api/transactions?year={today.Year}&month={today.Month}", JsonOptions);
        Assert.Contains(transactions!.Transactions, t => t.Amount == 1500m && t.Type == TransactionType.DebtRepayment);
    }

    [Fact]
    public async Task RecordRepayment_ExceedsBalance_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "Small Debt", 100m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        var response = await client.PostAsJsonAsync(
            $"/api/debts/{created!.Id}/repayments", new RecordRepaymentRequest(200m, PaymentMethod.Cash));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordRepayment_AlreadyClosedDebt_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/debts", new CreateDebtRequest(DebtDirection.Receivable, "Closes Fast", 100m));
        var created = await createResponse.Content.ReadFromJsonAsync<DebtResponse>(JsonOptions);

        await client.PostAsJsonAsync(
            $"/api/debts/{created!.Id}/repayments", new RecordRepaymentRequest(100m, PaymentMethod.Cash));

        var secondAttempt = await client.PostAsJsonAsync(
            $"/api/debts/{created.Id}/repayments", new RecordRepaymentRequest(1m, PaymentMethod.Cash));

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task RecordRepayment_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/debts/{Guid.NewGuid()}/repayments", new RecordRepaymentRequest(100m, PaymentMethod.Cash));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
