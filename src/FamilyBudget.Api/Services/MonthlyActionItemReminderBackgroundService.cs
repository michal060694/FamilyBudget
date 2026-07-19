namespace FamilyBudget.Api.Services;

/// <summary>
/// Periodically checks for overdue action items and emails a reminder. Registered as a singleton
/// <see cref="IHostedService"/> but needs a scoped <see cref="MonthlyActionItemReminderService"/> (and,
/// through it, a scoped DbContext) — so each tick opens its own DI scope rather than resolving
/// scoped services directly. A 5-minute initial delay guarantees this never fires mid test-run (the
/// whole test suite finishes in seconds), so no explicit test-environment branching is needed. Any
/// failure (e.g. SMTP not yet configured) is caught and logged so a bad reminder check never takes
/// down the app.
/// </summary>
public class MonthlyActionItemReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyActionItemReminderBackgroundService> _logger;

    public MonthlyActionItemReminderBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<MonthlyActionItemReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            await CheckOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reminderService = scope.ServiceProvider.GetRequiredService<MonthlyActionItemReminderService>();
            var today = DateOnly.FromDateTime(DateTime.Now);
            var sentCount = await reminderService.SendOverdueRemindersAsync(today, cancellationToken);

            if (sentCount > 0)
            {
                _logger.LogInformation("Sent overdue-action-item reminder email covering {Count} item(s).", sentCount);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to check/send overdue action item reminders.");
        }
    }
}
