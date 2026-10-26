using Knightage.Crm.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.ExternalServices;

// Used whenever Fcm:ServiceAccountKeyPath isn't configured (the default for a fresh
// local checkout, and for production until a real Firebase service account is set),
// so nothing tries -- and fails -- to call FCM without credentials. Logs what would
// have been sent instead. Mirrors NullEmailSender.
public class NullPushSender : IPushSender
{
    private readonly ILogger<NullPushSender> _logger;

    public NullPushSender(ILogger<NullPushSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        IReadOnlyCollection<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fcm:ServiceAccountKeyPath is not configured -- skipping real delivery. Would have sent push to {TokenCount} device(s), title \"{Title}\": {Body}",
            tokens.Count, title, body);
        return Task.CompletedTask;
    }
}
