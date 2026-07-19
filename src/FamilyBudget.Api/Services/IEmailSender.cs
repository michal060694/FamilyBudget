namespace FamilyBudget.Api.Services;

public interface IEmailSender
{
    Task SendAsync(IReadOnlyList<string> recipients, string subject, string body, CancellationToken cancellationToken = default);
}
