namespace Knightage.Crm.Core.Interfaces;

/// <summary>
/// One user's exceptions to their role defaults, as set on Platform's Users screen: which Knightage apps
/// they may open (keyed "Crm", "InventorySales", "Accounting") and, inside apps, "Crm:leads:add" -> false.
/// </summary>
public record UserAccessSnapshot(IReadOnlyDictionary<string, bool> Apps, IReadOnlyDictionary<string, bool> Overrides)
{
    /// <summary>What everyone has when nothing is stored (or Platform can't be asked): every app, role defaults.</summary>
    public static readonly UserAccessSnapshot None = new(new Dictionary<string, bool>(), new Dictionary<string, bool>());

    public bool AppAllowed(string service) => !Apps.TryGetValue(service, out var allowed) || allowed;

    public bool? Override(string key) => Overrides.TryGetValue(key, out var allowed) ? allowed : null;
}

public interface IUserAccessProvider
{
    /// <summary>Never throws: when Platform can't be reached, the last answer seen is used, else <see cref="UserAccessSnapshot.None"/>.</summary>
    Task<UserAccessSnapshot> GetAsync(Guid organizationId, Guid userId, string? authorizationHeader, CancellationToken cancellationToken = default);
}

/// <summary>What the signed-in user may do in this app (area key -> allowed actions), and which Knightage apps they may open.</summary>
public record EffectivePermissions(bool AppAccess, IReadOnlyDictionary<string, bool> Apps, IReadOnlyDictionary<string, IReadOnlyList<string>> Features)
{
    public bool Can(string feature, string action) => AppAccess && Features.TryGetValue(feature, out var actions) && actions.Contains(action);
}

public interface IPermissionService
{
    /// <summary>The caller's effective permissions, worked out once per request.</summary>
    Task<EffectivePermissions> GetCurrentAsync();

    Task<bool> CanAsync(string feature, string action);
}
