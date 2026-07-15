using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Tests;

public class TransactionEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    // The API server serializes enums as strings (Program.cs's ConfigureHttpJsonOptions), but
    // HttpClient's JSON extension methods use their own default options unless given these
    // explicitly — without this, responses containing TransactionType/PaymentMethod fail to
    // deserialize back into the enum.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public TransactionEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTransaction_Income_PersistsAndAppearsInMonthList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(new DateOnly(2040, 3, 10), 500m, TransactionType.Income, PaymentMethod.BankTransfer, true, "Salary"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created!.IsTitheApplicable);

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/transactions?year=2040&month=3", JsonOptions);
        Assert.Contains(list!.Transactions, t => t.Id == created.Id);
    }

    [Fact]
    public async Task CreateTransaction_IncomeWithoutTitheFlag_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(new DateOnly(2040, 4, 1), 500m, TransactionType.Income, PaymentMethod.Cash, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_NonIncomeWithTitheFlag_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(new DateOnly(2040, 4, 2), 50m, TransactionType.RegularExpense, PaymentMethod.Cash, true, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTransaction_NonPositiveAmount_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(new DateOnly(2040, 4, 3), 0m, TransactionType.RegularExpense, PaymentMethod.Cash, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactions_FiltersByTypeAndPaymentMethod()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/transactions",
            new CreateTransactionRequest(new DateOnly(2041, 5, 1), 100m, TransactionType.RegularExpense, PaymentMethod.Cash, null, "Groceries"));
        await client.PostAsJsonAsync("/api/transactions",
            new CreateTransactionRequest(new DateOnly(2041, 5, 2), 200m, TransactionType.FixedExpense, PaymentMethod.CreditCard, null, "Rent"));

        var cashOnly = await client.GetFromJsonAsync<TransactionListResponse>(
            "/api/transactions?year=2041&month=5&paymentMethod=Cash", JsonOptions);
        Assert.Single(cashOnly!.Transactions);
        Assert.Equal("Groceries", cashOnly.Transactions[0].Description);

        var fixedOnly = await client.GetFromJsonAsync<TransactionListResponse>(
            "/api/transactions?year=2041&month=5&type=FixedExpense", JsonOptions);
        Assert.Single(fixedOnly!.Transactions);
        Assert.Equal("Rent", fixedOnly.Transactions[0].Description);
    }

    [Fact]
    public async Task GetTransactions_InvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/transactions?year=2041&month=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTransaction_ChangesAmount_ReflectedInSubsequentGet()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(new DateOnly(2042, 6, 1), 100m, TransactionType.RegularExpense, PaymentMethod.Cash, null, "Original"));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/transactions/{created!.Id}",
            new CreateTransactionRequest(new DateOnly(2042, 6, 1), 150m, TransactionType.RegularExpense, PaymentMethod.Cash, null, "Updated"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        Assert.Equal(150m, updated!.Amount);
        Assert.Equal("Updated", updated.Description);
    }

    [Fact]
    public async Task UpdateTransaction_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/transactions/{Guid.NewGuid()}",
            new CreateTransactionRequest(new DateOnly(2042, 6, 2), 50m, TransactionType.RegularExpense, PaymentMethod.Cash, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTransaction_RemovesItFromSubsequentGet()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/transactions",
            new CreateTransactionRequest(new DateOnly(2043, 7, 1), 75m, TransactionType.RegularExpense, PaymentMethod.Check, null, "To delete"));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/transactions/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<TransactionListResponse>("/api/transactions?year=2043&month=7", JsonOptions);
        Assert.DoesNotContain(list!.Transactions, t => t.Id == created.Id);
    }

    [Fact]
    public async Task DeleteTransaction_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/transactions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
