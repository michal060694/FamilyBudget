using System.Net.Http.Json;
using FamilyBudget.Api.Contracts;
using FamilyBudget.Core.Entities;
using FamilyBudget.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FamilyBudget.Api.Tests;

public class DashboardEndpointTests : IClassFixture<FamilyBudgetApiFactory>
{
    private readonly FamilyBudgetApiFactory _factory;

    public DashboardEndpointTests(FamilyBudgetApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_ReturnsAllocationLineMatchingSmoothingFormula()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.AnnualBudgetItems.Add(new AnnualBudgetItem(
                Guid.NewGuid(), year: 2026, name: "December Holidays",
                totalAmount: 4000m, targetMonth: 12, amountAlreadySetAside: 1000m));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var response = await client.GetFromJsonAsync<DashboardResponse>(
            "/api/dashboard?year=2026&month=10");

        Assert.NotNull(response);
        var line = Assert.Single(response!.AllocationLines);
        Assert.Equal(1000m, line.AllocatedMonthly);
        Assert.Equal(1000m, response.TotalRequiredAllocation);
    }

    [Fact]
    public async Task GetDashboard_InvalidMonth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/dashboard?year=2026&month=13");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_IncludesTitheDue_MatchingMonthlyOverview()
    {
        const int year = 2051;
        const int month = 4;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.TitheSettings.Add(new FamilyBudget.Core.Entities.TitheSetting(0.2m));
            db.Transactions.Add(new FamilyBudget.Core.Entities.Transaction(
                Guid.NewGuid(), new DateOnly(year, month, 1), 1000m,
                FamilyBudget.Core.Entities.TransactionType.Income,
                FamilyBudget.Core.Entities.PaymentMethod.BankTransfer, true, "Salary"));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var dashboard = await client.GetFromJsonAsync<DashboardResponse>($"/api/dashboard?year={year}&month={month}");

        Assert.NotNull(dashboard);
        Assert.Equal(200m, dashboard!.TitheDue.GrossTitheTarget); // 1000 * 0.2
        Assert.Equal(200m, dashboard.TitheDue.NetTitheDue); // no deductions this month
    }
}

public class FamilyBudgetApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FamilyBudgetDbContext>>();

            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.AddSingleton(connection);
            services.AddDbContext<FamilyBudgetDbContext>((provider, options) =>
                options.UseSqlite(provider.GetRequiredService<Microsoft.Data.Sqlite.SqliteConnection>()));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
            db.Database.Migrate();
        });
    }
}
