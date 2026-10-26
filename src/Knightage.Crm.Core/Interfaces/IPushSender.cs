namespace Knightage.Crm.Core.Interfaces;

public interface IPushSender
{
    Task SendAsync(
        IReadOnlyCollection<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
