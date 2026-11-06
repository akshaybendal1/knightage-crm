namespace Knightage.Crm.Core.Permissions;

public static class PermissionActions
{
    public const string View = "view";
    public const string Add = "add";
    public const string Edit = "edit";
    public const string Delete = "delete";
}

public record PermissionFeature(string Key, IReadOnlyList<string> Actions);

/// <summary>
/// The CRM areas whose access an organization's Owner / Admins can set per user on Platform's Users screen,
/// and which actions each has. The keys are ScreenCatalog's configurable screens; the role defaults are
/// that screen's Access Control setting (a role that can open a screen can do everything on it, which is
/// what CRM allowed before per-user access existed).
///
/// knightage-platform's AccessCatalog holds the same list (with labels) for its screen -- keep them in step.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>The Platform service key: per-user exceptions arrive as "Crm" (open CRM) and "Crm:leads:add".</summary>
    public const string Service = "Crm";

    private static readonly string[] ViewOnly = { PermissionActions.View };
    private static readonly string[] Full = { PermissionActions.View, PermissionActions.Add, PermissionActions.Edit, PermissionActions.Delete };

    public static readonly IReadOnlyList<PermissionFeature> Features = new[]
    {
        new PermissionFeature("dashboard", ViewOnly),
        new PermissionFeature("leads", Full),
        new PermissionFeature("customers", new[] { PermissionActions.View, PermissionActions.Add }),
        new PermissionFeature("pipeline-stages", Full),
        new PermissionFeature("timeline", ViewOnly),
        new PermissionFeature("tasks", new[] { PermissionActions.View, PermissionActions.Add, PermissionActions.Edit }),
        new PermissionFeature("calendar", new[] { PermissionActions.View, PermissionActions.Add, PermissionActions.Edit }),
        new PermissionFeature("contact-us", ViewOnly),
        new PermissionFeature("sales-entry", new[] { PermissionActions.View, PermissionActions.Add }),
        new PermissionFeature("sales-dashboard", ViewOnly),
        new PermissionFeature("sales-coverage", ViewOnly),
        new PermissionFeature("products", Full),
        new PermissionFeature("product-categories", Full),
        new PermissionFeature("reports", ViewOnly),
        new PermissionFeature("message-templates", Full),
    };

    private static readonly Dictionary<string, PermissionFeature> ByKey = Features.ToDictionary(f => f.Key, StringComparer.Ordinal);

    public static PermissionFeature? Find(string? key) => key is not null && ByKey.TryGetValue(key, out var f) ? f : null;
}
