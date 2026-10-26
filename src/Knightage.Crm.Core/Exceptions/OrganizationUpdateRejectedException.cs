namespace Knightage.Crm.Core.Exceptions;

/// <summary>
/// knightage-identity refused an organization update for a reason the user can act on (the name is
/// already taken, empty or too long) -- as opposed to being unreachable. The message is Identity's
/// own wording, shown to the user as-is.
/// </summary>
public class OrganizationUpdateRejectedException : Exception
{
    public int StatusCode { get; }

    public OrganizationUpdateRejectedException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
