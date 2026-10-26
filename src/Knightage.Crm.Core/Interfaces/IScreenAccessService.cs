using Knightage.Crm.Core.ScreenAccess;

namespace Knightage.Crm.Core.Interfaces;

/// <summary>One stored cell that differs from the default: this role may (or may not) open this screen.</summary>
public record ScreenAccessOverride(string Role, string ScreenKey, bool IsAllowed);

public interface IRoleScreenAccessRepository
{
    Task<IReadOnlyList<ScreenAccessOverride>> GetOverridesAsync();

    /// <summary>Replaces this organization's whole override set atomically. An empty list resets it to the defaults.</summary>
    Task ReplaceOverridesAsync(IReadOnlyCollection<ScreenAccessOverride> overrides, string? updatedByUserId);
}

/// <summary>The access matrix as the Access Control screen shows it.</summary>
public record ScreenAccessMatrix(IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> AllowedByRole);

public interface IScreenAccessService
{
    /// <summary>Keys of every screen this role may open in the caller's organization.</summary>
    Task<IReadOnlyList<string>> GetAllowedScreensAsync(string? role);

    Task<bool> CanAccessAsync(string? role, string screenKey);

    /// <summary>Effective access for every configurable role and every screen.</summary>
    Task<ScreenAccessMatrix> GetMatrixAsync();

    /// <summary>
    /// Saves the full set of configurable cells. Cells equal to the default are not stored. Throws
    /// <see cref="ArgumentException"/> (message safe to show) for an unknown role/screen or a locked cell.
    /// </summary>
    Task SaveMatrixAsync(IReadOnlyDictionary<string, IReadOnlyDictionary<string, bool>> allowedByRole, string? updatedByUserId);
}
