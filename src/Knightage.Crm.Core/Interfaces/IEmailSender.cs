namespace Knightage.Crm.Core.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);

    Task SendWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName, CancellationToken cancellationToken = default);
}
