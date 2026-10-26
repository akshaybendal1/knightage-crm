using FirebaseAdmin.Messaging;
using Knightage.Crm.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Knightage.Crm.Infrastructure.ExternalServices;

// FirebaseApp.Create() (the one-time SDK init this needs) runs once in Program.cs's
// startup path, not here -- it throws if called more than once per process, so it
// can't safely live inside a scoped service's constructor.
public class FcmPushSender : IPushSender
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly ILogger<FcmPushSender> _logger;

    public FcmPushSender(IDeviceTokenRepository deviceTokenRepository, ILogger<FcmPushSender> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _logger = logger;
    }

    public async Task SendAsync(
        IReadOnlyCollection<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        var message = new MulticastMessage
        {
#pragma warning disable CS0618 // Tokens is marked obsolete in favor of Fids (Firebase
            // Installations), a newer targeting mechanism the Capacitor push-notifications
            // plugin doesn't support -- it returns a classic FCM registration token via its
            // 'registration' event, so Tokens is the correct field for this client.
            Tokens = tokens.ToList(),
#pragma warning restore CS0618
            Notification = new Notification { Title = title, Body = body },
            Data = data,
        };

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);

        // Self-cleaning: a token FCM reports as unregistered/invalid (app uninstalled,
        // app data cleared, token rotated) will just keep failing every future digest
        // run otherwise -- removing it here means no separate cleanup job is needed.
        var tokenList = tokens.ToList();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            if (result.IsSuccess)
            {
                continue;
            }

            var errorCode = result.Exception?.MessagingErrorCode;
            if (errorCode == MessagingErrorCode.Unregistered || errorCode == MessagingErrorCode.InvalidArgument)
            {
                await _deviceTokenRepository.RemoveTokenAsync(tokenList[i]);
            }
            else
            {
                _logger.LogWarning(result.Exception, "Push send failed for one device token.");
            }
        }
    }
}
