using System.Text.Json.Serialization;
using FamilyBudget.Api.Endpoints;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Services;
using FamilyBudget.Infrastructure.Persistence;
using FamilyBudget.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<FamilyBudgetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("FamilyBudget")));

builder.Services.AddScoped<IAnnualBudgetItemRepository, AnnualBudgetItemRepository>();
builder.Services.AddScoped<IAnnualReserveRepository, AnnualReserveRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITitheSettingRepository, TitheSettingRepository>();
builder.Services.AddScoped<IMonthlyExpenseBudgetItemRepository, MonthlyExpenseBudgetItemRepository>();
builder.Services.AddSingleton<CalendarYearCycle>();
builder.Services.AddScoped<BudgetSmoothingEngine>();
builder.Services.AddScoped<AnnualBudgetQueryService>();
builder.Services.AddScoped<TitheEngine>();
builder.Services.AddScoped<MonthlyOverviewQueryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>().Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDashboardEndpoints();
app.MapAnnualBudgetEndpoints();
app.MapTransactionEndpoints();
app.MapMonthlyOverviewEndpoints();
app.MapMonthlyExpenseBudgetEndpoints();

app.Run();

/// <summary>Exposed for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.</summary>
public partial class Program;
