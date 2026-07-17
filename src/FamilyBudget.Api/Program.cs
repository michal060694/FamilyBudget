using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using FamilyBudget.Api.Endpoints;
using FamilyBudget.Api.Services;
using FamilyBudget.Core.Abstractions;
using FamilyBudget.Core.Services;
using FamilyBudget.Infrastructure.Persistence;
using FamilyBudget.Infrastructure.Repositories;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<FamilyBudgetDbContext>(options =>
    options.UseNpgsql(NormalizeConnectionString(builder.Configuration.GetConnectionString("FamilyBudget"))));

builder.Services.AddScoped<IAnnualBudgetItemRepository, AnnualBudgetItemRepository>();
builder.Services.AddScoped<IAnnualReserveRepository, AnnualReserveRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITitheSettingRepository, TitheSettingRepository>();
builder.Services.AddScoped<IMonthlyExpenseBudgetItemRepository, MonthlyExpenseBudgetItemRepository>();
builder.Services.AddScoped<IFixedDonationStandingOrderRepository, FixedDonationStandingOrderRepository>();
builder.Services.AddScoped<IFundRepository, FundRepository>();
builder.Services.AddScoped<IFundEarmarkRepository, FundEarmarkRepository>();
builder.Services.AddScoped<IDebtRepository, DebtRepository>();
builder.Services.AddScoped<IMonthlyTemplateItemRepository, MonthlyTemplateItemRepository>();
builder.Services.AddSingleton<CalendarYearCycle>();
builder.Services.AddScoped<BudgetSmoothingEngine>();
builder.Services.AddScoped<AnnualBudgetQueryService>();
builder.Services.AddScoped<AnnualBudgetCopyService>();
builder.Services.AddScoped<TitheEngine>();
builder.Services.AddScoped<MonthlyOverviewQueryService>();
builder.Services.AddScoped<FundSummaryQueryService>();
builder.Services.AddScoped<DebtRepaymentService>();
builder.Services.AddScoped<MonthlyTemplateApplyService>();
builder.Services.AddScoped<ExcelExportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>().Database.Migrate();
}

// This app has no other access control (no user accounts, no per-request authorization) — a single
// shared username/password gates the whole site. Production must never boot without it configured;
// local development may still run without it for convenience.
var basicAuthUsername = builder.Configuration["BasicAuth:Username"];
var basicAuthPassword = builder.Configuration["BasicAuth:Password"];

if (app.Environment.IsProduction() && (string.IsNullOrEmpty(basicAuthUsername) || string.IsNullOrEmpty(basicAuthPassword)))
{
    throw new InvalidOperationException(
        "BasicAuth:Username and BasicAuth:Password must be set in production. This app holds private " +
        "household financial data and has no other access control — refusing to start unprotected.");
}

if (!string.IsNullOrEmpty(basicAuthUsername) && !string.IsNullOrEmpty(basicAuthPassword))
{
    app.Use(async (context, next) =>
    {
        // Render's health check hits this path with no credentials; it must stay open or every
        // deploy times out waiting for a 200 that Basic Auth will never let through.
        if (context.Request.Path == "/healthz")
        {
            await next();
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
                var separatorIndex = decoded.IndexOf(':');
                if (separatorIndex > 0 &&
                    FixedTimeEquals(decoded[..separatorIndex], basicAuthUsername) &&
                    FixedTimeEquals(decoded[(separatorIndex + 1)..], basicAuthPassword))
                {
                    await next();
                    return;
                }
            }
            catch (FormatException)
            {
                // malformed header -> fall through to 401
            }
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Basic realm=\"FamilyBudget\"";
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Ok());

app.MapDashboardEndpoints();
app.MapAnnualBudgetEndpoints();
app.MapTransactionEndpoints();
app.MapMonthlyOverviewEndpoints();
app.MapMonthlyExpenseBudgetEndpoints();
app.MapFixedDonationStandingOrderEndpoints();
app.MapFundEndpoints();
app.MapDebtEndpoints();
app.MapMonthlyTemplateEndpoints();
app.MapExportEndpoints();

app.Run();

/// <summary>Constant-time string comparison so a mistyped Basic Auth credential can't be timed out character by character.</summary>
static bool FixedTimeEquals(string a, string b)
{
    var aBytes = Encoding.UTF8.GetBytes(a);
    var bBytes = Encoding.UTF8.GetBytes(b);
    return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
}

/// <summary>
/// Hosted Postgres providers (Neon, Railway, Heroku, Supabase...) commonly hand out a libpq-style
/// URI (<c>postgresql://user:pass@host/db?sslmode=require</c>), but Npgsql's connection-string
/// parser only understands ADO.NET keyword=value pairs and throws on a URI. Converting here means
/// the provider's connection string can be pasted into config as-is.
/// </summary>
static string? NormalizeConnectionString(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw) ||
        !(raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
          raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
    {
        return raw;
    }

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);
    var query = QueryHelpers.ParseQuery(uri.Query);

    var csBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
        SslMode = SslMode.Require,
    };

    if (query.TryGetValue("sslmode", out var sslMode) && sslMode.ToString().Equals("disable", StringComparison.OrdinalIgnoreCase))
    {
        csBuilder.SslMode = SslMode.Disable;
    }

    return csBuilder.ConnectionString;
}

/// <summary>Exposed for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.</summary>
public partial class Program;
