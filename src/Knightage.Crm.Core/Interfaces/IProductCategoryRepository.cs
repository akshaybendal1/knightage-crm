using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory>> GetActiveAsync();
    Task<ProductCategory?> GetByIdAsync(Guid id);
    Task<ProductCategory> CreateAsync(ProductCategory category);
    Task UpdateAsync(ProductCategory category);
    Task SetActiveAsync(Guid id, bool isActive);
}
