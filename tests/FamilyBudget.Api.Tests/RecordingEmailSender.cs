using FamilyBudget.Api.Services;

namespace FamilyBudget.Api.Tests;

/// <summary>Test double for <see cref="IEmailSender"/> — records every call instead of hitting real SMTP, so the reminder path is verifiable without Gmail credentials.</summary>
public class RecordingEmailSender : IEmailSender
{
    public List<(IReadOnlyList<string> Recipients, string Subject, string Body)> SentEmails { get; } = [];

    public Task SendAsync(IReadOnlyList<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((recipients, subject, body));
        return Task.CompletedTask;
    }
}
