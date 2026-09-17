using Knightage.Crm.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.ExternalServices;

// Used whenever Smtp:Host isn't configured (the default for a fresh local checkout,
// and for production until real SMTP credentials are set), so local dev and an
// as-yet-unconfigured prod never try -- and hang or error on -- a real SMTP
// connection. Logs what would have been sent instead.
public class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Smtp:Host is not configured -- skipping real delivery. Would have sent to {ToEmail}, subject \"{Subject}\":\n{Body}",
            toEmail, subject, body);
        return Task.CompletedTask;
    }

    public Task SendWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Smtp:Host is not configured -- skipping real delivery. Would have sent to {ToEmail}, subject \"{Subject}\" with attachment {AttachmentFileName} ({AttachmentSize} bytes):\n{Body}",
            toEmail, subject, attachmentFileName, attachmentBytes.Length, body);
        return Task.CompletedTask;
    }
}
