namespace Knightage.Crm.Core.Models;

public class OrganizationBranding
{
    // Reference-equality sentinel so OrganizationBrandingRepository's cache can
    // distinguish "no branding row for this org" from "not looked up yet" -- IMemoryCache's
    // GetOrCreateAsync has no way to cache a null result as present otherwise.
    public static readonly OrganizationBranding NotFound = new();

    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string LogoContentType { get; set; } = string.Empty;
    public byte[] LogoData { get; set; } = Array.Empty<byte>();
    public string? UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
