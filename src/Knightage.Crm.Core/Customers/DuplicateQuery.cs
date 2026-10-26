namespace Knightage.Crm.Core.Customers;

/// <summary>
/// What someone typed into a "new lead" (or customer) form, cleaned up the same way a customer is saved, so the duplicate
/// check agrees with usp_Customers_Upsert: same email, same phone (last 10 digits), or same name + company.
/// </summary>
public sealed record DuplicateQuery(string? Name, string? Company, string? Email, string? PhoneKey)
{
    /// <summary>True when there is nothing worth looking up (all fields blank).</summary>
    public bool IsEmpty => Name is null && Email is null && PhoneKey is null;

    public static DuplicateQuery Create(string? name, string? company, string? email, string? phone) =>
        new(
            CustomerDraft.Clean(name, CustomerDraft.NameMax),
            CustomerDraft.Clean(company, CustomerDraft.CompanyMax),
            CustomerDraft.Clean(email, CustomerDraft.EmailMax),
            CustomerDraft.PhoneKeyOf(CustomerDraft.Clean(phone, CustomerDraft.PhoneMax)));
}

/// <param name="MatchedOn">"email", "phone" or "name+company": why this lead was flagged (strongest reason first).</param>
public sealed record LeadMatch(Guid Id, string Name, string? Company, string? Email, string? Phone, string? StageName, string? OwnerUserId, DateTime CreatedAtUtc, string MatchedOn);

public sealed record CustomerMatch(Guid Id, string Name, string? Company, string? Email, string? Phone, string MatchedOn);
