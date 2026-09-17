using Knightage.Crm.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Knightage.Crm.Infrastructure.ExternalServices;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        var port = int.TryParse(smtp["Port"], out var configuredPort) ? configuredPort : 587;
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromAddress = smtp["FromAddress"] ?? throw new InvalidOperationException("Smtp:FromAddress is not configured.");
        var fromName = smtp["FromName"] ?? "Knightage CRM";
        var useStartTls = !bool.TryParse(smtp["UseStartTls"], out var configuredStartTls) || configuredStartTls;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
        if (!string.IsNullOrEmpty(username))
        {
            await client.AuthenticateAsync(username, password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    public async Task SendWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName, CancellationToken cancellationToken = default)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
        var port = int.TryParse(smtp["Port"], out var configuredPort) ? configuredPort : 587;
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromAddress = smtp["FromAddress"] ?? throw new InvalidOperationException("Smtp:FromAddress is not configured.");
        var fromName = smtp["FromName"] ?? "Knightage CRM";
        var useStartTls = !bool.TryParse(smtp["UseStartTls"], out var configuredStartTls) || configuredStartTls;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { TextBody = body };
        builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse("application/pdf"));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
        if (!string.IsNullOrEmpty(username))
        {
            await client.AuthenticateAsync(username, password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
