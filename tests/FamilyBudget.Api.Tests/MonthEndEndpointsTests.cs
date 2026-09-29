using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;

namespace FamilyBudget.Api.Tests;

public class MonthEndEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public MonthEndEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MonthEndItems_AreScopedByMonthAndSupportCrud()
    {
        var client = _factory.CreateClient();
        const int year = 2087;
        const int month = 4;

        var assetResponse = await client.PostAsJsonAsync("/api/month-end",
            new CreateMonthEndItemRequest(year, month, MonthEndDirection.Asset, "חשבון", 500m));
        var asset = await assetResponse.Content.ReadFromJsonAsync<MonthEndItemView>(JsonOptions);
        Assert.Equal(HttpStatusCode.Created, assetResponse.StatusCode);

        var liabilityResponse = await client.PostAsJsonAsync("/api/month-end",
            new CreateMonthEndItemRequest(year, month, MonthEndDirection.Liability, "אשראי", 300m));
        var liability = await liabilityResponse.Content.ReadFromJsonAsync<MonthEndItemView>(JsonOptions);
        Assert.Equal(HttpStatusCode.Created, liabilityResponse.StatusCode);

        await client.PostAsJsonAsync("/api/month-end",
            new CreateMonthEndItemRequest(year, month + 1, MonthEndDirection.Asset, "מזומן", 100m));

        var items = await client.GetFromJsonAsync<List<MonthEndItemView>>(
            $"/api/month-end?year={year}&month={month}", JsonOptions);
        Assert.Equal(2, items!.Count);
        Assert.Equal(500m, items.Where(item => item.Direction == MonthEndDirection.Asset).Sum(item => item.Amount));
        Assert.Equal(300m, items.Where(item => item.Direction == MonthEndDirection.Liability).Sum(item => item.Amount));

        var updateResponse = await client.PutAsJsonAsync($"/api/month-end/{asset!.Id}",
            new UpdateMonthEndItemRequest("חשבון בנק", 450m));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/month-end/{liability!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        items = await client.GetFromJsonAsync<List<MonthEndItemView>>(
            $"/api/month-end?year={year}&month={month}", JsonOptions);
        var remaining = Assert.Single(items!);
        Assert.Equal("חשבון בנק", remaining.Name);
        Assert.Equal(450m, remaining.Amount);
    }

    [Fact]
    public async Task CreateMonthEndItem_InvalidMonthOrAmount_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var invalidMonth = await client.PostAsJsonAsync("/api/month-end",
            new CreateMonthEndItemRequest(2088, 13, MonthEndDirection.Asset, "חשבון", 100m));
        var invalidAmount = await client.PostAsJsonAsync("/api/month-end",
            new CreateMonthEndItemRequest(2088, 5, MonthEndDirection.Liability, "אשראי", 0m));

        Assert.Equal(HttpStatusCode.BadRequest, invalidMonth.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidAmount.StatusCode);
    }
}