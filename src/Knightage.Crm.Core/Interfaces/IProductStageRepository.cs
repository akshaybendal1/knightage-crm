using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IProductStageRepository
{
    Task<IReadOnlyList<ProductStage>> GetActiveAsync();
    Task<ProductStage?> GetByIdAsync(Guid id);
    Task<ProductStage> CreateAsync(ProductStage stage);
    Task UpdateAsync(ProductStage stage);
    Task SetActiveAsync(Guid id, bool isActive);
}
