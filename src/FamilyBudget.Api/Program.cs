using FamilyBudget.Api.Endpoints;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Services;
using FamilyBudget.Infrastructure.Persistence;
using FamilyBudget.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FamilyBudgetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FamilyBudget")));

builder.Services.AddScoped<IAnnualBudgetItemRepository, AnnualBudgetItemRepository>();
builder.Services.AddScoped<IAnnualReserveRepository, AnnualReserveRepository>();
builder.Services.AddSingleton<CalendarYearCycle>();
builder.Services.AddScoped<BudgetSmoothingEngine>();
builder.Services.AddScoped<AnnualBudgetQueryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>().Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDashboardEndpoints();
app.MapAnnualBudgetEndpoints();

app.Run();

/// <summary>Exposed for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.</summary>
public partial class Program;
