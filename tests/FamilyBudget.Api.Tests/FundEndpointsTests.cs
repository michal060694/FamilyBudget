using System.Net;
using System.Net.Http.Json;
using FamilyBudget.Api.Contracts;

namespace FamilyBudget.Api.Tests;

public class FundEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private readonly FamilyBudgetApiFactory _factory;

    public FundEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateFund_PersistsAndAppearsInList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("Meitav", 15000m));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FundSummaryResponse>();
        Assert.NotNull(created);
        Assert.Equal(0m, created!.EarmarkedTotal);
        Assert.Equal(15000m, created.Discrepancy); // fully unearmarked

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds");
        Assert.Contains(list!.Funds, f => f.FundId == created.FundId);
    }

    [Fact]
    public async Task CreateFund_EmptyName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("", 100m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateFund_NegativeBalance_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("IBI", -1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateFund_ChangesNameAndBalance()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("Original", 1000m));
        var created = await createResponse.Content.ReadFromJsonAsync<FundSummaryResponse>();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/funds/{created!.FundId}", new UpdateFundRequest("Renamed", 2000m));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FundSummaryResponse>();
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(2000m, updated.TotalBalance);
    }

    [Fact]
    public async Task UpdateFund_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/funds/{Guid.NewGuid()}", new UpdateFundRequest("X", 100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteFund_RemovesItAndItsEarmarks()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("To Delete", 500m));
        var created = await createResponse.Content.ReadFromJsonAsync<FundSummaryResponse>();

        await client.PostAsJsonAsync(
            $"/api/funds/{created!.FundId}/earmarks", new CreateFundEarmarkRequest("General", 500m));

        var deleteResponse = await client.DeleteAsync($"/api/funds/{created.FundId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<FundListResponse>("/api/funds");
        Assert.DoesNotContain(list!.Funds, f => f.FundId == created.FundId);
    }

    [Fact]
    public async Task DeleteFund_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/funds/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Earmarks_AddEditDelete_ReconciliationUpdatesAsExpected()
    {
        var client = _factory.CreateClient();

        var createFund = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("Reconciled Fund", 15000m));
        var fund = await createFund.Content.ReadFromJsonAsync<FundSummaryResponse>();

        var addPeah = await client.PostAsJsonAsync(
            $"/api/funds/{fund!.FundId}/earmarks", new CreateFundEarmarkRequest("פאה", 10000m));
        Assert.Equal(HttpStatusCode.Created, addPeah.StatusCode);
        var peahEarmark = await addPeah.Content.ReadFromJsonAsync<FundEarmarkResponse>();

        await client.PostAsJsonAsync(
            $"/api/funds/{fund.FundId}/earmarks", new CreateFundEarmarkRequest("שנתי 26", 5000m));

        var listAfterAdding = await client.GetFromJsonAsync<FundListResponse>("/api/funds");
        var reconciled = listAfterAdding!.Funds.Single(f => f.FundId == fund.FundId);
        Assert.Equal(15000m, reconciled.EarmarkedTotal);
        Assert.Equal(0m, reconciled.Discrepancy); // fully reconciled

        // Editing an earmark to exceed the balance should flip the discrepancy negative (mismatch).
        var updateEarmark = await client.PutAsJsonAsync(
            $"/api/funds/earmarks/{peahEarmark!.Id}", new UpdateFundEarmarkRequest("פאה", 13000m));
        Assert.Equal(HttpStatusCode.OK, updateEarmark.StatusCode);

        var listAfterOverEarmarking = await client.GetFromJsonAsync<FundListResponse>("/api/funds");
        var overEarmarked = listAfterOverEarmarking!.Funds.Single(f => f.FundId == fund.FundId);
        Assert.Equal(-3000m, overEarmarked.Discrepancy); // 15000 - 18000

        // Deleting the earmark should bring the discrepancy back down.
        var deleteEarmark = await client.DeleteAsync($"/api/funds/earmarks/{peahEarmark.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteEarmark.StatusCode);

        var listAfterDelete = await client.GetFromJsonAsync<FundListResponse>("/api/funds");
        var afterDelete = listAfterDelete!.Funds.Single(f => f.FundId == fund.FundId);
        Assert.Equal(10000m, afterDelete.Discrepancy); // 15000 - 5000, unearmarked remainder
    }

    [Fact]
    public async Task CreateEarmark_UnknownFundId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/funds/{Guid.NewGuid()}/earmarks", new CreateFundEarmarkRequest("X", 100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEarmark_NonPositiveAmount_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var createFund = await client.PostAsJsonAsync("/api/funds", new CreateFundRequest("Validation Fund", 100m));
        var fund = await createFund.Content.ReadFromJsonAsync<FundSummaryResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/funds/{fund!.FundId}/earmarks", new CreateFundEarmarkRequest("X", 0m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEarmark_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/funds/earmarks/{Guid.NewGuid()}", new UpdateFundEarmarkRequest("X", 100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEarmark_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/funds/earmarks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
