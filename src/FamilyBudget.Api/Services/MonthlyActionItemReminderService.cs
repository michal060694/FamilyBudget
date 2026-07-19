using System.Text;
using FamilyBudget.Core.Abstractions;

namespace FamilyBudget.Api.Services;

/// <summary>
/// Finds overdue, incomplete, not-yet-reminded <see cref="Core.Entities.MonthlyActionItem"/> rows and
/// emails one combined reminder listing them, then marks each as reminded so it doesn't nag again on
/// the next check. Callable on demand (a manual "send reminders now" endpoint) and by the periodic
/// <see cref="MonthlyActionItemReminderBackgroundService"/> — mirrors how <c>MonthlyTemplateApplyService</c>
/// is shared between an endpoint and other callers.
/// </summary>
public class MonthlyActionItemReminderService
{
    private readonly IMonthlyActionItemRepository _repository;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public MonthlyActionItemReminderService(
        IMonthlyActionItemRepository repository, IEmailSender emailSender, IConfiguration configuration)
    {
        _repository = repository;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task<int> SendOverdueRemindersAsync(DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var overdue = await _repository.GetOverdueUncompletedWithoutReminderAsync(asOfDate, cancellationToken);
        if (overdue.Count == 0)
        {
            return 0;
        }

        var recipients = _configuration.GetSection("Reminders:RecipientEmails").Get<string[]>() ?? [];
        if (recipients.Length == 0)
        {
            throw new InvalidOperationException("Reminders:RecipientEmails must be configured before reminder emails can be sent.");
        }

        var body = BuildReminderBody(overdue);
        await _emailSender.SendAsync(recipients, "תזכורת: פעולות שלא בוצעו", body, cancellationToken);

        foreach (var item in overdue)
        {
            item.MarkReminderSent(asOfDate);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return overdue.Count;
    }

    private static string BuildReminderBody(IReadOnlyList<Core.Entities.MonthlyActionItem> overdueItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine("הפעולות הבאות עברו את הדד-ליין שלהן ועדיין לא סומנו כבוצעו:");
        builder.AppendLine();

        foreach (var item in overdueItems)
        {
            builder.AppendLine($"- {item.Description} (דד-ליין: {item.DeadlineDate:yyyy-MM-dd})");
        }

        return builder.ToString();
    }
}
