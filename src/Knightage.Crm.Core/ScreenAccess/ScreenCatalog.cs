namespace Knightage.Crm.Core.ScreenAccess;

public enum ScreenKind
{
    /// <summary>An Owner or Admin can decide, per role, whether the screen is available.</summary>
    Configurable,

    /// <summary>Always Owner/Admin only and cannot be granted: user management and settings.</summary>
    AdminOnly,

    /// <summary>Always available to every signed-in user (My Profile).</summary>
    Everyone,
}

/// <param name="Key">Stable identifier, also used by the client's routes and menu.</param>
/// <param name="AdminDefault">Whether an Admin gets it when nothing has been customised.</param>
public record ScreenDefinition(
    string Key, string Label, string Group, ScreenKind Kind,
    bool AdminDefault, bool MemberDefault, bool SalesDefault);

/// <summary>
/// Every screen an Owner/Admin can manage access to, with the built-in defaults. The defaults are
/// exactly what the app enforced before the Access Control screen existed (Sales: only the field
/// sales screens; Member: the everyday screens; Admin: everything), so an organization that
/// never opens the screen sees no change at all.
/// </summary>
public static class ScreenCatalog
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string Sales = "Sales";

    /// <summary>Roles whose access can be customised. Owner always has every screen.</summary>
    public static readonly IReadOnlyList<string> ConfigurableRoles = new[] { Admin, Member, Sales };

    public static readonly IReadOnlyList<ScreenDefinition> All = new[]
    {
        // Everyday work -- Admin, Member (not Sales) by default.
        new ScreenDefinition("dashboard", "Dashboard", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("leads", "Leads", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("customers", "Customers", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("pipeline-stages", "Pipeline and Product Stages", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("timeline", "Timeline", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("tasks", "Tasks", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("calendar", "Calendar", "Everyday work", ScreenKind.Configurable, true, true, false),
        new ScreenDefinition("contact-us", "Contact Us", "Everyday work", ScreenKind.Configurable, true, true, false),

        // Field sales -- still needs the organization's Field Force Tracking feature turned on.
        new ScreenDefinition("sales-entry", "Add Sales Entry", "Field sales", ScreenKind.Configurable, true, true, true),
        new ScreenDefinition("sales-dashboard", "Sales Dashboard", "Field sales", ScreenKind.Configurable, true, true, true),
        new ScreenDefinition("sales-coverage", "Sales Coverage", "Field sales", ScreenKind.Configurable, true, false, false),

        // Catalog, insights and templates -- Admin only by default.
        new ScreenDefinition("products", "Products", "Catalog & insights", ScreenKind.Configurable, true, false, false),
        new ScreenDefinition("product-categories", "Product Categories", "Catalog & insights", ScreenKind.Configurable, true, false, false),
        new ScreenDefinition("reports", "Reports", "Catalog & insights", ScreenKind.Configurable, true, false, false),
        new ScreenDefinition("message-templates", "Message Templates", "Catalog & insights", ScreenKind.Configurable, true, false, false),

        // Locked: cannot be granted to anyone else (granting these would let a user manage other users
        // or the organization itself, or change this very screen).
        new ScreenDefinition("team", "Team", "Administration", ScreenKind.AdminOnly, true, false, false),
        new ScreenDefinition("organization", "Organization", "Administration", ScreenKind.AdminOnly, true, false, false),
        new ScreenDefinition("access-control", "Access Control", "Administration", ScreenKind.AdminOnly, true, false, false),

        new ScreenDefinition("profile", "My Profile", "Account", ScreenKind.Everyone, true, true, true),
    };

    private static readonly Dictionary<string, ScreenDefinition> ByKey = All.ToDictionary(s => s.Key, StringComparer.Ordinal);

    public static ScreenDefinition? Find(string? key) => key is not null && ByKey.TryGetValue(key, out var s) ? s : null;

    public static bool IsConfigurableRole(string? role) => role is not null && ConfigurableRoles.Contains(role, StringComparer.Ordinal);

    /// <summary>What a role gets for a screen when the organization has not customised that cell.</summary>
    public static bool DefaultFor(string role, ScreenDefinition screen) => role switch
    {
        Owner => true,
        Admin => screen.AdminDefault,
        Member => screen.MemberDefault,
        Sales => screen.SalesDefault,
        _ => false, // an unrecognised role (e.g. PlatformAdmin) gets nothing, like Roles="Owner,Admin" always did
    };

    /// <summary>
    /// The effective answer for one role and screen. Locked screens ignore any stored override,
    /// and Owner always has everything, so no stored data can ever lock the people who manage this
    /// screen out of it.
    /// </summary>
    public static bool IsAllowed(string role, ScreenDefinition screen, IReadOnlyDictionary<(string Role, string Screen), bool> overrides)
    {
        if (role == Owner)
        {
            return true;
        }
        if (screen.Kind != ScreenKind.Configurable)
        {
            return DefaultFor(role, screen);
        }
        return overrides.TryGetValue((role, screen.Key), out var allowed) ? allowed : DefaultFor(role, screen);
    }
}
