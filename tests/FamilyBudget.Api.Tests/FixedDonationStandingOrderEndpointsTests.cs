using System.Net;
using System.Net.Http.Json;
using FamilyBudget.Api.Contracts;

namespace FamilyBudget.Api.Tests;

public class FixedDonationStandingOrderEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private readonly FamilyBudgetApiFactory _factory;

    public FixedDonationStandingOrderEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateWithNoEndDate_AppliesToEveryFutureMonth()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/fixed-donation-standing-orders",
            new CreateFixedDonationStandingOrderRequest("Yeshiva", 100m, null, null));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FixedDonationStandingOrderView>();
        Assert.NotNull(created);

        var list2070 = await client.GetFromJsonAsync<FixedDonationStandingOrderListResponse>(
            "/api/fixed-donation-standing-orders?year=2070&month=1");
        Assert.Contains(list2070!.Items, i => i.Id == created!.Id);
    }

    [Fact]
    public async Task CreateWithEndDate_StopsApplyingAfterThatMonth()
    {
        const int year = 2071;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/fixed-donation-standing-orders",
            new CreateFixedDonationStandingOrderRequest("Temporary Pledge", 50m, year, 6));
        var created = await createResponse.Content.ReadFromJsonAsync<FixedDonationStandingOrderView>();

        var stillActive = await client.GetFromJsonAsync<FixedDonationStandingOrderListResponse>(
            $"/api/fixed-donation-standing-orders?year={year}&month=6");
        Assert.Contains(stillActive!.Items, i => i.Id == created!.Id);

        var expired = await client.GetFromJsonAsync<FixedDonationStandingOrderListResponse>(
            $"/api/fixed-donation-standing-orders?year={year}&month=7");
        Assert.DoesNotContain(expired!.Items, i => i.Id == created!.Id);
    }

    [Fact]
    public async Task Create_MismatchedValidUntilFields_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/fixed-donation-standing-orders",
            new CreateFixedDonationStandingOrderRequest("Bad", 50m, 2071, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesAmountAndEndDate()
    {
        const int year = 2072;
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/fixed-donation-standing-orders",
            new CreateFixedDonationStandingOrderRequest("Pledge", 100m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<FixedDonationStandingOrderView>();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/fixed-donation-standing-orders/{created!.Id}",
            new UpdateFixedDonationStandingOrderRequest("Pledge Updated", 150m, year, 3));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FixedDonationStandingOrderView>();
        Assert.Equal("Pledge Updated", updated!.Name);
        Assert.Equal(150m, updated.Amount);
        Assert.Equal(year, updated.ValidUntilYear);
        Assert.Equal(3, updated.ValidUntilMonth);
    }

    [Fact]
    public async Task Delete_RemovesItFromSubsequentGet()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/fixed-donation-standing-orders",
            new CreateFixedDonationStandingOrderRequest("To Delete", 20m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<FixedDonationStandingOrderView>();

        var deleteResponse = await client.DeleteAsync($"/api/fixed-donation-standing-orders/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<FixedDonationStandingOrderListResponse>(
            "/api/fixed-donation-standing-orders?year=2073&month=1");
        Assert.DoesNotContain(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/fixed-donation-standing-orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
