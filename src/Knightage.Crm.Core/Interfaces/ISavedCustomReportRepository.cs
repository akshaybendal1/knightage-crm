using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface ISavedCustomReportRepository
{
    Task<IReadOnlyList<SavedCustomReport>> GetAllAsync();
    Task<SavedCustomReport?> GetByIdAsync(Guid id);
    Task<SavedCustomReport> CreateAsync(SavedCustomReport report);
    Task UpdateAsync(SavedCustomReport report);
    Task SetActiveAsync(Guid id, bool isActive);
}
