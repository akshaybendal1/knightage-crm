using System.Text;
using System.Text.RegularExpressions;
using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Customers;

/// <summary>
/// The cleaned-up details a customer is saved with. Built only through <see cref="Create"/> so every
/// path (a won lead, the "add customer" form, the bulk import) trims, collapses spaces and derives the
/// phone key the same way -- the duplicate check in usp_Customers_Upsert depends on that.
/// </summary>
public sealed record CustomerDraft(string Name, string? Company, string? Email, string? Phone, string? PhoneKey)
{
    public const int NameMax = 200;
    public const int CompanyMax = 200;
    public const int EmailMax = 320;
    public const int PhoneMax = 50;

    /// <summary>Null when there is no usable name -- a customer must have one.</summary>
    public static CustomerDraft? Create(string? name, string? company, string? email, string? phone)
    {
        var cleanName = Clean(name, NameMax);
        if (cleanName is null)
        {
            return null;
        }

        var cleanPhone = Clean(phone, PhoneMax);
        return new CustomerDraft(cleanName, Clean(company, CompanyMax), Clean(email, EmailMax), cleanPhone, PhoneKeyOf(cleanPhone));
    }

    public static CustomerDraft? FromLead(Lead lead) => Create(lead.Name, lead.Company, lead.Email, lead.Phone);

    /// <summary>
    /// The last 10 digits of a phone number, or null when there are fewer than 7 digits (too short to
    /// say two numbers are the same). "+91 98765-43210" and "9876543210" give the same key.
    /// </summary>
    public static string? PhoneKeyOf(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
        {
            return null;
        }

        var digits = new StringBuilder(phone.Length);
        foreach (var c in phone)
        {
            if (c is >= '0' and <= '9')
            {
                digits.Append(c);
            }
        }

        if (digits.Length < 7)
        {
            return null;
        }

        return digits.Length > 10 ? digits.ToString(digits.Length - 10, 10) : digits.ToString();
    }

    public static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = Regex.Replace(value.Trim(), @"\s+", " ");
        return collapsed.Length > maxLength ? collapsed[..maxLength].TrimEnd() : collapsed;
    }
}
