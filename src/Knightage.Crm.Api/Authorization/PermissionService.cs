using System.Security.Claims;
using Knightage.Crm.Core.Interfaces;
using Knightage.Crm.Core.Permissions;
using Knightage.Crm.Core.ScreenAccess;
using Knightage.Crm.Core.Tenancy;

namespace Knightage.Crm.Api.Authorization;

/// <summary>
/// Works out what the signed-in user may do in CRM, once per request:
/// the Owner may do everything; anyone else starts from their role's Access Control setting for each
/// screen (a role that can open a screen can do everything on it -- CRM's behaviour before per-user
/// access) with their own exceptions from Platform's Users screen on top. An action other than View
/// also needs View: someone who can't see an area can't change it either.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantContext _tenantContext;
    private readonly IScreenAccessService _screenAccessService;
    private readonly IUserAccessProvider _userAccessProvider;
    private EffectivePermissions? _current;

    public PermissionService(IHttpContextAccessor httpContextAccessor, TenantContext tenantContext, IScreenAccessService screenAccessService, IUserAccessProvider userAccessProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
        _screenAccessService = screenAccessService;
        _userAccessProvider = userAccessProvider;
    }

    public async Task<bool> CanAsync(string feature, string action) => (await GetCurrentAsync()).Can(feature, action);

    public async Task<EffectivePermissions> GetCurrentAsync()
    {
        if (_current is not null)
        {
            return _current;
        }

        var context = _httpContextAccessor.HttpContext;
        var user = context?.User;
        var role = user?.FindFirst(ClaimTypes.Role)?.Value;

        var access = UserAccessSnapshot.None;
        if (role != ScreenCatalog.Owner
            && _tenantContext.OrganizationId is Guid organizationId
            && Guid.TryParse(user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value, out var userId))
        {
            access = await _userAccessProvider.GetAsync(organizationId, userId, context!.Request.Headers.Authorization.ToString(), context.RequestAborted);
        }

        var roleScreens = (await _screenAccessService.GetAllowedScreensAsync(role)).ToHashSet(StringComparer.Ordinal);
        var features = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var feature in PermissionCatalog.Features)
        {
            var byDefault = roleScreens.Contains(feature.Key);
            bool Allowed(string action) => role == ScreenCatalog.Owner
                || (access.Override($"{PermissionCatalog.Service}:{feature.Key}:{action}") ?? byDefault);

            var canView = Allowed(PermissionActions.View);
            features[feature.Key] = canView
                ? feature.Actions.Where(a => a == PermissionActions.View || Allowed(a)).ToList()
                : Array.Empty<string>();
        }

        _current = new EffectivePermissions(
            role == ScreenCatalog.Owner || access.AppAllowed(PermissionCatalog.Service),
            new[] { "Crm", "InventorySales", "Accounting" }.ToDictionary(s => s, s => role == ScreenCatalog.Owner || access.AppAllowed(s)),
            features);
        return _current;
    }
}
