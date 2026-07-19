using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FamilyBudget.Api.Services;

/// <summary>
/// Sends email via Gmail SMTP (or any SMTP server) using an app-password-style credential, read the
/// same raw-config-string way as <c>BasicAuth:Username</c>/<c>Password</c> in Program.cs. Unlike Basic
/// Auth, a missing/incomplete SMTP config is not fatal at startup — email is a convenience feature,
/// not the app's access-control gate — so this only throws when actually asked to send.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(IReadOnlyList<string> recipients, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var portText = _configuration["Smtp:Port"];
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromAddress = _configuration["Smtp:FromAddress"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromAddress))
        {
            throw new InvalidOperationException(
                "Smtp:Host, Smtp:Username, Smtp:Password and Smtp:FromAddress must all be configured before email can be sent.");
        }

        if (!int.TryParse(portText, out var port))
        {
            port = 587;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
