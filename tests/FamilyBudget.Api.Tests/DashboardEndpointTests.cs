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
