using Knightage.Crm.Core.Models;

namespace Knightage.Crm.Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task SetActiveAsync(Guid id, bool isActive);
}
