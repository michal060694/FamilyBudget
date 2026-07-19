using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyBudget.Api.Tests;

public class MonthlyActionItemEndpointsTests : IClassFixture<FamilyBudgetApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FamilyBudgetApiFactory _factory;

    public MonthlyActionItemEndpointsTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateActionItem_PersistsAndAppearsInMonthList()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-action-items",
            new CreateMonthlyActionItemRequest(2071, 1, "שלחי כסף לתרומה", DeadlineDate: new DateOnly(2071, 1, 10)));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);
        Assert.NotNull(created);
        Assert.False(created!.IsCompleted);

        var list = await client.GetFromJsonAsync<MonthlyActionItemListResponse>(
            "/api/monthly-action-items?year=2071&month=1", JsonOptions);
        Assert.Contains(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task CreateActionItem_EmptyDescription_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/monthly-action-items", new CreateMonthlyActionItemRequest(2071, 2, "   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetActionItems_InvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/monthly-action-items?year=2071&month=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateActionItem_ChangesDescriptionAndDeadline()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-action-items", new CreateMonthlyActionItemRequest(2071, 3, "טיוטה"));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/monthly-action-items/{created!.Id}",
            new UpdateMonthlyActionItemRequest("הפקידי לקרן כספית", null, new DateOnly(2071, 3, 15)));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);
        Assert.Equal("הפקידי לקרן כספית", updated!.Description);
        Assert.Equal(new DateOnly(2071, 3, 15), updated.DeadlineDate);
    }

    [Fact]
    public async Task UpdateActionItem_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/monthly-action-items/{Guid.NewGuid()}",
            new UpdateMonthlyActionItemRequest("משהו", null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetComplete_TogglesIsCompleted()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-action-items", new CreateMonthlyActionItemRequest(2071, 4, "לסמן כבוצע"));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);

        var completeResponse = await client.PatchAsJsonAsync(
            $"/api/monthly-action-items/{created!.Id}/complete", new SetMonthlyActionItemCompleteRequest(true));

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);
        Assert.True(completed!.IsCompleted);
    }

    [Fact]
    public async Task DeleteActionItem_RemovesItFromSubsequentGet()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-action-items", new CreateMonthlyActionItemRequest(2071, 5, "למחוק"));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/monthly-action-items/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var list = await client.GetFromJsonAsync<MonthlyActionItemListResponse>(
            "/api/monthly-action-items?year=2071&month=5", JsonOptions);
        Assert.DoesNotContain(list!.Items, i => i.Id == created.Id);
    }

    [Fact]
    public async Task GetActionItems_PastDeadlineIncomplete_IsFlaggedOverdue()
    {
        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/monthly-action-items",
            new CreateMonthlyActionItemRequest(2020, 6, "פעולה ישנה", DeadlineDate: new DateOnly(2020, 6, 1)));
        var created = await createResponse.Content.ReadFromJsonAsync<MonthlyActionItemView>(JsonOptions);

        var list = await client.GetFromJsonAsync<MonthlyActionItemListResponse>(
            "/api/monthly-action-items?year=2020&month=6", JsonOptions);
        var item = Assert.Single(list!.Items, i => i.Id == created!.Id);

        Assert.True(item.IsOverdue);
    }

    [Fact]
    public async Task SendRemindersNow_OverdueIncompleteItem_SendsOneEmailAndDedupesOnSecondCall()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.MonthlyActionItems.Add(new FamilyBudget.Core.Entities.MonthlyActionItem(
                Guid.NewGuid(), 2022, 1, $"פעולה שעברה דד-ליין {marker}", deadlineDate: new DateOnly(2022, 1, 1)));
            await db.SaveChangesAsync();
        }

        var firstResponse = await client.PostAsync("/api/monthly-action-items/send-reminders-now", null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<SendRemindersNowResponse>(JsonOptions);
        Assert.True(firstResult!.RemindersSent >= 1);

        var recordingSender = _factory.Services.GetRequiredService<RecordingEmailSender>();
        Assert.Contains(recordingSender.SentEmails, e => e.Body.Contains(marker));
        var matchingEmail = recordingSender.SentEmails.First(e => e.Body.Contains(marker));
        Assert.Equal(2, matchingEmail.Recipients.Count);
        Assert.Contains("4154113@gmail.com", matchingEmail.Recipients);
        Assert.Contains("S6709491@gmail.com", matchingEmail.Recipients);

        var sentEmailCountBeforeSecondCall = recordingSender.SentEmails.Count;
        var secondResponse = await client.PostAsync("/api/monthly-action-items/send-reminders-now", null);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<SendRemindersNowResponse>(JsonOptions);

        // The item from this test is already marked as reminded, so it must not appear again — but we
        // can't assert RemindersSent == 0 globally since other tests in this shared-fixture class may
        // seed their own overdue items. What matters is no *new* email mentioning this test's marker.
        Assert.DoesNotContain(
            recordingSender.SentEmails.Skip(sentEmailCountBeforeSecondCall), e => e.Body.Contains(marker));
        _ = secondResult;
    }

    [Fact]
    public async Task SendRemindersNow_CompletedItemPastDeadline_IsNotReminded()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            var item = new FamilyBudget.Core.Entities.MonthlyActionItem(
                Guid.NewGuid(), 2023, 1, $"פעולה שהושלמה {marker}", deadlineDate: new DateOnly(2023, 1, 1));
            item.SetCompleted(true);
            db.MonthlyActionItems.Add(item);
            await db.SaveChangesAsync();
        }

        await client.PostAsync("/api/monthly-action-items/send-reminders-now", null);

        var recordingSender = _factory.Services.GetRequiredService<RecordingEmailSender>();
        Assert.DoesNotContain(recordingSender.SentEmails, e => e.Body.Contains(marker));
    }

    [Fact]
    public async Task SendRemindersNow_FutureDeadline_IsNotReminded()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.MonthlyActionItems.Add(new FamilyBudget.Core.Entities.MonthlyActionItem(
                Guid.NewGuid(), 2090, 1, $"פעולה עתידית {marker}", deadlineDate: new DateOnly(2090, 1, 1)));
            await db.SaveChangesAsync();
        }

        await client.PostAsync("/api/monthly-action-items/send-reminders-now", null);

        var recordingSender = _factory.Services.GetRequiredService<RecordingEmailSender>();
        Assert.DoesNotContain(recordingSender.SentEmails, e => e.Body.Contains(marker));
    }

    [Fact]
    public async Task SendRemindersNow_NoDeadline_IsNotReminded()
    {
        var client = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8];

        await client.PostAsJsonAsync(
            "/api/monthly-action-items", new CreateMonthlyActionItemRequest(2071, 7, $"בלי דד-ליין {marker}"));

        await client.PostAsync("/api/monthly-action-items/send-reminders-now", null);

        var recordingSender = _factory.Services.GetRequiredService<RecordingEmailSender>();
        Assert.DoesNotContain(recordingSender.SentEmails, e => e.Body.Contains(marker));
    }
}
